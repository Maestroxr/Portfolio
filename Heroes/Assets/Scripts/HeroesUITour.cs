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
    /// interface, a few moves, the log, the hero's way home into his town, his book, the town with its market and
    /// tavern, the questions the rules ask, the next day, the pause menu, the results of a game, the online lobby and
    /// the title it goes back to. To have
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
            yield return Homecoming();
            yield return Screens();
            yield return Meetings();
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
            yield return OtherLook();
            Write("Tour over.");
            yield return new WaitForSeconds(0.5f);
            log?.Flush();
            Application.Quit();
        }

        /// <summary>
        /// Steps to the next look the game lists while the title shows, as the settings row does (not remembered), shoots
        /// the title redrawn in it without a reload, and goes back to the look the tour started with.
        /// </summary>
        private IEnumerator OtherLook()
        {
            if (GameThemes.Available(GameType.Heroes).Count < 2)
            {
                Write("One look only: no theme switch.");
                yield break;
            }
            GameTheme before = GameThemes.Active(GameType.Heroes);
            GameTheme other = GameThemes.SelectNext(GameType.Heroes, 1, false);
            yield return new WaitForSecondsRealtime(1.5f);
            Write($"Look switched from {before?.DisplayName} to {other?.DisplayName}: title shown {UI.Title.IsShown}, " +
                  $"art {manager.Art?.name}, body font {UIKit.Art?.bodyFont?.name}.");
            yield return Shot("title_other_look");
            GameThemes.Select(GameType.Heroes, before, false);
            yield return new WaitForSecondsRealtime(1f);
            Write($"Look back to {GameThemes.Active(GameType.Heroes)?.DisplayName}: body font {UIKit.Art?.bodyFont?.name}.");
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
        private IEnumerator HoverCell(int cell, string name, bool click = false)
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
            if (click)
            {
                UI.TourClick = true;
                yield return null;
            }
            UI.TourPointer = null;
            TooltipBox.Pointer = null;
            yield return null;
        }

        /// <summary>
        /// The hero goes home: over his town the pointer says a click sends him in, and the click walks him into its
        /// gate and opens the town with him in it. He is given the movement for the way behind the rules' back.
        /// </summary>
        private IEnumerator Homecoming()
        {
            HeroState hero = FirstHero();
            PlayerState me = manager.ViewerState;
            TownState town = me != null && me.towns.Count > 0 ? Game.State.Town(me.towns[0]) : null;
            if (hero == null || town == null || hero.cell == town.cell)
            {
                Write("No way home to show: no hero, no town, or the hero never left it.");
                yield break;
            }
            hero.movement = Mathf.Max(hero.movement, hero.maxMovement);
            // The camera glides to the hero in hand: the pointer is placed once it has come to rest.
            manager.Select(hero);
            yield return new WaitForSeconds(2f);
            yield return HoverCell(town.center, "hover_own_town", true);
            yield return WaitForTurn(20f);
            yield return new WaitForSeconds(0.6f);
            HeroState visitor = Game.State.HeroAt(town.cell);
            Write($"A click on {town.name}: town open {UI.Town.IsOpen}, visitor {(visitor != null ? visitor.Name : "none")}.");
            yield return Shot("town_entered");
            UI.Town.Close();
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

        // ------------------------------------------------------------------ meetings, splits, dwellings, a turned camera

        /// <summary>
        /// A second hero of the realm is set beside the first behind the rules' back, and the first walks up to him: the
        /// screen of their meeting opens, a stack is split and an artifact handed over through it. Then the first hero is
        /// set beside a dwelling with an empty treasury, and visits it: its window opens all the same. Last, the camera
        /// turns around the hero.
        /// </summary>
        private IEnumerator Meetings()
        {
            HeroState hero = FirstHero();
            PlayerState me = manager.ViewerState;
            if (hero == null || me == null || Game.State.freeHeroes.Count == 0)
            {
                Write("No meeting to show.");
                yield break;
            }
            HexGrid grid = Game.State.map.grid;
            int beside = FreeBeside(hero.cell);
            if (beside < 0)
            {
                Write("No room beside the hero for another.");
                yield break;
            }
            HeroState other = Game.SpawnHero(me.index, Game.State.freeHeroes[0], beside, true);
            manager.Map.Sync();
            hero.movement = Mathf.Max(hero.movement, hero.maxMovement);
            manager.Select(hero);
            yield return new WaitForSeconds(1f);
            manager.Commands.MoveHero(hero.id, other.cell);
            yield return WaitForTurn(10f);
            yield return new WaitForSeconds(0.6f);
            Write($"{hero.Name} walked up to {other.Name}: meeting open {UI.Meeting.IsOpen}.");
            yield return Shot("meeting");

            if (UI.Split.Show(Holder.Hero(hero.id), 0, Holder.Hero(other.id), 6))
            {
                yield return new WaitForSeconds(0.5f);
                yield return Shot("split");
                int before = other.army.TotalCreatures;
                UI.Split.GetComponentInChildren<UnityEngine.UI.Slider>().value = 2;
                foreach (UnityEngine.UI.Button button in UI.Split.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    if (button.name == "Split")
                    {
                        button.onClick.Invoke();
                    }
                }
                yield return WaitForTurn(5f);
                Write($"Split two over: {other.Name} had {before}, has {other.army.TotalCreatures}.");
            }
            else
            {
                Write("The split box would not open.");
            }
            int artifact = Array.Find(hero.equipped, id => id >= 0);
            if (artifact >= 0 || hero.backpack.Count > 0)
            {
                bool worn = artifact >= 0;
                int given = worn ? artifact : hero.backpack[0];
                manager.Commands.GiveArtifact(hero.id, given, other.id, worn);
                yield return WaitForTurn(5f);
                Write($"Handed {(ArtifactId)given} over: {other.Name} has it {Array.IndexOf(other.equipped, given) >= 0 || other.backpack.Contains(given)}.");
            }
            UI.Refresh();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("meeting_traded");
            UI.Meeting.Close();

            MapObject dwelling = null;
            int closest = int.MaxValue;
            int dwellings = 0;
            foreach (MapObject what in Game.State.objects)
            {
                if (what.removed || what.kind != ObjectKind.Dwelling)
                {
                    continue;
                }
                dwellings++;
                if (grid.Distance(hero.cell, what.cell) < closest && FreeBeside(what.cell) >= 0)
                {
                    dwelling = what;
                    closest = grid.Distance(hero.cell, what.cell);
                }
            }
            if (dwelling == null && FreeBeside(hero.cell) >= 0)
            {
                // None on this map: one is put up beside the hero, as the generator puts them up on others.
                int spot = FreeBeside(hero.cell);
                CreatureId creature = Creatures.OfTier(Faction.Castle, 2);
                dwelling = new MapObject
                {
                    id = Game.State.objects.Count, kind = ObjectKind.Dwelling, cell = spot, subtype = (int)creature, amount = Creatures.Get(creature).Growth
                };
                dwelling.footprint.Add(spot);
                Game.State.objects.Add(dwelling);
                grid = Game.State.map.grid;
                Game.State.map.occupant[spot] = dwelling.id;
                manager.Map.Sync();
                Write($"No dwelling on the map ({dwellings}): one put up beside {hero.Name}.");
            }
            if (dwelling == null)
            {
                Write($"No dwelling to visit ({dwellings} on the map).");
            }
            else
            {
                int stand = grid.Distance(hero.cell, dwelling.cell) == 1 ? hero.cell : FreeBeside(dwelling.cell);
                // Its guards (if any) are left out of it: the dwelling is the realm's already.
                dwelling.owner = me.index;
                hero.cell = stand;
                manager.Map.Warp(hero.id, stand);
                manager.Rig?.Snap(manager.Map.Point(stand));
                int gold = me.resources[ResourceKind.Gold];
                me.resources[ResourceKind.Gold] = 0;
                hero.movement = Mathf.Max(hero.movement, hero.maxMovement);
                manager.Commands.MoveHero(hero.id, dwelling.cell);
                yield return WaitForTurn(10f);
                yield return new WaitForSeconds(0.6f);
                Write($"{hero.Name} visited the dwelling of {(CreatureId)dwelling.subtype} with no gold: window open {UI.Dwelling.IsOpen}.");
                yield return Shot("dwelling_no_gold");
                me.resources[ResourceKind.Gold] = gold + 5000;
                UI.Refresh();
                yield return new WaitForSeconds(0.4f);
                yield return Shot("dwelling");
                UI.Dwelling.Close();
            }

            manager.Rig?.Snap(manager.Map.Point(hero.cell));
            yield return new WaitForSeconds(0.5f);
            yield return Shot("camera_north");
            manager.Rig?.TurnBy(120f);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("camera_turned");
            manager.Rig?.TurnBy(-120f);
        }

        /// <summary>A free cell of open land beside <paramref name="cell"/>, or -1.</summary>
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
