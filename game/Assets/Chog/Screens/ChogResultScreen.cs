using System.Threading.Tasks;
using Chog.Match;
using Quantum;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace Chog.Screens
{
    /// <summary>
    /// The end of a match, in the game's look: the result in large Koulen letters and, beside it, the
    /// player's own hero in 3D (ChogHeroStage), cheering after a win, slumping after a loss, its name under
    /// it; the score of both teams; for a Genesis, its career once the referee has written this match
    /// on Monad (GenesisCareers.Recorded); and the way on: PLAY AGAIN, which leaves this match and looks
    /// for the next one at once, or MENU. On a computer Enter plays again and Escape goes to the menu.
    /// It takes the place of the sample's one line of text (CoinGrabUI), which left the end of a match
    /// without a way out. A spectator, who plays no hero, reads which team won, without a hero.
    /// </summary>
    public static class ChogResultScreen
    {
        private const string ObjectName = "ChogResultScreen";
        // The buttons answer after this long, so a click or a key still held from the match does not leave at once.
        private const float ArmSeconds = 1.2f;

        /// <summary>Whether the end of a match is on screen.</summary>
        public static bool IsShown => GameObject.Find(ObjectName) != null;

        /// <summary>
        /// Shows the screen for the local player of "localTeam" (below 0 for a spectator), the team that
        /// won, the hero played (its metadata id, ChogHeroModel.Recruit for a recruit) and the Genesis
        /// behind it (0 for none). The scores are read from the last verified frame. Returns whether it
        /// was shown, so the sample's text can step aside.
        /// </summary>
        public static bool Show(int localTeam, int winnerTeam, int metadataId, int tokenId = 0)
        {
            if (IsShown)
                return true;

            var host = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var dim = Child<Image>("Dim", host.transform, Vector2.zero, Vector2.one);
            dim.color = new Color(ChogTheme.Night.r, ChogTheme.Night.g, ChogTheme.Night.b, 0.82f);
            // The dim takes every click, so nothing of the match behind it is touched any more.
            dim.raycastTarget = true;

            var card = Child<Image>("Card", host.transform, new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.90f));
            ChogTheme.AsCard(card);
            card.raycastTarget = false;

            var playing = localTeam >= 0;
            var won = playing && localTeam == winnerTeam;
            var textLeft = 0.06f;
            if (playing && ChogHeroModel.Has(metadataId))
            {
                var glow = Child<Image>("Glow", card.transform, new Vector2(0.02f, 0.06f), new Vector2(0.46f, 0.52f));
                glow.sprite = ChogSprites.Disc(128, 1f);
                glow.color = won ? new Color(1f, 0.85f, 0.3f, 0.35f) : new Color(0.51f, 0.43f, 0.98f, 0.25f);
                glow.raycastTarget = false;
                var stageArea = new GameObject("Hero", typeof(RectTransform)).GetComponent<RectTransform>();
                Stretch(stageArea, card.transform, new Vector2(0.02f, 0.12f), new Vector2(0.46f, 0.98f));
                var stage = ChogHeroStage.Create(stageArea);
                stage.Show(metadataId);
                stage.Mood(won);

                var name = Text("Name", card.transform, new Vector2(0.02f, 0.02f), new Vector2(0.46f, 0.13f), 52f);
                ChogTheme.AsTitle(name);
                name.color = ChogTheme.Cream;
                var attack = ChogHeroModel.AttackLine(metadataId);
                name.text = (metadataId == ChogHeroModel.Recruit ? "RECRUIT" : "CHOG #" + metadataId)
                            + (attack.Length > 0 ? "\n<size=45%><color=#A99BFC>" + attack + "</color></size>" : "");
                textLeft = 0.48f;
            }

            var title = Text("Result", card.transform, new Vector2(textLeft, 0.64f), new Vector2(0.96f, 0.95f), 180f);
            ChogTheme.AsTitle(title);
            title.color = !playing ? ChogTheme.Text : won ? ChogTheme.Gold : ChogTheme.PurpleSoft;
            title.text = !playing ? (winnerTeam == 0 ? "LEFT TEAM WINS" : "RIGHT TEAM WINS") : won ? "VICTORY!" : "DEFEAT";

            var score = Text("Score", card.transform, new Vector2(textLeft, 0.50f), new Vector2(0.96f, 0.64f), 64f);
            ChogTheme.AsTitle(score);
            score.color = ChogTheme.Text;
            score.text = ScoreLine(playing ? localTeam : 0, playing);

            var career = Text("Career", card.transform, new Vector2(textLeft, 0.30f), new Vector2(0.96f, 0.50f), 44f);
            ChogTheme.AsBody(career);
            career.color = ChogTheme.Muted;
            career.text = !playing ? ""
                : tokenId > 0 ? "Your Genesis's career is being written on Monad..."
                : "Play a Genesis to give it a career: connect the wallet that holds it, or rent one.";

            var again = MakeButton("PlayAgain", card.transform, new Vector2(textLeft + 0.02f, 0.08f), new Vector2(textLeft + 0.29f, 0.25f), "PLAY AGAIN", ChogTheme.ButtonKind.Primary);
            var menu = MakeButton("Menu", card.transform, new Vector2(textLeft + 0.31f, 0.08f), new Vector2(0.94f, 0.25f), "MENU", ChogTheme.ButtonKind.Quiet);

            var controls = host.AddComponent<ResultControls>();
            controls.Career = career;
            controls.TokenId = tokenId;
            again.onClick.AddListener(() => controls.Leave(true));
            menu.onClick.AddListener(() => controls.Leave(false));
            return true;
        }

        // "YOUR TEAM 99 - 80 THEM" in damage to the structures (a siege) or coins, from the last verified frame.
        private static unsafe string ScoreLine(int ours, bool playing)
        {
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var frame = game != null && game.Frames != null ? game.Frames.Verified : null;
            if (frame == null)
                return "";
            var teams = frame.ResolveList(frame.Global->TeamsData);
            if (teams.Count < 2)
                return "";
            var mine = teams[ours == 1 ? 1 : 0].Score;
            var theirs = teams[ours == 1 ? 0 : 1].Score;
            var unit = MatchModes.IsSiege(frame) ? "damage" : "coins";
            return playing
                ? "<color=#A99BFC>YOUR TEAM</color>  " + mine + "  -  " + theirs + "  <color=#A99BFC>THEM</color>  <size=55%>" + unit + "</size>"
                : "LEFT " + teams[0].Score + "  -  " + teams[1].Score + " RIGHT  <size=55%>" + unit + "</size>";
        }

        private static Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, string text, ChogTheme.ButtonKind kind)
        {
            var back = Child<Image>(name, parent, min, max);
            back.raycastTarget = true;
            var button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            var label = Text("Label", back.transform, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f), 64f);
            label.text = text;
            ChogTheme.Style(button, kind);
            return button;
        }

        /// <summary>
        /// Leaves the match as the menu's own leave button does (QuantumMenuUIGameplay), shows the menu
        /// and, for PLAY AGAIN, looks for the next match as QUICK PLAY does.
        /// </summary>
        private static async Task LeaveMatch(bool playAgain)
        {
            var gameplay = Object.FindFirstObjectByType<QuantumMenuUIGameplay>(FindObjectsInactive.Include);
            if (gameplay == null || gameplay.Connection == null || gameplay.Controller == null)
            {
                Debug.LogWarning("CHOG_RESULT no menu to go back to");
                return;
            }

            await gameplay.Connection.DisconnectAsync(ConnectFailReason.UserRequest);
            gameplay.Controller.Show<QuantumMenuUIMain>();
            Debug.Log("CHOG_RESULT " + (playAgain ? "play again" : "back to the menu"));
            if (playAgain)
            {
                var main = gameplay.Controller.Get<QuantumMenuUIMain>();
                if (main != null)
                    main.SendMessage("OnPlayButtonPressed", SendMessageOptions.DontRequireReceiver);
            }
        }

        // The keys, the delay before the buttons answer, and the career written while the screen is up.
        private sealed class ResultControls : MonoBehaviour
        {
            public TextMeshProUGUI Career;
            public int TokenId;
            private float _shownAt;
            private bool _leaving;

            private void Awake()
            {
                _shownAt = Time.unscaledTime;
                GenesisCareers.Recorded += OnRecorded;
            }

            private void OnDestroy() => GenesisCareers.Recorded -= OnRecorded;

            private void OnRecorded(GenesisCareer career, bool credited)
            {
                if (Career == null || career == null || career.tokenId != TokenId || !credited)
                    return;
                Career.color = ChogTheme.Text;
                Career.text = "<color=#5CE68A>Recorded on Monad.</color>  " + GenesisCareers.DescribeAll(career);
            }

            private void Update()
            {
                var keys = Keyboard.current;
                if (keys == null || _leaving)
                    return;
                if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame)
                    Leave(true);
                else if (keys.escapeKey.wasPressedThisFrame)
                    Leave(false);
            }

            public async void Leave(bool playAgain)
            {
                if (_leaving || Time.unscaledTime - _shownAt < ArmSeconds)
                    return;
                _leaving = true;
                // Hidden at once, taken away once the menu is back: this object runs the leaving.
                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);
                await LeaveMatch(playAgain);
                Destroy(gameObject);
            }
        }

        private static T Child<T>(string name, Transform parent, Vector2 min, Vector2 max) where T : Component
        {
            var thing = new GameObject(name, typeof(RectTransform));
            Stretch((RectTransform)thing.transform, parent, min, max);
            return thing.AddComponent<T>();
        }

        private static TextMeshProUGUI Text(string name, Transform parent, Vector2 min, Vector2 max, float largest)
        {
            var text = Child<TextMeshProUGUI>(name, parent, min, max);
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = largest;
            text.raycastTarget = false;
            text.richText = true;
            return text;
        }

        private static void Stretch(RectTransform area, Transform parent, Vector2 min, Vector2 max)
        {
            area.SetParent(parent, false);
            area.anchorMin = min;
            area.anchorMax = max;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
        }
    }
}
