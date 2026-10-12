using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class GenesisWagerTests
    {
        // Reference values encoded by cast (tools/contracts/wager_calldata.sh).
        private const string Key = "0x3ed1720486303eb00d7fa1f4b59ffac849dc5553fe292b20555ae44e3387317a";
        private const string TwentyFiveChog = "25000000000000000000";
        private static readonly string Signature = "0x" + new string('x', 64).Replace("x", "ab") + "1b";
        private static readonly string SignatureWords =
            "0000000000000000000000000000000000000000000000000000000000000041" +
            "abababababababababababababababababababababababababababababababab" +
            "abababababababababababababababababababababababababababababababab" +
            "1b00000000000000000000000000000000000000000000000000000000000000";

        [Test]
        public void StakeIsEncodedAsTheContractReadsIt()
        {
            Assert.AreEqual(
                "0x0a621329" + Key.Substring(2) +
                "0000000000000000000000000000000000000000000000000000000000000001" +
                "0000000000000000000000000000000000000000000000015af1d78b58c40000" +
                "000000000000000000000000000000000000000000000000000000006ab13bf8" +
                "00000000000000000000000000000000000000000000000000000000000000a0" + SignatureWords,
                GenesisWager.StakeData(Key, 1, TwentyFiveChog, 1790000120, Signature));
        }

        [Test]
        public void ThePaymentAndTheCallingOffAreEncodedAsTheContractReadsThem()
        {
            Assert.AreEqual(
                "0x99bcbc7f" + Key.Substring(2) +
                "0000000000000000000000000000000000000000000000000000000000000001" +
                "0000000000000000000000000000000000000000000000000000000000000060" + SignatureWords,
                GenesisWager.SettleData(Key, 1, Signature));
            Assert.AreEqual(
                "0xe3c01265" + Key.Substring(2) +
                "0000000000000000000000000000000000000000000000000000000000000040" + SignatureWords,
                GenesisWager.VoidData(Key, Signature));
        }

        [TestCase(1, 1, 1000, "22500000000000000000", TestName = "One a side: the loser's stake less a tenth")]
        [TestCase(3, 3, 1000, "22500000000000000000", TestName = "Three a side: the same for each winner")]
        [TestCase(2, 1, 1000, "11250000000000000000", TestName = "Two winners share one stake")]
        [TestCase(1, 3, 0, "75000000000000000000", TestName = "Nothing burned: all of what was lost")]
        [TestCase(1, 0, 1000, "0", TestName = "Nobody on the other side: nothing to win")]
        [TestCase(0, 2, 1000, "0", TestName = "No winner: nothing to share")]
        public void AWinnerGainsAnEqualShareOfWhatWasLostLessTheBurnedShare(int winners, int losers, int burnedShare, string expected)
        {
            Assert.AreEqual(expected, GenesisWager.WinnerGain(TwentyFiveChog, winners, losers, burnedShare));
        }

        [Test]
        public void TheTeamAStakerIsOnIsFoundWhateverTheLetterCase()
        {
            var stakers = new[]
            {
                new WagerStaker { Address = "0x70997970c51812dc3a010c7d01b50e0d17dc79c8", Team = 0 },
                new WagerStaker { Address = "0x3c44cdddb6a900fa2b585dd299e03d12fa4293bc", Team = 1 },
            };
            Assert.AreEqual(1, GenesisWager.TeamStakedOn(stakers, "0x3C44CdDdB6a900fa2b585dd299e03d12FA4293BC"));
            Assert.AreEqual(0, GenesisWager.TeamStakedOn(stakers, "0x70997970c51812dc3a010c7d01b50e0d17dc79c8"));
            Assert.AreEqual(-1, GenesisWager.TeamStakedOn(stakers, "0x000000000000000000000000000000000000dead"));
            Assert.AreEqual(-1, GenesisWager.TeamStakedOn(stakers, ""));
        }

        [Test]
        public void AStakedMatchIsKeptAndReadBack()
        {
            // The name of a match may hold any character, the separator included.
            var kept = GenesisWager.Kept("0:eu:party|night~1fdeb21d", Key, 1, TwentyFiveChog);
            Assert.IsTrue(GenesisWager.TryRead(kept, out var gameId, out var matchKey, out var team, out var stakeWei));
            Assert.AreEqual("0:eu:party|night~1fdeb21d", gameId);
            Assert.AreEqual(Key, matchKey);
            Assert.AreEqual(1, team);
            Assert.AreEqual(TwentyFiveChog, stakeWei);
        }

        [TestCase("")]
        [TestCase("nothing")]
        [TestCase("0x12|1|25|game")]
        [TestCase("0x3ed1720486303eb00d7fa1f4b59ffac849dc5553fe292b20555ae44e3387317a|2|25|game")]
        [TestCase("0x3ed1720486303eb00d7fa1f4b59ffac849dc5553fe292b20555ae44e3387317a|1|a lot|game")]
        [TestCase("0x3ed1720486303eb00d7fa1f4b59ffac849dc5553fe292b20555ae44e3387317a|1|25|")]
        public void AnythingElseIsNotAStakedMatch(string kept)
        {
            Assert.IsFalse(GenesisWager.TryRead(kept, out _, out _, out _, out _));
        }
    }
}
