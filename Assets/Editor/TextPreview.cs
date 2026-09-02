using System.IO;
using Pathweaver.Game.Presentation;
using Pathweaver.Game.Presentation.Menus;
using Pathweaver.Game.Presentation.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pathweaver.EditorTools
{
    /// <summary>
    /// Renders a sheet of every text size the game uses, at phone aspect.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same argument as <c>ProjectBootstrap.CaptureBoardPreview</c>: whether text is legible needs
    /// a person to look at it, but it does not need a device, an Editor session, or a build. The
    /// sheet is rendered at 1080x2376 — the phone the board previews use — so a size judged here is
    /// the size a player sees.
    /// </para>
    /// <para>
    /// Worth re-running whenever <c>LabelMetrics</c> changes. The numbers in that file are the whole
    /// subject of this image.
    /// </para>
    /// </remarks>
    internal static class TextPreview
    {
        private const int Width = 1080;
        private const int Height = 2376;

        /// <summary>
        /// Writes the sheet.
        /// </summary>
        /// <remarks>
        /// Run with:
        /// <code>
        /// unity -batchmode -quit -projectPath . \
        ///   -executeMethod Pathweaver.EditorTools.TextPreview.Capture \
        ///   -output Artifacts/text-preview.png -logFile /tmp/unity.log
        /// </code>
        /// </remarks>
        internal static void Capture()
        {
            var outputPath = OutputPath();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = BuildPreviewCamera();
            var sheet = new GameObject("Sheet").transform;

            // Realistic strings rather than lorem ipsum, and both languages the atlas covers, because
            // the accented characters are the ones that sit closest to the line above.
            Line(sheet, camera, "heading", 0.86f, LabelMetrics.HeadingHeightFraction, "First Waters");
            Line(sheet, camera, "heading-accented", 0.80f, LabelMetrics.HeadingHeightFraction, "Últimas Aguas");
            Line(sheet, camera, "body", 0.72f, LabelMetrics.BodyHeightFraction, "246 of 600");
            Line(sheet, camera, "body-prose", 0.66f, LabelMetrics.BodyHeightFraction, "A tile joins its own kind.");
            Line(sheet, camera, "caption", 0.58f, LabelMetrics.CaptionHeightFraction, "biome1-01 — First Waters");
            Line(sheet, camera, "caption-accented", 0.53f, LabelMetrics.CaptionHeightFraction, "pingüino, último, ñu, ¿qué?");
            Line(sheet, camera, "minimum", 0.45f, LabelMetrics.MinimumHeightFraction, "the smallest text the game may draw");

            // Secondary text, whose whole question is whether it is still readable when dimmed. The
            // first version of this sheet used TokenEmpty and came out at 1.8:1 against the
            // background, which is what put BoardPalette.TextSecondary in the palette.
            var secondary = Line(
                sheet, camera, "secondary", 0.37f, LabelMetrics.BodyHeightFraction, "3 tokens, 3 skips");
            secondary.SetColour(BoardPalette.TextSecondary);

            var wrapped = Line(
                sheet, camera, "wrapped", 0.24f, LabelMetrics.BodyHeightFraction,
                "A spring's ring grows outward and a hub's collapses inward, so the role reads "
                + "without colour and without words.");
            wrapped.SetWrapWidth(0.8f);

            Write(camera, outputPath);
        }

        /// <summary>
        /// Renders the help screen as a player would meet it, one image per page.
        /// </summary>
        /// <remarks>
        /// Run with:
        /// <code>
        /// unity -batchmode -quit -projectPath . \
        ///   -executeMethod Pathweaver.EditorTools.TextPreview.CaptureHelp \
        ///   -output Artifacts/help -logFile /tmp/unity.log
        /// </code>
        /// Writes <c>&lt;output&gt;-1.png</c> and so on. Wrapping is the reason this exists: how many
        /// lines a paragraph becomes depends on the screen, so the only way to know a page fits is to
        /// render it at the size a phone will.
        /// </remarks>
        internal static void CaptureHelp()
        {
            var prefix = OutputPath("Artifacts/help");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = BuildPreviewCamera();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var help = new GameObject("HelpScreen").AddComponent<HelpView>();

            help.Build(camera, material);

            for (var page = 0; page < HelpView.PageCount; page++)
            {
                help.ShowPage(page);
                Write(camera, $"{prefix}-{page + 1}.png");
            }
        }

        /// <summary>
        /// Renders the level list, at phone aspect, with every level shown as cleared and as locked.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Run with:
        /// <code>
        /// unity -batchmode -quit -projectPath . \
        ///   -executeMethod Pathweaver.EditorTools.TextPreview.CaptureLevelSelect \
        ///   -output Artifacts/level-select -logFile /tmp/unity.log
        /// </code>
        /// Writes <c>&lt;output&gt;-cleared.png</c> and <c>&lt;output&gt;-fresh.png</c>.
        /// </para>
        /// <para>
        /// This exists because the grid grows rather than scrolls, and growing has a limit. The campaign
        /// went from twenty levels to forty when biome two was finished, which took the grid from four
        /// columns to five and from five rows to eight and nearly halved the row spacing. Arithmetic can
        /// say the rows do not overlap; only a render says whether forty buttons and forty numbers still
        /// read as a list.
        /// </para>
        /// </remarks>
        internal static void CaptureLevelSelect()
        {
            var prefix = OutputPath("Artifacts/level-select");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = BuildPreviewCamera();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var campaign = Pathweaver.Game.App.CampaignCatalogue.Load();

            // Both extremes, because they are drawn differently: a cleared level is green, the next one
            // is offered, and everything past it is locked.
            var everything = Pathweaver.Core.Campaign.CampaignProgress.Empty;
            foreach (var id in campaign.LevelIds)
            {
                everything = everything.WithCleared(id);
            }

            foreach (var (progress, suffix) in new[]
                     {
                         (everything, "cleared"),
                         (Pathweaver.Core.Campaign.CampaignProgress.Empty, "fresh"),
                     })
            {
                var view = new GameObject($"LevelSelect {suffix}").AddComponent<LevelSelectView>();
                view.Build(camera, material, campaign, progress);

                Write(camera, $"{prefix}-{suffix}.png");

                Object.DestroyImmediate(view.gameObject);
            }
        }

        private static Camera BuildPreviewCamera()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = MenuCamera.OrthographicSize;
            camera.backgroundColor = BoardPalette.Background;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.aspect = (float)Width / Height;

            return camera;
        }

        private static TextLabel Line(
            Transform parent, Camera camera, string name, float viewportY, float heightFraction, string content)
        {
            var label = TextLabel.Create(
                parent,
                camera,
                name,
                new Vector2(0.5f, viewportY),
                heightFraction,
                BoardPalette.TextPrimary,
                TextAlignmentOptions.Center);

            label.SetText(content);
            return label;
        }

        private static string OutputPath(string fallback = "Artifacts/text-preview.png")
        {
            var arguments = System.Environment.GetCommandLineArgs();

            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == "-output")
                {
                    return arguments[index + 1];
                }
            }

            return fallback;
        }

        private static void Write(Camera camera, string outputPath)
        {
            var target = new RenderTexture(Width, Height, 24);
            camera.targetTexture = target;

            // Text meshes are built during a layout pass rather than at construction, so a render
            // taken immediately after Create shows nothing. This forces the pass.
            Canvas.ForceUpdateCanvases();
            foreach (var text in Object.FindObjectsByType<TextMeshPro>())
            {
                text.ForceMeshUpdate();
            }

            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var image = new Texture2D(Width, Height, TextureFormat.RGB24, mipChain: false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            Debug.Log($"[text] wrote {outputPath} at {Width}x{Height}");
        }
    }
}
