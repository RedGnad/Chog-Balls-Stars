namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine.UI;

  public class SpecialFillBarUI : QuantumEntityViewComponent<CustomViewContext>
  {
    private Image _image;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      if (ViewContext.LocalView != null && EntityView.EntityRef != ViewContext.LocalView.EntityRef)
      {
        gameObject.SetActive(false);
      }

      _image = GetComponent<Image>();
    }

    public override unsafe void OnUpdateView()
    {
      if (ViewContext.LocalView == null || PredictedFrame.Exists(ViewContext.LocalView.EntityRef) == false)
        return;
      float specialAmount = AttributesHelper.GetCurrentValue(PredictedFrame, ViewContext.LocalView.EntityRef, EAttributeType.Special).AsFloat;
      _image.fillAmount = specialAmount;
    }
  }
}