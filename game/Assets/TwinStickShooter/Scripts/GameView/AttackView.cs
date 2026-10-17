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

    public override unsafe void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      _colorSetters = GetComponentsInChildren<ColorSetter>();
      Attack attack = frame.Get<Attack>(EntityRef);
      _ownerEntity = attack.Source;
      SetColors();

      // The basic attack of a hero shows what the hat of its Genesis gives it (Chog.Match.ChogAttackLook);
      // a recruit throws a leaf of its grey crown. A lobbed one is drawn on its arc, with the spot it
      // lands on marked in the colour of the thrower's side.
      var data = frame.FindAsset<AttackData>(attack.AttackData.Id);
      var owner = frame.TryGet<GenesisHero>(_ownerEntity, out var genesis) ? genesis.MetadataId
        : frame.Has<PlayerLink>(_ownerEntity) ? Chog.Match.ChogHeroModel.Recruit : -1;
      Vector3? landing = null;
      if (data is BallisticAttackData lob && frame.Unsafe.TryGetPointer<Attack>(EntityRef, out var running))
      {
        landing = lob.GetTargetPosition(frame, running).ToUnityVector3();
      }
      Chog.Match.ChogAttackLook.Dress(transform, data != null ? data.name : "", owner, landing, SideColour(frame));
    }

    // Green for the local player's own attack, blue for its team's, red for the other team's.
    private Color SideColour(Frame frame)
    {
      if (IsLocal)
      {
        return Chog.Match.ChogTeamRing.Self;
      }
      if (ViewContext.LocalView == null || !frame.TryGet<TeamInfo>(_ownerEntity, out var owner)
          || !frame.TryGet<TeamInfo>(ViewContext.LocalView.EntityRef, out var local))
      {
        return Chog.Match.ChogTeamRing.Enemy;
      }
      return owner.Index == local.Index ? Chog.Match.ChogTeamRing.Ally : Chog.Match.ChogTeamRing.Enemy;
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
        // Before this player's hero exists (joining a match under way) an attack reads as the other team's.
        var sameTeam = ViewContext.LocalView != null
                       && PredictedFrame.TryGet<TeamInfo>(_ownerEntity, out var owner)
                       && PredictedFrame.TryGet<TeamInfo>(ViewContext.LocalView.EntityRef, out var local)
                       && owner.Index == local.Index;
        for (int i = 0; i < _colorSetters.Length; i++)
        {
          _colorSetters[i].SetRemote(sameTeam);
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