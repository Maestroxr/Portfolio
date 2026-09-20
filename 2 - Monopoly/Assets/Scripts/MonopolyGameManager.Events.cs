using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    public partial class MonopolyGameManager
    {
        // ------------------------------------------------------------------ playing events

        private int ColorOf(int seat)
        {
            return Match != null && seat >= 0 && seat < Match.players.Count ? Match.players[seat].color : seat;
        }

        private Color PlayerColor(int seat)
        {
            return MonopolyStyle.PlayerColor(ColorOf(seat));
        }

        private string Named(int seat)
        {
            return seat >= 0 && seat < Match.players.Count ? MonopolyStyle.Named(Match.players[seat]) : seat == Party.Pot ? "the Free Parking pot" : "the bank";
        }

        /// <summary>A seat (or the bank or the pot) as the object of a sentence.</summary>
        private string NamedObject(int seat)
        {
            return seat >= 0 && seat < Match.players.Count ? MonopolyStyle.NamedObject(Match.players[seat]) : Named(seat);
        }

        /// <summary>A verb agreeing with the seat: "Ada builds", "You build".</summary>
        private string Verb(int seat, string thirdPerson)
        {
            return seat >= 0 && seat < Match.players.Count ? MonopolyStyle.Verb(Match.players[seat], thirdPerson) : thirdPerson;
        }

        private int HumanCount => Match.players.Count(p => !p.bot && !p.bankrupt);

        private IEnumerator Play(MatchEvent e)
        {
            switch (e.kind)
            {
                case MatchEventKind.TurnStarted:
                {
                    PlayerState player = Match.players[e.player];
                    for (int i = 0; i < Match.players.Count && i < tokens.Count; i++)
                    {
                        tokens[i].SetTurn(i == e.player);
                    }
                    ui.SetTurn(e.player);
                    if (!ui.Deed.IsOffer)
                    {
                        ui.Deed.Close();
                    }
                    ui.Manage.Close();
                    board.ClearHighlights();
                    cameraRig?.ClearFocus();
                    if (!player.bot)
                    {
                        sound?.Play(Sfx.Turn);
                        SaveMatch(true);
                        if (HumanCount > 1)
                        {
                            ui.Banner($"{MonopolyStyle.Possessive(player).ToUpperInvariant()} TURN", PlayerColor(e.player), 0.7f);
                        }
                    }
                    yield return new WaitForSeconds(Beat(0.2f));
                    break;
                }
                case MatchEventKind.RoundStarted:
                    ui.SetMatchInfo(ModeName, e.value, Match.rules.roundLimit);
                    if (Match.rules.roundLimit > 0 && e.value == Match.rules.roundLimit)
                    {
                        ui.Banner("FINAL ROUND!", MonopolyStyle.Gold, 1f);
                        yield return new WaitForSeconds(Beat(0.8f));
                    }
                    break;
                case MatchEventKind.DiceRolled:
                    yield return RollDice(e);
                    break;
                case MatchEventKind.Doubles:
                    ui.Banner(e.value >= 2 ? "DOUBLES AGAIN!" : "DOUBLES!", MonopolyStyle.Gold, 0.6f);
                    sound?.Play(Sfx.Doubles);
                    yield return new WaitForSeconds(Beat(0.45f));
                    break;
                case MatchEventKind.Triples:
                    ui.Banner("TRIPLES! GO ANYWHERE", MonopolyStyle.Gold, 1f);
                    sound?.Play(Sfx.Jackpot);
                    yield return new WaitForSeconds(Beat(0.7f));
                    break;
                case MatchEventKind.SpeedDie:
                    ui.Banner(e.value == (int)SpeedFace.Bus ? "BUS!" : "MR. MONOPOLY!", e.value == (int)SpeedFace.Bus ? MonopolyStyle.Blue : MonopolyStyle.Red, 0.8f);
                    sound?.Play(Sfx.Whoosh);
                    yield return new WaitForSeconds(Beat(0.55f));
                    break;
                case MatchEventKind.Moved:
                    yield return MoveToken(e.player, e.from, e.space, e.value);
                    break;
                case MatchEventKind.WentToJail:
                    ui.Banner("GO TO JAIL!", MonopolyStyle.Ink, 0.9f);
                    sound?.Play(Sfx.Jail);
                    yield return JailToken(e.player);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "goes")} to jail.", MonopolyStyle.Ink, Icons.Lock);
                    break;
                case MatchEventKind.PassedGo:
                    ui.Floating.AtWorld($"+{MonopolyStyle.Money(e.value)}", MonopolyStyle.Green, board.Center(0) + Vector3.up * 0.4f);
                    if (!string.IsNullOrEmpty(e.text))
                    {
                        ui.Banner("DOUBLE SALARY!", MonopolyStyle.Green, 0.8f);
                    }
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "collects")} {MonopolyStyle.Money(e.value)} salary.", MonopolyStyle.Green, Icons.Coins);
                    break;
                case MatchEventKind.Landed:
                    FlashSpace(e.space);
                    break;
                case MatchEventKind.Transfer:
                    MoneyMoved(e);
                    yield return new WaitForSeconds(Beat(0.12f));
                    break;
                case MatchEventKind.BuyOffered:
                    if (Match.players[e.player].bot)
                    {
                        ui.Deed.ShowInfo(Match, e.space);
                        yield return new WaitForSeconds(Beat(0.5f));
                    }
                    break;
                case MatchEventKind.Bought:
                {
                    bool auction = e.text == "auction";
                    board.SetOwner(e.space, ColorOf(e.player));
                    ui.Deed.Close();
                    if (auction)
                    {
                        ui.Auction.Close();
                        ui.Banner("SOLD!", PlayerColor(e.player), 0.7f);
                        sound?.Play(Sfx.Gavel);
                    }
                    sound?.Play(Sfx.Buy);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, auction ? "wins" : "buys")} {Match.Space(e.space).name} for {MonopolyStyle.Money(e.value)}.", PlayerColor(e.player), Icons.House);
                    yield return new WaitForSeconds(Beat(0.35f));
                    break;
                }
                case MatchEventKind.Dealt:
                    board.SetOwner(e.space, ColorOf(e.player));
                    sound?.Play(Sfx.Card, 0.6f);
                    yield return new WaitForSeconds(Beat(0.12f));
                    break;
                case MatchEventKind.Rent:
                    ui.Floating.AtWorld($"RENT {MonopolyStyle.Money(e.value)}", MonopolyStyle.Red, board.Center(e.space) + Vector3.up * 0.5f);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "pays")} {NamedObject(e.other)} {MonopolyStyle.Money(e.value)} rent for {Match.Space(e.space).name}.", PlayerColor(e.other), Icons.Coins);
                    yield return new WaitForSeconds(Beat(0.25f));
                    break;
                case MatchEventKind.NoRent:
                    ui.Toast(e.value == 1 ? $"{Named(e.other)} {Verb(e.other, "is")} in jail: no rent!" : $"{Match.Space(e.space).name} is mortgaged: no rent.", MonopolyStyle.Muted, Icons.Info);
                    break;
                case MatchEventKind.Tax:
                    ui.Floating.AtWorld($"-{MonopolyStyle.Money(e.value)}", MonopolyStyle.Red, board.Center(e.space) + Vector3.up * 0.5f);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "pays")} {Match.Space(e.space).name}:{MonopolyStyle.Money(e.value)}.", MonopolyStyle.Ink, Icons.MoneyBag);
                    break;
                case MatchEventKind.CardDrawn:
                {
                    PlayerState player = Match.players[e.player];
                    if (!ui.Deed.IsOffer)
                    {
                        ui.Deed.Close();
                    }
                    sound?.Play(Sfx.Card);
                    yield return ui.Cards.Reveal(e.deck, e.text, player, !player.bot, Beat(2.3f));
                    break;
                }
                case MatchEventKind.LeftJail:
                {
                    int who = e.player;
                    string how = e.value == 0 ? $"{Verb(who, "rolls")} doubles and {Verb(who, "walks")} out of jail"
                        : e.value == 1 ? $"{Verb(who, "pays")} the fine and {Verb(who, "leaves")} jail"
                        : e.value == 2 ? $"{Verb(who, "uses")} a Get Out of Jail Free card"
                        : $"{Verb(who, "pays")} the fine after three tries";
                    ui.Toast($"{Named(e.player)} {how}.", MonopolyStyle.Green, Icons.Lock);
                    shownJailed[e.player] = false;
                    ArrangeSpace(Match.Board.JailIndex, true);
                    yield return new WaitForSeconds(Beat(0.3f));
                    break;
                }
                case MatchEventKind.StayedInJail:
                    ui.Toast($"No doubles: {Named(e.player)} {Verb(e.player, "stays")} in jail.",MonopolyStyle.Ink, Icons.Lock);
                    sound?.Play(Sfx.Error, 0.5f);
                    break;
                case MatchEventKind.JailCardGained:
                    ui.Toast($"{Named(e.player)} {Verb(e.player, e.other >= 0 ? "receives" : "keeps")} a Get Out of Jail Free card.", MonopolyStyle.Gold, Icons.Ticket);
                    break;
                case MatchEventKind.Built:
                    board.SetBuildings(e.space, e.value, true);
                    sound?.Play(Sfx.Build);
                    ui.Toast(e.text == "free"
                        ? $"Free upgrade for {NamedObject(e.player)} on {Match.Space(e.space).name}!"
                        : $"{Named(e.player)} {Verb(e.player, "builds")} {(e.value == MonopolyMatch.Hotel ? "a hotel" : "a house")} on {Match.Space(e.space).name}.", PlayerColor(e.player), e.value == MonopolyMatch.Hotel ? Icons.Hotel : Icons.House);
                    ui.Manage.Refresh();
                    yield return new WaitForSeconds(Beat(0.3f));
                    break;
                case MatchEventKind.SoldBuilding:
                    board.SetBuildings(e.space, e.value, false);
                    sound?.Play(Sfx.Sell);
                    ui.Manage.Refresh();
                    yield return new WaitForSeconds(Beat(0.12f));
                    break;
                case MatchEventKind.Mortgaged:
                    board.SetMortgaged(e.space, true);
                    sound?.Play(Sfx.Mortgage);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "mortgages")} {Match.Space(e.space).name}.", MonopolyStyle.Muted, Icons.Mortgage);
                    ui.Manage.Refresh();
                    break;
                case MatchEventKind.Unmortgaged:
                    board.SetMortgaged(e.space, false);
                    sound?.Play(Sfx.Mortgage);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "lifts")} the mortgage on {Match.Space(e.space).name}.", MonopolyStyle.Green, Icons.Mortgage);
                    ui.Manage.Refresh();
                    break;
                case MatchEventKind.AuctionStarted:
                    ui.Deed.Close();
                    sound?.Play(Sfx.Gavel);
                    ui.Toast($"{Match.Space(e.space).name} goes to auction.", MonopolyStyle.ChanceOrange, Icons.Gavel);
                    if (Match.auction.Running)
                    {
                        ui.Auction.Refresh(Match, ui.TokenSprite, -1, null, null);
                    }
                    yield return new WaitForSeconds(Beat(0.4f));
                    break;
                case MatchEventKind.Bid:
                    sound?.Play(Sfx.CoinsIn, 0.5f);
                    ui.Floating.AtRect(MonopolyStyle.Money(e.value), PlayerColor(e.player), ui.Panel(e.player).Anchor, new Vector2(0f, 40f), 36f);
                    if (Match.auction.Running)
                    {
                        ui.Auction.Refresh(Match, ui.TokenSprite, -1, null, null);
                    }
                    yield return new WaitForSeconds(Beat(0.3f));
                    break;
                case MatchEventKind.BidPassed:
                    if (Match.auction.Running)
                    {
                        ui.Auction.Refresh(Match, ui.TokenSprite, -1, null, null);
                    }
                    yield return new WaitForSeconds(Beat(0.15f));
                    break;
                case MatchEventKind.AuctionUnsold:
                    ui.Auction.Close();
                    ui.Toast($"Nobody bids: {Match.Space(e.space).name} stays with the bank.", MonopolyStyle.Muted, Icons.Gavel);
                    break;
                case MatchEventKind.Traded:
                    sound?.Play(Sfx.Trade);
                    ui.Toast($"{Named(e.player)} and {NamedObject(e.other)} make a deal.", MonopolyStyle.Green, Icons.Handshake);
                    yield return new WaitForSeconds(Beat(0.4f));
                    break;
                case MatchEventKind.OwnerChanged:
                    board.SetOwner(e.space, e.other >= 0 ? ColorOf(e.other) : -1);
                    if (e.other < 0)
                    {
                        board.SetMortgaged(e.space, false);
                    }
                    break;
                case MatchEventKind.DebtStarted:
                    sound?.Play(Sfx.Error);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "owes")} {NamedObject(e.other)} {MonopolyStyle.Money(e.value)} and must raise money.", MonopolyStyle.Red, Icons.Coins);
                    break;
                case MatchEventKind.DebtPaid:
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "settles")} the debt of {MonopolyStyle.Money(e.value)}.", MonopolyStyle.Green, Icons.Check);
                    break;
                case MatchEventKind.Bankrupt:
                {
                    PlayerState player = Match.players[e.player];
                    sound?.Play(Sfx.Bankrupt);
                    ui.Banner($"{player.name.ToUpperInvariant()} {Verb(e.player, "is").ToUpperInvariant()} BANKRUPT", MonopolyStyle.Ink, 1.2f);
                    ui.Toast($"{Named(e.player)} {Verb(e.player, "is")} bankrupt; everything goes to {NamedObject(e.other)}.", MonopolyStyle.Ink, Icons.Flag);
                    yield return tokens[e.player].Topple();
                    shownPosition[e.player] = -1;
                    ui.RefreshPlayers(Match);
                    break;
                }
                case MatchEventKind.PotChanged:
                    ui.SetPot(Match.rules.freeParkingJackpot, e.value);
                    break;
                case MatchEventKind.Jackpot:
                    ui.Banner($"JACKPOT {MonopolyStyle.Money(e.value)}!", MonopolyStyle.Gold, 1.2f);
                    sound?.Play(Sfx.Jackpot);
                    yield return new WaitForSeconds(Beat(0.6f));
                    break;
                case MatchEventKind.Message:
                    if (!string.IsNullOrEmpty(e.text))
                    {
                        ui.Toast(e.text, MonopolyStyle.Ink, Icons.Info);
                    }
                    break;
            }
        }

        private IEnumerator RollDice(MatchEvent e)
        {
            sound?.Play(Sfx.DiceShake);
            if (dice == null)
            {
                yield break;
            }
            Vector3 origin = ThrowOrigin(e.player);
            yield return new WaitForSeconds(Beat(0.12f));
            sound?.Play(Sfx.DiceThrow);
            yield return dice.Roll(e.roll, origin, 0.95f / Speed);
            string total = e.roll.HasSpeed && e.roll.SpeedValue > 0 ? $"{e.roll.Total + e.roll.SpeedValue}" : $"{e.roll.Total}";
            ui.Floating.AtWorld(total, Color.white, dice.transform.position + Vector3.up * 0.9f, 64f);
            yield return new WaitForSeconds(Beat(0.2f));
        }

        /// <summary>Where the dice are thrown from: the corner of the table next to the player's panel.</summary>
        private Vector3 ThrowOrigin(int seat)
        {
            float h = board.Side * 0.5f;
            Vector3[] corners = { new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h), new Vector3(-h, 0f, -h) };
            return board.transform.TransformPoint(corners[Mathf.Clamp(seat, 0, 3)] * 0.7f + Vector3.up * board.Surface);
        }

        private void FlashSpace(int space)
        {
            StartCoroutine(Flash(space));
        }

        private IEnumerator Flash(int space)
        {
            board.Highlight(space, new Color(1f, 1f, 1f, 0.85f), false);
            yield return new WaitForSeconds(0.9f);
            if (Match == null || Match.phase != MatchPhase.MoveAnywhere)
            {
                board.ClearHighlight(space);
            }
        }

        private void MoneyMoved(MatchEvent e)
        {
            int from = e.player;
            int to = e.other;
            int amount = e.value;
            if (from >= 0 && from < shownCash.Count)
            {
                shownCash[from] -= amount;
                PlayerUI panel = ui.Panel(from);
                panel?.SetCash(shownCash[from], true);
                if (panel != null)
                {
                    ui.Floating.AtRect($"-{MonopolyStyle.Money(amount)}", MonopolyStyle.Red, panel.Anchor, new Vector2(90f, -10f), 34f);
                }
            }
            if (to >= 0 && to < shownCash.Count)
            {
                shownCash[to] += amount;
                PlayerUI panel = ui.Panel(to);
                panel?.SetCash(shownCash[to], true);
                if (panel != null)
                {
                    ui.Floating.AtRect($"+{MonopolyStyle.Money(amount)}", MonopolyStyle.Green, panel.Anchor, new Vector2(90f, 20f), 34f);
                }
            }
            Vector2 a = PartyPoint(from);
            Vector2 b = PartyPoint(to);
            ui.Floating.FlyMoney(a, b, Mathf.Clamp(amount / 100 + 1, 1, 5));
            sound?.Play(to >= 0 && from < 0 ? Sfx.CoinsIn : Sfx.CoinsOut, Mathf.Clamp01(0.5f + amount / 500f));
        }

        /// <summary>Where money comes from or goes to on screen: a player's panel, the bank (board centre) or the pot (Free Parking).</summary>
        private Vector2 PartyPoint(int party)
        {
            if (party >= 0 && ui.Panel(party) != null)
            {
                return ui.Floating.LayerPoint(ui.Panel(party).Anchor);
            }
            Vector3 world = party == Party.Pot ? board.Center(Match.Board.FreeParkingIndex) : board.transform.TransformPoint(new Vector3(0f, board.Surface, 0f));
            return ui.Floating.WorldToLayer(world);
        }

        // ------------------------------------------------------------------ tokens

        private IEnumerator MoveToken(int seat, int from, int to, int steps)
        {
            if (seat < 0 || seat >= tokens.Count)
            {
                yield break;
            }
            MonopolyPlayer token = tokens[seat];
            int count = Match.SpaceCount;
            int direction = steps >= 0 ? 1 : -1;
            int hops = Math.Abs(steps);
            if (hops == 0)
            {
                yield break;
            }
            int start = shownPosition[seat];
            shownPosition[seat] = -1;
            shownJailed[seat] = false;
            if (start >= 0)
            {
                ArrangeSpace(start, true);
            }
            shownPosition[seat] = to;
            Vector3 finalSpot = SpotFor(seat, to);
            shownPosition[seat] = -1;
            var path = new List<Vector3>(hops);
            for (int k = 1; k <= hops; k++)
            {
                int space = ((from + direction * k) % count + count) % count;
                path.Add(k == hops ? finalSpot : board.TokenSpot(space, 0, 1, false));
            }
            cameraRig?.Focus(token.transform.position, 1f);
            if (hops > 12)
            {
                // A long trip (a card sending the token across the board) is a flight, not thirty hops.
                sound?.Play(Sfx.Whoosh);
                cameraRig?.Focus(finalSpot, 0.8f);
                yield return token.Fly(finalSpot, 1.1f / Speed, 2.6f);
                sound?.Play(Sfx.Hop, 0.7f);
            }
            else
            {
                float hop = Mathf.Clamp(0.25f - hops * 0.006f, 0.12f, 0.25f) / Speed;
                yield return token.Hop(path, hop, k =>
                {
                    sound?.Play(Sfx.Hop, 0.55f);
                    cameraRig?.Focus(path[k], 1f);
                });
            }
            shownPosition[seat] = to;
            ArrangeSpace(to, true);
        }

        private IEnumerator JailToken(int seat)
        {
            if (seat < 0 || seat >= tokens.Count)
            {
                yield break;
            }
            int start = shownPosition[seat];
            int jail = Match.Board.JailIndex;
            shownPosition[seat] = -1;
            if (start >= 0)
            {
                ArrangeSpace(start, true);
            }
            shownPosition[seat] = jail;
            shownJailed[seat] = true;
            Vector3 spot = SpotFor(seat, jail);
            shownPosition[seat] = -1;
            cameraRig?.Focus(board.Center(jail), 0.8f);
            yield return tokens[seat].Fly(spot, 0.9f / Speed);
            shownPosition[seat] = jail;
            ArrangeSpace(jail, true);
        }

        /// <summary>The tokens shown on a space (jailed ones separately from visitors), in seat order.</summary>
        private List<int> TokensOn(int space, bool jailed)
        {
            var list = new List<int>();
            for (int i = 0; i < shownPosition.Count; i++)
            {
                if (shownPosition[i] == space && !Match.players[i].bankrupt && shownJailed[i] == jailed)
                {
                    list.Add(i);
                }
            }
            return list;
        }

        private Vector3 SpotFor(int seat, int space)
        {
            bool jailed = shownJailed[seat];
            List<int> group = TokensOn(space, jailed);
            int slot = Mathf.Max(0, group.IndexOf(seat));
            return board.TokenSpot(space, slot, Mathf.Max(1, group.Count), jailed);
        }

        /// <summary>Spreads the tokens on a space so they stand side by side.</summary>
        private void ArrangeSpace(int space, bool animate)
        {
            foreach (bool jailed in new[] { false, true })
            {
                List<int> group = TokensOn(space, jailed);
                for (int slot = 0; slot < group.Count; slot++)
                {
                    int seat = group[slot];
                    Vector3 spot = board.TokenSpot(space, slot, group.Count, jailed);
                    MonopolyPlayer token = tokens[seat];
                    if (token.IsMoving)
                    {
                        continue;
                    }
                    if (animate && token.isActiveAndEnabled)
                    {
                        StartCoroutine(token.Shuffle(spot, 0.25f));
                    }
                    else
                    {
                        token.PlaceAt(spot);
                    }
                }
            }
        }

        private void ArrangeAll()
        {
            for (int i = 0; i < shownPosition.Count; i++)
            {
                if (Match.players[i].bankrupt)
                {
                    tokens[i].gameObject.SetActive(false);
                }
            }
            foreach (int space in shownPosition.Where(p => p >= 0).Distinct().ToList())
            {
                ArrangeSpace(space, false);
            }
        }

        /// <summary>
        /// Makes the board and the panels match the rules engine exactly (after the events of a turn, and when a game is
        /// loaded): tokens, owner tags, buildings, mortgages, cash and the match info.
        /// </summary>
        private void SyncBoard(bool animate)
        {
            if (Match == null)
            {
                return;
            }
            bool moved = false;
            for (int i = 0; i < Match.players.Count && i < tokens.Count; i++)
            {
                PlayerState player = Match.players[i];
                if (player.bankrupt)
                {
                    shownPosition[i] = -1;
                    continue;
                }
                if (shownPosition[i] != player.position || shownJailed[i] != player.inJail)
                {
                    shownPosition[i] = player.position;
                    shownJailed[i] = player.inJail;
                    moved = true;
                }
                if (shownCash[i] != player.cash)
                {
                    shownCash[i] = player.cash;
                    ui.Panel(i)?.SetCash(player.cash, animate);
                }
            }
            if (moved)
            {
                ArrangeAll();
            }
            foreach (int space in Match.Board.Properties)
            {
                DeedState deed = Match.Deed(space);
                board.SetOwner(space, deed.Owned ? ColorOf(deed.owner) : -1);
                board.SetMortgaged(space, deed.mortgaged);
                if (board.Tile(space) != null && board.Tile(space).Houses != deed.houses)
                {
                    board.SetBuildings(space, deed.houses, animate);
                }
            }
            ui.SetMatchInfo(ModeName, Match.round, Match.rules.roundLimit);
            ui.SetPot(Match.rules.freeParkingJackpot, Match.pot);
            ui.RefreshPlayers(Match);
            ui.Manage.Refresh();
        }

        // ------------------------------------------------------------------ saving

        [Serializable]
        private class SaveData
        {
            public MonopolyMatch match;
            public MatchSetup setup;
            public int mode;
        }

        public override bool DoesSaveGameExist()
        {
            IStorageStrategy disk = Disk;
            return disk != null && disk.DoesKeyExist(SavePrefix + "Exists") && disk.GetBool(SavePrefix + "Exists") && disk.DoesKeyExist(SavePrefix + "Data");
        }

        public override void SaveGame()
        {
            SaveMatch(false);
        }

        private void SaveMatch(bool silent)
        {
            IStorageStrategy disk = Disk;
            if (disk == null || Match == null || Match.IsOver)
            {
                return;
            }
            var data = new SaveData { match = Match, setup = Setup, mode = LevelIndex };
            disk.SetString(SavePrefix + "Data", JsonUtility.ToJson(data));
            disk.SetBool(SavePrefix + "Exists", true);
            try
            {
                disk.Persist();
            }
            catch (NotImplementedException notImplemented)
            {
                if (!silent)
                {
                    UI?.UpdateError($"Cannot save the game: {notImplemented.Message}");
                }
                return;
            }
            UI?.EnableLoad();
            if (!silent)
            {
                ui.Toast("Game saved.", MonopolyStyle.Green, Icons.Save);
            }
        }

        private void ClearSave()
        {
            IStorageStrategy disk = Disk;
            if (disk == null)
            {
                return;
            }
            if (disk.DoesKeyExist(SavePrefix + "Data"))
            {
                disk.DeleteByKey(SavePrefix + "Data");
            }
            disk.SetBool(SavePrefix + "Exists", false);
            try
            {
                disk.Persist();
            }
            catch (NotImplementedException)
            {
                // Nothing to clear on a transient store.
            }
        }

        public override void LoadGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null || !DoesSaveGameExist())
            {
                UI?.UpdateError("There is no saved game to continue.");
                return;
            }
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(disk.GetString(SavePrefix + "Data"));
            }
            catch (ArgumentException exception)
            {
                UI?.UpdateError($"The saved game cannot be read: {exception.Message}");
                return;
            }
            if (data == null || data.match == null || data.match.players == null || data.match.players.Count < 2)
            {
                UI?.UpdateError("The saved game is incomplete.");
                return;
            }
            LoadLevel(data.mode);
            MonopolyLevel mode = CurrentMode;
            matchSettings = mode != null && mode.Settings != null ? mode.Settings : MonopolySettings;
            Setup = data.setup ?? MatchSetup.Default();
            data.match.Attach(matchSettings.Board.CreateLayout(), random);
            BeginDirecting(data.match, false);
            ui.Toast("Welcome back! The game goes on.", MonopolyStyle.Green, Icons.Play);
        }
    }
}
