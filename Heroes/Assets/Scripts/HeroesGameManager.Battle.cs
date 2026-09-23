using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Battles on the screen. A battle a player at this device fights is opened when its BattleStarted event is played
    /// and closed when its BattleEnded event is: on a battlefield of its own, the HeroesBattle scene is loaded next to the
    /// game's scene and the adventure map is put away behind a fade to black while it is up; on the map, the field is
    /// drawn where the armies met and the camera holds still over it. Either way the battle bar takes the place of the
    /// adventure panels, and a dialog tells how the battle went before the map comes back. A battle nobody at this device
    /// fights (computer players among themselves, or online the battles of the others) is told in the log and played out
    /// without being shown. Every way out of a scenario closes a battle that is up.
    /// </summary>
    public partial class HeroesGameManager
    {
        /// <summary>How long the results of a battle wait for the player in an online game before they close themselves.</summary>
        private const float ResultsPatience = 20f;

        /// <summary>
        /// How long they wait on the host while a computer player or the wilds are to move: only the host's client works
        /// those moves out, and it does not while a battle is on its screen, so the whole table waits for this dialog.
        /// </summary>
        private const float ResultsGlance = 3f;

        private BattlefieldScene battlefield;
        private MapBattlefield mapField;
        /// <summary>Whether the battle the director is playing out is on the screen.</summary>
        private bool battleShown;
        private readonly List<Light> dimmed = new List<Light>();

        /// <summary>The field of the battle on the screen, or null.</summary>
        public IBattlefield Field => Battle != null && Battle.Running ? Battle.Field : null;

        /// <summary>Whether the game can be saved now: a scenario at this device, and no battle being fought.</summary>
        public bool CanSave => Game != null && !InSession && !Game.InBattle;

        /// <summary>How fast a battle plays: the battle speed of the settings, and online faster while the rules are ahead.</summary>
        private float BattlePace => Mathf.Max(0.25f, Options != null ? Options.battleSpeed : 1f) * OnlinePace;

        /// <summary>How long the computer seems to think over a move: nothing at all for a battle nobody here watches.</summary>
        private float ThinkDelay => Game != null && Game.InBattle ? battleShown ? 0.3f / BattlePace : 0f : 0.12f / Speed;

        private WaitForSeconds BattleBeat(float seconds)
        {
            return new WaitForSeconds(seconds / BattlePace);
        }

        /// <summary>The battle bar's Options: the pause menu at one device, the match menu of the room online.</summary>
        public void OpenMenu()
        {
            if (IsGameRunning)
            {
                RequestState(Gamebox.BaseGameState.Paused);
            }
        }

        // ------------------------------------------------------------------ the events

        private IEnumerator BattleStarted(GameEvent what)
        {
            Path?.Clear();
            BattleState start = what.battle ?? (Game.InBattle ? Game.Battle.Clone() : null);
            battleShown = false;
            if (start == null)
            {
                yield break;
            }
            if (start.AliveCount(1, true) == 0)
            {
                // Nobody stands against the attacker: the battle is over before it begins (its end comes next).
                yield break;
            }
            if (!ShowsBattle(start))
            {
                if (InSight(start))
                {
                    ui.Log(Sentence($"{SideName(start, 0)} fights {SideName(start, 1)}."), start.attackerPlayer);
                }
                yield break;
            }
            battleShown = true;
            yield return OpenBattle(start);
        }

        private IEnumerator BattleEnded(GameEvent what)
        {
            var result = (BattleResult)what.a;
            BattleState end = what.battle;
            if (!battleShown || !Battle.Running)
            {
                battleShown = false;
                if (end != null && end.stacks.Count > 0 && InSight(end))
                {
                    ui.Log(!string.IsNullOrEmpty(what.text) ? what.text : ResultLine(end, result), what.player);
                }
                Map.Sync();
                ui.Refresh();
                yield break;
            }
            yield return BattleBeat(0.7f);
            int side = ResultSide(end ?? Battle.Shown, result);
            bool won = result == BattleResult.AttackerWon || result == BattleResult.DefenderFled ? side == 0 : side == 1;
            sound?.Play(won ? Sfx.Victory : Sfx.Defeat, 0.8f);
            bool closed = false;
            ui.Bar.ShowResults(what, end ?? Battle.Shown, side, Game.State, () => closed = true);
            // Online the table goes on without anyone: the dialog does not keep this player's turns waiting for long, and
            // gives way after a glance when the others wait for this device (asked again every frame, since the rules
            // run ahead of the screen online).
            float shown = Time.unscaledTime;
            // A dialog taken away some other way (the interface cleared) counts as closed.
            while (!closed && Game != null && Battle.Running && ui.Bar.Results.IsOpen)
            {
                if (IsOnlineGame && Time.unscaledTime > shown + (TableWaitsForThisDevice ? ResultsGlance : ResultsPatience))
                {
                    ui.Bar.Results.Close();
                }
                yield return null;
            }
            yield return CloseBattle();
            ui.Refresh();
        }

        /// <summary>
        /// Whether the battle is shown at this device: online when the player here fights in it (the director only
        /// plays those), at one device when a person, not the computer, leads one of its sides.
        /// </summary>
        private bool ShowsBattle(BattleState start)
        {
            if (IsOnlineGame)
            {
                return IsLocalBattle;
            }
            PlayerState attacker = Game.State.Player(start.attackerPlayer);
            PlayerState defender = Game.State.Player(start.defenderPlayer);
            return attacker != null && attacker.human || defender != null && defender.human;
        }

        /// <summary>
        /// The side the end of a battle is told for (its victory or defeat, its sound): the player's at this device, and
        /// when people lead both sides at this device (a hot seat), the winner's, since one of them has won either way.
        /// </summary>
        private int ResultSide(BattleState battle, BattleResult result)
        {
            if (!IsOnlineGame)
            {
                PlayerState attacker = Game.State.Player(battle.attackerPlayer);
                PlayerState defender = Game.State.Player(battle.defenderPlayer);
                if (attacker != null && attacker.human && defender != null && defender.human)
                {
                    return result == BattleResult.AttackerWon || result == BattleResult.DefenderFled ? 0 : 1;
                }
            }
            return MySide(battle);
        }

        /// <summary>The side the player at this device fights on (the attacker's when both or neither are).</summary>
        private int MySide(BattleState battle)
        {
            if (IsOnlineGame)
            {
                return battle.defenderPlayer == localSeat ? 1 : 0;
            }
            if (battle.attackerPlayer == Viewer)
            {
                return 0;
            }
            if (battle.defenderPlayer == Viewer)
            {
                return 1;
            }
            PlayerState attacker = Game.State.Player(battle.attackerPlayer);
            return attacker != null && attacker.human ? 0 : 1;
        }

        private string SideName(BattleState battle, int side)
        {
            HeroState hero = Game.State.Hero(battle.HeroOf(side));
            if (hero != null)
            {
                return hero.Name;
            }
            TownState town = side == 1 ? Game.State.Town(battle.town) : null;
            if (town != null)
            {
                return town.name;
            }
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side == side && !stack.IsTower)
                {
                    return $"a band of {stack.Def.Plural}";
                }
            }
            return "an army of the wilds";
        }

        private string ResultLine(BattleState end, BattleResult result)
        {
            switch (result)
            {
                case BattleResult.AttackerWon:
                    return Sentence($"{SideName(end, 0)} wins the battle.");
                case BattleResult.DefenderWon:
                    return Sentence($"{SideName(end, 1)} holds the field.");
                case BattleResult.AttackerFled:
                    return Sentence($"{SideName(end, 0)} retreats from the battle.");
                default:
                    return Sentence($"{SideName(end, 1)} retreats from the battle.");
            }
        }

        /// <summary>A line of the log that may begin with "a band of ..." begins with a capital.</summary>
        private static string Sentence(string line)
        {
            return string.IsNullOrEmpty(line) ? line : char.ToUpperInvariant(line[0]) + line.Substring(1);
        }

        // ------------------------------------------------------------------ opening and closing

        /// <summary>Puts a battle on the screen, from the copy of it as it was deployed.</summary>
        private IEnumerator OpenBattle(BattleState start)
        {
            sound?.Play(Sfx.BattleStart);
            sound?.PlayBattleMusic();
            if (start.IsField)
            {
                yield return ui.Bar.Fade(1f, 0.35f);
                BattlefieldScene loaded = null;
                yield return BattlefieldScene.Load(art, made => loaded = made);
                if (Game == null)
                {
                    SceneManager.SetActiveScene(gameObject.scene);
                    loaded.Unload();
                    yield break;
                }
                battlefield = loaded;
                HideAdventure();
                battlefield.Build(start, TownLook(start, out int townColor), townColor);
                // The view first: the bar shows the battle the view holds.
                Battle.Begin(start, battlefield, Game);
                ui.BeginBattle(start);
                ui.ShowAdventureHud(false);
                battlefield.SetBottom(ui.Bar.CoveredBottom());
                battlefield.Intro();
                yield return ui.Bar.Fade(0f, 0.7f);
            }
            else
            {
                mapField = new MapBattlefield(Map, cameraRig, transform);
                // The land around the field as the player here who fights has explored it: in a hot seat the map may
                // still be showing the other player's.
                PlayerState fighter = Game.State.Player(start.PlayerOf(MySide(start)));
                mapField.Open(start, ui.Bar.FreeArea(), fighter != null && fighter.human ? fighter : null);
                Battle.Begin(start, mapField, Game);
                ui.BeginBattle(start);
                ui.ShowAdventureHud(false);
            }
            ui.Bar.Intro(start, Game.State);
            yield return BattleBeat(1.5f);
        }

        /// <summary>Takes the battle off the screen and brings the map back, fading through black from a field of its own.</summary>
        private IEnumerator CloseBattle()
        {
            if (battlefield != null)
            {
                yield return ui.Bar.Fade(1f, 0.4f);
                Battle.End();
                ui.EndBattle(Battle.Shown != null ? Battle.Shown.result : BattleResult.None, Battle.Shown);
                RestoreAdventure(true);
                AsyncOperation unloading = battlefield.Unload();
                battlefield = null;
                while (unloading != null && !unloading.isDone)
                {
                    yield return null;
                }
                sound?.PlayAdventureMusic();
                Map?.Sync();
                yield return ui.Bar.Fade(0f, 0.5f);
            }
            else
            {
                Battle.End();
                mapField?.Close();
                mapField = null;
                ui.EndBattle(BattleResult.None, null);
                ui.ShowAdventureHud(true);
                sound?.PlayAdventureMusic();
                Map?.Sync();
            }
            battleShown = false;
        }

        /// <summary>
        /// Closes a battle on the screen at once, for a scenario that is being left (the title, another scenario, a saved
        /// game, a session that ended): nothing is waited for. <paramref name="keepHud"/> brings the adventure panels
        /// back, for a map that stays up.
        /// </summary>
        private void CloseBattleNow(bool keepHud)
        {
            bool open = Battle != null && Battle.Running || battlefield != null || mapField != null;
            if (Battle != null && Battle.Running)
            {
                Battle.End();
            }
            mapField?.Close();
            mapField = null;
            if (battlefield != null)
            {
                RestoreAdventure(keepHud);
                battlefield.Unload();
                battlefield = null;
            }
            // A field still loading, or one the director was stopped from taking over or unloading, goes too.
            SceneManager.SetActiveScene(gameObject.scene);
            BattlefieldScene.Abandon();
            if (open)
            {
                ui.EndBattle(BattleResult.None, null);
                if (keepHud)
                {
                    ui.ShowAdventureHud(true);
                }
            }
            // The screen may have been left black halfway through the fade into or out of a battle.
            if (ui != null && ui.Bar != null)
            {
                ui.Bar.ClearFade();
            }
            battleShown = false;
        }

        /// <summary>
        /// Puts the adventure map away while a field of its own is up: the map, its fog, the camera over it (its
        /// listener stays: every sound of the game is heard through it) and the light of the game's scene.
        /// </summary>
        private void HideAdventure()
        {
            if (Map != null)
            {
                Map.Fog.Visible = false;
                Map.gameObject.SetActive(false);
            }
            if (cameraRig != null)
            {
                cameraRig.Locked = true;
                if (cameraRig.View != null)
                {
                    cameraRig.View.enabled = false;
                }
            }
            dimmed.Clear();
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type == LightType.Directional && light.enabled && light.gameObject.scene == gameObject.scene)
                {
                    light.enabled = false;
                    dimmed.Add(light);
                }
            }
        }

        private void RestoreAdventure(bool keepHud)
        {
            SceneManager.SetActiveScene(gameObject.scene);
            foreach (Light light in dimmed)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }
            dimmed.Clear();
            if (cameraRig != null)
            {
                cameraRig.Locked = false;
                if (cameraRig.View != null)
                {
                    cameraRig.View.enabled = true;
                }
            }
            if (Map != null)
            {
                Map.gameObject.SetActive(true);
                Map.Fog.Visible = true;
                Map.Fog.MarkDirty();
            }
            if (Effects != null && cameraRig != null)
            {
                Effects.View = cameraRig.View;
            }
            if (keepHud)
            {
                ui.ShowAdventureHud(true);
            }
        }

        /// <summary>
        /// A besieged town stands behind its wall, in the colors of whoever holds it (<paramref name="color"/>, 4 for
        /// nobody), which its gatehouse flies too.
        /// </summary>
        private GameObject TownLook(BattleState start, out int color)
        {
            TownState town = start.town >= 0 ? Game.State.Town(start.town) : null;
            PlayerState owner = town != null ? Game.State.Player(town.owner) : null;
            color = owner != null ? (int)owner.color : 4;
            return town != null && art != null ? art.Town(town.faction, color) : null;
        }

        /// <summary>A saved game that stopped in the middle of a battle goes on with the battle on the screen.</summary>
        private IEnumerator ResumeBattle()
        {
            if (Game == null || !Game.InBattle || Battle.Running)
            {
                yield break;
            }
            BattleState start = Game.Battle.Clone();
            battleShown = ShowsBattle(start);
            if (battleShown)
            {
                yield return OpenBattle(start);
                ui.SetBattleRound(start.round);
            }
        }
    }
}
