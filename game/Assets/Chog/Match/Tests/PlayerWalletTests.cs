using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class PlayerWalletTests
    {
        [Test]
        public void TheGenesisPlayedIsOneTheWalletHolds()
        {
            Assert.AreEqual(0, PlayerWallet.PickToken(new int[0], 1795));
            Assert.AreEqual(0, PlayerWallet.PickToken(null, 1795));
            Assert.AreEqual(3, PlayerWallet.PickToken(new[] { 3, 4, 8 }, 0));
            Assert.AreEqual(8, PlayerWallet.PickToken(new[] { 3, 4, 8 }, 8));
            // The Genesis chosen last time was sold since: the first one held is played.
            Assert.AreEqual(3, PlayerWallet.PickToken(new[] { 3, 4, 8 }, 1795));
        }

        [Test]
        public void AnAddressIsShortenedForDisplay()
        {
            Assert.AreEqual("0x80fb...cd64", PlayerWallet.ShortAddress("0x80fb079237af2a634ba9b95263ba0bd53d20cd64"));
            Assert.AreEqual("", PlayerWallet.ShortAddress(null));
            Assert.AreEqual("0x12", PlayerWallet.ShortAddress("0x12"));
        }

        [Test]
        public void TheServicesSitNextToTheGamePage()
        {
            Assert.AreEqual("https://game.example/api/",
                GameServices.Url("https://game.example/?token=1795&perf=1"));
            Assert.AreEqual("https://game.example/play/api/",
                GameServices.Url("https://game.example/play/index.html#top"));
            Assert.AreEqual("http://192.168.1.85:8083/api/", GameServices.Url("http://192.168.1.85:8083"));
            Assert.AreEqual(GameServices.EditorUrl, GameServices.Url(""));

            Assert.AreEqual("https://game.example/hero/", GameServices.HeroPageUrl("https://game.example/?token=1795"));
            Assert.AreEqual("http://192.168.1.85:8083/hero/", GameServices.HeroPageUrl("http://192.168.1.85:8083/"));
        }

        [Test]
        public void AnHttpsPageCanBeReopenedInTheBrowserOfTheMetaMaskApp()
        {
            Assert.AreEqual("https://link.metamask.io/dapp/game.example/",
                PlayerWallet.MetaMaskLink("https://game.example/?token=1795&perf=1"));
            Assert.AreEqual("https://link.metamask.io/dapp/game.example/play/index.html",
                PlayerWallet.MetaMaskLink("https://game.example/play/index.html#top"));
            // The app cannot reach a plain-HTTP page of the local network that way.
            Assert.AreEqual("", PlayerWallet.MetaMaskLink("http://192.168.1.85:8083/"));
            Assert.AreEqual("", PlayerWallet.MetaMaskLink(""));
        }

        [Test]
        public void AKeptSessionIsOnlyGoodForItsAddressAndUntilItsEnd()
        {
            const string holder = "0x80fb079237af2a634ba9b95263ba0bd53d20cd64";
            Assert.IsTrue(PlayerWallet.IsSessionUsable("token", holder, 2000, holder, 1000));
            Assert.IsFalse(PlayerWallet.IsSessionUsable("token", holder, 1000, holder, 1000));
            Assert.IsFalse(PlayerWallet.IsSessionUsable("token", holder, 2000, "0x1111111111111111111111111111111111111111", 1000));
            Assert.IsFalse(PlayerWallet.IsSessionUsable("", holder, 2000, holder, 1000));
            Assert.IsFalse(PlayerWallet.IsSessionUsable("token", "", 2000, "", 1000));
        }

        [Test]
        public void AMatchHasTheSameNameOnEveryDeviceAndOnlyLettersTheServicesAccept()
        {
            Assert.AreEqual("LR76C2LZ:p0000012345", GenesisCareers.MatchId("LR76C2LZ", 12345));
            Assert.AreEqual("room_with_spaces__:n2147483648", GenesisCareers.MatchId("room with spaces !", int.MinValue));
            Assert.AreEqual(":p0000000000", GenesisCareers.MatchId(null, 0));
            Assert.AreEqual(60 + 12, GenesisCareers.MatchId(new string('a', 200), 7).Length);
        }

        [Test]
        public void AResultIsSentAgainOnlyWhenTheServicesFailedAndAttemptsRemain()
        {
            Assert.IsTrue(GenesisCareers.ShouldSendAgain(0, 1), "the request never arrived");
            Assert.IsTrue(GenesisCareers.ShouldSendAgain(502, 2), "the chain could not be read");
            Assert.IsFalse(GenesisCareers.ShouldSendAgain(502, 3), "no attempt left");
            Assert.IsFalse(GenesisCareers.ShouldSendAgain(200, 1));
            Assert.IsFalse(GenesisCareers.ShouldSendAgain(401, 1), "a refusal is final");
            Assert.IsFalse(GenesisCareers.ShouldSendAgain(403, 1));
        }

        [Test]
        public void APassIsReadFromTheAddressOnlyWhenItHasTheShapeOfOne()
        {
            const string pass = "eyJ0b2tlbklkIjoxNzk1fQ.c2lnbmF0dXJl-_0";
            Assert.AreEqual(pass, GuestPass.FromUrl("https://game.example/?perf=1&pass=" + pass));
            Assert.AreEqual(pass, GuestPass.FromUrl("http://192.168.1.85:8083/?pass=" + pass + "#top"));
            Assert.AreEqual("", GuestPass.FromUrl("https://game.example/"));
            Assert.AreEqual("", GuestPass.FromUrl("https://game.example/?pass="));
            Assert.AreEqual("", GuestPass.FromUrl("https://game.example/?pass=no-dot"));
            Assert.AreEqual("", GuestPass.FromUrl("https://game.example/?pass=two.dots.here"));
            Assert.AreEqual("", GuestPass.FromUrl("https://game.example/?pass=a.b%3Cscript%3E"));
            Assert.IsFalse(GuestPass.LooksLikeAPass(new string('a', 400) + "." + new string('b', 400)), "too long to be one");
        }

        [Test]
        public void TheLinkThatLendsAGenesisIsThePageWithoutItsOptionsThenThePass()
        {
            Assert.AreEqual("https://game.example/?pass=abc.def", GuestPass.LinkFor("https://game.example/?perf=1&token=3", "abc.def"));
            Assert.AreEqual("https://game.example/play/index.html?pass=abc.def", GuestPass.LinkFor("https://game.example/play/index.html#top", "abc.def"));
            Assert.AreEqual("", GuestPass.LinkFor("", "abc.def"));
            Assert.AreEqual("", GuestPass.LinkFor("https://game.example/", ""));
            // A guest who opens the link gets the same pass back.
            Assert.AreEqual("abc.def", GuestPass.FromUrl(GuestPass.LinkFor("https://game.example/?pass=old.one", "abc.def")));
        }

        [Test]
        public void TheTimeLeftOnALoanReadsInDays()
        {
            const long day = 24L * 3600L * 1000L;
            Assert.AreEqual("30 more days", GuestPass.TimeLeft(1000 + 30 * day, 1000));
            Assert.AreEqual("1 more day", GuestPass.TimeLeft(1000 + 2 * day - 1, 1000));
            Assert.AreEqual("less than a day", GuestPass.TimeLeft(1000 + day - 1, 1000));
            Assert.AreEqual("less than a day", GuestPass.TimeLeft(1000, 5000), "already over");
        }

        [Test]
        public void ARefusedPassIsExplainedAndOnlyTheServicesRefusalsAreFinal()
        {
            Assert.AreEqual("it has run out", GuestPass.InWords("pass_expired"));
            Assert.AreEqual("its holder stopped lending this Genesis", GuestPass.InWords("lending_stopped"));
            Assert.AreEqual("the wallet that lent this Genesis no longer holds it", GuestPass.InWords("lender_not_the_holder"));
            Assert.AreEqual("it was not made by this game", GuestPass.InWords("bad_pass"));
            Assert.IsTrue(GuestPass.IsFinalRefusal("lending_stopped"));
            Assert.IsFalse(GuestPass.IsFinalRefusal("upstream_failed"), "the chain could not be read: the pass may still count");
            Assert.IsFalse(GuestPass.IsFinalRefusal(null));
        }

        [Test]
        public void AMatchIsReportedByTheSignedInHolderOrByAGuestAndByNobodyElse()
        {
            // The holder plays the Genesis chosen in the wallet.
            Assert.AreEqual(GenesisCareers.Reporter.Holder, GenesisCareers.ReporterFor(1795, 1795, true, 0));
            Assert.AreEqual(GenesisCareers.Reporter.Nobody, GenesisCareers.ReporterFor(1795, 1795, false, 0), "not signed in");
            // A guest plays the Genesis of the pass, with no Genesis of their own chosen.
            Assert.AreEqual(GenesisCareers.Reporter.Guest, GenesisCareers.ReporterFor(3, 0, false, 3));
            Assert.AreEqual(GenesisCareers.Reporter.Nobody, GenesisCareers.ReporterFor(8, 0, false, 3), "a test entry, not the lent Genesis");
            // A holder who also has a pass played their own Genesis: the pass reports nothing.
            Assert.AreEqual(GenesisCareers.Reporter.Holder, GenesisCareers.ReporterFor(1795, 1795, true, 3));
            Assert.AreEqual(GenesisCareers.Reporter.Nobody, GenesisCareers.ReporterFor(3, 1795, true, 3));
            Assert.AreEqual(GenesisCareers.Reporter.Nobody, GenesisCareers.ReporterFor(0, 0, false, 0), "a recruit");
        }

        [Test]
        public void ACareerReadsAsMatchesWinsAndBadges()
        {
            Assert.AreEqual("no match yet", GenesisCareers.Describe(null));
            Assert.AreEqual("no match yet", GenesisCareers.Describe(new GenesisCareer()));
            Assert.AreEqual("1 match, 1 win  [First match]  [First win]", GenesisCareers.Describe(new GenesisCareer
            {
                matches = 1, wins = 1,
                badges = new[] { new CareerBadge { name = "First match" }, new CareerBadge { name = "First win" } }
            }));
            Assert.AreEqual("12 matches, 0 wins", GenesisCareers.Describe(new GenesisCareer { matches = 12 }));
        }

        [Test]
        public void MasteryReadsAsALevelAndTheWayToTheNextOne()
        {
            Assert.AreEqual("", GenesisCareers.DescribeMastery(null));
            Assert.AreEqual("", GenesisCareers.DescribeMastery(new GenesisCareer()), "services that send no mastery");
            Assert.AreEqual("Mastery 1  0/3", GenesisCareers.DescribeMastery(new GenesisCareer { mastery = new CareerMastery { level = 1, next = 3 } }));
            Assert.AreEqual("Mastery 6", GenesisCareers.DescribeMastery(new GenesisCareer { mastery = new CareerMastery { level = 6, points = 44 } }));

            var career = new GenesisCareer { matches = 4, wins = 1, mastery = new CareerMastery { level = 2, points = 5, next = 8 } };
            Assert.AreEqual("Mastery 2  5/8   4 matches, 1 win", GenesisCareers.DescribeAll(career));
            Assert.AreEqual("4 matches, 1 win", GenesisCareers.DescribeMatches(career));
            // A Genesis that never played still shows what it plays for.
            Assert.AreEqual("Mastery 1  0/3   no match yet",
                GenesisCareers.DescribeAll(new GenesisCareer { mastery = new CareerMastery { level = 1, next = 3 } }));
        }
    }
}
