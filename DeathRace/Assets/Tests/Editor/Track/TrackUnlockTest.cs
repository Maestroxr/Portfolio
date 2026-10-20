using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Which tracks a device has open (<see cref="DeathRaceCampaign.IsUnlocked"/>): the first three from the start, the
    /// others by a star on the ordinary track before (the Showdown's arena skipped) or a career that reached them, the
    /// arena by a title; and what the quick race window says about a locked one.
    /// </summary>
    public class TrackUnlockTest
    {
        private const int DowntownCarnage = 7;
        private const int TheCrucible = 8;
        private const int RefineryRow = 9;
        private const int AvalancheRidge = 10;
        private const int GridlockBoulevard = 11;

        private DeathRaceCampaign campaign;
        private DeathRaceProgress progress;

        [SetUp]
        public void SetUp()
        {
            campaign = ScriptableObject.CreateInstance<DeathRaceCampaign>();
            for (int i = 0; i < TrackLayouts.Count; i++)
            {
                DeathRaceLevel level = ScriptableObject.CreateInstance<DeathRaceLevel>();
                TrackLayout layout = TrackLayouts.Of(i);
                level.Configure(i, layout.Biome, layout.Laps, layout.Showdown, layout.Description);
                campaign.LevelList.Add(level);
            }
            progress = new DeathRaceProgress(new TransientStrategy());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameLevel level in campaign.LevelList)
            {
                Object.DestroyImmediate(level);
            }
            Object.DestroyImmediate(campaign);
        }

        private List<int> Open()
        {
            var open = new List<int>();
            for (int i = 0; i < campaign.Count; i++)
            {
                if (campaign.IsUnlocked(i, progress))
                {
                    open.Add(i);
                }
            }
            return open;
        }

        [Test]
        public void ANewDeviceHasTheFirstThreeTracks()
        {
            Assert.AreEqual(12, campaign.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, Open());
            Assert.IsFalse(campaign.IsUnlocked(-1, progress));
            Assert.IsFalse(campaign.IsUnlocked(campaign.Count, progress));
            Assert.IsTrue(campaign.IsUnlocked(GridlockBoulevard, null), "without progress everything is open");
        }

        [Test]
        public void AStarOpensTheNextTrack()
        {
            progress.RecordLevel(2, 1, 0);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, Open());
            progress.RecordLevel(3, 3, 0);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, Open());
        }

        [Test]
        public void RefineryRowFollowsDowntownCarnageAndSkipsTheArena()
        {
            progress.RecordLevel(TheCrucible, 3, 0);
            Assert.IsFalse(campaign.IsUnlocked(RefineryRow, progress), "a star on the arena opens nothing");
            Assert.IsFalse(campaign.IsUnlocked(TheCrucible, progress), "and the arena wants a title");
            progress.RecordLevel(DowntownCarnage, 1, 0);
            Assert.IsTrue(campaign.IsUnlocked(RefineryRow, progress));
            Assert.IsFalse(campaign.IsUnlocked(AvalancheRidge, progress));
            progress.RecordLevel(RefineryRow, 2, 0);
            Assert.IsTrue(campaign.IsUnlocked(AvalancheRidge, progress));
            Assert.IsFalse(campaign.IsUnlocked(GridlockBoulevard, progress));
            progress.RecordLevel(AvalancheRidge, 1, 0);
            Assert.IsTrue(campaign.IsUnlocked(GridlockBoulevard, progress));
        }

        [Test]
        public void TheArenaOpensWithATitleOfAnyLeague()
        {
            Assert.IsFalse(campaign.IsUnlocked(TheCrucible, progress));
            progress.WinLeague((int)LeagueKind.Iron);
            Assert.IsTrue(campaign.IsUnlocked(TheCrucible, progress));
            Assert.IsFalse(campaign.IsUnlocked(RefineryRow, progress), "a title opens the arena, not the tracks");
        }

        [Test]
        public void ACareerOpensTheTracksItReached()
        {
            progress.OpenCareerTrack(AvalancheRidge);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, AvalancheRidge }, Open(), "a harder league opens its own tracks, in any order");
            progress.OpenCareerTracks(5);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, AvalancheRidge }, Open(), "the first league's count still counts");
            progress.OpenCareerTracks(new[] { RefineryRow, GridlockBoulevard, TheCrucible });
            Assert.IsTrue(campaign.IsUnlocked(GridlockBoulevard, progress));
            Assert.IsFalse(campaign.IsUnlocked(TheCrucible, progress), "a career's tracks never include the arena: only a title opens it");
        }

        [Test]
        public void ThePreviousOrdinaryTrackSkipsShowdowns()
        {
            Assert.AreEqual(DowntownCarnage, DeathRaceCampaign.PreviousOrdinary(RefineryRow, campaign));
            Assert.AreEqual(DowntownCarnage, DeathRaceCampaign.PreviousOrdinary(RefineryRow, null), "the track table knows the arena too");
            Assert.AreEqual(RefineryRow, DeathRaceCampaign.PreviousOrdinary(AvalancheRidge, campaign));
            Assert.AreEqual(2, DeathRaceCampaign.PreviousOrdinary(3, campaign));
            Assert.AreEqual(-1, DeathRaceCampaign.PreviousOrdinary(0, campaign));
            Assert.IsTrue(DeathRaceCampaign.IsShowdown(TheCrucible, campaign));
            Assert.IsTrue(DeathRaceCampaign.IsShowdown(TheCrucible, null));
            Assert.IsFalse(DeathRaceCampaign.IsShowdown(GridlockBoulevard, null));
        }

        [Test]
        public void TheLockSaysWhichCareerOpensATrack()
        {
            Assert.AreEqual(16, QuickRaceWindow.RankOpening(Leagues.Wasteland, 3), "the Wasteland's ranks as before");
            Assert.AreEqual(3, QuickRaceWindow.RankOpening(Leagues.Wasteland, 7));
            Assert.AreEqual(0, QuickRaceWindow.RankOpening(Leagues.Wasteland, 1), "open from the first day");
            Assert.AreEqual(0, QuickRaceWindow.RankOpening(Leagues.Wasteland, RefineryRow), "not a Wasteland track");
            var league = new LeagueSpec { Tracks = new[] { 0, 1, 2, RefineryRow, 3 }, StartTracks = 2, TrackRanks = new[] { 12, 6 } };
            Assert.AreEqual(0, QuickRaceWindow.RankOpening(league, 1));
            Assert.AreEqual(12, QuickRaceWindow.RankOpening(league, 2));
            Assert.AreEqual(6, QuickRaceWindow.RankOpening(league, RefineryRow));
            Assert.AreEqual(0, QuickRaceWindow.RankOpening(league, 3), "no rank left for it");
            Assert.AreEqual(0, QuickRaceWindow.RankOpening(null, 3));
        }

        [Test]
        public void TheQuickRaceListShowsEveryTrackAtOnce()
        {
            const float room = 704f;
            float row = QuickRaceWindow.RowHeight(TrackLayouts.Count, room);
            Assert.LessOrEqual(row * TrackLayouts.Count + 6f * (TrackLayouts.Count - 1), room + 0.01f);
            Assert.GreaterOrEqual(row, 50f, "still tall enough for a name and a line under it");
            Assert.AreEqual(78f, QuickRaceWindow.RowHeight(6, room), "a short list keeps the roomy rows");
        }
    }
}
