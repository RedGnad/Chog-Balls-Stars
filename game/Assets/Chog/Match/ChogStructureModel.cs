using Chog.Diagnostics;
using Quantum;
using UnityEngine;

namespace Chog.Match
{
    /// <summary>
    /// The tower and the core in 3D (tools/blender/make_chog_structures.py): a totem with the head of a
    /// Chog whose laser eyes the tower fires with, and a heart held in a crown of Chog leaves. Read from
    /// the library "ChogStructures", made like the library of the heroes' parts: four records, each
    /// structure in the colour of each side. A page shows them with "structures3d=1" until the owner of
    /// the game says yes to them; StructureViews draws the flat pictures otherwise.
    /// </summary>
    public static class ChogStructureModel
    {
        public const string LibraryResource = "ChogStructures";
        public const string QueryParameter = "structures3d";
        public const int TowerOurs = 1, TowerTheirs = 2, CoreOurs = 3, CoreTheirs = 4;

        /// <summary>The parts the game moves: the head of the tower, where its beam starts, the heart.</summary>
        public const string HeadPart = "Head", EyesPart = "Eyes", HeartPart = "Heart";

        private static ChogPartsLibrary s_library;
        private static bool s_loaded;
        private static bool? s_asked;

        /// <summary>Whether this page draws the structures in 3D: asked by its address, and the library read.</summary>
        public static bool On
        {
            get
            {
                s_asked ??= PerformanceSampling.TryGetQueryValue(Application.absoluteURL, QueryParameter, out var value)
                            && value == "1";
                return s_asked.Value && Library != null;
            }
        }

        /// <summary>The library of the structures, read once; null when the project has none.</summary>
        public static ChogPartsLibrary Library
        {
            get
            {
                if (s_loaded)
                    return s_library;
                s_loaded = true;
                var file = Resources.Load<TextAsset>(LibraryResource);
                if (file == null)
                    return null;
                try
                {
                    s_library = ChogPartsLibrary.Read(file.bytes);
                    Debug.Log($"CHOG_STRUCTURES3D library: {s_library.Heroes.Count} models, {s_library.Pieces.Length} parts");
                }
                catch (System.Exception problem)
                {
                    Debug.LogWarning("CHOG_STRUCTURES3D the library of the structures cannot be read: " + problem.Message);
                }
                Resources.UnloadAsset(file);
                return s_library;
            }
        }

        /// <summary>The record of a structure for one side.</summary>
        public static int RecordOf(EStructureKind kind, bool ours)
        {
            if (kind == EStructureKind.Core)
                return ours ? CoreOurs : CoreTheirs;
            return ours ? TowerOurs : TowerTheirs;
        }

        /// <summary>The model under "parent", facing +Z; a ruin in greys. Null without the library.</summary>
        public static Transform Build(EStructureKind kind, bool ours, bool ruined, Transform parent)
        {
            return ChogHeroModel.Build(Library, RecordOf(kind, ours), parent, ruined);
        }
    }
}
