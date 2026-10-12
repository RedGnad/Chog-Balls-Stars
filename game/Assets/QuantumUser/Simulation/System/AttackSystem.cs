using Photon.Deterministic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public unsafe class AttackSystem : SystemMainThreadFilter<AttackSystem.Filter>
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Attack* Attack;
		}

		// Updates the Attack entities (projectiles, damage zones, etc)
		// Deactivates attacks if their TTL is finished, otherwise updates it's logic
		public override void Update(Frame frame, ref Filter filter)
		{
			AttackData data = frame.FindAsset<AttackData>(filter.Attack->AttackData.Id);
			if (TimerExpired(ref filter, data) == true)
			{
				data.OnDeactivate(frame, filter.Entity);
				return;
			}

			data.OnUpdate(frame, filter.Entity, filter.Attack);
		}

		private bool TimerExpired(ref Filter filter, AttackData attackData)
		{
			return filter.Attack->TTL > attackData.TTL;
		}
	}
}
