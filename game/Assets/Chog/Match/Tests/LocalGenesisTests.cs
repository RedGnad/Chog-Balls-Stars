using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class LocalGenesisTests
    {
        private const string Lan = "http://192.168.1.85:8083/";
        private const string Public = "https://game.example/";

        [Test]
        public void TheUrlCarriesTheCanonicalTokenIdWhereTheGameIsDeveloped()
        {
            Assert.AreEqual(1795, LocalGenesis.RequestedTokenId(0, 0, Lan + "?perf=1&token=1795", false, 0));
            Assert.AreEqual(0, LocalGenesis.RequestedTokenId(0, 0, Lan, false, 0));
        }

        [Test]
        public void OnAPublicAddressTheTestEntryDeclaresNoGenesis()
        {
            Assert.AreEqual(0, LocalGenesis.RequestedTokenId(0, 0, Public + "?token=1795", false, 0));
            Assert.AreEqual(0, LocalGenesis.RequestedTokenId(0, 0, "https://192.168.1.85.example/?token=1795", false, 0));
            // The Editor has no page address: its own entries keep working.
            Assert.AreEqual(1795, LocalGenesis.RequestedTokenId(0, 0, Public + "?token=1795", true, 0));
        }

        [Test]
        public void APageIsPrivateOnlyOnThisComputerOrItsLocalNetwork()
        {
            foreach (var url in new[]
                     {
                         "http://localhost:8083/", "http://127.0.0.1:8083/?token=1", "http://192.168.1.85:8083", "http://10.0.0.7/",
                         "http://172.16.0.1/", "http://172.31.255.255/", "http://mac.local:8083/", "HTTP://LOCALHOST/"
                     })
                Assert.IsTrue(LocalGenesis.IsPrivatePage(url), url);

            foreach (var url in new[]
                     {
                         "https://game.example/", "https://chog.xyz/?token=1", "http://172.32.0.1/", "http://11.0.0.1/", "http://192.169.1.1/",
                         "https://localhost.example/", "https://192.168.1.85@game.example/", "http://1.2.3/", "", null, "no address"
                     })
                Assert.IsFalse(LocalGenesis.IsPrivatePage(url), url ?? "null");
        }

        [Test]
        public void AnInvalidUrlValueDeclaresNoGenesis()
        {
            Assert.AreEqual(0, LocalGenesis.RequestedTokenId(0, 0, Lan + "?token=abc", false, 0));
            Assert.AreEqual(0, LocalGenesis.RequestedTokenId(0, 0, Lan + "?token=-4", false, 0));
            Assert.AreEqual(0, LocalGenesis.RequestedTokenId(0, 0, Lan + "?token=0", true, 12));
        }

        [Test]
        public void TheEditorPreferenceOnlyAppliesInTheEditor()
        {
            Assert.AreEqual(12, LocalGenesis.RequestedTokenId(0, 0, "", true, 12));
            Assert.AreEqual(0, LocalGenesis.RequestedTokenId(0, 0, "", false, 12));
            Assert.AreEqual(1795, LocalGenesis.RequestedTokenId(0, 0, Lan + "?token=1795", true, 12));
        }

        [Test]
        public void TheBotsOptionKeepsOnlyTokenIds()
        {
            CollectionAssert.AreEqual(new[] { 181, 70 }, TestOptionsFromUrl.TokenIds("181,70"));
            CollectionAssert.AreEqual(new[] { 5 }, TestOptionsFromUrl.TokenIds("abc,-3,0,5,"));
            CollectionAssert.AreEqual(new int[0], TestOptionsFromUrl.TokenIds(null));
        }

        [Test]
        public void TheGenesisChosenInTheWalletWinsOverALentOneAndOverTheTestEntries()
        {
            Assert.AreEqual(408, LocalGenesis.RequestedTokenId(408, 3, Lan + "?token=1795", true, 12));
            Assert.AreEqual(408, LocalGenesis.RequestedTokenId(408, 0, "", false, 0));
        }

        [Test]
        public void TheSecondFormIsPlayedOnlyWhenPickedAndUnlockedByMastery()
        {
            Assert.AreEqual(0, GenesisForms.Played(0, 6, false), "not picked");
            Assert.AreEqual(0, GenesisForms.Played(1, GenesisForms.UnlockLevel - 1, false), "picked on another device, mastery too low here");
            Assert.AreEqual(1, GenesisForms.Played(1, GenesisForms.UnlockLevel, false));
            Assert.AreEqual(1, GenesisForms.Played(1, 6, false));
            Assert.AreEqual(1, GenesisForms.Played(0, 1, true), "the test option plays it without mastery");
            Assert.IsFalse(GenesisForms.IsUnlocked(1));
            Assert.IsTrue(GenesisForms.IsUnlocked(GenesisForms.UnlockLevel));
        }

        [Test]
        public void TheFormTestOptionOnlyWorksWhereTestEntriesDo()
        {
            Assert.IsTrue(GenesisForms.TestOption(Lan + "?token=1795&form=1", false));
            Assert.IsFalse(GenesisForms.TestOption(Lan + "?token=1795&form=0", false));
            Assert.IsFalse(GenesisForms.TestOption(Lan + "?token=1795", false));
            Assert.IsFalse(GenesisForms.TestOption(Public + "?form=1", false), "ignored on a public address");
        }

        [Test]
        public void ALentGenesisIsPlayedOnAnyAddressAndWinsOverTheTestEntries()
        {
            Assert.AreEqual(3, LocalGenesis.RequestedTokenId(0, 3, Public, false, 0));
            Assert.AreEqual(3, LocalGenesis.RequestedTokenId(0, 3, Lan + "?token=1795", true, 12));
        }
    }
}
