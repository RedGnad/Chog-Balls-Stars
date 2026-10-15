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

      // The basic attack of a hero shows what the hat of its Genesis gives it (Chog.Match.ChogAttackLook);
      // a recruit throws a leaf of its grey crown.
      var data = frame.FindAsset<AttackData>(attack.AttackData.Id);
      var owner = frame.TryGet<GenesisHero>(_ownerEntity, out var genesis) ? genesis.MetadataId
        : frame.Has<PlayerLink>(_ownerEntity) ? Chog.Match.ChogHeroModel.Recruit : -1;
      Chog.Match.ChogAttackLook.Dress(transform, data != null ? data.name : "", owner);
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