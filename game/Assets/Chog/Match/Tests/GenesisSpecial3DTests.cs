using NUnit.Framework;
using Quantum;
using UnityEngine;

namespace Chog.Match.Tests
{
    public sealed class GenesisSpecial3DTests
    {
        private static readonly GenesisEyeLook Laser = new GenesisEyeLook
        {
            Outline = new Color(0.3f, 0.05f, 0.4f, 1f), Outer = new Color(0.8f, 0.25f, 0.95f, 1f), Core = new Color(1f, 0.7f, 1f, 1f)
        };

        private static readonly GenesisEyeLook Glasses3D = new GenesisEyeLook
        {
            Outline = new Color(0.05f, 0.05f, 0.2f, 1f), Outer = new Color(0.1f, 0.2f, 0.95f, 1f), Core = Color.white,
            Other = new Color(0.95f, 0.15f, 0.1f, 1f)
        };

        private static readonly GenesisEyeLook SideEye = new GenesisEyeLook
        {
            Outline = new Color(0.08f, 0.08f, 0.1f, 1f), Outer = new Color(0.08f, 0.08f, 0.1f, 1f), Core = Color.white,
            Glyph = GenesisEyeGlyph.BallLidded
        };

        private static readonly GenesisEyeLook Happy = new GenesisEyeLook
        {
            Outline = new Color(0.08f, 0.08f, 0.1f, 1f), Outer = new Color(0.08f, 0.08f, 0.1f, 1f), Core = Color.white,
            Glyph = GenesisEyeGlyph.Arc
        };

        private static readonly Color PinkSkin = new Color(0.95f, 0.6f, 0.75f, 1f);

        // Each triangle's front (Unity draws the side where its corners turn clockwise) faces along the
        // normals of its corners, except an outline shell's, which faces inwards.
        private static void AssertFacesOutward(EffectMeshDraft draft, string what)
        {
            for (var t = 0; t < draft.Triangles.Count; t += 3)
            {
                int a = draft.Triangles[t], b = draft.Triangles[t + 1], c = draft.Triangles[t + 2];
                var front = Vector3.Cross(draft.Vertices[b] - draft.Vertices[a], draft.Vertices[c] - draft.Vertices[a]);
                if (front.sqrMagnitude < 1e-12f)
                    continue;
                var normal = draft.Normals[a] + draft.Normals[b] + draft.Normals[c];
                var shell = draft.Flags[a].w > 0.5f;
                var dot = Vector3.Dot(front, normal);
                Assert.That(shell ? dot < 0f : dot > 0f, what + ": triangle " + t / 3 + (shell ? " of the outline" : "") + " faces the wrong way");
            }
        }

        [Test]
        public void EveryClosedShapeFacesOutwardsAndItsOutlineInwards()
        {
            AssertFacesOutward(GenesisSpecialMeshes.LaserDraft(Laser), "laser");
            AssertFacesOutward(GenesisSpecialMeshes.TearDraft(Laser), "tear");
            AssertFacesOutward(GenesisSpecialMeshes.EyeballDraft(SideEye, PinkSkin), "eyeball");
            AssertFacesOutward(GenesisSpecialMeshes.EyeballDraft(Happy, PinkSkin), "closed eyeball");
            AssertFacesOutward(GenesisSpecialMeshes.PuddleDraft(Laser, 3f), "puddle");
            AssertFacesOutward(GenesisSpecialMeshes.FanDraft(Glasses3D, 3.7f, 32f), "fan of light");
            AssertFacesOutward(GenesisSpecialMeshes.SparkDraft(Color.white, Color.black), "spark");
            AssertFacesOutward(GenesisSpecialMeshes.GlowBallDraft(Laser.Outer, Laser.Core), "glow");
        }

        [Test]
        public void ATearIsRoundBelowAndPointedAbove()
        {
            var profile = GenesisSpecialMeshes.TearProfile(14);
            Assert.That(profile[0].x, Is.EqualTo(0f).Within(1e-4f), "it closes at its bottom");
            Assert.That(profile[0].y, Is.EqualTo(-1f).Within(1e-4f));
            Assert.That(profile[profile.Length - 1].x, Is.EqualTo(0f).Within(1e-4f), "and at its tip");
            Assert.That(profile[profile.Length - 1].y, Is.EqualTo(1f).Within(1e-4f));
            var widest = 0;
            for (var k = 0; k < profile.Length; k++)
                if (profile[k].x > profile[widest].x)
                    widest = k;
            Assert.That(profile[widest].y, Is.LessThan(0f), "its widest part is in its lower half");
            Assert.That(profile[widest].x, Is.GreaterThan(0.7f));
        }

