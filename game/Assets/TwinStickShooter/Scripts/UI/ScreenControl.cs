namespace TwinStickShooter
{
  using UnityEngine.EventSystems;
  using UnityEngine.InputSystem.OnScreen;
  using Chog.Controls;

  public abstract class ScreenControl : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
  {
    protected PointerCapture Pointer;
    public abstract void ResetInput();

    protected override void OnDisable()
    {
      ResetInput();
      base.OnDisable();
    }

    protected virtual void OnApplicationFocus(bool focused)
    {
      if (!focused) ResetInput();
    }

    protected virtual void OnApplicationPause(bool paused)
    {
      if (paused) ResetInput();
    }

    public abstract void OnPointerUp(PointerEventData eventData);

    public abstract void OnPointerDown(PointerEventData eventData);

    public abstract void OnDrag(PointerEventData eventData);
  }
}
