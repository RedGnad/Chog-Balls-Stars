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
    /// The end of a match, as the validated mockup "Result" lays it out: the player's team in blue at
    /// the left and the other in red at the right, the result in large outlined letters, the score of
    /// both teams; the player's own hero in 3D on its gold pedestal (ChogHeroStage), cheering after a win,
    /// slumping after a loss; for a Genesis, its mastery road, which moves and says what this match
    /// added once the referee has written it on Monad (GenesisCareers.Recorded); and the way on: PLAY
    /// AGAIN, which leaves this match and looks for the next one at once, or HOME. On a computer Enter
    /// plays again and Escape goes home.
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

            var playing = localTeam >= 0;
            var won = playing && localTeam == winnerTeam;

            // The player's team in blue at the left, the other in red at the right, a dark seam between.
            // The back takes every click, so nothing of the match behind it is touched any more.
            var back = ChogUi.Area("Back", host.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            back.color = ChogTheme.Night;
            Half(host.transform, "Ours", 0f, ChogTheme.Hex("1F4FA6"), ChogTheme.Ally);
            Half(host.transform, "Theirs", 0.5f, ChogTheme.Hex("6A1F3A"), ChogTheme.Hex("B8455F"));
            var seam = ChogUi.Area("Seam", host.transform, new Vector2(0.47f, 0f), new Vector2(0.53f, 1f)).gameObject.AddComponent<Image>();
            seam.sprite = ChogSprites.Disc(64, 1f);
            seam.color = new Color(ChogTheme.Ink.r, ChogTheme.Ink.g, ChogTheme.Ink.b, 0.55f);
            seam.raycastTarget = false;

            var title = ChogUi.Title("Result", host.transform, new Vector2(0.1f, 0.83f), new Vector2(0.9f, 0.985f), 150f);
            title.color = !playing ? ChogTheme.Text : won ? ChogTheme.Hex("FFE27A") : ChogTheme.Hex("E9E4FF");
            title.text = !playing ? (winnerTeam == 0 ? "LEFT TEAM WINS" : "RIGHT TEAM WINS") : won ? "VICTORY!" : "DEFEAT";
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.rectTransform.localRotation = Quaternion.Euler(0f, 0f, won ? 3f : 0f);
            var score = ChogUi.Title("Score", host.transform, new Vector2(0.1f, 0.765f), new Vector2(0.9f, 0.835f), 40f);
            score.text = ScoreLine(playing ? localTeam : 0, playing);

            if (playing && ChogHeroModel.Has(metadataId))
            {
                var glow = ChogUi.Area("Glow", host.transform, new Vector2(0.04f, 0.22f), new Vector2(0.46f, 0.80f)).gameObject.AddComponent<Image>();
                glow.sprite = ChogSprites.Disc(128, 1f);
                glow.color = won ? new Color(1f, 0.90f, 0.63f, 0.45f) : new Color(0.75f, 0.70f, 1f, 0.22f);
                glow.raycastTarget = false;
                var stage = ChogHeroStage.Create(ChogUi.Area("Hero", host.transform, new Vector2(0.08f, 0.30f), new Vector2(0.42f, 0.76f)));
                stage.Show(metadataId);
                stage.Mood(won);

                var name = ChogUi.Title("Name", host.transform, new Vector2(0.06f, 0.21f), new Vector2(0.44f, 0.30f), 50f);
                var attack = ChogHeroModel.AttackLine(metadataId);
                name.text = (metadataId == ChogHeroModel.Recruit ? "RECRUIT" : "CHOG #" + metadataId)
                            + (attack.Length > 0 ? "\n<size=48%><color=#E9E4FF>" + attack.ToUpperInvariant() + "</color></size>" : "");
            }

            // For a Genesis, its mastery road and whether this match is written on Monad; for a recruit,
            // how to have a career.
            ResultControls controls;
            var card = ChogUi.Card("Card", host.transform, new Vector2(0.545f, 0.26f), new Vector2(0.97f, 0.74f), raised: true);
            card.gameObject.SetActive(playing);
            controls = host.AddComponent<ResultControls>();
            controls.TokenId = tokenId;
            if (tokenId > 0)
            {
                controls.Road = MasteryRoad.Create(card.transform, new Vector2(0.03f, 0.24f), new Vector2(0.97f, 0.97f));
                var chip = ChogUi.Plate("Monad", card.transform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.20f),
                    new Color(0.08f, 0.06f, 0.14f, 0.6f), 12);
                controls.Tick = ChogUi.Icon("Tick", chip.transform, ChogIcons.Icon.Check, new Vector2(0.015f, 0.12f), new Vector2(0.09f, 0.88f));
                controls.Tick.enabled = false;
                controls.Career = ChogUi.Body("Career", chip.transform, new Vector2(0.10f, 0.08f), new Vector2(0.70f, 0.92f), 30f);
                controls.Career.text = "Writing this match on Monad...";
                controls.Gained = ChogUi.Title("Gained", chip.transform, new Vector2(0.62f, 0.08f), new Vector2(0.97f, 0.92f), 40f,
                    TextAlignmentOptions.Right, ChogTheme.Hex("9BF7A8"));
                if (GenesisCareers.TryGet(tokenId, out var before))
                {
                    controls.PointsBefore = before.mastery != null ? before.mastery.points : -1;
                    controls.Road.Show(before, MasteryRoad.HasSecondForm(tokenId));
                }
                else
                {
                    controls.Road.Show(null, MasteryRoad.HasSecondForm(tokenId));
                }
            }
            else
            {
                var text = ChogUi.Body("Career", card.transform, new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.9f), 42f, TextAlignmentOptions.Center);
                text.text = "A recruit has no career.\nPlay a Genesis to give it one on Monad: connect the wallet that holds it, or rent one.";
            }

            var again = ChogUi.Button("PlayAgain", host.transform, new Vector2(0.545f, 0.09f), new Vector2(0.80f, 0.215f), ChogTheme.ButtonKind.Primary,
                "PLAY AGAIN", 60f);
            var menu = ChogUi.Button("Menu", host.transform, new Vector2(0.815f, 0.09f), new Vector2(0.97f, 0.215f), ChogTheme.ButtonKind.Secondary,
                "HOME", 52f);
            ChogTheme.SetButtonFill(menu, ChogTheme.Ally);
            if (ControlModeWatcher.Mode == ControlMode.Desktop)
                ChogUi.Body("Keys", host.transform, new Vector2(0.545f, 0.035f), new Vector2(0.97f, 0.08f), 24f, TextAlignmentOptions.Center,
                    ChogTheme.Hex("E9E4FF")).text = "ENTER plays again, ESCAPE goes home";

            again.onClick.AddListener(() => controls.Leave(true));
            menu.onClick.AddListener(() => controls.Leave(false));
            return true;
        }

        // Half of the screen in a team's colour, darker at the bottom.
        private static void Half(Transform parent, string name, float left, Color bottom, Color top)
        {
            var half = ChogUi.Area(name, parent, new Vector2(left, 0f), new Vector2(left + 0.5f, 1f)).gameObject.AddComponent<Image>();
            half.sprite = ChogSprites.Gradient(bottom, top);
            half.raycastTarget = false;
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


        /// <summary>
        /// Leaves the match as the menu's own leave button does (QuantumMenuUIGameplay), shows the menu
        /// and, for PLAY AGAIN, looks for the next match as QUICK PLAY does.
        /// </summary>
        public static async Task LeaveMatch(bool playAgain)
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
            public TextMeshProUGUI Gained;
            public Image Tick;
            public MasteryRoad Road;
            public int TokenId;
            public int PointsBefore = -1;
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
                Career.text = "Recorded on Monad";
                Tick.enabled = true;
                Road.Show(career, MasteryRoad.HasSecondForm(TokenId));
                // What this match added, when the career before it was known.
                var after = career.mastery != null ? career.mastery.points : -1;
                var gained = PointsBefore >= 0 && after > PointsBefore ? after - PointsBefore : 0;
                if (gained > 0)
                    Gained.text = "+" + gained + " MASTERY";
                StartCoroutine(Chimes(Mathf.Max(1, gained)));
            }

            // One chime for the record, a note higher for each point it added.
            private System.Collections.IEnumerator Chimes(int count)
            {
                for (var step = 0; step < count; step++)
                {
                    ChogSounds.Reward(step);
                    yield return new WaitForSecondsRealtime(0.18f);
                }
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



    }
}
