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
                var roster = Resources.Load<GenesisRoster>("GenesisRoster");
                var index = roster != null ? roster.IndexOf(1795) : -1;
                if (index < 0)
                    yield break;
                var metadataId = roster.Entries[index].MetadataId;
                if (!Show(0, 0, metadataId, 1795))
                    yield break;
                var result = GameObject.Find(ObjectName);
                ChogUi.Body("PreviewMark", result.transform, new Vector2(0.56f, 0.75f), new Vector2(0.95f, 0.80f), 26f,
                    TextAlignmentOptions.Center, ChogTheme.Cream).text = RankPreview
                        ? "LOCAL DESIGN PREVIEW, NO RANK WRITE" : "LOCAL DESIGN PREVIEW, NO CAREER WRITE";
                if (RankPreview)
                    result.GetComponent<ResultControls>().PreviewRank();
                else
                    result.GetComponent<ResultControls>().Preview();
            }
        }

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
            controls.Won = won;
            var game = QuantumRunner.Default != null ? QuantumRunner.Default.Game : null;
            var resultFrame = game != null && game.Frames != null ? game.Frames.Verified : null;
            controls.RankEligible = playing && resultFrame != null && MatchModes.IsSiege(resultFrame);
            controls.VictoryTitle = title;
            controls.VictoryGlow = playing && ChogHeroModel.Has(metadataId)
                ? host.transform.Find("Glow")?.GetComponent<Image>() : null;
            if (tokenId > 0)
            {
                controls.Road = MasteryRoad.Create(card.transform, new Vector2(0.03f, 0.24f), new Vector2(0.97f, 0.97f));
                var chip = ChogUi.Plate("Monad", card.transform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.20f),
                    new Color(0.08f, 0.06f, 0.14f, 0.6f), 12);
                controls.Tick = ChogUi.Icon("Tick", chip.transform, ChogIcons.Icon.Check, new Vector2(0.015f, 0.12f), new Vector2(0.09f, 0.88f));
                controls.Tick.enabled = false;
                controls.Career = ChogUi.Body("Career", chip.transform, new Vector2(0.10f, 0.08f), new Vector2(0.70f, 0.92f), 30f);
                var reporter = GenesisCareers.ReporterFor(tokenId, PlayerWallet.ChosenTokenId,
                    PlayerWallet.SignIn == SignInState.SignedIn, GuestPass.TokenId);
                controls.WaitingForRecord = reporter != GenesisCareers.Reporter.Nobody &&
                    !BuildIdentity.PhotonVersion.Contains(BuildIdentity.PreviewSuffix);
                controls.Career.text = controls.WaitingForRecord ? "VERIFYING MATCH"
                    : BuildIdentity.PhotonVersion.Contains(BuildIdentity.PreviewSuffix) ? "PREVIEW: NO CAREER POINTS"
                    : "CAREER NOT RECORDED";
                controls.Gained = ChogUi.Title("Gained", chip.transform, new Vector2(0.62f, 0.08f), new Vector2(0.97f, 0.92f), 40f,
                    TextAlignmentOptions.Right, ChogTheme.Hex("9BF7A8"));
                controls.BuildReward(card.transform);
                if (GenesisCareers.TryGet(tokenId, out var before))
                {
                    controls.BeforeCareer = before;
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
                controls.Career = text;
                var rank = PlayerRanks.Current?.rank;
                text.text = PlayerWallet.SignIn == SignInState.SignedIn && rank != null
                    ? "PLAYER RANK\n<size=150%><color=#FFE27A>" + rank.tier + " " + rank.points +
                      "</color></size>\n<size=65%>Every verified Siege counts, even as a Recruit.</size>"
                    : "A recruit has no Genesis career.\nSign in to earn player rank points, or play a Genesis for its own career.";
            }

            if (playing)
                controls.BuildRank(host.transform);

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
            public int PointsBefore = -1;
            public bool WaitingForRecord;
            public bool Won;
            public bool RankEligible;
            public TextMeshProUGUI VictoryTitle;
            public Image VictoryGlow;
            private TextMeshProUGUI _rankStatus;
            private Image _rankShade;
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

            public void BuildRank(Transform screen)
            {
                var strip = ChogUi.Plate("RankStatus", screen, new Vector2(0.545f, 0.22f), new Vector2(0.97f, 0.258f),
                    ChogTheme.Night, 10);
                _rankStatus = ChogUi.Body("Words", strip.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f),
                    22f, TextAlignmentOptions.Center, ChogTheme.Cream);
                _rankStatus.text = PlayerWallet.SignIn == SignInState.SignedIn &&
                    RankEligible && !BuildIdentity.PhotonVersion.Contains(BuildIdentity.PreviewSuffix)
                    ? (Won ? "RANK REWARD INCOMING" : "UPDATING PLAYER RANK") : "PLAYER RANK: CASUAL MATCH";

                _rankShade = ChogUi.Area("RankShade", screen, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
                _rankShade.color = new Color(ChogTheme.Ink.r, ChogTheme.Ink.g, ChogTheme.Ink.b, 0.60f);
                _rankShade.raycastTarget = false;
                _rankShade.gameObject.SetActive(false);
                _rankMoment = ChogUi.Area("RankMoment", screen, new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.76f));
                var plate = _rankMoment.gameObject.AddComponent<Image>();
                plate.sprite = ChogSprites.Rounded(26, 4, ChogTheme.Ink, ChogTheme.CardRaised);
                plate.type = Image.Type.Sliced;
                _rankGroup = _rankMoment.gameObject.AddComponent<CanvasGroup>();
                _rankHeading = ChogUi.Title("Heading", _rankMoment, new Vector2(0.05f, 0.77f), new Vector2(0.95f, 0.96f), 38f);
                _rankHeading.color = ChogTheme.PurpleSoft;
                _rankAmount = ChogUi.Title("Amount", _rankMoment, new Vector2(0.04f, 0.27f), new Vector2(0.96f, 0.80f), 130f);
                _rankDetail = ChogUi.Body("Detail", _rankMoment, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.28f),
                    30f, TextAlignmentOptions.Center, ChogTheme.Cream);
                _rankMoment.gameObject.SetActive(false);
                if (PlayerWallet.SignIn == SignInState.SignedIn && RankEligible &&
                    !BuildIdentity.PhotonVersion.Contains(BuildIdentity.PreviewSuffix))
                    StartCoroutine(PollRank());
            }

            private IEnumerator PollRank()
            {
                var failures = 0;
                for (var attempt = 0; attempt < 90 && !_leaving; attempt++)
                {
                    PlayerRankAnswer answer = null;
                    var returned = false;
                    PlayerRanks.Read(value => { answer = value; returned = true; });
                    while (!returned && !_leaving)
                        yield return null;
                    if (_leaving)
                        yield break;
                    if (answer == null && ++failures >= 3)
                    {
                        _rankStatus.text = "PLAYER RANK: SERVICE UNAVAILABLE, CHECK LATER";
                        yield break;
                    }
                    if (answer != null && !answer.enabled)
                    {
                        _rankStatus.text = "PLAYER RANK: NOT AVAILABLE YET";
                        yield break;
                    }
                    var match = answer?.latestMatch;
                    if (match != null && match.status == "ranked" && answer.rank?.last != null &&
                        answer.rank.last.gameId == match.gameId)
                    {
                        var gain = answer.rank.last;
                        _rankStatus.text = "PLAYER RANK  " + answer.rank.tier + "  " + answer.rank.points +
                            " PTS  (" + (gain.delta >= 0 ? "+" : "") + gain.delta + ")";
                        if (TokenId == 0 && Career != null)
                            Career.text = "PLAYER RANK\n<size=150%><color=#FFE27A>" + answer.rank.tier + " " +
                                answer.rank.points + "</color></size>\n<size=65%>" + answer.rank.matches +
                                " VERIFIED SIEGE MATCHES</size>";
                        if (gain.delta != 0 || gain.tierAfter != gain.tierBefore)
                            yield return RevealRank(gain);
                        yield break;
                    }
                    if (match != null && match.status != "verifying" && match.status != "preparing")
                    {
                        _rankStatus.text = "CASUAL MATCH: NO RANK POINTS  |  " + answer.rank.tier + " " + answer.rank.points + " PTS";
                        yield break;
                    }
                    yield return new WaitForSecondsRealtime(4f);
                }
                if (_rankStatus != null && !_leaving)
                    _rankStatus.text = "PLAYER RANK: STILL VERIFYING, CHECK AGAIN FROM HOME";
            }

            private IEnumerator RevealRank(PlayerRankGain gain)
            {
                while (_reward != null && _reward.gameObject.activeSelf && !_leaving)
                    yield return null;
                if (_leaving)
                    yield break;
                _rankHeading.text = gain.tierAfter != gain.tierBefore ? "RANK UP!" : Won ? "VICTORY REWARD" : "PLAYER RANK";
                _rankAmount.text = gain.tierAfter != gain.tierBefore ? gain.tierAfter :
                    (gain.delta > 0 ? "+" : "") + gain.delta + " PTS";
                _rankAmount.color = gain.delta >= 0 ? ChogTheme.Gold : ChogTheme.Cream;
                _rankDetail.text = gain.before + "  >  " + gain.after + " PLAYER POINTS";
                _rankMoment.localScale = ChogTheme.ReducedMotion ? Vector3.one : Vector3.one * 0.74f;
                _rankGroup.alpha = 0f;
                _rankShade.gameObject.SetActive(true);
                _rankMoment.gameObject.SetActive(true);
                ChogSounds.Reward(gain.tierAfter != gain.tierBefore ? 2 : 0);
                var start = Time.unscaledTime;
                const float duration = 0.52f;
                while (Time.unscaledTime - start < duration && !_leaving)
                {
                    var t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                    _rankGroup.alpha = t;
                    if (!ChogTheme.ReducedMotion)
                        _rankMoment.localScale = Vector3.one * (t < 0.75f
                            ? Mathf.Lerp(0.74f, 1.05f, t / 0.75f)
                            : Mathf.Lerp(1.05f, 1f, (t - 0.75f) / 0.25f));
                    yield return null;
                }
                _rankGroup.alpha = 1f;
                _rankMoment.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(2.4f);
                _rankMoment.gameObject.SetActive(false);
                _rankShade.gameObject.SetActive(false);
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
                StartCoroutine(RevealRank(new PlayerRankGain
                {
                    before = 75, after = 100, delta = 25,
                    tierBefore = "BRONZE", tierAfter = "SILVER"
                }));
            }

            private void OnRecorded(GenesisCareer career, bool credited)
            {
                if (Career == null || career == null || career.tokenId != TokenId)
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
