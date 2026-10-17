using NUnit.Framework;
using UnityEngine;

namespace Chog.Match.Tests
{
    /// <summary>The rules of the sounds and of the camera shake, which are plain numbers.</summary>
    public sealed class ChogJuiceTests
    {
        [Test]
        public void OneClipPlaysAtMostTwiceInFiftyMilliseconds()
        {
            var never = new[] { -1f, -1f };
            Assert.IsTrue(ChogSoundRules.MayPlay(10f, never));
            Assert.IsTrue(ChogSoundRules.MayPlay(10f, new[] { 9.98f, -1f }), "a second play in the window");
            Assert.IsFalse(ChogSoundRules.MayPlay(10f, new[] { 9.99f, 9.97f }), "not a third");
            Assert.IsTrue(ChogSoundRules.MayPlay(10f, new[] { 9.99f, 9.94f }), "the older play left the window");
            Assert.AreEqual(2, ChogSoundRules.MaxPlaysInWindow);
            Assert.AreEqual(0.05f, ChogSoundRules.Window);
        }

        [Test]
        public void ThePitchVariesByEightPercentEitherWay()
        {
            Assert.AreEqual(0.92f, ChogSoundRules.Pitch(0f), 1e-5f);
            Assert.AreEqual(1.08f, ChogSoundRules.Pitch(1f), 1e-5f);
            Assert.AreEqual(1f, ChogSoundRules.Pitch(0.5f), 1e-5f);
            Assert.AreEqual(1.08f, ChogSoundRules.Pitch(3f), 1e-5f, "never beyond");
        }

        [Test]
        public void TheLocalHerosFightIsLouderThanTheOthers()
        {
            foreach (var sound in new[] { ChogSound.Hit, ChogSound.Death, ChogSound.Respawn, ChogSound.StructureHit })
            {
                Assert.Greater(ChogSoundRules.Volume(sound, true), ChogSoundRules.Volume(sound, false), sound.ToString());
                Assert.LessOrEqual(ChogSoundRules.Volume(sound, true), 1f);
                Assert.Greater(ChogSoundRules.Volume(sound, false), 0f);
            }
        }

        [Test]
        public void ARewardClimbsOneNoteAStepAndComesBackAfterAnOctave()
        {
            Assert.AreEqual(1f, ChogSoundRules.RewardPitch(0), 1e-5f);
            for (var step = 1; step < 8; step++)
                Assert.Greater(ChogSoundRules.RewardPitch(step), ChogSoundRules.RewardPitch(step - 1));
            Assert.AreEqual(2f, ChogSoundRules.RewardPitch(7), 1e-4f, "an octave up");
            Assert.AreEqual(1f, ChogSoundRules.RewardPitch(8), 1e-5f, "then from the start");
        }

        [Test]
        public void AStoredVolumeIsReadBetweenZeroAndOne()
        {
            Assert.AreEqual(0.5f, ChogSoundRules.Setting(-1f, 0.5f), "nothing stored");
            Assert.AreEqual(0.5f, ChogSoundRules.Setting(float.NaN, 0.5f));
            Assert.AreEqual(0f, ChogSoundRules.Setting(0f, 0.5f), "silence is a choice");
            Assert.AreEqual(1f, ChogSoundRules.Setting(4f, 0.5f));
            Assert.AreEqual(0.3f, ChogSoundRules.Setting(0.3f, 0.5f));
        }

        [Test]
        public void ADamageNumberPopsFromSixTenthsToOneAndAThirdAndSettlesInATenthOfASecond()
        {
            Assert.AreEqual(0.6f, ChogDamageNumbers.Pop(0f), 1e-5f);
            Assert.AreEqual(1.3f, ChogDamageNumbers.Pop(0.05f), 1e-5f, "the peak halfway");
            Assert.AreEqual(1f, ChogDamageNumbers.Pop(0.1f), 1e-5f);
            Assert.AreEqual(1f, ChogDamageNumbers.Pop(0.4f), 1e-5f);
            var highest = 0f;
            for (var t = 0f; t <= 0.1f; t += 0.005f)
                highest = Mathf.Max(highest, ChogDamageNumbers.Pop(t));
            Assert.LessOrEqual(highest, 1.3f + 1e-5f, "never beyond its peak");
        }

