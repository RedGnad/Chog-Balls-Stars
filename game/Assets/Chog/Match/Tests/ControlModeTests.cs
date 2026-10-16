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

        [Test]
        public void AClickIsSentAsAPressThenARelease()
        {
            var pulse = new PressPulse();
            Assert.IsTrue(pulse.Next(true, 100));
            Assert.IsFalse(pulse.Next(false, 101));
            Assert.IsFalse(pulse.Next(false, 102));
        }

        [Test]
        public void AHeldButtonAttacksEveryOtherTick()
        {
            var pulse = new PressPulse();
            var sent = new bool[6];
            for (var tick = 0; tick < sent.Length; tick++)
                sent[tick] = pulse.Next(true, 40 + tick);
            CollectionAssert.AreEqual(new[] { true, false, true, false, true, false }, sent);
        }

        [Test]
        public void ATickAskedForAgainGetsTheSameAnswer()
        {
            var pulse = new PressPulse();
            Assert.IsTrue(pulse.Next(true, 7));
            Assert.IsTrue(pulse.Next(true, 7));
            Assert.IsFalse(pulse.Next(true, 8));
            Assert.IsFalse(pulse.Next(true, 8));
            Assert.IsTrue(pulse.Next(true, 9));
        }

        [Test]
        public void LettingGoInTheMiddleSendsTheReleaseAndStartsAgainOnTheNextPress()
        {
            var pulse = new PressPulse();
            Assert.IsTrue(pulse.Next(true, 1));
            Assert.IsFalse(pulse.Next(false, 2));
            Assert.IsTrue(pulse.Next(true, 3));
        }
    }
}
