using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quantum
{
	partial class RuntimePlayer
	{
		public bool ForceAI;
		public int TeamIndex;

		partial void SerializeUserData(BitStream stream)
		{
			stream.Serialize(ref ForceAI);
			stream.Serialize(ref TeamIndex);
		}
	}
}
