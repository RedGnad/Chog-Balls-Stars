using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Quantum;

namespace Chog.Match.Tests
{
    /// <summary>
    /// The library of the 3D tower and core (Resources/ChogStructures.bytes, written by
    /// tools/blender/make_chog_structures.py), read as the game reads it.
    /// </summary>
    public sealed class ChogStructureModelTests
    {
        private const string LibraryPath = "game/Assets/Chog/Resources/ChogStructures.bytes";

        private static ChogPartsLibrary s_library;

        private static ChogPartsLibrary Library => s_library ??= ChogPartsLibrary.Read(File.ReadAllBytes(Find(LibraryPath)));

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
        public void EachStructureOfEachSideHasItsModel()
        {
            foreach (var kind in new[] { EStructureKind.Tower, EStructureKind.Core })
            foreach (var ours in new[] { true, false })
                Assert.IsTrue(Library.TryGetHero(ChogStructureModel.RecordOf(kind, ours), out _), kind + " ours=" + ours);
            Assert.AreNotEqual(ChogStructureModel.RecordOf(EStructureKind.Tower, true), ChogStructureModel.RecordOf(EStructureKind.Tower, false));
        }

        [Test]
        public void TheTowerHasAHeadAndEyesAndTheCoreAHeart()
        {
            var rest = Library.Rest.Select(part => part.Name).ToArray();
            CollectionAssert.Contains(rest, ChogStructureModel.HeadPart);
            CollectionAssert.Contains(rest, ChogStructureModel.EyesPart);
            CollectionAssert.Contains(rest, ChogStructureModel.HeartPart);

            Library.TryGetHero(ChogStructureModel.TowerOurs, out var tower);
            Library.TryGetHero(ChogStructureModel.CoreOurs, out var core);
            Assert.IsTrue(tower.Pieces.Any(index => Library.Pieces[index].On == ChogStructureModel.HeadPart), "the head of the tower");
            Assert.IsTrue(core.Pieces.Any(index => Library.Pieces[index].On == ChogStructureModel.HeartPart), "the heart of the core");
            Assert.IsFalse(core.Pieces.Any(index => Library.Pieces[index].On == ChogStructureModel.HeadPart), "a core has no head");
        }

        [Test]
        public void TheTwoSidesDifferOnlyByTheColourOfTheirSkin()
        {
            Library.TryGetHero(ChogStructureModel.TowerOurs, out var ours);
            Library.TryGetHero(ChogStructureModel.TowerTheirs, out var theirs);
            CollectionAssert.AreEqual(ours.Pieces, theirs.Pieces);
            Assert.AreNotEqual(ours.Skin, theirs.Skin);
            var blue = Library.Skins[ours.Skin];
            var red = Library.Skins[theirs.Skin];
            Assert.Greater(blue.SkinB, blue.SkinR, "our side is blue, as the bars and the alarms of the game");
            Assert.Greater(red.SkinR, red.SkinB, "the other side is red");
        }

        [Test]
        public void AStructureStaysWithinWhatAPhoneDrawsEasily()
        {
            foreach (var record in Library.Heroes.Keys)
            {
                Library.TryGetHero(record, out var model);
                var triangles = model.Pieces.Sum(index => Library.Pieces[index].Indices.Length / 3);
                Assert.Less(triangles, 6000, "record " + record);
            }
        }
    }
}
