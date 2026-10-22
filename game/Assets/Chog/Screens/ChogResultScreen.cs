using System.Collections;
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

        // A private-page design fixture exercises the reward motion without a wallet, a Photon
        // room, a career write or a claim that its sample numbers are live player data.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PreviewIfAsked()
        {
            var rank = LocalGenesis.TestFlag(Application.absoluteURL, Application.isEditor, "rankpreview");
            if (!rank && !LocalGenesis.TestFlag(Application.absoluteURL, Application.isEditor, "rewardpreview"))
                return;
            new GameObject("LocalRewardDesignPreview").AddComponent<PreviewRunner>().RankPreview = rank;
        }

        private sealed class PreviewRunner : MonoBehaviour
        {
            public bool RankPreview;

            private IEnumerator Start()
            {
                yield return new WaitForSecondsRealtime(3f);
                if (RankPreview)
                {
                    if (!Show(0, 0, ChogHeroModel.Recruit))
                        yield break;
                    var rankResult = GameObject.Find(ObjectName);
                    ChogUi.Body("PreviewMark", rankResult.transform, new Vector2(0.56f, 0.75f), new Vector2(0.95f, 0.80f), 26f,
                        TextAlignmentOptions.Center, ChogTheme.Cream).text = "LOCAL DESIGN PREVIEW, NO RANK WRITE";
                    rankResult.GetComponent<ResultControls>().PreviewRank();
                    yield break;
                }
                var roster = Resources.Load<GenesisRoster>("GenesisRoster");
                var index = roster != null ? roster.IndexOf(1795) : -1;
                if (index < 0)
                    yield break;
                var metadataId = roster.Entries[index].MetadataId;
                if (!Show(0, 0, metadataId, 1795))
                    yield break;
                var result = GameObject.Find(ObjectName);
                ChogUi.Body("PreviewMark", result.transform, new Vector2(0.56f, 0.75f), new Vector2(0.95f, 0.80f), 26f,
                    TextAlignmentOptions.Center, ChogTheme.Cream).text = "LOCAL DESIGN PREVIEW, NO CAREER WRITE";
                result.GetComponent<ResultControls>().Preview();
            }
        }

        /// <summary>Whether the end of a match is on screen.</summary>
        public static bool IsShown => GameObject.Find(ObjectName) != null;

        private static string PendingKey(string address) => "chog.rank.pending." + address.ToLowerInvariant();

        public static string PendingRankGameId(string address) => string.IsNullOrEmpty(address)
            ? "" : PlayerPrefs.GetString(PendingKey(address), "");

        public static void ClearPendingRank(string address, string gameId)
        {
            if (!string.IsNullOrEmpty(gameId) && PendingRankGameId(address) == gameId)
            {
                PlayerPrefs.DeleteKey(PendingKey(address));
                PlayerPrefs.Save();
            }
        }

        private static void RememberPendingRank(string address, string gameId)
        {
            if (string.IsNullOrEmpty(address) || string.IsNullOrEmpty(gameId))
                return;
            PlayerPrefs.SetString(PendingKey(address), gameId);
            PlayerPrefs.Save();
        }

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
            Object.DontDestroyOnLoad(host);
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
                // The basic attack in the words of the hero's class ("LOBS GREEN CANDLES").
                var played = Resources.Load<GenesisRoster>("GenesisRoster");
                var at = played != null && tokenId > 0 ? played.IndexOf(tokenId) : -1;
                var roleId = at >= 0 && played.RoleIds != null && played.Entries[at].Role < played.RoleIds.Length
                    ? played.RoleIds[played.Entries[at].Role] : "";
                var attack = ChogHeroModel.AttackWords.Short(ChogHeroModel.AttackLine(metadataId), roleId);
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
            controls.Won = won;
            controls.GameId = QuantumRunner.Default?.NetworkClient?.CurrentRoom?.Name;
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var resultFrame = game != null && game.Frames != null ? game.Frames.Verified : null;
            controls.RankEligible = playing && !string.IsNullOrEmpty(controls.GameId) &&
                resultFrame != null && MatchModes.IsSiege(resultFrame);
            controls.VictoryTitle = title;
            controls.VictoryGlow = playing && ChogHeroModel.Has(metadataId)
                ? host.transform.Find("Glow")?.GetComponent<Image>() : null;
            // Every player, a Genesis too, sees the player rank card and its bar, where the points of a verified siege
            // fly (22 October 2026, the owner: the Genesis' mastery road here read as a window nobody understood, and
            // hid the rank moment). The Genesis' mastery stays on its hero sheet.
            controls.BuildRecruitRank(card.transform);

            if (playing)
            {
                controls.BuildRank(host.transform);
                // The stake of this match, when this device staked on it (nothing otherwise).
                WagerResultCard.Show(host.transform, localTeam);
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
            public GenesisCareer BeforeCareer;
            public int TokenId;
            public string GameId;
            public int PointsBefore = -1;
            public bool WaitingForRecord;
            public bool Won;
            public bool RankEligible;
            public string RankAddress;
            public TextMeshProUGUI VictoryTitle;
            public Image VictoryGlow;
            private TextMeshProUGUI _rankStatus;
            private TextMeshProUGUI _rankTier;
            private TextMeshProUGUI _rankValue;
            private TextMeshProUGUI _rankNext;
            private RectTransform _rankFill;
            private RectTransform _rankMoment;
            private CanvasGroup _rankGroup;
            private TextMeshProUGUI _rankHeading;
            private TextMeshProUGUI _rankAmount;
            private TextMeshProUGUI _rankDetail;
            private RectTransform _reward;
            private CanvasGroup _rewardGroup;
            private TextMeshProUGUI _rewardTitle;
            private TextMeshProUGUI _rewardAmount;
            private TextMeshProUGUI _rewardDetail;
            private bool _skipReveal;
            private float _shownAt;
            private bool _leaving;
            private Task _disconnectTask;
            private QuantumMenuUIController _menuController;

            public void BuildRecruitRank(Transform card)
            {
                _rankTier = ChogUi.Title("RankTier", card, new Vector2(0.06f, 0.70f), new Vector2(0.94f, 0.90f), 64f);
                _rankTier.color = ChogTheme.Gold;
                _rankValue = ChogUi.Title("RankPoints", card, new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.68f), 45f);
                var track = ChogUi.Plate("RankTrack", card, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.42f), ChogTheme.Night, 14);
                _rankFill = ChogUi.Area("RankFill", track.transform, Vector2.zero, new Vector2(0.001f, 1f));
                var fill = _rankFill.gameObject.AddComponent<Image>();
                fill.sprite = ChogSprites.Rounded(18, 3, ChogTheme.Ink, ChogTheme.Gold);
                fill.type = Image.Type.Sliced;
                fill.raycastTarget = false;
                _rankNext = ChogUi.Body("NextRank", card, new Vector2(0.06f, 0.21f), new Vector2(0.94f, 0.32f),
                    27f, TextAlignmentOptions.Center, ChogTheme.Cream);
                Career = ChogUi.Body("Career", card, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.18f),
                    25f, TextAlignmentOptions.Center, ChogTheme.Cream);
                if (PlayerWallet.SignIn == SignInState.SignedIn && PlayerRanks.Current?.rank != null)
                    ShowRank(PlayerRanks.Current.rank, PlayerRanks.Current.rank.points);
                else
                {
                    _rankTier.text = "PLAYER RANK";
                    _rankValue.text = PlayerWallet.SignIn == SignInState.SignedIn ? "RANK LOADING" : "SIGN IN TO EARN";
                    _rankNext.text = "";
                    Career.text = TokenId > 0 ? "Siege matches earn player points." : "Recruit has no Genesis mastery. Siege can earn player points.";
                }
            }

            private void ShowRank(PlayerRank rank, int displayedPoints)
            {
                if (_rankTier == null || rank == null)
                    return;
                _rankTier.text = rank.tier;
                _rankValue.text = displayedPoints + " PLAYER POINTS";
                _rankNext.text = rank.next > 0 ? "NEXT RANK AT " + rank.next + " POINTS" : "TOP RANK";
                _rankFill.anchorMax = new Vector2(rank.next > 0
                    ? Mathf.Clamp((float)displayedPoints / rank.next, 0.001f, 1f) : 1f, 1f);
                Career.text = rank.matches + " VERIFIED SIEGE MATCHES";
            }

            public void BuildRank(Transform screen)
            {
                var strip = ChogUi.Plate("RankStatus", screen, new Vector2(0.545f, 0.22f), new Vector2(0.97f, 0.258f),
                    ChogTheme.Night, 10);
                _rankStatus = ChogUi.Body("Words", strip.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f),
                    22f, TextAlignmentOptions.Center, ChogTheme.Cream);
                var signedMatch = PlayerWallet.SignIn == SignInState.SignedIn && RankEligible &&
                    !BuildIdentity.PhotonVersion.Contains(BuildIdentity.PreviewSuffix);
                _rankStatus.text = signedMatch ? "RANK CHECKING IN BACKGROUND. PLAY AGAIN" : "PLAYER RANK: CASUAL MATCH";
                if (signedMatch)
                {
                    RankAddress = PlayerWallet.Address;
                    RememberPendingRank(RankAddress, GameId);
                }

                // Over the hero's half, so the rank card and its bar, where the points land, stay in view.
                _rankMoment = ChogUi.Area("RankMoment", screen, new Vector2(0.05f, 0.30f), new Vector2(0.49f, 0.74f));
                var plate = _rankMoment.gameObject.AddComponent<Image>();
                plate.sprite = ChogSprites.Rounded(26, 4, ChogTheme.Ink, ChogTheme.CardRaised);
                plate.type = Image.Type.Sliced;
                plate.raycastTarget = false;
                _rankGroup = _rankMoment.gameObject.AddComponent<CanvasGroup>();
                _rankGroup.blocksRaycasts = false;
                _rankHeading = ChogUi.Title("Heading", _rankMoment, new Vector2(0.05f, 0.77f), new Vector2(0.95f, 0.96f), 38f);
                _rankHeading.color = ChogTheme.PurpleSoft;
                _rankAmount = ChogUi.Title("Amount", _rankMoment, new Vector2(0.04f, 0.27f), new Vector2(0.96f, 0.80f), 130f);
                _rankDetail = ChogUi.Body("Detail", _rankMoment, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.28f),
                    30f, TextAlignmentOptions.Center, ChogTheme.Cream);
                _rankMoment.gameObject.SetActive(false);
                if (signedMatch)
                    StartCoroutine(PollRank());
            }

            private IEnumerator PollRank()
            {
                var failures = 0;
                for (var attempt = 0; attempt < 90 && !_leaving; attempt++)
                {
                    if (PlayerWallet.SignIn != SignInState.SignedIn ||
                        !string.Equals(RankAddress, PlayerWallet.Address, System.StringComparison.OrdinalIgnoreCase))
                        yield break;
                    PlayerRankAnswer answer = null;
                    var returned = false;
                    PlayerRanks.Read(value => { answer = value; returned = true; });
                    var deadline = Time.realtimeSinceStartup + 10f;
                    while (!returned && !_leaving && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (_leaving)
                        yield break;
                    if (PlayerWallet.SignIn != SignInState.SignedIn ||
                        !string.Equals(RankAddress, PlayerWallet.Address, System.StringComparison.OrdinalIgnoreCase))
                        yield break;
                    if (answer == null && ++failures >= 3)
                    {
                        _rankStatus.text = "PLAYER RANK: SERVICE UNAVAILABLE, CHECK LATER";
                        yield break;
                    }
                    if (answer != null && !answer.enabled)
                    {
                        _rankStatus.text = "PLAYER RANK: NOT AVAILABLE YET";
                        ClearPendingRank(RankAddress, GameId);
                        yield break;
                    }
                    var match = answer?.latestMatch;
                    if (match != null && match.gameId == GameId && match.status == "ranked" &&
                        answer.rank?.last != null && answer.rank.last.gameId == GameId)
                    {
                        var gain = answer.rank.last;
                        _rankStatus.text = "PLAYER RANK  " + answer.rank.tier + "  " + answer.rank.points +
                            " PTS  (" + (gain.delta >= 0 ? "+" : "") + gain.delta + ")";
                        if (gain.delta != 0 || gain.tierAfter != gain.tierBefore)
                            yield return RevealRank(gain, answer.rank);
                        else
                            ShowRank(answer.rank, answer.rank.points);
                        if (!_leaving)
                            ClearPendingRank(RankAddress, GameId);
                        yield break;
                    }
                    if (match != null && match.gameId == GameId &&
                        match.status != "verifying" && match.status != "preparing")
                    {
                        _rankStatus.text = answer.rank != null
                            ? "NO RANK POINTS  |  " + answer.rank.tier + " " + answer.rank.points + " PTS"
                            : "NO RANK POINTS FOR THIS MATCH";
                        if (answer.rank != null)
                            ShowRank(answer.rank, answer.rank.points);
                        ClearPendingRank(RankAddress, GameId);
                        yield break;
                    }
                    yield return new WaitForSecondsRealtime(4f);
                }
                if (_rankStatus != null && !_leaving)
                    _rankStatus.text = "PLAYER RANK: STILL VERIFYING, CHECK AGAIN FROM HOME";
            }

            // The flight of the points (22 October 2026, the owner: "we should literally see the points
            // fly into the bar as it fills"): one gold spark a point (grouped past MaxSparks), each
            // leaving the "+N" in its own arc and landing on the rank bar, where the counter ticks up, the
            // bar fills to the new value and pulses, and a note sounds up the scale.
            private const int MaxSparks = 12;
            private const float SparkFlightSeconds = 0.6f;
            private const float SparkGapSeconds = 0.1f;
            // The spark and its glow, in points of the 1920 by 1080 screen: large enough to read on a phone.
            private const float SparkSize = 44f;
            private const float SparkCoreSize = 18f;

            private IEnumerator RevealRank(PlayerRankGain gain, PlayerRank rank)
            {
                while (_reward != null && _reward.gameObject.activeSelf && !_leaving)
                    yield return null;
                if (_leaving)
                    yield break;
                var tierChanged = gain.tierAfter != gain.tierBefore;
                _rankHeading.text = tierChanged ? (gain.delta > 0 ? "RANK UP!" : "RANK CHANGED") : Won ? "VICTORY REWARD" : "PLAYER RANK";
                _rankAmount.text = (gain.delta > 0 ? "+" : "") + gain.delta + " PTS";
                _rankAmount.color = gain.delta >= 0 ? ChogTheme.Gold : ChogTheme.Cream;
                _rankDetail.text = tierChanged
                    ? gain.tierBefore + "  >  " + gain.tierAfter + "   |   " + gain.before + "  >  " + gain.after
                    : gain.before + "  >  " + gain.after + " PLAYER POINTS";
                var reduced = ChogTheme.ReducedMotion;
                _rankMoment.localScale = reduced ? Vector3.one : Vector3.one * 0.74f;
                _rankGroup.alpha = 0f;
                _rankMoment.gameObject.SetActive(true);
                ChogSounds.Reward(tierChanged ? 2 : 0);

                // The window pops in, the rank card still at the points of before.
                var start = Time.unscaledTime;
                const float popSeconds = 0.35f;
                while (Time.unscaledTime - start < popSeconds && !_leaving)
                {
                    var t = Mathf.Clamp01((Time.unscaledTime - start) / popSeconds);
                    _rankGroup.alpha = t;
                    if (!reduced)
                        _rankMoment.localScale = Vector3.one * (t < 0.7f ? Mathf.Lerp(0.74f, 1.06f, t / 0.7f) : Mathf.Lerp(1.06f, 1f, (t - 0.7f) / 0.3f));
                    yield return null;
                }
                _rankGroup.alpha = 1f;
                _rankMoment.localScale = Vector3.one;
                ShowRankPoints(rank, gain.before, tierChanged);

                // The points fly: a gain in sparks, a loss drains the bar without them.
                if (!reduced && gain.delta > 0 && _rankFill != null)
                    yield return FlyPoints(gain, rank, tierChanged);
                else
                {
                    const float countSeconds = 0.6f;
                    start = Time.unscaledTime;
                    while (Time.unscaledTime - start < countSeconds && !_leaving)
                    {
                        var t = Mathf.Clamp01((Time.unscaledTime - start) / countSeconds);
                        ShowRankPoints(rank, Mathf.RoundToInt(Mathf.Lerp(gain.before, gain.after, t * t * (3f - 2f * t))), tierChanged);
                        yield return null;
                    }
                }
                ShowRank(rank, gain.after);
                ClearPendingRank(RankAddress, GameId);
                yield return new WaitForSecondsRealtime(1.1f);
                _rankMoment.gameObject.SetActive(false);
            }

            // The counter and the bar at a number of points between before and after; the tier line is
            // left to the end when the tier changes (the bar then counts toward the new threshold).
            private void ShowRankPoints(PlayerRank rank, int points, bool tierChanged)
            {
                if (tierChanged)
                {
                    if (_rankValue != null)
                        _rankValue.text = points + " PLAYER POINTS";
                    if (_rankFill != null && rank.next > 0)
                        _rankFill.anchorMax = new Vector2(Mathf.Clamp((float)points / rank.next, 0.001f, 1f), 1f);
                }
                else
                    ShowRank(rank, points);
            }

            private IEnumerator FlyPoints(PlayerRankGain gain, PlayerRank rank, bool tierChanged)
            {
                var sparks = Mathf.Min(gain.delta, MaxSparks);
                // The sparks fly on the screen the window sits on (anchored at its centre, the points of
                // the "+N" and of the bar brought into that same space), over everything of it.
                var layer = (RectTransform)_rankMoment.parent;
                var from = (RectTransform)_rankAmount.transform;
                var to = _rankFill.parent as RectTransform;
                var flying = new System.Collections.Generic.List<(RectTransform spark, float born, Vector2 start, Vector2 end, float bend)>();
                var landed = 0;
                var launched = 0;
                var nextLaunch = Time.unscaledTime;
                while ((launched < sparks || flying.Count > 0) && !_leaving)
                {
                    var now = Time.unscaledTime;
                    if (launched < sparks && now >= nextLaunch)
                    {
                        var spark = MakeSpark(layer, launched);
                        var start = WorldCentre(from, layer, new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.2f, 0.2f)));
                        // Lands along the bar where the fill will reach for this point.
                        var share = rank.next > 0 ? Mathf.Clamp01((gain.before + (launched + 1f) * gain.delta / sparks) / rank.next) : 1f;
                        var end = WorldCentre(to, layer, new Vector2(share - 0.5f, 0f));
                        spark.anchoredPosition = start;
                        flying.Add((spark, now, start, end, Random.Range(60f, 140f) * (launched % 2 == 0 ? 1f : -1f)));
                        launched++;
                        nextLaunch = now + SparkGapSeconds;
                    }
                    for (var i = flying.Count - 1; i >= 0; i--)
                    {
                        var (spark, born, start, end, bend) = flying[i];
                        var t = Mathf.Clamp01((now - born) / SparkFlightSeconds);
                        var eased = t * t * (3f - 2f * t);
                        var arc = Mathf.Sin(t * Mathf.PI) * bend;
                        var was = spark.anchoredPosition;
                        spark.anchoredPosition = Vector2.Lerp(start, end, eased) + new Vector2(arc * 0.35f, arc);
                        spark.localScale = Vector3.one * (0.7f + 0.6f * Mathf.Sin(t * Mathf.PI));
                        // The tail points back along the way just flown.
                        var way = spark.anchoredPosition - was;
                        if (way.sqrMagnitude > 0.01f)
                            spark.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(way.y, way.x) * Mathf.Rad2Deg);
                        if (t >= 1f)
                        {
                            Destroy(spark.gameObject);
                            flying.RemoveAt(i);
                            landed++;
                            var points = landed == sparks ? gain.after : gain.before + Mathf.RoundToInt((float)landed * gain.delta / sparks);
                            ShowRankPoints(rank, points, tierChanged);
                            ChogSounds.Reward(landed);
                            StartCoroutine(PulseBar());
                        }
                    }
                    yield return null;
                }
                foreach (var (spark, _, _, _, _) in flying)
                    if (spark != null)
                        Destroy(spark.gameObject);
            }

            private static RectTransform MakeSpark(Transform layer, int index)
            {
                var spark = ChogUi.Pinned("Spark" + index, layer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SparkSize, SparkSize));
                var glow = spark.gameObject.AddComponent<Image>();
                glow.sprite = ChogSprites.Disc(32, 0.55f);
                glow.color = ChogTheme.Gold;
                glow.raycastTarget = false;
                // A short tail behind the spark, the same gold, fading: a comet.
                var tail = ChogUi.Pinned("Tail", spark, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(SparkSize * 1.6f, SparkSize * 0.55f))
                    .gameObject.AddComponent<Image>();
                tail.sprite = ChogSprites.Disc(32, 0.9f);
                tail.color = new Color(ChogTheme.Gold.r, ChogTheme.Gold.g, ChogTheme.Gold.b, 0.55f);
                tail.raycastTarget = false;
                var core = ChogUi.Pinned("Core", spark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SparkCoreSize, SparkCoreSize))
                    .gameObject.AddComponent<Image>();
                core.sprite = ChogSprites.Disc(16, 0.1f);
                core.color = Color.white;
                core.raycastTarget = false;
                spark.SetAsLastSibling();
                return spark;
            }

            // A point of a rect ("offset" in shares of its size from its centre) as the anchored position of a
            // spark pinned at the centre of "layer": the local point of the layer, offset by where its own
            // centre lies (its pivot may not be its middle).
            private static Vector2 WorldCentre(RectTransform rect, RectTransform layer, Vector2 offset)
            {
                var world = rect.TransformPoint(new Vector3(rect.rect.width * offset.x, rect.rect.height * offset.y, 0f) + (Vector3)rect.rect.center);
                var local = (Vector2)layer.InverseTransformPoint(world);
                return local - layer.rect.center;
            }

            private IEnumerator PulseBar()
            {
                var bar = _rankFill != null ? _rankFill.parent as RectTransform : null;
                if (bar == null)
                    yield break;
                var start = Time.unscaledTime;
                const float seconds = 0.18f;
                while (Time.unscaledTime - start < seconds && bar != null)
                {
                    var t = (Time.unscaledTime - start) / seconds;
                    bar.localScale = new Vector3(1f, 1f + 0.35f * Mathf.Sin(t * Mathf.PI), 1f);
                    yield return null;
                }
                if (bar != null)
                    bar.localScale = Vector3.one;
            }

            public void BuildReward(Transform card)
            {
                _reward = ChogUi.Area("MasteryReward", card, new Vector2(0.17f, 0.42f), new Vector2(0.83f, 0.85f));
                var plate = _reward.gameObject.AddComponent<Image>();
                plate.sprite = ChogSprites.Rounded(26, 4, ChogTheme.Ink, ChogTheme.CardRaised);
                plate.type = Image.Type.Sliced;
                plate.pixelsPerUnitMultiplier = 0.8f;
                var skip = _reward.gameObject.AddComponent<Button>();
                skip.transition = Selectable.Transition.None;
                skip.onClick.AddListener(() => _skipReveal = true);
                _rewardGroup = _reward.gameObject.AddComponent<CanvasGroup>();
                _rewardTitle = ChogUi.Title("Heading", _reward, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.94f), 34f);
                _rewardTitle.color = ChogTheme.PurpleSoft;
                _rewardAmount = ChogUi.Title("Amount", _reward, new Vector2(0.03f, 0.28f), new Vector2(0.97f, 0.73f), 76f);
                _rewardDetail = ChogUi.Body("Detail", _reward, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.29f), 24f,
                    TextAlignmentOptions.Center, ChogTheme.Text);
                _reward.gameObject.SetActive(false);
            }

            private void Awake()
            {
                _shownAt = Time.unscaledTime;
                GenesisCareers.Recorded += OnRecorded;
            }

            private void Start()
            {
                if (Won && VictoryTitle != null && !ChogTheme.ReducedMotion)
                    StartCoroutine(VictoryEntrance());
                if (QuantumRunner.Default?.NetworkClient?.CurrentRoom != null)
                    _ = DisconnectForResult();
            }

            private IEnumerator VictoryEntrance()
            {
                var glowAlpha = VictoryGlow != null ? VictoryGlow.color.a : 0f;
                var start = Time.unscaledTime;
                const float duration = 0.65f;
                while (Time.unscaledTime - start < duration)
                {
                    var t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                    VictoryTitle.rectTransform.localScale = Vector3.one * (t < 0.50f
                        ? Mathf.Lerp(1.26f, 1.06f, t * 2f)
                        : Mathf.Lerp(1.06f, 1f, (t - 0.5f) * 2f));
                    if (VictoryGlow != null)
                    {
                        var colour = VictoryGlow.color;
                        colour.a = Mathf.Lerp(0.70f, glowAlpha, t);
                        VictoryGlow.color = colour;
                    }
                    yield return null;
                }
                VictoryTitle.rectTransform.localScale = Vector3.one;
            }

            // The result has already copied the final verified frame. Close the room
            // immediately, keep the screen alive, then await the server's real award.
            private async Task DisconnectForResult()
            {
                var gameplay = Object.FindFirstObjectByType<QuantumMenuUIGameplay>(FindObjectsInactive.Include);
                if (gameplay == null || gameplay.Connection == null || gameplay.Controller == null)
                    return;
                _menuController = gameplay.Controller;
                try
                {
                    _disconnectTask = gameplay.Connection.DisconnectAsync(ConnectFailReason.UserRequest);
                    await _disconnectTask;
                    if (!_leaving && _menuController != null)
                        _menuController.Show<QuantumMenuUIMain>();
                }
                catch (System.Exception error)
                {
                    Debug.LogWarning("CHOG_RESULT could not close the finished session: " + error.Message);
                    _disconnectTask = null;
                }
            }

            private void OnDestroy() => GenesisCareers.Recorded -= OnRecorded;

            public void Preview()
            {
                // The result shows the player rank card now (no mastery road): its preview is the rank moment's.
                if (Road == null)
                {
                    PreviewRank();
                    return;
                }
                BeforeCareer = new GenesisCareer
                {
                    tokenId = TokenId, matches = 1,
                    mastery = new CareerMastery { level = 1, points = 2, next = 3 },
                    badges = new[] { new CareerBadge { id = "first-match", name = "First match" } }
                };
                PointsBefore = 2;
                Road.Show(BeforeCareer, MasteryRoad.HasSecondForm(TokenId));
                Career.text = "LOCAL DESIGN PREVIEW";
                Gained.text = "+2 MASTERY";
                var after = new GenesisCareer
                {
                    tokenId = TokenId, matches = 2, wins = 1,
                    mastery = new CareerMastery { level = 2, points = 4, next = 8 },
                    badges = new[]
                    {
                        new CareerBadge { id = "first-match", name = "First match" },
                        new CareerBadge { id = "first-win", name = "First win" }
                    }
                };
                StartCoroutine(Reveal(after, 2));
            }

            public void PreviewRank()
            {
                if (_rankStatus == null)
                    return;
                _rankStatus.text = "LOCAL DESIGN PREVIEW: SILVER 100 PTS";
                ShowRank(new PlayerRank { tier = "BRONZE", points = 75, next = 100, matches = 0 }, 75);
                StartCoroutine(RevealRank(new PlayerRankGain
                {
                    before = 75, after = 100, delta = 25,
                    tierBefore = "BRONZE", tierAfter = "SILVER"
                }, new PlayerRank { tier = "SILVER", points = 100, next = 250, matches = 1 }));
            }

            private void OnRecorded(GenesisCareer career, bool credited)
            {
                if (Career == null || Road == null || career == null || career.tokenId != TokenId)
                    return;
                WaitingForRecord = false;
                if (!credited)
                {
                    Career.text = "NO NEW CAREER POINTS";
                    Road.Show(career, MasteryRoad.HasSecondForm(TokenId));
                    return;
                }
                Career.text = "Recorded on Monad";
                Tick.enabled = true;
                // The first confirmed match has an unambiguous zero-point baseline even when
                // the career was not loaded before the result screen opened.
                if (BeforeCareer == null && career.matches == 1)
                {
                    BeforeCareer = new GenesisCareer
                    {
                        tokenId = TokenId,
                        mastery = new CareerMastery { level = 1, points = 0, next = MasteryRoad.Steps[0] },
                        badges = new CareerBadge[0]
                    };
                    PointsBefore = 0;
                }
                // What this match added, when the career before it was known.
                var after = career.mastery != null ? career.mastery.points : -1;
                var gained = PointsBefore >= 0 && after > PointsBefore ? after - PointsBefore : 0;
                if (gained > 0)
                    Gained.text = "+" + gained + " MASTERY";
                if (gained > 0 && BeforeCareer != null && _reward != null)
                    StartCoroutine(Reveal(career, gained));
                else
                    Road.Show(career, MasteryRoad.HasSecondForm(TokenId));
            }

            private IEnumerator Reveal(GenesisCareer after, int gained)
            {
                var hasSecondForm = MasteryRoad.HasSecondForm(TokenId);
                var badges = MasteryRoad.NewBadgeNames(BeforeCareer, after);
                var oldLevel = MasteryRoad.LevelForPoints(PointsBefore);
                var newLevel = MasteryRoad.LevelForPoints(after.mastery.points);
                _skipReveal = false;
                Road.ShowAtPoints(BeforeCareer, PointsBefore, hasSecondForm);

                Moment("MATCH REWARDED", "+" + gained, gained == 1 ? "MASTERY POINT" : "MASTERY POINTS", ChogTheme.Hex("FFE27A"));
                ChogSounds.Reward(0);
                yield return FadeMoment(true, 0.42f, true);
                if (!_skipReveal)
                    yield return FadeMoment(false, 0.18f, false);

                var lastPoint = PointsBefore;
                if (!_skipReveal && !ChogTheme.ReducedMotion)
                {
                    var duration = 0.75f;
                    var start = Time.unscaledTime;
                    while (!_skipReveal && Time.unscaledTime - start < duration)
                    {
                        var t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                        var eased = 1f - (1f - t) * (1f - t);
                        var point = Mathf.Min(after.mastery.points, Mathf.FloorToInt(Mathf.Lerp(PointsBefore, after.mastery.points, eased)));
                        Road.ShowAtPoints(BeforeCareer, point, hasSecondForm);
                        if (point > lastPoint)
                        {
                            if (lastPoint > PointsBefore)
                                ChogSounds.Reward(1);
                            lastPoint = point;
                        }
                        yield return null;
                    }
                }
                Road.Show(after, hasSecondForm);

                if (!_skipReveal && newLevel > oldLevel)
                {
                    var detail = newLevel == GenesisForms.UnlockLevel && hasSecondForm
                        ? "SECOND FORM UNLOCKED" : "A NEW MASTERY MILESTONE";
                    Moment("LEVEL UP", "MASTERY " + newLevel, detail, ChogTheme.Hex("FFE27A"));
                    ChogSounds.Reward(2);
                    yield return FadeMoment(true, 0.18f, false);
                    yield return HoldMoment(0.60f);
                    yield return FadeMoment(false, 0.18f, false);
                }

                foreach (var badge in badges)
                {
                    if (_skipReveal)
                        break;
                    Moment("BADGE UNLOCKED", badge, "FOR THIS GENESIS", ChogTheme.Ok);
                    yield return FadeMoment(true, 0.16f, false);
                    yield return HoldMoment(0.42f);
                    yield return FadeMoment(false, 0.16f, false);
                }

                _reward.gameObject.SetActive(false);
                Road.Show(after, hasSecondForm);
            }

            private void Moment(string title, string amount, string detail, Color colour)
            {
                _rewardTitle.text = title;
                _rewardAmount.text = amount;
                _rewardAmount.color = colour;
                _rewardDetail.text = detail;
                _reward.localScale = Vector3.one;
                _rewardGroup.alpha = 0f;
                _reward.gameObject.SetActive(true);
            }

            private IEnumerator FadeMoment(bool fadeIn, float seconds, bool pop)
            {
                var start = Time.unscaledTime;
                do
                {
                    var t = Mathf.Clamp01((Time.unscaledTime - start) / seconds);
                    _rewardGroup.alpha = fadeIn ? t : 1f - t;
                    if (pop && !ChogTheme.ReducedMotion)
                    {
                        var scale = t < 0.70f ? Mathf.Lerp(0.86f, 1.05f, t / 0.70f)
                            : Mathf.Lerp(1.05f, 1f, (t - 0.70f) / 0.30f);
                        _reward.localScale = Vector3.one * scale;
                    }
                    if (_skipReveal)
                        yield break;
                    yield return null;
                } while (Time.unscaledTime - start < seconds);
                _rewardGroup.alpha = fadeIn ? 1f : 0f;
                _reward.localScale = Vector3.one;
            }

            private IEnumerator HoldMoment(float seconds)
            {
                var start = Time.unscaledTime;
                while (!_skipReveal && Time.unscaledTime - start < seconds)
                    yield return null;
            }

            private void Update()
            {
                if (WaitingForRecord && Time.unscaledTime - _shownAt > 20f)
                {
                    Career.text = "STILL VERIFYING. YOU CAN LEAVE";
                    WaitingForRecord = false;
                }
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
                _skipReveal = true;
                // Hidden at once, taken away once the menu is back: this object runs the leaving.
                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);
                if (_disconnectTask != null)
                {
                    try
                    {
                        await _disconnectTask;
                        if (_menuController != null)
                        {
                            _menuController.Show<QuantumMenuUIMain>();
                            if (playAgain)
                                _menuController.Get<QuantumMenuUIMain>()?.SendMessage("OnPlayButtonPressed", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    catch (System.Exception error)
                    {
                        Debug.LogWarning("CHOG_RESULT could not leave the finished session: " + error.Message);
                    }
                }
                else
                {
                    await LeaveMatch(playAgain);
                }
                Destroy(gameObject);
            }
        }



    }
}
