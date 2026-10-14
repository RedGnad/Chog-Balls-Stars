using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SensorTactics : Sensor
	{
		public AssetRef<TacticalSensor>[] TacticalSensors;

		private TacticalSensor[] _tacticalSensors;

		public override void Execute(Frame frame, EntityRef entity)
		{
			var bot = frame.Unsafe.GetPointer<Bot>(entity);
			if (bot->TacticalCommitment > 0)
			{
				bot->TacticalCommitment -= frame.DeltaTime;
				return;
			}

			// TickRate is authored in ticks at 60 Hz; keep the same real-time pace at any rate.
			if (frame.Number % TickIntervals.FromAuthoringTicks(frame, TickRate) != 0)
			{
				return;
			}

			// A structure to defend or to push comes before every other tactic. The bot fights
			// there: the state that fights a hero does the rest, whatever the target is. The
			// state is entered once, since entering it again would restart its wait before a shot.
			EntityRef duty = Structures.BotObjective(frame, entity);
			if (duty != default)
			{
				frame.Unsafe.GetPointer<AIBlackboardComponent>(entity)->Set(frame, AIConstants.KEY_TARGET, duty);
				if (bot->CurrentTactic != ETactics.Engage)
				{
					HFSMManager.TriggerEvent(frame, entity, AIConstants.TACTIC_EVENT_ENGAGE);
					bot->CurrentTactic = ETactics.Engage;
				}

				bot->TacticalCommitment = FP._1;
				return;
			}

			for (int i = 0; i < _tacticalSensors.Length; i++)
			{
				bool succeeded = _tacticalSensors[i].TrySetTactic(frame, entity, bot);

				if (succeeded == true)
				{
					bot->TacticalCommitment = _tacticalSensors[i].CommitmentValue;
					//Log.Info($"Tactical choice was: {_tacticalSensors[i].Path}");
					return;
				}
			}
		}

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			base.Loaded(resourceManager, allocator);

			_tacticalSensors = new TacticalSensor[TacticalSensors == null ? 0 : TacticalSensors.Length];
			if (TacticalSensors != null)
			{
				for (int i = 0; i < TacticalSensors.Length; i++)
				{
					_tacticalSensors[i] = (TacticalSensor)resourceManager.GetAsset(TacticalSensors[i].Id);
				}
			}
		}
	}
}
