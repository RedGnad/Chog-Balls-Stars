using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class CollectibleDataCoin : CollectibleData
	{
		// Collecting/Dropping coins increases/reduces that team's score

		public override void OnCollect(Frame frame, EntityRef entity, Inventory* inventory)
		{
			int teamId = frame.Get<TeamInfo>(entity).Index;

			var team = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId);
			team->Score += 1;
		}

		public override void OnDrop(Frame frame, EntityRef entity, Inventory* inventory, int amount, FPVector2 position)
		{
			base.OnDrop(frame, entity, inventory, amount, position);

			int teamId = frame.Get<TeamInfo>(entity).Index;

			var team = frame.ResolveList(frame.Global->TeamsData).GetPointer(teamId);
			team->Score -= amount;
		}
	}
}
