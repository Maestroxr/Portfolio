using UnityEngine;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The objectives of the field campaigns (MissionObjective.FieldCampaigns.cs): escort freighters across the sector,
    /// defend a station for a time, hunt marked enemies. All three keep their measure in <see cref="Survived"/>: the
    /// freighters brought through (and the share of the way the one under way has come), the seconds the station has
    /// stood, the marked enemies destroyed. So the progress the simulator of a shared mission reports, a saved mission and
    /// the HUD all read one number, and <see cref="Target"/> is the goal it has to reach.
    /// </summary>
    public partial class MissionObjective
    {
        /// <summary>The hull of the freighter or station, 0 to 1, for the HUD line; negative when there is none to show.</summary>
        public float AllyHull { get; set; } = -1f;

        /// <summary>Freighters that reached the far side of the sector.</summary>
        public int Escorted => Type == LevelObjective.Escort ? Mathf.FloorToInt(Survived + 0.0001f) : 0;

        /// <summary>Marked enemies destroyed.</summary>
        public int MarkedDestroyed => Type == LevelObjective.Hunt ? Mathf.FloorToInt(Survived + 0.0001f) : 0;

        /// <summary>Whether this is an objective of the field campaigns that protects a freighter or a station.</summary>
        public bool HasAlly => Type == LevelObjective.Escort || Type == LevelObjective.Defend;

        private bool CampaignComplete => Target > 0 && Survived >= Target - 0.0001f;

        private float CampaignProgress => Target > 0 ? Mathf.Clamp01(Survived / Target) : 0f;


        /// <summary>The freighter under way has come <paramref name="share"/> (0 to 1) of its way across.</summary>
        public void EscortUnderWay(float share)
        {
            if (Type == LevelObjective.Escort)
            {
                Survived = Mathf.Min(Target, Escorted + Mathf.Clamp(share, 0f, 0.999f));
            }
        }


        /// <summary>A freighter reached the far side.</summary>
        public void FreighterDelivered()
        {
            if (Type == LevelObjective.Escort)
            {
                Survived = Mathf.Min(Target, Escorted + 1);
            }
        }


        /// <summary>The station stood for another <paramref name="deltaTime"/> seconds.</summary>
        public void Hold(float deltaTime)
        {
            if (Type == LevelObjective.Defend)
            {
                Survived = Mathf.Min(Target, Survived + deltaTime);
            }
        }


        /// <summary>A marked enemy was destroyed.</summary>
        public void MarkedDown()
        {
            if (Type == LevelObjective.Hunt)
            {
                Survived = Mathf.Min(Target, MarkedDestroyed + 1);
            }
        }


        private string CampaignBriefing
        {
            get
            {
                switch (Type)
                {
                    case LevelObjective.Escort: return Target == 1 ? T("Escort the freighter across the sector") : F("Escort {0} freighters across the sector", Target);
                    case LevelObjective.Defend: return F("Defend the station for {0}", FormatTime(Target));
                    default: return Target == 1 ? T("Destroy the marked target") : F("Destroy {0} marked targets", Target);
                }
            }
        }


        /// <summary>The HUD line of a campaign objective at <paramref name="measure"/> (see the class summary).</summary>
        private string DescribeCampaign(float measure, int unused)
        {
            string hull = AllyHull >= 0f ? "   " + F("HULL {0}%", Mathf.CeilToInt(Mathf.Clamp01(AllyHull) * 100f)) : string.Empty;
            switch (Type)
            {
                case LevelObjective.Escort:
                    return F("ESCORT  {0} / {1}", Mathf.Min(Mathf.FloorToInt(measure + 0.0001f), Target), Target) + hull;
                case LevelObjective.Defend:
                    return F("DEFEND  {0}", FormatTime(Mathf.Max(0f, Target - measure))) + hull;
                default:
                    return F("MARKED  {0} / {1}", Mathf.Min(Mathf.FloorToInt(measure + 0.0001f), Target), Target);
            }
        }
    }
}
