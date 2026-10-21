using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class ChogPriceTests
    {
        // The price read on 21 October 2026 (CoinGecko).
        private const double Price = 0.00150916;
        private const string HundredChog = "100000000000000000000";
        private const string ThousandChog = "1000000000000000000000";

        [Test]
        public void AnAmountOfChogIsTurnedIntoDollarsAtThePrice()
        {
            Assert.AreEqual(1.50916, ChogPrice.UsdOf(ThousandChog, Price), 1e-9);
            Assert.AreEqual(0.150916, ChogPrice.UsdOf(HundredChog, Price), 1e-9);
            Assert.AreEqual(0, ChogPrice.UsdOf(HundredChog, 0));
            Assert.AreEqual(0, ChogPrice.UsdOf("hundred", Price));
        }

        [TestCase(0, "$0")]
        [TestCase(0.004, "<$0.01")]
        [TestCase(0.015, "$0.02")]
        [TestCase(0.15, "$0.15")]
        [TestCase(1.5, "$1.50")]
        [TestCase(12.4, "$12.40")]
        [TestCase(999.994, "$999.99")]
        [TestCase(1234.4, "$1,234")]
        public void DollarsReadShort(double dollars, string expected)
        {
            Assert.AreEqual(expected, ChogPrice.DollarsText(dollars));
        }

        [Test]
        public void ALabelCarriesTheDollarsOnlyWhenThePriceIsKnown()
        {
            Assert.AreEqual("$0.15", ChogPrice.UsdText(HundredChog, Price));
            Assert.AreEqual("", ChogPrice.UsdText(HundredChog, 0), "no price, no dollars: nothing is made up");
            Assert.AreEqual("100 $CHOG ($0.15)", ChogPrice.WithUsd(HundredChog, Price));
            Assert.AreEqual("100 $CHOG", ChogPrice.WithUsd(HundredChog, 0));
            Assert.AreEqual("1000 TEST $CHOG ($1.51)", ChogPrice.WithUsd(ThousandChog, Price, "TEST $CHOG"));
            Assert.AreEqual("<0.01 $CHOG (<$0.01)", ChogPrice.WithUsd("1000000000000000", Price));
        }

        [Test]
        public void ThePillShowsTheBalanceShortAndWhatItIsWorthUnderIt()
        {
            Assert.AreEqual("12.5K\n<size=58%>$18.86</size>", ChogPrice.PillText("12500000000000000000000", Price));
            Assert.AreEqual("39.03", ChogPrice.PillText("39030000000000000000", 0));
            Assert.AreEqual("3.1M", ChogPrice.Compact("3100000"));
            Assert.AreEqual("12.4K", ChogPrice.Compact("12400"));
            Assert.AreEqual("9999", ChogPrice.Compact("9999"));
            Assert.AreEqual("?", ChogPrice.Compact("?"));
        }
    }
}
