using Photon.Deterministic;

namespace Quantum
{
	public partial class RuntimeConfig
	{
		// Which rules this match is played with (MatchMode.qtn). A config without it, as in every
		// record made before 16 October 2026, is TowersAndCoins.
		public EMatchMode Mode;
	}

	/// <summary>
	/// What each mode changes in the match. A siege has no coin: none is spawned and the coin
	/// countdown of the sample's game manager never starts; its clock runs from the start of play
	/// for SiegeSeconds, and the score of each team is the damage it has done to the other team's
	/// structures, in hundreds of health (what decides when no core falls, as the coins do in the
	/// other modes). A match of coins has no structure.
	/// </summary>
	public static unsafe class MatchModes
	{
		public static readonly FP SiegeSeconds = 180;
		// Health of the other team's structures that is worth one point of a siege.
		public static readonly FP HealthPerSiegePoint = 100;

		public static bool HasStructures(Frame frame) => frame.RuntimeConfig.Mode != EMatchMode.Coins;
		public static bool HasCoins(Frame frame) => frame.RuntimeConfig.Mode != EMatchMode.Siege;
		public static bool IsSiege(Frame frame) => frame.RuntimeConfig.Mode == EMatchMode.Siege;

		/// <summary>The play of the match begins: the clock of a siege starts (never at exactly 0, which means "not yet").</summary>
		public static void PlayBegins(Frame frame)
		{
			frame.Global->GameplayStartedAt = FPMath.Max(frame.Number * frame.DeltaTime, FP.Epsilon);
		}

		/// <summary>What is left of a siege's time: all of it before play begins, below zero once it ran out.</summary>
		public static FP SiegeTimeLeft(Frame frame)
		{
			return SiegeTimeLeftAt(frame.Global->GameplayStartedAt, frame.Number * frame.DeltaTime);
		}

		public static FP SiegeTimeLeftAt(FP startedAt, FP now)
		{
			return startedAt <= 0 ? SiegeSeconds : SiegeSeconds - (now - startedAt);
		}

		/// <summary>
		/// In a siege, gives each team as its score the health its attacks took from the other
		/// team's structures, in whole points: a fallen structure counts all its health.
		/// </summary>
		public static void CountSiegePoints(Frame frame)
		{
			if (IsSiege(frame) == false)
			{
				return;
			}

			var teams = frame.ResolveList(frame.Global->TeamsData);
			if (teams.Count < 2)
			{
				return;
			}

			FP takenFrom0 = 0, takenFrom1 = 0;
			var all = frame.Filter<Structure, TeamInfo, Health>();
			while (all.NextUnsafe(out EntityRef entity, out Structure* structure, out TeamInfo* owner, out Health* standing))
			{
				var health = AttributesHelper.GetAttributeData(frame, entity, EAttributeType.Health);
				FP taken = standing->IsDead ? health.MaxValue : FPMath.Max(0, health.MaxValue - health.CurrentValue);
				if (owner->Index == 0)
				{
					takenFrom0 += taken;
				}
				else if (owner->Index == 1)
				{
					takenFrom1 += taken;
				}
			}

			teams.GetPointer(0)->Score = FPMath.FloorToInt(takenFrom1 / HealthPerSiegePoint);
			teams.GetPointer(1)->Score = FPMath.FloorToInt(takenFrom0 / HealthPerSiegePoint);
		}
	}
}
