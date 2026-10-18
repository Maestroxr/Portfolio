using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// What a wave of the field campaigns adds (AsteroidsLevel.FieldCampaigns.cs): the large iron, hydra and wreck rocks
    /// it starts with, and its flares, ion storms, sentries, minelayers, stalkers and corsairs.
    /// </summary>
    public partial class WaveSpec
    {
        [Header("Field campaigns: large asteroids")]
        public int metal;
        public int hydra;
        public int wrecks;

        [Header("Field campaigns: hazards and enemies")]
        public int flares;
        public int storms;
        public int sentries;
        public int minelayers;
        public int stalkers;
        public int corsairs;

        private int CampaignAsteroids => metal + hydra + wrecks;

        private int CampaignHazards => flares + storms + sentries + minelayers + stalkers + corsairs;

        /// <summary>How many rocks of a kind the field campaigns added; null for the kinds of the first campaign.</summary>
        private int? CampaignCount(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Metal: return metal;
                case AsteroidKind.Hydra: return hydra;
                case AsteroidKind.Wreck: return wrecks;
                default: return null;
            }
        }

        /// <summary>How many hazards of a kind the field campaigns added; null for the kinds of the first campaign.</summary>
        private int? CampaignCount(HazardKind kind)
        {
            switch (kind)
            {
                case HazardKind.SolarFlare: return flares;
                case HazardKind.IonStorm: return storms;
                case HazardKind.Sentry: return sentries;
                case HazardKind.Minelayer: return minelayers;
                case HazardKind.Stalker: return stalkers;
                case HazardKind.Corsair: return corsairs;
                default: return null;
            }
        }
    }


    /// <summary>
    /// The settings of the field campaigns' missions (AsteroidsLevel.FieldCampaigns.cs): the quarry of a hunt, the
    /// freighter or station of an escort or defence and how tough it is, and the share of saucers that are scouts.
    /// </summary>
    public partial class AsteroidsLevel
    {
        [Header("Field campaigns")]
        [Tooltip("Hunt missions: the kinds of enemy that are marked as the quarry.")]
        [SerializeField] internal HazardKind[] quarry = new HazardKind[0];
        [Tooltip("Hunt missions: the share of the quarry's kind that is marked (1 marks every one).")]
        [SerializeField, Range(0f, 1f)] internal float quarryShare = 1f;
        [Tooltip("Escort and defence missions: the hull of the freighter or the station.")]
        [SerializeField] internal float allyHull = 120f;
        [Tooltip("Escort missions: seconds a freighter takes to cross the sector.")]
        [SerializeField] internal float crossing = 45f;
        [Tooltip("Chance that a saucer is the small, aiming scout; below zero the sector decides (the first campaign's rule).")]
        [SerializeField] internal float scoutChance = -1f;

        /// <summary>The kinds of enemy a hunt marks as its quarry.</summary>
        public HazardKind[] Quarry => quarry ?? new HazardKind[0];

        /// <summary>The share of the quarry's kind that is marked.</summary>
        public float QuarryShare => quarryShare;

        public float AllyHull => allyHull;

        public float Crossing => crossing;

        public float ScoutChance => scoutChance;

        /// <summary>Whether enemies of <paramref name="kind"/> are the quarry of this mission (a hunt).</summary>
        public bool IsQuarry(HazardKind kind)
        {
            return objective == LevelObjective.Hunt && Array.IndexOf(Quarry, kind) >= 0;
        }

        /// <summary>The checks of the campaign objectives: a positive target, a quarry to hunt, a tough enough ally.</summary>
        private bool IsCampaignObjectiveValid(out string message)
        {
            message = string.Empty;
            if (objective != LevelObjective.Escort && objective != LevelObjective.Defend && objective != LevelObjective.Hunt)
            {
                return true;
            }
            if (objectiveTarget <= 0)
            {
                message = $"The {objective} objective needs a positive target.";
                return false;
            }
            if (objective == LevelObjective.Hunt && Quarry.Length == 0)
            {
                message = "The hunt has no quarry.";
                return false;
            }
            if (objective != LevelObjective.Hunt && allyHull <= 0f)
            {
                message = $"The {objective} mission's ally has no hull.";
                return false;
            }
            if (objective == LevelObjective.Escort && crossing <= 0f)
            {
                message = "The escort's crossing takes no time.";
                return false;
            }
            return true;
        }
    }
}
