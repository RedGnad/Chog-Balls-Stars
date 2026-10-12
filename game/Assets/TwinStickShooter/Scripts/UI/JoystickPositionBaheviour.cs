namespace TwinStickShooter
{
  using UnityEngine;
  using UnityEngine.UI;
  using UnityEngine.EventSystems;
  using Chog.Controls;

  public class JoystickPositionBaheviour : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
  {
    public ScreenStick ScreenStick;
    public GameObject ControlsParent;

    [SerializeField] private RectTransform _joystick;
    private Vector3 _defaultPos;
    private ScreenControl[] _controls;
    private PointerCapture _pointer;

    void Awake()
    {
      _defaultPos = _joystick.localPosition;
      Image image = GetComponent<Image>();
      image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
      _controls = ControlsParent.GetComponents<ScreenControl>();
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
      if (eventData == null || !_pointer.Owns(eventData.pointerId)) return;
      foreach (var item in _controls)
      {
        item.OnDrag(eventData);
      }
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
      if (eventData == null)
        throw new System.ArgumentNullException(nameof(eventData));
      if (!_pointer.TryCapture(eventData.pointerId)) return;

      RectTransformUtility.ScreenPointToLocalPointInRectangle
      (transform.parent.GetComponentInParent<RectTransform>(), eventData.position, eventData.pressEventCamera,
        out var position);
      _joystick.localPosition = position;

      foreach (var item in _controls)
      {
        item.OnPointerDown(eventData);
      }
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
      if (eventData == null || !_pointer.Release(eventData.pointerId)) return;
      foreach (var item in _controls)
      {
        item.OnPointerUp(eventData);
      }

      _joystick.localPosition = _defaultPos;
    }

    private void OnDisable() => ResetInput();
    private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }

    private void ResetInput()
    {
      _pointer.Reset();
      if (_controls != null)
        foreach (var item in _controls) item.ResetInput();
      if (_joystick != null) _joystick.localPosition = _defaultPos;
    }
  }
}
