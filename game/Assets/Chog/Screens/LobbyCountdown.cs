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
    /// The count waits while this player's wallet is busy with the stakes. The other players read
    /// that the host starts the match: only the host's game sends the start, as only the host has
    /// START.
    /// </summary>
    public sealed class LobbyCountdown : MonoBehaviour
    {
        private const string ObjectName = "ChogLobbyCountdown";
        private const float Seconds = 30f;
        private static readonly Vector2 Place = new Vector2(0.5f, 0.535f);

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
            _disc = ChogUi.Pinned("Disc", transform, Place, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f)).gameObject;
            var plate = ChogUi.Plate("Plate", _disc.transform, Vector2.zero, Vector2.one, ChogTheme.Action, 56);
            plate.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -6f);
            _number = ChogUi.Title("Number", _disc.transform, new Vector2(0f, 0.08f), Vector2.one, 66f);
            _caption = ChogUi.Number("Caption", transform, Vector2.zero, Vector2.zero, 24f, TextAlignmentOptions.Center, ChogTheme.Hex("FFE27A"));
            ChogUi.Pin(_caption.rectTransform, Place, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(420f, 34f));
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

            _disc.SetActive(hosts);
            if (!hosts)
            {
                _caption.text = "THE HOST STARTS THE MATCH";
                return;
            }

            var left = Seconds - (Time.unscaledTime - _startedAt - _waited);
            _number.text = Mathf.Max(0, Mathf.CeilToInt(left)).ToString();
            _caption.text = "STARTS BY ITSELF";
            if (left > 0f || _fired)
                return;
            _fired = true;
            Debug.Log("CHOG_LOBBY the clock started the match");
            _start?.Invoke();
        }
    }
}
