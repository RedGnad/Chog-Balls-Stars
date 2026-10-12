using Photon.Deterministic;

namespace Quantum
{
	public unsafe abstract partial class TacticalSensor : AssetObject
	{
		public FP CommitmentValue;

		public abstract bool TrySetTactic(Frame frame, EntityRef entity, Bot* bot);
	}
}
