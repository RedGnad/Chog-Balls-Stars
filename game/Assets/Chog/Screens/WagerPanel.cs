using System;
using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The stakes of a match, on the screen where the teams are prepared: a signed-in player
    /// picks an amount of $CHOG, or sees the one the match is already played for, and stakes it
    /// on its own team; everyone sees how many staked and what a winner gains. When the match
    /// is started the stakes are locked first. Shown only where the game services stake on
    /// matches (GenesisWager); elsewhere the screen is as it was.
    /// </summary>
    public sealed class WagerPanel : MonoBehaviour
    {
        private const string ObjectName = "ChogWagerPanel";
        private const float ReadEverySeconds = 3f;

        // Whole $CHOG a player can stake when the match has no stake yet.
        private static readonly int[] Amounts = { 1, 5, 10, 25, 50, 100, 250, 500, 1000 };
        private const int FirstAmount = 2;

        private static readonly Color PanelColor = ChogTheme.Card;
        private static readonly Color AccentColor = ChogTheme.Cheek;
        private static readonly Color ButtonTextColor = Color.white;
        // The free corner of the preparation screen, under the first team's slots.
        private static readonly Vector2 PanelMin = new Vector2(0.02f, 0.02f);
        private static readonly Vector2 PanelMax = new Vector2(0.38f, 0.17f);
        // Where the test tokens button sits, and the line beside it when it is shown.
        private static readonly Vector2 TapMin = new Vector2(0.70f, 0.50f);
        private static readonly Vector2 StatusMax = new Vector2(0.97f, 0.97f);
        private static readonly Vector2 StatusMaxBesideTap = new Vector2(0.68f, 0.97f);

        private static int s_team = -1;

        private TextMeshProUGUI _status;
        private Button _less;
        private Button _more;
        private Button _stake;
        private TextMeshProUGUI _stakeLabel;
        private Button _tap;
        private int _amount = FirstAmount;
        private float _readAt;

        /// <summary>
        /// Shows the panel on the preparation screen "screen" and tells it which team the local
        /// player is in (-1 while it has no character). "template" is any text of the screen:
        /// the panel copies its font from it. Called every frame the screen is shown.
        /// </summary>
        public static void Show(Transform screen, TextMeshProUGUI template, int localTeam)
        {
            s_team = localTeam;
            if (GenesisWager.State != WagerState.Open || screen.Find(ObjectName) != null)
                return;

            // Wagering is opt-in. A player who has not signed in and is not in a wager should see a
            // clean preparation screen, rather than a second unexplained task beside starting a match.
            var involved = PlayerWallet.SignIn == SignInState.SignedIn || GenesisWager.Stakers.Count > 0 ||
                GenesisWager.StakeWei.Length > 0 || GenesisWager.IsBusy;
            if (!involved)
                return;

            var panel = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(screen, false);
            panel.AddComponent<WagerPanel>().Build(template);
        }

        /// <summary>Starts the match once its stakes are locked; at once when there is nothing to lock.</summary>
        public static void BeforeStart(Action start) => GenesisWager.WhenLocked(start);

        /// <summary>
        /// The line the panel shows, from what is known of the stakes of the match. "currency"
        /// is what the stakes are called: "TEST $CHOG" when the wager is on the testnet.
        /// </summary>
        public static string StatusText(bool signedIn, bool inMatch, bool locked, int stakers, string stakeWei, int stakedTeam,
                                        int winnersIfMine, int losersIfMine, int burnedShare, string currency = "$CHOG")
        {
            if (!signedIn)
                return "SIGN IN ON THE MENU TO STAKE " + currency + " ON A MATCH";
            if (!inMatch)
                return "STAKES: WAITING FOR THE GAME SERVER";

            var each = stakeWei.Length > 0 ? MonadTransactions.PriceText(stakeWei) + " " + currency : "";
            if (stakedTeam >= 0)
            {
                var gain = GenesisWager.WinnerGain(stakeWei, winnersIfMine, losersIfMine, burnedShare);
                var toWin = gain == "0" ? "NOBODY ON THE OTHER TEAM YET" : "TO WIN +" + MonadTransactions.PriceText(gain);
                return "YOU STAKED " + each + (locked ? "  LOCKED" : "") + "\n" + stakers + " STAKED  " + toWin;
            }

            if (locked)
                return "THE STAKES ARE LOCKED\n" + stakers + " STAKED " + each;
            return stakeWei.Length > 0
                ? "THIS MATCH IS PLAYED FOR " + each + " EACH\n" + stakers + " STAKED"
                : "STAKE " + currency + " ON YOUR TEAM\nTHE WINNERS SHARE WHAT THE LOSERS STAKED";
        }

        /// <summary>What the panel says while something is under way, or empty. "chainName" is the chain of the wager.</summary>
        public static string StepText(WagerStep step, WagerAction doing, int lockWaitSeconds, string problem, string chainName = "Monad")
        {
            switch (step)
            {
                case WagerStep.Asking: return "ASKING THE REFEREE FOR A TICKET";
                case WagerStep.Switching: return "YOUR WALLET: SWITCH TO " + (chainName ?? "").ToUpperInvariant();
                case WagerStep.Approving: return "YOUR WALLET: ALLOW THE STAKE";
                case WagerStep.Sending:
                    return doing == WagerAction.Claim ? "YOUR WALLET: CONFIRM THE PAYMENT"
                        : doing == WagerAction.TestTokens ? "YOUR WALLET: CONFIRM THE TEST TOKENS"
                        : "YOUR WALLET: CONFIRM THE STAKE";
                case WagerStep.Confirming: return "WAITING FOR THE CHAIN";
                case WagerStep.Locking:
                    return lockWaitSeconds > 0 ? "LOCKING THE STAKES: A TICKET IS STILL OUT, " + lockWaitSeconds + " S" : "LOCKING THE STAKES";
                case WagerStep.Failed: return (problem ?? "").ToUpperInvariant() + "\nTAP TO TRY AGAIN";
                default: return "";
            }
        }

        private void OnEnable() => GenesisWager.Changed += Refresh;

        private void OnDisable() => GenesisWager.Changed -= Refresh;

        private void Build(TextMeshProUGUI template)
        {
            var area = (RectTransform)transform;
            area.anchorMin = PanelMin;
            area.anchorMax = PanelMax;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            var background = GetComponent<Image>();
            background.color = PanelColor;
            ChogTheme.AsCard(background);
            // A tap on the panel puts a failed stake away.
            gameObject.AddComponent<Button>().onClick.AddListener(GenesisWager.Dismiss);

            _status = Text(template, "Status", new Vector2(0.03f, 0.45f), StatusMax, Color.white, 30f, transform);
            _less = MakeButton(template, "Less", new Vector2(0.03f, 0.06f), new Vector2(0.15f, 0.42f), "-", out _);
            _more = MakeButton(template, "More", new Vector2(0.17f, 0.06f), new Vector2(0.29f, 0.42f), "+", out _);
            _stake = MakeButton(template, "Stake", new Vector2(0.32f, 0.06f), new Vector2(0.97f, 0.42f), "", out _stakeLabel);
            // On the testnet only: test tokens from the tap of the test token, beside the line.
            _tap = MakeButton(template, "TestTokens", TapMin, new Vector2(0.97f, 0.95f), "GET 1000\nTEST $CHOG", out _);
            _less.onClick.AddListener(() => Step(-1));
            _more.onClick.AddListener(() => Step(1));
            _stake.onClick.AddListener(() => GenesisWager.Stake(s_team, Amounts[_amount]));
            _tap.onClick.AddListener(GenesisWager.TakeTestTokens);
            Refresh();
        }

        private void Step(int by)
        {
            _amount = Mathf.Clamp(_amount + by, 0, Amounts.Length - 1);
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < _readAt || string.IsNullOrEmpty(PlayerWallet.Session) || GenesisWager.IsBusy)
                return;

            _readAt = Time.unscaledTime + ReadEverySeconds;
            GenesisWager.ReadMine();
        }

        private void Refresh()
        {
            if (_status == null)
                return;

            var signedIn = !string.IsNullOrEmpty(PlayerWallet.Session);
            var stakers = GenesisWager.Stakers;
            var stakedTeam = GenesisWager.TeamStakedOn(stakers, PlayerWallet.Address);
            var winners = 0;
            foreach (var staker in stakers)
                if (staker.Team == stakedTeam)
                    winners++;

            var busy = GenesisWager.Step != WagerStep.Idle;
            var currency = GenesisWager.Currency;
            _status.text = busy
                ? StepText(GenesisWager.Step, GenesisWager.Doing, GenesisWager.LockWaitSeconds, GenesisWager.Problem, GenesisWager.Chain.Name)
                : StatusText(signedIn, GenesisWager.InMatch, GenesisWager.Locked, stakers.Count, GenesisWager.StakeWei, stakedTeam,
                    winners, stakers.Count - winners, GenesisWager.BurnedShare, currency);
            var tap = signedIn && stakedTeam < 0 && GenesisWager.CanTakeTestTokens;
            _tap.gameObject.SetActive(tap);
            _status.rectTransform.anchorMax = tap ? StatusMaxBesideTap : StatusMax;

            var canStake = signedIn && GenesisWager.InMatch && !GenesisWager.Locked && stakedTeam < 0 && !busy && s_team >= 0
                && PlayerWallet.State == WalletState.Ready;
            var free = canStake && GenesisWager.StakeWei.Length == 0;
            _less.gameObject.SetActive(free);
            _more.gameObject.SetActive(free);
            _stake.gameObject.SetActive(canStake);
            _stakeLabel.text = "STAKE " + (GenesisWager.StakeWei.Length > 0 ? MonadTransactions.PriceText(GenesisWager.StakeWei) : Amounts[_amount].ToString())
                + " " + currency;
        }

        private Button MakeButton(TextMeshProUGUI template, string name, Vector2 min, Vector2 max, string label, out TextMeshProUGUI text)
        {
            var area = Child(name, transform, min, max);
            var background = area.gameObject.AddComponent<Image>();
            background.color = AccentColor;
            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ChogTheme.Shape(button);
            text = Text(template, "Label", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), ButtonTextColor, 34f, area);
            ChogTheme.AsTitle(text);
            text.text = label;
            return button;
        }

        private static TextMeshProUGUI Text(TextMeshProUGUI template, string name, Vector2 min, Vector2 max, Color color, float largest, Transform parent)
        {
            var text = Instantiate(template, parent);
            text.name = name;
            var area = text.rectTransform;
            area.anchorMin = min;
            area.anchorMax = max;
            area.pivot = new Vector2(0.5f, 0.5f);
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            text.richText = false;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = largest;
            text.text = "";
            return text;
        }

        private static RectTransform Child(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
