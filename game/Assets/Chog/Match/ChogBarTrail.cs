using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The light trail behind a health bar (TwinStickShooter.FilledBarUI): what a blow took stays shown for
    /// HoldSeconds, then drains down to what is left, faster when the gap is large; another blow in the
    /// meantime makes it wait again, and health given back moves it up at once. Plain numbers, tested without
    /// an engine.
    /// </summary>
    public sealed class ChogBarTrail
    {
        public const float HoldSeconds = 0.4f;
        // The drain: at least this share of the bar per second, and this many times the gap left.
        public const float SlowestDrain = 0.3f;
        public const float DrainRate = 5f;

        /// <summary>A pale gold, read on every team colour of the bars.</summary>
        public static readonly Color TrailColour = new Color(1f, 0.92f, 0.6f, 0.95f);

        private float _shown = 1f;
        private float _last = 1f;
        private float _lastDrop = float.NegativeInfinity;

        /// <summary>What the trail shows now, 0 to 1 of the bar.</summary>
        public float Shown => _shown;

        /// <summary>Starts at that value with nothing to drain.</summary>
        public void Reset(float value)
        {
            _shown = Mathf.Clamp01(value);
            _last = _shown;
            _lastDrop = float.NegativeInfinity;
        }

        /// <summary>The bar reads "value" at "now", "deltaTime" after the last step: what the trail shows.</summary>
        public float Step(float value, float now, float deltaTime)
        {
            value = Mathf.Clamp01(value);
            if (value < _last - 1e-4f)
                _lastDrop = now;
            _last = value;

            if (value >= _shown)
            {
                _shown = value;
                return _shown;
            }

            if (now - _lastDrop >= HoldSeconds)
            {
                var speed = Mathf.Max(SlowestDrain, (_shown - value) * DrainRate);
                _shown = Mathf.MoveTowards(_shown, value, speed * deltaTime);
            }
            return _shown;
        }
    }
}
