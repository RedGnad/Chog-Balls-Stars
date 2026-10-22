using Chog.Match;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// What became of the last match this player staked on, on the menu: a match is judged
    /// after its room closes, so the answer comes once the player is back here. While the match
    /// is not judged the strip says so; then it says who won and offers to bring the referee's
    /// decision to the contract, which pays every winner or gives every stake back, whoever
    /// brings it. Hidden when this device has no stake waiting.
    /// </summary>
    public sealed class WagerResult : MonoBehaviour
    {
        private const string ObjectName = "ChogWagerResult";
        private const float ReadEverySeconds = 4f;

        private static readonly Color PanelColor = ChogTheme.Card;
        private static readonly Color AccentColor = ChogTheme.Cheek;
        private static readonly Color ButtonTextColor = Color.white;
        // A card in the free middle of the top bar, between the player's chip and the wallet, in the theme's fonts with the coin of
        // $CHOG (22 October 2026, the owner: a large plain text thrown across the top of the screen was a bad design).
        private static readonly Vector2 StripMin = new Vector2(0.245f, 0.905f);
        private static readonly Vector2 StripMax = new Vector2(0.645f, 0.99f);

        private TextMeshProUGUI _text;
        private Button _button;
        private Button _close;
        private string _shownKey = "";
        private string _closedOn = "";
        private TextMeshProUGUI _buttonLabel;
        private float _readAt;

        /// <summary>
        /// The line and the button of the strip. "mine" tells whether the player's team won;
        /// "gainWei" is what a winner gains on top of its stake. The button is empty when there
        /// is nothing to tap.
        /// </summary>
        public static (string line, string button) Describe(WagerOutcome outcome, string chainState, bool mine, string stakeWei, string gainWei,
                                                            string calledOffBecause, bool canClaim, string currency = "$CHOG", double usdPerChog = 0,
                                                            bool canRefund = false, string refundAt = "")
        {
            // One short line and one action at most (22 October 2026, the owner: "the UX is not clear, a stake must not float and stay
            // stuck"). What a stake came to, never how the services work.
            var stake = ChogPrice.WithUsd(stakeWei, usdPerChog, currency);
            var paid = chainState == "settled" || chainState == "refunded";
            if (canRefund)
                return ("Your " + stake + " can come back now", "GET IT BACK");
            switch (outcome)
            {
                case WagerOutcome.Pending:
                    return ("Your " + stake + " is at stake: the match is being checked", "");
                case WagerOutcome.Lost:
                    return paid ? ("Your " + stake + " came back", "OK")
                        : ("Your " + stake + " comes back" + (refundAt.Length > 0 ? " at " + refundAt : " within the hour"), "");
                // A team won but only one side had staked: the contract gives every stake back.
                case WagerOutcome.Won when chainState == "refunded" || gainWei == "0":
                    return ("Nobody staked against you: your " + stake + (paid ? " came back" : " comes back"), paid ? "OK" : canClaim ? "GET IT BACK" : "");
                case WagerOutcome.Won when mine:
                    return ("You won +" + ChogPrice.WithUsd(gainWei, usdPerChog, currency), paid ? "OK" : canClaim ? "CLAIM" : "");
                case WagerOutcome.Won:
                    return ("You lost your " + stake, paid ? "OK" : canClaim ? "SETTLE" : "");
                case WagerOutcome.Draw:
                    return ("A draw: your " + stake + (paid ? " came back" : " comes back"), paid ? "OK" : canClaim ? "GET IT BACK" : "");
                case WagerOutcome.CalledOff:
                    return ("Match called off: your " + stake + (paid ? " came back" : " comes back"), paid ? "OK" : canClaim ? "GET IT BACK" : "");
                default:
                    return ("", "");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Attach();
            Attach();
        }

        // Adds the strip to the main screen of the menu, once per menu.
        private static void Attach()
        {
            var menu = FindAnyObjectByType<QuantumMenuUIMain>(FindObjectsInactive.Include);
            if (menu == null || menu.transform.Find(ObjectName) != null)
                return;

            var strip = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(menu.transform, false);
            strip.AddComponent<WagerResult>().Build();
        }

        private void OnEnable()
        {
            GenesisWager.Changed += Refresh;
            ChogPrice.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GenesisWager.Changed -= Refresh;
            ChogPrice.Changed -= Refresh;
        }

        private void Build()
        {
            var area = (RectTransform)transform;
            area.anchorMin = StripMin;
            area.anchorMax = StripMax;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            GetComponent<Image>().color = ChogTheme.CardRaised;
            ChogTheme.AsCard(GetComponent<Image>());
            var frame = gameObject.AddComponent<Outline>();
            frame.effectColor = ChogTheme.Gold;
            frame.effectDistance = new Vector2(3f, -3f);
            var token = Resources.Load<Texture2D>("ChogTokenCoin");
            var textLeft = 0.03f;
            if (token != null)
            {
                var coin = Child("Coin", transform, new Vector2(0.015f, 0.12f), new Vector2(0.105f, 0.88f));
                var picture = coin.gameObject.AddComponent<Image>();
                picture.sprite = Sprite.Create(token, new Rect(0f, 0f, token.width, token.height), new Vector2(0.5f, 0.5f), 100f);
                picture.preserveAspect = true;
                picture.raycastTarget = false;
                textLeft = 0.12f;
            }

            _close = ChogUi.Button("Close", transform, new Vector2(0.905f, 0.18f), new Vector2(0.985f, 0.82f), ChogTheme.ButtonKind.Quiet,
                "X", 26f);
            _close.onClick.AddListener(OnClose);
            _text = ChogUi.Body("Text", transform, new Vector2(textLeft, 0.06f), new Vector2(0.66f, 0.94f), 26f,
                TextAlignmentOptions.Left, Color.white);
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 14f;
            _text.fontSizeMax = 26f;
            var buttonArea = Child("Button", transform, new Vector2(0.67f, 0.14f), new Vector2(0.895f, 0.86f));
            var background = buttonArea.gameObject.AddComponent<Image>();
            background.color = AccentColor;
            _button = buttonArea.gameObject.AddComponent<Button>();
            _button.targetGraphic = background;
            ChogTheme.Shape(_button);
            _button.onClick.AddListener(OnButton);
            _buttonLabel = Text("Label", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), ButtonTextColor, 30f, buttonArea);
            ChogTheme.AsTitle(_buttonLabel);
            Refresh();
        }

        private static long NowSeconds() => (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;

        private static void OnButton()
        {
            if (GenesisWager.Step == WagerStep.Failed)
                GenesisWager.Dismiss();
            else if (GenesisWager.CanRefundExpired(NowSeconds()))
                GenesisWager.RefundExpired();
            else if (GenesisWager.LastChainState == "settled" || GenesisWager.LastChainState == "refunded")
                GenesisWager.ForgetLast();
            else
                GenesisWager.Claim();
        }

        private void Update()
        {
            if (!GenesisWager.HasLastMatch || GenesisWager.IsBusy || Time.unscaledTime < _readAt)
                return;

            // Once the stake is paid or given back there is nothing left to read.
            if (GenesisWager.LastChainState == "settled" || GenesisWager.LastChainState == "refunded")
                return;

            _readAt = Time.unscaledTime + ReadEverySeconds;
            GenesisWager.ReadOutcome();
        }

        private void Refresh()
        {
            if (_text == null)
                return;

            var shown = GenesisWager.State == WagerState.Open && GenesisWager.HasLastMatch;
            Show(shown);
            _button.gameObject.SetActive(false);
            if (!shown)
                return;

            if (GenesisWager.Step != WagerStep.Idle && GenesisWager.Doing == WagerAction.Claim)
            {
                _text.text = WagerPanel.StepText(GenesisWager.Step, GenesisWager.Doing, 0, GenesisWager.Problem, GenesisWager.Chain.Name);
                _button.gameObject.SetActive(GenesisWager.Step == WagerStep.Failed);
                _buttonLabel.text = "OK";
                return;
            }

            var mine = GenesisWager.Outcome == WagerOutcome.Won && GenesisWager.WinnerTeam == GenesisWager.LastTeam;
            var gain = GenesisWager.WinnerGain(GenesisWager.LastStakeWei, GenesisWager.LastWinners, GenesisWager.LastLosers, GenesisWager.TakenShare);
            var refundAt = GenesisWager.RefundableAt > 0
                ? System.DateTimeOffset.FromUnixTimeSeconds(GenesisWager.RefundableAt).ToLocalTime().ToString("HH:mm") : "";
            var (line, button) = Describe(GenesisWager.Outcome, GenesisWager.LastChainState, mine, GenesisWager.LastStakeWei, gain,
                GenesisWager.CalledOffBecause, GenesisWager.CanClaim, GenesisWager.Currency, GenesisWager.IsTest ? 0 : ChogPrice.UsdPerChog,
                GenesisWager.CanRefundExpired(NowSeconds()), refundAt);
            // Nothing is shown before the services answered, nor after the player closed it (until something changes).
            var key = line + "|" + button;
            if (line.Length == 0 || key == _closedOn)
            {
                Show(false);
                return;
            }
            _shownKey = key;
            _text.text = line;
            _button.gameObject.SetActive(button.Length > 0);
            _buttonLabel.text = button;
        }

        // The line, the action and the close of the card, or none of them.
        private void Show(bool shown)
        {
            GetComponent<Image>().enabled = shown;
            GetComponent<Outline>().enabled = shown;
            var coin = transform.Find("Coin");
            if (coin != null)
                coin.gameObject.SetActive(shown);
            _text.gameObject.SetActive(shown);
            _close.gameObject.SetActive(shown);
            if (!shown)
                _button.gameObject.SetActive(false);
        }

        // The close: the card goes until what it says changes (a result arrives, a refund opens).
        private void OnClose()
        {
            _closedOn = _shownKey;
            Show(false);
        }

        private static TextMeshProUGUI Text(string name, Vector2 min, Vector2 max, Color color, float largest, Transform parent)
        {
            var text = Child(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.richText = false;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = largest;
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
