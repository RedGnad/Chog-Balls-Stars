using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Chog.Editor
{
    /// <summary>
    /// The two fonts of the Chog identity (research/chog-visual-identity-2026-10-15.md): Koulen for
    /// titles, buttons and numbers, Convergence for the rest, both under the SIL Open Font License
    /// (the licence files are beside them). This makes the TextMesh Pro font asset
    /// of each, once, into Assets/Chog/Resources/Fonts, where the screens load them (ChogTheme). The
    /// atlas is dynamic: a letter not drawn yet is added when a text first needs it. The build calls
    /// it before building (BuildRequestWatcher).
    /// </summary>
    public static class ChogFontAssets
    {
        private const string Sources = "Assets/Chog/Resources/Fonts";
        private const string Target = "Assets/Chog/Resources/Fonts";

        // Drawn into the atlas from the start: what the screens write most.
        private const string Common =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz";

        /// <summary>Makes the font assets that are missing; says what it did.</summary>
        public static string Ensure()
        {
            var made = 0;
            made += Make("Koulen-Regular.ttf", "Koulen SDF") ? 1 : 0;
            made += Make("Convergence-Regular.ttf", "Convergence SDF") ? 1 : 0;
            return made == 0 ? "fonts ready" : made + " font asset(s) made";
        }

        private static bool Make(string file, string name)
        {
            var path = Target + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null)
                return false;

            var font = AssetDatabase.LoadAssetAtPath<Font>(Sources + "/" + file);
            if (font == null)
                throw new FileNotFoundException("no font at " + Sources + "/" + file);

            Directory.CreateDirectory(Target);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            // The atlas and the material live inside the font asset, as TextMesh Pro's own menu does it.
            asset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.TryAddCharacters(Common, out _);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
