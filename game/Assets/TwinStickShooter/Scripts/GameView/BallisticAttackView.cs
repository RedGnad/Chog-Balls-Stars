namespace TwinStickShooter
{
  using Photon.Deterministic;
  using Quantum;
  using UnityEngine;

  public unsafe class BallisticAttackView : QuantumEntityViewComponent<CustomViewContext>
  {
    [SerializeField] private float _currentSpeed;
    [SerializeField] private float _currentGravity;
    private float _distanceToTarget;

    private void Start()
    {
      Attack* attackComponent = PredictedFrame.Unsafe.GetPointer<Attack>(EntityRef);
      Transform2D myTransform = PredictedFrame.Get<Transform2D>(EntityRef);

      FPVector2 myPosition = myTransform.Position;
      FPVector2 finalPosition = PredictedFrame.FindAsset<BallisticAttackData>(attackComponent->AttackData.Id)
        .GetTargetPosition(PredictedFrame, attackComponent);
      _distanceToTarget = FPVector2.Distance(myPosition, finalPosition).AsFloat;

      var multiplier = 8 / _distanceToTarget;
      _currentGravity *= multiplier;
    }

    void Update()
    {
      if (_currentSpeed == 0)
        return;
      transform.Translate(0, _currentSpeed * Time.deltaTime, 0);
      _currentSpeed += _currentGravity * Time.deltaTime;
    }
  }
}
