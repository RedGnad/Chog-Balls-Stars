using System;
using System.Collections;
using System.Globalization;
using System.Numerics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The price of $CHOG in US dollars, as the game services read it from public sources
    /// (server/src/price.js), so that every amount on the screens reads in both: the balance
    /// on the home screen, a stake, a rent a day, an item of the shop. The references keep
    /// their currency in sight at all times (Brawl Stars and Clash Royale show gems and coins
    /// at the top of every screen); here the dollar beside the $CHOG says what it is worth.
    /// Without a price (the services down, no source answering) every label falls back to
    /// $CHOG alone: nothing is ever made up.
    /// </summary>
    public static class ChogPrice
    {
        // The services keep the price five minutes; the page asks a little less often.
        private const float ReadEverySeconds = 300f;

        /// <summary>Dollars for one $CHOG as last read; 0 while unknown.</summary>
        public static double UsdPerChog { get; private set; }

        /// <summary>Whether a price was read.</summary>
        public static bool Known => UsdPerChog > 0;

        /// <summary>Where the services read it: "coingecko", "geckoterminal" or "dexscreener"; empty while unknown.</summary>
        public static string Source { get; private set; } = "";

        /// <summary>Raised after the price changed.</summary>
        public static event Action Changed;

        private static bool s_started;

        // Field names are the JSON keys of the services.
        [Serializable]
        private sealed class PriceAnswer
        {
            public string token;
            public double usd;
            public string source;
            public bool stale;
        }

        /// <summary>Reads the price now and every few minutes after. Called once when the page is up.</summary>
        public static void Begin()
        {
            if (s_started)
                return;
            s_started = true;
            PlayerWallet.Run(ReadForever());
        }

        private static IEnumerator ReadForever()
        {
            while (true)
            {
                yield return GameServices.Call("price", null, null, (status, text) =>
                {
                    var answer = status == 200 && !string.IsNullOrEmpty(text) && text[0] == '{' ? JsonUtility.FromJson<PriceAnswer>(text) : null;
                    if (answer == null)
                    {
                        Debug.Log("CHOG_PRICE not read: " + status);
                        return;
                    }

                    // A price of zero is "unknown" (JSON null reads as 0): the last one read stays.
                    if (answer.usd > 0 && (Math.Abs(answer.usd - UsdPerChog) > 1e-12 || Source != (answer.source ?? "")))
                    {
                        UsdPerChog = answer.usd;
                        Source = answer.source ?? "";
                        Debug.Log("CHOG_PRICE " + UsdPerChog.ToString("0.########", CultureInfo.InvariantCulture) + " USD from " + Source);
                        Changed?.Invoke();
                    }
                });
                yield return new WaitForSecondsRealtime(ReadEverySeconds);
            }
        }

        // What follows is plain arithmetic and text, with the price given so it can be tested.

        /// <summary>An amount in the smallest unit of $CHOG as dollars at a price; 0 for anything that is not an amount.</summary>
        public static double UsdOf(string wei, double usdPerChog)
        {
            if (!MonadTransactions.IsDigits(wei) || usdPerChog <= 0)
                return 0;
            // Whole units and six decimals are enough for a label.
            var millionths = BigInteger.Parse(wei, CultureInfo.InvariantCulture) / BigInteger.Pow(10, 12);
            return (double)millionths / 1e6 * usdPerChog;
        }

        /// <summary>
        /// "$1.50", "$0.02", "<$0.01", "$1,234": dollars for an amount in the smallest unit of
        /// $CHOG at a price; empty while the price is unknown, so a label is never wrong.
        /// </summary>
        public static string UsdText(string wei, double usdPerChog)
        {
            if (!MonadTransactions.IsDigits(wei) || usdPerChog <= 0)
                return "";
            return DollarsText(UsdOf(wei, usdPerChog));
        }

        /// <summary>Dollars as a short label: "$0", "<$0.01", "$0.15", "$1.50", "$12.40", "$1,234".</summary>
        public static string DollarsText(double dollars)
        {
            if (dollars <= 0)
                return "$0";
            if (dollars < 0.01)
                return "<$0.01";
            if (dollars < 1000)
                return "$" + dollars.ToString("0.00", CultureInfo.InvariantCulture);
            return "$" + Math.Round(dollars).ToString("#,0", CultureInfo.InvariantCulture);
        }

        /// <summary>"100 $CHOG ($0.15)", or "100 $CHOG" while the price is unknown.</summary>
        public static string WithUsd(string wei, double usdPerChog, string currency = "$CHOG")
        {
            var usd = UsdText(wei, usdPerChog);
            return MonadTransactions.PriceText(wei) + " " + currency + (usd.Length > 0 ? " (" + usd + ")" : "");
        }

        /// <summary>The same with the price last read.</summary>
        public static string WithUsd(string wei, string currency = "$CHOG") => WithUsd(wei, UsdPerChog, currency);

        /// <summary>The dollars of an amount with the price last read; empty while unknown.</summary>
        public static string UsdText(string wei) => UsdText(wei, UsdPerChog);

        /// <summary>
        /// The $CHOG pill of the home screen: the balance short ("1.2K"), and under it, smaller,
        /// what it is worth ("$1.80") when the price is known.
        /// </summary>
        public static string PillText(string balanceWei, double usdPerChog)
        {
            var amount = Compact(MonadTransactions.PriceText(balanceWei));
            var usd = UsdText(balanceWei, usdPerChog);
            return usd.Length > 0 ? amount + "\n<size=58%>" + usd + "</size>" : amount;
        }

        /// <summary>The same with the price last read.</summary>
        public static string PillText(string balanceWei) => PillText(balanceWei, UsdPerChog);

        /// <summary>"39.03", "12.4K", "3.1M": an amount of $CHOG that fits a pill.</summary>
        public static string Compact(string amount)
        {
            if (!double.TryParse(amount, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return amount;
            if (value >= 1e6)
                return (value / 1e6).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            if (value >= 1e4)
                return (value / 1e3).ToString("0.#", CultureInfo.InvariantCulture) + "K";
            return amount;
        }
    }
}
