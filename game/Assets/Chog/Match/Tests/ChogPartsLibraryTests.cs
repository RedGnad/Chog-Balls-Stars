using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Chog.Match.Tests
{
    /// <summary>
    /// The library of 3D hero parts the game ships (Resources/ChogParts.bytes, written by
    /// tools/blender/export_chog_parts.py), read as the game reads it.
    /// </summary>
    public sealed class ChogPartsLibraryTests
    {
        private const string LibraryPath = "game/Assets/Chog/Resources/ChogParts.bytes";
        private const int OriginGenesis = 1869;
        private const int TriangleLimit = 8000;

        private static ChogPartsLibrary s_library;

        private static ChogPartsLibrary Library => s_library ??= ChogPartsLibrary.Read(File.ReadAllBytes(Find(LibraryPath)));

        // The repository, found upwards from where the tests run.
        private static string Find(string relative)
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (var folder = new DirectoryInfo(start); folder != null; folder = folder.Parent)
                {
                    var path = Path.Combine(folder.FullName, relative);
                    if (File.Exists(path))
                        return path;
                }
            }

            throw new FileNotFoundException("no " + relative + " above the folder the tests run in");
        }

        [Test]
        public void EveryGenesisOfTheBaseOriginAndTheRecruitHaveAHero()
        {
            Assert.AreEqual(OriginGenesis + 1, Library.Heroes.Count);
            Assert.IsTrue(Library.TryGetHero(ChogHeroModel.Recruit, out var recruit));
            Assert.AreEqual("Recruit", Library.Skins[recruit.Skin].Name);
            Assert.AreEqual(4, recruit.Pieces.Length, "the recruit is the bare Chog: body, head, two arms");
        }

        [Test]
        public void EveryHeroHasItsFourPartsOnceAndStaysUnderTheTriangleLimit()
        {
            foreach (var pair in Library.Heroes)
            {
                var worn = pair.Value.Pieces.Select(index => Library.Pieces[index]).ToList();
                foreach (var rest in new[] { "Body", "Head", "armLeft", "armRight" })
                    Assert.AreEqual(1, worn.Count(piece => piece.Name == rest && piece.On == rest), $"CHOG #{pair.Key} {rest}");
                Assert.IsTrue(worn.All(piece => Library.Rest.Any(rest => rest.Name == piece.On)), $"CHOG #{pair.Key}");
                var triangles = worn.Sum(piece => piece.Indices.Length / 3);
                Assert.LessOrEqual(triangles, TriangleLimit, $"CHOG #{pair.Key}");
            }
        }

        [Test]
        public void TheOwnersGenesisWearsItsCapOnARedSkin()
        {
            Assert.IsTrue(Library.TryGetHero(1287, out var hero));
            Assert.AreEqual("Red", Library.Skins[hero.Skin].Name);
            var names = hero.Pieces.Select(index => Library.Pieces[index].Name).ToList();
            Assert.That(names, Has.Some.StartsWith("Hat_Cap"));
            Assert.That(names, Has.Some.StartsWith("Eyes_"));
            Assert.That(names, Has.Some.StartsWith("Mouth_"));
        }

        [Test]
        public void TheHeroStandsOnItsFeetLooksAlongZAndHasItsLeftArmOnMinusX()
        {
            var rest = Library.Rest.ToDictionary(part => part.Name);
            Assert.AreEqual("Body", Library.Rest[0].Name, "the body comes first: the others hang on it");
            Assert.Greater(rest["Head"].Y, 0.4f);
            Assert.Less(rest["armLeft"].X, 0f);
            Assert.Greater(rest["armRight"].X, 0f);

            // The cream face is at the front of the head.
            var head = Library.Pieces.First(piece => piece.Name == "Head");
            var face = Enumerable.Range(0, head.VertexCount)
                .Where(v => Library.Materials[head.Materials[v]].Role == ChogPartsLibrary.RoleFace).ToList();
            Assert.IsNotEmpty(face);
            Assert.Greater(face.Average(v => head.Positions[v * 3 + 2]), 0.05f);

            // No part goes under the ground.
            var lowest = Library.Pieces.Where(piece => piece.On == "Body")
                .Min(piece => Enumerable.Range(0, piece.VertexCount).Min(v => piece.Positions[v * 3 + 1]));
            Assert.GreaterOrEqual(lowest, -0.001f);
        }

        [Test]
        public void TheOutlineIsDrawnFromInsideAndTheSkinTakesTheGenesisColour()
        {
            var outline = Library.Materials.Single(material => material.Name == "ChogOutline");
            Assert.AreEqual(ChogPartsLibrary.LightOutline, outline.Light);
            Assert.AreEqual(0.2f, ChogPartsLibrary.LightOf(outline.Light));

            Assert.IsTrue(Library.TryGetHero(1287, out var hero));
            var skin = Array.FindIndex(Library.Materials, material => material.Role == ChogPartsLibrary.RoleSkin);
            Library.ColourOf((ushort)skin, hero.Skin, out var r, out var g, out var b, out var a);
            var red = Library.Skins[hero.Skin];
            Assert.AreEqual(red.SkinR, r);
            Assert.AreEqual(red.SkinG, g);
            Assert.AreEqual(red.SkinB, b);
            Assert.AreEqual(1f, a);
        }

        [Test]
        public void AFileOfAnotherLayoutIsRefused()
        {
            Assert.Throws<InvalidDataException>(() => ChogPartsLibrary.Read(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }));
            Assert.Throws<InvalidDataException>(() => ChogPartsLibrary.Read(null));
        }

        [Test]
        public void TheViewerTakesTheNumbersAskedOrItsOwnSet()
        {
            CollectionAssert.AreEqual(new[] { 1287, 14 }, ChogHeroViewer.Asked("1287,14"));
            Assert.Greater(ChogHeroViewer.Asked("1").Length, 4);
            Assert.Greater(ChogHeroViewer.Asked("").Length, 4);
        }
    }
}
