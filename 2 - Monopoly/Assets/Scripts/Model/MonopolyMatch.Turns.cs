using System;
using System.Linq;

namespace Portfolio.Monopoly
{
    public sealed partial class MonopolyMatch
    {
        // ------------------------------------------------------------------ turn flow

        private void BeginTurn()
        {
            PlayerState player = Current;
            player.doubles = 0;
            extraRoll = false;
            mrMonopolyPending = false;
            pendingMove = 0;
            pendingPurchase = -1;
            Emit(MatchEventKind.TurnStarted, current, value: round);
            phase = player.inJail ? MatchPhase.JailChoice : MatchPhase.Roll;
        }

        /// <summary>Ends the current player's turn (only once they finished moving).</summary>
        public bool EndTurn()
        {
            if (phase != MatchPhase.EndTurn)
            {
                return false;
            }
            NextTurn();
            return true;
        }

        private void NextTurn()
        {
            if (phase == MatchPhase.GameOver)
            {
                return;
            }
            int count = players.Count;
            int next = -1;
            for (int i = 1; i <= count; i++)
            {
                int seat = (current + i) % count;
                if (!players[seat].bankrupt)
                {
                    next = seat;
                    break;
                }
            }
            if (next < 0)
            {
                FinishGame();
                return;
            }
            if (next <= current)
            {
                round++;
                Emit(MatchEventKind.RoundStarted, value: round);
                if (rules.roundLimit > 0 && round > rules.roundLimit)
                {
                    round = rules.roundLimit;
                    Emit(MatchEventKind.Message, text: "Time is up! The richest player wins.");
                    FinishGame();
                    return;
                }
            }
            current = next;
            turn++;
            BeginTurn();
        }

        /// <summary>
        /// Moves the match on after an action: pending debts, auctions and purchases first, then the rest of the move
        /// (the fine after the last jail roll, Mr. Monopoly), then another roll after doubles, or the end of the turn.
        /// </summary>
        private void Continue()
        {
            for (int guard = 0; guard < 64; guard++)
            {
                if (phase == MatchPhase.GameOver || CheckGameEnd())
                {
                    return;
                }
                if (debts.Count > 0)
                {
                    if (SettleFirstDebt())
                    {
                        continue;
                    }
                    phase = MatchPhase.RaiseFunds;
                    return;
                }
                if (auction.Running)
                {
                    if (AuctionDone)
                    {
                        CloseAuction();
                        continue;
                    }
                    phase = MatchPhase.Auction;
                    return;
                }
                if (auctionQueue.Count > 0)
                {
                    int space = auctionQueue[0];
                    auctionQueue.RemoveAt(0);
                    if (deeds[space].Owned)
                    {
                        continue;
                    }
                    StartAuction(space, current);
                    continue;
                }
                if (Current.bankrupt)
                {
                    NextTurn();
                    return;
                }
                if (pendingPurchase >= 0)
                {
                    phase = MatchPhase.BuyChoice;
                    return;
                }
                if (pendingMove > 0)
                {
                    int steps = pendingMove;
                    pendingMove = 0;
                    MoveForward(Current, steps);
                    continue;
                }
                if (mrMonopolyPending)
                {
                    mrMonopolyPending = false;
                    MrMonopolyMove();
                    continue;
                }
                phase = extraRoll && !Current.inJail ? MatchPhase.Roll : MatchPhase.EndTurn;
                return;
            }
            throw new InvalidOperationException("The match did not settle; this is a bug in the rules engine.");
        }

        // ------------------------------------------------------------------ rolling

        /// <summary>Rolls the dice for the current player (a normal roll, or for doubles in jail).</summary>
        public bool Roll()
        {
            if (phase != MatchPhase.Roll && phase != MatchPhase.JailChoice)
            {
                return false;
            }
            return Roll(NextRoll(Current));
        }

        /// <summary>Plays a roll with the given dice (tests, the tour, and <see cref="Roll()"/>).</summary>
        public bool Roll(DiceRoll roll)
        {
            if (phase != MatchPhase.Roll && phase != MatchPhase.JailChoice)
            {
                return false;
            }
            PlayerState player = Current;
            if (!UsesSpeedDie(player))
            {
                roll.speed = SpeedFace.None;
            }
            lastRoll = roll;
            var rolled = new MatchEvent { kind = MatchEventKind.DiceRolled, player = current, roll = roll, other = -1, space = -1, from = -1 };
            Emit(rolled);

            if (phase == MatchPhase.JailChoice)
            {
                RollInJail(roll);
                return true;
            }

            if (roll.IsDoubles)
            {
                player.doubles++;
                if (player.doubles >= 3)
                {
                    Emit(MatchEventKind.Message, current, text: "Three doubles in a row! Go to jail.");
                    SendToJail(player);
                    Continue();
                    return true;
                }
                extraRoll = true;
                Emit(MatchEventKind.Doubles, current, value: player.doubles);
            }
            else
            {
                extraRoll = false;
            }

            if (roll.IsTriples)
            {
                extraRoll = false;
                Emit(MatchEventKind.Triples, current);
                phase = MatchPhase.MoveAnywhere;
                return true;
            }
            if (roll.speed == SpeedFace.Bus)
            {
                Emit(MatchEventKind.SpeedDie, current, value: (int)SpeedFace.Bus);
                phase = MatchPhase.BusChoice;
                return true;
            }
            if (roll.speed == SpeedFace.MrMonopoly)
            {
                Emit(MatchEventKind.SpeedDie, current, value: (int)SpeedFace.MrMonopoly);
                mrMonopolyPending = true;
            }
            MoveForward(player, roll.Total + roll.SpeedValue);
            Continue();
            return true;
        }

