using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class ControlModeTests
    {
        [Test]
        public void APhoneStartsWithTheSticksAndAComputerWithTheMouse()
        {
            Assert.AreEqual(ControlMode.Touch, new ControlModeRule(true).Mode);
            Assert.AreEqual(ControlMode.Desktop, new ControlModeRule(false).Mode);
        }

        [Test]
        public void AFingerBringsTheSticksAndTheMouseTakesThemAway()
        {
            var rule = new ControlModeRule(false);
            Assert.IsTrue(rule.Observe(ControlInput.Finger, 10f));
            Assert.AreEqual(ControlMode.Touch, rule.Mode);
            Assert.IsTrue(rule.Observe(ControlInput.Mouse, 20f));
            Assert.AreEqual(ControlMode.Desktop, rule.Mode);
            Assert.IsTrue(rule.Observe(ControlInput.Finger, 30f));
            Assert.IsTrue(rule.Observe(ControlInput.Key, 40f));
            Assert.AreEqual(ControlMode.Desktop, rule.Mode);
        }

        [Test]
        public void TheMouseEventsABrowserMakesUpAfterATapAreNotAMouse()
        {
            var rule = new ControlModeRule(true);
            rule.Observe(ControlInput.Finger, 5f);
            Assert.IsFalse(rule.Observe(ControlInput.Mouse, 5f));
            Assert.IsFalse(rule.Observe(ControlInput.Mouse, 5f + ControlModeRule.EchoSeconds * 0.9f));
            Assert.AreEqual(ControlMode.Touch, rule.Mode);
            Assert.IsTrue(rule.Observe(ControlInput.Mouse, 5f + ControlModeRule.EchoSeconds));
        }

        [Test]
        public void AFingerHeldOnAStickKeepsTheSticks()
        {
            var rule = new ControlModeRule(true);
            for (var time = 0f; time < 5f; time += 0.1f)
            {
                rule.Observe(ControlInput.Finger, time);
                rule.Observe(ControlInput.Mouse, time);
            }
            Assert.AreEqual(ControlMode.Touch, rule.Mode);
        }

        [Test]
        public void SomethingUsedAgainInTheSameModeChangesNothing()
        {
            var rule = new ControlModeRule(false);
            Assert.IsFalse(rule.Observe(ControlInput.Mouse, 1f));
            Assert.IsFalse(rule.Observe(ControlInput.Key, 2f));
            Assert.AreEqual(ControlMode.Desktop, rule.Mode);
        }
    }
}
