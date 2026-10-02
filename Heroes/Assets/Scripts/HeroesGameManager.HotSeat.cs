using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The hot seat: two to four people take their turns at one device, on a skirmish map, with the computer playing any
    /// realm nobody sits at (<see cref="HeroesHotSeatRules"/>, the shared setup of <see cref="BaseGameManager.OpenLocalPlay"/>).
    ///
    /// The screen belongs to one person at a time, the holder. Whenever the game goes on with another person (their
    /// turn, a question the rules put to them, a battle they are dragged into on somebody else's turn) the hand-over
    /// screen covers the map first (<see cref="BaseGameManager.HandOver"/>), and only once that person is ready does the
    /// map change to what they see: their fog, treasury, heroes and towns, chronicle, and the hero and the place they
    /// were looking at. As soon as a person ends their turn the device goes to the next person in the order of turns,
    /// who watches the computer's moves in between as their own realm sees the map, never as the last person saw it. A
    /// battle between two people is fought at one screen, so it needs no hand-over; whose troops act is said by name.
    ///
    /// People are called by their names, never "you". A hot seat game is saved in a slot of its own next to the solo
    /// save of its map and goes on from Continue with its people and names; it earns no stars, records or campaign
    /// progress, and ends with the standings of the realms.
    /// </summary>
    public partial class HeroesGameManager
    {
        /// <summary>The hero and the place a person was looking at when the device left them.</summary>
        private struct SeatView
        {
            public int hero;
            public Vector3 goal;
            public float distance;
        }

        private HeroesHotSeatRules hotSeatRules;
        /// <summary>The person who has the device in a hot seat (-1: nobody yet).</summary>
        private int holder = -1;
        /// <summary>The hand-over screen is up and the game waits for the next person.</summary>
        private bool passing;
        /// <summary>The frame the last hand-over ended: its click or key does nothing else.</summary>
        private int passedFrame = -1;
        /// <summary>A saved hot seat game being taken up (through <see cref="BaseGameManager.BeginLocalMatch"/>).</summary>
        private GameState resuming;
        /// <summary>Where the battles of the game about to start are fought, when a hot seat chose it.</summary>
        private int? pendingBattleStyle;
        private readonly Dictionary<int, SeatView> seatViews = new Dictionary<int, SeatView>();

        /// <summary>A realm that has explored nothing: the fog of the map between two people.</summary>
        private static readonly PlayerState Nobody = new PlayerState { index = -1, human = false };

        public override LocalPlayRules LocalPlay
        {
            get
            {
                if (hotSeatRules == null && campaign != null)
                {
                    hotSeatRules = new HeroesHotSeatRules(campaign);
                }
                hotSeatRules?.Refresh();
                return hotSeatRules;
            }
        }

        /// <summary>Whether the game is a hot seat: several people take turns at this device.</summary>
        public bool IsHotSeat => InLocalMatch && !InSession;

        /// <summary>The person who has the device in a hot seat, or -1.</summary>
        public int HotSeatHolder => IsHotSeat ? holder : -1;

        /// <summary>Whether the hand-over screen waits for the next person, or has only just let them in (this frame).</summary>
        public bool IsPassing => passing || passedFrame == Time.frameCount;

        // ------------------------------------------------------------------ starting

        /// <summary>
        /// The setup's Start: the map chosen, at the size chosen, its seats played as the setup says, its battles fought
        /// where it says; riches and wandering armies as the settings have them.
        /// </summary>
        protected override void OnLocalMatchBegun(LocalMatch match)
        {
            ResetHotSeat();
            if (resuming != null)
            {
                GameState state = resuming;
                resuming = null;
                Continue(state);
                return;
            }
            HeroesHotSeatRules rules = match.Rules as HeroesHotSeatRules ?? hotSeatRules;
            int level = rules != null ? rules.LevelOf(match) : -1;
            HeroesLevel scenario = campaign != null && level >= 0 ? campaign.Scenario(level) : null;
            if (scenario == null)
            {
                UI?.UpdateError("The map of the hot seat is missing.");
                EndLocalMatch();
                return;
            }
            HeroesSettings options = Options;
            MapSpec spec = HotSeat.Spec(scenario.Map, HeroesHotSeatRules.Players(match),
                Mathf.Max(0, match.Option(HeroesHotSeatRules.SizeOption)),
                options != null ? options.treasure : 2, options != null ? options.monsters : 2);
            int style = match.Option(HeroesHotSeatRules.BattlesOption);
            pendingBattleStyle = style >= 0 ? style : (int?)null;
            BeginScenario(spec, level);
        }

        protected override void OnLocalMatchEnded(LocalMatch match)
        {
            ResetHotSeat();
        }

        private void ResetHotSeat()
        {
            holder = -1;
            passing = false;
            seatViews.Clear();
        }

        /// <summary>The results' Play Again: the same people, the same map, the same choices, from the start.</summary>
        public void PlayHotSeatAgain()
        {
            LocalMatch match = LocalMatch;
            if (match == null)
            {
                ReturnToTitle();
                return;
            }
            BeginLocalMatch(match);
        }

        // ------------------------------------------------------------------ handing the device over

        /// <summary>
        /// Called by the director before it goes on: covers the screen for the person who has to hold the device next
        /// (<see cref="HotSeat.HandOverTo"/>), unless a battle is on the screen (its end is told to those who fought it).
        /// True while the game waits for them.
        /// </summary>
        private bool HandOverIfDue()
        {
            if (!IsHotSeat || Game == null)
            {
                return false;
            }
            if (!passing && !battleShown)
            {
                int to = HotSeat.HandOverTo(Game.State, holder, Game.WaitingPlayer);
                if (to >= 0)
                {
                    PassTo(to);
                }
            }
            return passing;
        }

        /// <summary>
        /// Covers the map for <paramref name="who"/> and, once they are ready, shows it as they see it. The screens the
        /// last person had open are closed first; nothing of theirs shows behind the cover or after it.
        /// </summary>
        private void PassTo(int who)
        {
            PlayerState player = Game.State.Player(who);
            LocalMatch match = LocalMatch;
            if (player == null || match == null || who >= match.AllSeats.Count)
            {
                return;
            }
            RememberView();
            WaitingForHuman = false;
            ui.CloseScreens();
            // The cover steps aside for the pause menu: behind it the map shows nobody's land and no panels.
            ui.ShowAdventureHud(false);
            Map.Fog.Show(Nobody);
            LocalSeat seat = match[who];
            seat.Name = player.name;
            seat.Color = HeroesArt.PlayerColor((int)player.color);
            passing = true;
            HeroesGame game = Game;
            HandOver(seat, () =>
            {
                if (Game != game)
                {
                    return;
                }
                passing = false;
                passedFrame = Time.frameCount;
                holder = who;
                ShowViewOf(who);
            }, PassDetail(who));
        }

        /// <summary>The line under the name on the hand-over screen: why the device comes to them.</summary>
        private string PassDetail(int who)
        {
            GameState state = Game.State;
            if (Game.InBattle)
            {
                return Words.T("To Battle!");
            }
            if (state.pending.Count > 0 && state.pending[0].player == who)
            {
                HeroState hero = state.Hero(state.pending[0].hero);
                if (state.pending[0].kind == ChoiceKind.LevelUp && hero != null)
                {
                    return Words.F("{0} reaches level {1}", Words.Name(hero.Name), hero.level);
                }
                return Words.T("A Choice");
            }
            return Words.F("Day {0}, Week {1}", state.DayOfWeek, state.Week);
        }

        /// <summary>Keeps the hero and the place the person holding the device was looking at, for their next turn.</summary>
        private void RememberView()
        {
            if (holder < 0 || Map == null || Battle.Running || cameraRig == null)
            {
                return;
            }
            seatViews[holder] = new SeatView
            {
                hero = Selected != null ? Selected.id : -1,
                goal = cameraRig.Goal,
                distance = cameraRig.GoalDistance
            };
        }

        /// <summary>The map as <paramref name="who"/> sees it: their fog, panels and chronicle, their hero in hand.</summary>
        private void ShowViewOf(int who)
        {
            PlayerState player = Game.State.Player(who);
            Viewer = who;
            Map.Fog.Show(player);
            ui.SwitchChronicle(who);
            ui.ShowAdventureHud(!Battle.Running);
            bool known = seatViews.TryGetValue(who, out SeatView view);
            HeroState hero = known ? Game.State.Hero(view.hero) : null;
            if (hero == null || !hero.alive || hero.owner != who)
            {
                hero = null;
                foreach (int id in player.heroes)
                {
                    HeroState own = Game.State.Hero(id);
                    if (own != null && own.alive)
                    {
                        hero = own;
                        break;
                    }
                }
            }
            selected = hero;
            Path?.Clear();
            if (cameraRig != null && !Battle.Running)
            {
                if (known)
                {
                    cameraRig.Snap(view.goal, view.distance);
                }
                else if (hero != null)
                {
                    cameraRig.Snap(Map.Point(hero.cell));
                }
                else if (player.towns.Count > 0 && Game.State.Town(player.towns[0]) is TownState town)
                {
                    cameraRig.Snap(Map.Point(town.cell));
                }
            }
            ui.Refresh();
        }

        // ------------------------------------------------------------------ names

        /// <summary>
        /// The name a person of a hot seat is called by, in the colour of their realm (rich text), or null when the game is
        /// no hot seat or <paramref name="player"/> is no person: the interface then says "you" as it always does.
        /// </summary>
        public string HotSeatName(int player)
        {
            PlayerState seat = IsHotSeat && Game != null ? Game.State.Player(player) : null;
            if (seat == null || !seat.human)
            {
                return null;
            }
            // A little lighter than the banners, to read on the dark bars.
            Color color = Color.Lerp(HeroesArt.PlayerColor((int)seat.color), Color.white, 0.3f);
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + seat.name + "</color>";
        }

        // ------------------------------------------------------------------ saving

        /// <summary>
        /// Takes up a saved hot seat game: its people, names and seats are in the game itself; the setup it began with
        /// (the hand-over screen, the map's choices for Play Again) was saved with it.
        /// </summary>
        private void ResumeHotSeat(GameState state, string setup)
        {
            if (LocalPlay is not HeroesHotSeatRules rules)
            {
                EndLocalMatch();
                Continue(state);
                return;
            }
            var match = new LocalMatch(GameType.Heroes, rules);
            match.Load(setup);
            match.SetOption(HeroesHotSeatRules.MapOption, Mathf.Max(0, rules.OptionOf(LevelIndex)));
            rules.OptionsChanged(match);
            foreach (PlayerState player in state.players)
            {
                if (player.index < match.AllSeats.Count)
                {
                    LocalSeat seat = match[player.index];
                    seat.Name = player.name;
                    seat.Kind = player.human ? SeatKind.Human : SeatKind.Computer;
                    seat.Level = player.aiLevel;
                    seat.Color = HeroesArt.PlayerColor((int)player.color);
                }
            }
            resuming = state;
            BeginLocalMatch(match);
        }

        // ------------------------------------------------------------------ the end

        /// <summary>
        /// A hot seat game is over: no stars, records or campaign progress; the standings of the realms, the winner first,
        /// with a way to play again with the same setup.
        /// </summary>
        private IEnumerator FinishHotSeat(GameState state, int days)
        {
            ClearSave();
            PlayerState winner = state.Player(state.winner);
            bool personWon = winner != null && winner.human;
            if (personWon)
            {
                sound?.PlayVictoryMusic();
            }
            else
            {
                sound?.PlayDefeatMusic();
            }
            yield return new WaitForSeconds(0.6f);
            ui.ShowHotSeatEnd(state, days);
            TransitionState(new Gamebox.GameState(personWon ? BaseGameState.Victory : BaseGameState.GameOver));
        }
    }
}
