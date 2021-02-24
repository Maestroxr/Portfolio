
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    #region Field Members
    // Should serialized fields start with capital letter even if private and be before public fields?
    [SerializeField] private TerrainCache Terrains;
    [SerializeField] private CollidableCache Collidables;
    [SerializeField] private TerrainBehaviour StartingTerrain;
    [SerializeField] private Player Player;
    [SerializeField] private Vector3 StartingPoint;
    [SerializeField] private Vector3 TerrainOffset;
    [SerializeField] private int ActiveTerrainsAmount;
    [SerializeField] private int CollidablesPerTerrain;
    [SerializeField] private int CollidablesRadius;
    [SerializeField] private Text ScoreText;
    [SerializeField] private Button StartGameButton;

    public bool IsGameRunning { get; private set; }

    private float lastPlayerLocation;
    private float terrainDelta;
    private int score = 0;
    #endregion


    // Start is called before the first frame update
    void Start()
    {
        Terrains.AddDeployed(StartingTerrain);
        terrainDelta = (StartingTerrain.CenterTerrainOffset * 2).z;
        lastPlayerLocation = Player.transform.position.z;

        PrepareGame();
        UpdateScore();
    }


    // Update is called once per frame
    void Update()
    {
        if (IsGameRunning)
        {
            if (lastPlayerLocation - Player.transform.position.z > -TerrainOffset.z)
            {
                LoopTile();
                lastPlayerLocation = Player.transform.position.z;
            }
        }
    }


   /// <summary>
   /// Deploys terrains and then goes over them to position and deploy collidables
   /// while increasing the position by a certain offset in each iteration
   /// </summary>
    void PrepareGame()
    {
        // Prepare terrains
        Vector3 currentTerrainLocation = StartingPoint;
        IList<TerrainBehaviour> activeTerrains = Terrains.GetOrderedDeployedCache();
        Terrains.Deploy(ActiveTerrainsAmount - activeTerrains.Count);
        activeTerrains = Terrains.GetOrderedDeployedCache(); 
        foreach (TerrainBehaviour terrain in activeTerrains)
        {
            PrepareTerrain(currentTerrainLocation, terrain);
            currentTerrainLocation += TerrainOffset;
        }
    }


    /// <summary>
    /// Resets game score and changes game state to running, also handles UI
    /// </summary>
    public void StartGame()
    {
        score = 0;
        UpdateScore();
        IsGameRunning = true;
        StartGameButton.gameObject.SetActive(false);
    }


    /// <summary>
    /// Changes games state to not running and positions the player to start a new game, also handles UI
    /// </summary>
    public void GameOver()
    {
        IList<TerrainBehaviour> activeTerrains = Terrains.GetOrderedDeployedCache();
        TerrainBehaviour firstTerrain = activeTerrains[0];
        StartingPoint = firstTerrain.transform.position;
        Player.transform.position = StartingPoint + firstTerrain.CenterTerrainOffset;
        lastPlayerLocation = Player.transform.position.z;
        IsGameRunning = false;
        PrepareGame();
        StartGameButton.gameObject.SetActive(true);
    }


    /// <summary>
    /// Positions the terrain and deploying its collidables
    /// </summary>
    /// <param name="terrainLocation"></param>
    /// <param name="terrain"></param>
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
        score++;
        UpdateScore();
    }


    private void UpdateScore()
    {
        ScoreText.text = $"Score: {score}";
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
}

