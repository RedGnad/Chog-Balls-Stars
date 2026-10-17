using Chog.Match;
using Chog.Screens;
using UnityEngine;

namespace TwinStickShooter
{
    /// <summary>
    /// Every press of a chunky button (Chog.Screens.ChogPress.Pressed) clicks (ChogSounds.Click), so no
    /// screen has to ask for its sound.
    /// </summary>
    public static class ChogPressSounds
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Listen()
        {
            ChogPress.Pressed -= ChogSounds.Click;
            ChogPress.Pressed += ChogSounds.Click;
        }
    }
}
