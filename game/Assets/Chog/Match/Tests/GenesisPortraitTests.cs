using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class GenesisPortraitTests
    {
        [Test]
        public void PortraitsSitNextToTheGamePageWhateverItsQuery()
        {
            Assert.AreEqual("https://game.example/genesis-art/256/",
                GenesisPortrait.BaseUrl("https://game.example/?token=1795&perf=1"));
            Assert.AreEqual("https://game.example/play/genesis-art/256/",
                GenesisPortrait.BaseUrl("https://game.example/play/index.html#top"));
            Assert.AreEqual("http://192.168.1.85:8083/genesis-art/256/",
                GenesisPortrait.BaseUrl("http://192.168.1.85:8083"));
        }

        [Test]
        public void OutsideAWebPageTheEditorAddressIsUsed()
        {
            Assert.AreEqual(GenesisPortrait.EditorBaseUrl, GenesisPortrait.BaseUrl(""));
            Assert.AreEqual(GenesisPortrait.EditorBaseUrl, GenesisPortrait.BaseUrl(null));
            Assert.AreEqual(GenesisPortrait.EditorBaseUrl, GenesisPortrait.BaseUrl("file:///Users/someone/build/index.html"));
        }

        [Test]
        public void TheCutsOfTheGenesisSitBesideTheirImages()
        {
            Assert.AreEqual("https://game.example/genesis-art/cut/",
                GenesisArt.CutBaseUrl("https://game.example/?token=1795&perf=1"));
            Assert.AreEqual("https://game.example/play/genesis-art/cut/",
                GenesisArt.CutBaseUrl("https://game.example/play/index.html#top"));
            Assert.AreEqual("http://127.0.0.1:8083/genesis-art/cut/", GenesisArt.CutBaseUrl(""));
        }

        [Test]
        public void ACutKeepsOfAPictureOnlyWhatItSays()
        {
            // Four points: kept whole, kept half, dropped, and one the round frame had already dropped.
            var pixels = new[]
            {
                new UnityEngine.Color32(10, 20, 30, 255), new UnityEngine.Color32(10, 20, 30, 255),
                new UnityEngine.Color32(10, 20, 30, 255), new UnityEngine.Color32(10, 20, 30, 0)
            };
            var cut = new[]
            {
                new UnityEngine.Color32(255, 255, 255, 255), new UnityEngine.Color32(128, 128, 128, 255),
                new UnityEngine.Color32(0, 0, 0, 255), new UnityEngine.Color32(255, 255, 255, 255)
            };
            GenesisArt.KeepOnly(pixels, cut);
            Assert.AreEqual(255, pixels[0].a);
            Assert.AreEqual(128, pixels[1].a);
            Assert.AreEqual(0, pixels[2].a);
            Assert.AreEqual(0, pixels[3].a, "what the round frame dropped stays dropped");
            Assert.AreEqual(10, pixels[1].r, "the colours are left as they are");
        }
    }
}
