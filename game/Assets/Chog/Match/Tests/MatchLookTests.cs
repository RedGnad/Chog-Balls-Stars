using NUnit.Framework;
using Part = Chog.Match.MatchLookParts.Part;

namespace Chog.Match.Tests
{
    public sealed class MatchLookTests
    {
        [Test]
        public void APageWithoutTheOptionDrawsEveryPartWithPostProcessingLeftToTheQuality()
        {
            Assert.AreEqual(Part.Shade | Part.Light | Part.Ring | Part.Post, MatchLookParts.Parse(null));
            Assert.AreEqual(MatchLookParts.Default, MatchLookParts.Parse(""));
            Assert.AreEqual(0, (int)(MatchLookParts.Parse(null) & Part.PostForced));
        }

        [Test]
        public void ZeroIsTheLookOfBefore()
        {
            Assert.AreEqual(Part.None, MatchLookParts.Parse("0"));
            Assert.AreEqual(Part.None, MatchLookParts.Parse("none"));
            Assert.AreEqual(Part.None, MatchLookParts.Parse("OLD"));
        }

        [Test]
        public void NamedPartsAreTheOnlyOnesAndNamingPostForcesIt()
        {
            Assert.AreEqual(Part.Shade | Part.Ring, MatchLookParts.Parse("shade,ring"));
            Assert.AreEqual(Part.Light | Part.Post | Part.PostForced, MatchLookParts.Parse("light, post"));
            Assert.AreEqual(MatchLookParts.Default | Part.PostForced, MatchLookParts.Parse("all"));
        }

        [Test]
        public void AMinusRemovesOnePartFromTheDefault()
        {
            Assert.AreEqual(Part.Shade | Part.Light | Part.Ring, MatchLookParts.Parse("-post"));
            Assert.AreEqual(Part.Shade | Part.Light | Part.Post, MatchLookParts.Parse("-ring"));
            Assert.AreEqual(Part.Shade, MatchLookParts.Parse("shade,light,-light"));
        }

        [Test]
        public void PostProcessingStartsAtTheLevelGoodOfTheProjectsList()
        {
            var names = new[] { "Fastest", "Fast", "Simple", "Good", "Beautiful", "Fantastic" };
            Assert.IsFalse(MatchLookParts.PostAllowed(1, names, "Good"), "a phone starts at Fast");
            Assert.IsFalse(MatchLookParts.PostAllowed(2, names, "Good"));
            Assert.IsTrue(MatchLookParts.PostAllowed(3, names, "Good"), "a computer starts at Good");
            Assert.IsTrue(MatchLookParts.PostAllowed(5, names, "good"));
            Assert.IsFalse(MatchLookParts.PostAllowed(5, new[] { "Low", "High" }, "Good"));
        }

        [Test]
        public void UnknownNamesAreLeftOut()
        {
            Assert.AreEqual(MatchLookParts.Default, MatchLookParts.Parse("sparkles"));
            Assert.AreEqual(Part.Ring, MatchLookParts.Parse("ring,sparkles"));
        }
    }

    public sealed class MatchLookTuningTests
    {
        [Test]
        public void TheOptionLaysNumbersAndColoursOverTheDefaults()
        {
            var tuning = MatchLookTuning.Parse("exposure:1.3,arenashade:FF0000,rim:0.5,nonsense:2,slope:abc");
            Assert.AreEqual(1.3f, tuning.Exposure, 1e-5);
            Assert.AreEqual((1f, 0f, 0f), tuning.ArenaShade);
            Assert.AreEqual(0.5f, tuning.Rim, 1e-5);
            Assert.AreEqual(new MatchLookTuning().Slope, tuning.Slope, "a value that is not a number is left out");
        }

        [Test]
        public void NumbersAreKeptInRangeAndTheKeyHasLengthOne()
        {
            var tuning = MatchLookTuning.Parse("warmth:7,lightx:3,lighty:0,lightz:4");
            Assert.AreEqual(1f, tuning.Warmth, 1e-5);
            var (x, y, z) = tuning.LightDirection();
            Assert.AreEqual(0.6f, x, 1e-5);
            Assert.AreEqual(0f, y, 1e-5);
            Assert.AreEqual(0.8f, z, 1e-5);
            var (dx, dy, dz) = MatchLookTuning.Parse("lightx:0,lighty:0,lightz:0").LightDirection();
            Assert.AreEqual(1f, dx * dx + dy * dy + dz * dz, 1e-4, "three zeros give the default direction");
        }
    }

    public sealed class CameraAnglesTests
    {
        [Test]
        public void WithoutTheOptionTheCameraIsTheSamples()
        {
            Assert.AreEqual(CameraAngles.Sample, CameraAngles.Parse(null));
            var (height, back) = CameraAngles.Offset(CameraAngles.Sample);
            Assert.AreEqual(25f, height, 1e-3, "the sample's camera stands 25 over the player");
            Assert.AreEqual(7.5f, back, 1e-3, "and 7.5 behind it");
            Assert.Less(CameraAngles.FarthestGround(CameraAngles.Sample), 40f, "its shadows fit the quality level Good");
        }

