using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public static unsafe class LineOfSightHelper
	{
		// Returns true if there's no static collider between source and target
		public static bool HasLineOfSight(Frame frame, FPVector2 source, FPVector2 target)
		{
			var layerMask = frame.Layers.GetLayerMask("Static");
			Physics2D.HitCollection hits = frame.Physics2D.LinecastAll(source, target, layerMask, QueryOptions.HitStatics);
			for (var i = 0; i < hits.Count; i++)
			{
				if (hits[i].IsDynamic == false)
				{
					return false;
				}
			}
			return true;
		}
	}
}