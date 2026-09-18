using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Endless Runner game module. Loops terrain tiles ahead of the player and scatters collectibles on them; the
    /// menu, pause and state flow come from <see cref="BaseGameManager"/>.
    /// </summary>
    public class RunnerGameManager : BaseGameManager
    {
        #region Field Members
        [SerializeField] private TerrainCache Terrains;
        [SerializeField] private CollidableCache Collidables;
        [SerializeField] private TerrainBehaviour StartingTerrain;
        [SerializeField] private Vector3 StartingPoint;
        [SerializeField] private Vector3 TerrainOffset;
        [SerializeField] private int ActiveTerrainsAmount;
        [SerializeField] private int CollidablesPerTerrain;
        [SerializeField] private int CollidablesRadius;
        [SerializeField] private RunnerSettings runnerSettings;
        [SerializeField] private RunnerController controller;
        [SerializeField] private RunnerUI ui;
        [SerializeField] private Campaign campaign;

        private RunnerSettings customSettings;
        private RunnerSettings activeSettings;
        private float lastPlayerLocation;
        private float terrainDelta;
        #endregion

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => runnerSettings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : runnerSettings;
            set => activeSettings = value as RunnerSettings;
        }

        protected override bool UsesTimer => false;

        public RunnerSettings RunnerSettings => Settings as RunnerSettings ?? runnerSettings;

        public RunnerPlayer Player => PlayerList.OfType<RunnerPlayer>().FirstOrDefault();


        private RunnerSettings CreateCustomSettings()
        {
            RunnerSettings copy = runnerSettings != null ? Instantiate(runnerSettings) : ScriptableObject.CreateInstance<RunnerSettings>();
            copy.name = $"{(runnerSettings != null ? runnerSettings.name : "RunnerSettings")} (custom)";
            return copy;
        }


        protected override void Start()
        {
            base.Start();
            if (StartingTerrain != null)
            {
                Terrains.AddDeployed(StartingTerrain);
                terrainDelta = (StartingTerrain.CenterTerrainOffset * 2).z;
            }
            if (Player != null)
            {
                lastPlayerLocation = Player.transform.position.z;
            }

            ApplySettings();
            PrepareGame();
            UpdateScore();
        }


        protected override void Update()
        {
            base.Update();
            if (!IsGameRunning || Player == null)
            {
                return;
            }
            if (lastPlayerLocation - Player.transform.position.z > -TerrainOffset.z)
            {
                LoopTile();
                lastPlayerLocation = Player.transform.position.z;
            }
        }


        private void ApplySettings()
        {
            RunnerSettings active = RunnerSettings;
            if (active == null)
            {
                return;
            }
            CollidablesPerTerrain = active.CollidablesPerTerrain;
            CollidablesRadius = active.CollidablesRadius;
            Player?.ApplySettings(active);
        }


        /// <summary>
        /// Deploys terrains and then goes over them to position and deploy collidables
        /// while increasing the position by a certain offset in each iteration
        /// </summary>
        private void PrepareGame()
        {
            Vector3 currentTerrainLocation = StartingPoint;
            IList<TerrainBehaviour> activeTerrains = Terrains.GetOrderedDeployedCache();
            if (ActiveTerrainsAmount > activeTerrains.Count)
            {
                Terrains.Deploy(ActiveTerrainsAmount - activeTerrains.Count);
            }
            activeTerrains = Terrains.GetOrderedDeployedCache();
            foreach (TerrainBehaviour terrain in activeTerrains)
            {
                PrepareTerrain(currentTerrainLocation, terrain);
                currentTerrainLocation += TerrainOffset;
            }
        }


        /// <summary>Resets the score, applies the settings and starts running.</summary>
        public override void StartGame()
        {
            ApplySettings();
            ResetScore();
            Player?.ResetMotion();
            TransitionState(BaseGameState.Running);
        }


        public override void LoadLevel(int level)
        {
            LevelIndex = level;
            CurrentLevel = LevelData.Create(level);
            if (Level is RunnerLevel runnerLevel && runnerLevel.Settings != null)
            {
                runnerSettings = runnerLevel.Settings;
                if (UsingDefaultSettings)
                {
                    Settings = runnerSettings;
                }
            }
            UpdateLevel();
        }


        /// <summary>
        /// Ends the run: puts the player back on the first terrain, rebuilds the track and shows the menu.
        /// </summary>
        public void GameOver()
        {
            if (!IsGameRunning)
            {
                return;
            }
            IList<TerrainBehaviour> activeTerrains = Terrains.GetOrderedDeployedCache();
            if (activeTerrains.Count > 0 && Player != null)
            {
                TerrainBehaviour firstTerrain = activeTerrains[0];
                StartingPoint = firstTerrain.transform.position;
                Player.ResetMotion();
                Player.transform.position = StartingPoint + firstTerrain.CenterTerrainOffset;
                lastPlayerLocation = Player.transform.position.z;
            }
            PrepareGame();
            TransitionState(BaseGameState.GameOver);
        }


        /// <summary>
        /// Positions the terrain and deploying its collidables
        /// </summary>
        private void PrepareTerrain(Vector3 terrainLocation, TerrainBehaviour terrain)
        {
            terrain.transform.position = terrainLocation;
            terrain.GameManager = this;

            // Prepare coins
            UndeployCollidables(terrain);
            ICollection<Collidable> collidables = Collidables.Deploy(CollidablesPerTerrain);
            terrain.PrepareCollidables(collidables, CollidablesRadius);
        }


        /// <summary>
        /// Loops the first terrain - one behind the player, and puts it infront of the first one
        /// </summary>
        private void LoopTile()
        {
            IList<TerrainBehaviour> activeTerrains = Terrains.GetOrderedDeployedCache();
            if (activeTerrains.Count == 0)
            {
                return;
            }

            TerrainBehaviour firstTerrain = activeTerrains[0];
            Terrains.Undeploy(firstTerrain);
            UndeployCollidables(firstTerrain);
            activeTerrains.RemoveAt(0);

            if (activeTerrains.Count < ActiveTerrainsAmount)
            {
                TerrainBehaviour lastTerrain = activeTerrains[activeTerrains.Count - 1];
                TerrainBehaviour newTerrain = Terrains.Deploy();
                Vector3 newTerrainPos = lastTerrain.transform.position + TerrainOffset;
                PrepareTerrain(newTerrainPos, newTerrain);
            }
        }


        public void ObjectCollided(Collidable item)
        {
            Collidables.Undeploy(item);
            if (IsGameRunning)
            {
                IncreaseScore(1);
            }
        }


        private void UndeployCollidables(TerrainBehaviour terrain)
        {
            if (null == terrain.Collidables)
            {
                throw new System.ArgumentNullException("Collidables list is null");
            }

            foreach (Collidable collidable in terrain.Collidables)
            {
                Collidables.Undeploy(collidable);
                collidable.BelongedTerrain = null;
            }
            terrain.Collidables.Clear();
        }


        public override bool DoesSaveGameExist()
        {
            return false;
        }


        public override void SaveGame()
        {
            UI?.UpdateError("Endless Runner does not support saving a run.");
        }


        public override void LoadGame()
        {
            UI?.UpdateError("Endless Runner does not support loading a run.");
        }
    }
}
