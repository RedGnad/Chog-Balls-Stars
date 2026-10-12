using Photon.Deterministic;
using System;

namespace Quantum
{
	partial class RuntimeConfig
	{
		public bool ShowIntroduction;

		public AssetRef<HFSMRoot> GameManagerHFSM;

		public AssetRef<EntityPrototype>[] RoomFillBots;
		public FP RoomFillInterval = 2;
		public bool FillWithBots = true;

		partial void SerializeUserData(BitStream stream)
		{
			stream.Serialize(ref ShowIntroduction);
			stream.Serialize(ref GameManagerHFSM);

			stream.SerializeArrayLength(ref RoomFillBots);
			for (var i = 0; i < RoomFillBots.Length; i++)
			{
				stream.Serialize(ref RoomFillBots[i]);
			}
			stream.Serialize(ref RoomFillInterval);

			stream.Serialize(ref FillWithBots);
		}
	}
}