        [Test]
        public void ABiggerShareOfHealthMakesABiggerNumberWithinBounds()
        {
            Assert.AreEqual(ChogDamageNumbers.SmallestSize, ChogDamageNumbers.Size(0f));
            Assert.Greater(ChogDamageNumbers.Size(0.2f), ChogDamageNumbers.Size(0.05f));
            Assert.AreEqual(ChogDamageNumbers.LargestSize, ChogDamageNumbers.Size(1f));
            Assert.AreEqual(ChogDamageNumbers.SmallestSize, ChogDamageNumbers.Size(-3f));
        }

        [Test]
        public void BlowsOnOneTargetWithinAFifthOfASecondMakeOneNumber()
        {
            Assert.IsTrue(ChogDamageNumbers.Merges(1f, 1.15f));
            Assert.IsTrue(ChogDamageNumbers.Merges(1f, 1.2f));
            Assert.IsFalse(ChogDamageNumbers.Merges(1f, 1.25f));
            Assert.IsFalse(ChogDamageNumbers.Merges(-1f, 0.1f), "no number before");
        }

        [Test]
        public void TheColourOfANumberSaysWhoWasHit()
        {
            Assert.AreEqual(ChogDamageSide.LocalHero, ChogDamageNumbers.SideOf(true, 0, 0));
            Assert.AreEqual(ChogDamageSide.Ally, ChogDamageNumbers.SideOf(false, 0, 0));
            Assert.AreEqual(ChogDamageSide.Enemy, ChogDamageNumbers.SideOf(false, 1, 0));
            Assert.AreEqual(ChogDamageSide.Enemy, ChogDamageNumbers.SideOf(false, 0, -1), "a spectator sees no side of their own");
            Assert.AreNotEqual(ChogDamageNumbers.Colour(ChogDamageSide.LocalHero), ChogDamageNumbers.Colour(ChogDamageSide.Enemy));
            Assert.AreNotEqual(ChogDamageNumbers.Colour(ChogDamageSide.Ally), ChogDamageNumbers.Colour(ChogDamageSide.Enemy));
        }

        [Test]
        public void AFallenHeroSwellsThenAReturningOneGrowsPastItsSizeAndSettles()
        {
            Assert.AreEqual(1f, ChogHeroPop.Swell(0f), 1e-5f);
            Assert.AreEqual(ChogHeroPop.SwellTo, ChogHeroPop.Swell(ChogHeroPop.SwellSeconds), 1e-5f);
            Assert.Greater(ChogHeroPop.Swell(0.05f), 1f);

            Assert.AreEqual(0f, ChogHeroPop.ComeBack(0f), 1e-5f, "from nothing");
            Assert.AreEqual(1f, ChogHeroPop.ComeBack(ChogHeroPop.ComeBackSeconds), 1e-5f);
            Assert.AreEqual(1f, ChogHeroPop.ComeBack(5f), 1e-5f);
            var highest = 0f;
            for (var t = 0f; t < ChogHeroPop.ComeBackSeconds; t += 0.005f)
            {
                var size = ChogHeroPop.ComeBack(t);
                Assert.GreaterOrEqual(size, 0f, "never a negative size");
                highest = Mathf.Max(highest, size);
            }
            Assert.Greater(highest, 1.05f, "a Back ease goes past its size");
            Assert.Less(highest, 1.15f);
        }

