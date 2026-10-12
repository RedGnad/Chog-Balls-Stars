using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct AIMemoryEntry
	{
		public void SetTicks(Frame frame, FP avaliableDelay, FP unavailableDelay)
		{
			AvailableTick = frame.Number + (int)(avaliableDelay * frame.UpdateRate);
			UnavailableTick = frame.Number + (int)(unavailableDelay * frame.UpdateRate);
		}

		public bool IsAvailable(Frame frame)
		{
			return frame.Number > AvailableTick;
		}

		public bool IsForgotten(Frame frame)
		{
			return frame.Number > UnavailableTick;
		}
	}
}
