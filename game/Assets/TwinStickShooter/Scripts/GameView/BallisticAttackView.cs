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
    // What was authored, read once: a view handed back by the pool of attack views (ChogAttackViewPool)
    // starts again from it, not from where its last throw ended.
    private float _authoredSpeed;
    private float _authoredGravity;
    private Vector3 _authoredPlace;
    private bool _authored;

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      if (_authored == false)
      {
        _authoredSpeed = _currentSpeed;
        _authoredGravity = _currentGravity;
        _authoredPlace = transform.localPosition;
        _authored = true;
      }
      _currentSpeed = _authoredSpeed;
      _currentGravity = _authoredGravity;
      transform.localPosition = _authoredPlace;

      Attack* attackComponent = frame.Unsafe.GetPointer<Attack>(EntityRef);
      Transform2D myTransform = frame.Get<Transform2D>(EntityRef);

      FPVector2 myPosition = myTransform.Position;
      FPVector2 finalPosition = frame.FindAsset<BallisticAttackData>(attackComponent->AttackData.Id)
        .GetTargetPosition(frame, attackComponent);
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
