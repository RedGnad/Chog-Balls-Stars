namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;

  public class AttackView : QuantumEntityViewComponent<CustomViewContext>
  {
    [SerializeField] private GameObject _createOnDestroy;
    private ColorSetter[] _colorSetters;
    private EntityRef _ownerEntity;

    public bool IsLocal => _ownerEntity == ViewContext.LocalView.EntityRef;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      _colorSetters = GetComponentsInChildren<ColorSetter>();
      Attack attack = frame.Get<Attack>(EntityRef);
      _ownerEntity = attack.Source;
      SetColors();
    }

    private void SetColors()
    {
      if (IsLocal == true)
      {
        for (int i = 0; i < _colorSetters.Length; i++)
        {
          _colorSetters[i].SetLocal();
        }
      }
      else
      {
        for (int i = 0; i < _colorSetters.Length; i++)
        {
          int ownerTeam = PredictedFrame.Get<TeamInfo>(_ownerEntity).Index;
          int localPlayerTeam = PredictedFrame.Get<TeamInfo>(ViewContext.LocalView.EntityRef).Index;
          _colorSetters[i].SetRemote(ownerTeam == localPlayerTeam);
        }
      }
    }

    public void DestructionVFX()
    {
      if (_createOnDestroy == null)
      {
        return;
      }

      Instantiate(_createOnDestroy, transform.position, Quaternion.identity);
    }
  }
}