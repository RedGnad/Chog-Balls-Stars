using NUnit.Framework;

namespace Chog.Presentation.Tests
{
    public sealed class QualityDefaultsTests
    {
        private static readonly string[] ProjectLevels = { "Fastest", "Fast", "Simple", "Good", "Beautiful", "Fantastic" };

        [Test]
        public void DesktopStartsWithShadowsAndMobileWithout()
        {
            Assert.AreEqual(3, QualityDefaults.Choose(false, ProjectLevels));
            Assert.AreEqual(1, QualityDefaults.Choose(true, ProjectLevels));
        }

        [Test]
        public void LevelsAreFoundByNameWhateverTheirOrder()
        {
            var reordered = new[] { "Good", "Fantastic", "fast" };
            Assert.AreEqual(0, QualityDefaults.Choose(false, reordered));
            Assert.AreEqual(2, QualityDefaults.Choose(true, reordered));
        }

        [Test]
        public void MissingNamesFallBackToAnExistingIndex()
        {
            Assert.AreEqual(1, QualityDefaults.Choose(false, new[] { "Low", "High" }));
            Assert.AreEqual(1, QualityDefaults.Choose(true, new[] { "Low", "High" }));
            Assert.AreEqual(0, QualityDefaults.Choose(true, new[] { "Only" }));
            Assert.AreEqual(-1, QualityDefaults.Choose(false, new string[0]));
            Assert.AreEqual(-1, QualityDefaults.Choose(true, null));
        }
    }
}
