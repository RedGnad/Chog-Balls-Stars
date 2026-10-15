using System.Collections.Generic;
using Chog.Match;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// When an online match ends, sends its result to the game services for the Genesis the
    /// local player played, so the career of that Genesis grows, and shows the career once the
    /// services have recorded it. GenesisCareers decides whether anything is sent: the Genesis
    /// must be the one chosen in the wallet, and the player signed in.
    /// </summary>
    public sealed class CareerReporter : MonoBehaviour
    {
        private const float NoticeSeconds = 9f;
        private static readonly Color NoticeColor = ChogTheme.Card;
        private const string CareerColor = "#FFD27A";

        // Name of each Genesis whose result was sent, for the notice.
        private static readonly Dictionary<int, string> Names = new Dictionary<int, string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Career Reporter");
            DontDestroyOnLoad(host);
            host.AddComponent<CareerReporter>();
        }

        private void Start()
        {
            QuantumEvent.Subscribe<EventGameOver>(this, OnGameOver);
            GenesisCareers.Recorded += ShowRecorded;
        }

        private void OnDestroy() => GenesisCareers.Recorded -= ShowRecorded;

        private static void OnGameOver(EventGameOver e)
        {
            var runner = QuantumRunner.Default;
            var room = runner == null || runner.NetworkClient == null || runner.NetworkClient.CurrentRoom == null
                ? null
                : runner.NetworkClient.CurrentRoom.Name;
            if (string.IsNullOrEmpty(room))
            {
                Debug.Log("CHOG_CAREER not sent: this match is not in an online room");
                return;
            }

            var frame = e.Game.Frames.Verified;
            var matchId = GenesisCareers.MatchId(room, frame.RuntimeConfig.Seed);
            var heroes = frame.Filter<PlayerLink, GenesisHero, TeamInfo>();
            while (heroes.Next(out var entity, out var playerLink, out var hero, out var team))
            {
                if (!e.Game.PlayerIsLocal(playerLink.PlayerRef))
                    continue;

                Names[hero.TokenId] = GenesisHeroCard.CleanName(GenesisCast.DisplayName(frame, entity));
                GenesisCareers.Report(hero.TokenId, matchId, team.Index, e.WinnerTeam);
            }
        }

        // A band across the screen for a few seconds: what the Genesis has done, this match included.
        private static void ShowRecorded(GenesisCareer career, bool credited)
        {
            if (!credited)
                return;

            var host = new GameObject("Chog Career Notice", typeof(Canvas), typeof(CanvasScaler));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var band = (RectTransform)new GameObject("Band", typeof(RectTransform), typeof(Image)).transform;
            band.SetParent(host.transform, false);
            band.anchorMin = new Vector2(0.2f, 0.28f);
            band.anchorMax = new Vector2(0.8f, 0.42f);
            band.offsetMin = Vector2.zero;
            band.offsetMax = Vector2.zero;
            var background = band.GetComponent<Image>();
            background.color = NoticeColor;
            ChogTheme.AsCard(background);
            background.raycastTarget = false;

            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            var area = text.rectTransform;
            area.SetParent(band, false);
            area.anchorMin = new Vector2(0.03f, 0.08f);
            area.anchorMax = new Vector2(0.97f, 0.92f);
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            text.richText = true;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 54f;
            text.color = Color.white;
            Names.TryGetValue(career.tokenId, out var name);
            text.text = "<b>" + (string.IsNullOrEmpty(name) ? "YOUR GENESIS" : name.ToUpperInvariant()) + "</b>" +
                        "  <size=70%>career recorded</size>\n<color=" + CareerColor + ">" + GenesisCareers.DescribeAll(career) + "</color>";

            Destroy(host, NoticeSeconds);
        }
    }
}
