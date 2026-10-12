using Fusion;
using UnityEngine;

namespace Chog.Arena
{
    [RequireComponent(typeof(NetworkCharacterController))]
    public sealed class ArenaHero : NetworkBehaviour
    {
        [Networked] public byte Team { get; set; }
        [Networked] public NetworkBool IsBot { get; set; }
        [Networked] public Vector3 SpawnPoint { get; set; }
        [Networked] public int Health { get; set; }
        [Networked] public float Energy { get; set; }
        [Networked] public float Special { get; set; }
        [Networked] public int ReceivedHits { get; set; }
        [Networked] public int ShotsFired { get; set; }
        [Networked] public int PrimaryCasts { get; set; }
        [Networked] public int SpecialCasts { get; set; }
        [Networked] private uint LastPrimaryRelease { get; set; }
        [Networked] private uint LastSpecialRelease { get; set; }
        [Networked] private byte BurstRemaining { get; set; }
        [Networked] private Vector3 BurstDirection { get; set; }
        [Networked] private TickTimer BurstInterval { get; set; }
        [Networked] private TickTimer AimLock { get; set; }
        [Networked] private TickTimer Respawn { get; set; }
        [Networked] private TickTimer SlowedUntil { get; set; }
        [Networked] private TickTimer BotNextAttack { get; set; }

        private NetworkCharacterController _motor;
        private static NetworkObject _projectilePrefab;

        public override void Spawned()
        {
            _motor = GetComponent<NetworkCharacterController>();
            _motor.acceleration = 45;
            _motor.braking = 25;
            if (HasStateAuthority)
            {
                Health = ArcherRules.MaxHealth;
                Energy = ArcherRules.MaxEnergy;
                Special = ArcherRules.MaxSpecial;
            }
            if (!HasStateAuthority && !HasInputAuthority) Runner.SetIsSimulated(Object, false);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority && !HasInputAuthority) return;
            if (Health <= 0)
            {
                _motor.Move(Vector3.zero);
                if (HasStateAuthority && Respawn.Expired(Runner))
                {
                    _motor.Teleport(SpawnPoint);
                    _motor.Velocity = Vector3.zero;
                    Health = ArcherRules.MaxHealth;
                    Energy = ArcherRules.MaxEnergy;
                    Respawn = TickTimer.None;
                }
                return;
            }

            _motor.maxSpeed = SlowedUntil.IsRunning && !SlowedUntil.Expired(Runner) ? 3.5f : 5f;
            if (HasStateAuthority) Energy = Mathf.Min(ArcherRules.MaxEnergy, Energy + ArcherRules.EnergyRegenPerSecond * Runner.DeltaTime);

            ArenaInput command;
            if (IsBot && HasStateAuthority) command = BotCommand();
            else if (GetInput(out ArenaInput input)) command = input.Sanitized();
            else command = default;

            _motor.Move(new Vector3(command.Move.x, 0, command.Move.y));
            if (command.Aim.sqrMagnitude > .01f && (!AimLock.IsRunning || AimLock.Expired(Runner)))
                transform.rotation = Quaternion.LookRotation(command.Aim);

            if (!HasStateAuthority) return;
            if (command.Valid)
            {
                if (command.PrimaryRelease != LastPrimaryRelease)
                {
                    LastPrimaryRelease = command.PrimaryRelease;
                    TryCastPrimary(command.Aim);
                }
                if (command.SpecialRelease != LastSpecialRelease)
                {
                    LastSpecialRelease = command.SpecialRelease;
                    TryCastSpecial(command.Aim);
                }
            }
            if (BurstRemaining > 0 && BurstInterval.ExpiredOrNotRunning(Runner)) FireBurstArrow();
        }

        private void TryCastPrimary(Vector3 aim)
        {
            if (Energy < 1 || BurstRemaining != 0 || (AimLock.IsRunning && !AimLock.Expired(Runner))) return;
            Energy -= 1;
            PrimaryCasts++;
            BurstDirection = aim.sqrMagnitude > .01f ? aim.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(BurstDirection);
            AimLock = TickTimer.CreateFromSeconds(Runner, ArcherRules.BasicAimLockSeconds);
            BurstRemaining = ArcherRules.BasicCount;
            BurstInterval = TickTimer.None;
        }

