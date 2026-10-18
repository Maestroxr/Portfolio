using UnityEngine;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The campaigns of the mission select: each tab (the asteroid field, the planet strike) shows one campaign of its mode
    /// at a time (<see cref="AsteroidsCampaign.Arc"/>), the campaign of the mission selected on it. The campaign bar's
    /// arrows switch to another campaign, which selects that campaign's next mission to fly
    /// (<see cref="CampaignSelect.NextToFly"/>); the campaign a tab showed last is remembered on the device
    /// (<see cref="AsteroidsProgress.ShownCampaign"/>), so the mission select opens on it again.
    /// </summary>
    public partial class AsteroidsGameManager
    {
        /// <summary>
        /// The campaign of <paramref name="mode"/> its tab shows: the campaign of the mission selected on it (the endless
        /// mission belongs to the first field campaign), else the one the tab showed last.
        /// </summary>
        public int ShownArc(MissionMode mode)
        {
            AsteroidsCampaign campaign = AsteroidsCampaign;
            if (campaign == null)
            {
                return 0;
            }
            int index = mode == MissionMode.Strike ? strikeMenuMission : fieldMenuMission;
            if (Mission != null && Mission.Mode == mode && phase == MissionPhase.Menu)
            {
                index = LevelIndex;
            }
            if (IsMissionOf(index, mode))
            {
                return campaign.ArcOf(index);
            }
            return RememberedArc(mode);
        }


        /// <summary>The campaign of <paramref name="mode"/> the device remembers (inside the campaigns the mode has).</summary>
        private int RememberedArc(MissionMode mode)
        {
            return CampaignSelect.ClampArc(AsteroidsCampaign, mode, progress != null ? progress.ShownCampaign(mode) : 0);
        }


        /// <summary>What the campaign bar shows on the tab of <paramref name="mode"/>: the campaign it shows, its stars and lock.</summary>
        public CampaignSummary CampaignSummaryOf(MissionMode mode)
        {
            AsteroidsCampaign campaign = AsteroidsCampaign;
            if (campaign == null || campaign.MissionCountOf(mode) == 0)
            {
                // A mode without missions has no campaign to show (the bar hides).
                return new CampaignSummary { Mode = mode };
            }
            int arc = ShownArc(mode);
            bool open = campaign.IsArcOpen(mode, arc, progress);
            return new CampaignSummary
            {
                Mode = mode,
                Arc = arc,
                Count = campaign.ArcCount(mode),
                Title = T(CampaignTitle(campaign, mode, arc)),
                Description = T(campaign.GetArc(mode, arc)?.description ?? string.Empty),
                Stars = progress != null ? campaign.TotalStars(progress, mode, arc) : 0,
                MaxStars = campaign.MaxStarsOf(mode, arc),
                Open = open,
                LockReason = open ? string.Empty : CampaignLockReason(mode, arc)
            };
        }


        /// <summary>
        /// Shows campaign <paramref name="arc"/> of <paramref name="mode"/> on its tab and selects its next mission to fly
        /// (its first open mission without stars, else its first). Only at the mission select.
        /// </summary>
        public void SelectCampaign(MissionMode mode, int arc)
        {
            AsteroidsCampaign campaign = AsteroidsCampaign;
            if (phase != MissionPhase.Menu || campaign == null || arc < 0 || arc >= campaign.ArcCount(mode))
            {
                return;
            }
            int index = CampaignSelect.NextToFly(campaign, mode, arc, progress);
            if (index < 0)
            {
                return;
            }
            sounds?.Click();
            progress?.SetShownCampaign(mode, arc);
            PreviewMission(index);
        }


        /// <summary>The campaign bar's arrows: the campaign <paramref name="step"/> places after the one the tab shows.</summary>
        public void StepCampaign(int step)
        {
            if (phase != MissionPhase.Menu || IsLobbyOpen)
            {
                return;
            }
            MissionMode mode = menuMode;
            AsteroidsCampaign campaign = AsteroidsCampaign;
            int arc = ShownArc(mode) + step;
            if (campaign == null || arc < 0 || arc >= campaign.ArcCount(mode))
            {
                sounds?.Denied();
                return;
            }
            SelectCampaign(mode, arc);
        }


        /// <summary>The device remembers the campaign of the mission at <paramref name="index"/> as the one its tab shows.</summary>
        private void RememberCampaign(int index)
        {
            AsteroidsLevel mission = MissionAt(index);
            if (mission != null && progress != null && AsteroidsCampaign != null)
            {
                progress.SetShownCampaign(mission.Mode, AsteroidsCampaign.ArcOf(index));
            }
        }


        /// <summary>The name of campaign <paramref name="arc"/> of <paramref name="mode"/> as the asset has it ("Campaign 2" without one).</summary>
        internal static string CampaignTitle(AsteroidsCampaign campaign, MissionMode mode, int arc)
        {
            string title = campaign != null ? campaign.GetArc(mode, arc)?.title : null;
            return !string.IsNullOrEmpty(title) ? title : $"Campaign {arc + 1}";
        }


        /// <summary>The name of the campaign of the mission at <paramref name="index"/> as the asset has it; null for the endless mission.</summary>
        private string CampaignTitleOf(int index)
        {
            AsteroidsCampaign campaign = AsteroidsCampaign;
            AsteroidsLevel mission = MissionAt(index);
            if (campaign == null || mission == null || mission.IsEndless)
            {
                return null;
            }
            return CampaignTitle(campaign, mission.Mode, campaign.ArcOf(index));
        }


        /// <summary>Why campaign <paramref name="arc"/> of <paramref name="mode"/> is still closed: the stars of the mode it needs.</summary>
        private string CampaignLockReason(MissionMode mode, int arc)
        {
            AsteroidsCampaign campaign = AsteroidsCampaign;
            int required = campaign != null ? campaign.ArcStarsRequired(mode, arc) : 0;
            int stars = campaign != null && progress != null ? campaign.TotalStars(progress, mode) : 0;
            return mode == MissionMode.Strike
                ? F("Collect {0} planet strike stars to open this campaign ({1} so far).", required, stars)
                : F("Collect {0} asteroid field stars to open this campaign ({1} so far).", required, stars);
        }


        /// <summary>The stars of the campaign the tab of <paramref name="mode"/> shows (its star counter and its sector gates).</summary>
        private int ShownCampaignStars(MissionMode mode)
        {
            AsteroidsCampaign campaign = AsteroidsCampaign;
            if (campaign == null)
            {
                return TotalStars;
            }
            return progress != null ? campaign.TotalStars(progress, mode, ShownArc(mode)) : 0;
        }


        /// <summary>The stars the campaign the tab of <paramref name="mode"/> shows can earn.</summary>
        private int ShownCampaignMaxStars(MissionMode mode)
        {
            AsteroidsCampaign campaign = AsteroidsCampaign;
            return campaign != null ? campaign.MaxStarsOf(mode, ShownArc(mode)) : MaxStars;
        }
    }
}
