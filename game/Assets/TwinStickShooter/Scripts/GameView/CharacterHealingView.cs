namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;
  using System.Collections.Generic;
  using System.Linq;

  public class CharacterHealingView : QuantumCallbacks
  {
    private ParticleSystem _healingVFX;
    private QuantumEntityView _entityView;

    private void Start()
    {
      QuantumEvent.Subscribe<EventCharacterHealed>(this, OnCharacterHealed);
      _entityView = GetComponentInParent<QuantumEntityView>();
      _healingVFX = GetComponentInChildren<ParticleSystem>();
      CharacterInvisibilityController.OnCharacterVisibilityChange += OnCharacterVisibilityChange;
    }

    private void OnCharacterVisibilityChange(bool value, EntityRef target)
    {
      if (target != _entityView.EntityRef || _healingVFX == null)
      {
        return;
      }

      _healingVFX.gameObject.gameObject.SetActive(value);
    }

    private void OnCharacterHealed(EventCharacterHealed e)
    {
      if (e.character != _entityView.EntityRef)
      {
        return;
      }

      _healingVFX.Play();
    }
  }
}