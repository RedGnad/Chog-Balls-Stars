namespace Quantum
{
	public unsafe partial struct _globals_
	{
		public InvisibilitySpot* GetInvisibilitySpot(Frame frame, int id)
		{
			if(id == -1)
			{
				return null;
			}

			var inviSpots = frame.ResolveDictionary(InvisibilitySpots);
			var inviSpotEntity = inviSpots[id];
			return frame.Unsafe.GetPointer<InvisibilitySpot>(inviSpotEntity);
		}

		public EntityRef GetInvisibilitySpotEntity(Frame frame, int id)
		{
			if (id == -1)
			{
				return default;
			}

			var inviSpots = frame.ResolveDictionary(InvisibilitySpots);
			return inviSpots[id];
		}
	}
}
