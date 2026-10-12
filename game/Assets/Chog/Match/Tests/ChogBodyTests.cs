using NUnit.Framework;
using UnityEngine;

namespace Chog.Match.Tests
{
    public sealed class ChogBodyTests
    {
        private const float Quarter = Mathf.PI / 2f;

        [Test]
        public void AStandingBodyKeepsItsArmsDownAndOnlyBreathes()
        {
            for (var time = 0f; time < 3f; time += 0.37f)
            {
                var pose = ChogBodyAnimator.Pose(0f, 0f, 0f, 0f, time);
                Assert.AreEqual(Vector3.zero, pose.ArmLeftTurn);
                Assert.AreEqual(Vector3.zero, pose.ArmRightTurn);
                Assert.AreEqual(Vector3.zero, pose.BodyTurn);
                Assert.Less(Mathf.Abs(pose.BodyOffset.y), 0.02f, "breathing is a few millimetres");
                Assert.AreEqual(0f, pose.BodyOffset.z);
            }
        }

        [Test]
        public void ARunningBodySwingsItsArmsAgainstEachOtherAndLeansForward()
        {
            var pose = ChogBodyAnimator.Pose(Quarter, 1f, 0f, 0f, 0f);
            Assert.Greater(pose.ArmLeftTurn.x, 40f);
            Assert.AreEqual(-pose.ArmLeftTurn.x, pose.ArmRightTurn.x, 0.001f);
            Assert.Greater(pose.BodyTurn.x, 0f, "leans into the run");
            Assert.Greater(pose.BodyOffset.y, 0.05f, "lifted at mid-step");

            // Half a stride later the arms have swapped, and the body is lifted again.
            var later = ChogBodyAnimator.Pose(3f * Quarter, 1f, 0f, 0f, 0f);
            Assert.AreEqual(pose.ArmLeftTurn.x, later.ArmRightTurn.x, 0.001f);
            Assert.AreEqual(pose.BodyOffset.y, later.BodyOffset.y, 0.001f);

            // A slow walk moves the same way, less.
            var walk = ChogBodyAnimator.Pose(Quarter, 0.3f, 0f, 0f, 0f);
            Assert.Less(walk.ArmLeftTurn.x, pose.ArmLeftTurn.x);
            Assert.Greater(walk.ArmLeftTurn.x, 0f);
        }

        [Test]
        public void AnAttackThrowsBothArmsForwardOnceAndBringsThemBack()
        {
            var peak = ChogBodyAnimator.Pose(0f, 0f, 0.25f, 0f, 0f);
            Assert.Less(peak.ArmLeftTurn.x, -90f, "a negative turn about X is forward for a hanging arm");
            Assert.AreEqual(peak.ArmLeftTurn.x, peak.ArmRightTurn.x, 0.001f, "together, not against each other");
            Assert.Greater(peak.BodyOffset.z, 0.1f, "the body goes with the blow");

            Assert.AreEqual(Vector3.zero, ChogBodyAnimator.Pose(0f, 0f, 0f, 0f, 0f).ArmLeftTurn, "before");
            Assert.AreEqual(Vector3.zero, ChogBodyAnimator.Pose(0f, 0f, 1f, 0f, 0f).ArmLeftTurn, "after");
            var late = ChogBodyAnimator.Pose(0f, 0f, 0.8f, 0f, 0f);
            Assert.Greater(late.ArmLeftTurn.x, peak.ArmLeftTurn.x, "on the way back");
            Assert.Less(late.ArmLeftTurn.x, 0f);
        }

        [Test]
        public void TheStandInBodyIsOnlyATestOptionOfPrivatePages()
        {
            Assert.IsTrue(ChogBody.StandInAsked("http://192.168.1.85:8083/?token=1795&body=standin", false));
            Assert.IsFalse(ChogBody.StandInAsked("http://192.168.1.85:8083/?token=1795", false));
            Assert.IsFalse(ChogBody.StandInAsked("http://192.168.1.85:8083/?body=other", false));
            Assert.IsFalse(ChogBody.StandInAsked("https://game.example/?body=standin", false), "ignored on a public address");
        }

        [Test]
        public void AWinRaisesTheArmsAndALossFoldsTheBody()
        {
            var won = ChogBodyAnimator.Pose(0f, 0f, 0f, 1f, 0f);
            Assert.Less(won.ArmLeftTurn.z, -100f);
            Assert.Greater(won.ArmRightTurn.z, 100f);
            Assert.AreEqual(0f, won.BodyTurn.x, "upright");

            var lost = ChogBodyAnimator.Pose(0f, 0f, 0f, -1f, 0f);
            Assert.Greater(lost.BodyTurn.x, 20f);
            Assert.Greater(lost.HeadTurn.x, 20f);
            Assert.Less(lost.BodyOffset.y, 0f);
            Assert.AreEqual(0f, lost.ArmLeftTurn.z, "arms are not raised");
        }

        [Test]
        public void TheLayoutOfAHeroCanBeTriedFromAPrivatePageOnly()
        {
            var asked = GenesisLook.For("http://192.168.1.85:8083/?token=1795&look=0.9,0.4,1.6", false);
            Assert.AreEqual(0.9f, asked.FaceSize);
            Assert.AreEqual(0.4f, asked.FaceLift);
            Assert.AreEqual(1.6f, asked.BodyScale);

            var standard = GenesisLook.Default;
            var ignored = GenesisLook.For("https://game.example/?look=0.9,0.4,1.6", false);
            Assert.AreEqual(standard.FaceSize, ignored.FaceSize, "ignored on a public address");
            Assert.AreEqual(standard.BodyScale, ignored.BodyScale);
            Assert.AreEqual(standard.FaceSize, GenesisLook.For("http://192.168.1.85:8083/", false).FaceSize);

            // The head never hides the whole body: it is smaller than the body is wide once enlarged.
            Assert.Less(standard.FaceSize, 1.3f);
            Assert.Greater(standard.BodyScale, 1f);
        }

        [Test]
        public void ALayoutThatCannotBeDrawnIsRefused()
        {
            Assert.IsTrue(GenesisLook.TryParse("1,-0.5,1", out var look));
            Assert.AreEqual(-0.5f, look.FaceLift);
            foreach (var bad in new[] { null, "", "1", "1,2", "1,2,3,4", "a,b,c", "0,0,1", "1,0,9", "1,5,1", "1;0;1" })
                Assert.IsFalse(GenesisLook.TryParse(bad, out _), bad ?? "null");
        }

        [Test]
        public void TheBodyTakesTheSkinColourExceptItsMittensAndItsOutline()
        {
            var skin = new Color(0.8f, 0.1f, 0.1f);
            Assert.AreEqual(skin, ChogBody.PartColour("ChogSkin", skin));
            Assert.AreEqual(ChogBody.GloveColour, ChogBody.PartColour("ChogGlove", skin));
            Assert.AreEqual(ChogBody.OutlineColour, ChogBody.PartColour("ChogOutline", skin));
            // A copy Unity makes of a material keeps its name with a suffix.
            Assert.AreEqual(ChogBody.GloveColour, ChogBody.PartColour("ChogGlove (Instance)", skin));
            // The model of a class of the sample, or a material without a name, is all skin.
            Assert.AreEqual(skin, ChogBody.PartColour("character_knight", skin));
            Assert.AreEqual(skin, ChogBody.PartColour(null, skin));
        }
    }
}
