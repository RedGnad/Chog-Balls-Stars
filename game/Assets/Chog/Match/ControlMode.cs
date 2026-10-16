namespace Chog.Match
{
    /// <summary>What a match shows and listens to: the touch sticks, or the mouse and the keyboard.</summary>
    public enum ControlMode
    {
        Touch,
        Desktop,
    }

    /// <summary>What the player has just used.</summary>
    public enum ControlInput
    {
        Finger,
        Mouse,
        Key,
    }

    /// <summary>
    /// Picks the controls from what the player last used, not from whether the browser reports a
    /// touch screen: Unity's web player reports one on a Mac too, and a laptop may have both. A
    /// phone starts with the sticks, anything else with the mouse. A browser follows a tap with
    /// mouse events it makes up from it, so mouse and keys used less than EchoSeconds after a
    /// finger do not switch back.
    /// </summary>
    public sealed class ControlModeRule
    {
        public const float EchoSeconds = 1f;

        private float _lastFinger = float.NegativeInfinity;

        public ControlModeRule(bool phone)
        {
            Mode = phone ? ControlMode.Touch : ControlMode.Desktop;
        }

        public ControlMode Mode { get; private set; }

        /// <summary>What was used, at that time in seconds; true when the mode changed.</summary>
        public bool Observe(ControlInput input, float time)
        {
            var next = Mode;
            if (input == ControlInput.Finger)
            {
                _lastFinger = time;
                next = ControlMode.Touch;
            }
            else if (time - _lastFinger >= EchoSeconds)
            {
                next = ControlMode.Desktop;
            }

            if (next == Mode)
                return false;
            Mode = next;
            return true;
        }
    }

    /// <summary>
    /// The simulation attacks when its button is let go: a finger aims first, then lifts. A mouse
    /// already aims when it is pressed, so a held mouse button is sent as a press then a release,
    /// one input tick each: the attack leaves on the press, and again for as long as it is held.
    /// </summary>
    public struct PressPulse
    {
        private bool _down;

        /// <summary>The state of the button to send for this input tick.</summary>
        public bool Next(bool held)
        {
            _down = held && !_down;
            return _down;
        }
    }
}
