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

        [Test]
        public void AStructureThatIsHitIsNamedInTheAlarm()
        {
            Assert.AreEqual("YOUR TOWER IS UNDER ATTACK", StructureViews.AlarmFor(EStructureKind.Tower));
            Assert.AreEqual("YOUR CORE IS UNDER ATTACK", StructureViews.AlarmFor(EStructureKind.Core));
        }

        [Test]
        public void TheClockOfASiegeReadsMinutesAndSecondsRoundedUp()
        {
            Assert.AreEqual("3:00", StructureViews.ClockText(180f));
            Assert.AreEqual("2:41", StructureViews.ClockText(160.2f));
            Assert.AreEqual("0:09", StructureViews.ClockText(8.5f));
            Assert.AreEqual("0:00", StructureViews.ClockText(-3f), "a siege whose time ran out never reads below zero");
        }
    }
}
