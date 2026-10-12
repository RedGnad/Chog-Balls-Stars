namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;
  using UnityEngine.UI;

  public class FilledBarUI : QuantumEntityViewComponent<CustomViewContext>
  {
    public EAttributeType AttributeType;
    public Image FilledBar;

    public Color LocalCharacterColor;
    public Color EnemyTeamColor;
    public Color TeammateColor;

    protected float _currentValue;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      HandleUI(frame);
    }

    public void HandleUI(Frame frame)
    {
      if (ViewContext.LocalView == null)
      {
        return;
      }
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

      Attributes attributes = PredictedFrame.Get<Attributes>(EntityRef);
      var dataDictionary = PredictedFrame.ResolveDictionary(attributes.DataDictionary);

      AttributeData attribute = dataDictionary[AttributeType];
      float amount = (attribute.CurrentValue / attribute.MaxValue).AsFloat;
      FilledBar.fillAmount = amount;

      _currentValue = attribute.CurrentValue.AsFloat;
    }
  }
}