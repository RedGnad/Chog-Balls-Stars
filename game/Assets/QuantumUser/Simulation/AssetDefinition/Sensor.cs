using Photon.Deterministic;

namespace Quantum
{
	public unsafe abstract partial class Sensor : AssetObject
	{
		public int TickRate = 1;

		public virtual void Execute(Frame frame, EntityRef entity)
		{
		}
	}
}
