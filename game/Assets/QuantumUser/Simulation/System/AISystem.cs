using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	// This sample's AI is a composition of a few sub-systems:
	// - The "brain" is the HFSM, responsible for decision making and to trigger a few actions such as Attacking
	// - The "muscles" is the Context Steering, responsible for actually moving the entity based on desires, threats and random evasion
	// - The "memory" comes in two flavors: the Blackboard, used directly by the HFSM, and the AIMemory, with it's own system
	// - The "ears/eyes/etc", which are the AISensors, responsible for perceiving data on the game state

	[Preserve]
	public unsafe class AISystem : SystemMainThreadFilter<AISystem.Filter>, ISignalOnComponentAdded<Bot>, ISignalOnNavMeshMoveAgent,
		ISignalOnGameStart, ISignalOnCreateAttack, ISignalOnCreateSkill
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Transform2D* Transform;
			public Bot* Bot;
			public Health* Health;
			public AISteering* AISteering;
		}

		public void OnAdded(Frame frame, EntityRef entity, Bot* component)
		{
			if (component->IsActive == true)
			{
				AISetupHelper.Botify(frame, entity);
			}
		}

		// When the game start, we automatically add all opponent characters to the avoidance memory, with a small threat area
		// This is useful for when the characters are focusing on something else and will still try to
		// steer away from any opponent character
		public void OnGameStart(Frame frame)
		{
			var allBots = frame.Filter<Bot, AIMemory, TeamInfo>();
			while (allBots.NextUnsafe(out EntityRef agentRef, out Bot* bot, out AIMemory* aiMemory, out TeamInfo* agentTeamInfo))
			{
				if (bot->IsActive == false)
				{
					continue;
				}

				var allCharacters = frame.Filter<Character, TeamInfo>();
				while (allCharacters.Next(out EntityRef characterRef, out Character character, out TeamInfo teamInfo))
				{
					if (agentTeamInfo->Index == teamInfo.Index)
					{
						continue;
					}

					AIMemoryEntry* memoryEntry = aiMemory->AddInfiniteMemory(frame, EMemoryType.AreaAvoidance);
					memoryEntry->Data.AreaAvoidance->SetData(characterRef, runDistance: FP._2);
				}
			}
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.Bot->IsActive == false)
			{
				return;
			}

			if(filter.Health->IsDead == true)
			{
				return;
			}

			UpdateSensors(frame, filter.Entity);

			HandleContextSteering(frame, filter);

			HFSMManager.Update(frame, frame.DeltaTime, filter.Entity);
		}

		// Update the Bot sensors
		// Each sensor has it's own tick rate and polymorphic logic
		private void UpdateSensors(Frame frame, EntityRef entity)
		{
			AIConfig aiConfig = frame.FindAsset<AIConfig>(frame.Get<HFSMAgent>(entity).Config.Id);
			Sensor[] sensors = aiConfig.SensorsInstances;
			for (int i = 0; i < sensors.Length; i++)
			{
				Assert.Check(sensors[i] != null, "Sensor {0} not found, for entity {1}", i, entity);
				Assert.Check(sensors[i].TickRate != 0, "Sensor {0} needs to have a Tick Rate greater than zero", i);
				sensors[i].Execute(frame, entity);
			}
		}

		// If the current steering mode currently is NavMesh steering, this means that the navigation agent has a desired position,
		// meaning that we will get a desired direction from this callback
		// We then store that information on the steering data, so it can be used by the context steering
		public void OnNavMeshMoveAgent(Frame frame, EntityRef entity, FPVector2 desiredDirection)
		{
			AISteering* aiSteering = frame.Unsafe.GetPointer<AISteering>(entity);

			if (aiSteering->IsNavMeshSteering == true)
			{
				aiSteering->MainSteeringData.SteeringEntryNavMesh->SetData(desiredDirection);
			}
		}

		// The context steering takes into consideration many things: what is the character main target, what are the threats, etc
		// It updates the agent position with the KCC based on a final desired direction
		// It works with/without the navigation agent
		private void HandleContextSteering(Frame frame, Filter filter)
		{
			// Process the final desired direction
			FPVector2 desiredDirection = filter.AISteering->GetDesiredDirection(frame, filter.Entity);

			// Lerp the current value towards the desired one so it doesn't turn too subtle
			filter.AISteering->CurrentDirection = FPVector2.MoveTowards(filter.AISteering->CurrentDirection, desiredDirection,
				frame.DeltaTime * filter.AISteering->LerpFactor);

			// The MovementDirection input is (de)coded and it is always normalized (unless it's value is zero)
			// That's why we compute the direction in a regular FPVector2 for the MoveTowards to properly work
			// Then we assign the value to the bot input, which is later used by the InputSystem in order to move the KCC
			filter.Bot->Input.MoveDirection = filter.AISteering->CurrentDirection;
		}

		// When an attack is created, we want to check if it is a threat to us and, if it is, we add it to the Agent's AIMemory
		// so that data can be used in the avoidance
		// We react to the actual Attack creation (not the Skill), because this is the moment where the Spellcaster skill
		// actually became a damage area on the ground
		public void OnCreateAttack(Frame frame, EntityRef attackEntity, Attack* attack)
		{
			var bots = frame.Filter<Bot, TeamInfo>();
			while (bots.NextUnsafe(out EntityRef botEntity, out Bot* bot, out TeamInfo* botTeamInfo))
			{
				TeamInfo* attackerTeamInfo = frame.Unsafe.GetPointer<TeamInfo>(botEntity);
				if (bot->IsActive == false || attackerTeamInfo->Index == botTeamInfo->Index)
				{
					continue;
				}

				AIConfig aiConfig = AIConfig.GetAIConfig(frame, botEntity);
				if (aiConfig != null)
				{
					SensorThreats sensorThreats = aiConfig.GetSensor<SensorThreats>();
					if (sensorThreats != null)
					{
						sensorThreats.OnCircularAttackCreated(frame, attackEntity, attack);
					}
				}
			}
		}

		// Similar to OnCreateAttack, used to register data for the avoidance
		// This is used to be aware of projectile attacks, such as the Archer basic hability
		public void OnCreateSkill(Frame frame, EntityRef attacker, FPVector2 characterPos, SkillData data, FPVector2 actionDirection)
		{
			var bots = frame.Filter<Bot, TeamInfo>();
			while (bots.NextUnsafe(out EntityRef botEntity, out Bot* bot, out TeamInfo* botTeamInfo))
			{
				TeamInfo* attackerTeamInfo = frame.Unsafe.GetPointer<TeamInfo>(attacker);
				if (bot->IsActive == false || attackerTeamInfo->Index == botTeamInfo->Index)
				{
					continue;
				}

				AIConfig aiConfig = AIConfig.GetAIConfig(frame, botEntity);
				if (aiConfig != null)
				{
					SensorThreats sensorThreats = aiConfig.GetSensor<SensorThreats>();
					if (sensorThreats != null)
					{
						sensorThreats.OnLinearAttackCreated(frame, attacker, characterPos, data, actionDirection);
					}
				}
			}
		}
	}
}
;
