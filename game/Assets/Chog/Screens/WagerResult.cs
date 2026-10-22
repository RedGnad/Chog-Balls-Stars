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
        private TextMeshProUGUI _buttonLabel;
        private float _readAt;

        /// <summary>
        /// The line and the button of the strip. "mine" tells whether the player's team won;
        /// "gainWei" is what a winner gains on top of its stake. The button is empty when there
        /// is nothing to tap.
        /// </summary>
        public static (string line, string button) Describe(WagerOutcome outcome, string chainState, bool mine, string stakeWei, string gainWei,
                                                            string calledOffBecause, bool canClaim, string currency = "$CHOG", double usdPerChog = 0)
        {
            var stake = ChogPrice.WithUsd(stakeWei, usdPerChog, currency);
            var paid = chainState == "settled" || chainState == "refunded";
            switch (outcome)
            {
                case WagerOutcome.Pending:
                    return ("STAKE " + stake + ": the match is being judged", "");
                // A team won but only one side had staked: the contract gives every stake back.
                case WagerOutcome.Won when chainState == "refunded" || gainWei == "0":
                    return ("NOBODY STAKED ON THE OTHER TEAM: YOUR STAKE OF " + stake + (paid ? " WAS GIVEN BACK" : " COMES BACK"),
                        paid ? "OK" : canClaim ? "GET IT BACK" : "");
                case WagerOutcome.Won when mine:
                    return ("YOUR TEAM WON: +" + ChogPrice.WithUsd(gainWei, usdPerChog, currency) + (paid ? ", PAID" : ""), paid ? "OK" : canClaim ? "CLAIM" : "");
                case WagerOutcome.Won:
                    return ("YOUR TEAM LOST: YOUR STAKE OF " + stake + " GOES TO THE WINNERS", paid ? "OK" : canClaim ? "SETTLE" : "");
                case WagerOutcome.Draw:
                    return ("A DRAW: YOUR STAKE OF " + stake + (paid ? " WAS GIVEN BACK" : " COMES BACK"), paid ? "OK" : canClaim ? "GET IT BACK" : "");
                case WagerOutcome.CalledOff:
                    return ("MATCH CALLED OFF (" + (calledOffBecause ?? "").ToUpperInvariant() + "): YOUR " + stake + (paid ? " WERE GIVEN BACK" : " COME BACK"),
                        paid ? "OK" : canClaim ? "GET IT BACK" : "");
                default:
                    return ("YOUR STAKE OF " + stake + ": ASKING THE GAME SERVER", "");
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

            _text = ChogUi.Body("Text", transform, new Vector2(textLeft, 0.06f), new Vector2(0.74f, 0.94f), 26f,
                TextAlignmentOptions.Left, Color.white);
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 14f;
            _text.fontSizeMax = 26f;
            var buttonArea = Child("Button", transform, new Vector2(0.76f, 0.12f), new Vector2(0.98f, 0.88f));
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

        private static void OnButton()
        {
            if (GenesisWager.Step == WagerStep.Failed)
                GenesisWager.Dismiss();
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
            GetComponent<Image>().enabled = shown;
            GetComponent<Outline>().enabled = shown;
            var coinShown = transform.Find("Coin");
            if (coinShown != null)
                coinShown.gameObject.SetActive(shown);
            _text.gameObject.SetActive(shown);
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
            var (line, button) = Describe(GenesisWager.Outcome, GenesisWager.LastChainState, mine, GenesisWager.LastStakeWei, gain,
                GenesisWager.CalledOffBecause, GenesisWager.CanClaim, GenesisWager.Currency, GenesisWager.IsTest ? 0 : ChogPrice.UsdPerChog);
            _text.text = line;
            _button.gameObject.SetActive(button.Length > 0);
            _buttonLabel.text = button;
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
