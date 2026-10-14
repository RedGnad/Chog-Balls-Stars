using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// The rules of the structures a team defends (Structure.qtn): where they stand, how a tower
	/// shoots, what happens when one is destroyed, and who wins when a core falls. Kept apart
	/// from the system that calls them so each rule can be tried on a bare frame.
	/// </summary>
	public static unsafe class Structures
	{
		// First numbers, to be tuned by play. A hero has about 6,000 health and hits for 400 to
		// 900: a tower takes some fifteen hits, a core some thirty.
		public static readonly FP TowerHealth = 8000;
		public static readonly FP CoreHealth = 12000;
		public static readonly FP TowerRadius = FP._0_50 + FP._0_10;
		public static readonly FP CoreRadius = FP._0_75;
		public static readonly FP TowerRange = 5;
		public static readonly FP TowerDamage = 500;
		public static readonly FP TowerInterval = 1;

		// Where they stand on the sample's map, measured on its colliders
		// (tools/quantum/free_ground.py): the map turns onto itself around its centre, team 0 is
		// at the bottom, team 1 at the top. A core is in front of its team's spawn points and its
		// tower 4 units further out, so the tower's range covers whoever attacks the core.
		private static readonly FP CoreDistance = 12 + FP._0_50;
		private static readonly FP TowerDistance = 8 + FP._0_50;

		public static FPVector2 CorePosition(int team) => team == 0 ? new FPVector2(0, -CoreDistance) : new FPVector2(0, CoreDistance);
		public static FPVector2 TowerPosition(int team) => team == 0 ? new FPVector2(FP._0_50, -TowerDistance) : new FPVector2(-FP._0_50, TowerDistance);

		/// <summary>The team that wins because a core fell, or -1 while both cores stand.</summary>
		public static int WinnerByCore(Frame frame)
		{
			return frame.Global->DestroyedCore == 0 ? -1 : 1 - (frame.Global->DestroyedCore - 1);
		}

		/// <summary>
		/// The clock of the match as the machine that runs it must read it (GetTimer): that
		/// machine ends the match when the clock is under zero, and a core that fell ends it at
		/// once, whatever the clock says.
		/// </summary>
		public static FP MatchClock(Frame frame)
		{
			return WinnerByCore(frame) >= 0 ? -FP._1 : frame.Global->MatchTimer;
		}

		/// <summary>Puts a tower and a core for each of the two teams, once per match.</summary>
		public static void Build(Frame frame)
		{
			if (frame.ComponentCount<Structure>() > 0)
			{
				return;
			}

			for (int team = 0; team < 2; team++)
			{
				Create(frame, team, EStructureKind.Core, CorePosition(team), CoreRadius, CoreHealth);
				Create(frame, team, EStructureKind.Tower, TowerPosition(team), TowerRadius, TowerHealth);
			}
		}

		public static EntityRef Create(Frame frame, int team, EStructureKind kind, FPVector2 position, FP radius, FP health)
		{
			EntityRef entity = frame.Create();
			frame.Add(entity, Transform2D.Create(position, 0));

			// On the layer of the heroes: that is where every attack looks for what it hits.
			int layer = frame.Layers.GetLayerIndex("Character");
			frame.Add(entity, PhysicsCollider2D.Create(frame, Shape2D.CreateCircle(radius), null, false, layer < 0 ? 0 : layer));

			frame.Add(entity, new TeamInfo { Index = team });

			// The dictionary is filled before the component is added: adding it initialises
			// every attribute it holds (AttributesSystem.OnAdded).
			var attributes = frame.AllocateDictionary<EAttributeType, AttributeData>();
			attributes.Add(EAttributeType.Health, new AttributeData { InitialValue = health, MaxValue = health, CurrentValue = health });
			frame.Add(entity, new Attributes { DataDictionary = attributes });
			frame.Add(entity, new Health { CanSelfHeal = false });

			bool shoots = kind == EStructureKind.Tower;
			frame.Add(entity, new Structure
			{
				Kind = kind,
				Range = shoots ? TowerRange : 0,
				Damage = shoots ? TowerDamage : 0,
				Interval = shoots ? TowerInterval : 0,
			});
			return entity;
		}

		/// <summary>
		/// A tower's turn: when its time has come, it hurts the nearest living hero of the other
		/// team within its range. Returns that hero, or none.
		/// </summary>
		public static EntityRef Shoot(Frame frame, EntityRef entity, Structure* structure, Transform2D* transform, TeamInfo* team, Health* health)
		{
			FP now = frame.Number * frame.DeltaTime;
			if (health->IsDead || structure->Range <= 0 || now < structure->NextShotTime)
			{
				return default;
			}

			EntityRef target = default;
			FP nearest = structure->Range * structure->Range;
			var heroes = frame.Filter<Character, Transform2D, TeamInfo, Health>();
			while (heroes.NextUnsafe(out EntityRef hero, out Character* character, out Transform2D* heroTransform, out TeamInfo* heroTeam, out Health* heroHealth))
			{
				if (heroTeam->Index == team->Index || heroHealth->IsDead)
				{
					continue;
				}

				FP distance = FPVector2.DistanceSquared(transform->Position, heroTransform->Position);
				if (distance <= nearest)
				{
					// At equal distance the first hero found is kept: the order of a filter is
					// the same on every machine.
					if (distance < nearest || target == default)
					{
						nearest = distance;
						target = hero;
					}
				}
			}

			if (target == default)
			{
				return default;
			}

			structure->NextShotTime = now + structure->Interval;
			AttributesHelper.ChangeAttribute(frame, target, EAttributeType.Health, EModifierAppliance.OneTime, EModifierOperation.Subtract, structure->Damage, 0);
			frame.Events.StructureShot(entity, target, structure->Damage);
			return target;
		}

		// How far outside a tower's range a bot already turns away, and how hard: the push grows
		// from KeepAwayPush at that border to more than twice that at the foot of the tower, which
		// outweighs whatever else the bot wanted (a direction of length one or so).
		private static readonly FP KeepAwayMargin = FP._0_75;
		private static readonly FP KeepAwayPush = 2;

		/// <summary>
		/// What a bot adds to the direction it wants to go, to stay out of the reach of the
		/// living towers of the other team: nothing outside that reach, a push straight away
		/// from the tower inside it. Without this a bot walks under a tower and dies there,
		/// since nothing else in its mind knows the towers. A bot that attacks a structure
		/// (pushed) is let under the towers of that structure's team.
		/// </summary>
		public static FPVector2 KeepAway(Frame frame, EntityRef agent, EntityRef pushed = default)
		{
			if (frame.Unsafe.TryGetPointer<Transform2D>(agent, out var transform) == false
				|| frame.Unsafe.TryGetPointer<TeamInfo>(agent, out var team) == false)
			{
				return default;
			}

			int pushedTeam = frame.Unsafe.TryGetPointer<Structure>(pushed, out _) ? frame.Get<TeamInfo>(pushed).Index : -1;

			FPVector2 push = default;
			var towers = frame.Filter<Structure, Transform2D, TeamInfo, Health>();
			while (towers.NextUnsafe(out EntityRef tower, out Structure* structure, out Transform2D* towerTransform, out TeamInfo* towerTeam, out Health* towerHealth))
			{
				if (structure->Range <= 0 || towerHealth->IsDead || towerTeam->Index == team->Index || towerTeam->Index == pushedTeam)
				{
					continue;
				}

				FPVector2 away = transform->Position - towerTransform->Position;
				FP reach = structure->Range + KeepAwayMargin;
				FP distance = away.Magnitude;
				if (distance >= reach || distance <= FP.Epsilon)
				{
					continue;
				}

				push += away / distance * (KeepAwayPush + KeepAwayPush * (reach - distance) / reach);
			}

			return push;
		}

		// How long a structure that was hit calls its team's bots, how far from it an attacker
		// is looked for (a little more than the longest attack of a hero, 9), and how close to a
		// bot an enemy hero must be for that bot to fight it and leave the structure alone.
		public static readonly FP AlarmDuration = 4;
		public static readonly FP AlarmRadius = 10;
		public static readonly FP PushCalmDistance = 6;

		/// <summary>A structure was hit: its team's bots come while the alarm lasts.</summary>
		public static void Hurt(Frame frame, EntityRef entity)
		{
			if (frame.Unsafe.TryGetPointer<Structure>(entity, out var structure))
			{
				structure->AlarmUntil = frame.Number * frame.DeltaTime + AlarmDuration;
			}
		}

		/// <summary>The structure of that team a hero of the other team can attack now: its tower while it stands, then its core. None once both fell.</summary>
		public static EntityRef Attackable(Frame frame, int team)
		{
			EntityRef core = default;
			var all = frame.Filter<Structure, TeamInfo, Health>();
			while (all.NextUnsafe(out EntityRef entity, out Structure* structure, out TeamInfo* owner, out Health* health))
			{
				if (owner->Index != team || health->IsDead)
				{
					continue;
				}

				if (structure->Kind == EStructureKind.Tower)
				{
					return entity;
				}

				core = entity;
			}

			return core;
		}

		/// <summary>
		/// What a bot goes for before anything else it had in mind, or none.
		/// First it defends: when a structure of its team is under attack, the living enemy hero
		/// nearest to that structure. Then it pushes the structure of the other team that can be
		/// attacked, when its team has more heroes alive than the other, or when a player of its
		/// team already stands within a tower's reach of that structure. A bot with an enemy hero
		/// close to it does not push: it fights that hero. A bot low on health does neither.
		/// </summary>
		public static EntityRef BotObjective(Frame frame, EntityRef agent)
		{
			if (frame.Unsafe.TryGetPointer<Transform2D>(agent, out var transform) == false
				|| frame.Unsafe.TryGetPointer<TeamInfo>(agent, out var team) == false)
			{
				return default;
			}

			if (frame.Unsafe.TryGetPointer<AIMemory>(agent, out var memory) && memory->HealthStatus == EHealthStatus.Low)
			{
				return default;
			}

			FP now = frame.Number * frame.DeltaTime;
			EntityRef raider = default;
			FP raiderDistance = AlarmRadius * AlarmRadius;
			var mine = frame.Filter<Structure, Transform2D, TeamInfo, Health>();
			while (mine.NextUnsafe(out EntityRef structureEntity, out Structure* structure, out Transform2D* place, out TeamInfo* owner, out Health* health))
			{
				if (owner->Index != team->Index || health->IsDead || structure->AlarmUntil <= now)
				{
					continue;
				}

				var attackers = frame.Filter<Character, Transform2D, TeamInfo, Health>();
				while (attackers.NextUnsafe(out EntityRef hero, out Character* character, out Transform2D* heroPlace, out TeamInfo* heroTeam, out Health* heroHealth))
				{
					FP distance = FPVector2.DistanceSquared(place->Position, heroPlace->Position);
					if (heroTeam->Index != team->Index && heroHealth->IsDead == false && distance < raiderDistance)
					{
						raiderDistance = distance;
						raider = hero;
					}
				}
			}

			if (raider != default)
			{
				return raider;
			}

			EntityRef target = Attackable(frame, 1 - team->Index);
			if (target == default)
			{
				return default;
			}

			FPVector2 targetPosition = frame.Unsafe.GetPointer<Transform2D>(target)->Position;
			int allies = 0;
			int enemies = 0;
			bool playerThere = false;
			var heroes = frame.Filter<Character, Transform2D, TeamInfo, Health>();
			while (heroes.NextUnsafe(out EntityRef hero, out Character* character, out Transform2D* heroPlace, out TeamInfo* heroTeam, out Health* heroHealth))
			{
				if (heroHealth->IsDead)
				{
					continue;
				}

				if (heroTeam->Index != team->Index)
				{
					enemies++;
					if (FPVector2.DistanceSquared(transform->Position, heroPlace->Position) < PushCalmDistance * PushCalmDistance)
					{
						return default;
					}

					continue;
				}

				allies++;
				bool isPlayer = frame.Unsafe.TryGetPointer<Bot>(hero, out var bot) == false || bot->IsActive == false;
				if (isPlayer && hero != agent && FPVector2.DistanceSquared(targetPosition, heroPlace->Position) <= TowerRange * TowerRange)
				{
					playerThere = true;
				}
			}

			return allies > enemies || playerThere ? target : default;
		}

		/// <summary>
		/// A structure that lost its last health: it stays where it was, dead, hit by nothing and
		/// stopping nobody. A core that falls ends the match.
		/// </summary>
		public static void Destroyed(Frame frame, EntityRef entity)
		{
			Health* health = frame.Unsafe.GetPointer<Health>(entity);
			if (health->IsDead)
			{
				return;
			}

			health->IsDead = true;
			if (frame.Unsafe.TryGetPointer<PhysicsCollider2D>(entity, out var collider))
			{
				collider->Enabled = false;
			}

			Structure structure = frame.Get<Structure>(entity);
			int team = frame.Get<TeamInfo>(entity).Index;
			if (structure.Kind == EStructureKind.Core && frame.Global->DestroyedCore == 0)
			{
				frame.Global->DestroyedCore = (byte)(1 + team);
			}

			frame.Events.StructureDestroyed(entity, team, structure.Kind);
		}
	}
}
