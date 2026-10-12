using Photon.Deterministic;

namespace Quantum
{
	public unsafe static class FPVector2Helpers
	{
		public static FPVector2 RandomInsideCircle(Frame frame, FPVector2 origin, FP radius)
		{
			FPVector2 randomPos = default;
			do
			{
				var x = frame.RNG->Next(-radius, radius);
				var z = frame.RNG->Next(-radius, radius);
				randomPos = origin + new FPVector2(x, z);
			} while (FPVector2.Distance(origin, randomPos) > radius);

			return randomPos;
		}
	}
}
