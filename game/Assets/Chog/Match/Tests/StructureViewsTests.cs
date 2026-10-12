using NUnit.Framework;
using Quantum;

namespace Chog.Match.Tests
{
    public sealed class StructureViewsTests
    {
        [Test]
        public void AFallenStructureIsAnnouncedFromTheSideOfWhoReadsIt()
        {
            Assert.AreEqual("ENEMY TOWER DESTROYED", StructureViews.NoticeFor(EStructureKind.Tower, 1, 0));
            Assert.AreEqual("YOUR TOWER FELL", StructureViews.NoticeFor(EStructureKind.Tower, 1, 1));
            Assert.AreEqual("ENEMY CORE DESTROYED", StructureViews.NoticeFor(EStructureKind.Core, 0, 1));
            Assert.AreEqual("YOUR CORE FELL", StructureViews.NoticeFor(EStructureKind.Core, 0, 0));
        }

        [Test]
        public void SomebodyWhoPlaysInNoTeamReadsItAsTeamZeroDoes()
        {
            Assert.AreEqual(StructureViews.NoticeFor(EStructureKind.Core, 1, 0), StructureViews.NoticeFor(EStructureKind.Core, 1, -1));
            Assert.AreEqual(StructureViews.NoticeFor(EStructureKind.Tower, 0, 0), StructureViews.NoticeFor(EStructureKind.Tower, 0, -1));
        }
    }
}
