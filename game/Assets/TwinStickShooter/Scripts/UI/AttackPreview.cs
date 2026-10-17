namespace TwinStickShooter
{
  using UnityEngine;
  using Quantum;
  using Photon.Deterministic;

  public enum EPreviewType
  {
    None,
    Linear,
    Ballistic,
    Angle
  }

  public unsafe class AttackPreview : QuantumSceneViewComponent<CustomViewContext>
  {
    public EPreviewType PreviewType;
    public EPreviewType SpecialPreviewType;

    [SerializeField] private FP _maxDistance;

    [SerializeField] private GameObject _previewsContainer;

    [SerializeField] private GameObject _linearPreview;
    [SerializeField] private GameObject _ballisticPreview;
    [SerializeField] private GameObject _anglePreview;

    [SerializeField] private GameObject _ballisticPreviewCircle;

    private MeshRenderer[] _meshRenderer;
    [SerializeField] private Color _standardAimMaterialColor;
    [SerializeField] private Color _blockedMaterialColor;

    private void Awake()
    {
      _meshRenderer = GetComponentsInChildren<MeshRenderer>(true);
    }

    private void TogglePreviews(EPreviewType previewType)
    {
      switch (previewType)
      {
        case EPreviewType.None:
          _linearPreview.SetActive(false);
          _ballisticPreview.SetActive(false);
          _ballisticPreviewCircle.SetActive(false);
          _anglePreview.SetActive(false);
          break;
        case EPreviewType.Linear:
          _ballisticPreview.SetActive(false);
          _ballisticPreviewCircle.SetActive(false);
          _anglePreview.SetActive(false);

          _linearPreview.SetActive(true);
          break;
        case EPreviewType.Ballistic:
          _linearPreview.SetActive(false);
          _anglePreview.SetActive(false);

          _ballisticPreview.SetActive(true);
          _ballisticPreviewCircle.SetActive(true);
          break;
        case EPreviewType.Angle:
          _linearPreview.SetActive(false);
          _ballisticPreviewCircle.SetActive(false);
          _ballisticPreview.SetActive(false);

          _anglePreview.SetActive(true);
          break;
      }
    }

    public void UpdateAttackPreview(FPVector2 aim, bool isSpecial)
    {
      Frame frame = QuantumRunner.Default.Game.Frames.Predicted;

      EPreviewType previewType = isSpecial ? SpecialPreviewType : PreviewType;
      FP maxDistance = _maxDistance;
      if (isSpecial)
      {
        ReadGenesisSignature(frame, ref previewType, ref maxDistance);
      }

      TogglePreviews(previewType);

      var aimNormalized = aim.Normalized;
      transform.position = ViewContext.LocalView.transform.position + Vector3.up * .2f;

      if (previewType != EPreviewType.Angle)
      {
        float previewSize = previewType == EPreviewType.Linear
          ? GetPreviewSizeLinear(frame, aimNormalized, maxDistance)
          : GetPreviewSizeBallistic(aim, maxDistance);
        _previewsContainer.transform.localScale = new Vector3(1, 1, previewSize);
      }
      else
      {
        _previewsContainer.transform.localScale = new Vector3(1, 1, 1);
      }

      _previewsContainer.transform.LookAt(transform.position +
                                          new Vector3(aimNormalized.X.AsFloat, 0, aimNormalized.Y.AsFloat));

      EAttributeType type = isSpecial ? EAttributeType.Special : EAttributeType.Energy;
      float attackLoad = AttributesHelper.GetCurrentValue(frame, ViewContext.LocalView.EntityRef, type).AsFloat;
      bool ready = attackLoad >= GetSkillCost(frame, ViewContext.LocalView.EntityRef, isSpecial);

      bool guided = false;
      if (previewType == EPreviewType.Ballistic)
      {
        // Held from the predicted position, as the aim of a mouse is, so the ring stays under the
        // pointer; the throw leaves from the hero as drawn.
        Transform2D characterTransform = frame.Get<Transform2D>(ViewContext.LocalView.EntityRef);
        var circlePosition = characterTransform.Position +
                             aim.Normalized * Mathf.Clamp(aim.Magnitude.AsFloat, 0, maxDistance.AsFloat).ToFP();
        _ballisticPreviewCircle.transform.position =
          new Vector3(circlePosition.X.AsFloat, .2f, circlePosition.Y.AsFloat);
        // The arc the throw will fly and where it lands (Chog.Match.ChogAimGuide), in place of the
        // sample's flat arrow and circle.
        guided = Chog.Match.ChogAimGuide.ShowLob(ViewContext.LocalView.transform.position,
          new Vector3(circlePosition.X.AsFloat, 0f, circlePosition.Y.AsFloat), maxDistance.AsFloat, ready);
        _ballisticPreview.SetActive(!guided);
        _ballisticPreviewCircle.SetActive(!guided);
      }
      if (!guided)
      {
        Chog.Match.ChogAimGuide.Hide();
      }

      foreach (var mesh in _meshRenderer)
      {
        mesh.material.color = ready ? _standardAimMaterialColor : _blockedMaterialColor;
      }
    }

    public override void OnDisable()
    {
      base.OnDisable();
      Chog.Match.ChogAimGuide.Hide();
    }

    private float GetSkillCost(Frame frame, EntityRef character, bool isSpecial)
    {
      CharacterAttacks attack = frame.Get<CharacterAttacks>(character);
      if (isSpecial)
        return frame.FindAsset<SkillData>(attack.SpecialSkillData.Id).Cost.AsFloat;
      return frame.FindAsset<SkillData>(attack.BasicSkillData.Id).Cost.AsFloat;
    }

    // A Genesis fires the signature of its own traits, whatever body it wears, so the preview
    // of its special is read from the skill instead of the settings of the body's prefab.
    private void ReadGenesisSignature(Frame frame, ref EPreviewType previewType, ref FP maxDistance)
    {
      EntityRef character = ViewContext.LocalView.EntityRef;
      if (frame.Has<GenesisHero>(character) == false)
      {
        return;
      }

      SkillData special = frame.FindAsset<SkillData>(frame.Get<CharacterAttacks>(character).SpecialSkillData.Id);
      if (special == null || special.Reach <= 0)
      {
        return;
      }

      maxDistance = special.Reach;
      if (special is BallisticSkillData)
      {
        previewType = EPreviewType.Ballistic;
      }
      else if (frame.FindAsset<AttackData>(special.AttackDataOverride.Id) is GenesisSweepAttackData sweep)
      {
        // A travelling area is aimed along a line; a standing one in front of the hero covers a
        // cone; one centred on the hero has no direction to show.
        previewType = sweep.Velocity > 0 ? EPreviewType.Linear
          : sweep.ForwardOffset > 0 ? EPreviewType.Angle
          : EPreviewType.None;
      }
    }

    private float GetPreviewSizeLinear(Frame frame, FPVector2 aimDirection, FP maxDistance)
    {
      Transform2D characterTransform = frame.Get<Transform2D>(ViewContext.LocalView.EntityRef);
      var hit = frame.Physics2D.Raycast(characterTransform.Position, aimDirection, maxDistance,
        frame.Layers.GetLayerMask("Static"));
      if (hit.HasValue == false)
      {
        return maxDistance.AsFloat;
      }

      var previewSize =
        Vector2.Distance(characterTransform.Position.ToUnityVector2(), hit.Value.Point.ToUnityVector2());
      return previewSize;
    }

    private float GetPreviewSizeBallistic(FPVector2 aim, FP maxDistance)
    {
      return Mathf.Clamp(aim.Magnitude.AsFloat, 0, maxDistance.AsFloat);
    }
  }
}