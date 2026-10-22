using System;
using System.Collections.Generic;
using Chog.Match;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The player's profile, opened by the chip at the top left of the menu (22 October 2026, the owner: "a player will want to see
    /// their information"; the chip only opened the sample's window that renames). As the profiles of Clash Royale and Brawl Stars
    /// (Clash Royale Wiki "Player Profile"; trackers such as RoyaleAPI and Brawlify): the name, the rank with its bar, the verified
    /// matches and wins, then the Genesis of the wallet with their mastery, and the badges they earned. Every number comes from the
    /// services (PlayerRanks for the player, GenesisCareers for each Genesis); nothing is shown that they did not give.
    /// </summary>
    public sealed class ChogProfile : MonoBehaviour
    {
        private const string ObjectName = "ChogProfile";
        private const string RosterResource = "GenesisRoster";
        private const int GenesisShown = 8;
        private const float Margin = 26f;

        private Action _rename;
        private Func<string> _name;
        private GenesisRoster _roster;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _tier;
        private TextMeshProUGUI _points;
        private RectTransform _fill;
        private TextMeshProUGUI _next;
        private TextMeshProUGUI _stats;
        private TextMeshProUGUI _genesisTitle;
        private readonly List<(RectTransform cell, TextMeshProUGUI words)> _cells = new List<(RectTransform, TextMeshProUGUI)>();
        private TextMeshProUGUI _badges;
        private int[] _askedFor = new int[0];

        /// <summary>Opens the profile over the main screen of the menu. "name" reads the player's name; "rename" opens the window that changes it.</summary>
        public static void Open(Transform main, Func<string> name, Action rename)
        {
            var existing = main.Find(ObjectName);
            var profile = existing != null ? existing.GetComponent<ChogProfile>() : null;
            if (profile == null)
            {
                var host = ChogUi.Area(ObjectName, main, Vector2.zero, Vector2.one);
                profile = host.gameObject.AddComponent<ChogProfile>();
                profile.Build();
            }
            profile._name = name;
            profile._rename = rename;
            profile.gameObject.SetActive(true);
            profile.transform.SetAsLastSibling();
            PlayerRanks.Read();
            profile.Refresh();
        }

        private void OnEnable()
        {
            PlayerWallet.Changed += Refresh;
            PlayerRanks.Changed += Refresh;
            GenesisCareers.Changed += Refresh;
        }

        private void OnDisable()
        {
            PlayerWallet.Changed -= Refresh;
            PlayerRanks.Changed -= Refresh;
            GenesisCareers.Changed -= Refresh;
        }

        private void Close() => gameObject.SetActive(false);

        private void Build()
        {
            _roster = Resources.Load<GenesisRoster>(RosterResource);
            var night = gameObject.AddComponent<Image>();
            night.sprite = ChogSprites.Gradient(ChogTheme.Night, ChogTheme.NightHigh);

            var back = ChogUi.PinnedButton("Back", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Margin, -26f),
                new Vector2(84f, 84f), ChogTheme.ButtonKind.Secondary, "", 30f, ChogIcons.Icon.Back);
            back.onClick.AddListener(Close);
            _title = ChogUi.Title("Name", ChogUi.Pinned("NameArea", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(134f, -22f),
                new Vector2(900f, 62f)), Vector2.zero, Vector2.one, 56f, TextAlignmentOptions.Left);
            var rename = ChogUi.Button("Rename", transform, new Vector2(0.70f, 0.875f), new Vector2(0.97f, 0.965f),
                ChogTheme.ButtonKind.Quiet, "CHANGE NAME", 30f);
            rename.onClick.AddListener(() =>
            {
                Close();
                _rename?.Invoke();
            });

            // The rank: its tier, its points and the bar to the next tier, then what the matches came to.
            var rank = ChogUi.Card("Rank", transform, new Vector2(0.03f, 0.50f), new Vector2(0.49f, 0.85f), raised: true);
            ChogUi.Body("Heading", rank.transform, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.96f), 26f, TextAlignmentOptions.Left,
                ChogTheme.PurpleSoft).text = "PLAYER RANK";
            _tier = ChogUi.Title("Tier", rank.transform, new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.82f), 60f, TextAlignmentOptions.Left,
                ChogTheme.Gold);
            _points = ChogUi.Title("Points", rank.transform, new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.58f), 36f, TextAlignmentOptions.Left);
            var track = ChogUi.Plate("Track", rank.transform, new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.36f), ChogTheme.Night, 14);
            _fill = ChogUi.Area("Fill", track.transform, Vector2.zero, new Vector2(0.001f, 1f));
            var fill = _fill.gameObject.AddComponent<Image>();
            fill.sprite = ChogSprites.Rounded(18, 3, ChogTheme.Ink, ChogTheme.Gold);
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            _next = ChogUi.Body("Next", rank.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.24f), 26f, TextAlignmentOptions.Left,
                ChogTheme.Cream);

            var stats = ChogUi.Card("Stats", transform, new Vector2(0.03f, 0.06f), new Vector2(0.49f, 0.46f), raised: true);
            ChogUi.Body("Heading", stats.transform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f), 26f, TextAlignmentOptions.Left,
                ChogTheme.PurpleSoft).text = "VERIFIED SIEGES";
            _stats = ChogUi.Body("Numbers", stats.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.80f), 34f, TextAlignmentOptions.TopLeft,
                Color.white);
            _stats.richText = true;

            // The Genesis of the wallet, with their mastery, then the badges they earned.
            var genesis = ChogUi.Card("Genesis", transform, new Vector2(0.52f, 0.30f), new Vector2(0.97f, 0.85f), raised: true);
            _genesisTitle = ChogUi.Body("Heading", genesis.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), 26f,
                TextAlignmentOptions.Left, ChogTheme.PurpleSoft);
            for (var i = 0; i < GenesisShown; i++)
            {
                var column = i % 2;
                var row = i / 2;
                var top = 0.86f - row * 0.21f;
                var cell = ChogUi.Area("Cell" + i, genesis.transform, new Vector2(0.04f + column * 0.48f, top - 0.19f),
                    new Vector2(0.48f + column * 0.48f, top));
                var portrait = ChogUi.Area("Portrait", cell, new Vector2(0f, 0f), new Vector2(0.24f, 1f));
                var words = ChogUi.Body("Words", cell, new Vector2(0.26f, 0f), new Vector2(1f, 1f), 24f, TextAlignmentOptions.Left, Color.white);
                words.richText = true;
                _cells.Add((portrait, words));
            }

            var badges = ChogUi.Card("Badges", transform, new Vector2(0.52f, 0.06f), new Vector2(0.97f, 0.27f), raised: true);
            ChogUi.Body("Heading", badges.transform, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.95f), 26f, TextAlignmentOptions.Left,
                ChogTheme.PurpleSoft).text = "BADGES";
            _badges = ChogUi.Body("Names", badges.transform, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.72f), 26f, TextAlignmentOptions.TopLeft,
                ChogTheme.Gold);
        }

        private void Refresh()
        {
            if (_title == null)
                return;
            _title.text = (_name?.Invoke() ?? "").ToUpperInvariant();

            var signedIn = PlayerWallet.SignIn == SignInState.SignedIn;
            var rank = signedIn ? PlayerRanks.Current?.rank : null;
            if (rank == null)
            {
                _tier.text = signedIn ? "RANK LOADING" : "NO RANK YET";
                _points.text = "";
                _fill.anchorMax = new Vector2(0.001f, 1f);
                _next.text = signedIn ? "" : "Connect and sign once: verified sieges earn player points.";
                _stats.text = signedIn ? "" : "Sieges are verified by replaying them on the game server.";
            }
            else
            {
                _tier.text = rank.tier;
                _points.text = rank.points + " PLAYER POINTS";
                _fill.anchorMax = new Vector2(rank.next > 0 ? Mathf.Clamp((float)rank.points / rank.next, 0.001f, 1f) : 1f, 1f);
                _next.text = rank.next > 0 ? "Next rank at " + rank.next + " points" : "Top rank";
                var rate = rank.matches > 0 ? Mathf.RoundToInt(100f * rank.wins / rank.matches) + "%" : "-";
                _stats.text = "<b>" + rank.matches + "</b> played\n<b>" + rank.wins + "</b> won\n<b>" + rate + "</b> win rate"
                    + (rank.lastReset != null && !string.IsNullOrEmpty(rank.lastReset.tierBefore)
                        ? "\n<size=70%>Last season ended at " + rank.lastReset.tierBefore + "</size>" : "");
            }

            var tokens = PlayerWallet.State == WalletState.Ready ? PlayerWallet.Tokens : new int[0];
            _genesisTitle.text = tokens.Length == 0 ? "NO GENESIS IN THIS WALLET" : "YOUR GENESIS (" + tokens.Length + ")";
            var shown = Math.Min(tokens.Length, GenesisShown);
            var asked = new int[shown];
            Array.Copy(tokens, asked, shown);
            if (!SameTokens(asked, _askedFor) && shown > 0)
            {
                _askedFor = asked;
                GenesisCareers.Read(asked);
            }

            var badgeNames = new List<string>();
            for (var i = 0; i < _cells.Count; i++)
            {
                var (portrait, words) = _cells[i];
                var on = i < shown;
                portrait.parent.gameObject.SetActive(on);
                if (!on)
                    continue;
                var tokenId = tokens[i];
                var index = _roster != null ? _roster.IndexOf(tokenId) : -1;
                var metadataId = index >= 0 ? _roster.Entries[index].MetadataId : 0;
                GenesisPortrait.Show(portrait, metadataId, 84f, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                var name = index >= 0 ? GenesisHeroCard.DisplayName(_roster.Entries[index]).ToUpperInvariant() : "GENESIS " + tokenId;
                if (GenesisCareers.TryGet(tokenId, out var career) && career != null)
                {
                    var level = career.mastery != null ? career.mastery.level : 1;
                    words.text = "<b>" + name + "</b>\n<size=80%>Mastery " + level + ", " + career.matches + " played, " + career.wins + " won</size>";
                    if (career.badges != null)
                        foreach (var badge in career.badges)
                            if (badge != null && !string.IsNullOrEmpty(badge.name) && !badgeNames.Contains(badge.name))
                                badgeNames.Add(badge.name);
                }
                else
                {
                    words.text = "<b>" + name + "</b>\n<size=85%>No match yet</size>";
                }
            }

            _badges.text = badgeNames.Count > 0 ? string.Join("   ", badgeNames).ToUpperInvariant()
                : tokens.Length > 0 ? "None yet: play your Genesis to earn them." : "Badges are earned by Genesis in verified matches.";
        }

        private static bool SameTokens(int[] a, int[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
    }
}