        private void RollInJail(DiceRoll roll)
        {
            PlayerState player = Current;
            extraRoll = false;
            if (roll.IsDoubles)
            {
                player.inJail = false;
                player.jailTurns = 0;
                Emit(MatchEventKind.LeftJail, current, value: 0);
                MoveForward(player, roll.Total);
                Continue();
                return;
            }
            player.jailTurns++;
            if (player.jailTurns >= rules.maxJailTurns)
            {
                player.inJail = false;
                player.jailTurns = 0;
                Emit(MatchEventKind.LeftJail, current, value: 3);
                pendingMove = roll.Total;
                Charge(current, FineCollector, rules.jailFine, "Jail fine");
                Continue();
                return;
            }
            Emit(MatchEventKind.StayedInJail, current, value: player.jailTurns);
            phase = MatchPhase.EndTurn;
        }

        /// <summary>Where fines and taxes go: the Free Parking pot with the jackpot house rule, otherwise the bank.</summary>
        private int FineCollector => rules.freeParkingJackpot ? Party.Pot : Party.Bank;

        public bool PayJailFine()
        {
            if (phase != MatchPhase.JailChoice || Current.cash < rules.jailFine)
            {
                return false;
            }
            PlayerState player = Current;
            Transfer(current, FineCollector, rules.jailFine, "Jail fine");
            player.inJail = false;
            player.jailTurns = 0;
            Emit(MatchEventKind.LeftJail, current, value: 1);
            phase = MatchPhase.Roll;
            return true;
        }

        public bool UseJailCard()
        {
            PlayerState player = Current;
            if (phase != MatchPhase.JailChoice || player.jailCards.Count == 0)
            {
                return false;
            }
            CardDeckKind deck = player.jailCards[0];
            player.jailCards.RemoveAt(0);
            ReturnJailCard(deck);
            player.inJail = false;
            player.jailTurns = 0;
            Emit(MatchEventKind.LeftJail, current, value: 2);
            phase = MatchPhase.Roll;
            return true;
        }

        /// <summary>Speed die bus: move by the first die (0), the second die (1) or both (2).</summary>
        public bool ChooseBus(int option)
        {
            if (phase != MatchPhase.BusChoice || option < 0 || option > 2)
            {
                return false;
            }
            int steps = option == 0 ? lastRoll.a : option == 1 ? lastRoll.b : lastRoll.Total;
            MoveForward(Current, steps);
            Continue();
            return true;
        }

        /// <summary>The distances the bus offers: each die and their sum.</summary>
        public int[] BusOptions => new[] { lastRoll.a, lastRoll.b, lastRoll.Total };

        /// <summary>Speed die triples: move to any space (forward, collecting the salary when passing GO).</summary>
        public bool ChooseDestination(int space)
        {
            if (phase != MatchPhase.MoveAnywhere || space < 0 || space >= board.Count)
            {
                return false;
            }
            PlayerState player = Current;
            int steps = (space - player.position + board.Count) % board.Count;
            if (steps == 0)
            {
                Land(player, space);
            }
            else
            {
                MoveForward(player, steps);
            }
            Continue();
            return true;
        }

        private void MrMonopolyMove()
        {
            PlayerState player = Current;
            if (player.inJail || player.bankrupt)
            {
                return;
            }
            int count = board.Count;
            for (int i = 1; i < count; i++)
            {
                int space = (player.position + i) % count;
                if (board[space].IsProperty && !deeds[space].Owned)
                {
                    Emit(MatchEventKind.Message, current, text: "Mr. Monopoly: on to the next property for sale!");
                    MoveForward(player, i);
                    return;
                }
            }
            for (int i = 1; i < count; i++)
            {
                int space = (player.position + i) % count;
                if (board[space].IsProperty && deeds[space].Owned && deeds[space].owner != current && !deeds[space].mortgaged)
                {
                    Emit(MatchEventKind.Message, current, text: "Mr. Monopoly: everything is sold, on to the next rent!");
                    MoveForward(player, i);
                    return;
                }
            }
            Emit(MatchEventKind.Message, current, text: "Mr. Monopoly has nowhere to go.");
        }

