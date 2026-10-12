using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class GenesisRentalTests
    {
        private const string TenChog = "10000000000000000000";
        private const string TenChogWord = "0000000000000000000000000000000000000000000000008ac7230489e80000";

        [Test]
        public void TheCallsOfARentalAreTheBytesTheContractExpects()
        {
            // offer(uint256,uint128): token 1795 at 10 $CHOG a day.
            Assert.AreEqual("0x92f6d20c"
                            + "0000000000000000000000000000000000000000000000000000000000000703" + TenChogWord,
                GenesisRental.OfferData(1795, TenChog));
            // withdraw(uint256): token 1795.
            Assert.AreEqual("0x2e1a7d4d" + "0000000000000000000000000000000000000000000000000000000000000703",
                GenesisRental.WithdrawData(1795));
            // rent(uint256,uint256,uint128): token 408 for 3 days at no more than 10 $CHOG a day.
            Assert.AreEqual("0xcfb0c2ab"
                            + "0000000000000000000000000000000000000000000000000000000000000198"
                            + "0000000000000000000000000000000000000000000000000000000000000003" + TenChogWord,
                GenesisRental.RentData(408, 3, TenChog));
        }

        [Test]
        public void AmountsOfChogAreCountedWithoutLosingAUnit()
        {
            Assert.AreEqual("25000000000000000000", MonadTransactions.WholeToWei(25));
            Assert.AreEqual("0", MonadTransactions.WholeToWei(-3));
            Assert.AreEqual("7500000000000000000", MonadTransactions.Times("2500000000000000000", 3));
            Assert.AreEqual("0", MonadTransactions.Times("two", 3));
            Assert.AreEqual("0", MonadTransactions.Times(TenChog, 0));
            Assert.AreEqual(TenChogWord, MonadTransactions.WordOf(TenChog));
        }

        [Test]
        public void AHolderReceivesThePaymentLessTheBurnedShare()
        {
            Assert.AreEqual("27000000000000000000", GenesisRental.HolderShare("30000000000000000000", 1000));
            Assert.AreEqual("30000000000000000000", GenesisRental.HolderShare("30000000000000000000", 0));
            Assert.AreEqual("0", GenesisRental.HolderShare("30000000000000000000", 10000));
            // One unit that cannot be split is not burned: it goes to the holder.
            Assert.AreEqual("9", GenesisRental.HolderShare("9", 1000));
            Assert.AreEqual("0", GenesisRental.HolderShare(null, 1000));
        }

        [Test]
        public void TheTimeLeftOnARentalReadsInDaysHoursOrMinutes()
        {
            const long now = 1790000000;
            Assert.AreEqual("2 d 5 h", GenesisRental.TimeLeft(now + 2 * 86400 + 5 * 3600 + 30, now));
            Assert.AreEqual("1 d 0 h", GenesisRental.TimeLeft(now + 86400, now));
            Assert.AreEqual("5 h", GenesisRental.TimeLeft(now + 5 * 3600 + 600, now));
            Assert.AreEqual("12 min", GenesisRental.TimeLeft(now + 12 * 60, now));
            Assert.AreEqual("1 min", GenesisRental.TimeLeft(now + 20, now));
            Assert.AreEqual("over", GenesisRental.TimeLeft(now, now));
        }

        [Test]
        public void AWalletPlaysWhatItHoldsThenWhatItRents()
        {
            CollectionAssert.AreEqual(new[] { 3, 1795, 408 }, PlayerWallet.Merge(new[] { 3, 1795 }, new[] { 408, 3 }));
            CollectionAssert.AreEqual(new[] { 408 }, PlayerWallet.Merge(new int[0], new[] { 408 }));
            CollectionAssert.AreEqual(new[] { 3 }, PlayerWallet.Merge(new[] { 3 }, null));
            CollectionAssert.AreEqual(new int[0], PlayerWallet.Merge(null, new int[0]));
            // The one chosen last time is kept when it is still held or rented.
            Assert.AreEqual(408, PlayerWallet.PickToken(PlayerWallet.Merge(new[] { 3 }, new[] { 408 }), 408));
            Assert.AreEqual(3, PlayerWallet.PickToken(PlayerWallet.Merge(new[] { 3 }, new int[0]), 408), "the rental ran out");
        }
    }
}
