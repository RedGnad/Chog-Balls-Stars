namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;

  public class CharacterHeadView : QuantumEntityViewComponent<CustomViewContext>
  {
    public GameObject Head;

    void Start()
    {
      if (Head == null)
      {
        Debug.LogWarning("Head tracking not working - no referenced Head game object");
      }
    }

    public override void OnUpdateView()
    {
      base.OnUpdateView();

      if (Head == null ||  PredictedFrame.Exists(_entityView.EntityRef) == false)
      {
        return;
      }

      MovementData movementData = PredictedFrame.Get<MovementData>(_entityView.EntityRef);
      EnemyPositionsHelper.TryGetClosestEnemyCharacter(PredictedFrame, _entityView.EntityRef, 3, 30, out var target);
      if (PredictedFrame.Exists(target) && movementData.IsOnAttackLock == false)
      {
        Transform2D targetTransform = PredictedFrame.Get<Transform2D>(target);
        var targetRotation =
          Quaternion.LookRotation((targetTransform.Position.ToUnityVector3() + Vector3.up / 2) - transform.position);
        Head.transform.rotation = Quaternion.Slerp(Head.transform.rotation, targetRotation, 5 * Time.deltaTime);
      }
      else
      {
        var targetRotation =
          Quaternion.LookRotation((transform.position + transform.forward + Vector3.up / 2) - Head.transform.position);
        Head.transform.rotation = Quaternion.Slerp(Head.transform.rotation, targetRotation, 5 * Time.deltaTime);
      }
    }
  }
}