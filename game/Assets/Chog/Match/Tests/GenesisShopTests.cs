using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class GenesisShopTests
    {
        private const string Shop = "0x3C2BafebbB0c8c58f39A976e725cD20D611d01e9";
        private const string Buyer = "0x2c7536E3605D9C16a7a3D7b1898e529396a65c23";
        private const string FiveHundred = "500000000000000000000";
        private const string Zeros24 = "000000000000000000000000";

        [Test]
        public void TheCallsOfAPurchaseAreTheBytesTheContractsExpect()
        {
            // approve(address,uint256): the shop, then 500 with eighteen decimals.
            Assert.AreEqual("0x095ea7b3" + Zeros24 + "3c2bafebbb0c8c58f39a976e725cd20d611d01e9"
                            + "00000000000000000000000000000000000000000000001b1ae4d6e2ef500000",
                MonadTransactions.ApproveData(Shop, FiveHundred));
            // allowance(address,address): the buyer, then the shop.
            Assert.AreEqual("0xdd62ed3e" + Zeros24 + "2c7536e3605d9c16a7a3d7b1898e529396a65c23"
                            + Zeros24 + "3c2bafebbb0c8c58f39a976e725cd20d611d01e9",
                MonadTransactions.AllowanceData(Buyer, Shop));
            // balanceOf(address): the buyer.
            Assert.AreEqual("0x70a08231" + Zeros24 + "2c7536e3605d9c16a7a3d7b1898e529396a65c23", MonadTransactions.BalanceData(Buyer));
            Assert.AreEqual(FiveHundred, MonadTransactions.HexToDecimal("0x00000000000000000000000000000000000000000000001b1ae4d6e2ef500000"));
            Assert.AreEqual("0", MonadTransactions.HexToDecimal("0x"));
            Assert.AreEqual("0", MonadTransactions.HexToDecimal(null));
            // buy(uint256,uint256): token 408, item 1.
            Assert.AreEqual("0xd6febde8"
                            + "0000000000000000000000000000000000000000000000000000000000000198"
                            + "0000000000000000000000000000000000000000000000000000000000000001",
                GenesisShop.BuyData(408, 1));
        }

        [Test]
        public void APriceReadsInWholeChogWithAtMostTwoDecimals()
        {
            Assert.AreEqual("500", MonadTransactions.PriceText(FiveHundred));
            Assert.AreEqual("12.5", MonadTransactions.PriceText("12500000000000000000"));
            Assert.AreEqual("1.25", MonadTransactions.PriceText("1250000000000000000"));
            Assert.AreEqual("0.05", MonadTransactions.PriceText("50000000000000000"));
            Assert.AreEqual("0", MonadTransactions.PriceText("0"));
            Assert.AreEqual("<0.01", MonadTransactions.PriceText("3333333333333333"), "a rental earned by the second is not shown as nothing");
            Assert.AreEqual("?", MonadTransactions.PriceText("1e21"));
            Assert.AreEqual("?", MonadTransactions.PriceText(null));
        }

        [Test]
        public void AnAllowanceIsEnoughOnlyWhenItCoversThePrice()
        {
            const string enough = "0x00000000000000000000000000000000000000000000001b1ae4d6e2ef500000";
            const string oneLess = "0x00000000000000000000000000000000000000000000001b1ae4d6e2ef4fffff";
            const string unlimited = "0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";
            Assert.IsTrue(MonadTransactions.IsAtLeast(enough, FiveHundred));
            Assert.IsFalse(MonadTransactions.IsAtLeast(oneLess, FiveHundred));
            Assert.IsTrue(MonadTransactions.IsAtLeast(unlimited, FiveHundred), "the largest number is not read as a negative one");
            Assert.IsFalse(MonadTransactions.IsAtLeast("0x", FiveHundred));
            Assert.IsFalse(MonadTransactions.IsAtLeast("", FiveHundred));
            Assert.IsFalse(MonadTransactions.IsAtLeast(enough, "five hundred"));
        }

        [Test]
        public void AReceiptSaysWhetherTheTransactionWentThroughOrIsStillAwaited()
        {
            Assert.IsNull(MonadTransactions.ReceiptSucceeded(null));
            Assert.IsNull(MonadTransactions.ReceiptSucceeded("null"), "not in a block yet");
            Assert.AreEqual(true, MonadTransactions.ReceiptSucceeded("{\"blockNumber\":\"0x6ad\",\"status\":\"0x1\",\"logs\":[]}"));
            Assert.AreEqual(false, MonadTransactions.ReceiptSucceeded("{\"status\": \"0x0\"}"));
            Assert.IsNull(MonadTransactions.ReceiptSucceeded("{\"blockNumber\":\"0x6ad\"}"), "a receipt without a status decides nothing");
        }

        [Test]
        public void TheSecondFormIsUnlockedByMasteryOrByAPurchaseForThatGenesis()
        {
            Assert.IsFalse(GenesisForms.IsUnlocked(1));
            Assert.IsTrue(GenesisForms.IsUnlocked(1, bought: true));
            Assert.IsTrue(GenesisForms.IsUnlocked(GenesisForms.UnlockLevel));
            Assert.AreEqual(1, GenesisForms.Played(1, 1, false, bought: true));
            Assert.AreEqual(0, GenesisForms.Played(0, 1, false, bought: true), "bought, but the first form is picked");
        }
    }
}
