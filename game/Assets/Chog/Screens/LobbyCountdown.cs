using System;
using Chog.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// The clock of the team preparation (mockup "Lobby": STARTS BY ITSELF): the player who hosts the
    /// room sees a gold disc count down from 30 seconds, started again whenever a player joins or
    /// leaves, and the match starts by itself at zero, as START does; START still starts it at once.
    /// The count waits while this player's wallet is busy with the stakes. Every player sees the
    /// count; the host's game starts the match at zero. When the host's game does not (a host who
    /// left the tab in the background: a browser stops drawing it, so its clock stops, and a public
    /// room waited 22 minutes in preparation on 17 October), the game of any other player starts it
    /// a few seconds later. The simulation takes the start from any player and only once (the
    /// selection system turns itself off), so two games sending it at once start one match.
    /// </summary>
    public sealed class LobbyCountdown : MonoBehaviour
    {
        private const string ObjectName = "ChogLobbyCountdown";
        private const float Seconds = 30f;
        // How long after zero the other players wait for the host before starting the match themselves.
        public const float HostGraceSeconds = 8f;
        // Just above START NOW (its top 840 from the top of a 1080 screen), its line of players above it, so the
        // clock and the button read as one (23 October 2026, the owner: the preparation screen was cluttered; CHANGE
        // TEAM, which sat under the clock, went to the corner of CANCEL).
        private static readonly Vector2 Place = new Vector2(0.5f, 0.319f);
        private const float DiscSize = 100f;

        private TextMeshProUGUI _number;
        private TextMeshProUGUI _caption;
        private GameObject _disc;
        private float _startedAt;
        private float _waited;
        private int _players = -1;
        private bool _fired;
        private Action _start;

        /// <summary>
        /// Called each frame of the preparation screen "screen": whether this player hosts the room, how
        /// many players (not bots) are in it, and what starting the match does.
        /// </summary>
        public static void Show(Transform screen, bool hosts, int players, Action start)
        {
            if (screen == null)
                return;
            var found = screen.Find(ObjectName);
            var clock = found != null ? found.GetComponent<LobbyCountdown>() : null;
            if (clock == null)
            {
                var area = ChogUi.Area(ObjectName, screen, Vector2.zero, Vector2.one);
                clock = area.gameObject.AddComponent<LobbyCountdown>();
                clock.Build();
            }
            clock._start = start;
            clock.Tick(hosts, players);
        }

        private void Build()
        {
            _disc = ChogUi.Pinned("Disc", transform, Place, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(DiscSize, DiscSize)).gameObject;
            var plate = ChogUi.Plate("Plate", _disc.transform, Vector2.zero, Vector2.one, ChogTheme.Action, 50);
            plate.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -6f);
            _number = ChogUi.Title("Number", _disc.transform, new Vector2(0f, 0.08f), Vector2.one, 60f);
            _caption = ChogUi.Number("Caption", transform, Vector2.zero, Vector2.zero, 24f, TextAlignmentOptions.Center, ChogTheme.Hex("FFE27A"));
            ChogUi.Pin(_caption.rectTransform, Place, new Vector2(0.5f, 0f), new Vector2(0f, DiscSize * 0.5f + 8f), new Vector2(650f, 34f));
            _startedAt = Time.unscaledTime;
        }

        private void Tick(bool hosts, int players)
        {
            // A player who joins or leaves gives everyone the whole time again.
            if (players != _players)
            {
                _players = players;
                _startedAt = Time.unscaledTime;
                _waited = 0f;
            }
            // While this player answers the wallet about the stakes, the clock waits.
            if (GenesisWager.IsBusy)
                _waited += Time.unscaledDeltaTime;

            var left = Seconds - (Time.unscaledTime - _startedAt - _waited);
            _number.text = Mathf.Max(0, Mathf.CeilToInt(left)).ToString();
            _caption.text = players >= 6
                ? "6/6 PLAYERS  |  STARTS BY ITSELF"
                : Mathf.Max(0, players) + "/6 PLAYERS  |  BOTS FILL THE REST";
            if (!StartsNow(hosts, left) || _fired)
                return;
            _fired = true;
            Debug.Log(hosts ? "CHOG_LOBBY the clock started the match" : "CHOG_LOBBY the host did not start the match, this player's game did");
            _start?.Invoke();
        }

        /// <summary>
        /// Whether this player's game starts the match with "left" seconds on the clock (below 0 once
        /// it passed zero): the host's at zero, any other's when the host let the grace pass.
        /// </summary>
        public static bool StartsNow(bool hosts, float left) => hosts ? left <= 0f : left <= -HostGraceSeconds;
    }
}