        [Test]
        public void APictureOnABallKeepsItsDirectionsAndItsMiddleInFront()
        {
            Assert.That(EffectMeshDraft.PictureDirection(Vector2.zero), Is.EqualTo(Vector3.forward));
            var rim = EffectMeshDraft.PictureDirection(new Vector2(0f, 1f));
            Assert.That(Vector3.Angle(Vector3.forward, rim), Is.EqualTo(EffectMeshDraft.PictureRimDegrees).Within(0.01f));
            Assert.That(rim.y, Is.GreaterThan(0f), "the top of the picture is at the top of the ball");
            // Seen from in front of the ball (from +Z), its right is -X: the picture's right lands there.
            Assert.That(EffectMeshDraft.PictureDirection(new Vector2(0.5f, 0f)).x, Is.LessThan(0f));
            // What the shader reads back for each pixel is the point that was laid there.
            foreach (var point in new[] { new Vector2(0.3f, 0.4f), new Vector2(-0.7f, 0.1f), new Vector2(0f, -0.95f) })
            {
                var back = EffectMeshDraft.PictureAt(EffectMeshDraft.PictureDirection(point));
                Assert.That(back.x, Is.EqualTo(point.x).Within(1e-3f));
                Assert.That(back.y, Is.EqualTo(point.y).Within(1e-3f));
            }
            Assert.That(EffectMeshDraft.PictureAt(Vector3.back).magnitude, Is.GreaterThan(1f), "the back of the ball wears nothing");
        }

        [Test]
        public void AnEyeballIsOneBallThatWearsItsPictureAndAnOutline()
        {
            var draft = GenesisSpecialMeshes.EyeballDraft(SideEye, PinkSkin);
            int wearing = 0, outline = 0;
            for (var v = 0; v < draft.VertexCount; v++)
            {
                if (draft.Flags[v].w > 0.5f)
                    outline++;
                else if (draft.Cores[v].w > 0.5f)
                    wearing++;
            }
            Assert.That(wearing, Is.GreaterThan(100), "the ball wears the picture");
            Assert.That(outline, Is.GreaterThan(50));
        }

        [Test]
        public void ThePictureOfAnEyeIsTheEyeInItsMiddleAndNothingAtItsCorners()
        {
            const int size = 32;
            var pixels = GenesisSpecialMeshes.EyePictureDraft(SideEye, PinkSkin, false, size);
            Assert.That(pixels.Length, Is.EqualTo(size * size));
            // A lidded ball: its dark lower half in the middle of the picture, nothing in a corner.
            var below = pixels[(size / 2 - 3) * size + size / 2];
            Assert.That(below.a, Is.EqualTo(255));
            Assert.That(below.r, Is.LessThan(60), "the dark of the eye");
            Assert.That(pixels[0].a, Is.EqualTo(0));
            Assert.That(pixels[size * size - 1].a, Is.EqualTo(0));

            // A heart: its red, painted over its dark outline, covers the middle; a glint of white sits on it.
            var heart = new GenesisEyeLook { Outer = new Color(0.9f, 0.12f, 0.15f, 1f), Core = Color.white, Glyph = GenesisEyeGlyph.Heart };
            var drawn = GenesisSpecialMeshes.EyePictureDraft(heart, PinkSkin, false, size);
            var middle = drawn[(size / 2 - 2) * size + size / 2];
            Assert.That(middle.r, Is.GreaterThan(200), "the red of the heart, over its outline");
            Assert.That(middle.g, Is.LessThan(80));
        }

        [Test]
        public void AnOpenEyeIsAWhiteBallAndAClosedOneIsTheSkin()
        {
            var open = GenesisEyes.Sclera(SideEye, PinkSkin);
            Assert.That(open.r, Is.GreaterThan(0.9f));
            Assert.That(open.g, Is.GreaterThan(0.9f));
            Assert.That(GenesisEyes.Sclera(Happy, PinkSkin), Is.EqualTo(GenesisEyes.Backing(PinkSkin, Happy)));
            Assert.That(GenesisEyes.IsClosed(GenesisEyeGlyph.Arc), Is.True);
            Assert.That(GenesisEyes.IsClosed(GenesisEyeGlyph.Heart), Is.False);
        }

        [Test]
        public void TheFanOfLightStaysInsideItsAreaAndBelowTheHeightOfAHero()
        {
            const float radius = 3.7f;
            const float half = 32f;
            var draft = GenesisSpecialMeshes.FanDraft(Glasses3D, radius, half);
            foreach (var at in draft.Vertices)
            {
                var flat = new Vector2(at.x, at.z);
                Assert.That(flat.magnitude, Is.LessThanOrEqualTo(radius + 0.08f));
                if (flat.magnitude > 0.2f)
                    Assert.That(Mathf.Abs(Mathf.Atan2(at.x, at.z) * Mathf.Rad2Deg), Is.LessThanOrEqualTo(half + 2f));
                Assert.That(at.y, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(GenesisSpecialMeshes.FanRimHeight + 0.08f));
            }
        }

