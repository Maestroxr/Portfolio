using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Online controller of the Monopoly module: matches with players on other devices, in a room of the game's
    /// SpacetimeDB database. Rooms, ready and start, the action log and its clock, the seats of the table and taking the
    /// log to the match come from the shared <see cref="LockstepOnlineController{TCommand}"/> and the base server; this
    /// class adds what is Monopoly. A room plays a game mode, against a clock and with computer players as the host likes.
    /// The server seats the table (Server/Lib.cs), every client starts the same match from it
    /// (MonopolyGameManager.Online.cs), and from then on the match changes only by the actions of the log: the commands of
    /// the interface are sent there instead of into the rules engine (<see cref="IMonopolyCommands"/>), the host's client
    /// sends the moves of the computer players, and every action is applied when it arrives, in the order and with the
    /// dice of the server.
    /// </summary>
    public class MonopolyOnlineController : LockstepOnlineController<MatchCommand>, IMonopolyCommands
    {
        private static readonly int[] TurnSeconds = { 0, 20, 30, 45, 60, 90 };

        private RoomOptionSpec[] optionSpecs;

        public MonopolyGameManager Monopoly => BaseManager as MonopolyGameManager;

        private MonopolyMatch Match => Monopoly != null ? Monopoly.Match : null;

        public override int MaxPlayersLimit => 4;

        public override int MinPlayersLimit => 2;

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
            RoomOptionSpec.Clock("Time to move", TurnSeconds, 3),
            RoomOptionSpec.Computers("Computer players", 2),
            RoomOptionSpec.ComputerLevel("Computer level")
        };

        public override string DescribeRoom(RoomInfo room)
        {
            int computers = room.Option(RoomOptions.ComputersOption, 0);
            return computers > 0 ? $"{base.DescribeRoom(room)}, computer players" : base.DescribeRoom(room);
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
        protected override bool PrepareTable(LockstepSetup setup)
        {
            MonopolyGameManager manager = Monopoly;
            if (manager == null)
            {
                return false;
            }
            var table = new MonopolyGameManager.OnlineTable { Setup = new MatchSetup(), LocalSeat = setup.TableSeat, Seed = setup.Seed };
            foreach (RoomSeatInfo seat in setup.Seats)
            {
                table.Setup.seats.Add(new SeatSetup
                {
                    kind = seat.IsHuman ? SeatKind.Human : SeatKind.Computer,
                    name = seat.Name,
                    token = seat.Look % MonopolyStyle.TokenCount,
                    level = (BotLevel)Mathf.Clamp(seat.BotLevel, 0, (int)BotLevel.Hard)
                });
            }
            // Another player opens every game of a room.
            table.FirstPlayer = (int)((setup.Round > 0 ? setup.Round - 1 : 0) % (uint)table.Setup.seats.Count);
            manager.PrepareOnlineMatch(table);
            return true;
        }

        // ------------------------------------------------------------------ the log

        /// <summary>
        /// The command of an action. The rules' own <see cref="CommandKind.SeatToComputer"/> is not one a player can send:
        /// the server says it with an action of its own.
        /// </summary>
        protected override MatchCommand Parse(byte seat, uint kind, string payload)
        {
            return kind < (uint)CommandKind.SeatToComputer ? MatchCommand.Parse(seat, kind, payload) : null;
        }

        protected override void Encode(MatchCommand command, out byte seat, out uint kind, out string payload)
        {
            seat = (byte)command.seat;
            kind = (uint)command.kind;
            payload = command.Payload();
        }

        /// <summary>The local player answers the offer on the table.</summary>
        internal void AnswerTrade(int seat, bool accept)
        {
            if (Monopoly != null && Monopoly.OnlineAnswering(seat) && !AwaitingEcho)
            {
                Submit(MatchCommand.Answer(seat, accept), true);
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

        private void Decide(CommandKind kind, int seat, int argument = 0)
        {
            if (Deciding(seat))
            {
                Submit(MatchCommand.Of(kind, seat, argument), true);
            }
        }

        /// <summary>Managing property goes on while a choice is on its way: it does not answer what the match waits for.</summary>
        private void Manage(CommandKind kind, int seat, int space)
        {
            if (Managing(seat))
            {
                Submit(MatchCommand.Of(kind, seat, space), false);
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
            Submit(MatchCommand.Propose(offer), false);
        }
    }
}
