using NUnit.Framework;
using UnityEngine;

namespace Chog.Match.Tests
{
    public sealed class StructureFacingTests
    {
        [Test]
        public void TheTwoTowersFaceEachOtherAcrossTheArena()
        {
            // Where Structures (simulation) puts the towers of team 0 and team 1.
            var ours = StructureViews.TowardTheMiddle(new Vector3(0.5f, 0f, -8.5f));
            var theirs = StructureViews.TowardTheMiddle(new Vector3(-0.5f, 0f, 8.5f));
            Assert.That(ours.z, Is.GreaterThan(0.99f));
            Assert.That(theirs.z, Is.LessThan(-0.99f));
            Assert.That(ours.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void AStructureAtTheVeryMiddleHasNoWayToTheMiddle()
        {
            Assert.That(StructureViews.TowardTheMiddle(new Vector3(0f, 2f, 0f)), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void TheCoreIsDrawnLargerThanItsModel()
        {
            Assert.That(StructureViews.CoreScale, Is.GreaterThan(1.2f));
        }
    }
}
