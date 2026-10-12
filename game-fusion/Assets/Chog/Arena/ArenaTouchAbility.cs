using Chog.Controls;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Chog.Arena
{
    public sealed class ArenaTouchAbility : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public ArenaControls Controls;
        private PointerCapture _capture;

        public void OnPointerDown(PointerEventData data)
        {
            if (_capture.TryCapture(data.pointerId) && Controls != null) Controls.SetTouchSpecial(true);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (_capture.Release(data.pointerId) && Controls != null)
            {
                Controls.SetTouchSpecial(false);
                Controls.QueueSpecialRelease();
            }
        }
        private void OnDisable() { _capture.Reset(); Controls?.SetTouchSpecial(false); }
        private void OnApplicationFocus(bool focus) { if (!focus) { _capture.Reset(); Controls?.SetTouchSpecial(false); } }
        private void OnApplicationPause(bool pause) { if (pause) { _capture.Reset(); Controls?.SetTouchSpecial(false); } }
    }
}
