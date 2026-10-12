using NUnit.Framework;
using Quantum;
using UnityEngine;

namespace Chog.Match.Tests
{
    public sealed class GenesisEyesTests
    {
        private static readonly GenesisEyeLook Heart = new GenesisEyeLook { Outer = new Color(0.9f, 0.12f, 0.15f) };
        private static readonly GenesisEyeLook DarkEyes = new GenesisEyeLook { Outer = new Color(0.08f, 0.08f, 0.1f) };

        [Test]
        public void AnEyeOfNearlyTheColourOfTheSkinGetsALighterSkin()
        {
            var red = new Color(0.95f, 0.05f, 0.05f, 1f);
            var backing = GenesisEyes.Backing(red, Heart);
            Assert.Greater(backing.g, 0.5f, "a red heart on red skin must be drawn on a lighter red");
            Assert.Greater(backing.r, 0.9f);
        }

        [Test]
        public void ASkinTheEyeStandsOutOnIsKept()
        {
            // The brown face of a Black skin under black eyes, as the art draws it.
            var brown = new Color(0.37f, 0.19f, 0.13f, 1f);
            Assert.AreEqual(brown, GenesisEyes.Backing(brown, DarkEyes));
            Assert.AreEqual(brown, GenesisEyes.Backing(brown, Heart));
        }

        [Test]
        public void AnUnknownSkinIsAPlainFace()
        {
            var backing = GenesisEyes.Backing(Color.clear, DarkEyes);
            Assert.AreEqual(1f, backing.a);
            Assert.Greater(backing.r, 0.8f);
        }
    }
}