        [Test]
        public void TheFanTakesTheVisorsColoursFromOneSideToTheOther()
        {
            AssertClose(GenesisSpecialMeshes.Across(Glasses3D, 0f), Glasses3D.Outer);
            AssertClose(GenesisSpecialMeshes.Across(Glasses3D, 0.5f), Glasses3D.Core);
            AssertClose(GenesisSpecialMeshes.Across(Glasses3D, 1f), Glasses3D.Other);
            AssertClose(GenesisSpecialMeshes.Across(Laser, 1f), Laser.Outer);
        }

        private static void AssertClose(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1e-4f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1e-4f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1e-4f));
        }

        [Test]
        public void APuddleBulgesInItsMiddleAndMeetsTheFloorAtItsRim()
        {
            Assert.That(GenesisSpecialMeshes.PuddleBulge(0f), Is.EqualTo(GenesisSpecialMeshes.PuddleHeight).Within(1e-5f));
            Assert.That(GenesisSpecialMeshes.PuddleBulge(1f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(GenesisSpecialMeshes.PuddleBulge(0.5f), Is.LessThan(GenesisSpecialMeshes.PuddleHeight));
            for (var angle = 0f; angle < Mathf.PI * 2f; angle += 0.1f)
                Assert.That(GenesisSpecialMeshes.PuddleRim(angle), Is.InRange(0.85f, 1.15f), "its rim waves, a little");
        }

        [Test]
        public void TheOutlineOfAnEnemysSpecialIsItsOwnColourExactly()
        {
            var enemy = Laser;
            enemy.Outline = new Color(0.95f, 0.1f, 0.1f, 1f);
            var draft = GenesisSpecialMeshes.LaserDraft(enemy);
            var shells = 0;
            for (var v = 0; v < draft.VertexCount; v++)
                if (draft.Flags[v].w > 0.5f)
                {
                    shells++;
                    Assert.That(draft.Colours[v], Is.EqualTo(enemy.Outline));
                }
            Assert.That(shells, Is.GreaterThan(0));
            Assert.That(GenesisSpecialMeshes.OutlineOf(new GenesisEyeLook()).a, Is.EqualTo(1f), "a look without an outline gets an ink");
        }

        [Test]
        public void TheAreaOfAStareTakesTheAuraElseTheSkin()
        {
            var aura = new Color(0.2f, 0.9f, 0.9f, 1f);
            Assert.That(GenesisSpecialDrawing.AreaLook(SideEye, PinkSkin, aura).Outer, Is.EqualTo(aura));
            Assert.That(GenesisSpecialDrawing.AreaLook(SideEye, PinkSkin, Color.clear).Outer, Is.EqualTo(PinkSkin));
            Assert.That(GenesisSpecialDrawing.IsExpression(GenesisVisualKind.EyeBurst), Is.True);
            Assert.That(GenesisSpecialDrawing.IsExpression(GenesisVisualKind.Beam), Is.False);
        }

        [Test]
        public void TheTestPageReadsItsGenesisAndPlaysEachShapeAsItsDataSays()
        {
            Assert.That(ChogSpecialsViewer.Asked("1").Length, Is.EqualTo(5), "one Genesis of each shape");
            Assert.That(ChogSpecialsViewer.Asked("1287,16,x,16,-3"), Is.EqualTo(new[] { 1287, 16 }));

            var beam = SpecialLoop.For("beam");
            Assert.That(beam.AheadAt(beam.Seconds), Is.EqualTo(0.5f + 33f * 0.34f).Within(1e-3f), "a laser reaches about 11 units");
            Assert.That(beam.AheadAt(5f), Is.EqualTo(beam.AheadAt(beam.Seconds)), "and no farther");
            Assert.That(beam.Showcase, Is.LessThan(beam.Seconds));

            var lob = SpecialLoop.For("puddle").With(5f, 0f, 0f, 3f, 2.5f);
            Assert.That(lob.Seconds, Is.EqualTo(2f).Within(1e-4f), "a tear flies its reach at its own speed");
            Assert.That(lob.AheadAt(10f), Is.EqualTo(lob.Reach).Within(1e-4f), "and lands where it was thrown");
            Assert.That(lob.ZoneSeconds, Is.EqualTo(2.5f));
            Assert.That(lob.Period, Is.GreaterThan(lob.Seconds));

            Assert.That(SpecialLoop.KindOf("wave"), Is.EqualTo(GenesisVisualKind.Stare));
            Assert.That(SpecialLoop.KindOf("nova"), Is.EqualTo(GenesisVisualKind.EyeBurst));
        }

        [Test]
        public void PoppingAndEasingStartAtZeroAndSettleAtOne()
        {
            Assert.That(GenesisSpecial3D.Pop(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(GenesisSpecial3D.Pop(1f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(GenesisSpecial3D.Pop(0.7f), Is.GreaterThan(1f), "it overshoots before it settles");
            Assert.That(GenesisSpecial3D.EaseOut(0.5f), Is.GreaterThan(0.5f));
            Assert.That(GenesisSpecial3D.EaseOut(2f), Is.EqualTo(1f));
        }
    }
}
