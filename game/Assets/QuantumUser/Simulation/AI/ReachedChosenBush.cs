using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class ReachedChosenBush : HFSMDecision
	{
		public AIBlackboardValueKey ChosenBush;
		public FP MinDistance;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			FPVector2 myPosition = frame.Get<Transform2D>(entity).Position;

			EntityRef bushEntity = frame.Get<AIBlackboardComponent>(entity).GetEntityRef(frame, ChosenBush.Key);
			FPVector2 chosenBushPosition = frame.Get<Transform2D>(bushEntity).Position;

			return FPVector2.Distance(myPosition, chosenBushPosition) < MinDistance;
		}
	}
}
