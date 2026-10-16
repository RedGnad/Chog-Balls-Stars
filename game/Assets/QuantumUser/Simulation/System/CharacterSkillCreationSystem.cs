using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
  [Preserve]
  public unsafe class CharacterSkillCreationSystem : SystemMainThreadFilter<CharacterSkillCreationSystem.Filter>
  {
    public struct Filter
    {
      public EntityRef Entity;
      public InputContainer* InputContainer;
      public Transform2D* Transform;
      public MovementData* MovementData;
      public CharacterAttacks* CharacterAttacks;
    }

    public override void Update(Frame frame, ref Filter filter)
    {
      var input = filter.InputContainer->Input;
      var entity = filter.Entity;
      var entityPos = filter.Transform->Position;
      var movementData = filter.MovementData;
      var characterAttacks = filter.CharacterAttacks;

      ApplyAttackInput(frame,
        input,
        input.Fire,
        entity,
        entityPos,
        EAttributeType.Energy,
        movementData,
        characterAttacks->BasicSkillData);

      ApplyAttackInput(frame,
        input,
        input.AltFire,
        entity,
        entityPos,
        EAttributeType.Special,
        movementData,
        characterAttacks->SpecialSkillData);
    }

    private void ApplyAttackInput(Frame frame, QuantumDemoInputTopDown input, Button button, EntityRef entity, FPVector2 entityPos, EAttributeType type, MovementData* movementData, AssetRef<SkillData> dataRef)
    {
      bool actionReleased = input.Casts(button);

      if (actionReleased == true && movementData->IsOnAttackLock == false)
      {
        SkillData data = frame.FindAsset<SkillData>(dataRef.Id);

        FP energyAttribute = AttributesHelper.GetCurrentValue(frame, entity, type);
        if (energyAttribute >= data.Cost)
        {
          movementData->DirectionTimer = data.RotationLockDuration;
          frame.Signals.OnCreateSkill(entity, entityPos, data, input.AimDirection);
          movementData->MovementTimer = data.MovementLockDuration;
        }
      }

      if (movementData->IsOnAttackLock == true)
      {
        movementData->DirectionTimer -= frame.DeltaTime;
      }

      if (movementData->IsOnAttackMovementLock == true)
      {
        movementData->MovementTimer -= frame.DeltaTime;
      }
    }
  }
}
