using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// How a chunky button answers a finger or a click (docs/DESIGN-BRIEF.md): it sinks to 0.93 while held,
    /// overshoots to 1.05 when let go and settles in about 150 ms. Every press is announced on
    /// <see cref="Pressed"/>, which the sounds listen to, so no screen has to ask for a click sound.
    /// </summary>
    public sealed class ChogPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float Held = 0.93f;
        private const float Overshoot = 1.05f;
        private const float SinkSeconds = 0.06f;
        private const float SettleSeconds = 0.15f;

        /// <summary>Raised on every press of a chunky button that can be pressed.</summary>
        public static event Action Pressed;

        private Selectable _selectable;
        private float _from = 1f;
        private float _to = 1f;
        private float _started;
        private float _duration;
        private bool _overshooting;
        private Vector3 _rest = Vector3.one;
        private bool _restKnown;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        private bool Live => _selectable == null || _selectable.IsInteractable();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Live)
                return;
            Go(Held, SinkSeconds, false);
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Go(Overshoot, SettleSeconds * 0.4f, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_to < 1f)
                Go(1f, SettleSeconds, false);
        }

        private void Go(float to, float duration, bool overshoot)
        {
            if (!_restKnown)
            {
                _rest = transform.localScale;
                _restKnown = true;
            }
            _from = Current();
            _to = to;
            _duration = duration;
            _started = Time.unscaledTime;
            _overshooting = overshoot;
            enabled = true;
        }

        private float Current() => _restKnown && _rest.x != 0f ? transform.localScale.x / _rest.x : 1f;

        private void Update()
        {
            if (!_restKnown)
            {
                enabled = false;
                return;
            }
            var t = _duration <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - _started) / _duration);
            // Ease out: fast at first, gentle at the end.
            var eased = 1f - (1f - t) * (1f - t);
            transform.localScale = _rest * Mathf.LerpUnclamped(_from, _to, eased);
            if (t < 1f)
                return;
            if (_overshooting)
            {
                // From the overshoot back to rest.
                Go(1f, SettleSeconds * 0.6f, false);
                return;
            }
            enabled = _to != 1f;
        }

        private void OnDisable()
        {
            if (_restKnown)
                transform.localScale = _rest;
        }
    }
}
