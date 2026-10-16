using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
  [Preserve]
  public unsafe class RotationSystem : SystemMainThreadFilter<RotationSystem.Filter>
  {
    public struct Filter
    {
      public EntityRef Entity;
      public InputContainer* InputContainer;
      public MovementData* MovementData;
      public Transform2D* Transform;
      public CharacterAttacks* CharacterAttacks;
    }

    public override void Update(Frame frame, ref Filter filter)
    {
      if (filter.MovementData->IsOnAttackLock == false)
      {
        FPVector2 moveDirection = filter.InputContainer->Input.MoveDirection.Normalized;
        if (moveDirection != default)
        {
          filter.Transform->Rotation = FPVector2.RadiansSignedSkipNormalize(FPVector2.Up, moveDirection);
        }

        var input = filter.InputContainer->Input;
        if (input.Casts(input.Fire) || input.Casts(input.AltFire))
        {
          SkillData attackData = frame.FindAsset<SkillData>(filter.CharacterAttacks->BasicSkillData.Id);

          if (filter.InputContainer->Input.AimDirection.Magnitude >= FP._0_10)
          {
            filter.MovementData->LastAutoAimDirection = filter.InputContainer->Input.AimDirection.Normalized;
          }
          else
          {
            bool foundTarget = EnemyPositionsHelper.TryGetClosestCharacterDirection(frame, filter.Entity, *filter.Transform, attackData.AutoAimRadius, true, attackData.AutoAimCheckSight, out var direction);
            if (foundTarget == true)
            {
              if (direction == FPVector2.Zero)
              {
                direction = filter.Transform->Up;
              }
              filter.MovementData->LastAutoAimDirection = direction;
            }
          }

          filter.Transform->Rotation = FPVector2.RadiansSignedSkipNormalize(FPVector2.Up, filter.MovementData->LastAutoAimDirection);
        }
      }
    }
  }
}
