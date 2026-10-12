namespace Quantum
{
    // Gives characters their Genesis identity. When the match config names a roster, players
    // bring their own Genesis and bots are drawn from the rest of the collection.
    public static unsafe class GenesisCast
    {
        public static GenesisRoster Roster(Frame frame)
        {
            var reference = frame.RuntimeConfig.GenesisRoster;
            return reference.IsValid ? frame.FindAsset(reference) : null;
        }

        // The Genesis a player declared, when the roster knows it and its role has a character.
        // This reads a declaration: ownership is checked before the match, not here.
        public static bool TryGetPlayerHero(Frame frame, RuntimePlayer player, out GenesisRosterEntry entry,
                                            out AssetRef<EntityPrototype> prototype)
        {
            entry = default;
            prototype = default;
            if (player == null || player.GenesisTokenId <= 0) return false;
            var roster = Roster(frame);
            if (roster == null) return false;
            var index = roster.IndexOf(player.GenesisTokenId);
            if (index < 0 || !roster.TryGetPrototype(roster.Entries[index].Role, out prototype))
            {
                Log.Warn("Genesis " + player.GenesisTokenId + " is not playable with this roster.");
                return false;
            }
            entry = roster.Entries[index];
            return true;
        }

        // Draws a Genesis that is not in the match yet, starting from a random position.
        public static bool TryDrawBotHero(Frame frame, out GenesisRosterEntry entry, out AssetRef<EntityPrototype> prototype)
        {
            entry = default;
            prototype = default;
            var roster = Roster(frame);
            if (roster == null || roster.Entries == null || roster.Entries.Length == 0) return false;

            // Test option: the Genesis named by the match config come first, in their order.
            var named = frame.RuntimeConfig.BotGenesisTokens;
            for (var i = 0; named != null && i < named.Length; i++)
            {
                var index = roster.IndexOf(named[i]);
                if (index < 0 || IsInMatch(frame, named[i])) continue;
                if (!roster.TryGetPrototype(roster.Entries[index].Role, out prototype)) continue;
                entry = roster.Entries[index];
                return true;
            }

            var count = roster.Entries.Length;
            var start = frame.RNG->Next(0, count);
            for (var offset = 0; offset < count; offset++)
            {
                var candidate = roster.Entries[(start + offset) % count];
                if (IsInMatch(frame, candidate.TokenId)) continue;
                if (!roster.TryGetPrototype(candidate.Role, out prototype)) continue;
                entry = candidate;
                return true;
            }
            prototype = default;
            return false;
        }

        // "form" is the form of the signature the hero plays: 0 the first one, 1 the second.
        // Anything else plays the first one.
        public static void Tag(Frame frame, EntityRef character, GenesisRosterEntry entry, int form = 0)
        {
            var played = (byte)(form == 1 ? 1 : 0);
            frame.Set(character, new GenesisHero
            {
                TokenId = entry.TokenId,
                MetadataId = entry.MetadataId,
                Role = entry.Role,
                Shape = entry.Shape,
                Effect = entry.Effect,
                Expression = entry.Expression,
                Tier = entry.Tier,
                Form = played
            });

            // The signature comes from the Genesis, whatever body it plays.
            var roster = Roster(frame);
            if (roster != null && roster.TryGetSignature(played, entry.Expression, entry.Shape, entry.Effect, out var signature)
                && frame.Unsafe.TryGetPointer<CharacterAttacks>(character, out var attacks))
            {
                attacks->SpecialSkillData = signature;
            }
        }

        // Name of the Genesis a character is, or null when it is not one.
        public static string DisplayName(Frame frame, EntityRef character)
        {
            if (!frame.TryGet<GenesisHero>(character, out var hero)) return null;
            var roster = Roster(frame);
            var index = roster == null ? -1 : roster.IndexOf(hero.TokenId);
            return index < 0 ? null : roster.Entries[index].Name;
        }

        // What the traits of a Genesis give it, as "class / shape / effect", or null when the
        // character is not a Genesis. For display only.
        public static string KitLabel(Frame frame, EntityRef character)
        {
            if (!frame.TryGet<GenesisHero>(character, out var hero)) return null;
            var roster = Roster(frame);
            if (roster == null) return null;
            return IdAt(roster.RoleIds, hero.Role) + " / " + roster.FormName(hero.Shape, hero.Form) + " / "
                + IdAt(roster.EffectIds, hero.Effect);
        }

        private static string IdAt(string[] ids, byte position)
        {
            return ids != null && position < ids.Length ? ids[position] : "?";
        }

        private static bool IsInMatch(Frame frame, int tokenId)
        {
            var heroes = frame.Filter<GenesisHero>();
            while (heroes.Next(out _, out var hero))
            {
                if (hero.TokenId == tokenId) return true;
            }
            return false;
        }
    }
}
