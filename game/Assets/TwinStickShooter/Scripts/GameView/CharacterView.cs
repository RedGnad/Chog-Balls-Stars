namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;

  public sealed unsafe class CharacterView : QuantumEntityViewComponent<CustomViewContext>
  {
    public static System.Action<CharacterView> OnLocalPlayerInstantiated;

    public GameObject Model;
    public GameObject ImmunityVFX;

    private CharacterMaterialController[] _materialController;

    public override void OnInitialize()
    {
      base.OnInitialize();
      
    }

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      PlayerLink playerLink = frame.Get<PlayerLink>(EntityRef);
      if (Game.PlayerIsLocal(playerLink.PlayerRef))
      {
        ViewContext.LocalView = this;
        OnLocalPlayerInstantiated?.Invoke(this);
      }

      _materialController = GetComponentsInChildren<CharacterMaterialController>();
      QuantumEvent.Subscribe<EventCharacterDefeated>(this, OnCharacterDefeated);
      QuantumEvent.Subscribe<EventCharacterRespawned>(this, OnCharacterRespawned);

      CharacterInvisibilityController.OnCharacterVisibilityChange += OnCharacterVisibilityChange;
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

      Model.SetActive(value);
    }

    private void OnCharacterDefeated(EventCharacterDefeated e)
    {
      if (e.character != EntityRef)
      {
        return;
      }

      Model.SetActive(false);
    }

    private void OnCharacterRespawned(EventCharacterRespawned e)
    {
      if (e.character != EntityRef)
      {
        return;
      }

      Model.SetActive(true);
    }

    public override void OnDeactivate()
    {
      base.OnDeactivate();

      QuantumEvent.UnsubscribeListener(this);
      CharacterInvisibilityController.OnCharacterVisibilityChange -= OnCharacterVisibilityChange;
    }
  }
}