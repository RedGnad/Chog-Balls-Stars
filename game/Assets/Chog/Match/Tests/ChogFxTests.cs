using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Chog.Match.Tests
{
    /// <summary>
    /// The effects of a fight read from the traits (ChogFxRecipes), how an effect takes a colour (FxColours),
    /// and the shapes of the faked lights and bolts (ChogFxMeshes), without the engine.
    /// </summary>
    public sealed class ChogFxTests
    {
        private const string LibraryPath = "game/Assets/Chog/Resources/ChogParts.bytes";
        private const string EffectsFolder = "game/Assets/Chog/Effects/Resources/ChogFx";

        // The objects whose impact is only the burst of pieces and a pop (no family of their own yet).
        private static readonly string[] Generic = { "RamHorns", "DemonHorns", "TapedBanana", "Brain" };

        [Test]
        public void EveryObjectAHatThrowsHasItsOwnImpact()
        {
            var library = ChogPartsLibrary.Read(File.ReadAllBytes(Find(LibraryPath)));
            var fallback = ChogFxRecipes.ForAttack("Attack_NothingLikeIt").Words;
            var thrown = library.Heroes.Values.Where(hero => hero.Attack != ChogPartsLibrary.NoAttack)
                .Select(hero => library.Pieces[hero.Attack].Name).Distinct().ToList();
            Assert.That(thrown.Count, Is.GreaterThan(30), "the library throws fewer objects than expected");
            var missing = thrown.Where(piece => ChogFxRecipes.ForAttack(piece).Words == fallback
                                                && !Generic.Contains(ChogFxRecipes.Key(piece))).ToList();
            Assert.That(missing, Is.Empty, "objects with only the generic impact: " + string.Join(", ", missing));
        }

        [Test]
        public void ImpactsShowTheirObject()
        {
            // Coins pop into coins, a money bag into dollar coins, a cap into cloth, a Thunder Cap calls lightning.
            Assert.AreEqual(ImpactPieces.Copies, ChogFxRecipes.ForAttack("Attack_CoinBitcoin").Pieces);
            Assert.AreEqual("Attack_CoinDollar", ChogFxRecipes.ForAttack("Attack_MoneyBag").CopiesOf);
            Assert.AreEqual(ImpactPieces.Flakes, ChogFxRecipes.ForAttack("Hat_Cap").Pieces);
            Assert.IsTrue(ChogFxRecipes.ForAttack("Attack_Bolt").Lightning);
            Assert.AreEqual(ChogFx.Ice, ChogFxRecipes.ForAttack("Attack_Snowball").Effect.Effect);
            Assert.IsTrue(ChogFxRecipes.ForAttack("Attack_RainbowBall").Rainbow);
            Assert.AreEqual("Snowball", ChogFxRecipes.Key("Attack_Snowball"));
            Assert.AreEqual("", ChogFxRecipes.Key(null));
        }

        [Test]
        public void EveryAuraHasAHitAndOnlyLastingEffectsLast()
        {
            foreach (Quantum.GenesisEffectIcon icon in Enum.GetValues(typeof(Quantum.GenesisEffectIcon)))
            {
                var recipe = ChogFxRecipes.ForAura(icon);
                Assert.IsTrue(recipe.Hit.On, icon + " has no effect on its target");
                Assert.IsFalse(string.IsNullOrEmpty(recipe.Words), icon + " says nothing");
            }
            var lasting = Enum.GetValues(typeof(Quantum.GenesisEffectIcon)).Cast<Quantum.GenesisEffectIcon>()
                .Where(icon => ChogFxRecipes.ForAura(icon).Lasting.On).ToArray();
            CollectionAssert.AreEquivalent(new[] { Quantum.GenesisEffectIcon.Flame, Quantum.GenesisEffectIcon.Star, Quantum.GenesisEffectIcon.Bubbles }, lasting);
            Assert.IsTrue(ChogFxRecipes.ForAura(Quantum.GenesisEffectIcon.Bolt).Lightning);
        }

        [Test]
        public void EveryDeliveryOfTheRosterIsKnown()
        {
            foreach (var delivery in new[] { "direct", "echo", "return", "persistent-zone", "inversion" })
                Assert.IsFalse(string.IsNullOrEmpty(ChogFxRecipes.ForDelivery(delivery).Words), delivery);
            Assert.AreEqual(2, ChogFxRecipes.ForDelivery("echo").Rings);
            Assert.Greater(ChogFxRecipes.ForDelivery("inversion").Flash.a, 0f);
        }

        [Test]
        public void EveryEffectARecipeNamesIsInTheProject()
        {
            var folder = Find(EffectsFolder);
            foreach (ChogFx effect in Enum.GetValues(typeof(ChogFx)))
                Assert.IsTrue(File.Exists(Path.Combine(folder, effect + ".prefab")), effect + " has no prefab in " + EffectsFolder);
        }

        [Test]
        public void ColoursTintPaintOrStay()
        {
            var authored = new Color(1f, 0.5f, 0.25f, 0.8f);
            var blue = new Color(0.2f, 0.4f, 1f, 1f);
            Assert.AreEqual(authored, FxColours.Mix(authored, blue, FxColour.Authored));
            var tinted = FxColours.Mix(authored, blue, FxColour.Tinted);
            Assert.AreEqual(0.2f, tinted.r, 1e-5f);
            Assert.AreEqual(0.8f, tinted.a, 1e-5f);
            var painted = FxColours.Mix(new Color(1f, 1f, 1f, 0.5f), blue, FxColour.Painted);
            Assert.AreEqual(blue.b, painted.b, 1e-5f, "white painted takes the colour whole");
            Assert.AreEqual(0.5f, painted.a, 1e-5f);
            var dark = FxColours.Mix(new Color(0.1f, 0.1f, 0.1f, 1f), blue, FxColour.Painted);
            Assert.Less(dark.b, painted.b, "a dark part stays darker");
            Assert.AreEqual(1f, FxColours.Lighter(Color.black, 1f).g, 1e-5f);
        }

        [Test]
        public void AFlareIsWhiteHotInItsMiddleAndGoneAtItsEdge()
        {
            var plain = ChogFxMeshes.FlareDraft(Color.red, 0);
            Assert.AreEqual(1f, plain.Colours[0].a, 1e-5f);
            Assert.Greater(plain.Colours[0].g, 0.5f, "the middle is near white");
            var rim = plain.Vertices.Select((point, index) => (point, index)).Where(v => Mathf.Abs(v.point.magnitude - 1f) < 1e-4f).ToList();
            Assert.IsNotEmpty(rim);
            Assert.IsTrue(rim.All(v => plain.Colours[v.index].a == 0f), "the edge fades to nothing");
            var rays = ChogFxMeshes.FlareDraft(Color.red, 6);
            Assert.AreEqual(plain.VertexCount + 6 * 4, rays.VertexCount);
            Assert.AreEqual(ChogFxMeshes.RayLength, rays.Vertices.Max(point => point.magnitude), 1e-4f);
            Assert.IsTrue(rays.Vertices.All(point => Mathf.Abs(point.z) < 1e-6f), "a flare is flat, facing +Z");
        }

        [Test]
        public void ARingIsBrightestOnItsRadius()
        {
            var ring = ChogFxMeshes.RingDraft(Color.cyan, 0.25f);
            var brightest = Enumerable.Range(0, ring.VertexCount).OrderByDescending(index => ring.Colours[index].a).First();
            Assert.AreEqual(1f, new Vector2(ring.Vertices[brightest].x, ring.Vertices[brightest].z).magnitude, 1e-4f);
            Assert.IsTrue(ring.Vertices.All(point => point.y == 0f), "a ring lies on the floor");
        }

        [Test]
        public void ABoltStartsAndLandsWhereItIsAimed()
        {
            var path = new List<Vector3>();
            var from = new Vector3(0f, 7f, 0f);
            var to = new Vector3(1f, 0f, 2f);
            ChogFxMeshes.BoltPath(path, from, to, 10, 0.5f, new System.Random(3));
            Assert.AreEqual(11, path.Count);
            Assert.AreEqual(from, path[0]);
            Assert.AreEqual(to, path[10]);
            for (var i = 1; i < 10; i++)
            {
                var straight = Vector3.Lerp(from, to, i / 10f);
                Assert.LessOrEqual(Vector3.Distance(straight, path[i]), 0.5f * 1.2f, "a point strays too far");
            }
            var draft = new EffectMeshDraft();
            ChogFxMeshes.Ribbon(draft, path, Vector3.back, 0.2f, Color.yellow, 1f);
            Assert.AreEqual(path.Count * 3, draft.VertexCount);
            Assert.AreEqual((path.Count - 1) * 4 * 3, draft.Triangles.Count);
            draft.Clear();
            Assert.AreEqual(0, draft.VertexCount);
        }

        // The repository, found upwards from where the tests run.
        private static string Find(string relative)
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (var folder = new DirectoryInfo(start); folder != null; folder = folder.Parent)
                {
                    var path = Path.Combine(folder.FullName, relative);
                    if (File.Exists(path) || Directory.Exists(path))
                        return path;
                }
            }
            throw new FileNotFoundException("no " + relative + " above the folder the tests run in");
        }
    }
}
