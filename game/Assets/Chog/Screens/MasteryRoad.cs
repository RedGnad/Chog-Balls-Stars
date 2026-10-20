using Chog.Match;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The mastery of a Genesis drawn as a road of six levels (docs/DESIGN-BRIEF.md: one progression
    /// track per hero, shown right after the match and on the home screen): the levels reached in
    /// green, the next one in gold, the second form of the special at level 2, the points still to
    /// earn, the badges of the career and the matches it counts. Everything it shows comes from the
    /// career the services read on Monad (GenesisCareers).
    /// </summary>
    public sealed class MasteryRoad : MonoBehaviour
    {
        /// <summary>The points at which a Genesis reaches levels 2 to 6 (MASTERY_STEPS in server/src/career.js).</summary>
        public static readonly int[] Steps = { 3, 8, 15, 25, 40 };

        /// <summary>The level reached by a confirmed number of mastery points.</summary>
        public static int LevelForPoints(int points)
        {
            var level = 1;
            foreach (var step in Steps)
                if (points >= step)
                    level++;
            return level;
        }

        // The badges a career can earn, as server/src/career.js names them (BADGES).
        private static readonly string[] BadgeIds = { "first-match", "first-win", "three-in-a-row", "veteran" };
        private static readonly string[] BadgeNames = { "FIRST MATCH", "FIRST WIN", "THREE IN A ROW", "VETERAN" };

        /// <summary>Badge labels newly present in a confirmed career, in their road order.</summary>
        public static string[] NewBadgeNames(GenesisCareer before, GenesisCareer after)
        {
            var names = new List<string>();
            for (var i = 0; i < BadgeIds.Length; i++)
                if (!HasBadge(before, BadgeIds[i]) && HasBadge(after, BadgeIds[i]))
                    names.Add(BadgeNames[i]);
            return names.ToArray();
        }

        private static bool HasBadge(GenesisCareer career, string id)
        {
            if (career?.badges == null)
                return false;
            foreach (var badge in career.badges)
                if (badge != null && badge.id == id)
                    return true;
            return false;
        }

        private const int Levels = 6;
        private const float NodeSize = 74f;
        private const float FirstNode = 0.07f;
        private const float LastNode = 0.93f;
        private const float NodeHeight = 0.62f;

        private static readonly Color Locked = ChogTheme.Hex("3B2F72");
        private static readonly Color LockedLabel = ChogTheme.Hex("A79CD6");
        private static readonly Color Gold = ChogTheme.Hex("FFE27A");

        private TextMeshProUGUI _points;
        private RectTransform _track;
        private readonly Image[] _nodes = new Image[Levels];
        private readonly Image[] _glows = new Image[Levels];
        private readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[Levels];
        private readonly Image[] _badges = new Image[BadgeIds.Length];
        private TextMeshProUGUI _matches;

        /// <summary>Whether the special of this Genesis has a second form, which level 2 unlocks.</summary>
        public static bool HasSecondForm(int tokenId)
        {
            var roster = Resources.Load<Quantum.GenesisRoster>("GenesisRoster");
            var index = roster != null ? roster.IndexOf(tokenId) : -1;
            return index >= 0 && roster.HasSecondForm(roster.Entries[index].Shape);
        }

        /// <summary>Builds a road filling "min" to "max" of "parent".</summary>
        public static MasteryRoad Create(Transform parent, Vector2 min, Vector2 max)
        {
            var area = ChogUi.Area("ChogMasteryRoad", parent, min, max);
            var road = area.gameObject.AddComponent<MasteryRoad>();
            road.Build(area);
            return road;
        }

        private void Build(RectTransform area)
        {
            ChogUi.Title("Title", area, new Vector2(0.03f, 0.80f), new Vector2(0.52f, 1f), 44f, TextAlignmentOptions.Left).text = "MASTERY ROAD";
            _points = ChogUi.Body("Points", area, new Vector2(0.50f, 0.89f), new Vector2(0.97f, 1f), 26f, TextAlignmentOptions.Right,
                ChogTheme.PurpleSoft);
            _matches = ChogUi.Body("Matches", area, new Vector2(0.50f, 0.80f), new Vector2(0.97f, 0.89f), 24f, TextAlignmentOptions.Right, Gold);

            var road = ChogUi.Area("Road", area, new Vector2(0f, 0.26f), new Vector2(1f, 0.78f));
            _track = ChogUi.Bar("Track", road, new Vector2(FirstNode, NodeHeight - 0.07f), new Vector2(LastNode, NodeHeight + 0.07f), ChogTheme.Confirm);
            for (var i = 0; i < Levels; i++)
            {
                var x = Mathf.Lerp(FirstNode, LastNode, i / (float)(Levels - 1));
                var glow = ChogUi.Pinned("Glow" + (i + 1), road, new Vector2(x, NodeHeight), new Vector2(0.5f, 0.5f), Vector2.zero,
                    Vector2.one * NodeSize * 1.9f).gameObject.AddComponent<Image>();
                glow.sprite = ChogSprites.Disc(64, 1f);
                glow.color = new Color(1f, 0.84f, 0.35f, 0.55f);
                glow.raycastTarget = false;
                _glows[i] = glow;

                var node = ChogUi.Pinned("Level" + (i + 1), road, new Vector2(x, NodeHeight), new Vector2(0.5f, 0.5f), Vector2.zero,
                    Vector2.one * NodeSize);
                _nodes[i] = ChogUi.Plate("Plate", node, Vector2.zero, Vector2.one, Locked, 14);
                ChogUi.Title("Number", node, new Vector2(0f, 0.08f), Vector2.one, 40f).text = (i + 1).ToString();
                var label = ChogUi.Pinned("Label" + (i + 1), road, new Vector2(x, NodeHeight), new Vector2(0.5f, 1f),
                    new Vector2(0f, -NodeSize * 0.62f), new Vector2(150f, 34f));
                _labels[i] = ChogUi.Number("Text", label, Vector2.zero, Vector2.one, 22f, TextAlignmentOptions.Center, Color.white, false);
            }

            var badges = ChogUi.Area("Badges", area, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.20f));
            var row = badges.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            for (var i = 0; i < BadgeIds.Length; i++)
                _badges[i] = Chip(badges, BadgeNames[i]);

        }

        // A small dark chip with an outline that says whether the badge is earned.
        private static Image Chip(Transform row, string name)
        {
            var chip = new GameObject("Badge", typeof(RectTransform)).AddComponent<Image>();
            chip.transform.SetParent(row, false);
            chip.sprite = ChogSprites.Rounded(14, 3, Color.white, new Color(0.08f, 0.06f, 0.14f, 0.85f));
            chip.type = Image.Type.Sliced;
            chip.pixelsPerUnitMultiplier = 0.9f;
            chip.raycastTarget = false;
            var pad = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            pad.padding = new RectOffset(16, 16, 6, 6);
            pad.childControlWidth = true;
            pad.childControlHeight = true;
            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(chip.transform, false);
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.fontSize = 18f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            ChogTheme.AsNumber(text, false);
            text.text = name;
            return chip;
        }

        /// <summary>Shows a career; null while it is being read.</summary>
        public void Show(GenesisCareer career, bool hasSecondForm)
            => ShowAtPoints(career, career?.mastery?.points ?? 0, hasSecondForm);

        /// <summary>Draws a confirmed career at an intermediate point total during the result reveal.</summary>
        public void ShowAtPoints(GenesisCareer career, int displayedPoints, bool hasSecondForm)
        {
            var points = Mathf.Max(0, displayedPoints);
            var level = LevelForPoints(points);
            var next = level < Levels ? Steps[level - 1] : 0;
            var previous = level >= 2 ? Steps[level - 2] : 0;

            _points.text = career == null ? "reading its career on Monad..."
                : next > 0 ? points + " of " + next + " points to level " + (level + 1)
                : "last level reached";

            var within = next > previous ? Mathf.Clamp01((points - previous) / (float)(next - previous)) : 0f;
            ChogUi.SetBar(_track, (level - 1 + within) / (Levels - 1));

            for (var i = 0; i < Levels; i++)
            {
                var number = i + 1;
                var reached = number <= level;
                var upcoming = number == level + 1;
                _nodes[i].color = reached ? ChogTheme.Confirm : upcoming ? ChogTheme.Action : Locked;
                _glows[i].enabled = upcoming && career != null;
                _labels[i].text = number == level ? "NOW"
                    : number == GenesisForms.UnlockLevel && hasSecondForm ? "2ND FORM"
                    : number >= 2 ? Steps[number - 2] + " PTS" : "";
                _labels[i].color = number == level ? Color.white : upcoming ? Gold : reached ? ChogTheme.Text : LockedLabel;
            }

            for (var i = 0; i < BadgeIds.Length; i++)
            {
                var earned = HasBadge(career, BadgeIds[i]);
                _badges[i].color = earned ? ChogTheme.Confirm : new Color(1f, 1f, 1f, 0.28f);
                _badges[i].GetComponentInChildren<TextMeshProUGUI>().color = earned ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }

            var matches = career?.matches ?? 0;
            _matches.text = career == null ? "" : matches == 1 ? "1 match on Monad" : matches + " matches on Monad";
        }
    }
}
