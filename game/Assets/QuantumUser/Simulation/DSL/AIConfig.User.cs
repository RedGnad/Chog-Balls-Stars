using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial class AIConfig
	{
		public AssetRef<Sensor>[] SensorsRefs;

		public bool DebugEnabled;

		[System.NonSerialized]
		public Sensor[] SensorsInstances;

		public static AIConfig GetAIConfig(Frame frame, EntityRef entity)
		{
			if(frame.Unsafe.TryGetPointer<HFSMAgent>(entity, out var hfsmAgent) == true)
			{
				return hfsmAgent->GetConfig(frame);
			}

			if (frame.Unsafe.TryGetPointer<BTAgent>(entity, out var btAgent) == true)
			{
				return btAgent->GetConfig(frame);
			}

			if (frame.Unsafe.TryGetPointer<UTAgent>(entity, out var utAgent) == true)
			{
				return utAgent->GetConfig(frame);
			}

			return null;
		}

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			SensorsInstances = new Sensor[SensorsRefs == null ? 0 : SensorsRefs.Length];
			if (SensorsRefs != null)
			{
				for (int i = 0; i < SensorsRefs.Length; i++)
				{
					SensorsInstances[i] = (Sensor)resourceManager.GetAsset(SensorsRefs[i].Id);
				}
			}
		}

		public T GetSensor<T>() where T : Sensor
		{
			for (int i = 0; i < SensorsInstances.Length; i++)
			{
				if (SensorsInstances[i] is T sensor)
					return sensor;
			}

			return null;
		}
	}
}
