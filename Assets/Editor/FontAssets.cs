using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Pathweaver.EditorTools
{
    /// <summary>
    /// Builds the one font asset the game uses, from the command line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every interface element in the game was generated geometry, because there was no font. That
    /// kept the toolchain small and forced a wordless iconography, but it also meant nothing on
    /// screen could explain itself — which is why the World Atlas had to be withheld from the closed
    /// test rather than shipped unexplained.
    /// </para>
    /// <para>
    /// The asset is generated rather than committed by hand for the same reason
    /// <see cref="ProjectBootstrap"/> exists: a signed-distance-field atlas built by clicking through
    /// the Font Asset Creator is a sequence of remembered settings, and the settings are what decide
    /// whether small text is legible on a phone.
    /// </para>
    /// </remarks>
    internal static class FontAssets
    {
        /// <summary>
        /// Where <c>scripts/import-tmp-resources.py</c> puts TextMesh Pro's shaders and settings.
        /// </summary>
        /// <remarks>
        /// Imported by a script rather than from here because
        /// <c>TMP_PackageResourceImporter.ImportResources</c> queues the import instead of performing
        /// it, so in <c>-batchmode -quit</c> the Editor exits before anything lands.
        /// </remarks>
        private const string EssentialResourcesFolder = "Assets/TextMesh Pro";

        private const string FontFolder = "Assets/Fonts";
        private const string SourceFontPath = FontFolder + "/VarelaRound-Regular.ttf";
        private const string FontAssetPath = FontFolder + "/VarelaRound SDF.asset";

        /// <summary>
        /// The size each glyph is rendered at before being distance-field encoded.
        /// </summary>
        /// <remarks>
        /// Larger is sharper when scaled up and costs atlas area. TMP's own default of 90 does not
        /// fit the whole of Latin-1 into a single 1024 page — it silently dropped the last seven
        /// characters, which is why <see cref="Build"/> now treats a dropped glyph as a failure. 64
        /// fits all 191 with room to spare, and the game's largest text is a help-screen heading.
        /// </remarks>
        private const int SamplingPointSize = 64;

        /// <summary>
        /// Padding around each glyph, in atlas pixels.
        /// </summary>
        /// <remarks>
        /// This is the distance field's range. Too little and outlines and soft shadows clip against
        /// the glyph's own edge; a tenth of the sampling size is the usual ratio.
        /// </remarks>
        private const int AtlasPadding = 6;

        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;

        /// <summary>
        /// Imports TextMesh Pro's essential resources and bakes the font atlas.
        /// </summary>
        /// <remarks>
        /// Run with:
        /// <code>
        /// unity -batchmode -quit -projectPath . \
        ///   -executeMethod Pathweaver.EditorTools.FontAssets.Build -logFile /tmp/unity.log
        /// </code>
        /// </remarks>
        internal static void Build()
        {
            if (!AssetDatabase.IsValidFolder(EssentialResourcesFolder))
            {
                Fail(
                    $"No {EssentialResourcesFolder}. Run ./scripts/import-tmp-resources.py first — "
                    + "TextMesh Pro cannot build a font asset without its settings and shaders.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                Fail($"No font at {SourceFontPath}.");
                return;
            }

            // Created dynamic, populated, then frozen. A static asset refuses to add glyphs, and a
            // dynamic one shipped in a build would rasterise glyphs on the device at first sight —
            // a stutter on the frame a number changes, for a vocabulary that is known at build time.
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasWidth,
                AtlasHeight,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: false);

            if (fontAsset == null)
            {
                Fail($"TextMesh Pro could not build a font asset from {SourceFontPath}.");
                return;
            }

            fontAsset.name = "VarelaRound SDF";

            if (!fontAsset.TryAddCharacters(Characters(), out var missing))
            {
                // Fatal, because the failure is silent otherwise: a dropped glyph renders as a box on
                // a player's phone and nothing here would have said so. Varela Round covers the whole
                // of Latin-1, so anything missing means the glyphs did not fit the atlas rather than
                // that the font lacks them — the fix is a smaller SamplingPointSize, never a second
                // atlas page, which would cost a draw call on every line of text.
                Fail(
                    $"these characters did not fit the {AtlasWidth}x{AtlasHeight} atlas at "
                    + $"{SamplingPointSize}pt: {missing}");
                return;
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.ReadFontAssetDefinition();

            Persist(fontAsset);
            MakeDefault(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[fonts] built {FontAssetPath} with {fontAsset.characterTable.Count} characters "
                + $"across {fontAsset.atlasTextures.Length} atlas page(s)");
        }

        /// <summary>
        /// The punctuation the game's own writing uses that Latin-1 does not contain.
        /// </summary>
        /// <remarks>
        /// Found by rendering a preview sheet: <c>biome1-01 — First Waters</c> came out as
        /// <c>biome1-01   First Waters</c>, because an em dash is U+2014 and the atlas stopped at
        /// U+00FF. The em dash is used constantly in this project's prose, so the gap would have
        /// reached a player. Seven glyphs is a negligible cost against finding this on a phone.
        /// </remarks>
        private const string TypographicPunctuation = "–—‘’“”…";

        /// <summary>
        /// Every character the atlas carries.
        /// </summary>
        /// <remarks>
        /// Basic Latin plus the Latin-1 supplement, which covers English and Spanish and most of
        /// western Europe, plus <see cref="TypographicPunctuation"/>. It stops there deliberately:
        /// the atlas is static, so its size is the cost of every language it might one day carry, and
        /// a language the game has no strings in yet would be paying that cost for nothing.
        /// </remarks>
        private static string Characters()
        {
            var characters = new StringBuilder();

            for (var code = 0x20; code <= 0x7E; code++)
            {
                characters.Append((char)code);
            }

            // Skips 0x7F to 0x9F, which are control codes with nothing to draw.
            for (var code = 0xA0; code <= 0xFF; code++)
            {
                characters.Append((char)code);
            }

            characters.Append(TypographicPunctuation);

            return characters.ToString();
        }

        /// <summary>
        /// Writes the asset, and the atlas and material it owns, into one file.
        /// </summary>
        /// <remarks>
        /// The atlas texture and the material are created in memory by
        /// <c>TMP_FontAsset.CreateFontAsset</c> and belong to nothing until they are added as
        /// sub-assets. Left unsaved they are lost on the next domain reload, and the font asset then
        /// references a null texture — which looks exactly like a broken shader.
        /// </remarks>
        private static void Persist(TMP_FontAsset fontAsset)
        {
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            foreach (var texture in fontAsset.atlasTextures)
            {
                texture.name = $"{fontAsset.name} Atlas";
                AssetDatabase.AddObjectToAsset(texture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontAsset.name} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }

        /// <summary>
        /// Makes this the font any text component uses unless told otherwise.
        /// </summary>
        /// <remarks>
        /// Set through the serialised field because TMP_Settings exposes its default font asset as a
        /// read-only property. Doing it here rather than per label means one place decides, and a
        /// label that forgot to name a font gets the game's font rather than Liberation Sans.
        /// </remarks>
        private static void MakeDefault(TMP_FontAsset fontAsset)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                Debug.LogWarning("[fonts] no TMP Settings asset; the default font is unchanged");
                return;
            }

            var serialised = new SerializedObject(settings);
            var defaultFont = serialised.FindProperty("m_defaultFontAsset");
            if (defaultFont == null)
            {
                Debug.LogWarning("[fonts] TMP Settings has no m_defaultFontAsset field");
                return;
            }

            defaultFont.objectReferenceValue = fontAsset;
            serialised.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void Fail(string message)
        {
            Debug.LogError($"[fonts] {message}");
            EditorApplication.Exit(1);
        }
    }
}
