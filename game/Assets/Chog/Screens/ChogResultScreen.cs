using Chog.Match;
using Quantum.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The end of a match, in the game's look: the result in large Koulen letters and, beside it, the
    /// player's own hero in 3D (ChogHeroStage), cheering after a win, slumping after a loss, its name under
    /// it. It takes the place of the sample's one line of text (CoinGrabUI). A spectator, who plays no
    /// hero, reads which team won, without a hero. BACK TO MENU (or Enter, Space or Escape) leaves the
    /// match the way the menu's own leave button does (QuantumMenuUIGameplay): the sample left the end of
    /// a match without a way out.
    /// </summary>
    public static class ChogResultScreen
    {
        private const string ObjectName = "ChogResultScreen";

        /// <summary>
        /// Shows the screen for the local player of "localTeam" (below 0 for a spectator), the team that
        /// won, and the hero played (its metadata id, ChogHeroModel.Recruit for a recruit). Returns whether
        /// it was shown, so the sample's text can step aside.
        /// </summary>
        public static bool Show(int localTeam, int winnerTeam, int metadataId)
        {
            if (GameObject.Find(ObjectName) != null)
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
            dim.color = new Color(ChogTheme.Night.r, ChogTheme.Night.g, ChogTheme.Night.b, 0.78f);
            dim.raycastTarget = false;

            var card = Child<Image>("Card", host.transform, new Vector2(0.16f, 0.14f), new Vector2(0.84f, 0.86f));
            ChogTheme.AsCard(card);
            card.raycastTarget = false;

            var playing = localTeam >= 0;
            var won = playing && localTeam == winnerTeam;
            var textLeft = 0.06f;
            if (playing && ChogHeroModel.Has(metadataId))
            {
                var glow = Child<Image>("Glow", card.transform, new Vector2(0.02f, 0.04f), new Vector2(0.48f, 0.5f));
                glow.sprite = ChogSprites.Disc(128, 1f);
                glow.color = won ? new Color(1f, 0.85f, 0.3f, 0.35f) : new Color(0.51f, 0.43f, 0.98f, 0.25f);
                glow.raycastTarget = false;
                var stageArea = new GameObject("Hero", typeof(RectTransform)).GetComponent<RectTransform>();
                Stretch(stageArea, card.transform, new Vector2(0.02f, 0.10f), new Vector2(0.48f, 0.98f));
                var stage = ChogHeroStage.Create(stageArea);
                stage.Show(metadataId);
                stage.Mood(won);

                var name = Text("Name", card.transform, new Vector2(0.02f, 0.02f), new Vector2(0.48f, 0.12f), 52f);
                ChogTheme.AsTitle(name);
                name.color = ChogTheme.Cream;
                var attack = ChogHeroModel.AttackLine(metadataId);
                name.text = (metadataId == ChogHeroModel.Recruit ? "RECRUIT" : "CHOG #" + metadataId)
                            + (attack.Length > 0 ? "\n<size=45%><color=#A99BFC>" + attack + "</color></size>" : "");
                textLeft = 0.50f;
            }

            var title = Text("Result", card.transform, new Vector2(textLeft, 0.48f), new Vector2(0.96f, 0.86f), 190f);
            ChogTheme.AsTitle(title);
            title.color = !playing ? ChogTheme.Text : won ? ChogTheme.Gold : ChogTheme.PurpleSoft;
            title.text = !playing ? (winnerTeam == 0 ? "LEFT TEAM WINS" : "RIGHT TEAM WINS") : won ? "VICTORY!" : "DEFEAT";

            var line = Text("Line", card.transform, new Vector2(textLeft, 0.30f), new Vector2(0.96f, 0.48f), 60f);
            ChogTheme.AsBody(line);
            line.color = ChogTheme.Text;
            line.text = !playing ? "" : won ? "Your team won the match." : "Your team lost this one.";

            var back = Child<Image>("Back", card.transform, new Vector2(textLeft + 0.06f, 0.08f), new Vector2(0.90f, 0.24f));
            var button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            var label = Text("Label", back.transform, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f), 64f);
            label.text = "BACK TO MENU";
            ChogTheme.Style(button, ChogTheme.ButtonKind.Primary);
            button.onClick.AddListener(() => Leave(host));
            host.AddComponent<LeaveKeys>().Host = host;
            return true;
        }

        // Leaves the match as the menu's leave button does, then takes the screen away.
        private static void Leave(GameObject host)
        {
            if (host == null)
                return;
            var gameplay = Object.FindFirstObjectByType<QuantumMenuUIGameplay>(FindObjectsInactive.Include);
            if (gameplay != null)
                gameplay.SendMessage("OnDisconnectPressed", SendMessageOptions.DontRequireReceiver);
            else
                Debug.LogWarning("CHOG_RESULT no menu to go back to");
            Object.Destroy(host);
        }

        // On a computer the keys that close a card: Enter, Space or Escape.
        private sealed class LeaveKeys : MonoBehaviour
        {
            public GameObject Host;

            private void Update()
            {
                var keys = Keyboard.current;
                if (keys != null && (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame))
                    Leave(Host);
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
