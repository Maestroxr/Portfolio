using System.Collections;
using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// Play tests of the strike mode's flow: a strike mission starts over the scrolling ground, the ship stays on screen,
    /// money, the win (the fly-off lands the ship and saves the pilot), the loss (the working copy goes), the Supply Room
    /// and the tabs of the mission select, and the asteroid field afterwards. When the campaign has no strike mission yet
    /// the tests fly one of their own (made from the first field mission). Tests that need the strike units and bosses of
    /// the content are in the category "Content" and are ignored while the scene has none.
    /// </summary>
    public class StrikeTest
    {
        private TemporaryObjects temporary;
        private Campaign originalCampaign;


        [SetUp]
        public void SetUp()
        {
            temporary = new TemporaryObjects();
        }


        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            AsteroidsGameManager manager = TestScenes.Manager;
            if (manager != null && originalCampaign != null)
            {
                // The scene runs on until the next test loads its own: give it its campaign back before the copy goes.
                manager.ReturnToMissionSelect();
                manager.CampaignAsset = originalCampaign;
                manager.ReturnToMissionSelect();
                yield return null;
            }
            originalCampaign = null;
            temporary.Dispose();
        }


        [UnityTest]
        public IEnumerator StrikeMissionStartsAndTheGroundScrolls()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            Assert.IsTrue(manager.IsStrike, "A strike mission is flown.");
            Assert.IsFalse(manager.Playground.Wraps, "The strike playfield has edges.");
            Assert.IsTrue(manager.Playground.IsFixed, "The strike playfield has a fixed size.");
            Assert.That(manager.Playground.HalfSize.x, Is.EqualTo(StrikeRules.HalfSize.x).Within(0.01f));
            Assert.That(manager.Lives, Is.EqualTo(1), "A strike pilot has one ship.");
            Assert.NotNull(manager.Working, "The mission flies a working copy of the pilot.");
            Assert.That(manager.Ship.Mode, Is.EqualTo(MissionMode.Strike));
            Assert.IsNull(manager.Director, "No wave director in strike.");
            float start = manager.ScrollDistance;
            yield return new WaitForSeconds(1.5f);
            float scrolled = manager.ScrollDistance - start;
            Assert.That(scrolled, Is.GreaterThan(2.4f), "The ground scrolls at about 2.4 m/s.");
            Assert.That(scrolled, Is.LessThan(5f), "The ground scrolls at about 2.4 m/s.");
            Assert.That(manager.ScrollProgress, Is.GreaterThan(0f));
            StrikeTerrain terrain = manager.Field.Terrain;
            if (terrain != null && manager.Mission is StrikeLevel level && level.Terrain != null)
            {
                Assert.IsTrue(terrain.IsShown, "The terrain shows.");
                Assert.Greater(terrain.TilesShown, 0, "Tiles cover the view.");
                Assert.That(terrain.Distance, Is.EqualTo(manager.ScrollDistance).Within(0.01f), "The tiles follow the scroll.");
            }
        }


        [UnityTest]
        public IEnumerator ShipCannotLeaveTheScreen()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsPlayer ship = manager.Ship;
            var input = new HeldInput { Move = new Vector2(1f, 1f) };
            ship.Input = input;
            yield return new WaitForSeconds(2f);
            Vector2 half = manager.Playground.HalfSize;
            Assert.That(ship.Position.x, Is.LessThanOrEqualTo(half.x + 0.01f), "The ship stops at the right edge.");
            Assert.That(ship.Position.y, Is.LessThanOrEqualTo(half.y + 0.01f), "The ship stops at the top edge.");
            Assert.That(ship.Position.x, Is.GreaterThan(half.x - 3f), "The ship flew all the way right.");
            input.Move = new Vector2(-1f, -1f);
            yield return new WaitForSeconds(2.5f);
            Assert.That(ship.Position.x, Is.GreaterThanOrEqualTo(-half.x - 0.01f), "No wrapping to the other side.");
            Assert.That(ship.Position.y, Is.GreaterThanOrEqualTo(-half.y - 0.01f), "The ship stops at the bottom edge.");
            Assert.That(ship.Position.x, Is.LessThan(-half.x + 3f), "The ship flew all the way left.");
            ship.Input = null;
        }


        [UnityTest]
        public IEnumerator WinFliesOffAndSavesThePilot()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            int wallet = manager.Pilot.Money;
            manager.AddMoney(5000, Vector2.zero);
            Assert.That(manager.Scoring.Score, Is.EqualTo(5000), "Mission money is the score.");
            Assert.That(manager.Pilot.Money, Is.EqualTo(wallet), "The saved pilot is paid only when the ship lands.");
            manager.Objective.Defeat();
            yield return WaitFor(() => manager.State.Is(BaseGameState.Victory), 10f, "the results of the won mission");
            Assert.IsTrue(manager.HasLanded, "The ship flew off the top and landed.");
            Assert.That(manager.Pilot.Money, Is.EqualTo(wallet + 5000), "The money of the mission is in the wallet.");
            Assert.That(manager.Progress.LoadPilot().Money, Is.EqualTo(wallet + 5000), "The pilot was saved.");
            Assert.That(manager.Progress.Stars(manager.LevelIndex), Is.GreaterThanOrEqualTo(1), "A won mission earns a star.");
            Assert.IsNull(manager.Working, "The working copy is gone.");
        }


        [UnityTest]
        public IEnumerator EnergyZeroFailsAndDiscardsTheWorkingCopy()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsPlayer ship = manager.Ship;
            int wallet = manager.Pilot.Money;
            float energy = manager.Pilot.Energy;
            manager.AddMoney(2500, Vector2.zero);
            yield return TestScenes.WaitForInvulnerability(ship);
            ship.TakeDamage(new DamageInfo(1000f, Vector2.down, ship.Position, DamageSource.Hazard, false));
            Assert.IsFalse(ship.IsAlive, "No energy left: the ship is destroyed.");
            yield return WaitFor(() => manager.State.Is(BaseGameState.GameOver), 6f, "the failed mission's results");
            Assert.IsNull(manager.Working, "The working copy was discarded.");
            Assert.That(manager.Pilot.Money, Is.EqualTo(wallet), "A failed mission pays nothing.");
            Assert.That(manager.Pilot.Energy, Is.EqualTo(energy).Within(0.01f), "The saved pilot keeps its energy.");
            Assert.IsFalse(manager.HasLanded);
        }


        [UnityTest]
        public IEnumerator FieldModeStillWorksAfterAStrikeMission()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            manager.ReturnToMissionSelect();
            yield return null;
            Assert.IsTrue(manager.Playground.Wraps, "The menu wraps again.");
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(0));
            yield return WaitFor(() => manager.IsMissionActive, 6f, "the field mission");
            Assert.IsFalse(manager.IsStrike);
            Assert.IsTrue(manager.Playground.Wraps, "The field wraps.");
            Assert.IsFalse(manager.Playground.IsFixed, "The field fits the camera again.");
            Assert.NotNull(manager.Director, "The waves run again.");
            Assert.That(manager.Field.ScrollSpeed, Is.EqualTo(0f));
            Assert.That(manager.Ship.Mode, Is.EqualTo(MissionMode.Field));
            Assert.That(manager.Lives, Is.GreaterThan(1), "The field has its lives back.");
            Assert.IsTrue(manager.Field.Terrain == null || !manager.Field.Terrain.IsShown, "No ground under the asteroids.");
            Assert.IsTrue(manager.backdrop == null || manager.backdrop.IsVisible, "The space backdrop is back.");
        }


        [UnityTest]
        public IEnumerator SupplyRoomBuysAndSellsForTheSavedPilot()
        {
            yield return LoadWithStrikeMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            manager.SelectMode(MissionMode.Strike);
            yield return null;
            Assert.That(manager.MenuMode, Is.EqualTo(MissionMode.Strike), "The strike tab is shown.");
            Assert.IsTrue(manager.IsStrike, "A strike mission is selected.");
            if (manager.StrikeScreens != null)
            {
                Assert.IsTrue(manager.StrikeScreens.missionSelect.gameObject.activeInHierarchy, "The strike tab shows its map.");
            }
            manager.Pilot.Money = 200000;
            manager.OpenSupply();
            yield return null;
            if (manager.StrikeScreens != null)
            {
                Assert.IsTrue(manager.StrikeScreens.IsSupplyOpen, "The Supply Room opened.");
            }
            int price = StrikeArmory.Price(StrikeItem.AirMissiles);
            manager.BuyItem(StrikeItem.AirMissiles);
            Assert.That(manager.Pilot.Count(StrikeItem.AirMissiles), Is.EqualTo(1), "Bought.");
            Assert.That(manager.Pilot.Money, Is.EqualTo(200000 - price));
            Assert.That(manager.Progress.LoadPilot().Count(StrikeItem.AirMissiles), Is.EqualTo(1), "The purchase was saved.");
            manager.SellItem(StrikeItem.AirMissiles);
            Assert.That(manager.Pilot.Count(StrikeItem.AirMissiles), Is.EqualTo(0), "Sold.");
            Assert.That(manager.Pilot.Money, Is.EqualTo(200000 - price + price / 2), "Sold for half the price.");
            manager.CloseSupply();
            yield return null;
            if (manager.StrikeScreens != null)
            {
                Assert.IsFalse(manager.StrikeScreens.IsSupplyOpen, "The Supply Room closed.");
            }
            manager.SelectMode(MissionMode.Field);
            yield return null;
            Assert.IsFalse(manager.IsStrike, "The field tab selects a field mission.");
            Assert.IsTrue(manager.backdrop == null || manager.backdrop.IsVisible, "The field tab shows space.");
        }


        [UnityTest, Category("Content")]
        public IEnumerator AirShotKillsAnAirUnit()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            StrikeAircraft dart = SpawnAir(manager, new Vector2(0f, 4f));
            if (dart == null)
            {
                Assert.Ignore("The scene has no strike aircraft yet.");
            }
            dart.Velocity = Vector2.zero;
            Shot shot = manager.spawner.FireStrikeShot(StrikeShotKind.PlasmaBolt, new Vector2(0f, 1f), Vector2.up * 20f, 500f, Altitude.Air, manager.Ship);
            Assert.NotNull(shot, "The plasma bolt was fired.");
            yield return WaitFor(() => !dart.IsAlive, 1.5f, "the aircraft to go down");
            Assert.IsFalse(dart.IsAlive, "An air shot kills an air unit.");
        }


        [UnityTest, Category("Content")]
        public IEnumerator GroundMissilePassesOverAirUnits()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            StrikeAircraft dart = SpawnAir(manager, new Vector2(0f, 2f));
            GroundUnit turret = SpawnGround(manager, StrikeUnit.Turret, new Vector2(0f, 6f));
            if (dart == null || turret == null)
            {
                Assert.Ignore("The scene has no strike units yet.");
            }
            dart.Velocity = Vector2.zero;
            float dartHealth = dart.Health;
            manager.spawner.FireStrikeShot(StrikeShotKind.GroundMissile, new Vector2(0f, -1f), Vector2.up * 14f, 500f, Altitude.Ground, manager.Ship);
            yield return WaitFor(() => !turret.IsAlive, 2f, "the turret to go down");
            Assert.That(dart.Health, Is.EqualTo(dartHealth), "The ground missile flew under the aircraft.");
            Assert.IsFalse(turret.IsAlive, "The ground missile hit the turret.");
        }


        [UnityTest, Category("Content")]
        public IEnumerator GroundUnitPaysItsBounty()
        {
            yield return StartStrike(false);
            AsteroidsGameManager manager = TestScenes.Manager;
            GroundUnit turret = SpawnGround(manager, StrikeUnit.Turret, new Vector2(4f, 4f));
            if (turret == null)
            {
                Assert.Ignore("The scene has no ground units yet.");
            }
            yield return WaitFor(() => turret.HasEntered, 1f, "the turret on screen");
            int before = manager.Scoring.Score;
            int destroyed = manager.HostilesDestroyed;
            turret.TakeHit(new DamageInfo(1000f, Vector2.up, turret.Position, DamageSource.PlayerShot, true));
            yield return null;
            Assert.That(manager.Scoring.Score, Is.EqualTo(before + turret.Score), "The bounty went into the mission money.");
            Assert.That(manager.HostilesDestroyed, Is.EqualTo(destroyed + 1), "The kill counts for the 70% star.");
        }


        [UnityTest, Category("Content")]
        public IEnumerator BossPartDiesOnItsOwnAndTheCoreArmourOpens()
        {
            yield return StartStrike(true);
            AsteroidsGameManager manager = TestScenes.Manager;
            if (!(manager.Mission is StrikeLevel level) || level.StrikeBossPrefab == null)
            {
                Assert.Ignore("The campaign has no strike boss yet.");
            }
            manager.SkipToBoss();
            yield return WaitFor(() => manager.ActiveBoss is StrikeBoss, 20f, "the boss");
            var boss = manager.ActiveBoss as StrikeBoss;
            Assert.NotNull(boss, "The boss arrived.");
            var lowest = new List<BossPart>();
            int tier = int.MaxValue;
            foreach (BossPart part in boss.Parts)
            {
                if (part != null && part.IsAlive)
                {
                    tier = Mathf.Min(tier, part.Tier);
                }
            }
            foreach (BossPart part in boss.Parts)
            {
                if (part != null && part.IsAlive && part.Tier == tier)
                {
                    lowest.Add(part);
                }
            }
            if (lowest.Count == 0 || boss.CoreTier <= tier)
            {
                Assert.Ignore("This boss has no armoured core over its parts.");
            }
            // The boss is armoured on its way in anyway (a ground boss rides the ground in for several seconds): wait until
            // it fights.
            yield return WaitFor(() => boss.HasEntered, 20f, "the boss to take its place");
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(boss.Invulnerable, "The core is armoured while its lower parts live.");
            BossPart first = lowest[0];
            first.TakeHit(new DamageInfo(100000f, Vector2.up, first.Position, DamageSource.PlayerShot, true));
            yield return null;
            Assert.IsFalse(first.IsAlive, "The part died on its own.");
            Assert.IsTrue(boss.IsAlive, "The boss lives on.");
            for (int i = 1; i < lowest.Count; i++)
            {
                lowest[i].TakeHit(new DamageInfo(100000f, Vector2.up, lowest[i].Position, DamageSource.PlayerShot, true));
            }
            yield return WaitFor(() => !boss.Invulnerable, 3f, "the core's armour to open");
            Assert.IsFalse(boss.Invulnerable, "With its lower tier gone the core can be hit.");
        }


        // ------------------------------------------------------------------ helpers

        /// <summary>A strike mission is flying: the campaign's first (a content test needs it), else one of the test's own.</summary>
        private IEnumerator StartStrike(bool needsContent)
        {
            yield return LoadWithStrikeMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            int index = FirstStrikeMission(manager);
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(index));
            yield return WaitFor(() => manager.IsMissionActive, 6f, "the strike mission");
            Assert.IsTrue(manager.IsMissionActive, "The strike mission started.");
        }


        /// <summary>Loads the game with its progress reset; when the campaign has no strike mission a copy gets one.</summary>
        private IEnumerator LoadWithStrikeMission()
        {
            yield return TestScenes.Load(TestScenes.GameScene);
            AsteroidsGameManager manager = TestScenes.Manager;
            Assert.NotNull(manager, "The game scene has no Asteroids manager.");
            manager.Progress?.ResetAll(manager.LevelCount);
            if (FirstStrikeMission(manager) < 0)
            {
                AddTestMission(manager);
                manager.ReturnToMissionSelect();
                yield return null;
            }
        }


        private static int FirstStrikeMission(AsteroidsGameManager manager)
        {
            for (int i = 0; i < manager.LevelCount; i++)
            {
                if (manager.AsteroidsCampaign.Mission(i) is StrikeLevel)
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary>
        /// A copy of the campaign with a strike sector and a plain strike mission of its own (settings from the first field
        /// mission, 30 plain tiles, no events, the boss out of reach).
        /// </summary>
        private void AddTestMission(AsteroidsGameManager manager)
        {
            AsteroidsCampaign original = manager.AsteroidsCampaign;
            AsteroidsCampaign copy = temporary.Keep(Object.Instantiate(original));
            var sectors = new List<AsteroidsCampaign.Sector>(copy.sectors)
            {
                new AsteroidsCampaign.Sector { title = "Test Range", starsRequired = 0, mode = MissionMode.Strike }
            };
            copy.sectors = sectors.ToArray();
            var level = temporary.Asset<StrikeLevel>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(original.Mission(0)), level);
            level.name = "TestStrike";
            level.sector = sectors.Count - 1;
            level.objective = LevelObjective.Boss;
            level.waves = new WaveSpec[0];
            level.boss = null;
            level.hints = new string[0];
            level.touchHints = new string[0];
            level.segments = new[] { new TerrainSegment { kind = TerrainKind.Plain, variant = 0, tiles = 30 } };
            level.events = new StrikeEvent[0];
            level.bossAt = 2000f;
            copy.LevelList.Add(level);
            originalCampaign = manager.CampaignAsset;
            manager.CampaignAsset = copy;
        }


        private static StrikeAircraft SpawnAir(AsteroidsGameManager manager, Vector2 at)
        {
            var spawn = new StrikeEvent { unit = StrikeUnit.Dart, x = at.x, flight = FlightType.Repeat, path = FlightPath.HoverMid };
            StrikeAircraft unit = manager.spawner.SpawnAircraft(StrikeUnit.Dart, spawn, 0, at, 0f);
            if (unit != null)
            {
                unit.Position = at;
            }
            return unit;
        }


        private static GroundUnit SpawnGround(AsteroidsGameManager manager, StrikeUnit kind, Vector2 at)
        {
            var spawn = new StrikeEvent { unit = kind, x = at.x };
            return manager.spawner.SpawnGround(kind, spawn, 0, at);
        }


        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string what)
        {
            float waited = 0f;
            while (!condition() && waited < seconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }


        /// <summary>Holds a direction and nothing else.</summary>
        private sealed class HeldInput : IShipInput
        {
            public Vector2 Move { get; set; }
            public float Turn => 0f;
            public float Thrust => 0f;
            public bool Brake => false;
            public bool Fire => false;
            public bool DashPressed => false;
            public bool BombPressed => false;
            public bool CyclePressed => false;


            public void Read(AsteroidsPlayer ship, float deltaTime)
            {
            }
        }
    }
}
