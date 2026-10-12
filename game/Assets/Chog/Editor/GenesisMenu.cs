using UnityEditor;
using UnityEngine;

namespace Chog.Editor
{
    // Chooses the Genesis the Editor enters matches with in Play Mode. Builds use "?token=" in the URL.
    public static class GenesisMenu
    {
        // Protocol string: must equal Chog.Match.LocalGenesis.PlayerPrefsKey.
        private const string TokenKey = "chog.token";

        [MenuItem("Chog/Genesis/Play As Token Id From Clipboard")]
        private static void UseClipboard()
        {
            if (!int.TryParse(GUIUtility.systemCopyBuffer.Trim(), out var token) || token <= 0)
            {
                Debug.LogWarning("CHOG: copy a Genesis token id (a positive number) first.");
                return;
            }

            PlayerPrefs.SetInt(TokenKey, token);
            PlayerPrefs.Save();
            Debug.Log("CHOG: Play Mode enters matches as Genesis token " + token + ".");
        }

        [MenuItem("Chog/Genesis/Play Without A Genesis")]
        private static void Clear()
        {
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
            Debug.Log("CHOG: Play Mode enters matches without a Genesis.");
        }
    }
}
