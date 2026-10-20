using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chog.Screens
{
    /// <summary>
    /// A small desktop hover response for the shared chunky buttons. It lifts the same complete button
    /// surface, including its label and pictogram, without adding a decorative line or separate effect.
    /// </summary>
    public sealed class ChogHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float Scale = 1.025f;
        private const float Lift = 3f;
        private const float Response = 18f;

        private Button _button;
        private RectTransform _rect;
        private Shadow _shadow;
        private Vector3 _restScale;
        private Vector3 _restPosition;
        private bool _restKnown;
        private bool _hovering;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _rect = transform as RectTransform;
            var fill = ChogTheme.ImageOf(_button);
            _shadow = fill != null ? fill.GetComponent<Shadow>() : null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Live)
                return;
            RememberRest();
            _hovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = false;
        }

        private bool Live => _button == null || _button.IsInteractable();

        private void RememberRest()
        {
            if (_restKnown || _rect == null)
                return;
            _restScale = _rect.localScale;
            _restPosition = _rect.localPosition;
            _restKnown = true;
        }

        private void Update()
        {
            if (!_restKnown || _rect == null)
                return;

            var active = _hovering && Live;
            var blend = 1f - Mathf.Exp(-Response * Time.unscaledDeltaTime);
            var scale = active ? Scale : 1f;
            var height = active ? Lift : 0f;
            _rect.localScale = Vector3.Lerp(_rect.localScale, _restScale * scale, blend);
            _rect.localPosition = Vector3.Lerp(_rect.localPosition,
                _restPosition + Vector3.up * height, blend);
            if (_shadow != null)
                _shadow.enabled = active;
        }

        private void OnDisable()
        {
            _hovering = false;
            if (_restKnown && _rect != null)
            {
                _rect.localScale = _restScale;
                _rect.localPosition = _restPosition;
            }
            if (_shadow != null)
                _shadow.enabled = false;
        }
    }
}
