using Photon.Deterministic;

namespace Quantum
{
	public unsafe static class FPHelpers
	{
		public static FP SignedAngle(FPVector2 from, FPVector2 to)
		{
			FP num = FPVector2.Angle(from, to);
			FP num2 = FPMath.Sign(from.X * to.Y - from.Y * to.X);
			return num * num2;
		}
	}
}
