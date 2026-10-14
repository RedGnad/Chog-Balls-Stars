using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct AISteering
	{
		public bool IsContextSteering => MainSteeringData.Field == SteeringData.STEERINGENTRYCONTEXT;
		public bool IsNavMeshSteering => MainSteeringData.Field == SteeringData.STEERINGENTRYNAVMESH;

		public FPVector2 GetDesiredDirection(Frame frame, EntityRef agent)
		{
			// First we process the main steering entry, which is either NavMesh or Context
			FPVector2 desiredDirection = ProcessSteeringEntry(frame, agent, MainSteeringData);

			// Then, we check on the memory the current avoidance stuff and add it to the desired direction
			AIMemory* aiMemory = frame.Unsafe.GetPointer<AIMemory>(agent);
			var memoryEntries = frame.ResolveList(aiMemory->MemoryEntries);
			for (int i = 0; i < memoryEntries.Count; i++)
			{
				if (memoryEntries[i].IsAvailable(frame) == true)
				{
					desiredDirection += ProcessAvoidanceFromMemory(frame, agent, memoryEntries.GetPointer(i));
				}
			}

			// Last, the towers of the other team: a bot stays out of their reach, unless it is
			// attacking a structure of that team.
			desiredDirection += Structures.KeepAway(frame, agent, Pushed(frame, agent));

			return desiredDirection.Normalized;
		}

		// The structure this bot is attacking, or none.
		private static EntityRef Pushed(Frame frame, EntityRef agent)
		{
			if (frame.Unsafe.TryGetPointer<Bot>(agent, out var bot) == false || bot->CurrentTactic != ETactics.Engage
				|| frame.Unsafe.TryGetPointer<AIBlackboardComponent>(agent, out var blackboard) == false)
			{
				return default;
			}

			EntityRef target = blackboard->GetEntityRef(frame, AIConstants.KEY_TARGET);
			return frame.Has<Structure>(target) ? target : default;
		}

		private FPVector2 ProcessSteeringEntry(Frame frame, EntityRef agent, SteeringData steeringData)
		{
			FPVector2 desiredDirection;

			switch (steeringData.Field)
			{
				case SteeringData.STEERINGENTRYNAVMESH:
					desiredDirection = ProcessNavMeshEntry(frame, agent, steeringData.SteeringEntryNavMesh);
					break;
				case SteeringData.STEERINGENTRYCONTEXT:
					desiredDirection = ProcessCharacterEntry(frame, agent, steeringData.SteeringEntryContext);
					break;
				default:
					return default(FPVector2);
			}
			return desiredDirection * MainSteeringWeight;
		}

		private FPVector2 ProcessNavMeshEntry(Frame frame, EntityRef agent, SteeringEntryNavMesh* entry)
		{
			// The direction generated for the nav mesh entry is done on the NavMesh callback, that's why
			// we don't need to calculate it here, just retrieve it as is
			return entry->NavMeshDirection;
		}

		private FPVector2 ProcessCharacterEntry(Frame frame, EntityRef agent, SteeringEntryContext* entry)
		{
			FPVector2 desiredDirection = default;

			FPVector2 agentPosition = frame.Unsafe.GetPointer<Transform2D>(agent)->Position;
			FPVector2 targetPosition = frame.Unsafe.GetPointer<Transform2D>(entry->CharacterRef)->Position;
			FPVector2 dirToTarget = (targetPosition - agentPosition).Normalized;

			FP distToTargetSquared = FPVector2.DistanceSquared(agentPosition, targetPosition);
			if (distToTargetSquared == 0)
				return default;

			FP runDistance = entry->RunDistance;
			FP threatDistance = entry->ThreatDistance;

			bool evasionIsCircular = false;

			if (Debug == true)
			{
				Draw.Circle(targetPosition, runDistance, ColorRGBA.Red.SetA(10));
				Draw.Circle(targetPosition, threatDistance, ColorRGBA.Green.SetA(10));
			}

			if (distToTargetSquared < runDistance * runDistance)
			{
				var hit = frame.Physics2D.Raycast(agentPosition, -dirToTarget, 3, frame.Layers.GetLayerMask("Static"), QueryOptions.HitStatics);

				if (Debug == true)
				{
					Draw.Line(agentPosition, agentPosition - dirToTarget * 3);
				}

				if (hit.HasValue == false)
				{

					FP force = 1 / FPMath.Sqrt(distToTargetSquared);
					desiredDirection -= dirToTarget * force;
				}
				else
				{
					var angle = (FPHelpers.SignedAngle(-dirToTarget, FPVector2.Right) + 10) * FP.Deg2Rad;
					desiredDirection += new FPVector2(FPMath.Sin(angle), FPMath.Cos(angle));

					if (Debug == true)
					{
						Draw.Line(agentPosition, agentPosition + desiredDirection, ColorRGBA.Green);
					}
				}
			}
			else if (distToTargetSquared < threatDistance * threatDistance)
			{
				evasionIsCircular = true;
			}
			else
			{
				desiredDirection += dirToTarget / 30;
			}

			HandleEvasion(frame, agent, distToTargetSquared, ref desiredDirection, dirToTarget, evasionIsCircular);

			return desiredDirection;
		}

		private void HandleEvasion(Frame frame, EntityRef agent, FP distToTargetSquared, ref FPVector2 desiredDirection, FPVector2 dirToTarget, bool isCircular)
		{
			Transform2D* agentTransform = frame.Unsafe.GetPointer<Transform2D>(agent);

			if (EvasionTimer <= 0)
			{
				DefineEvasionDirection(frame, agentTransform, dirToTarget, isCircular);
			}
			else
			{
				PerformEvasion(frame, isCircular, ref desiredDirection, dirToTarget);
			}
		}

		private void DefineEvasionDirection(Frame frame, Transform2D* agentTransform, FPVector2 dirToTarget, bool isCircular)
		{
			EvasionTimer = MaxEvasionDuration;

			if (isCircular == false)
			{
				// We re-balance the random direction based on the previous random dir so we won't repeat the same direction too much
				int randomDir = frame.RNG->NextInclusive(-2, 2) - EvasionDirection;
				if (randomDir <= -1)
				{
					EvasionDirectionVector = agentTransform->Left / 50;
					EvasionDirection = -1;
				}
				else if (randomDir >= 1)
				{
					EvasionDirectionVector = agentTransform->Right / 50;
					EvasionDirection = 1;
				}
				else
				{
					EvasionDirectionVector = default;
					EvasionDirection = 0;
				}

				if(EvasionDirection != 0)
				{
					var hit = frame.Physics2D.Raycast(agentTransform->Position, EvasionDirectionVector, 5, frame.Layers.GetLayerMask("Static"), QueryOptions.HitStatics);
					if (Debug == true)
					{
						Draw.Line(agentTransform->Position, agentTransform->Position + EvasionDirectionVector * 5);
					}
					if (hit.HasValue == true)
					{
						EvasionDirectionVector *= -1;
					}
				}
			}
			else
			{
				// When the evasion is circular, we just do a 50/50, because otherwise the Bot would stop moving
				// so we just let it always do zig-zag
				int randomDir = frame.RNG->NextInclusive(0, 1);

				FPVector2 evasionDir = default;
				FP angle = 0;
				if(randomDir == 0)
				{
					angle = (FPHelpers.SignedAngle(dirToTarget, FPVector2.Right) + 5) * FP.Deg2Rad;
				}
				else
				{
					angle = (FPHelpers.SignedAngle(dirToTarget, FPVector2.Left) - 5) * FP.Deg2Rad;
				}
				evasionDir = new FPVector2(FPMath.Sin(angle), FPMath.Cos(angle));

				var hit = frame.Physics2D.Raycast(agentTransform->Position, evasionDir, 4, frame.Layers.GetLayerMask("Static"), QueryOptions.HitStatics);
				if (Debug == true)
				{
					Draw.Line(agentTransform->Position, agentTransform->Position + evasionDir * 4);
				}
				if (hit.HasValue == true)
				{
					randomDir = randomDir == 0 ? 1 : 0;
				}

				EvasionDirection = randomDir;
			}
		}

		private void PerformEvasion(Frame frame, bool isCircular, ref FPVector2 desiredDirection, FPVector2 dirToTarget)
		{
			EvasionTimer -= frame.DeltaTime;

			if (isCircular == false)
			{
				desiredDirection += EvasionDirectionVector;
			}
			else
			{
				FP angle;
				if (EvasionDirection == 0)
				{
					angle = (FPHelpers.SignedAngle(dirToTarget, FPVector2.Right) + 5) * FP.Deg2Rad;
					// Prevents it from spiraling IN the circle
					desiredDirection -= dirToTarget / FP._10;
				}
				else
				{
					angle = (FPHelpers.SignedAngle(dirToTarget, FPVector2.Left) - 5) * FP.Deg2Rad;
					// Prevents it from spiraling OUT of the circle
					desiredDirection += dirToTarget / FP._10;
				}

				desiredDirection += new FPVector2(FPMath.Sin(angle), FPMath.Cos(angle));
			}
		}

		// ---- Avoidance from areas and lines (usually attacks or enemy heroes which are not on focus)
		private FPVector2 ProcessAvoidanceFromMemory(Frame frame, EntityRef agent, AIMemoryEntry* entry)
		{
			FPVector2 desiredDirection;

			switch (entry->Data.Field)
			{
				case MemoryData.AREAAVOIDANCE:
					desiredDirection = ProcessAreaAvoidance(frame, agent, entry->Data.AreaAvoidance);
					break;
				case MemoryData.LINEAVOIDANCE:
					desiredDirection = ProcessLineAvoidance(frame, agent, entry->Data.LineAvoidance);
					break;
				default:
					return default(FPVector2);
			}

			return desiredDirection;
		}

		private FPVector2 ProcessAreaAvoidance(Frame frame, EntityRef agent, MemoryDataAreaAvoidance* entry)
		{
			FPVector2 desiredDirection = default;

			FPVector2 agentPosition = frame.Unsafe.GetPointer<Transform2D>(agent)->Position;
			FPVector2 targetPosition = frame.Unsafe.GetPointer<Transform2D>(entry->Entity)->Position;
			FPVector2 dirToTarget = (targetPosition - agentPosition).Normalized;

			FP distToTargetSquared = FPVector2.DistanceSquared(agentPosition, targetPosition);
			FP runDistance = entry->RunDistance;

			if (Debug == true)
			{
				Draw.Circle(targetPosition, runDistance, ColorRGBA.Red.SetA(10));
			}

			if (distToTargetSquared < runDistance * runDistance)
			{
				var hit = frame.Physics2D.Raycast(agentPosition, -dirToTarget, 3, frame.Layers.GetLayerMask("Static"), QueryOptions.HitStatics);

				if (Debug == true)
				{
					Draw.Line(agentPosition, agentPosition - dirToTarget * 3);
				}

				if (hit.HasValue == false)
				{
					desiredDirection -= dirToTarget;
				}
				else
				{
					var angle = (FPHelpers.SignedAngle(-dirToTarget, FPVector2.Right) + 10) * FP.Deg2Rad;
					desiredDirection += new FPVector2(FPMath.Sin(angle), FPMath.Cos(angle));

					if (Debug == true)
					{
						Draw.Line(agentPosition, agentPosition + desiredDirection, ColorRGBA.Green);
					}
				}
			}

			return desiredDirection * entry->Weight;
		}

		private FPVector2 ProcessLineAvoidance(Frame frame, EntityRef agent, MemoryDataLineAvoidance* entry)
		{
			FPVector2 runawayDirection = default;

			// Run to the perpendicular direction considering the attack
			Transform2D attackerTransform = frame.Get<Transform2D>(entry->Entity);
			FPVector2 dir = frame.Get<Transform2D>(agent).Position - frame.Get<Transform2D>(entry->Entity).Position;
			runawayDirection.X = attackerTransform.Up.Y;
			runawayDirection.Y = -attackerTransform.Up.X;

			if (FPVector2.RadiansSigned(dir.Normalized, attackerTransform.Up) < 0)
			{
				runawayDirection *= -1;
			}

			runawayDirection *= 2;

			if (Debug == true)
			{
				var agentPos = frame.Get<Transform2D>(agent).Position;
				Draw.Line(agentPos, agentPos + runawayDirection, ColorRGBA.Blue);
			}

			return runawayDirection * entry->Weight;
		}

		// ---- Switch Steering
		public void SetContextSteeringEntry(Frame frame, EntityRef agentRef, EntityRef characterRef, FP runDistance, FP threatDistance)
		{
			MainSteeringData.SteeringEntryContext->SetData(characterRef, runDistance, threatDistance);
		}

		public void SetNavMeshSteeringEntry(Frame frame, EntityRef agentRef)
		{
			// Touch the pointer getter just so it changes the union "type"
			MainSteeringData.SteeringEntryNavMesh->NavMeshDirection = default;
		}
	}
}
