using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The versus half of the manager: several players at one board, taking turns against a turn timer
    /// (<see cref="VersusMatch"/>). On one device the round of the manager judges the flips as it does for one player,
    /// and the players pass the device around. Online the server deals and judges
    /// (<see cref="MemoryCardsOnlineController"/>): the round here only mirrors the board, a click asks the server for
    /// a flip, and what the server answers is played with the same animations as a local flip.
    /// </summary>
    public partial class MemoryCardsGameManager
    {
        /// <summary>A card as the server shows it: where it lies and, while face up, what it is.</summary>
        internal struct OnlineCard
        {
            public int Index;
            public CardKind Kind;
            /// <summary>Index of the animal in the pool of the level; -1 for special cards.</summary>
            public int Animal;
        }

        /// <summary>What the server said a flip did (its FlipEvent), in the terms of this game.</summary>
        internal sealed class OnlineFlip
        {
            public int Seat;
            public int Card;
            /// <summary>A flip, or one of the server's own events (<see cref="FlipOutcome.FlippedBack"/> and the ones after it).</summary>
            public FlipOutcome Outcome;
            public int Points;
            public int Combo;
            public readonly List<OnlineCard> Cards = new List<OnlineCard>();
        }

        /// <summary>The board of an online game as the server dealt it, and who sits where.</summary>
        internal sealed class OnlineBoard
        {
            public int Cards;
            public int Columns;
            public int MatchSize;
            public int Sets;
            public float PreviewSeconds;
            public bool[] Frozen;
            public string[] Names;
            public int LocalSeat;
            /// <summary>The faces of every card, when the board is shown before the first turn (memorize).</summary>
            public List<OnlineCard> Preview;
        }

        [SerializeField] internal MemoryCardsOnlineController online;

        private readonly List<MemoryCardsPlayer> seatPlayers = new List<MemoryCardsPlayer>();
        /// <summary>What the server sent, in its order: flips to play and turns to begin, taken while the board is free.</summary>
        private readonly Queue<System.Action> onlineEvents = new Queue<System.Action>();
        /// <summary>Until when the banner of the last flip (a bomb, a lost turn) shows: the next turn is announced after it.</summary>
        private float bannerBusyUntil;
        private VersusMatch versus;
        private OnlineBoard onlineBoard;
        private int localPlayers = 1;
        private int localSeat = -1;
        private int actingSeat = -1;
        private int onlineSets;
        private bool versusOver;
        /// <summary>The server began the first turn of the online game: until then nobody can flip.</summary>
        private bool onlineTurnBegan;
        /// <summary>The mirror of an online board: a mistake shows until the server turns it back.</summary>
        private bool mirrorMismatch;
        /// <summary>The animals of the mirror round, by the order they turned up: their index in the pool of the level.</summary>
        private List<int> mirrorAnimals = new List<int>();

        /// <summary>Several players share the board.</summary>
        internal bool IsVersus => versus != null;

        /// <summary>A versus game judged by the server.</summary>
        internal bool IsOnlineVersus => versus != null && onlineBoard != null;

        internal VersusMatch Versus => versus;

        /// <summary>The player a flip counts for: the seat that acts in a versus game, else the one player.</summary>
        private MemoryCardsPlayer Acting => versus != null && actingSeat >= 0 && actingSeat < seatPlayers.Count ? seatPlayers[actingSeat] : player;

        /// <summary>The colour points pop up in: the acting seat's in a versus game.</summary>
        private Color PointsColor => versus != null && gameUI != null ? gameUI.VersusSeatColor(actingSeat) : gold;

        private bool CanPlayVersus(MemoryCardsLevel level)
        {
            return level != null && !level.IsEndless;
        }

        #region Level select

        /// <summary>The players button of the level select: one player, or a versus game for two to four at this device.</summary>
        public void CyclePlayers()
        {
            if (phase != Phase.Menu)
            {
                return;
            }
            PlayClick();
            localPlayers = localPlayers % VersusMatch.MaxPlayers + 1;
            RefreshPlayers();
        }

        protected override IOnlineLobby OnlineLobby => online;

        /// <summary>The online button of the level select: opens the lobby of the game's server.</summary>
        public override void OpenOnline()
        {
            if (phase != Phase.Menu)
            {
                return;
            }
            PlayClick();
            base.OpenOnline();
        }

        private void RefreshPlayers()
        {
            bool allowed = campaign != null && CanPlayVersus(campaign.Level(selectedLevel));
            gameUI?.ShowPlayers(allowed ? localPlayers : 1, allowed);
        }

        #endregion


        #region Setting up

        /// <summary>The online controller hands over the board of the game that is about to start.</summary>
        internal void PrepareOnlineGame(OnlineBoard board)
        {
            onlineBoard = board;
            onlineEvents.Clear();
        }

        /// <summary>The flip of another player, or the answer to the local player's own, in the order of the server.</summary>
        internal void OnlineFlipArrived(OnlineFlip flip)
        {
            onlineEvents.Enqueue(() => PlayOnlineFlip(flip));
        }

        /// <summary>The server began a turn; it is taken after the flips that came before it (the one that ended the last turn).</summary>
        internal void OnlineTurn(int seat, int number)
        {
            onlineEvents.Enqueue(() => BeginOnlineTurn(seat, number));
        }

        private void BeginOnlineTurn(int seat, int number)
        {
            if (!IsOnlineVersus)
            {
                return;
            }
            bool changed = seat != versus.Current || !onlineTurnBegan;
            onlineTurnBegan = true;
            versus.SetTurn(seat, number, 0f);
            if (changed && phase != Phase.Finished)
            {
                // The banner of a bomb or a lost turn shows first; the new turn is announced after it.
                float wait = bannerBusyUntil - Time.time;
                if (wait > 0f)
                {
                    After(wait, AnnounceTurn);
                }
                else
                {
                    AnnounceTurn();
                }
            }
            RefreshVersusInput();
        }

        /// <summary>The scores are the server's; a seat whose member is gone stops playing.</summary>
        internal void OnlineSeat(int seat, int score, bool playing)
        {
            if (!IsOnlineVersus || seat < 0 || seat >= versus.Seats.Count)
            {
                return;
            }
            versus.Seats[seat].Score = score;
            versus.Seats[seat].Playing = playing;
            SyncSeatPlayers();
        }

        /// <summary>The server finished the game: the results show once the last flip played out.</summary>
        internal void OnlineFinished()
        {
            if (IsOnlineVersus)
            {
                versusOver = true;
            }
        }

        /// <summary>Whether the game that starts has several players, and sets them up when it does.</summary>
        private bool SetUpVersus(MemoryCardsLevel level)
        {
            ClearVersus(keepOnlineBoard: true);
            if (InSession && onlineBoard != null)
            {
                localSeat = onlineBoard.LocalSeat;
                versus = new VersusMatch(onlineBoard.Names, 0f);
            }
            else if (localPlayers >= VersusMatch.MinPlayers && CanPlayVersus(level))
            {
                onlineBoard = null;
                localSeat = -1;
                var names = new List<string>();
                for (int i = 0; i < localPlayers; i++)
                {
                    names.Add($"Player {i + 1}");
                }
                versus = new VersusMatch(names, VersusMatch.DefaultTurnSeconds);
            }
            else
            {
                onlineBoard = null;
                return false;
            }
            SeatPlayers();
            return true;
        }

        /// <summary>The rules of the board for the players that share it; an online board has the size the server dealt.</summary>
        private RoundRules VersusRules(RoundRules board)
        {
            RoundRules shared = VersusMatch.RulesFor(board);
            if (onlineBoard != null)
            {
                shared.Cards = onlineBoard.Cards;
                shared.Columns = onlineBoard.Columns;
                shared.MatchSize = onlineBoard.MatchSize;
                shared.PreviewTime = onlineBoard.PreviewSeconds;
            }
            return shared;
        }

        /// <summary>A player component per seat: the one of the scene for the first seat, more made as they are needed.</summary>
        private void SeatPlayers()
        {
            if (seatPlayers.Count == 0 && player != null)
            {
                seatPlayers.Add(player);
            }
            while (seatPlayers.Count < versus.Seats.Count && player != null)
            {
                var seatObject = new GameObject($"Player {seatPlayers.Count + 1}");
                seatObject.transform.SetParent(player.transform.parent, false);
                var seatPlayer = seatObject.AddComponent<MemoryCardsPlayer>();
                RegisterPlayer(seatPlayer);
                seatPlayers.Add(seatPlayer);
            }
            var active = new List<IPlayer>();
            for (int i = 0; i < versus.Seats.Count && i < seatPlayers.Count; i++)
            {
                PlayerControl control = onlineBoard == null || i == localSeat ? PlayerControl.Local : PlayerControl.Remote;
                seatPlayers[i].Assign(i, control, versus.Seats[i].Name);
                seatPlayers[i].ResetMatches();
                active.Add(seatPlayers[i]);
            }
            ActivePlayers = active;
        }

        private void ClearVersus(bool keepOnlineBoard = false)
        {
            versus = null;
            versusOver = false;
            onlineTurnBegan = false;
            actingSeat = -1;
            localSeat = -1;
            onlineSets = 0;
            if (!keepOnlineBoard)
            {
                onlineBoard = null;
                onlineEvents.Clear();
            }
            if (player != null)
            {
                player.Assign(-1, PlayerControl.Local);
                ActivePlayers = new List<IPlayer> { player };
            }
        }

        /// <summary>The board of an online game before anybody turned a card: every face is unknown.</summary>
        private Deal MirrorDeal()
        {
            var deal = new Deal();
            mirrorAnimals = deal.Animals;
            mirrorMismatch = false;
            for (int i = 0; i < onlineBoard.Cards; i++)
            {
                deal.Cards.Add(new MemoryCard
                {
                    Id = i,
                    Slot = i,
                    Kind = CardKind.Animal,
                    Animal = -1,
                    Frozen = onlineBoard.Frozen != null && i < onlineBoard.Frozen.Length && onlineBoard.Frozen[i]
                });
            }
            return deal;
        }

        private RoundHud VersusHudSetup(RoundHud setup)
        {
            setup.Versus = true;
            setup.Countdown = false;
            setup.Hearts = 0;
            setup.MoveLimit = 0;
            setup.Parade = false;
            setup.VersusLabel = onlineBoard != null ? $"ONLINE - {versus.Seats.Count} PLAYERS" : $"VERSUS - {versus.Seats.Count} PLAYERS";
            return setup;
        }

        #endregion


        #region Playing

        /// <summary>Runs the versus game every frame: the clock of a local turn, the flips that arrived from the server.</summary>
        private void UpdateVersus(float deltaTime)
        {
            if (versus == null || round == null)
            {
                return;
            }
            if (IsOnlineVersus)
            {
                while (phase == Phase.Playing && onlineEvents.Count > 0)
                {
                    onlineEvents.Dequeue()();
                }
                if (versusOver && phase == Phase.Playing && onlineEvents.Count == 0)
                {
                    Finish();
                }
            }
            else if (phase == Phase.Playing && !round.IsOver && !round.MismatchShowing && versus.Tick(deltaTime))
            {
                LocalTurnTimedOut();
            }
            RefreshVersusHud();
        }

        /// <summary>A local flip was judged by the round: the versus game books it for the player whose turn it is.</summary>
        private VersusOutcome BookLocalFlip(FlipResult result)
        {
            // A bomb passes the turn at once and takes a half finished set back with it (result.FlippedBack).
            VersusOutcome outcome = versus.Apply(result, round);
            if (outcome.PassesNow && !round.IsOver)
            {
                // The turn passed already; the next player hears about it once the bomb went off.
                After(flipTime + 0.9f, AnnounceTurn);
            }
            SyncSeatPlayers();
            return outcome;
        }

        /// <summary>A mistake turned back: on one device the next player is up.</summary>
        private void MismatchHidden()
        {
            if (versus != null && !IsOnlineVersus && round != null && !round.IsOver)
            {
                versus.PassTurn();
                AnnounceTurn();
            }
        }

        private void LocalTurnTimedOut()
        {
            foreach (MemoryCard card in round.HideRevealed())
            {
                ViewOf(card)?.FlipDown();
            }
            unflipTimer = -1f;
            sounds?.Play(sounds.mismatch, 0.7f, 0.85f);
            gameUI?.ShowBanner("TIME'S UP!", $"{versus.CurrentSeat.Name} ran out of time", badColor, 1.1f);
            versus.PassTurn();
            After(1.1f, AnnounceTurn);
        }

        private void AnnounceTurn()
        {
            if (versus == null || round == null || phase == Phase.Finished || phase == Phase.Menu || (IsOnlineVersus && !onlineTurnBegan))
            {
                return;
            }
            VersusSeat seat = versus.CurrentSeat;
            bool mine = IsOnlineVersus && seat.Seat == localSeat;
            string title = mine ? "YOUR TURN!" : $"{seat.Name.ToUpperInvariant()}'S TURN";
            gameUI?.ShowBanner(title, null, gameUI.VersusSeatColor(seat.Seat), 0.9f);
            sounds?.Play(sounds.select, 0.7f, mine || !IsOnlineVersus ? 1.1f : 0.9f);
        }

        /// <summary>Online only the player whose turn it is can click; at one device the cards are everybody's.</summary>
        private void RefreshVersusInput()
        {
            if (IsOnlineVersus && phase == Phase.Playing)
            {
                SetCardsInteractable(onlineTurnBegan && versus.Current == localSeat);
            }
        }

        /// <summary>A click in an online game: the server flips the card, when it is the local player's turn.</summary>
        private void RequestOnlineFlip(Flippable view)
        {
            MemoryCard card = round.Cards[view.Index];
            if (!onlineTurnBegan || versus.Current != localSeat || card.State != CardState.Hidden || mirrorMismatch || online == null)
            {
                return;
            }
            online.Flip(view.Index);
        }

        /// <summary>Plays a flip the server judged: the mirror of the board follows, and the view does what a local flip does.</summary>
        private void PlayOnlineFlip(OnlineFlip flip)
        {
            actingSeat = flip.Seat;
            foreach (OnlineCard shown in flip.Cards)
            {
                Learn(shown);
            }
            if (flip.Outcome == FlipOutcome.FlippedBack || flip.Outcome == FlipOutcome.TimedOut)
            {
                foreach (OnlineCard turned in flip.Cards)
                {
                    MemoryCard back = CardAt(turned.Index);
                    if (back != null)
                    {
                        back.State = CardState.Hidden;
                        ViewOf(back)?.FlipDown();
                    }
                }
                mirrorMismatch = false;
                if (flip.Outcome == FlipOutcome.TimedOut && flip.Seat >= 0 && flip.Seat < versus.Seats.Count)
                {
                    sounds?.Play(sounds.mismatch, 0.7f, 0.85f);
                    string who = flip.Seat == localSeat ? "You" : versus.Seats[flip.Seat].Name;
                    gameUI?.ShowBanner("TIME'S UP!", $"{who} ran out of time", badColor, 1.1f);
                    bannerBusyUntil = Time.time + 1.1f;
                }
                return;
            }

            MemoryCard card = CardAt(flip.Card);
            Flippable view = ViewOf(card);
            if (card == null || view == null)
            {
                return;
            }
            var result = new FlipResult { Outcome = flip.Outcome, Card = card, Points = flip.Points, Combo = flip.Combo };
            foreach (OnlineCard part in flip.Cards)
            {
                MemoryCard other = CardAt(part.Index);
                if (other != null)
                {
                    result.Cards.Add(other);
                }
            }
            VersusSeat seat = flip.Seat >= 0 && flip.Seat < versus.Seats.Count ? versus.Seats[flip.Seat] : null;
            switch (result.Outcome)
            {
                case FlipOutcome.Cracked:
                    card.Frozen = false;
                    result.Cards.Clear();
                    break;
                case FlipOutcome.Revealed:
                    card.State = CardState.Revealed;
                    break;
                case FlipOutcome.Matched:
                case FlipOutcome.WildMatched:
                    foreach (MemoryCard matched in result.Cards)
                    {
                        matched.State = CardState.Matched;
                        matched.Frozen = false;
                    }
                    // Two wild cards that cancel out score, but find no set.
                    result.SetCompleted = result.Cards.Exists(matched => matched.IsAnimal);
                    if (seat != null && result.SetCompleted)
                    {
                        seat.Sets++;
                        seat.BestCombo = Mathf.Max(seat.BestCombo, flip.Combo);
                        onlineSets++;
                    }
                    break;
                case FlipOutcome.Mismatched:
                    card.State = CardState.Revealed;
                    mirrorMismatch = true;
                    if (seat != null)
                    {
                        seat.Mistakes++;
                    }
                    break;
                case FlipOutcome.Bomb:
                case FlipOutcome.Peek:
                    card.State = CardState.Spent;
                    // The cards of a peek are the ones it shows, not a set.
                    result.Cards.Clear();
                    if (result.Outcome == FlipOutcome.Bomb)
                    {
                        // The next turn, which came with the bomb, is announced once it went off.
                        bannerBusyUntil = Time.time + flipTime + 0.9f;
                    }
                    break;
            }
            SyncSeatPlayers();
            PlayFlip(view, result, flip.Points);
        }

        /// <summary>A face the server showed: the mirror card and its view know it from now on.</summary>
        private void Learn(OnlineCard shown)
        {
            MemoryCard card = CardAt(shown.Index);
            if (card == null || shown.Kind == CardKind.Unknown)
            {
                return;
            }
            card.Kind = shown.Kind;
            card.Animal = -1;
            Sprite face;
            if (shown.Kind == CardKind.Animal)
            {
                // The round counts animals from 0 in the order they turn up; the deal maps them to the pool.
                int roundAnimal = mirrorAnimals.IndexOf(shown.Animal);
                if (roundAnimal < 0)
                {
                    mirrorAnimals.Add(shown.Animal);
                    roundAnimal = mirrorAnimals.Count - 1;
                }
                card.Animal = roundAnimal;
                face = shown.Animal >= 0 && shown.Animal < animalPool.Count ? animalPool[shown.Animal] : null;
            }
            else
            {
                int special = (int)shown.Kind - 1;
                face = specialFaces != null && special >= 0 && special < specialFaces.Length ? specialFaces[special] : null;
            }
            ViewOf(card)?.SetFace(face);
        }

        private MemoryCard CardAt(int index)
        {
            return round != null && index >= 0 && index < round.Cards.Count ? round.Cards[index] : null;
        }

        /// <summary>The player components follow the seats of the versus game.</summary>
        private void SyncSeatPlayers()
        {
            for (int i = 0; versus != null && i < versus.Seats.Count && i < seatPlayers.Count; i++)
            {
                seatPlayers[i].Score = versus.Seats[i].Score;
            }
        }

        private void RefreshVersusHud()
        {
            if (versus == null || gameUI == null)
            {
                return;
            }
            float fraction = 1f;
            float seconds = -1f;
            if (IsOnlineVersus)
            {
                Gamebox.Online.RoomTurnInfo turn = online != null && online.ServerClient != null ? online.ServerClient.Turn : null;
                if (turn != null && turn.HasLimit)
                {
                    fraction = turn.RemainingFraction;
                    seconds = turn.Remaining;
                }
            }
            else if (versus.HasTurnLimit)
            {
                fraction = versus.TurnLeft / versus.TurnSeconds;
                seconds = versus.TurnLeft;
            }
            if (phase != Phase.Playing && phase != Phase.Busy)
            {
                seconds = -1f;
            }
            PlayerScore = localSeat >= 0 && localSeat < versus.Seats.Count ? versus.Seats[localSeat].Score : versus.Standings()[0].Score;
            gameUI.UpdateVersus(versus.Seats, IsOnlineVersus && !onlineTurnBegan ? -1 : versus.Current, fraction, seconds);
            if (seconds >= 0f && seconds <= 5f && (!IsOnlineVersus || versus.Current == localSeat))
            {
                int second = Mathf.CeilToInt(seconds);
                if (second > 0 && second != lastTick)
                {
                    lastTick = second;
                    sounds?.Play(sounds.tick, 0.7f, 1.1f);
                }
            }
            else
            {
                lastTick = -1;
            }
        }

        #endregion


        #region Finishing

        private IEnumerator VersusFinishRoutine()
        {
            yield return new WaitForSeconds(flipTime + 0.45f);
            CelebrateCards();
            particles?.Confetti(110, ConfettiColors());
            sounds?.Play(sounds.victory);
            List<VersusSeat> winners = versus.Winners();
            string title = winners.Count > 1 ? "IT'S A DRAW!" : winners[0].Seat == localSeat ? "YOU WIN!" : $"{winners[0].Name.ToUpperInvariant()} WINS!";
            Color color = winners.Count > 1 ? gold : gameUI != null ? gameUI.VersusSeatColor(winners[0].Seat) : gold;
            gameUI?.ShowBanner(title, null, color, 1.6f);
            yield return new WaitForSeconds(1.9f);

            bool localWon = localSeat < 0 || winners.Exists(seat => seat.Seat == localSeat);
            var standings = new StringBuilder();
            List<VersusSeat> ranking = versus.Standings();
            for (int i = 0; i < ranking.Count; i++)
            {
                VersusSeat seat = ranking[i];
                string name = seat.Seat == localSeat ? $"{seat.Name} (you)" : seat.Name;
                standings.Append(i > 0 ? "\n" : string.Empty)
                    .Append($"{i + 1}.  <b>{name}</b>   {seat.Score.ToString("N0", CultureInfo.InvariantCulture)}")
                    .Append($"   <size=70%>{seat.Sets} {(seat.Sets == 1 ? "set" : "sets")}</size>");
            }
            MemoryCardsLevel level = CardsLevel;
            int sets = 0;
            int bestCombo = 0;
            foreach (VersusSeat seat in versus.Seats)
            {
                if (localSeat < 0 || seat.Seat == localSeat)
                {
                    sets += seat.Sets;
                    bestCombo = Mathf.Max(bestCombo, seat.BestCombo);
                }
            }
            progress.RecordGame(sets, bestCombo);
            gameUI?.ShowResults(new RoundResult
            {
                LevelTitle = level.IsCampaign ? $"{CampaignNumber(LevelIndex)}. {level.Title}" : level.Title,
                Kind = level.Kind,
                Victory = localWon,
                Accent = winners.Count > 1 ? gold : color,
                Versus = true,
                Online = IsOnlineVersus,
                VersusTitle = title,
                VersusStandings = standings.ToString()
            });
            sounds?.PlayMusic(sounds.menuMusic);
            TransitionState(localWon ? BaseGameState.Victory : BaseGameState.GameOver);
        }

        #endregion
    }
}
