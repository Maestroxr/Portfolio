using System;
using System.Collections;
using System.IO;
using Gamebox;
using Gamebox.UI;
using Portfolio.Heroes.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Development players only. Started with <c>-heroes-hotseat &lt;folder&gt;</c>, it plays a hot seat and saves a
    /// screenshot of each step into the folder, then quits; without the argument it does nothing. It sets up Alice, the
    /// computer and Bob on a map for three (the setup the shared local play screen remembers), starts it, and looks at
    /// the hand-over screen before every person's turn and at each person's map after it, the computer's turn watched by
    /// Bob, a battle Alice and Bob fight at one screen, Continue taking the saved hot seat up again, the results, and Play
    /// Again. The people's moves are the computer's brain's; the battle and the end are arranged behind the rules' back.
    /// </summary>
    public class HeroesHotSeatTour : MonoBehaviour
    {
        private string folder;
        private HeroesGameManager manager;
        private StreamWriter log;
        private int shot;
        private readonly AdventureAI brain = new AdventureAI();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue("-heroes-hotseat");
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<HeroesHotSeatTour>() != null)
            {
                return;
            }
            var host = new GameObject("Heroes Hot Seat Tour");
            DontDestroyOnLoad(host);
            host.AddComponent<HeroesHotSeatTour>().folder = target;
        }

        private HeroesGame Game => manager != null ? manager.Game : null;

        private HeroesUI UI => manager.HeroesUI;

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "tour.log")) { AutoFlush = true };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Exception || type == LogType.Error || type == LogType.Warning && message.StartsWith("Heroes"))
                {
                    Write($"{type}: {message}\n{stack}");
                }
            };
            Application.runInBackground = true;
            while ((manager = FindAnyObjectByType<HeroesGameManager>()) == null)
            {
                yield return null;
            }
            Write($"Screen {Screen.width}x{Screen.height}, language {(GameLanguages.IsEnglish ? "English" : "not English")}.");
            yield return new WaitForSeconds(3f);
            HeroesSettings own = Instantiate(manager.Options);
            own.name = "Heroes Hot Seat Tour Settings";
            own.edgeScroll = false;
            own.heroSpeed = 3f;
            own.battleSpeed = 3f;
            manager.Settings = own;

            yield return Shot("title");
            yield return Setup();
            yield return Turns();
            yield return PeopleFight();
            yield return ContinueSaved();
            yield return End();
            Write("Tour over.");
            yield return new WaitForSeconds(0.5f);
            log?.Flush();
            Application.Quit();
        }

        // ------------------------------------------------------------------ the setup

        /// <summary>Alice, the computer and Bob on the first map for three, remembered as the last setup, and the setup opened on it.</summary>
        private IEnumerator Setup()
        {
            if (manager.LocalPlay is not HeroesHotSeatRules rules)
            {
                Write("No hot seat rules.");
                yield break;
            }
            var match = new LocalMatch(GameType.Heroes, rules);
            for (int option = 0; option < rules.Maps.Count; option++)
            {
                match.SetOption(HeroesHotSeatRules.MapOption, option);
                rules.OptionsChanged(match);
                if (match.Count == 3)
                {
                    break;
                }
            }
            match.SetOption(HeroesHotSeatRules.SizeOption, 0);
            match.SetOption(HeroesHotSeatRules.BattlesOption, (int)BattleStyle.Battlefield);
            match[0].Name = "Alice";
            match[0].Kind = SeatKind.Human;
            if (match.Count >= 3)
            {
                match[1].Name = rules.DefaultName(1);
                match[1].Kind = SeatKind.Computer;
                match[1].Level = 1;
                match[2].Name = "Bob";
                match[2].Kind = SeatKind.Human;
            }
            else
            {
                match[1].Name = "Bob";
                match[1].Kind = SeatKind.Human;
            }
            match.HandOver = true;
            match.Remember();

            manager.OpenLocalPlay();
            yield return new WaitForSecondsRealtime(1f);
            LocalPlayUI setup = LocalPlayUI.For(manager);
            Write($"Setup open {setup.IsOpen}: {setup.Match?.Count} players on map {rules.LevelOf(setup.Match)}.");
            yield return Shot("setup");
            setup.StartMatch();
            float until = Time.unscaledTime + 30f;
            while ((Game == null || !manager.IsHandingOver) && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.8f);
            Write($"Started: hot seat {manager.IsHotSeat}, curtain {manager.IsHandingOver}, players " +
                  string.Join(", ", Game.State.players.ConvertAll(p => $"{p.name} ({(p.human ? "person" : "computer")})")));
            yield return Shot("curtain_first");
        }

        // ------------------------------------------------------------------ the turns

        private IEnumerator Turns()
        {
            for (int turn = 0; turn < 4 && Game != null; turn++)
            {
                yield return Lift($"turn{turn}");
                PlayerState person = manager.ViewerState;
                yield return Moves(4);
                yield return Shot($"turn{turn}_{Safe(person?.name)}_moved");
                UI.EndTurn();
                yield return WaitForCurtain(90f);
                yield return Shot($"turn{turn}_curtain_next");
                if (turn == 0)
                {
                    // The pause menu over the hand-over screen: the cover steps aside, and the map behind shows nobody's.
                    UI.OpenMenu();
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Shot("pause_while_handing_over");
                    manager.TransitionState(new Gamebox.GameState(BaseGameState.Running));
                    yield return new WaitForSecondsRealtime(0.8f);
                    Write($"Back from the pause menu: curtain {manager.IsHandingOver}.");
                }
            }
        }

        /// <summary>The person at the hand-over screen is ready: the screen lifts and their turn shows.</summary>
        private IEnumerator Lift(string name)
        {
            if (!manager.IsHandingOver)
            {
                Write($"{name}: no hand-over screen to lift.");
            }
            TurnCurtain.For(manager).Lift();
            yield return WaitForTurn(60f);
            yield return new WaitForSeconds(1.2f);
            PlayerState viewer = manager.ViewerState;
            Write($"{name}: {viewer?.name} holds the device (viewer {manager.Viewer}, holder {manager.HotSeatHolder}), " +
                  $"day {Game?.State.day}, current {Game?.State.currentPlayer}, log lines {UI.Hud != null}.");
            yield return Shot($"{name}_{Safe(viewer?.name)}");
        }

        /// <summary>A few moves of the person whose turn it is, as the computer's brain would make them.</summary>
        private IEnumerator Moves(int count)
        {
            for (int i = 0; i < count && Game != null && !Game.IsOver; i++)
            {
                if (Game.State.pending.Count > 0)
                {
                    manager.Commands.Choose(0);
                    yield return WaitForTurn(10f);
                    continue;
                }
                int who = Game.WaitingPlayer;
                if (Game.InBattle || manager.Battle.Running)
                {
                    yield return FightOut(60f);
                    continue;
                }
                GameCommand command = brain.Next(Game, who);
                if (command == null || command.kind == CommandKind.EndTurn)
                {
                    break;
                }
                if (!manager.Submit(command))
                {
                    brain.Refused(command);
                }
                yield return WaitForTurn(20f);
            }
        }

        // ------------------------------------------------------------------ a battle of two people

        /// <summary>
        /// A hero of Bob's is set beside Alice's hero behind the rules' back, on Alice's turn, and Alice attacks him: the
        /// battle is fought at one screen without a hand-over, and the bar names whose troops act.
        /// </summary>
        private IEnumerator PeopleFight()
        {
            if (Game == null)
            {
                yield break;
            }
            yield return Lift("before_battle");
            PlayerState attacker = manager.ViewerState;
            PlayerState defender = null;
            foreach (PlayerState player in Game.State.players)
            {
                if (player.human && player.alive && player.index != attacker.index)
                {
                    defender = player;
                }
            }
            HeroState hero = manager.Selected;
            if (hero == null && attacker.heroes.Count > 0)
            {
                hero = Game.State.Hero(attacker.heroes[0]);
            }
            int beside = hero != null ? FreeBeside(hero.cell) : -1;
            if (defender == null || beside < 0 || Game.State.freeHeroes.Count == 0)
            {
                Write("No battle of two people to show.");
                yield break;
            }
            HeroState other = Game.SpawnHero(defender.index, Game.State.freeHeroes[0], beside, true);
            manager.Map.Sync();
            hero.movement = Mathf.Max(hero.movement, hero.maxMovement);
            manager.Select(hero);
            yield return new WaitForSeconds(0.8f);
            manager.Commands.MoveHero(hero.id, other.cell);
            float until = Time.unscaledTime + 30f;
            while (Game != null && !(Game.InBattle && manager.WaitingForHuman && !UI.Busy) && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return new WaitForSeconds(2.5f);
            Write($"Battle of {attacker.name} and {defender.name}: in battle {Game.InBattle}, curtain {manager.IsHandingOver}, " +
                  $"acting {Game.WaitingPlayer}.");
            yield return Shot("battle_people");
            // The first stacks to act whose they are: a shot when the other side's troops are up.
            int firstSide = Game.InBattle ? Game.Battle.PlayerOf(Game.Battle.Current.side) : -1;
            for (int i = 0; i < 16 && Game.InBattle; i++)
            {
                yield return BattleTurn();
                if (Game.InBattle && Game.WaitingPlayer != firstSide && manager.WaitingForHuman)
                {
                    yield return new WaitForSeconds(0.6f);
                    Write($"The other side's troops act: seat {Game.WaitingPlayer}.");
                    yield return Shot("battle_other_side");
                    break;
                }
                yield return Act();
            }
            yield return FightOut(120f, "battle_results");
            yield return Moves(2);
            UI.EndTurn();
            yield return WaitForCurtain(90f);
            yield return Shot("after_battle_curtain");
        }

        /// <summary>One move of the battle by its brain, for whichever side's troops act.</summary>
        private IEnumerator Act()
        {
            yield return BattleTurn();
            if (Game != null && Game.InBattle && manager.WaitingForHuman)
            {
                manager.Submit(BattleAI.Next(Game) ?? GameCommand.BattleDefend(Game.WaitingPlayer, Game.Battle.current));
            }
            yield return new WaitForSeconds(0.2f);
        }

        /// <summary>Waits until the troops whose turn it is in the battle can be given their orders.</summary>
        private IEnumerator BattleTurn()
        {
            float until = Time.unscaledTime + 15f;
            while (Game != null && Game.InBattle && !(manager.WaitingForHuman && !UI.Busy) && Time.unscaledTime < until)
            {
                yield return null;
            }
        }

        /// <summary>Fights the battle on the screen out and closes its results (after a shot of them, when named).</summary>
        private IEnumerator FightOut(float patience, string resultsShot = null)
        {
            float until = Time.unscaledTime + patience;
            while (Game != null && (Game.InBattle || manager.Battle.Running) && Time.unscaledTime < until)
            {
                if (Game.InBattle)
                {
                    yield return Act();
                }
                else if (UI.Bar.Results.IsOpen)
                {
                    yield return new WaitForSeconds(1f);
                    if (resultsShot != null)
                    {
                        yield return Shot(resultsShot);
                        resultsShot = null;
                    }
                    UI.Bar.Results.Close();
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return null;
                }
            }
            yield return WaitForTurn(10f);
        }

        // ------------------------------------------------------------------ Continue, the end, Play Again

        /// <summary>To the title (the game is saved on its way), and Continue takes the hot seat up again behind the hand-over screen.</summary>
        private IEnumerator ContinueSaved()
        {
            if (Game == null)
            {
                yield break;
            }
            TurnCurtain.For(manager).Lift();
            yield return WaitForTurn(30f);
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(2f);
            SavedGame latest = manager.LatestSave;
            Write($"On the title: hot seat {manager.IsHotSeat}, Continue {(latest != null ? $"{latest.Name}: {latest.Summary}" : "nothing")}.");
            yield return Shot("title_continue");
            if (latest == null)
            {
                Write("Nothing to continue.");
                yield break;
            }
            manager.LoadGame();
            yield return WaitForCurtain(30f);
            Write($"Continued: hot seat {manager.IsHotSeat}, curtain {manager.IsHandingOver}, players " +
                  string.Join(", ", Game.State.players.ConvertAll(p => $"{p.name} ({(p.human ? "person" : "computer")})")));
            yield return Shot("continue_curtain");
            yield return Lift("continued");
        }

        /// <summary>
        /// The other realms are knocked out behind the rules' back and the person whose turn it is ends it: the rules
        /// find one realm standing and end the game. The results, then Play Again.
        /// </summary>
        private IEnumerator End()
        {
            if (Game == null)
            {
                yield break;
            }
            int winner = Game.State.currentPlayer;
            foreach (PlayerState player in Game.State.players)
            {
                player.alive = player.index == winner;
            }
            Write($"Only {Game.State.Player(winner)?.name} is left standing.");
            UI.EndTurn();
            float until = Time.unscaledTime + 30f;
            while (Game != null && !UI.Results.IsOpen && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            Write($"Results open {UI.Results.IsOpen}, state {manager.State?.BaseState}.");
            yield return Shot("results");

            manager.PlayHotSeatAgain();
            UI.Results.Close();
            yield return WaitForCurtain(30f);
            yield return Shot("again_curtain");
            manager.ReturnToTitle();
            yield return new WaitForSecondsRealtime(2f);
            Write($"Back on the title: hot seat {manager.IsHotSeat}, title shown {UI.Title.IsShown}.");
            yield return Shot("title_after");
        }

        // ------------------------------------------------------------------ helpers

        private IEnumerator WaitForCurtain(float patience)
        {
            float until = Time.unscaledTime + patience;
            while (Game != null && !Game.IsOver && !manager.IsHandingOver && Time.unscaledTime < until)
            {
                if (Game.State.pending.Count > 0 && manager.WaitingForHuman)
                {
                    manager.Commands.Choose(0);
                }
                if (Game.InBattle && manager.WaitingForHuman || UI.Bar.Results.IsOpen)
                {
                    yield return FightOut(60f);
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.8f);
        }

        private IEnumerator WaitForTurn(float patience)
        {
            float until = Time.unscaledTime + patience;
            yield return new WaitForSeconds(0.3f);
            while (Game != null && !Game.IsOver && (!manager.WaitingForHuman || UI.Busy) && !UI.Bar.Results.IsOpen &&
                   Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
        }

        private int FreeBeside(int cell)
        {
            MapData map = Game.State.map;
            foreach (int next in map.grid.Neighbors(cell))
            {
                if (map.Open(next) && map.occupant[next] < 0 && Game.State.HeroAt(next) == null && Game.State.ObjectAt(next) == null)
                {
                    return next;
                }
            }
            return -1;
        }

        private static string Safe(string name)
        {
            return string.IsNullOrEmpty(name) ? "nobody" : name.Replace(' ', '_');
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string file = Path.Combine(folder, $"{++shot:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(file);
            Write($"shot {file}");
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private void Write(string text)
        {
            log?.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
        }
    }
}