        // ------------------------------------------------------------------ moving and landing

        private void MoveForward(PlayerState player, int steps, int rentMultiplier = 1, int utilityMultiplier = 0)
        {
            int count = board.Count;
            int from = player.position;
            int to = (from + steps) % count;
            player.position = to;
            Emit(MatchEventKind.Moved, player.index, space: to, from: from, value: steps);
            if (from + steps >= count)
            {
                CollectSalary(player, to == 0);
            }
            Land(player, to, rentMultiplier, utilityMultiplier);
        }

        private void MoveBackward(PlayerState player, int steps)
        {
            int count = board.Count;
            int from = player.position;
            int to = ((from - steps) % count + count) % count;
            player.position = to;
            Emit(MatchEventKind.Moved, player.index, space: to, from: from, value: -steps);
            Land(player, to);
        }

        private void CollectSalary(PlayerState player, bool exactly)
        {
            player.passedGo = true;
            int amount = rules.salary * (exactly && rules.doubleSalaryOnGo ? 2 : 1);
            Emit(MatchEventKind.PassedGo, player.index, value: amount, text: exactly && rules.doubleSalaryOnGo ? "Double salary!" : null);
            Transfer(Party.Bank, player.index, amount, exactly && rules.doubleSalaryOnGo ? "Landed on GO: double salary" : "Salary");
        }

        private void Land(PlayerState player, int space, int rentMultiplier = 1, int utilityMultiplier = 0)
        {
            Emit(MatchEventKind.Landed, player.index, space: space);
            SpaceData data = board[space];
            switch (data.kind)
            {
                case SpaceKind.Street:
                case SpaceKind.Railroad:
                case SpaceKind.Utility:
                    LandOnProperty(player, space, rentMultiplier, utilityMultiplier);
                    break;
                case SpaceKind.Chance:
                    DrawCard(player, CardDeckKind.Chance);
                    break;
                case SpaceKind.CommunityChest:
                    DrawCard(player, CardDeckKind.CommunityChest);
                    break;
                case SpaceKind.Tax:
                    Emit(MatchEventKind.Tax, player.index, space: space, value: data.tax);
                    Charge(player.index, FineCollector, data.tax, data.name);
                    break;
                case SpaceKind.GoToJail:
                    SendToJail(player);
                    break;
                case SpaceKind.FreeParking:
                    if (rules.freeParkingJackpot && pot > 0)
                    {
                        int won = pot;
                        Emit(MatchEventKind.Jackpot, player.index, space: space, value: won);
                        Transfer(Party.Pot, player.index, won, "Free Parking jackpot");
                        if (rules.jackpotSeed > 0)
                        {
                            pot += rules.jackpotSeed;
                            Emit(MatchEventKind.PotChanged, value: pot);
                        }
                    }
                    break;
            }
        }

        private void LandOnProperty(PlayerState player, int space, int rentMultiplier, int utilityMultiplier)
        {
            DeedState deed = deeds[space];
            if (!deed.Owned)
            {
                pendingPurchase = space;
                Emit(MatchEventKind.BuyOffered, player.index, space: space, value: board[space].price);
                return;
            }
            if (deed.owner == player.index)
            {
                return;
            }
            if (deed.mortgaged)
            {
                Emit(MatchEventKind.NoRent, player.index, deed.owner, space, value: 0);
                return;
            }
            PlayerState owner = players[deed.owner];
            if (rules.noRentInJail && owner.inJail)
            {
                Emit(MatchEventKind.NoRent, player.index, deed.owner, space, value: 1);
                return;
            }
            int diceTotal = lastRoll.Total;
            if (utilityMultiplier > 0 && board[space].kind == SpaceKind.Utility)
            {
                DiceRoll roll = NextPlainRoll();
                Emit(new MatchEvent { kind = MatchEventKind.DiceRolled, player = player.index, roll = roll, other = -1, space = space, from = -1, text = "utility" });
                diceTotal = roll.Total;
                rentMultiplier = utilityMultiplier;
            }
            int rent = Rent(space, diceTotal, board[space].kind == SpaceKind.Street ? 1 : rentMultiplier);
            if (rent <= 0)
            {
                return;
            }
            Emit(MatchEventKind.Rent, player.index, deed.owner, space, value: rent);
            player.rentPaid += rent;
            owner.rentCollected += rent;
            Charge(player.index, deed.owner, rent, $"Rent for {board[space].name}");
        }

