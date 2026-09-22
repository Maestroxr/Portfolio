using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using Portfolio.Monopoly.Server;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Online controller of the Monopoly module: matches with players on other devices, in a room of the game's
    /// SpacetimeDB database. Rooms, ready and start, the action log and its clock come from the base
    /// <see cref="OnlineGameController"/> and the base server; this class adds what is Monopoly. A room plays a game
    /// mode, against a clock and with computer players as the host likes. The server seats the table (Server/Lib.cs),
    /// every client starts the same match from it (MonopolyGameManager.Online.cs), and from then on the match changes
    /// only by the actions of the log: the commands of the interface are sent there instead of into the rules engine
    /// (<see cref="IMonopolyCommands"/>), the host's client sends the moves of the computer players, and every action
    /// is applied when it arrives, in the order and with the dice of the server.
    /// </summary>
    public class MonopolyOnlineController : OnlineGameController, IMonopolyCommands
    {
        public const string TurnOption = "turn";
        public const string ComputersOption = "bots";
        public const string LevelOption = "botlevel";

        private static readonly string[] TurnSeconds = { "0", "20", "30", "45", "60", "90" };
        private static readonly string[] TurnLabels = { "No clock", "20 seconds", "30 seconds", "45 seconds", "60 seconds", "90 seconds" };
        private static readonly string[] Computers = { "0", "1", "2" };
        private static readonly string[] ComputerLabels = { "None", "Up to 1", "Up to 2" };
        private static readonly string[] Levels = { "0", "1", "2" };
        private static readonly string[] LevelLabels = { "Easy", "Normal", "Hard" };

        /// <summary>Seconds a choice of the local player may take to come back in the log before it is offered again.</summary>
        private const float EchoPatience = 5f;
        /// <summary>The kind of a seat a member of the room plays (SeatHuman of Server/Lib.cs); the others are the computer's.</summary>
        private const byte HumanSeat = 0;

        private RoomOptionSpec[] optionSpecs;
        private bool awaitingEcho;
        private float echoDeadline;

        public MonopolyGameManager Monopoly => BaseManager as MonopolyGameManager;

        private GameServerClient Client => Server as GameServerClient;

        private MonopolyMatch Match => Monopoly != null ? Monopoly.Match : null;

        public override int MaxPlayersLimit => 4;

        public override int MinPlayersLimit => 2;

        /// <summary>A choice of the local player is on its way to the server and has not come back in the log yet.</summary>
        public bool AwaitingEcho => awaitingEcho && Time.unscaledTime < echoDeadline;

        /// <summary>
        /// The modes with rules of their own. The custom rules are the house rules of one device, which the other
        /// players do not have.
        /// </summary>
        public override IReadOnlyList<RoomLevelChoice> LevelChoices
        {
            get
            {
                var choices = new List<RoomLevelChoice>();
                MonopolyCampaign modes = Monopoly != null ? Monopoly.ModeList : null;
                for (int i = 0; modes != null && i < modes.Count; i++)
                {
                    MonopolyLevel mode = modes.Mode(i);
                    if (mode != null && !mode.UsesCustomRules)
                    {
                        choices.Add(new RoomLevelChoice(i, mode.Title));
                    }
                }
                return choices;
            }
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs => optionSpecs ??= new[]
        {
            new RoomOptionSpec(TurnOption, "Time to move", TurnSeconds, TurnLabels, 3),
            new RoomOptionSpec(ComputersOption, "Computer players", Computers, ComputerLabels, 0),
            new RoomOptionSpec(LevelOption, "Computer level", Levels, LevelLabels, 1)
        };

        public override string DescribeRoom(RoomInfo room)
        {
            int computers = room.Option(ComputersOption, 0);
            return computers > 0 ? $"{base.DescribeRoom(room)}, computer players" : base.DescribeRoom(room);
        }

        /// <summary>The clock of the room belongs to no seat: the match knows who the table waits for.</summary>
        public override string TurnCaption(RoomTurnInfo turn)
        {
            return Monopoly != null ? Monopoly.OnlineCaption() : base.TurnCaption(turn);
        }

        protected override IEnumerable<string> RoomQueries(RoomInfo room)
        {
            yield return $"SELECT * FROM monopoly_seat WHERE room_id = {room.Id}";
        }

        protected override void Start()
        {
            base.Start();
            if (Server != null)
            {
                Server.LoggedIn += AskForToken;
            }
        }

        protected override void OnDestroy()
        {
            if (Server != null)
            {
                Server.LoggedIn -= AskForToken;
            }
            base.OnDestroy();
        }

        /// <summary>
        /// The avatar of the profile is the token the player likes to move: the one of their seat in the games at this
        /// device. The server seats them with it unless somebody earlier at the table has it.
        /// </summary>
        private void AskForToken(PlayerProfile profile)
        {
            int token = SetupUI.PreferredToken();
            if (token >= 0 && profile.Avatar != token)
            {
                Server.SetAvatar((uint)token);
            }
        }

        // ------------------------------------------------------------------ the room

        /// <summary>The server seated the table: the manager gets it, then loads the mode of the room and starts the match.</summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            MonopolyGameManager manager = Monopoly;
            DbConnection connection = Client != null ? Client.Connection : null;
            if (manager == null || connection == null)
            {
                Report("The game is not set up for online play.");
                return;
            }
            var table = new MonopolyGameManager.OnlineTable { Setup = new MatchSetup(), LocalSeat = -1, Seed = room.Seed };
            foreach (MonopolySeat seat in connection.Db.MonopolySeat.RoomId.Filter(room.Id).OrderBy(row => row.Seat))
            {
                if (seat.Kind == HumanSeat && Server.Identity.HasValue && seat.Player == Server.Identity.Value)
                {
                    table.LocalSeat = table.Setup.seats.Count;
                }
                table.Setup.seats.Add(new SeatSetup
                {
                    kind = seat.Kind == HumanSeat ? SeatKind.Human : SeatKind.Computer,
                    name = seat.Name,
                    token = seat.Token % MonopolyStyle.TokenCount,
                    level = (BotLevel)Mathf.Clamp(seat.BotLevel, 0, (int)BotLevel.Hard)
                });
            }
            if (table.Setup.seats.Count < 2 || table.LocalSeat < 0)
            {
                Report("The table of the match did not arrive.");
                LeaveMatch();
                return;
            }
            // Another player opens every game of a room.
            table.FirstPlayer = (int)((room.Round > 0 ? room.Round - 1 : 0) % (uint)table.Setup.seats.Count);
            awaitingEcho = false;
            manager.PrepareOnlineMatch(table);
            base.OnRoomStarted(room);
            if (!manager.IsOnlineMatch)
            {
                LeaveMatch();
            }
        }

        /// <summary>Every action of the log goes to the match, the local player's own included: they count when they arrive.</summary>
        protected override void OnActionReceived(RoomActionInfo action)
        {
            MonopolyGameManager manager = Monopoly;
            if (!InCharge || manager == null)
            {
                return;
            }
            if (action.Seat == manager.LocalSeat && Server.Identity.HasValue && action.Sender == Server.Identity.Value)
            {
                awaitingEcho = false;
            }
            manager.OnlineActionArrived(action.Seat, action.Kind, action.Payload, action.Random, action.IsTimeout);
        }

        /// <summary>The manager shows its own results once the board played the match out; a room that ended early ends the match.</summary>
        protected override void OnRoomFinished(RoomInfo room)
        {
            if (InCharge)
            {
                Monopoly?.OnlineRoomFinished();
            }
        }

        /// <summary>What the local player is worth, as the turns pass: the score of the member.</summary>
        internal void ReportWorth(int worth)
        {
            if (IsPlaying)
            {
                Server.ReportScore(worth);
            }
        }

        /// <summary>The match is over for the local player; the room finishes when it is for everybody.</summary>
        internal void ReportFinished(int worth)
        {
            if (IsPlaying)
            {
                Server.FinishPlaying(worth);
            }
        }

        // ------------------------------------------------------------------ sending commands

        /// <summary>The host's client sends the move it worked out for a computer player.</summary>
        internal void PlayComputer(MatchCommand command)
        {
            if (IsPlaying && IsHost && command != null)
            {
                Submit(command);
            }
        }

        /// <summary>The local player answers the offer on the table.</summary>
        internal void AnswerTrade(int seat, bool accept)
        {
            if (Monopoly != null && Monopoly.OnlineAnswering(seat) && !AwaitingEcho)
            {
                Send(MatchCommand.Answer(seat, accept), true);
            }
        }

        private bool Deciding(int seat)
        {
            return IsPlaying && Monopoly != null && Monopoly.OnlineDeciding(seat) && !AwaitingEcho;
        }

        private bool Managing(int seat)
        {
            return IsPlaying && Monopoly != null && Monopoly.OnlineManaging(seat);
        }

        /// <summary>
        /// Sends a command of the local player. A choice the match waits for is not offered again until it came back
        /// (<paramref name="awaited"/>); managing property goes on meanwhile.
        /// </summary>
        private void Send(MatchCommand command, bool awaited)
        {
            if (awaited)
            {
                awaitingEcho = true;
                echoDeadline = Time.unscaledTime + EchoPatience;
            }
            Submit(command, awaited);
        }

        private void Submit(MatchCommand command, bool awaited = false)
        {
            Server.SubmitAction((byte)command.seat, (uint)command.kind, command.Payload(), error =>
            {
                if (error == null)
                {
                    return;
                }
                Report(error);
                if (awaited)
                {
                    awaitingEcho = false;
                    Monopoly?.OnlineCommandFailed();
                }
            });
        }

        private void Decide(CommandKind kind, int seat, int argument = 0)
        {
            if (Deciding(seat))
            {
                Send(MatchCommand.Of(kind, seat, argument), true);
            }
        }

        private void Manage(CommandKind kind, int seat, int space)
        {
            if (Managing(seat))
            {
                Send(MatchCommand.Of(kind, seat, space), false);
            }
        }

        public void Roll(int seat)
        {
            if (Deciding(seat))
            {
                Monopoly.MonopolyUI.Manage.Close();
                Decide(CommandKind.Roll, seat);
            }
        }

        public void Buy(int seat)
        {
            Decide(CommandKind.Buy, seat);
        }

        public void DeclineBuy(int seat)
        {
            Decide(CommandKind.DeclineBuy, seat);
        }

        public void PayJailFine(int seat)
        {
            Decide(CommandKind.PayJailFine, seat);
        }

        public void UseJailCard(int seat)
        {
            Decide(CommandKind.UseJailCard, seat);
        }

        public void ChooseBus(int seat, int option)
        {
            Decide(CommandKind.ChooseBus, seat, option);
        }

        public void ChooseDestination(int seat, int space)
        {
            if (Deciding(seat))
            {
                Monopoly.Board.ClearHighlights();
                Decide(CommandKind.ChooseDestination, seat, space);
            }
        }

        public void Bid(int seat, int amount)
        {
            Decide(CommandKind.Bid, seat, amount);
        }

        public void PassBid(int seat)
        {
            Decide(CommandKind.PassBid, seat);
        }

        public void EndTurn(int seat)
        {
            if (Deciding(seat))
            {
                Monopoly.MonopolyUI.Manage.Close();
                Decide(CommandKind.EndTurn, seat);
            }
        }

        public void DeclareBankruptcy(int seat)
        {
            if (Deciding(seat))
            {
                Monopoly.MonopolyUI.Manage.Close();
                Decide(CommandKind.DeclareBankruptcy, seat);
            }
        }

        public void Build(int seat, int space)
        {
            Manage(CommandKind.Build, seat, space);
        }

        public void Sell(int seat, int space)
        {
            Manage(CommandKind.Sell, seat, space);
        }

        public void Mortgage(int seat, int space)
        {
            Manage(CommandKind.Mortgage, seat, space);
        }

        public void Unmortgage(int seat, int space)
        {
            Manage(CommandKind.Unmortgage, seat, space);
        }

        public void OpenManager(int seat)
        {
            if (Managing(seat))
            {
                Monopoly.MonopolyUI.Trade.Close();
                Monopoly.MonopolyUI.Manage.Show(Match, seat, this);
            }
        }

        public void OpenTrade(int seat)
        {
            if (Managing(seat) && Match.ActiveCount > 1)
            {
                Monopoly.MonopolyUI.Manage.Close();
                Monopoly.MonopolyUI.Trade.Show(Match, seat, this, Monopoly.MonopolyUI.TokenSprite, true);
            }
        }

        /// <summary>The local player puts an offer on the table: the other side answers on their device, the host for a computer player.</summary>
        public void ProposeTrade(TradeOffer offer)
        {
            if (offer == null || !Managing(offer.from) || !Match.CanTrade(offer, out _))
            {
                Monopoly?.MonopolyUI.Sound?.Play(Sfx.Error, 0.7f);
                return;
            }
            Send(MatchCommand.Propose(offer), false);
        }
    }
}
