namespace TwinStickShooter
{
  using Chog.Match;
  using Quantum;
  using UnityEngine;

  public sealed unsafe class CharacterView : QuantumEntityViewComponent<CustomViewContext>
  {
    public static System.Action<CharacterView> OnLocalPlayerInstantiated;

    public GameObject Model;
    public GameObject ImmunityVFX;

    private CharacterMaterialController[] _materialController;

    // A fall swells the hero for a tenth of a second before it bursts (Chog.Match.ChogHeroPop); a return
    // grows it from nothing with a Back ease. Below 0: not running.
    private float _fallStarted = -1f;
    private float _returnStarted = -1f;
    private bool _down;
    private Vector3 _modelScale = Vector3.one;
    private Transform _heroModel;
    private Vector3 _heroScale = Vector3.one;
    private bool _isLocal;

    // The simulation moves a fallen hero far away at once: for its swell, the model is left where it fell
    // (taken out from under the view), then put back in place, hidden.
    private Transform _modelParent;
    private Vector3 _modelPlace;
    private Quaternion _modelTurn;
    private Vector3 _fellAt;
    private bool _detached;

    // Above the feet, where the pieces of a fall fly from.
    private const float BurstHeight = 1.1f;

    public override void OnInitialize()
    {
      base.OnInitialize();

    }

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      PlayerLink playerLink = frame.Get<PlayerLink>(EntityRef);
      _isLocal = Game.PlayerIsLocal(playerLink.PlayerRef);
      if (_isLocal)
      {
        ViewContext.LocalView = this;
        OnLocalPlayerInstantiated?.Invoke(this);
      }

      _materialController = GetComponentsInChildren<CharacterMaterialController>();
      QuantumEvent.Subscribe<EventCharacterDefeated>(this, OnCharacterDefeated);
      QuantumEvent.Subscribe<EventCharacterRespawned>(this, OnCharacterRespawned);

      CharacterInvisibilityController.OnCharacterVisibilityChange += OnCharacterVisibilityChange;

      if (Model != null)
      {
        _modelScale = Model.transform.localScale;
      }
      _fallStarted = -1f;
      _returnStarted = -1f;
      _down = false;
    }


    public override void OnUpdateView()
    {
      base.OnUpdateView();

      if (PredictedFrame.TryGet<Invisibility>(EntityRef, out var invisibility))
      {
        HandleInvisibility(invisibility);
      }

      if (PredictedFrame.TryGet<Immunity>(EntityRef, out var immunity))
      {
        HandleImmunity(immunity);
      }

      AnimateFallAndReturn();
    }

    private void HandleInvisibility(Invisibility invisibility)
    {
      if (invisibility.IsInvisible)
      {
        foreach (var controller in _materialController)
        {
          controller.SetInvisibleMaterial();
        }
      }
      else
      {
        foreach (var controller in _materialController)
        {
          controller.SetVisible();
        }
      }
    }

    private void HandleImmunity(Immunity immunity)
    {
      if (ImmunityVFX == null) return;

      if (immunity.IsImmune)
      {
        if (ImmunityVFX.activeSelf == false)
        {
          ImmunityVFX.SetActive(true);
        }
      }
      else
      {
        if (ImmunityVFX.activeSelf)
        {
          ImmunityVFX.SetActive(false);
        }
      }
    }

    private void OnCharacterVisibilityChange(bool value, EntityRef target)
    {
      if (target != EntityRef || Model == null)
      {
        return;
      }

      // A hero that fell stays hidden until it comes back.
      Model.SetActive(value && !_down);
    }

    private void OnCharacterDefeated(EventCharacterDefeated e)
    {
      if (e.character != EntityRef || _down)
      {
        return;
      }

      _down = true;
      _returnStarted = -1f;
      _fallStarted = Time.time;
      FindHeroModel();
      // Where it fell: this view has not been moved to where the simulation hid the hero yet.
      _fellAt = transform.position;
      if (Model != null && _heroModel == null)
      {
        Detach();
      }
      ChogSounds.Death(_isLocal);
      if (_isLocal)
      {
        ChogCameraShake.Add(ChogCameraShake.BigBlow);
        ChogHaptics.Pulse(ChogHaptics.FallMilliseconds);
      }
    }

