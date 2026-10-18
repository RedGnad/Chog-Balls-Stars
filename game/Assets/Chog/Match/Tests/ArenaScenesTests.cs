using NUnit.Framework;
using Quantum;

namespace Chog.Match.Tests
{
    public sealed class ArenaScenesTests
    {
        [Test]
        public void BothArenasAreRecognisedAndTheMenuIsNot()
        {
            Assert.That(ArenaScenes.IsArena("Gameplay"), Is.True);
            Assert.That(ArenaScenes.IsArena("GameplaySiege"), Is.True);
            Assert.That(ArenaScenes.IsArena("QuantumSampleMenu"), Is.False);
            Assert.That(ArenaScenes.IsArena(""), Is.False);
            Assert.That(ArenaScenes.IsArena(null), Is.False);
        }

        [Test]
        public void TheSiegePlaysOnItsOwnMapAndTheCoinsOnTheSamples()
        {
            Assert.That(ArenaScenes.MapPathOf(EMatchMode.Siege), Is.EqualTo("QuantumUser/Resources/SiegeMap"));
            Assert.That(ArenaScenes.MapPathOf(EMatchMode.Coins), Is.EqualTo("QuantumUser/Resources/Map1"));
            Assert.That(ArenaScenes.MapPathOf(EMatchMode.TowersAndCoins), Is.EqualTo("QuantumUser/Resources/Map1"));
        }
    }
}
