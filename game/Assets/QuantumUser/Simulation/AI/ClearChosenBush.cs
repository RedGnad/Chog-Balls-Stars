using Photon.Deterministic;
using Quantum.Physics2D;

namespace Quantum
{
	[System.Serializable]
	public unsafe class ClearChosenBush : AIAction
	{
		public AIBlackboardValueKey ChosenBush;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->Set(frame, ChosenBush.Key, default(EntityRef));
		}
	}
}
