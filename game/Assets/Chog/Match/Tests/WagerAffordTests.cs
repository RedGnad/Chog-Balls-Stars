using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class WagerAffordTests
    {
        private const string HundredChog = "100000000000000000000";
        private const string TwentyFiveChog = "25000000000000000000";

        [Test]
        public void AStakeAboveTheBalanceIsRefusedBeforeAnySignature()
        {
            Assert.IsTrue(GenesisWager.CanAfford(HundredChog, TwentyFiveChog));
            Assert.IsTrue(GenesisWager.CanAfford(TwentyFiveChog, TwentyFiveChog));
            Assert.IsFalse(GenesisWager.CanAfford("0", TwentyFiveChog));
            Assert.IsFalse(GenesisWager.CanAfford("24999999999999999999", TwentyFiveChog));
        }

        [Test]
        public void AnUnknownBalanceLeavesTheDecisionToTheChain()
        {
            Assert.IsTrue(GenesisWager.CanAfford("", TwentyFiveChog));
            Assert.IsTrue(GenesisWager.CanAfford(null, TwentyFiveChog));
        }
    }
}
