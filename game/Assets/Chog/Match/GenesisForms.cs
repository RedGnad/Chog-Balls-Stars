using System;
using Chog.Diagnostics;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// Which form of its signature each Genesis plays from this device. Every shape has a second
    /// form, another way to throw the same attack. A Genesis may play it once its mastery reached
    /// UnlockLevel, or once it was bought for that Genesis with $CHOG (GenesisShop). Mastery is
    /// earned by playing; both belong to the Genesis, so the second form follows it to its next
    /// holder and to a guest it is lent to. The player picks the form on the menu and the
    /// browser keeps the choice for each Genesis. The match trusts what each game declares, as
    /// it does for the Genesis itself.
    /// </summary>
    public static class GenesisForms
    {
        /// <summary>Mastery level at which the second form of a Genesis can be played.</summary>
        public const int UnlockLevel = 2;

        /// <summary>Test option of the page address, honoured where test entries are: "form=1" plays the second form.</summary>
        public const string QueryParameter = "form";

        private const string KeyPrefix = "chog.form.";

        /// <summary>Raised after the player picked a form.</summary>
        public static event Action Changed;

        /// <summary>Whether a Genesis may play its second form: by its mastery, or because it was bought for it.</summary>
        public static bool IsUnlocked(int masteryLevel, bool bought = false) => bought || masteryLevel >= UnlockLevel;

        /// <summary>
        /// The form a Genesis enters a match with: the second one when its player picked it and
        /// it is unlocked, or when the test option asks for it; else the first one.
        /// </summary>
        public static int Played(int picked, int masteryLevel, bool testOption, bool bought = false)
        {
            return testOption || (picked == 1 && IsUnlocked(masteryLevel, bought)) ? 1 : 0;
        }

        /// <summary>The form the player picked for a Genesis on this device, unlocked or not.</summary>
        public static int Picked(int tokenId)
        {
            return tokenId > 0 && PlayerPrefs.GetInt(KeyPrefix + tokenId, 0) == 1 ? 1 : 0;
        }

        public static void Pick(int tokenId, int form)
        {
            if (tokenId <= 0 || Picked(tokenId) == (form == 1 ? 1 : 0))
                return;

            PlayerPrefs.SetInt(KeyPrefix + tokenId, form == 1 ? 1 : 0);
            // Written at once: a browser page has no reliable moment of quitting to save at.
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Whether this Genesis may play its second form, from what this device knows now.</summary>
        public static bool IsUnlockedFor(int tokenId)
        {
            return IsUnlocked(GenesisCareers.MasteryLevel(tokenId), GenesisShop.Owns(tokenId));
        }

        /// <summary>The form the local player's Genesis enters with, from what this device knows now.</summary>
        public static int PlayedBy(int tokenId)
        {
            return Played(Picked(tokenId), GenesisCareers.MasteryLevel(tokenId), TestOption(Application.absoluteURL, Application.isEditor),
                GenesisShop.Owns(tokenId));
        }

        /// <summary>Whether the page address asks for the second form, where test entries are honoured.</summary>
        public static bool TestOption(string url, bool isEditor)
        {
            return LocalGenesis.TestEntriesAllowed(url, isEditor)
                && PerformanceSampling.TryGetQueryInt(url, QueryParameter, out var form) && form == 1;
        }
    }
}