        [Test]
        public void APitchAloneKeepsTheFieldOfViewAndTheDistance()
        {
            var rig = CameraAngles.Parse("62");
            Assert.AreEqual(62f, rig.Pitch, 1e-4);
            Assert.AreEqual(35f, rig.FieldOfView, 1e-4);
            Assert.AreEqual(CameraAngles.SampleDistance, rig.Distance, 1e-4);
            var (height, back) = CameraAngles.Offset(rig);
            Assert.Less(height, 25f);
            Assert.Greater(back, 7.5f, "a lower camera stands farther behind");
        }

        [Test]
        public void ANarrowerFieldOfViewMovesTheCameraBackToSeeAsWide()
        {
            var rig = CameraAngles.Parse("62,27.5");
            Assert.AreEqual(33.35f, rig.Distance, 0.05);
            Assert.AreEqual(40f, CameraAngles.Parse("62,27.5,40").Distance, 1e-4);
        }

        [Test]
        public void AnythingOutOfRangeGivesTheSamples()
        {
            Assert.AreEqual(CameraAngles.Sample, CameraAngles.Parse("10"));
            Assert.AreEqual(CameraAngles.Sample, CameraAngles.Parse("62,abc"));
            Assert.AreEqual(CameraAngles.Sample, CameraAngles.Parse("62,35,200"));
            Assert.AreEqual(CameraAngles.Sample, CameraAngles.Parse("62,35,30,1"));
        }
    }

    public sealed class ArenaPalettesTests
    {
        [Test]
        public void ThePageWithoutTheOptionKeepsTheNight()
        {
            var colours = ArenaPalettes.Resolve(ArenaPalettes.OptionOf("http://127.0.0.1:8091/?perf=1"), out var ambientGiven);
            Assert.AreEqual("2A1A46", colours["WoodDark"]);
            Assert.AreEqual("F2A8E7", colours["Metal"]);
            Assert.AreEqual("77528B", colours[ArenaPalettes.Ambient]);
            Assert.IsFalse(ambientGiven);
        }

        [Test]
        public void DayIsChosenByName()
        {
            var colours = ArenaPalettes.Resolve(ArenaPalettes.OptionOf("http://x/?arena=day&perf=1"), out _);
            Assert.AreEqual("79A942", colours["WoodDark"]);
            Assert.AreEqual("CFC6E4", colours["Stone"]);
            Assert.AreEqual("7D6FB0", colours["StoneDark"]);
            Assert.AreEqual("836EF9", colours["Metal"]);
            Assert.AreEqual("E39A55", colours["BrownDark"]);
            Assert.AreEqual("2F8F5E", colours["Green"]);
        }

        [Test]
        public void PairsAreLaidOverThePaletteNamedWhereverItStands()
        {
            var colours = ArenaPalettes.Resolve("floor:#6fa03c,day,Plant:112233", out var ambientGiven);
            Assert.AreEqual("6FA03C", colours["WoodDark"]);
            Assert.AreEqual("112233", colours["Green"]);
            Assert.AreEqual("CFC6E4", colours["Stone"], "the rest is the day's");
            Assert.IsFalse(ambientGiven);
        }

        [Test]
        public void AnAmbientGivenIsToldApartAndNonsenseIsLeftOut()
        {
            var colours = ArenaPalettes.Resolve("ambient:ABCDEF,floor:zzzzzz,crate:12345", out var ambientGiven);
            Assert.IsTrue(ambientGiven);
            Assert.AreEqual("ABCDEF", colours[ArenaPalettes.Ambient]);
            Assert.AreEqual("2A1A46", colours["WoodDark"]);
            Assert.AreEqual("A763A8", colours["BrownDark"]);
        }

        [Test]
        public void EachPaletteBringsItsLightAndThePageOptionComesOverIt()
        {
            Assert.AreEqual(ArenaPalettes.Night, ArenaPalettes.NameOf(""));
            Assert.AreEqual(ArenaPalettes.Day, ArenaPalettes.NameOf("floor:112233,Day"));
            var day = MatchLookTuning.Parse(ArenaPalettes.TuningOf(ArenaPalettes.Day));
            var night = MatchLookTuning.Parse(ArenaPalettes.TuningOf(ArenaPalettes.Night));
            Assert.Greater(day.Warmth, night.Warmth, "the day is warmer than the night");
            var mine = MatchLookTuning.Parse(ArenaPalettes.TuningOf(ArenaPalettes.Day) + ",warmth:0.1");
            Assert.AreEqual(0.1f, mine.Warmth, 1e-5);
        }

        [Test]
        public void AnEscapedOptionIsRead()
        {
            Assert.AreEqual("day,floor:#79a942", ArenaPalettes.OptionOf("http://x/?a=1&arena=day%2Cfloor%3A%2379a942#top"));
            var (r, g, b) = ArenaPalettes.Rgb("FF8000");
            Assert.AreEqual(1f, r, 1e-6);
            Assert.AreEqual(128f / 255f, g, 1e-6);
            Assert.AreEqual(0f, b, 1e-6);
        }
    }
}
