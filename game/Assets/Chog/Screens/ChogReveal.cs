using System.Collections;
using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using GenesisRoster = Quantum.GenesisRoster;

namespace Chog.Screens
{
    /// <summary>
    /// The moment a Genesis comes to life (docs/DESIGN-BRIEF.md), the first time this browser plays it:
    /// its NFT picture pops up, turns over and becomes its hero in 3D, then each trait flies to the hero
    /// with what it gives (the class from the outfit, the basic attack from the hat, the special from
    /// the eyes, the effect from the aura). About four seconds; a tap anywhere skips it. A page tested
    /// by script ("perf=1" or "noguide=1" in its address) never shows it.
    /// </summary>
    public sealed class ChogReveal : MonoBehaviour
    {
        private const string ObjectName = "ChogReveal";
        private const string SeenKey = "chog.revealed.";
        private const float CardSize = 420f;
        private const float StayAfter = 2.2f;

        private RectTransform _card;
        private RawImage _picture;
        private RectTransform _heroArea;
        private ChogHeroStage _stage;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _subtitle;
        private TextMeshProUGUI _hint;
        private readonly RectTransform[] _traits = new RectTransform[4];
        private bool _skipped;

        /// <summary>Shows the moment for this Genesis unless this browser has already seen it.</summary>
        public static void ShowOnce(Transform main, int tokenId)
        {
            var url = Application.absoluteURL ?? "";
            if (tokenId <= 0 || url.Contains("perf=1") || url.Contains("noguide=1") || main.Find(ObjectName) != null)
                return;
            var key = SeenKey + tokenId;
            if (PlayerPrefs.GetInt(key, 0) == 1)
                return;
            try
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
            catch (System.Exception)
            {
                // Not kept: the moment may show again on the next visit.
            }

            var roster = Resources.Load<GenesisRoster>("GenesisRoster");
            var index = roster != null ? roster.IndexOf(tokenId) : -1;
            if (index < 0)
                return;
            var dim = ChogUi.Dim(ObjectName, main, 0.97f);
            dim.transform.SetAsLastSibling();
            var reveal = dim.gameObject.AddComponent<ChogReveal>();
            dim.gameObject.AddComponent<Button>().onClick.AddListener(reveal.Skip);
            reveal.StartCoroutine(reveal.Play(roster, index));
        }

        private void Skip()
        {
            // A first tap hurries the moment to its end, a second one closes it.
            if (_skipped)
                Destroy(gameObject);
            _skipped = true;
        }

        private IEnumerator Play(GenesisRoster roster, int index)
        {
            var hero = roster.Entries[index];
            var lines = roster.CardLines(hero, GenesisForms.PlayedBy(hero.TokenId));
            Build(hero.MetadataId, GenesisHeroCard.DisplayName(hero).ToUpperInvariant(),
                new[] { "ITS OUTFIT GIVES", "ITS HAT GIVES", "ITS EYES GIVE", "ITS AURA GIVES" },
                new[] { lines[0].Gives, ChogHeroModel.AttackLine(hero.MetadataId), lines[1].Gives, lines[2].Gives });

            // The picture pops up, then turns over into the hero.
            yield return Animate(0.45f, t => _card.localScale = Vector3.one * Overshoot(t));
            yield return Wait(0.5f);
            yield return Animate(0.22f, t => _card.localScale = new Vector3(1f - t, 1f, 1f));
            _card.gameObject.SetActive(false);
            _heroArea.gameObject.SetActive(true);
            _stage.Show(ChogHeroModel.Has(hero.MetadataId) ? hero.MetadataId : ChogHeroModel.Recruit);
            yield return Animate(0.3f, t => _heroArea.localScale = new Vector3(Overshoot(t), 1f, 1f));
            _stage.Mood(true);

            // Each trait flies from the middle to its place beside the hero.
            for (var i = 0; i < _traits.Length; i++)
            {
                var trait = _traits[i];
                trait.gameObject.SetActive(true);
                var to = trait.anchoredPosition;
                yield return Animate(0.32f, t =>
                {
                    trait.anchoredPosition = Vector2.LerpUnclamped(Vector2.zero, to, Ease(t));
                    trait.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, Ease(t));
                });
                yield return Wait(0.12f);
            }

