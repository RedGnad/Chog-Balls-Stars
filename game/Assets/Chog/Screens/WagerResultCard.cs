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
    /// The stake of the match on the end-of-match screen, for a player who staked on it. At once
    /// what the match the player just saw comes to ("+90 $CHOG ($0.14)" for a win, "-100 $CHOG"
    /// for a loss), marked as being confirmed with the time waited, since the services replay the
    /// whole match from Photon's record before anything is paid (22 October 2026, the owner: "your
    /// match is being judged" with nothing else read as stuck). Once the replay agrees, the amount
    /// is counted up from zero the way the references count trophies up on their result screen
    /// (Brawl Stars and Clash Royale tick the trophies gained after each match before the total
    /// settles); a draw or a match called off reads "given back". The payment itself is brought to
    /// the contract from the home screen (WagerResult); this card only shows. Hidden when this
    /// device has no stake on the match. Placed by ChogResultScreen.Show:
    /// WagerResultCard.Show(host.transform, localTeam, winnerTeam).
    /// </summary>
    public sealed class WagerResultCard : MonoBehaviour
    {
        private const string ObjectName = "ChogWagerResultCard";
        private const float ReadEverySeconds = 4f;
        private const float CountSeconds = 0.9f;
        // How long the replay usually takes on the free server, said to the player while it runs.
        private const int UsualSeconds = 120;
        private const int LongSeconds = 300;
        // Under the hero, at the left, where the result screen keeps nothing.
        private static readonly Vector2 CardMin = new Vector2(0.06f, 0.09f);
        private static readonly Vector2 CardMax = new Vector2(0.44f, 0.195f);

        private TextMeshProUGUI _title;
        private TextMeshProUGUI _amount;
        private TextMeshProUGUI _detail;
        private float _readAt;
        private bool _counted;
        private int _localTeam;
        private int _matchWinner = -1;
        private float _shownAt;
        private int _waitedShown = -1;

        /// <summary>
        /// Adds the card to the result screen "host" for the local player of "localTeam", when
        /// this device staked on a match whose stake is not paid yet. Nothing otherwise.
        /// "winnerTeam" is the winner the game showed, said at once while the replay confirms it.
        /// </summary>
        public static WagerResultCard Show(Transform host, int localTeam, int winnerTeam = -1)
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
            card._matchWinner = winnerTeam;
            card._shownAt = Time.unscaledTime;
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
            var confirming = GenesisWager.Outcome == WagerOutcome.Unknown || GenesisWager.Outcome == WagerOutcome.Pending;
            // The time waited moves every second, so the card never reads as frozen.
            if (confirming && Waited() != _waitedShown)
                Refresh();
            if (!confirming || !GenesisWager.HasLastMatch || GenesisWager.IsBusy || Time.unscaledTime < _readAt)
                return;
            _readAt = Time.unscaledTime + ReadEverySeconds;
            GenesisWager.ReadOutcome();
        }

        private int Waited() => Mathf.FloorToInt(Time.unscaledTime - _shownAt);

        /// <summary>
        /// The three lines of the card from what is known: the title, the amount (signed) and
        /// the detail. "gainWei" is what a winner gains on top of its stake. "confirming" says the
        /// outcome is the one the game showed and the services are still replaying the match,
        /// for "waitedSeconds" so far; "refundAt" is when a stake the services lost comes back.
        /// </summary>
        public static (string title, string amount, string detail) Describe(WagerOutcome outcome, bool mine, string stakeWei, string gainWei,
                                                                            string currency, double usdPerChog, bool confirming = false,
                                                                            int waitedSeconds = 0, string refundAt = "")
        {
            var stake = ChogPrice.WithUsd(stakeWei, usdPerChog, currency);
            var check = Confirming(waitedSeconds);
            switch (outcome)
            {
                case WagerOutcome.Won when gainWei == "0":
                    return ("YOUR STAKE", stake, "nobody staked against you: it comes back");
                case WagerOutcome.Won when mine:
                    return ("YOU WON", "+" + ChogPrice.WithUsd(gainWei, usdPerChog, currency),
                        confirming ? check : "on top of your " + MonadTransactions.PriceText(stakeWei) + ": claim it on the home screen");
                case WagerOutcome.Won:
                    return ("YOU LOST", "-" + stake, confirming ? check : "your stake goes to the winners");
                case WagerOutcome.Draw:
                    return ("A DRAW", stake, "given back");
                case WagerOutcome.CalledOff:
                    return ("MATCH CALLED OFF", stake, "given back on the home screen");
                case WagerOutcome.Lost:
                    return ("YOUR STAKE", stake, "this match was not recorded: it comes back " + (refundAt.Length > 0 ? "at " + refundAt : "within the hour"));
                default:
                    return ("YOUR STAKE", stake, check);
            }
        }

        // The replay of the match on the services, with the time waited (m:ss).
        private static string Confirming(int waitedSeconds)
        {
            var waited = waitedSeconds / 60 + ":" + (waitedSeconds % 60).ToString("00", CultureInfo.InvariantCulture);
            return waitedSeconds < LongSeconds
                ? "confirming the replay, about " + UsualSeconds / 60 + " min (" + waited + ")"
                : "still confirming (" + waited + "): your stake is safe, see the home screen";
        }

        private void Refresh()
        {
            if (_title == null)
                return;

            var outcome = GenesisWager.Outcome;
            var mine = outcome == WagerOutcome.Won && GenesisWager.WinnerTeam == GenesisWager.LastTeam;
            var gain = GenesisWager.WinnerGain(GenesisWager.LastStakeWei, GenesisWager.LastWinners, GenesisWager.LastLosers, GenesisWager.TakenShare);
            var usdPerChog = GenesisWager.IsTest ? 0 : ChogPrice.UsdPerChog;
            var confirming = outcome == WagerOutcome.Unknown || outcome == WagerOutcome.Pending;
            var shown = outcome;
            if (confirming && (_matchWinner == 0 || _matchWinner == 1))
            {
                // What the game showed, until the replay says the same.
                shown = WagerOutcome.Won;
                mine = _matchWinner == GenesisWager.LastTeam;
                var (with, against) = GenesisWager.StakersFor(_matchWinner);
                gain = GenesisWager.WinnerGain(GenesisWager.LastStakeWei, with, against, GenesisWager.TakenShare);
            }
            _waitedShown = Waited();
            var refundAt = GenesisWager.RefundableAt > 0
                ? System.DateTimeOffset.FromUnixTimeSeconds(GenesisWager.RefundableAt).ToLocalTime().ToString("HH:mm") : "";
            var (title, amount, detail) = Describe(shown, mine, GenesisWager.LastStakeWei, gain, GenesisWager.Currency, usdPerChog,
                confirming, _waitedShown, refundAt);
            _title.text = title;
            // Version 2 of the wager: a wallet that played a Genesis earns its part of the lost stakes, won or lost.
            var asGenesis = GenesisWager.EarnedAsGenesisPlayer();
            _detail.text = asGenesis != "0" ? detail + "\nyour Genesis earns +" + ChogPrice.WithUsd(asGenesis, usdPerChog, GenesisWager.Currency) : detail;
            var decided = outcome == WagerOutcome.Won || outcome == WagerOutcome.Draw || outcome == WagerOutcome.CalledOff;
            _amount.color = shown == WagerOutcome.Won && mine && gain != "0" ? ChogTheme.Hex("FFE27A")
                : shown == WagerOutcome.Won && !mine && gain != "0" ? ChogTheme.Hex("FF9C8A") : Color.white;
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
