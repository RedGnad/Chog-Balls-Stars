using NUnit.Framework;

namespace Chog.Match.Tests
{
    public sealed class LobArcTests
    {
        [Test]
        public void ALobLeavesAndLandsAtTheHeightItWasThrownFromAndTopsOutHalfWay()
        {
            var height = ChogAttackLook.LobHeight(6f);
            Assert.That(ChogAttackLook.LobRise(height, 0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(ChogAttackLook.LobRise(height, 1f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(ChogAttackLook.LobRise(height, 0.5f), Is.EqualTo(height).Within(1e-5f));
            Assert.That(ChogAttackLook.LobRise(height, 0.25f), Is.LessThan(height));
        }

        [Test]
        public void AFartherLobFliesHigherSoItNeverReadsAsAStraightShot()
        {
            Assert.That(ChogAttackLook.LobHeight(8f), Is.GreaterThan(ChogAttackLook.LobHeight(2f)));
            // Even a short throw rises well above the hands of a hero (0.9).
            Assert.That(ChogAttackLook.LobHeight(0f), Is.GreaterThan(1.5f));
        }
    }
}