            _title.gameObject.SetActive(true);
            _subtitle.gameObject.SetActive(true);
            yield return Animate(0.3f, t => _title.transform.localScale = Vector3.one * Overshoot(t));
            _hint.gameObject.SetActive(true);
            yield return Wait(StayAfter);
            Destroy(gameObject);
        }

        private void Build(int metadataId, string name, string[] traits, string[] gives)
        {
            _card = ChogUi.Pinned("Card", transform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * CardSize);
            ChogUi.Plate("Frame", _card, Vector2.zero, Vector2.one, ChogTheme.Action, 22);
            _picture = ChogUi.Area("Picture", _card, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
            _picture.rectTransform.offsetMin = new Vector2(12f, 12f);
            _picture.rectTransform.offsetMax = new Vector2(-12f, -12f);
            _picture.raycastTarget = false;
            _picture.texture = GenesisArt.Disc();
            GenesisArt.Request(metadataId, texture =>
            {
                if (_picture != null)
                    _picture.texture = texture;
            });
            _card.localScale = Vector3.zero;

            var glow = ChogUi.Area("Glow", transform, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.95f)).gameObject.AddComponent<Image>();
            glow.sprite = ChogSprites.Disc(128, 1f);
            glow.color = new Color(1f, 0.84f, 0.45f, 0.35f);
            glow.raycastTarget = false;
            glow.transform.SetAsFirstSibling();

            _heroArea = ChogUi.Area("Hero", transform, new Vector2(0.33f, 0.22f), new Vector2(0.67f, 0.86f));
            _stage = ChogHeroStage.Create(_heroArea);
            _heroArea.gameObject.SetActive(false);

            // Two traits each side of the hero.
            var places = new[] { new Vector2(-560f, 140f), new Vector2(-560f, -60f), new Vector2(560f, 140f), new Vector2(560f, -60f) };
            var fills = new[] { ChogTheme.Danger, ChogTheme.Cheek, ChogTheme.Action, ChogTheme.Confirm };
            for (var i = 0; i < traits.Length; i++)
            {
                var trait = ChogUi.Pinned("Trait" + i, transform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), places[i], new Vector2(460f, 126f));
                ChogUi.Plate("Plate", trait, Vector2.zero, Vector2.one, fills[i], 16);
                ChogUi.Number("From", trait, new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.94f), 22f, TextAlignmentOptions.Center,
                    new Color(1f, 1f, 1f, 0.9f), false).text = traits[i];
                var words = ChogUi.Title("Words", trait, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.72f), 40f);
                words.fontSizeMin = 18f;
                words.text = (gives[i] ?? "").ToUpperInvariant();
                trait.gameObject.SetActive(false);
                _traits[i] = trait;
            }

            _title = ChogUi.Title("Title", transform, new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.97f), 96f, TextAlignmentOptions.Center,
                ChogTheme.Hex("FFE27A"));
            _title.text = name;
            _title.gameObject.SetActive(false);
            _subtitle = ChogUi.Title("Subtitle", transform, new Vector2(0.1f, 0.09f), new Vector2(0.9f, 0.16f), 48f);
            _subtitle.text = "YOUR GENESIS IS YOUR HERO";
            _subtitle.gameObject.SetActive(false);
            _hint = ChogUi.Body("Hint", transform, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.09f), 26f, TextAlignmentOptions.Center,
                ChogTheme.PurpleSoft);
            _hint.text = "Tap to continue";
            _hint.gameObject.SetActive(false);
        }

        // Runs "step" from 0 to 1 over "seconds" of real time; a skip finishes it at once.
        private IEnumerator Animate(float seconds, System.Action<float> step)
        {
            var started = Time.unscaledTime;
            while (!_skipped && Time.unscaledTime - started < seconds)
            {
                step(Mathf.Clamp01((Time.unscaledTime - started) / seconds));
                yield return null;
            }
            step(1f);
        }

        private IEnumerator Wait(float seconds)
        {
            var started = Time.unscaledTime;
            while (!_skipped && Time.unscaledTime - started < seconds)
                yield return null;
        }

        private static float Ease(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        // Up to 1.08 and back to 1, as a pop.
        private static float Overshoot(float t)
        {
            const float back = 1.7f;
            var u = t - 1f;
            return 1f + (back + 1f) * u * u * u + back * u * u;
        }
    }
}
