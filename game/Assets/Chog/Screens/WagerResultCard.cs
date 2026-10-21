using System.Collections;
using System.Globalization;
using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BigInteger = System.Numerics.BigInteger;

namespace Chog.Screens
{
    /// <summary>
    /// The stake of the match on the end-of-match screen, for a player who staked on it: the
    /// stake with its dollars while the match is being judged, then what it came to, counted up
    /// from zero the way the references count trophies up on their result screen (Brawl Stars
    /// and Clash Royale tick the trophies gained after each match before the total settles):
    /// "+90 $CHOG ($0.14)" for a win, "-100 $CHOG" for a loss, "GIVEN BACK" for a draw or a
    /// match called off. The payment itself is brought to the contract from the home screen
    /// (WagerResult); this card only shows. Hidden when this device has no stake on the match.
    /// Placed by ChogResultScreen.Show (stream G): WagerResultCard.Show(host.transform, localTeam).
    /// </summary>
    public sealed class WagerResultCard : MonoBehaviour
    {
        private const string ObjectName = "ChogWagerResultCard";
        private const float ReadEverySeconds = 4f;
        private const float CountSeconds = 0.9f;
        // Under the hero, at the left, where the result screen keeps nothing.
        private static readonly Vector2 CardMin = new Vector2(0.06f, 0.09f);
        private static readonly Vector2 CardMax = new Vector2(0.44f, 0.195f);

        private TextMeshProUGUI _title;
        private TextMeshProUGUI _amount;
        private TextMeshProUGUI _detail;
        private float _readAt;
        private bool _counted;
        private int _localTeam;

        /// <summary>
        /// Adds the card to the result screen "host" for the local player of "localTeam", when
        /// this device staked on a match whose stake is not paid yet. Nothing otherwise.
        /// </summary>
        public static WagerResultCard Show(Transform host, int localTeam)
        {
            if (host == null || host.Find(ObjectName) != null || GenesisWager.State != WagerState.Open || !GenesisWager.HasLastMatch)
                return null;

            var area = ChogUi.Area(ObjectName, host, CardMin, CardMax);
            var plate = area.gameObject.AddComponent<Image>();
            plate.sprite = ChogSprites.Rounded(22, 4, ChogTheme.Ink, ChogTheme.CardRaised);
            plate.type = Image.Type.Sliced;
            plate.raycastTarget = false;
            var card = area.gameObject.AddComponent<WagerResultCard>();
            card._localTeam = localTeam;
            card.Build(area);
            return card;
        }

        private void Build(RectTransform area)
        {
            _title = ChogUi.Number("Title", area, new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.94f), 24f, TextAlignmentOptions.Center, ChogTheme.PurpleSoft, false);
            _amount = ChogUi.Title("Amount", area, new Vector2(0.04f, 0.22f), new Vector2(0.62f, 0.66f), 46f, TextAlignmentOptions.Left);
            _detail = ChogUi.Body("Detail", area, new Vector2(0.60f, 0.10f), new Vector2(0.96f, 0.66f), 22f, TextAlignmentOptions.Right, ChogTheme.Cream);
            Refresh();
        }

        private void OnEnable()
        {
            GenesisWager.Changed += Refresh;
            ChogPrice.Changed += Refresh;
        }

        private void OnDisable()
        {
            GenesisWager.Changed -= Refresh;
            ChogPrice.Changed -= Refresh;
        }

        private void Update()
        {
            if (!GenesisWager.HasLastMatch || GenesisWager.IsBusy || Time.unscaledTime < _readAt)
                return;
            if (GenesisWager.Outcome != WagerOutcome.Unknown && GenesisWager.Outcome != WagerOutcome.Pending)
                return;
            _readAt = Time.unscaledTime + ReadEverySeconds;
            GenesisWager.ReadOutcome();
        }

        /// <summary>
        /// The three lines of the card from what is known: the title, the amount (signed) and
        /// the detail. "gainWei" is what a winner gains on top of its stake.
        /// </summary>
        public static (string title, string amount, string detail) Describe(WagerOutcome outcome, bool mine, string stakeWei, string gainWei,
                                                                            string currency, double usdPerChog)
        {
            var stake = ChogPrice.WithUsd(stakeWei, usdPerChog, currency);
            switch (outcome)
            {
                case WagerOutcome.Won when gainWei == "0":
                    return ("YOUR STAKE", stake, "nobody staked on the other team: given back");
                case WagerOutcome.Won when mine:
                    return ("YOU WON", "+" + ChogPrice.WithUsd(gainWei, usdPerChog, currency), "on top of your stake of " + MonadTransactions.PriceText(stakeWei) + ", claim it on the home screen");
                case WagerOutcome.Won:
                    return ("YOU LOST", "-" + stake, "your stake goes to the winners");
                case WagerOutcome.Draw:
                    return ("A DRAW", stake, "given back");
                case WagerOutcome.CalledOff:
                    return ("MATCH CALLED OFF", stake, "given back");
                default:
                    return ("YOUR STAKE", stake, "the match is being judged...");
            }
        }

        private void Refresh()
        {
            if (_title == null)
                return;

            var outcome = GenesisWager.Outcome;
            var mine = outcome == WagerOutcome.Won && GenesisWager.WinnerTeam == GenesisWager.LastTeam;
            var gain = GenesisWager.WinnerGain(GenesisWager.LastStakeWei, GenesisWager.LastWinners, GenesisWager.LastLosers, GenesisWager.BurnedShare);
            var usdPerChog = GenesisWager.IsTest ? 0 : ChogPrice.UsdPerChog;
            var (title, amount, detail) = Describe(outcome, mine, GenesisWager.LastStakeWei, gain, GenesisWager.Currency, usdPerChog);
            _title.text = title;
            _detail.text = detail;
            var decided = outcome == WagerOutcome.Won || outcome == WagerOutcome.Draw || outcome == WagerOutcome.CalledOff;
            _amount.color = outcome == WagerOutcome.Won && mine && gain != "0" ? ChogTheme.Hex("FFE27A")
                : outcome == WagerOutcome.Won && !mine ? ChogTheme.Hex("FF9C8A") : Color.white;
            if (decided && !_counted)
            {
                // Counted once, from zero: a win in gold, a loss in red.
                _counted = true;
                var counted = outcome == WagerOutcome.Won && gain != "0" ? (mine ? gain : GenesisWager.LastStakeWei) : "";
                if (counted.Length > 0 && !ChogTheme.ReducedMotion)
                {
                    StartCoroutine(CountUp(counted, mine ? "+" : "-", GenesisWager.Currency, usdPerChog, amount));
                    ChogSounds.Reward(mine ? 1 : 0);
                    return;
                }
            }
            _amount.text = amount;
        }

        private IEnumerator CountUp(string targetWei, string sign, string currency, double usdPerChog, string finalText)
        {
            var target = BigInteger.Parse(targetWei, CultureInfo.InvariantCulture);
            var start = Time.unscaledTime;
            while (Time.unscaledTime - start < CountSeconds)
            {
                var t = Mathf.Clamp01((Time.unscaledTime - start) / CountSeconds);
                var eased = 1f - (1f - t) * (1f - t);
                var shown = target * new BigInteger(Mathf.RoundToInt(eased * 1000f)) / 1000;
                _amount.text = sign + ChogPrice.WithUsd(shown.ToString(CultureInfo.InvariantCulture), usdPerChog, currency);
                yield return null;
            }
            _amount.text = finalText;
        }
    }
}
