using System;
using Chog.Match;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chog.Screens
{
    /// <summary>
    /// Watches what the player uses and keeps ControlModeRule's answer for the whole game: the
    /// touch layout (MobileButtonsManager), the input (TopDownInput through it) and the desktop
    /// panel (ChogDesktopControls) read Mode and hear Changed.
    /// </summary>
    public sealed class ControlModeWatcher : MonoBehaviour
    {
        // A mouse that moves less than this many pixels in a frame has not been used.
        private const float MouseMovePixels = 2f;

        private static ControlModeRule s_rule;

        public static event Action Changed;

        public static ControlMode Mode => Rule.Mode;

        private static ControlModeRule Rule =>
            s_rule ??= new ControlModeRule(!Application.isEditor && Application.isMobilePlatform);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("Chog Control Mode");
            DontDestroyOnLoad(host);
            host.AddComponent<ControlModeWatcher>();
        }

        private void Update()
        {
            var now = Time.unscaledTime;
            // The finger first: the mouse events a browser makes up from a tap come in the same
            // frame or after it.
            var screen = Touchscreen.current;
            if (screen != null && screen.press.isPressed)
                Observe(ControlInput.Finger, now);

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame ||
                                  mouse.delta.ReadValue().sqrMagnitude > MouseMovePixels * MouseMovePixels))
                Observe(ControlInput.Mouse, now);

            var keys = Keyboard.current;
            if (keys != null && keys.anyKey.wasPressedThisFrame)
                Observe(ControlInput.Key, now);
        }

        private static void Observe(ControlInput input, float now)
        {
            if (!Rule.Observe(input, now))
                return;
            Debug.Log("CHOG_CONTROLS " + Rule.Mode + " after " + input);
            Changed?.Invoke();
        }
    }
}
