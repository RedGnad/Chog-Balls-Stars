using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class ArcherBasicAttackData : RangedAttackData
	{
		// The Archer basic attack currently doesn't need any extra logic. Only it's parent class logic is necessary
		// We still created this asset as to make it character specific, but that's not mandatory
	}
}
