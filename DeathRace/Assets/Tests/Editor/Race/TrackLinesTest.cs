using Gamebox.Racing;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The computer's drivers on the game's real tracks: a line on the road, faster than the middle, a spread of skill.</summary>
    public class TrackLinesTest
    {
        [Test]
        public void EveryTrackHasALineOnTheRoadThatBeatsTheMiddle()
        {
            var handling = new CarHandling(new CarLoadout { model = CarModel.Viper }.Stats());
            for (int index = 0; index < TrackLayouts.Count; index++)
            {
                TrackLayout layout = TrackLayouts.Of(index);
                var path = new RacePath(layout.Points, true, 2f);
                float half = layout.Width * 0.5f;
                RacingLine line = RacingLine.Build(path, d => half);
                for (int i = 0; i < line.Count; i++)
                {
                    Assert.LessOrEqual(Mathf.Abs(line.OffsetOf(i)), half - RacingLine.DefaultMargin + 1e-3f, $"{layout.Name} sample {i}");
                }
                float onLine = new SpeedProfile(line, handling, 1f, 1f).LapTime(handling);
                float inMiddle = new SpeedProfile(RacingLine.Centre(path), handling, 1f, 1f).LapTime(handling);
                Assert.Less(onLine, inMiddle, layout.Name);
            }
        }

        [Test]
        public void SkillShowsInTheLapTimesOnEveryTrack()
        {
            var handling = new CarHandling(new CarLoadout { model = CarModel.Rustbucket }.Stats());
            for (int index = 0; index < TrackLayouts.Count; index++)
            {
                TrackLayout layout = TrackLayouts.Of(index);
                var path = new RacePath(layout.Points, true, 2f);
                RacingLine line = RacingLine.Build(path, d => layout.Width * 0.5f);
                float Lap(float skill)
                {
                    var driver = new AiDriver(skill, 3);
                    driver.Prepare(line, handling, d => layout.Width * 0.5f);
                    return driver.PlannedLapTime();
                }
                float easy = Lap(0.2f);
                float medium = Lap(0.5f);
                float hard = Lap(0.8f);
                Assert.Greater(easy, medium * 1.04f, $"{layout.Name}: easy {easy:0.00} medium {medium:0.00}");
                Assert.Greater(medium, hard * 1.04f, $"{layout.Name}: medium {medium:0.00} hard {hard:0.00}");
                Assert.Less(easy, hard * 1.4f, $"{layout.Name}: even a beginner keeps up somewhat");
            }
        }
    }
}
