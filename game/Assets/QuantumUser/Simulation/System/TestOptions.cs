using Photon.Deterministic;

namespace Quantum
{
    public partial class RuntimeConfig
    {
        // Test option: every character enters the match with its special fully charged.
        public bool StartWithFullSpecial;

        // Test option: the bots are these Genesis, in this order, before any drawn at random.
        public int[] BotGenesisTokens;
    }

    // Options that only exist to make a match quicker to test. They are part of the match
    // config, so every client simulates the same thing.
    public static unsafe class TestOptions
    {
        public static void ApplyToNewCharacter(Frame frame, EntityRef character)
        {
            if (frame.RuntimeConfig.StartWithFullSpecial)
            {
                AttributesHelper.ChangeAttribute(frame, character, EAttributeType.Special,
                    EModifierAppliance.OneTime, EModifierOperation.Add, FP._1, 0);
            }
        }
    }
}
