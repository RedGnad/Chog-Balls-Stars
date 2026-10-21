using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// Dresses the screens of the arena scene in the game's look (ChogTheme), as ChogMenuSkin does the
    /// menu: the team preparation (its background, title, team names, slots and buttons) and the texts
    /// of the match. Each action has one coloured face, one contour and one lower lip. The sample
    /// class picker is hidden because Genesis or the single recruit resolves the role. Behaviour is not touched.
    /// </summary>
    public static class ChogGameSkin
    {
        private const string BackgroundName = "ChogBackground";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            SceneManager.sceneLoaded += (scene, mode) => Dress(scene);
            Dress(SceneManager.GetActiveScene());
        }

        private static void Dress(Scene scene)
        {
            if (!scene.IsValid())
                return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var selection = Find(root.transform, "CharacterSelection");
                if (selection != null)
                    DressPreparation(selection);
                foreach (var canvas in new[] { "GameplayCanvas", "GameModeCanvas", "TeamStrategyCanvas", "UIButtonsCanvas" })
                {
                    var found = Find(root.transform, canvas);
                    if (found == null)
                        continue;
                    DressTexts(found);
                    // What the match screens are made of, written to the console with "?uitree=1".
                    if ((Application.absoluteURL ?? "").Contains("uitree=1"))
                        ChogMenuSkin.LogTree(found, 0);
                }

                // Touch controls are calm, flat controls: one outline per visible piece, no glow,
                // splash, concentric effect or chunky-button treatment. The outer move zone and its
                // movable thumb are separate controls, while attacks are one direct button each.
                DressMoveStick(Find(root.transform, "MoveJoystick"));
                DressActionStick(Find(root.transform, "AimBasicJoystick"), ChogTheme.Cheek, ChogIcons.Icon.Crosshair);
                DressActionStick(Find(root.transform, "AimSpecialJoystick"), ChogTheme.Gold, ChogIcons.Icon.Star);
            }
        }

        private static void DressPreparation(Transform selection)
        {
            // The dark backdrop of the screen: the night of the menu.
            var backdrop = Find(selection, "Background");
            var image = backdrop != null ? backdrop.GetComponent<Image>() : null;
            if (image != null && backdrop.Find(BackgroundName) == null)
            {
                image.sprite = ChogSprites.Gradient(ChogTheme.Night, ChogTheme.NightHigh);
                image.color = Color.white;
                image.type = Image.Type.Simple;
                // The two teams: blue at the left, red at the right, a dark seam between (mockup "Lobby").
                var halves = ChogUi.Area(BackgroundName, backdrop, Vector2.zero, Vector2.one);
                halves.SetAsFirstSibling();
                Half(halves, 0f, ChogTheme.Hex("16336B"), ChogTheme.Hex("2A5BB8"));
                Half(halves, 0.5f, ChogTheme.Hex("4A1430"), ChogTheme.Hex("8E2F4E"));
                var seam = ChogUi.Area("Seam", halves, new Vector2(0.44f, 0f), new Vector2(0.56f, 1f)).gameObject.AddComponent<Image>();
                seam.sprite = ChogSprites.Disc(64, 1f);
                seam.color = new Color(ChogTheme.Night.r, ChogTheme.Night.g, ChogTheme.Night.b, 0.85f);
                seam.raycastTarget = false;
            }

            foreach (var button in selection.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("Chog"))
                    continue;
                switch (button.name)
                {
                    case "StartButton":
                        ChogTheme.Style(button, ChogTheme.ButtonKind.Primary);
                        ChogTheme.Paint(button, ChogTheme.ButtonKind.Primary);
                        // The host's clock starts the match by itself (LobbyCountdown); this starts it now.
                        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                            label.text = "START NOW";
                        break;
                    case "ChangeTeamButton":
                        ChogTheme.Style(button, ChogTheme.ButtonKind.Quiet);
                        ChogTheme.Paint(button, ChogTheme.ButtonKind.Quiet);
                        break;
                    default:
                        // The Photon sample exposes its three prototype classes while the player is
                        // joining. Chog Arena does not ask a player to choose one: it resolves the
                        // Genesis or the single recruit automatically.
                        if (button.GetComponent<TwinStickShooter.CharacterSelectionButtonDelegate>() != null)
                            button.gameObject.SetActive(false);
                        else
                        {
                            var kind = button.name.IndexOf("Cancel", System.StringComparison.OrdinalIgnoreCase) >= 0
                                ? ChogTheme.ButtonKind.Danger : ChogTheme.ButtonKind.Quiet;
                            ChogTheme.Style(button, kind);
                            ChogTheme.Paint(button, kind);
                        }
                        break;
                }
            }

            if (selection.GetComponent<ChogPreparationCopy>() == null)
                selection.gameObject.AddComponent<ChogPreparationCopy>();

            // The player's own rank stays in preparation, away from fighter health and attack cues.
            var rank = PlayerRanks.Current?.rank;
            if (rank != null && selection.Find("ChogPlayerRank") == null)
            {
                var badge = ChogUi.Plate("ChogPlayerRank", selection, new Vector2(0.77f, 0.86f),
                    new Vector2(0.93f, 0.93f), rank.tier == "STAR" ? ChogTheme.Gold : ChogTheme.Purple, 12);
                ChogUi.Title("Words", badge.transform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.98f), 27f).text =
                    "RANK " + rank.tier + "  " + rank.points;
            }

            // The slots of the teams: cards in the colour of their team, outlined in ink.
            foreach (var slot in selection.GetComponentsInChildren<Transform>(true))
            {
                if (!slot.name.StartsWith("PlayerSlot"))
                    continue;
                var card = slot.GetComponent<Image>();
                if (card == null)
                {
                    var background = slot.Find("Background");
                    card = background != null ? background.GetComponent<Image>() : null;
                }
                ChogTheme.AsCard(card);
                if (card == null)
                    continue;
                var right = InRightTeam(slot);
                card.sprite = ChogSprites.Rounded(24, 3, ChogTheme.Ink, right ? ChogTheme.Hex("6A1F3A") : ChogTheme.Hex("1F4FA6"));
            }

            DressTexts(selection);
        }

        /// <summary>
        /// Owns only the presentation copy of the Photon preparation screen. The sample refreshes
        /// player cards every view frame, so this runs after it and keeps prototype names out of
        /// Chog Arena without touching selection commands or deterministic game data.
        /// </summary>
        private sealed class ChogPreparationCopy : MonoBehaviour
        {
            private void LateUpdate()
            {
                foreach (var choice in GetComponentsInChildren<TwinStickShooter.CharacterSelectionButtonDelegate>(true))
                    choice.gameObject.SetActive(false);

                foreach (var label in GetComponentsInChildren<TMP_Text>(true))
                {
                    var name = (label.text ?? "").Trim().ToUpperInvariant();
                    if (name == "KNIGHT" || name == "ARCHER" || name == "SPELLCASTER" ||
                        name == "BRUTE" || name == "RANGER" || name == "CONTROLLER")
                    {
                        label.text = "RECRUIT";
                        label.color = ChogTheme.Text;
                    }
                }
            }
        }

        // Large letters in Koulen, the rest in Convergence; our own panels are already dressed.
        private static void DressTexts(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (IsOurs(text.transform) || text.GetComponentInParent<Button>(true) != null)
                    continue;
                if (text.fontSize >= 40f || text.name == "Title")
                    ChogTheme.AsTitle(text);
                else
                    ChogTheme.AsBody(text);
            }
        }

        // A slot of the right team: one under the second team's list (TeamSelectionUI-2).
        private static bool InRightTeam(Transform slot)
        {
            for (var at = slot; at != null; at = at.parent)
                if (at.name.EndsWith("-2"))
                    return true;
            return false;
        }

        private static void Half(Transform parent, float left, Color bottom, Color top)
        {
            var half = ChogUi.Area(left < 0.5f ? "Left" : "Right", parent, new Vector2(left, 0f), new Vector2(left + 0.5f, 1f)).gameObject.AddComponent<Image>();
            half.sprite = ChogSprites.Gradient(bottom, top);
            half.raycastTarget = false;
        }

        private static void DressMoveStick(Transform stick)
        {
            if (stick == null)
                return;
            var field = stick.Find("JoystickArea") != null ? stick.Find("JoystickArea").GetComponent<Image>() : null;
            var thumb = stick.Find("Joystick") != null ? stick.Find("Joystick").GetComponent<Image>() : null;
            if (field != null)
            {
                field.enabled = true;
                field.sprite = ChogSprites.Rounded(48, 2, ChogTheme.Ink,
                    new Color(ChogTheme.PurpleSoft.r, ChogTheme.PurpleSoft.g, ChogTheme.PurpleSoft.b, 0.24f));
                field.type = Image.Type.Simple;
                field.preserveAspect = true;
                field.color = Color.white;
            }
            if (thumb != null)
            {
                thumb.enabled = true;
                thumb.sprite = ChogSprites.Rounded(44, 2, ChogTheme.Ink,
                    new Color(ChogTheme.Cream.r, ChogTheme.Cream.g, ChogTheme.Cream.b, 0.82f));
                thumb.type = Image.Type.Simple;
                thumb.preserveAspect = true;
                thumb.color = Color.white;
            }
        }

        private static void DressActionStick(Transform stick, Color fill, ChogIcons.Icon icon)
        {
            if (stick == null)
                return;
            var field = stick.Find("JoystickArea") != null ? stick.Find("JoystickArea").GetComponent<Image>() : null;
            var button = stick.Find("Joystick") != null ? stick.Find("Joystick").GetComponent<Image>() : null;
            if (field != null)
                field.enabled = false;
            if (button == null)
                return;

            button.enabled = true;
            button.sprite = ChogSprites.Rounded(52, 3, ChogTheme.Ink, fill);
            button.type = Image.Type.Simple;
            button.preserveAspect = true;
            button.color = Color.white;

            var image = button.transform.Find("Img") != null ? button.transform.Find("Img").GetComponent<Image>() : null;
            if (image == null)
                return;
            image.enabled = true;
            image.sprite = ChogIcons.Get(icon);
            image.preserveAspect = true;
            image.color = icon == ChogIcons.Icon.Star ? ChogTheme.Hex("FFE27A") : ChogTheme.Cream;
        }

        private static bool IsOurs(Transform part)
        {
            for (var at = part; at != null; at = at.parent)
                if (at.name.StartsWith("Chog"))
                    return true;
            return false;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true))
                if (part.name == name)
                    return part;
            return null;
        }
    }
}
