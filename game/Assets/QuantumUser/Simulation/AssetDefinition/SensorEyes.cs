using Photon.Deterministic;

namespace Quantum
{
	[System.Serializable]
	public unsafe partial class SensorEyes : Sensor
	{
		public FP VisibilityRange;

		public override void Execute(Frame frame, EntityRef entity)
		{
			AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(entity);
			Transform2D myTransform = frame.Get<Transform2D>(entity);

			TeamData* opponentTeamData = frame.Global->GetOpponentTeamData(frame, entity);
			EntityRef closestEnemy = default;
			if (opponentTeamData->TeamStatus.HasFlag(ETeamStatus.Winning) == true
				|| opponentTeamData->TeamStatus.HasFlag(ETeamStatus.SafelyWinning) == true)
			{
				EnemyPositionsHelper.TryGetMostValuableCharacter(frame, entity, myTransform,
					VisibilityRange * 3, checkLineSight: false, ignoreSameTeam: true, out closestEnemy);
			}
			else
			{
				EnemyPositionsHelper.TryGetClosestCharacter(frame, entity,
					VisibilityRange, checkLineSight: false, ignoreSameTeam: true, out closestEnemy);
			}

			// A structure to defend or to push comes before the enemy the eyes found.
			EntityRef duty = Structures.BotObjective(frame, entity);
			if (duty != default)
			{
				closestEnemy = duty;
			}

			if(closestEnemy == default)
			{
				return;
			}

			AIBlackboardComponent* blackboard = frame.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			blackboard->Set(frame, AIConstants.KEY_TARGET, closestEnemy);
		}
	}
}
