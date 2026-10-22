using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class GenesisRentalOffersTests
    {
        private static RentalOffer Offer(int tokenId, string priceWei) => new RentalOffer { TokenId = tokenId, PricePerDayWei = priceWei, Lender = "0x" };

        [Test]
        public void TheCheapestOffersComeFirstByPriceThenTokenId()
        {
            var offers = new[]
            {
                Offer(1795, "10000000000000000000"),
                Offer(8, "2500000000000000000"),
                Offer(3, "10000000000000000000"),
                Offer(1, "2000000000000000000"),
            };
            var cheapest = GenesisRental.CheapestOf(offers, 3);
            Assert.AreEqual(3, cheapest.Count);
            Assert.AreEqual(1, cheapest[0].TokenId);
            Assert.AreEqual(8, cheapest[1].TokenId);
            Assert.AreEqual(3, cheapest[2].TokenId, "the same price: the lower token id first");
            Assert.AreEqual(4, GenesisRental.CheapestOf(offers, 10).Count);
            Assert.AreEqual(0, GenesisRental.CheapestOf(offers, 0).Count);
            Assert.AreEqual(0, GenesisRental.CheapestOf(new RentalOffer[0], 3).Count);
            // A price too large for a long is still compared as a number.
            var large = GenesisRental.CheapestOf(new[] { Offer(2, "100000000000000000000000000"), Offer(5, "99000000000000000000000000") }, 1);
            Assert.AreEqual(5, large[0].TokenId);
        }

        [Test]
        public void ARentalEndsSoonUnderTwelveHours()
        {
            const long now = 1790000000;
            Assert.IsTrue(GenesisRental.EndsSoon(now + 11 * 3600, now));
            Assert.IsTrue(GenesisRental.EndsSoon(now + 60, now));
            Assert.IsFalse(GenesisRental.EndsSoon(now + 13 * 3600, now));
            Assert.IsFalse(GenesisRental.EndsSoon(now, now), "over is not soon");
            Assert.IsFalse(GenesisRental.EndsSoon(0, now));
        }
    }
}