        [Test]
        public void TheTrailOfAHealthBarWaitsThenDrainsAndHealingMovesItAtOnce()
        {
            var trail = new ChogBarTrail();
            trail.Reset(1f);
            Assert.AreEqual(1f, trail.Step(1f, 0f, 0.03f));

            // A blow takes 30%: the trail stays at full for HoldSeconds.
            Assert.AreEqual(1f, trail.Step(0.7f, 1f, 0.03f));
            Assert.AreEqual(1f, trail.Step(0.7f, 1f + ChogBarTrail.HoldSeconds - 0.01f, 0.03f));

            // Then it drains down to what is left, and never below.
            var shown = 1f;
            for (var t = 1f + ChogBarTrail.HoldSeconds; t < 3f; t += 1f / 32f)
            {
                var next = trail.Step(0.7f, t, 1f / 32f);
                Assert.LessOrEqual(next, shown);
                Assert.GreaterOrEqual(next, 0.7f);
                shown = next;
            }
            Assert.AreEqual(0.7f, shown, 1e-4f, "drained within two seconds");

            // Another blow while it drains makes it wait again.
            trail.Reset(1f);
            trail.Step(0.8f, 0f, 0.03f);
            trail.Step(0.8f, 0.5f, 0.03f);
            var before = trail.Shown;
            trail.Step(0.6f, 0.55f, 0.03f);
            Assert.AreEqual(before, trail.Step(0.6f, 0.7f, 0.03f), 1e-5f, "waits after the second blow");

            // Health given back above the trail: the trail follows at once.
            for (var t = 0.7f; t < 4f; t += 1f / 32f)
                trail.Step(0.6f, t, 1f / 32f);
            Assert.AreEqual(0.6f, trail.Shown, 1e-4f);
            Assert.AreEqual(0.9f, trail.Step(0.9f, 5f, 0.03f), 1e-5f);
        }

        [Test]
        public void AStructureThatIsHitWobblesLessThanAHeroAndSettles()
        {
            var hit = StructureViews.Jelly(0f);
            Assert.Less(hit.y, 1f);
            Assert.Greater(hit.x, 1f);
            Assert.Greater(hit.y, ChogBodyAnimator.FlinchHeight, "less than a hero");
            Assert.AreEqual(Vector3.one, StructureViews.Jelly(ChogBodyAnimator.FlinchSeconds));
            Assert.AreEqual(Vector3.one, StructureViews.Jelly(-1f));
        }

        [Test]
        public void TheShakeIsTheSquareOfATraumaThatDecays()
        {
            var hit = ChogCameraShake.AddTo(0f, ChogCameraShake.LocalHit);
            Assert.AreEqual(0.2f, hit, 1e-5f);
            Assert.AreEqual(0.04f, ChogCameraShake.Shake(hit), 1e-5f, "a hit barely moves the camera");
            var fall = ChogCameraShake.AddTo(hit, ChogCameraShake.BigBlow);
            Assert.AreEqual(0.7f, fall, 1e-5f);
            Assert.AreEqual(1f, ChogCameraShake.AddTo(fall, 0.5f), "never beyond 1");
            Assert.AreEqual(fall, ChogCameraShake.AddTo(fall, -1f), "a blow never calms it");

            // It is still after half a second, whatever the blows were.
            Assert.AreEqual(0f, ChogCameraShake.Decay(0.7f, 0.5f));
            Assert.Less(ChogCameraShake.Decay(0.7f, 0.1f), 0.7f);
        }

        [Test]
        public void TheCameraNeverMovesFartherThanTheLightestShakeAllows()
        {
            Assert.LessOrEqual(ChogCameraShake.MaxOffset, 0.5f, "a light shake");
            for (var time = 0f; time < 3f; time += 0.013f)
            {
                var offset = ChogCameraShake.Offset(1f, time);
                Assert.LessOrEqual(Mathf.Abs(offset.x), ChogCameraShake.MaxOffset + 1e-5f);
                Assert.LessOrEqual(Mathf.Abs(offset.y), ChogCameraShake.MaxOffset + 1e-5f);
                Assert.AreEqual(Vector2.zero, ChogCameraShake.Offset(0f, time));
            }
        }
    }
}
