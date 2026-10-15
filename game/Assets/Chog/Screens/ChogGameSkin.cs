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
    /// of the match. Buttons keep their colours where a colour means something (the colour of each
    /// class); they become pills with a hard shadow and Koulen letters. Behaviour is not touched.
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
                foreach (var canvas in new[] { "GameplayCanvas", "GameModeCanvas", "TeamStrategyCanvas" })
                {
                    var found = Find(root.transform, canvas);
                    if (found != null)
                        DressTexts(found);
                }

                // The sticks of the touch controls, in the colours of the game: moving in lavender, the
                // basic attack in cheek pink, the special in gold. Their transparency is kept.
                Tint(Find(root.transform, "MoveJoystick"), ChogTheme.PurpleSoft);
                Tint(Find(root.transform, "AimBasicJoystick"), ChogTheme.Cheek);
                Tint(Find(root.transform, "AimSpecialJoystick"), ChogTheme.Gold);
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
            }

            foreach (var button in selection.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("Chog"))
                    continue;
                switch (button.name)
                {
                    case "StartButton":
                        ChogTheme.Style(button, ChogTheme.ButtonKind.Primary);
                        break;
                    case "ChangeTeamButton":
                        ChogTheme.Style(button, ChogTheme.ButtonKind.Quiet);
                        break;
                    default:
                        // A class: its own colour, dark letters on it, the name this game gives it.
                        ChogTheme.Shape(button);
                        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                        {
                            label.color = ChogTheme.Night;
                            label.text = ChogTheme.ClassName(label.text);
                        }
                        break;
                }
            }

            // The slots of the teams: dark cards.
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
            }

            DressTexts(selection);
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

        private static void Tint(Transform stick, Color colour)
        {
            if (stick == null)
                return;
            foreach (var image in stick.GetComponentsInChildren<Image>(true))
                image.color = new Color(colour.r, colour.g, colour.b, image.color.a);
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
