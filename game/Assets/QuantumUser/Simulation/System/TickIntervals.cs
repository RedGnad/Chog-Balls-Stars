namespace Quantum
{
	/// <summary>
	/// Converts tick intervals authored for the sample's original 60 Hz simulation rate
	/// into ticks of the running session, so timed bot logic keeps the same real-time pace
	/// at any rate. Integer arithmetic only, hence deterministic on every client.
	/// </summary>
	public static class TickIntervals
	{
		public const int AuthoringRate = 60;

		/// <summary>Rounds to the nearest tick and never returns less than one tick.</summary>
		public static int FromAuthoringTicks(Frame frame, int ticksAtAuthoringRate)
		{
			return FromAuthoringTicks(frame.UpdateRate, ticksAtAuthoringRate);
		}

		public static int FromAuthoringTicks(int updateRate, int ticksAtAuthoringRate)
		{
			var scaled = (ticksAtAuthoringRate * updateRate + AuthoringRate / 2) / AuthoringRate;
			return scaled < 1 ? 1 : scaled;
		}
	}
}
