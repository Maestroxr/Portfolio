using Gamebox;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    public class ProjectTest
    {
        [Test]
        public void TheGameHasItsOwnType()
        {
            Assert.AreEqual(GameType.DeathRace, L.Game);
            Assert.AreEqual("DeathRace", Loc.Table(L.Game));
        }
    }
}
