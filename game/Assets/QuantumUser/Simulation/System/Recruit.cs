namespace Quantum
{
    // A player who enters a Genesis match without a Genesis plays a recruit: the plain character
    // of the class they pick, with the special of that class, and a little less health. Bots are
    // always Genesis, so a recruit is always a human player.
    public static unsafe class Recruit
    {
        // Percentage of the class's health a recruit gets. A first number, not balanced.
        public const int HealthPercent = 85;

        // Call before the first spawn: a spawn fills the health from these two values.
        public static void Apply(Frame frame, EntityRef character)
        {
            if (GenesisCast.Roster(frame) == null) return;
            if (!frame.Unsafe.TryGetPointer<Attributes>(character, out var attributes)) return;
            var data = frame.ResolveDictionary(attributes->DataDictionary);
            if (!data.TryGetValuePointer(EAttributeType.Health, out var health)) return;
            health->MaxValue = health->MaxValue * HealthPercent / 100;
            health->InitialValue = health->InitialValue * HealthPercent / 100;
        }

        // Whether a character is a recruit: a player's character without a Genesis, in a match
        // that has a roster.
        public static bool Is(Frame frame, EntityRef character)
        {
            // Every character carries the Bot component; it is active on bots only.
            return GenesisCast.Roster(frame) != null
                && frame.Has<PlayerLink>(character)
                && !frame.Has<GenesisHero>(character)
                && !(frame.TryGet<Bot>(character, out var bot) && bot.IsActive);
        }
    }
}
