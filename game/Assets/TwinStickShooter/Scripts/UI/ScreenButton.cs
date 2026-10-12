namespace TwinStickShooter
{
  using UnityEngine;
  using UnityEngine.EventSystems;
  using UnityEngine.InputSystem.Layouts;

  public class ScreenButton : ScreenControl, IPointerDownHandler, IPointerUpHandler
  {
    public override void OnPointerUp(PointerEventData eventData)
    {
      if (eventData == null || !Pointer.Release(eventData.pointerId)) return;
      ResetInput();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
      if (eventData == null || !Pointer.TryCapture(eventData.pointerId)) return;
      SendValueToControl(1.0f);
    }

    public override void ResetInput()
    {
      Pointer.Reset();
      SendValueToControl(0.0f);
    }

    public override void OnDrag(PointerEventData eventData)
    {
    }

    [InputControl(layout = "Button")] [SerializeField]
    private string _controlPath;

    protected override string controlPathInternal
    {
      get => _controlPath;
      set => _controlPath = value;
    }
  }
}
