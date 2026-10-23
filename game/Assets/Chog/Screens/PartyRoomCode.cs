using Chog.Match;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace Chog.Screens
{
    /// <summary>
    /// The code of a room made with FRIENDS (CREATE A ROOM), at the top left of the preparation screen: a friend types it in
    /// JOIN A ROOM, which also takes them to the room's region (the sample's party screen writes the region into the code).
    /// A tap copies it. Such a room waits for its host's START NOW instead of starting by itself (LobbyCountdown). 23 October
    /// 2026: after CREATE A ROOM the code showed nowhere, and the clock started the match before a friend could join.
    /// </summary>
    public static class PartyRoomCode
    {
        private const string ObjectName = "ChogRoomCode";
        // The codes of the sample's generator (QuantumMenuPartyCodeGenerator): eight of these characters. A quick match's
        // room has a name Photon makes, never one of these.
        private const string Characters = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
        private const int Length = 8;

        public static bool IsPartyCode(string roomName)
        {
            if (roomName == null || roomName.Length != Length)
                return false;
            foreach (var character in roomName)
                if (Characters.IndexOf(character) < 0)
                    return false;
            return true;
        }

        /// <summary>Whether the room this game is in was made with FRIENDS.</summary>
        public static bool InPartyRoom => IsPartyCode(QuantumRunner.Default?.NetworkClient?.CurrentRoom?.Name);

        /// <summary>Shows the code on the preparation screen "screen" once, when the room was made with FRIENDS.</summary>
        public static void Show(Transform screen, string roomName)
        {
            if (screen == null || !IsPartyCode(roomName) || screen.Find(ObjectName) != null)
                return;
            var plate = ChogUi.Plate(ObjectName, screen, new Vector2(0.015f, 0.855f), new Vector2(0.27f, 0.975f), ChogTheme.Night, 14);
            plate.raycastTarget = true;
            var outline = plate.gameObject.AddComponent<Outline>();
            outline.effectColor = ChogTheme.Gold;
            outline.effectDistance = new Vector2(3f, -3f);
            var kicker = ChogUi.Body("Kicker", plate.transform, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.95f), 20f,
                TextAlignmentOptions.Left, ChogTheme.Hex("FFE27A"));
            kicker.text = "ROOM CODE  |  TAP TO COPY";
            var code = ChogUi.Title("Code", plate.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.66f), 48f);
            code.alignment = TextAlignmentOptions.Left;
            code.color = ChogTheme.Cream;
            code.text = roomName;
            var button = plate.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.onClick.AddListener(() => PlayerWallet.CopyLink(roomName, outcome =>
            {
                if (kicker != null)
                    kicker.text = outcome == "copied" ? "COPIED: SEND IT TO A FRIEND" : "ROOM CODE";
            }));
        }
    }
}
