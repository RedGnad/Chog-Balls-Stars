namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;
  using UnityEngine.UI;

  /// <summary>
  /// A bar of a character (its health, its energy). A health bar keeps a light trail behind it: when a
  /// blow takes health, the part lost stays shown for a moment, then drains down to what is left, so the
  /// eye reads how much the blow took (Chog.Match.ChogBarTrail). Health given back fills at once.
  /// </summary>
  public class FilledBarUI : QuantumEntityViewComponent<CustomViewContext>
  {
    public EAttributeType AttributeType;
    public Image FilledBar;

    public Color LocalCharacterColor;
    public Color EnemyTeamColor;
    public Color TeammateColor;

    protected float _currentValue;

    // The trail behind a health bar, made from the bar itself, and what it shows.
    private Image _trail;
    private Chog.Match.ChogBarTrail _trailState;
    // Whether the bar took its colour (or hid itself) from the local hero: a player who joins a match
    // under way meets the other heroes before its own, so the bar tries again until it can.
    private bool _handled;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      HandleUI(frame);
      _trailState = new Chog.Match.ChogBarTrail();
      _trailState.Reset(1f);
    }

    public void HandleUI(Frame frame)
    {
      _handled = false;
      if (ViewContext.LocalView == null || frame.Exists(ViewContext.LocalView.EntityRef) == false || frame.Exists(EntityRef) == false)
      {
        return;
      }
      _handled = true;
      switch (AttributeType)
      {
        case EAttributeType.Health:
          HandleTeamHealthUI(frame);
          break;
        case EAttributeType.Energy:
          HandleTeamEnergyUI();
          break;
      }
    }

    private void HandleTeamHealthUI(Frame frame)
    {
      TeamInfo teamInfo = frame.Get<TeamInfo>(EntityRef);
      TeamInfo localTeamInfo = frame.Get<TeamInfo>(ViewContext.LocalView.EntityRef);

      ColorSetter colorSetter = GetComponent<ColorSetter>();
      if (ViewContext.LocalView.EntityRef == EntityRef)
      {
        colorSetter.SetLocal();
      }
      else
      {
        colorSetter.SetRemote(teamInfo.Index == localTeamInfo.Index);
      }
    }

    private void HandleTeamEnergyUI()
    {
      if (ViewContext.LocalView.EntityRef != EntityRef.None &&
          ViewContext.LocalView.EntityRef != EntityRef)
      {
        gameObject.SetActive(false);
      }
    }

    public override void OnUpdateView()
    {
      if (FilledBar == null || PredictedFrame.Exists(EntityRef) == false)
      {
        return;
      }
      if (_handled == false)
      {
        HandleUI(PredictedFrame);
      }

      Attributes attributes = PredictedFrame.Get<Attributes>(EntityRef);
      var dataDictionary = PredictedFrame.ResolveDictionary(attributes.DataDictionary);

      AttributeData attribute = dataDictionary[AttributeType];
      float amount = (attribute.CurrentValue / attribute.MaxValue).AsFloat;
      FilledBar.fillAmount = amount;

      _currentValue = attribute.CurrentValue.AsFloat;

      if (AttributeType == EAttributeType.Health)
      {
        UpdateTrail(amount);
      }
    }

    private void UpdateTrail(float amount)
    {
      if (_trail == null)
      {
        _trail = MakeTrail();
        if (_trail == null)
        {
          return;
        }
        _trailState.Reset(amount);
      }

      _trail.fillAmount = _trailState.Step(amount, Time.time, Time.deltaTime);
    }

    // A copy of the bar's look, behind it, in a light colour: the part a blow took shows through.
    private Image MakeTrail()
    {
      var bar = FilledBar.rectTransform;
      var holder = new GameObject("HealthTrail", typeof(RectTransform), typeof(Image));
      var rect = (RectTransform)holder.transform;
      rect.SetParent(bar.parent, false);
      rect.SetSiblingIndex(bar.GetSiblingIndex());
      rect.anchorMin = bar.anchorMin;
      rect.anchorMax = bar.anchorMax;
      rect.pivot = bar.pivot;
      rect.anchoredPosition = bar.anchoredPosition;
      rect.sizeDelta = bar.sizeDelta;
      rect.localRotation = bar.localRotation;
      rect.localScale = bar.localScale;

      var trail = holder.GetComponent<Image>();
      trail.sprite = FilledBar.sprite;
      trail.type = Image.Type.Filled;
      trail.fillMethod = FilledBar.fillMethod;
      trail.fillOrigin = FilledBar.fillOrigin;
      trail.fillClockwise = FilledBar.fillClockwise;
      trail.preserveAspect = FilledBar.preserveAspect;
      trail.raycastTarget = false;
      trail.color = Chog.Match.ChogBarTrail.TrailColour;
      return trail;
    }
  }
}
