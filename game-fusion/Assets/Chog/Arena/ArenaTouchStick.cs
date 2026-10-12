using Chog.Controls;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Chog.Arena
{
    public sealed class ArenaTouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public ArenaControls Controls;
        public bool IsAim;
        public RectTransform Handle;
        private PointerCapture _capture;

        public void OnPointerDown(PointerEventData data)
        {
            if (_capture.TryCapture(data.pointerId)) OnDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (!_capture.Owns(data.pointerId)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,
                data.position, data.pressEventCamera, out var point);
            var value = Vector2.ClampMagnitude(point / 65, 1);
            Handle.anchoredPosition = value * 55;
            if (IsAim) Controls.SetTouchAim(value);
            else Controls.TouchMove = value;
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (!_capture.Release(data.pointerId)) return;
            if (IsAim && Controls != null && Controls.TouchAiming) Controls.QueuePrimaryRelease();
            ClearValue();
        }
        private void OnDisable() { _capture.Reset(); ClearValue(); }
        private void OnApplicationFocus(bool focus) { if (!focus) { _capture.Reset(); ClearValue(); } }
        private void OnApplicationPause(bool pause) { if (pause) { _capture.Reset(); ClearValue(); } }
        private void ClearValue()
        {
            if (Handle != null) Handle.anchoredPosition = Vector2.zero;
            if (Controls == null) return;
            if (IsAim) Controls.SetTouchAim(Vector2.zero);
            else Controls.TouchMove = Vector2.zero;
        }
    }
}
