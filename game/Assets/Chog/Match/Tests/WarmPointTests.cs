using NUnit.Framework;
using UnityEngine;

namespace Chog.Match.Tests
{
    public sealed class WarmPointTests
    {
        [Test]
        public void TheWarmUpIsDrawnUnderTheFloorAlongTheCameraLine()
        {
            var camera = new Vector3(0f, 18f, -5f);
            var forward = new Vector3(0f, -0.966f, 0.259f);
            Assert.That(ChogFxRunner.WarmPoint(camera, forward, out var at), Is.True);
            Assert.That(at.y, Is.EqualTo(-3f).Within(1e-3f));
            // On the camera's line of sight, so inside its view.
            var toPoint = (at - camera).normalized;
            Assert.That(Vector3.Dot(toPoint, forward.normalized), Is.GreaterThan(0.999f));
        }

        [Test]
        public void ACameraThatDoesNotLookDownWarmsNothing()
        {
            Assert.That(ChogFxRunner.WarmPoint(Vector3.up * 5f, Vector3.forward, out _), Is.False);
        }
    }
}