        private void FireBurstArrow()
        {
            if (_projectilePrefab == null) _projectilePrefab = Resources.Load<NetworkObject>("Chog/Projectile");
            Vector3 direction = BurstDirection.sqrMagnitude > .01f ? BurstDirection : transform.forward;
            Vector3 origin = transform.position + Vector3.up + direction * 1.1f;
            Runner.Spawn(_projectilePrefab, origin, Quaternion.LookRotation(direction),
                onBeforeSpawned: (runner, obj) =>
                {
                    var projectile = obj.GetComponent<ArenaProjectile>();
                    projectile.Team = Team;
                    projectile.Owner = Object.Id;
                    projectile.Kind = 0;
                    projectile.Damage = ArcherRules.BasicDamage;
                    projectile.Velocity = direction * ArcherRules.BasicProjectileSpeed;
                    projectile.Lifetime = TickTimer.CreateFromSeconds(runner, ArcherRules.BasicProjectileLife);
                });
            ShotsFired++;
            BurstRemaining--;
            BurstInterval = BurstRemaining > 0 ? TickTimer.CreateFromSeconds(Runner, ArcherRules.BasicInterval) : TickTimer.None;
        }

        private void TryCastSpecial(Vector3 aim)
        {
            if (Special < 1 || (AimLock.IsRunning && !AimLock.Expired(Runner))) return;
            Special -= 1;
            SpecialCasts++;
            Vector3 offset = Vector3.ClampMagnitude(aim, ArcherRules.SpecialRange);
            if (offset.sqrMagnitude < .04f) offset = transform.forward * ArcherRules.SpecialRange;
            transform.rotation = Quaternion.LookRotation(offset);
            AimLock = TickTimer.CreateFromSeconds(Runner, ArcherRules.SpecialAimLockSeconds);
            if (_projectilePrefab == null) _projectilePrefab = Resources.Load<NetworkObject>("Chog/Projectile");
            Vector3 origin = transform.position + Vector3.up;
            Vector3 target = transform.position + offset + Vector3.up;
            Runner.Spawn(_projectilePrefab, origin, Quaternion.LookRotation(offset),
                onBeforeSpawned: (runner, obj) =>
                {
                    var projectile = obj.GetComponent<ArenaProjectile>();
                    projectile.Team = Team;
                    projectile.Owner = Object.Id;
                    projectile.Kind = 1;
                    projectile.Target = target;
                    projectile.Velocity = offset.normalized * ArcherRules.SpecialProjectileSpeed;
                    projectile.Lifetime = TickTimer.CreateFromSeconds(runner, 5);
                });
        }

        private ArenaInput BotCommand()
        {
            ArenaHero target = null;
            float best = float.MaxValue;
            foreach (var candidate in FindObjectsByType<ArenaHero>(FindObjectsSortMode.None))
            {
                if (candidate == this || candidate.Runner != Runner || !candidate.HasStateAuthority ||
                    candidate.Team == Team || candidate.Health <= 0) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                target = candidate;
            }
            if (target == null) return new ArenaInput { Valid = true, Aim = transform.forward * ArcherRules.SpecialRange };
            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0;
            var direction = offset.sqrMagnitude > .01f ? offset.normalized : transform.forward;
            Vector2 move = best > 16 ? new Vector2(direction.x, direction.z) : Vector2.zero;
            var command = new ArenaInput { Valid = true, Aim = Vector3.ClampMagnitude(offset, ArcherRules.SpecialRange), Move = move,
                PrimaryRelease = LastPrimaryRelease, SpecialRelease = LastSpecialRelease };
            // The offline sparring bot lets the player initiate combat and practice both casts.
            if (ReceivedHits == 0 || (BotNextAttack.IsRunning && !BotNextAttack.Expired(Runner))) return command;
            if (best < 64 && BurstRemaining == 0 && Energy >= 1 && AimLock.ExpiredOrNotRunning(Runner))
            {
                command.PrimaryRelease++;
                BotNextAttack = TickTimer.CreateFromSeconds(Runner, 5);
            }
            else if (best < 36 && Special >= 1 && AimLock.ExpiredOrNotRunning(Runner))
            {
                command.SpecialRelease++;
                BotNextAttack = TickTimer.CreateFromSeconds(Runner, 5);
            }
            return command;
        }

        public void ApplyHit(int amount)
        {
            if (!HasStateAuthority || Health <= 0 || amount <= 0) return;
            ReceivedHits++;
            if (IsBot && ReceivedHits == 1) BotNextAttack = TickTimer.CreateFromSeconds(Runner, 5);
            Health = Mathf.Max(0, Health - amount);
            if (Health == 0)
            {
                BurstRemaining = 0;
                Respawn = TickTimer.CreateFromSeconds(Runner, 2);
            }
        }

        public void ChargeSpecial(float amount)
        {
            if (HasStateAuthority && Health > 0) Special = Mathf.Min(ArcherRules.MaxSpecial, Special + amount);
        }

        public void ApplySlow(float seconds)
        {
            if (HasStateAuthority && Health > 0) SlowedUntil = TickTimer.CreateFromSeconds(Runner, seconds);
        }
    }
}
