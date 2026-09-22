using System;
using System.Collections;
using System.IO;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Development players only. Started with <c>-monopoly-tour &lt;folder&gt;</c>, it plays through the game with scripted
    /// dice (the title screen, the new game screen, a turn, a Chance card, buying, building, trading, an auction, jail,
    /// the pause menu and house rules, the speed die and the results) and saves a screenshot of every step into the
    /// folder, then quits. The human seat is played by the tour itself. Without the argument it does nothing.
    /// </summary>
    public class MonopolyTour : MonoBehaviour
    {
        private string folder;
        private MonopolyGameManager manager;
        private MonopolyController controller;
        private StreamWriter log;
        private bool pilot;
        private Func<bool> hold;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue("-monopoly-tour");
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<MonopolyTour>() != null)
            {
                return;
            }
            var host = new GameObject("Monopoly Tour");
            DontDestroyOnLoad(host);
            host.AddComponent<MonopolyTour>().folder = target;
        }

        private MonopolyMatch Match => manager.Match;

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error)
                {
                    Write($"{type}: {message}\n{stack}");
                }
            };
            while ((manager = FindAnyObjectByType<MonopolyGameManager>()) == null)
            {
                yield return null;
            }
            controller = manager.MonopolyController;
            PlayerPrefs.DeleteKey("Monopoly.Setup");
            yield return new WaitForSeconds(3f);
            yield return Shot("01_title");

            manager.MonopolyUI.ShowSetup();
            yield return new WaitForSeconds(1.2f);
            yield return Shot("02_setup");

            // Classic: the tour plays "You" against three computer players.
            MatchSetup setup = MatchSetup.Default();
            manager.MonopolyUI.Setup.HideNow();
            manager.BeginMatch(setup, 0);
            yield return WaitFor(() => manager.WaitingForHuman && Match.phase == MatchPhase.Roll, 20f);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("03_first_turn");

            // A Chance card: from GO, a seven lands on Chance.
            Match.ScriptRoll(new DiceRoll(3, 4));
            controller.Roll(0);
            yield return WaitFor(() => manager.MonopolyUI.Cards.IsOpen, 10f);
            yield return new WaitForSeconds(0.9f);
            yield return Shot("04_chance");
            manager.MonopolyUI.Cards.Confirm();
            pilot = true;
            hold = () => Match.phase == MatchPhase.BuyChoice;
            StartCoroutine(Pilot());

            // Buying: the next property for sale the tour lands on.
            yield return WaitFor(() => Match.phase == MatchPhase.BuyChoice && Match.Decider == 0 && manager.MonopolyUI.Deed.IsOpen, 90f);
            yield return new WaitForSeconds(0.8f);
            yield return Shot("05_buy_offer");
            hold = null;

            // A few rounds of play.
            yield return WaitFor(() => Match.round >= 4, 240f);
            yield return WaitFor(() => manager.WaitingForHuman && Match.Decider == 0 && Match.phase == MatchPhase.Roll, 60f);
            yield return Shot("06_midgame");

            // Building: hand the tour the light blue set and some cash, then build.
            pilot = false;
            yield return WaitFor(() => manager.WaitingForHuman && Match.Decider == 0 && (Match.phase == MatchPhase.Roll || Match.phase == MatchPhase.EndTurn), 60f);
            foreach (int space in new[] { 6, 8, 9, 1, 3 })
            {
                Match.deeds[space].owner = 0;
                Match.deeds[space].mortgaged = false;
            }
            Match.players[0].cash += 1500;
            controller.OpenManager(0);
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < 3; i++)
            {
                foreach (int space in new[] { 6, 8, 9 })
                {
                    controller.Build(0, space);
                    yield return new WaitForSeconds(0.45f);
                }
            }
            controller.Build(0, 1);
            yield return new WaitForSeconds(0.5f);
            controller.Build(0, 3);
            yield return new WaitForSeconds(1f);
            yield return Shot("07_manage");
            manager.MonopolyUI.Manage.Close();
            yield return new WaitForSeconds(1.2f);
            yield return Shot("08_houses");

            // Trading.
            controller.OpenTrade(0);
            yield return new WaitForSeconds(1f);
            yield return Shot("09_trade");
            manager.MonopolyUI.Trade.Close();
            yield return new WaitForSeconds(0.5f);

            // An auction: on the tour's next roll, land on a property nobody owns and pass on it.
            if (Match.phase == MatchPhase.EndTurn && Match.Decider == 0)
            {
                controller.EndTurn(0);
            }
            hold = () => Match.phase == MatchPhase.Roll;
            pilot = true;
            yield return WaitFor(() => manager.WaitingForHuman && Match.Decider == 0 && Match.phase == MatchPhase.Roll, 120f);
            pilot = false;
            hold = null;
            int position = Match.players[0].position;
            bool IsFree(int s) => Match.Space(s).IsProperty && !Match.Deed(s).Owned;
            int free = Enumerable.Range(3, 9).Select(k => (position + k) % 40).Where(IsFree).DefaultIfEmpty(-1).First();
            if (free < 0)
            {
                // Nothing for sale within a roll: start the roll seven spaces before the first free property.
                free = Enumerable.Range(0, 40).Where(IsFree).DefaultIfEmpty(-1).First();
                if (free >= 0)
                {
                    position = (free - 7 + 40) % 40;
                    Match.players[0].position = position;
                }
            }
            if (free >= 0 && Match.phase == MatchPhase.Roll && !Match.players[0].inJail)
            {
                int distance = (free - position + 40) % 40;
                int a = Mathf.Clamp(distance / 2, 1, 6);
                int b = distance - a;
                if (a == b)
                {
                    a -= 1;
                    b += 1;
                }
                Match.ScriptRoll(new DiceRoll(a, b));
                controller.Roll(0);
                yield return WaitFor(() => Match.phase == MatchPhase.BuyChoice && Match.Decider == 0 && manager.WaitingForHuman, 15f);
                controller.DeclineBuy(0);
                yield return WaitFor(() => manager.MonopolyUI.Auction.IsOpen, 10f);
                yield return new WaitForSeconds(1.4f);
                yield return Shot("10_auction");
            }
            else
            {
                Write($"auction skipped (free {free}, phase {Match.phase})");
            }
            pilot = true;

            // Jail: the tour's next roll goes to jail.
            yield return WaitFor(() => manager.WaitingForHuman && Match.Decider == 0 && Match.phase == MatchPhase.Roll, 120f);
            pilot = false;
            int toJail = (30 - Match.players[0].position + 40) % 40;
            if (toJail >= 3 && toJail <= 11)
            {
                int a = Mathf.Clamp(toJail / 2, 1, 6);
                int b = toJail - a;
                if (a == b)
                {
                    a -= 1;
                    b += 1;
                }
                Match.ScriptRoll(new DiceRoll(a, b));
            }
            else
            {
                Match.players[0].position = 27;
                Match.ScriptRoll(new DiceRoll(1, 2));
            }
            controller.Roll(0);
            // The rules engine is done at once; wait for the board to play the trip to jail.
            yield return new WaitForSeconds(0.3f);
            yield return WaitFor(() => Match.players[0].inJail && manager.WaitingForHuman, 20f);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("11_go_to_jail");
            pilot = true;
            yield return WaitFor(() => manager.WaitingForHuman && Match.Decider == 0 && Match.phase == MatchPhase.JailChoice, 120f);
            pilot = false;
            yield return new WaitForSeconds(0.5f);
            yield return Shot("12_jail_choice");

            // The pause menu and the house rules.
            manager.TransitionState(BaseGameState.Paused);
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("13_pause");
            manager.MonopolyUI.ShowSettings();
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("14_house_rules");
            manager.MonopolyUI.HideSettings();
            manager.TransitionState(BaseGameState.Running);
            yield return new WaitForSeconds(0.5f);

            // The speed die: a Speed Die game where the tour has passed GO and rolls the bus.
            manager.BeginMatch(MatchSetup.Default(), 1);
            yield return WaitFor(() => manager.WaitingForHuman && Match.phase == MatchPhase.Roll && Match.Decider == 0, 20f);
            Match.players[0].passedGo = true;
            Match.ScriptRoll(new DiceRoll(2, 5, SpeedFace.Bus));
            controller.Roll(0);
            yield return new WaitForSeconds(0.3f);
            yield return WaitFor(() => manager.WaitingForHuman && Match.phase == MatchPhase.BusChoice, 15f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("15_speed_die_bus");
            controller.ChooseBus(0, 2);
            pilot = true;
            hold = () => true;
            yield return new WaitForSeconds(0.3f);
            yield return WaitFor(() => manager.WaitingForHuman && Match.Decider == 0, 30f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("16_speed_die_play");
            hold = null;

            // The end: the richest wins when the match is called off.
            pilot = false;
            yield return WaitFor(() => manager.WaitingForHuman, 60f);
            Match.players[0].cash += 3000;
            Match.Resign();
            yield return WaitFor(() => manager.MonopolyUI.Results.IsOpen, 20f);
            yield return new WaitForSeconds(2f);
            yield return Shot("17_results");

            Write("tour finished");
            // The tour leaves no progress behind; the login of the online game is not the tour's to forget.
            string server = manager.Online != null && manager.Online.ServerClient != null ? manager.Online.ServerClient.ServerUri : null;
            string token = server != null ? IdentityTokens.Load(server) : "";
            PlayerPrefs.DeleteAll();
            if (!string.IsNullOrEmpty(token))
            {
                IdentityTokens.Save(server, token);
            }
            PlayerPrefs.Save();
            log.Dispose();
            yield return null;
            Application.Quit();
        }

        /// <summary>Plays the human seat: rolls, buys what it can, passes auctions, pays or gives up, ends turns.</summary>
        private IEnumerator Pilot()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.35f);
                if (!pilot || Match == null)
                {
                    continue;
                }
                // A card drawn by a person waits for a click while the turn's events play.
                if (manager.MonopolyUI.Cards.IsOpen)
                {
                    manager.MonopolyUI.Cards.Confirm();
                    continue;
                }
                if (!manager.WaitingForHuman || Match.Decider != 0 || (hold != null && hold()))
                {
                    continue;
                }
                switch (Match.phase)
                {
                    case MatchPhase.Roll:
                    case MatchPhase.JailChoice:
                        controller.Roll(0);
                        break;
                    case MatchPhase.BusChoice:
                        controller.ChooseBus(0, 2);
                        break;
                    case MatchPhase.MoveAnywhere:
                        controller.ChooseDestination(0, 39);
                        break;
                    case MatchPhase.BuyChoice:
                        if (Match.CanAffordPurchase && Match.players[0].cash > 300)
                        {
                            controller.Buy(0);
                        }
                        else
                        {
                            controller.DeclineBuy(0);
                        }
                        break;
                    case MatchPhase.Auction:
                        controller.PassBid(0);
                        break;
                    case MatchPhase.RaiseFunds:
                    {
                        int space = Match.PropertiesOf(0).FirstOrDefault(s => Match.CanMortgage(0, s, out _));
                        if (space > 0)
                        {
                            controller.Mortgage(0, space);
                        }
                        else
                        {
                            controller.DeclareBankruptcy(0);
                        }
                        break;
                    }
                    case MatchPhase.EndTurn:
                        manager.MonopolyUI.Manage.Close();
                        controller.EndTurn(0);
                        break;
                }
            }
        }

        private IEnumerator WaitFor(Func<bool> condition, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout)
                {
                    Write($"timed out after {timeout} s (phase {(Match != null ? Match.phase.ToString() : "none")}, state {manager.State})");
                    yield break;
                }
                yield return null;
            }
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            Write($"{name}: state {manager.State}, phase {(Match != null ? Match.phase.ToString() : "none")}, round {(Match != null ? Match.round : 0)}");
            yield return null;
            yield return null;
        }

        private void Write(string line)
        {
            log?.WriteLine($"{Time.realtimeSinceStartup:0.0} {line}");
        }
    }
}
