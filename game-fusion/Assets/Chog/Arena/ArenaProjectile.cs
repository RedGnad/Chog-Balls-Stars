using Fusion;
using UnityEngine;

namespace Chog.Arena
{
    public sealed class ArenaProjectile : NetworkBehaviour
    {
        [Networked] public byte Team { get; set; }
        [Networked] public byte Kind { get; set; } // 0: basic arrow; 1: ballistic zone delivery.
        [Networked] public NetworkId Owner { get; set; }
        [Networked] public int Damage { get; set; }
        [Networked] public Vector3 Velocity { get; set; }
        [Networked] public Vector3 Target { get; set; }
        [Networked] public TickTimer Lifetime { get; set; }
        private static NetworkObject _zonePrefab;

        public override void Spawned()
        {
            if (!HasStateAuthority) Runner.SetIsSimulated(Object, false);
            if (Application.isBatchMode) return;
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = Kind == 1 ? new Color(1, .35f, .95f) : new Color(1, .85f, .24f);
            transform.localScale = Vector3.one * (Kind == 1 ? .45f : .18f);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (Lifetime.Expired(Runner)) { Runner.Despawn(Object); return; }
            Vector3 step = Velocity * Runner.DeltaTime;
            if (Kind == 1)
            {
                if (step.sqrMagnitude < .0001f || Vector3.Distance(transform.position, Target) <= step.magnitude)
                {
                    if (_zonePrefab == null) _zonePrefab = Resources.Load<NetworkObject>("Chog/Zone");
                    var center = new Vector3(Target.x, .06f, Target.z);
                    Runner.Spawn(_zonePrefab, center, Quaternion.identity, onBeforeSpawned: (runner, obj) =>
                    {
                        var zone = obj.GetComponent<ArenaZone>();
                        zone.Team = Team;
                        zone.Owner = Owner;
                        zone.PulsesRemaining = ArcherRules.SpecialPulses;
                        zone.Lifetime = TickTimer.CreateFromSeconds(runner, ArcherRules.SpecialPulses * ArcherRules.SpecialInterval);
                    });
                    Runner.Despawn(Object);
                    return;
                }
                transform.position += step;
                return;
            }
            if (Runner.GetPhysicsScene().SphereCast(transform.position, .12f, step.normalized,
                out var hit, step.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                var hero = hit.collider.GetComponentInParent<ArenaHero>();
                if (hero != null && hero.Team != Team)
                {
                    hero.ApplyHit(Damage);
                    if (Runner.TryFindObject(Owner, out var owner))
                        owner.GetComponent<ArenaHero>()?.ChargeSpecial(ArcherRules.SpecialChargePerArrowHit);
                }
                Runner.Despawn(Object);
                return;
            }
            transform.position += step;
        }
    }
}
