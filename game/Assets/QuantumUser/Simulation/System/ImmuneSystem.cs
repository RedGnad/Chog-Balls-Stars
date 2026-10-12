using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	// This system deals with updating the character immunity time
	[Preserve]
	public unsafe class ImmuneSystem : SystemMainThreadFilter<ImmuneSystem.Filter>, ISignalOnSetCharacterImmune
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Immunity* Immunity;
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if(filter.Immunity->Timer > 0)
			{
				filter.Immunity->Timer -= frame.DeltaTime;
			}
		}

		public void OnSetCharacterImmune(Frame frame, EntityRef character, FP time)
		{
			// We have a specific immunity component, with a partial declaration to tell "when" the character is immune
			Immunity* immunity = frame.Unsafe.GetPointer<Immunity>(character);
			immunity->Timer = time;
		}
	}
}
