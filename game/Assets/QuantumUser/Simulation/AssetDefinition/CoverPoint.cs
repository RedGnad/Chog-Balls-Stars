using Photon.Deterministic;

namespace Quantum
{
	// Asset used to store the Cover Points normals, used by the AI when reasoning about a good place to hide
	// This is used on the baking process on Unity, when triggering a Map bake
	public unsafe partial class CoverPoint : AssetObject
	{
		public FPVector3 Normal;
	}
}
