using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Chog.Editor
{
    /// <summary>
    /// The fonts of the game, all under the SIL Open Font License (the licence files are beside them):
    /// since 17 October 2026 (docs/DESIGN-BRIEF.md) Lilita One for titles and buttons, Nunito ExtraBold for
    /// sentences and Nunito Black for numbers, Koulen kept for the logo; Convergence stays for older
    /// screens. This makes the TextMesh Pro font asset of each, once, into Assets/Chog/Resources/Fonts,
    /// where the screens load them (ChogTheme), and the outlined materials of the titles and numbers:
    /// a thick dark outline and a hard shadow under the letters, as material assets so the shader keeps
    /// those features in the build. The atlas is dynamic: a letter not drawn yet is added when a text
    /// first needs it. The build calls it before building (BuildRequestWatcher).
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
            made += Make("LilitaOne-Regular.ttf", "Lilita One SDF") ? 1 : 0;
            made += Make("Nunito-ExtraBold.ttf", "Nunito ExtraBold SDF") ? 1 : 0;
            made += Make("Nunito-Black.ttf", "Nunito Black SDF") ? 1 : 0;
            made += Outlined("Lilita One SDF", "Lilita One SDF Outlined", 0.24f, -0.75f) ? 1 : 0;
            made += Outlined("Nunito Black SDF", "Nunito Black SDF Outlined", 0.20f, -0.7f) ? 1 : 0;
            made += Outlined("Koulen SDF", "Koulen SDF Outlined", 0.22f, -0.75f) ? 1 : 0;
            return made == 0 ? "fonts ready" : made + " font asset(s) made";
        }

        // The Supercell way of writing on a button or a banner: a near-black outline about an eighth of
        // the letters' height and a hard shadow straight down (docs/DESIGN-BRIEF.md).
        private static bool Outlined(string fontName, string materialName, float outline, float shadowDown)
        {
            var path = Target + "/" + materialName + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                return false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Target + "/" + fontName + ".asset");
            if (font == null)
                throw new FileNotFoundException("no font asset " + fontName);

            var material = new Material(font.material) { name = materialName };
            var ink = new Color(0.078f, 0.059f, 0.141f, 1f);
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", ink);
            material.SetFloat("_OutlineWidth", outline);
            material.SetFloat("_FaceDilate", outline * 0.5f);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", ink);
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", shadowDown);
            material.SetFloat("_UnderlayDilate", outline * 2f);
            material.SetFloat("_UnderlaySoftness", 0f);
            // The scale ratios the shader reads with those values, as TextMesh Pro's inspector sets them.
            ShaderUtilities.UpdateShaderRatios(material);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return true;
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
