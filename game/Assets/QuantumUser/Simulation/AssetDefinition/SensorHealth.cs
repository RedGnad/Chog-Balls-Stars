using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SensorHealth : Sensor
	{
		public FP LowHealthThreshold = 30;
		public FP MediumHealthThreshold = 50;
		public FP HighHealthThreshold = 80;

		public override void Execute(Frame frame, EntityRef entity)
		{
			AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(entity);
			FP healthPercentage = AttributesHelper.GetPercentage(frame, entity, EAttributeType.Health);

			if(healthPercentage >= HighHealthThreshold)
			{
				aiMemory->HealthStatus = EHealthStatus.High;
			}
			else if(healthPercentage >= MediumHealthThreshold)
			{
				aiMemory->HealthStatus = EHealthStatus.Medium;
			}
			else
			{
				aiMemory->HealthStatus = EHealthStatus.Low;
			}
		}
	}
}
