using System;
using System.Collections;
using System.IO;
using Gamebox;
using Gamebox.Online;
using Portfolio.Heroes.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Development players only. Started with <c>-heroes-ui-tour &lt;folder&gt;</c>, it walks through the screens of the
    /// game outside its battles and saves a screenshot of each into the folder, then quits; without the argument it
    /// does nothing. It looks at the title, the credits and the settings, the campaign and the skirmish maps, then
    /// starts the first chapter and looks at the adventure screen, what the pointer shows over the map and the
    /// interface, a few moves, the log, the hero's book, the town with its market and tavern, the questions the rules
    /// ask, the next day, the pause menu, the results of a game, the online lobby and the title it goes back to. To have
    /// something to show it gives the hero skills, spells and artifacts and the town a few buildings, behind the rules'
    /// back; the questions and the results are shown without being asked.
    /// </summary>
    public class HeroesUITour : MonoBehaviour
    {
        private string folder;
        private HeroesGameManager manager;
        private StreamWriter log;
        private int shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue("-heroes-ui-tour");
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<HeroesUITour>() != null)
            {
                return;
            }
            var host = new GameObject("Heroes UI Tour");
            DontDestroyOnLoad(host);
            host.AddComponent<HeroesUITour>().folder = target;
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
            Write($"Screen {Screen.width}x{Screen.height}.");
            yield return new WaitForSeconds(3f);
            // The pointer of the desktop may rest at an edge of the window: the map must not scroll away under it. The
            // tour changes a copy of the settings in use (the manager has picked them by now), never the defaults or
            // the player's own saved settings.
            HeroesSettings own = Instantiate(manager.Options);
            own.name = "Heroes UI Tour Settings";
            own.edgeScroll = false;
            manager.Settings = own;
            yield return Title();
            yield return Adventure();
            yield return Screens();
            yield return Questions();
            yield return Menus();
            yield return Lobby();

            // Back to the title the way a player goes: the pause menu's Leave to the Title.
            UI.OpenMenu();
            yield return new WaitForSecondsRealtime(0.5f);
            GameObject leave = GameObject.Find("ExitToTitle");
            if (leave != null && leave.TryGetComponent(out UnityEngine.UI.Button button))
            {
                button.onClick.Invoke();
            }
            else
            {
                Write("No Leave to the Title in the pause menu.");
                manager.ReturnToTitle();
            }
            yield return new WaitForSecondsRealtime(2.5f);
            Write($"Back on the title: HUD shown {UI.Hud.IsShown}, title shown {UI.Title.IsShown}.");
            yield return Shot("title_back");
            Write("Tour over.");
            yield return new WaitForSeconds(0.5f);
            log?.Flush();
            Application.Quit();
        }

        // ------------------------------------------------------------------ before a game

        private IEnumerator Title()
        {
            yield return Shot("title");
            if (!string.IsNullOrEmpty(MobilePlatform.ArgumentValue("-heroes-ui-tour-views")) && UI.Title.Diorama != null)
            {
                // The ways the camera can look at the valley behind the title, to choose from.
                for (int view = 0; view < TitleDiorama.ViewCount; view++)
                {
                    UI.Title.Diorama.View(view);
                    yield return new WaitForSecondsRealtime(0.4f);
                    yield return Shot($"title_view{view}");
                }
                UI.Title.Diorama.View(0);
            }
            Hover(UI.Title.transform, "Campaign");
            yield return new WaitForSeconds(0.7f);
            yield return Shot("title_hover");
            Unhover();

            UI.OpenCredits();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("credits");
            UI.Message.Close();

            UI.ShowSettings();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("settings_title");
            UI.HideSettings();

            UI.OpenCampaign(false);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("campaign_chapters");
            UI.Campaign.Show(true);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("campaign_skirmish");
            UI.Campaign.Close();
            yield return new WaitForSeconds(0.4f);
            Write($"After the campaign's Back: title shown {UI.Title.IsShown}.");
        }

        // ------------------------------------------------------------------ the adventure map

        private IEnumerator Adventure()
        {
            manager.BeginScenario(null, 0);
            float until = Time.unscaledTime + 40f;
            while (Game == null && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return WaitForTurn(30f);
            if (Game == null)
            {
                Write("The scenario never started.");
                yield break;
            }
            Write($"Scenario started: battles {Game.State.rules.battleStyle}, map {Game.State.map.grid.columns}x{Game.State.map.grid.rows}.");
            yield return new WaitForSeconds(0.6f);
            yield return Shot("adventure_start");
            yield return new WaitForSeconds(2.2f);
            yield return Shot("adventure_settled");

            HeroState hero = FirstHero();
            MapObject monster = Nearest(hero, ObjectKind.Monster);
            if (monster != null)
            {
                yield return HoverCell(monster.cell, "hover_monster");
            }
            MapObject town = Nearest(hero, ObjectKind.Town);
            if (town != null)
            {
                yield return HoverCell(town.cell, "hover_town");
            }
            HeroButton card = FindAnyObjectByType<HeroButton>();
            if (card != null)
            {
                Hover(card.transform, null);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("hover_hero_card");
                Unhover();
            }

            // A few moves: to the treasure nearest the hero, answering what it asks.
            for (int step = 0; step < 3 && hero != null && Game != null; step++)
            {
                MapObject pickup = NearestPickup(hero);
                if (pickup == null)
                {
                    break;
                }
                Write($"{hero.Name} goes for {pickup.kind} at {pickup.cell}.");
                manager.Commands.MoveHero(hero.id, pickup.cell);
                yield return WaitForTurn(20f);
                if (Game.State.pending.Count > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot($"choice_real_{step}");
                    manager.Commands.Choose(0);
                    UI.Choice.Close();
                    yield return WaitForTurn(10f);
                }
                hero = Game.State.Hero(hero.id);
            }
            yield return new WaitForSeconds(0.6f);
            yield return Shot("adventure_moved");

            UI.Hud.SetLogOpen(true);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("log_open");
            UI.Hud.SetLogOpen(false);
        }

        /// <summary>Points at a cell of the map, as the mouse would, and takes the tooltip it shows.</summary>
        private IEnumerator HoverCell(int cell, string name)
        {
            Camera view = manager.Rig != null ? manager.Rig.View : Camera.main;
            if (view == null || manager.Map == null)
            {
                yield break;
            }
            Vector3 at = view.WorldToScreenPoint(manager.Map.Point(cell) + Vector3.up * 0.3f);
            if (at.z <= 0f || at.x < 0f || at.y < 0f || at.x > Screen.width || at.y > Screen.height)
            {
                manager.Rig?.Snap(manager.Map.Point(cell));
                yield return new WaitForSeconds(0.4f);
                at = view.WorldToScreenPoint(manager.Map.Point(cell) + Vector3.up * 0.3f);
            }
            UI.TourPointer = new Vector3(at.x, at.y, 0f);
            TooltipBox.Pointer = new Vector2(at.x, at.y);
            yield return new WaitForSeconds(0.9f);
            yield return Shot(name);
            UI.TourPointer = null;
            TooltipBox.Pointer = null;
            yield return null;
        }

        // ------------------------------------------------------------------ the hero and the town

        private IEnumerator Screens()
        {
            HeroState hero = FirstHero();
            if (hero == null)
            {
                yield break;
            }
            Outfit(hero);
            manager.Select(hero);
            UI.OpenHero();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("hero");
            UI.Sheet.Close();

            PlayerState me = manager.ViewerState;
            TownState town = me != null && me.towns.Count > 0 ? Game.State.Town(me.towns[0]) : null;
            if (town == null)
            {
                Write("No town to look at.");
                yield break;
            }
            UI.OpenTown();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("town");
            Furnish(town);
            UI.Refresh();
            yield return new WaitForSeconds(0.4f);
            yield return Shot("town_furnished");

            UI.Town.OpenMarket();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("market");
            UI.Town.HandleBack();
            UI.Town.OpenTavern();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("tavern");
            UI.Town.HandleBack();
            UI.Town.Close();
        }

        /// <summary>Skills, spells and artifacts for the hero, so his book has something in every part.</summary>
        private void Outfit(HeroState hero)
        {
            foreach ((SkillId skill, int level) in new[] { (SkillId.Logistics, 2), (SkillId.Wisdom, 1), (SkillId.Offense, 3) })
            {
                if (hero.SkillLevel(skill) == 0 && hero.skills.Count < HeroData.MaxSkills)
                {
                    hero.skills.Add(new SkillEntry { skill = (int)skill, level = level });
                }
            }
            foreach (SpellId spell in new[] { SpellId.MagicArrow, SpellId.Bless, SpellId.Haste, SpellId.Cure, SpellId.LightningBolt, SpellId.Fireball, SpellId.StoneSkin })
            {
                if (!hero.Knows(spell))
                {
                    hero.spells.Add((int)spell);
                }
            }
            int worn = 0;
            int carried = 0;
            foreach (ArtifactDef def in Artifacts.All)
            {
                int slot = (int)def.Slot;
                if (worn < 4 && slot >= 0 && slot < hero.equipped.Length && hero.equipped[slot] < 0)
                {
                    hero.equipped[slot] = (int)def.Id;
                    worn++;
                }
                else if (carried < 3 && !hero.backpack.Contains((int)def.Id))
                {
                    hero.backpack.Add((int)def.Id);
                    carried++;
                }
            }
        }

        /// <summary>A tavern, a market, a guild and the first dwellings for the town, with creatures waiting in them.</summary>
        private static void Furnish(TownState town)
        {
            foreach (BuildingId building in new[] { BuildingId.Fort, BuildingId.Tavern, BuildingId.Marketplace, BuildingId.MageGuild1,
                         BuildingId.Dwelling1, BuildingId.Dwelling2, BuildingId.Dwelling3 })
            {
                if (!town.Has(building))
                {
                    town.built.Add((int)building);
                }
            }
            for (int tier = 1; tier <= 3; tier++)
            {
                town.available[tier - 1] = Mathf.Max(town.available[tier - 1], 16 / tier);
            }
        }

        // ------------------------------------------------------------------ questions, a new day, menus, the end

        private IEnumerator Questions()
        {
            HeroState hero = FirstHero();
            if (hero == null)
            {
                yield break;
            }
            UI.ShowChoice(new PendingChoice { kind = ChoiceKind.Treasure, player = hero.owner, hero = hero.id, options = new[] { 1500, 1000 } });
            yield return new WaitForSeconds(0.6f);
            yield return Shot("choice_treasure");
            UI.Choice.Close();
            UI.ShowChoice(new PendingChoice
            {
                kind = ChoiceKind.LevelUp, player = hero.owner, hero = hero.id, stat = (int)PrimaryStat.Power,
                options = new[] { (int)SkillId.Wisdom, (int)SkillId.Scouting }
            });
            yield return new WaitForSeconds(0.6f);
            yield return Shot("choice_levelup");
            UI.Choice.Close();

            int day = Game.State.day;
            UI.EndTurn();
            float until = Time.unscaledTime + 60f;
            while (Game != null && (Game.State.day == day || !manager.WaitingForHuman) && Time.unscaledTime < until)
            {
                if (Game.State.pending.Count > 0 && manager.WaitingForHuman)
                {
                    manager.Commands.Choose(0);
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            yield return Shot("next_day");
            yield return new WaitForSeconds(2.5f);
        }

        private IEnumerator Menus()
        {
            // The pause menu stops the game's clock: the tour waits in real time.
            UI.OpenMenu();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("pause");
            UI.ShowSettings();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("settings_pause");
            UI.HideSettings();
            manager.TransitionState(new Gamebox.GameState(BaseGameState.Running));
            yield return new WaitForSecondsRealtime(0.5f);

            UI.ShowEnd(true, 2, 34);
            yield return new WaitForSeconds(1.6f);
            yield return Shot("results");
            UI.Results.Close();
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Lobby()
        {
            var lobby = FindAnyObjectByType<OnlineLobbyUI>(FindObjectsInactive.Include);
            if (lobby == null)
            {
                Write("No lobby in the scene.");
                yield break;
            }
            if (string.IsNullOrEmpty(MobilePlatform.ArgumentValue("-gamebox-server")))
            {
                // The lobby connects as it opens: a tour only ever talks to a server of its own (none running is fine).
                Write("The lobby is left out: no -gamebox-server given.");
                yield break;
            }
            manager.OpenOnline();
            yield return new WaitForSeconds(2.5f);
            yield return Shot("lobby");

            // The form of a new room holds more options than it shows at once: a bar beside it tells there are more.
            GameObject open = GameObject.Find("Open a room");
            if (open != null && open.TryGetComponent(out UnityEngine.UI.Button create))
            {
                create.onClick.Invoke();
                yield return new WaitForSeconds(0.8f);
                yield return Shot("lobby_create");
                GameObject form = GameObject.Find("FormArea");
                var bar = form != null ? form.GetComponentInChildren<UnityEngine.UI.Scrollbar>(true) : null;
                Write($"Open a room: the form's scroll bar is {(bar == null ? "missing" : bar.gameObject.activeInHierarchy ? "shown" : "hidden")}.");
            }
            else
            {
                Write("No Open a room button in the lobby.");
            }
            lobby.Hide();
            yield return new WaitForSeconds(0.3f);
        }

        // ------------------------------------------------------------------ plumbing

        private HeroState FirstHero()
        {
            if (Game == null)
            {
                return null;
            }
            HeroState hero = manager.Selected;
            if (hero == null && manager.ViewerState != null && manager.ViewerState.heroes.Count > 0)
            {
                hero = Game.State.Hero(manager.ViewerState.heroes[0]);
            }
            return hero;
        }

        private MapObject Nearest(HeroState hero, ObjectKind kind)
        {
            if (hero == null)
            {
                return null;
            }
            MapObject best = null;
            int closest = int.MaxValue;
            foreach (MapObject what in Game.State.objects)
            {
                if (what.removed || what.kind != kind || !manager.ViewerState.Explored(what.cell))
                {
                    continue;
                }
                int distance = Game.State.map.grid.Distance(hero.cell, what.cell);
                if (distance > 0 && distance < closest)
                {
                    closest = distance;
                    best = what;
                }
            }
            return best;
        }

        /// <summary>The nearest thing to pick up that the hero reaches today without a fight.</summary>
        private MapObject NearestPickup(HeroState hero)
        {
            MapObject best = null;
            int closest = int.MaxValue;
            foreach (MapObject what in Game.State.objects)
            {
                if (what.removed || !MapObjects.IsPickup(what.kind) || Game.GuardOf(what.cell) != null)
                {
                    continue;
                }
                MovePlan plan = manager.PlanFor(hero, what.cell);
                if (plan == null || plan.Cells.Count == 0 || plan.Today < plan.Cells.Count)
                {
                    continue;
                }
                if (plan.Cells.Count < closest)
                {
                    closest = plan.Cells.Count;
                    best = what;
                }
            }
            return best;
        }

        /// <summary>The pointer enters a part of the interface (found by name under <paramref name="root"/> when given).</summary>
        private void Hover(Transform root, string name)
        {
            Transform target = string.IsNullOrEmpty(name) ? root : Find(root, name);
            if (target == null)
            {
                Write($"Nothing called {name} to point at.");
                return;
            }
            Vector3 at = target.position;
            TooltipBox.Pointer = new Vector2(at.x, at.y);
            ExecuteEvents.Execute(target.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            hovered = target.gameObject;
        }

        private GameObject hovered;

        private void Unhover()
        {
            if (hovered != null)
            {
                ExecuteEvents.Execute(hovered, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            }
            hovered = null;
            TooltipBox.Pointer = null;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }

        private IEnumerator WaitForTurn(float patience)
        {
            float until = Time.unscaledTime + patience;
            yield return new WaitForSeconds(0.3f);
            while (Game != null && !Game.IsOver && (!manager.WaitingForHuman || UI.Busy) && Time.unscaledTime < until)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
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
