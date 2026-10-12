using Photon.Deterministic;
using System;

namespace Quantum
{
	[System.Serializable]
	public class ArcherSpecialFirstAttackData : BallisticAttackData
	{
		// Get the TargetPosition from the attack's runtime data
		// As this is a Ballistic attack, this basically informs where is the final destination of the ballistic shot
		public override unsafe FPVector2 GetTargetPosition(Frame frame, Attack* attack)
		{
			return attack->AttackRuntimeData.ArcherSpecialAttackRD->TargetPosition;
		}
	}
}
