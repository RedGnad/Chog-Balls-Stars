namespace Quantum
{
	public unsafe partial struct MovementData
	{
		public bool IsOnAttackLock => DirectionTimer > 0;
		public bool IsOnAttackMovementLock => MovementTimer > 0;
	}
}
