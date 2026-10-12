using System;
using System.Collections.Generic;
using Chog.Heroes;
using Quantum;

namespace Chog.Genesis
{
    // Runs in the lobby/backend, never in a deterministic Frame update.
    public static class GenesisLoadoutBinder
    {
        public static RuntimePlayer Bind(HeroDefinition hero, IEnumerable<GenesisCombatLoadout> loadouts)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (loadouts == null) throw new ArgumentNullException(nameof(loadouts));

            GenesisCombatLoadout selected = null;
            foreach (var candidate in loadouts)
            {
                if (candidate == null || !Matches(hero, candidate)) continue;
                if (selected != null)
                    throw new InvalidOperationException("Several combat loadouts match this Genesis profile.");
                selected = candidate;
            }
            if (selected == null)
                throw new InvalidOperationException("No combat loadout matches this Genesis profile and catalog version.");
            if (!selected.Guid.IsValid || !selected.HasCombatReferences)
                throw new InvalidOperationException("The Genesis loadout has missing combat asset references.");

            return new RuntimePlayer
            {
                PlayerNickname = hero.DisplayName,
                PlayerAvatar = selected.CharacterPrototype,
                GenesisLoadout = new AssetRef<GenesisCombatLoadout> { Id = selected.Guid },
                GenesisIdentityKey = hero.IdentityKey,
                GenesisMetadataId = hero.MetadataId,
                GenesisMetadataHash = hero.MetadataHash,
                GenesisCatalogHash = hero.CatalogHash
            };
        }

        private static bool Matches(HeroDefinition hero, GenesisCombatLoadout loadout)
        {
            return Equal(hero.RulesetVersion, loadout.RulesetVersion)
                && Equal(hero.CatalogHash, loadout.CatalogHash)
                && Equal(hero.ArchetypeId, loadout.ArchetypeId)
                && Equal(hero.BasicAttackId, loadout.BasicAttackId)
                && Equal(hero.ClassAbilityId, loadout.ClassAbilityId)
                && Equal(hero.SignatureShapeId, loadout.SignatureShapeId)
                && Equal(hero.SignatureEffectId, loadout.SignatureEffectId)
                && Equal(hero.TierExpressionId, loadout.TierExpressionId)
                && Equal(hero.UniqueId ?? "", loadout.UniqueId ?? "");
        }

        private static bool Equal(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }
    }
}
