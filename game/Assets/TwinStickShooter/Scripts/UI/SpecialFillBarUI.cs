namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;
  using UnityEngine.UI;

  /// <summary>
  /// How charged the local hero's special is. The moment it is full, the gauge pops (swells to about 1.3 of its size
  /// and settles in 0.3 s) and a chime plays (Chog.Match.ChogSounds.SpecialReady), so
  /// the player knows without looking at it twice.
  /// </summary>
  public class SpecialFillBarUI : QuantumEntityViewComponent<CustomViewContext>
  {
    private const float PopSeconds = 0.3f;
    private const float PopSwell = 0.35f;

    private Image _image;
    private float _lastAmount = -1f;
    private float _popStarted = -1f;
    private Vector3 _restScale = Vector3.one;
    private bool _restKnown;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      if (ViewContext.LocalView != null && EntityView.EntityRef != ViewContext.LocalView.EntityRef)
      {
        gameObject.SetActive(false);
      }

      _image = GetComponent<Image>();
      if (!_restKnown)
      {
        _restScale = transform.localScale;
        _restKnown = true;
      }
      _lastAmount = -1f;
      _popStarted = -1f;
    }

    public override unsafe void OnUpdateView()
    {
      if (ViewContext.LocalView == null || PredictedFrame.Exists(ViewContext.LocalView.EntityRef) == false)
        return;
      float specialAmount = AttributesHelper.GetCurrentValue(PredictedFrame, ViewContext.LocalView.EntityRef, EAttributeType.Special).AsFloat;
      _image.fillAmount = specialAmount;

      // Full now, not full before: the pop and the chime (not when the match starts full).
      if (specialAmount >= 0.999f && _lastAmount >= 0f && _lastAmount < 0.999f)
      {
        _popStarted = Time.time;
        Chog.Match.ChogSounds.SpecialReady();
      }
      _lastAmount = specialAmount;

      if (_popStarted >= 0f)
      {
        var t = (Time.time - _popStarted) / PopSeconds;
        if (t >= 1f)
        {
          _popStarted = -1f;
          transform.localScale = _restScale;
        }
        else
        {
          transform.localScale = _restScale * (1f + PopSwell * Mathf.Sin(Mathf.PI * t) * (1f - 0.5f * t));
        }
      }
    }
  }
}