        private void SendToJail(PlayerState player)
        {
            int jail = board.JailIndex;
            int from = player.position;
            player.position = jail;
            player.inJail = true;
            player.jailTurns = 0;
            player.doubles = 0;
            player.timesInJail++;
            Emit(MatchEventKind.WentToJail, player.index, space: jail, from: from);
            if (player.index == current)
            {
                extraRoll = false;
                mrMonopolyPending = false;
                pendingMove = 0;
            }
        }

        // ------------------------------------------------------------------ cards

        private void DrawCard(PlayerState player, CardDeckKind kind)
        {
            CardDeck deck = kind == CardDeckKind.Chance ? chanceDeck : chestDeck;
            if (deck.order.Count == 0)
            {
                return;
            }
            int index = deck.order[0];
            CardData card = board.Deck(kind)[index];
            deck.Draw(card.action == CardAction.GetOutOfJail);
            Emit(new MatchEvent { kind = MatchEventKind.CardDrawn, player = player.index, deck = kind, card = index, text = card.text, other = -1, space = player.position, from = -1 });
            ApplyCard(player, card);
        }

        private void ApplyCard(PlayerState player, CardData card)
        {
            int count = board.Count;
            switch (card.action)
            {
                case CardAction.AdvanceTo:
                {
                    int target = ((card.value % count) + count) % count;
                    int steps = (target - player.position + count) % count;
                    if (steps == 0)
                    {
                        steps = count;
                    }
                    MoveForward(player, steps);
                    break;
                }
                case CardAction.AdvanceToNearest:
                {
                    for (int i = 1; i <= count; i++)
                    {
                        int space = (player.position + i) % count;
                        if (board[space].IsProperty && board[space].group == card.group)
                        {
                            bool utility = board[space].kind == SpaceKind.Utility;
                            MoveForward(player, i, utility ? 1 : Math.Max(1, card.value), utility ? Math.Max(1, card.value) : 0);
                            break;
                        }
                    }
                    break;
                }
                case CardAction.MoveBy:
                    if (card.value < 0)
                    {
                        MoveBackward(player, -card.value);
                    }
                    else if (card.value > 0)
                    {
                        MoveForward(player, card.value);
                    }
                    break;
                case CardAction.Collect:
                    Transfer(Party.Bank, player.index, card.value, "Card");
                    break;
                case CardAction.Pay:
                    Charge(player.index, FineCollector, card.value, "Card");
                    break;
                case CardAction.PayEachPlayer:
                    foreach (PlayerState other in players.Where(p => !p.bankrupt && p.index != player.index).ToList())
                    {
                        Charge(player.index, other.index, card.value, "Card");
                    }
                    break;
                case CardAction.CollectFromEachPlayer:
                    foreach (PlayerState other in players.Where(p => !p.bankrupt && p.index != player.index).ToList())
                    {
                        Charge(other.index, player.index, card.value, "Card");
                    }
                    break;
                case CardAction.Repairs:
                {
                    int amount = Houses(player.index) * card.value + Hotels(player.index) * card.value2;
                    if (amount > 0)
                    {
                        Charge(player.index, FineCollector, amount, "Repairs");
                    }
                    else
                    {
                        Emit(MatchEventKind.Message, player.index, text: "No buildings, nothing to repair.");
                    }
                    break;
                }
                case CardAction.GoToJail:
                    SendToJail(player);
                    break;
                case CardAction.GetOutOfJail:
                    player.jailCards.Add(card.deck);
                    Emit(new MatchEvent { kind = MatchEventKind.JailCardGained, player = player.index, deck = card.deck, other = -1, space = -1, from = -1 });
                    break;
                case CardAction.CollectFromRichest:
                {
                    PlayerState richest = players.Where(p => !p.bankrupt && p.index != player.index)
                        .OrderByDescending(p => NetWorth(p.index)).FirstOrDefault();
                    if (richest != null)
                    {
                        Emit(MatchEventKind.Message, richest.index, player.index, text: $"The richest player pays up: {richest.name}.");
                        Charge(richest.index, player.index, card.value, "Robin Hood");
                    }
                    break;
                }
                case CardAction.EveryonePaysPot:
                    foreach (PlayerState other in players.Where(p => !p.bankrupt).ToList())
                    {
                        Charge(other.index, FineCollector, card.value, "Charity gala");
                    }
                    break;
                case CardAction.FreeHouse:
                {
                    int space = board.Properties.Where(s => deeds[s].owner == player.index && CanBuildRules(player.index, s, out _))
                        .OrderBy(s => board[s].houseCost).ThenBy(s => deeds[s].houses).DefaultIfEmpty(-1).First();
                    if (space >= 0)
                    {
                        PlaceBuilding(player.index, space, true);
                    }
                    else
                    {
                        Emit(MatchEventKind.Message, player.index, text: "No complete set to build on. Better luck next time!");
                    }
                    break;
                }
            }
        }
    }
}
