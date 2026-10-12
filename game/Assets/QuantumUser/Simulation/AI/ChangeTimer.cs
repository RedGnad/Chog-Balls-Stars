using Photon.Deterministic;

namespace Quantum
{
	public enum EOperation { None, Set, Add, Subtract }

	[System.Serializable]
	public unsafe class ChangeTimer : AIAction
	{
		public EOperation Operation;
		public bool ValueIsDeltaTime;
		public FP Value;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			FP value = ValueIsDeltaTime ? frame.DeltaTime : Value;

			switch (Operation)
			{
				case EOperation.Set:
					frame.Global->MatchTimer = value;
					break;
				case EOperation.Add:
					frame.Global->MatchTimer += value;
					break;
				case EOperation.Subtract:
					frame.Global->MatchTimer -= value;
					break;
				default:
					break;
			}
		}
	}
}
