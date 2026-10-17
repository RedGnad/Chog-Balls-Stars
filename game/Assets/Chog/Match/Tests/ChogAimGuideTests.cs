using NUnit.Framework;
using UnityEngine;

namespace Chog.Match.Tests
{
    public sealed class ChogAimGuideTests
    {
        [Test]
        public void TheAimOfALobFollowsThePathTheThrowFlies()
        {
            var hero = new Vector3(2f, 0f, -1f);
            var landing = new Vector3(8f, 0f, 7f);
            var length = 10f;
            var top = ChogAttackLook.LobHeight(length);
            foreach (var done in new[] { 0f, 0.2f, 0.5f, 0.8f, 1f })
            {
                var point = ChogAimGuide.ArcPoint(hero, landing, done);
                Assert.That(point.y, Is.EqualTo(ChogAimGuide.HandHeight + ChogAttackLook.LobRise(top, done)).Within(1e-4f));
                Assert.That(point.x, Is.EqualTo(Mathf.Lerp(hero.x, landing.x, done)).Within(1e-4f));
                Assert.That(point.z, Is.EqualTo(Mathf.Lerp(hero.z, landing.z, done)).Within(1e-4f));
            }
        }

        [Test]
        public void TheAimLeavesFromTheHandsAndEndsAboveTheLanding()
        {
            var hero = new Vector3(0f, 0f, 0f);
            var landing = new Vector3(0f, 0f, 6f);
            Assert.That(ChogAimGuide.ArcPoint(hero, landing, 0f), Is.EqualTo(new Vector3(0f, ChogAimGuide.HandHeight, 0f)));
            Assert.That(ChogAimGuide.ArcPoint(hero, landing, 1f), Is.EqualTo(new Vector3(0f, ChogAimGuide.HandHeight, 6f)));
            Assert.That(ChogAimGuide.ArcPoint(hero, landing, 0.5f).y,
                Is.EqualTo(ChogAimGuide.HandHeight + ChogAttackLook.LobHeight(6f)).Within(1e-4f));
        }

        [Test]
        public void TheDotsStayOnTheArcAndMoveTowardTheLanding()
        {
            for (var i = 0; i < 12; i++)
            {
                var place = ChogAimGuide.DotPlace(i, 0.1f);
                Assert.That(place, Is.InRange(0f, 1f));
            }
            Assert.That(ChogAimGuide.DotPlace(3, 0.2f), Is.GreaterThan(ChogAimGuide.DotPlace(3, 0.1f)));
        }
    }
}
