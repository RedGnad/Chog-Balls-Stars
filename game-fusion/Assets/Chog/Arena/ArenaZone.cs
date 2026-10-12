using Fusion;
using UnityEngine;

namespace Chog.Arena
{
    // Server-owned area attack left by the Archer's ballistic special.
    public sealed class ArenaZone : NetworkBehaviour
    {
        [Networked] public byte Team { get; set; }
        [Networked] public NetworkId Owner { get; set; }
        [Networked] public int PulsesRemaining { get; set; }
        [Networked] public TickTimer Lifetime { get; set; }
        [Networked] private TickTimer NextPulse { get; set; }

        public override void Spawned()
        {
            if (!HasStateAuthority) Runner.SetIsSimulated(Object, false);
            if (Application.isBatchMode) return;
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = new Color(1, .32f, .7f, .85f);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (Lifetime.Expired(Runner)) { Runner.Despawn(Object); return; }
            if (PulsesRemaining <= 0 || !NextPulse.ExpiredOrNotRunning(Runner)) return;
            bool hit = false;
            foreach (var hero in FindObjectsByType<ArenaHero>(FindObjectsSortMode.None))
            {
                if (hero.Runner != Runner || !hero.HasStateAuthority || hero.Team == Team || hero.Health <= 0) continue;
                var delta = hero.transform.position - transform.position;
                delta.y = 0;
                if (delta.sqrMagnitude > ArcherRules.SpecialRadius * ArcherRules.SpecialRadius) continue;
                hero.ApplyHit(ArcherRules.SpecialDamage);
                hero.ApplySlow(ArcherRules.SpecialSlowSeconds);
                hit = true;
            }
            if (hit && Runner.TryFindObject(Owner, out var owner))
                owner.GetComponent<ArenaHero>()?.ChargeSpecial(ArcherRules.SpecialChargePerZonePulse);
            PulsesRemaining--;
            NextPulse = TickTimer.CreateFromSeconds(Runner, ArcherRules.SpecialInterval);
        }
    }
}
