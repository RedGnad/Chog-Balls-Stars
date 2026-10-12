using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class HasChosenBush : HFSMDecision
	{
		public AIBlackboardValueKey ChosenBush;

		public override bool Decide(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			EntityRef bushEntity = frame.Get<AIBlackboardComponent>(entity).GetEntityRef(frame, ChosenBush.Key);

			return frame.Exists(bushEntity);
		}
	}
}
