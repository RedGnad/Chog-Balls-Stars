using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe class HoldAttack : AIAction
	{
		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			Bot* bot = frame.Unsafe.GetPointer<Bot>(entity);

			// Hold both buttons, just to make it ready for when one of them gets released, so we get the
			// WasReleased state right away
			bot->Input.AltFire.Update(frame.Number, true);
			bot->Input.Fire.Update(frame.Number, true);
		}
	}
}
