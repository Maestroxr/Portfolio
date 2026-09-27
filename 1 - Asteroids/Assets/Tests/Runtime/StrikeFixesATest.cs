using System.Collections;
using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// Play tests of the fixes of the strike review (group A) that need the game scene: the countdown keeps the start
    /// invulnerability and the megabombs, the fly-off cannot turn a win into a loss, the damage star counts every hit,
    /// a retry starts over clean ground and the ground follows a camera that re-fits while the scroll stands.
    /// </summary>
    public class StrikeFixesATest
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
            if (manager != null && manager.Ship != null)
            {
                manager.Ship.Input = null;
            }
            if (manager != null && originalCampaign != null)
            {
                manager.ReturnToMissionSelect();
                manager.CampaignAsset = originalCampaign;
                manager.ReturnToMissionSelect();
                yield return null;
            }
            originalCampaign = null;
            temporary.Dispose();
        }


        [UnityTest]
        public IEnumerator TheCountdownKeepsTheStartInvulnerabilityAndTheMegabombs()
        {
            yield return LoadWithStrikeMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            manager.Pilot.Megabombs = 2;
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(FirstStrikeMission(manager)));
            yield return WaitFor(() => manager.IsBriefing, 3f, "the countdown");
            Assert.IsTrue(manager.IsBriefing, "The countdown runs.");
            manager.Ship.Input = new PressingInput(manager);
            yield return WaitFor(() => manager.IsMissionActive, 6f, "GO");
            Assert.IsTrue(manager.IsMissionActive, "The mission started.");
            Assert.That(manager.Working.Megabombs, Is.EqualTo(2), "No megabomb is dropped before GO.");
            Assert.That(manager.Ship.InvulnerableTime, Is.GreaterThan(StrikeRules.StartInvulnerability - 0.2f),
                "The start invulnerability starts at GO.");
            manager.Ship.Input = null;
        }


        [UnityTest]
        public IEnumerator TheFlyOffCannotTurnTheWinIntoALoss()
        {
            yield return StartStrike();
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsPlayer ship = manager.Ship;
            yield return TestScenes.WaitForInvulnerability(ship);
            int wallet = manager.Pilot.Money;
            manager.AddMoney(5000, Vector2.zero);
            manager.Objective.Defeat();
            yield return WaitFor(() => manager.IsFlyingOff, 6f, "the fly-off");
            Assert.IsTrue(manager.IsFlyingOff, "The fly-off began.");
            Assert.IsFalse(ship.TakeDamage(new DamageInfo(1000f, Vector2.down, ship.Position, DamageSource.Enemy, false)),
                "Nothing hurts the ship while it is flown out.");
            Assert.IsTrue(ship.IsAlive);
            foreach (Shot shot in manager.Field.EnemyShots)
            {
                Assert.IsFalse(shot.InPlay, "The enemy fire fizzled out.");
            }
            yield return WaitFor(() => manager.State.Is(BaseGameState.Victory), 10f, "the results of the won mission");
            Assert.IsTrue(manager.State.Is(BaseGameState.Victory), "The mission is won.");
            Assert.IsTrue(manager.HasLanded, "The ship landed.");
            Assert.That(manager.Pilot.Money, Is.EqualTo(wallet + 5000), "The mission's money is in the wallet.");
        }


        [UnityTest]
        public IEnumerator DamageInTheFrameOfAnEnergyGainCountsForTheStar()
        {
            yield return StartStrike();
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsPlayer ship = manager.Ship;
            yield return TestScenes.WaitForInvulnerability(ship);
            StrikeLoadout working = manager.Working;
            float before = manager.DamageTaken;
            float energy = working.Energy;
            Assert.IsTrue(ship.TakeDamage(new DamageInfo(20f, Vector2.down, ship.Position, DamageSource.Enemy, false)));
            float taken = energy - working.Energy;
            working.AddEnergy(25f);
            yield return null;
            Assert.That(taken, Is.GreaterThan(0f));
            Assert.That(manager.DamageTaken - before, Is.EqualTo(taken).Within(1e-3f), "The hit counts although the energy went up in the frame.");
        }


        [UnityTest]
        public IEnumerator TheGroundFollowsTheCameraAtTheResultsAndARetryStartsClean()
        {
            yield return StartStrike();
            AsteroidsGameManager manager = TestScenes.Manager;
            StrikeTerrain terrain = manager.Field.Terrain;
            if (terrain == null || !terrain.IsShown)
            {
                Assert.Ignore("The scene shows no strike terrain.");
            }
            AsteroidsPlayer ship = manager.Ship;
            yield return TestScenes.WaitForInvulnerability(ship);
            terrain.AddCrater(new Vector2(0f, 0f), 2f);
            Assert.That(terrain.CratersShown, Is.GreaterThan(0), "A crater lies on the ground.");
            ship.TakeDamage(new DamageInfo(1000f, Vector2.down, ship.Position, DamageSource.Hazard, false));
            yield return WaitFor(() => manager.State.Is(BaseGameState.GameOver), 6f, "the failed mission's results");
            Assert.IsTrue(manager.State.Is(BaseGameState.GameOver));

            // The window changes shape at the results: the camera backs off while the scroll stands.
            float depth = DepthLayer.Distance;
            int tiles = terrain.TilesShown;
            DepthLayer.Distance = depth * 3.5f;
            yield return null;
            Assert.That(terrain.TilesShown, Is.GreaterThan(tiles), "The tiles cover the taller view.");
            DepthLayer.Distance = depth;
            yield return null;

            manager.RetryMission();
            yield return WaitFor(() => manager.IsBriefing, 3f, "the countdown of the retry");
            Assert.IsTrue(terrain.IsShown);
            Assert.That(terrain.CratersShown, Is.Zero, "The retry starts over ground without the old craters.");
        }


        // ------------------------------------------------------------------ helpers

        private IEnumerator StartStrike()
        {
            yield return LoadWithStrikeMission();
            AsteroidsGameManager manager = TestScenes.Manager;
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(FirstStrikeMission(manager)));
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


        /// <summary>A copy of the campaign with a plain strike mission of its own (no events, the boss out of reach).</summary>
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


        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string what)
        {
            float waited = 0f;
            while (!condition() && waited < seconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }


        /// <summary>Presses the megabomb on every frame of the countdown and nothing else.</summary>
        private sealed class PressingInput : IShipInput
        {
            private readonly AsteroidsGameManager manager;


            public PressingInput(AsteroidsGameManager manager)
            {
                this.manager = manager;
            }

            public Vector2 Move => Vector2.zero;
            public float Turn => 0f;
            public float Thrust => 0f;
            public bool Brake => false;
            public bool Fire => false;
            public bool DashPressed => false;
            public bool BombPressed => manager.IsBriefing;
            public bool CyclePressed => false;


            public void Read(AsteroidsPlayer ship, float deltaTime)
            {
            }
        }
    }
}
