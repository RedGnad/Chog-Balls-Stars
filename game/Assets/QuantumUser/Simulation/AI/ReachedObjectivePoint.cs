using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class ReachedObjectivePoint : HFSMDecision
	{
		public AIBlackboardValueKey ObjectivePoint;
		public FP MinDistance = FP._1;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			FPVector2 myPosition = frame.Get<Transform2D>(entity).Position;

			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			FPVector2 objectivePoint = blackboard->GetVector2(frame, ObjectivePoint.Key);

			return FPVector2.Distance(myPosition, objectivePoint) < MinDistance;
		}
	}
}