    private void OnCharacterRespawned(EventCharacterRespawned e)
    {
      if (e.character != EntityRef)
      {
        return;
      }

      ComeBack();
    }

    // The hero comes back: shown again, growing from nothing past its size, then settling.
    private void ComeBack()
    {
      _down = false;
      _fallStarted = -1f;
      _returnStarted = Time.time;
      FindHeroModel();
      SetShown(true);
      SetScale(0f);
      ChogSounds.Respawn(_isLocal);
    }

    private void AnimateFallAndReturn()
    {
      if (Model == null)
      {
        return;
      }

      if (_fallStarted >= 0f)
      {
        var seconds = Time.time - _fallStarted;
        if (seconds < ChogHeroPop.SwellSeconds)
        {
          SetScale(ChogHeroPop.Swell(seconds));
          return;
        }

        // The swell is over: it bursts into pieces of its colours and is gone.
        _fallStarted = -1f;
        Burst();
        Attach();
        SetShown(false);
        SetScale(1f);
        return;
      }

      if (_returnStarted >= 0f)
      {
        var seconds = Time.time - _returnStarted;
        SetScale(ChogHeroPop.ComeBack(seconds));
        if (seconds >= ChogHeroPop.ComeBackSeconds)
        {
          _returnStarted = -1f;
        }
        return;
      }

      // A fall that only a predicted frame saw: the hero is alive after all.
      if (_down && PredictedFrame.TryGet<Health>(EntityRef, out var health) && health.IsDead == false)
      {
        ComeBack();
      }
    }

    private void Burst()
    {
      var metadataId = PredictedFrame != null && PredictedFrame.TryGet<GenesisHero>(EntityRef, out var genesis)
        ? genesis.MetadataId
        : ChogHeroModel.Recruit;
      ChogHeroPop.Colours(metadataId, out var skin, out var face);
      ChogImpact.Burst(_fellAt + Vector3.up * BurstHeight, skin, ChogHeroPop.BurstScale, ChogHeroPop.BurstSpeed, face);
    }

    private void Detach()
    {
      var model = Model.transform;
      _modelParent = model.parent;
      _modelPlace = model.localPosition;
      _modelTurn = model.localRotation;
      model.SetParent(null, true);
      _detached = true;
    }

    // The model back under the view, where it was, at its own size.
    private void Attach()
    {
      if (!_detached || Model == null)
      {
        return;
      }

      _detached = false;
      var model = Model.transform;
      if (_modelParent != null)
      {
        model.SetParent(_modelParent, false);
        model.localPosition = _modelPlace;
        model.localRotation = _modelTurn;
      }
      else
      {
        Destroy(Model);
      }
    }

    // The 3D hero, when it was put beside the model instead of under it, is shown, hidden and scaled with it.
    private void FindHeroModel()
    {
      if (_heroModel != null)
      {
        return;
      }

      foreach (var part in GetComponentsInChildren<Transform>(true))
      {
        if (part.name == ChogHeroModel.ModelName && !part.IsChildOf(Model.transform))
        {
          _heroModel = part;
          _heroScale = part.localScale;
          return;
        }
      }
    }

    private void SetShown(bool shown)
    {
      Model.SetActive(shown);
      if (_heroModel != null)
      {
        _heroModel.gameObject.SetActive(shown);
      }
    }

    private void SetScale(float size)
    {
      // Out from under the view, the model's own scale is in the world's axes: the view's scale times its own.
      var parentScale = _detached && _modelParent != null ? _modelParent.lossyScale : Vector3.one;
      Model.transform.localScale = Vector3.Scale(parentScale, _modelScale) * size;
      if (_heroModel != null)
      {
        _heroModel.localScale = _heroScale * size;
      }
    }

    public override void OnDeactivate()
    {
      base.OnDeactivate();

      Attach();
      _fallStarted = -1f;
      _returnStarted = -1f;
      if (Model != null)
      {
        Model.transform.localScale = _modelScale;
      }
      QuantumEvent.UnsubscribeListener(this);
      CharacterInvisibilityController.OnCharacterVisibilityChange -= OnCharacterVisibilityChange;
    }
  }
}
