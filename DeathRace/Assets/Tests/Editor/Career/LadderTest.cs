using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The ladder: who ranks where, how ties fall, the thirds the races draw from, and the offers of a day.</summary>
    public sealed class LadderTest
    {
        [Test]
        public void ANewCareerStartsAtTheBottom()
        {
            CareerState state = CareerTestKit.NewCareer();
            Assert.AreEqual(CareerRules.RivalCount, state.rivals.Count);
            Assert.AreEqual(CareerRules.LadderSize, Ladder.Rank(state), "the newcomer is below the rival who also has no points");
            List<LadderEntry> ladder = Ladder.Entries(state);
            Assert.AreEqual(CareerRules.LadderSize, ladder.Count);
            Assert.IsTrue(ladder[ladder.Count - 1].IsPlayer);
            Assert.AreEqual("undertaker", ladder[0].Id, "the best of the roster leads on the first day");
            Assert.AreEqual(Enumerable.Range(1, CareerRules.LadderSize), ladder.Select(entry => entry.Rank));
        }

        [Test]
        public void PointsThenWinsThenSeniorityDecide()
        {
            CareerState state = CareerTestKit.NewCareer();
            RivalState top = state.Rival("undertaker");
            state.points = top.points;
            Assert.AreEqual(2, Ladder.Rank(state), "equal points and wins: the rival stays ahead");
            state.wins = top.wins + 1;
            Assert.AreEqual(1, Ladder.Rank(state), "more wins break the tie");
            state.points = top.points - 1;
            Assert.AreEqual(2, Ladder.Rank(state), "points come before wins");
            RivalState vex = state.Rival("vex");
            vex.points = top.points;
            List<LadderEntry> ladder = Ladder.Entries(state);
            Assert.AreEqual("undertaker", ladder[0].Id, "the one higher on the first day goes first on a tie");
            Assert.AreEqual("vex", ladder[1].Id);
            Assert.AreEqual(Ladder.Entries(state).Select(entry => entry.Id), ladder.Select(entry => entry.Id), "the same ladder every time");
            Assert.AreEqual(2, Ladder.RankOf(state, "vex"));
        }

        [Test]
        public void TheThirdsSplitTheRivalsByRank()
        {
            CareerState state = CareerTestKit.NewCareer();
            List<RivalState> hard = Ladder.Third(state, Difficulty.Hard);
            List<RivalState> medium = Ladder.Third(state, Difficulty.Medium);
            List<RivalState> easy = Ladder.Third(state, Difficulty.Easy);
            Assert.AreEqual(7, hard.Count);
            Assert.AreEqual(6, medium.Count);
            Assert.AreEqual(6, easy.Count);
            Assert.AreEqual(CareerRules.RivalCount, hard.Concat(medium).Concat(easy).Select(rival => rival.id).Distinct().Count());
            Assert.Greater(hard.Min(rival => rival.points), medium.Max(rival => rival.points));
            Assert.Greater(medium.Min(rival => rival.points), easy.Max(rival => rival.points) - 1);
            Assert.IsTrue(hard.Contains(state.Rival("undertaker")));
            Assert.IsTrue(easy.Contains(state.Rival("terry")));
            // A bottom rival who scores big moves to the top third.
            state.Rival("terry").points = 500;
            Assert.IsTrue(Ladder.Third(state, Difficulty.Hard).Contains(state.Rival("terry")));
        }

        [Test]
        public void EveryDayOffersAnEasyAMediumAndAHardRaceFromTheRightThirds()
        {
            CareerState state = CareerTestKit.NewCareer();
            for (int day = 1; day <= 30; day++)
            {
                List<RaceOffer> offers = RaceOffers.Generate(state, day);
                Assert.AreEqual(new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard }, offers.Select(offer => offer.difficulty));
                foreach (RaceOffer offer in offers)
                {
                    Assert.AreEqual(3, offer.rivals.Count);
                    Assert.AreEqual(3, offer.rivals.Distinct().Count());
                    List<string> third = Ladder.Third(state, offer.difficulty).Select(rival => rival.id).ToList();
                    Assert.IsTrue(offer.rivals.All(third.Contains), $"the rivals of the {offer.difficulty} race are of its third");
                    Assert.Less(offer.track, state.tracksOpen, "only open tracks");
                    Assert.AreEqual(CareerRules.Laps(offer.difficulty, TrackLayouts.Of(offer.track).Laps), offer.laps);
                }
                Assert.AreEqual(3, offers.Select(offer => offer.track).Distinct().Count(), "three different tracks");
            }
        }

        [Test]
        public void TheSameDayAlwaysOffersTheSameRaces()
        {
            CareerState state = CareerTestKit.NewCareer();
            List<RaceOffer> first = RaceOffers.Generate(state, 5);
            List<RaceOffer> again = RaceOffers.Generate(state, 5);
            List<RaceOffer> other = RaceOffers.Generate(state, 6);
            Assert.AreEqual(first.Select(offer => string.Join(",", offer.rivals) + offer.track + offer.seed),
                again.Select(offer => string.Join(",", offer.rivals) + offer.track + offer.seed));
            Assert.AreNotEqual(first.Select(offer => offer.seed), other.Select(offer => offer.seed));
        }

        [Test]
        public void TheShowdownIsOfferedWhenThePlayerStartsTheDayFirst()
        {
            CareerState state = CareerTestKit.NewCareer();
            Assert.IsFalse(RaceOffers.ShowdownDue(state));
            Assert.AreEqual(3, RaceOffers.Generate(state, 1).Count);
            state.points = 999;
            Assert.IsTrue(RaceOffers.ShowdownDue(state));
            List<RaceOffer> offers = RaceOffers.Generate(state, 1);
            Assert.AreEqual(4, offers.Count);
            RaceOffer showdown = offers[3];
            Assert.IsTrue(showdown.IsShowdown);
            Assert.AreEqual(CareerRules.ShowdownTrack, showdown.track);
            Assert.AreEqual(Roster.WardenId, showdown.rivals[0]);
            Assert.AreEqual(new[] { "undertaker", "vex" }, showdown.rivals.Skip(1), "The Warden brings the two best rivals");
            state.champion = true;
            Assert.IsFalse(RaceOffers.ShowdownDue(state), "a champion is not challenged again");
        }

        [Test]
        public void TracksBeyondTheOpenOnesAreNeverOffered()
        {
            CareerState state = CareerTestKit.NewCareer();
            state.tracksOpen = 8;
            var tracks = new HashSet<int>();
            for (int day = 1; day <= 40; day++)
            {
                foreach (RaceOffer offer in RaceOffers.Generate(state, day))
                {
                    tracks.Add(offer.track);
                }
            }
            Assert.IsFalse(tracks.Contains(CareerRules.ShowdownTrack), "The Crucible is for the Showdown only");
            Assert.GreaterOrEqual(tracks.Count, 6, "the open tracks are all raced on");
            Assert.IsTrue(RaceOffers.Generate(state, 3).Where(offer => offer.difficulty == Difficulty.Hard).All(offer => offer.track >= 5),
                "a Hard race is on one of the tracks opened last");
        }
    }
}
