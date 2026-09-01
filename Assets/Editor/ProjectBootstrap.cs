using System;
using System.Collections.Generic;
using System.IO;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Levels;
using Pathweaver.Game.App;
using Pathweaver.Game.Platform;
using Pathweaver.Game.Presentation;
using Pathweaver.Game.Presentation.Menus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pathweaver.EditorTools
{
    /// <summary>
    /// One-off project setup steps, runnable from the command line so the project
    /// can be configured without clicking through the Editor.
    /// </summary>
    internal static class ProjectBootstrap
    {
        private const string UniversalRenderPipeline = "com.unity.render-pipelines.universal";

        /// <summary>
        /// Adds the Universal Render Pipeline package, letting the Package Manager
        /// pick the version compatible with this Editor.
        /// </summary>
        internal static void AddUniversalRenderPipeline()
        {
            AddPackage(UniversalRenderPipeline);
        }

        /// <summary>
        /// Creates the Universal Render Pipeline asset with a 2D renderer and makes
        /// it the project's pipeline.
        /// </summary>
        /// <remarks>
        /// The 2D renderer is the point: the game is flat hex art, and the 2D
        /// renderer is what later gives the cozy lighting PRD section 2.2 asks for
        /// without reworking materials once real art lands.
        /// </remarks>
        internal static void ConfigureUniversalRenderPipeline()
        {
            const string settingsFolder = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(settingsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(rendererData, $"{settingsFolder}/Renderer2D.asset");

            var pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(pipeline, $"{settingsFolder}/URP-Pathweaver.asset");

            // Assigned through the serialised field because the typed API for
            // attaching a renderer is not public.
            var serialised = new SerializedObject(pipeline);
            var rendererList = serialised.FindProperty("m_RendererDataList");
            if (rendererList == null)
            {
                Debug.LogError("[bootstrap] URP asset has no m_RendererDataList field.");
                EditorApplication.Exit(1);
                return;
            }

            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            serialised.ApplyModifiedPropertiesWithoutUndo();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[bootstrap] pipeline set to {GraphicsSettings.defaultRenderPipeline?.name ?? "none"}");
        }

        /// <summary>
        /// Creates the game scene: a camera framing the board, and the objects that
        /// draw it.
        /// </summary>
        /// <remarks>
        /// Built in code rather than by hand so the scene is reproducible and its diff
        /// is explainable. A scene assembled through the Editor is a binary-ish blob
        /// nobody can review.
        /// </remarks>
        internal static void CreateGameScene()
        {
            const string folder = "Assets/Scenes";
            const string path = folder + "/Game.unity";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = BuildCamera();
            var board = new GameObject("Board").AddComponent<BoardView>();
            var heldTile = new GameObject("HeldTile").AddComponent<HeldTileView>();

            var tileMaterial = CreateTileMaterial();
            var boardSerialised = new SerializedObject(board);
            boardSerialised.FindProperty("_tileMaterial").objectReferenceValue = tileMaterial;
            boardSerialised.FindProperty("_theme").objectReferenceValue = CreateBoardTheme();
            boardSerialised.ApplyModifiedPropertiesWithoutUndo();
            var session = new GameObject("Session").AddComponent<GameSession>();
            var input = new GameObject("Input").AddComponent<InputController>();

            var fitter = new GameObject("CameraFitter").AddComponent<BoardCameraFitter>();

            var rotateHint = new GameObject("RotateHint").AddComponent<RotateHintAnimator>();
            Wire(rotateHint, ("_heldTileView", heldTile), ("_session", session));

            var restart = new GameObject("RestartButton").AddComponent<RestartButtonView>();
            Wire(restart, ("_boardView", board), ("_camera", camera), ("_session", session));

            var restartConfirm = new GameObject("RestartConfirm").AddComponent<RestartConfirmView>();
            Wire(restartConfirm, ("_boardView", board), ("_camera", camera));

            var hud = new GameObject("Hud");

            var progress = new GameObject("ProgressBar").AddComponent<ProgressBarView>();
            Wire(progress, ("_boardView", board), ("_camera", camera), ("_session", session));

            var pivotPips = new GameObject("PivotPips").AddComponent<TokenPipsView>();
            Wire(pivotPips, ("_boardView", board), ("_camera", camera), ("_session", session));
            SetPips(pivotPips, TokenKind.Pivot, new Vector2(0.12f, 0.26f));

            var pivotButton = new GameObject("PivotButton").AddComponent<PivotButtonView>();
            Wire(pivotButton, ("_boardView", board), ("_camera", camera), ("_session", session));

            var skip = new GameObject("SkipButton").AddComponent<SkipButtonView>();
            Wire(skip, ("_boardView", board), ("_camera", camera), ("_session", session));

            var skipPips = new GameObject("SkipPips").AddComponent<TokenPipsView>();
            Wire(skipPips, ("_boardView", board), ("_camera", camera), ("_session", session));
            SetPips(skipPips, TokenKind.Skip, new Vector2(0.86f, 0.26f));

            // The two board animations. Outside the HUD and outside the play controls: a finished board
            // hides its controls but keeps its board, and a paused board is still being looked at, so the
            // springs should keep breathing through both.
            var endpointPulse = new GameObject("EndpointPulse").AddComponent<EndpointPulseAnimator>();
            Wire(endpointPulse, ("_boardView", board));

            var flowPulse = new GameObject("FlowPulse").AddComponent<FlowPulseAnimator>();
            flowPulse.transform.SetParent(board.transform, worldPositionStays: true);
            Wire(flowPulse, ("_boardView", board), ("_session", session));

            var platform = new GameObject("Platform");
            var frameRate = platform.AddComponent<FrameRateGovernor>();
            var haptics = platform.AddComponent<HapticsService>();

            // Wired after the governor exists, because the opening camera flight asks for the active
            // frame rate while it runs.
            Wire(
                fitter,
                ("_camera", camera),
                ("_boardView", board),
                ("_frameRateGovernor", frameRate));

            Wire(heldTile, ("_boardView", board), ("_camera", camera));
            Wire(
                session,
                ("_boardView", board),
                ("_heldTileView", heldTile),
                ("_cameraFitter", fitter));
            Wire(
                input,
                ("_session", session),
                ("_boardView", board),
                ("_heldTileView", heldTile),
                ("_camera", camera),
                ("_frameRateGovernor", frameRate),
                ("_haptics", haptics),
                ("_restartButton", restart),
                ("_restartConfirm", restartConfirm),
                ("_skipButton", skip),
                ("_pivotButton", pivotButton),
                ("_cameraFitter", fitter));

            // Two groups, because they are hidden for different reasons. The HUD goes away while a
            // menu is up; the controls inside it also go away when a board is finished, leaving the
            // counters and the bar to report what happened. The board itself stays outside both,
            // because pausing should not blank the puzzle the player is looking at.
            var playControls = new GameObject("Controls");
            playControls.transform.SetParent(hud.transform, worldPositionStays: true);

            foreach (var controlObject in new[]
                     {
                         skip.gameObject, restart.gameObject, pivotButton.gameObject,
                         heldTile.gameObject, rotateHint.gameObject, restartConfirm.gameObject,
                     })
            {
                controlObject.transform.SetParent(playControls.transform, worldPositionStays: true);
            }

            foreach (var playObject in new[]
                     {
                         progress.gameObject, pivotPips.gameObject, skipPips.gameObject,
                     })
            {
                playObject.transform.SetParent(hud.transform, worldPositionStays: true);
            }

            var router = new GameObject("ScreenRouter").AddComponent<ScreenRouter>();

            var mainMenu = new GameObject("MainMenu").AddComponent<MainMenuView>();
            var levelSelect = new GameObject("LevelSelect").AddComponent<LevelSelectView>();
            var pauseScreen = new GameObject("PauseScreen").AddComponent<PauseView>();
            var settingsScreen = new GameObject("SettingsScreen").AddComponent<SettingsView>();
            var atlasScreen = new GameObject("AtlasScreen").AddComponent<AtlasView>();
            var helpScreen = new GameObject("HelpScreen").AddComponent<HelpView>();

            var routerSerialised = new SerializedObject(router);
            routerSerialised.FindProperty("_mainMenu").objectReferenceValue = mainMenu.gameObject;
            routerSerialised.FindProperty("_levelSelect").objectReferenceValue = levelSelect.gameObject;
            routerSerialised.FindProperty("_paused").objectReferenceValue = pauseScreen.gameObject;
            routerSerialised.FindProperty("_settings").objectReferenceValue = settingsScreen.gameObject;
            routerSerialised.FindProperty("_atlas").objectReferenceValue = atlasScreen.gameObject;
            routerSerialised.FindProperty("_help").objectReferenceValue = helpScreen.gameObject;
            routerSerialised.ApplyModifiedPropertiesWithoutUndo();

            var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
            Wire(
                flow,
                ("_router", router),
                ("_session", session),
                ("_mainMenu", mainMenu),
                ("_levelSelect", levelSelect),
                ("_pause", pauseScreen),
                ("_settings", settingsScreen),
                ("_atlas", atlasScreen),
                ("_help", helpScreen),
                ("_boardView", board),
                ("_camera", camera));

            var flowSerialised = new SerializedObject(flow);
            flowSerialised.FindProperty("_hud").objectReferenceValue = hud;
            flowSerialised.FindProperty("_playControls").objectReferenceValue = playControls;
            flowSerialised.ApplyModifiedPropertiesWithoutUndo();

            Wire(input, ("_router", router), ("_flow", flow));

            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };

            Debug.Log($"[bootstrap] created {path}");
        }

        /// <summary>
        /// Sets a pip row's currency and where it sits.
        /// </summary>
        private static void SetPips(Component pips, TokenKind kind, Vector2 viewportPosition)
        {
            var serialised = new SerializedObject(pips);
            serialised.FindProperty("_kind").enumValueIndex = (int)kind;
            serialised.FindProperty("_viewportPosition").vector2Value = viewportPosition;
            serialised.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Assigns serialised object references, since a scene built in code cannot
        /// drag them into place.
        /// </summary>
        private static void Wire(Component target, params (string Field, Component Value)[] links)
        {
            var serialised = new SerializedObject(target);

            foreach (var (field, value) in links)
            {
                var property = serialised.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[bootstrap] {target.GetType().Name} has no field {field}.");
                    EditorApplication.Exit(1);
                    return;
                }

                property.objectReferenceValue = value;
            }

            serialised.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Renders the opening position of a level to a PNG.
        /// </summary>
        /// <remarks>
        /// Rendering is the part of this project tests cannot judge, so this exists to
        /// make it reviewable: it produces an image from the command line, with no
        /// device, no Editor window, and nobody having to describe what they saw.
        /// </remarks>
        internal static void CaptureBoardPreview()
        {
            // Phone aspect, not square. A square preview showed a correctly framed board
            // while the real portrait screen cut it off at both edges.
            const int width = 1080;
            const int height = 2376;

            var levelId = ArgumentOr("-levelId", "biome1-01");
            var outputPath = ArgumentOr("-output", "Artifacts/board-preview.png");

            // Where in the endpoint pulse's cycle to freeze the board. Animation cannot be judged from a
            // still, but it can be judged from four of them — and the motion is a pure function of a
            // phase precisely so this is possible without a device.
            var pulsePhase = float.TryParse(ArgumentOr("-pulsePhase", "0.5"), out var phase) ? phase : 0.5f;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = BuildCamera();
            var board = new GameObject("Board").AddComponent<BoardView>();

            var previewSerialised = new SerializedObject(board);
            previewSerialised.FindProperty("_tileMaterial").objectReferenceValue = CreateTileMaterial();
            previewSerialised.ApplyModifiedPropertiesWithoutUndo();

            var previewFitter = new GameObject("CameraFitter").AddComponent<BoardCameraFitter>();
            Wire(previewFitter, ("_camera", camera), ("_boardView", board));

            var levelPath = Path.Combine("levels", levelId + ".pwlevel");
            var level = LevelLoader.Parse(File.ReadAllText(levelPath));
            var state = level.CreateGame(seed: 42UL);
            board.Build(state);

            var available = new HashSet<HexCoord>();
            foreach (var placement in state.LegalPlacements)
            {
                if (placement.Rotation == 0)
                {
                    available.Add(placement.Coordinate);
                }
            }

            board.Refresh(state, available);

            var elapsed = Mathf.Repeat(pulsePhase, 1f) * EndpointPulse.PeriodSeconds;
            foreach (var cell in board.PulsingCells)
            {
                cell.SetPulse(
                    EndpointPulse.ScaleAt(elapsed, cell.PulseRole),
                    EndpointPulse.FadeAt(elapsed, cell.PulseRole));
            }

            // Match the aspect the image is rendered at, or the fit is computed for the
            // Editor's game view instead.
            camera.aspect = (float)width / height;
            previewFitter.Fit(state);

            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var image = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            Debug.Log(
                $"[bootstrap] wrote {outputPath} for {level.Id} ({state.Board.Coordinates.Count} cells) "
                + $"at pulse phase {pulsePhase:0.00}");
        }

        /// <summary>
        /// Points a board view at the tile material asset.
        /// </summary>
        /// <remarks>
        /// The material is a serialised field rather than a runtime lookup, because a shader nothing
        /// references is stripped from a player build. Anything that builds a board view outside a
        /// scene needs this, which is why it is here rather than repeated.
        /// </remarks>
        internal static void WireTileMaterial(BoardView board)
        {
            var serialised = new SerializedObject(board);
            serialised.FindProperty("_tileMaterial").objectReferenceValue = CreateTileMaterial();
            serialised.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Creates the material the board is drawn with, as an asset.
        /// </summary>
        /// <remarks>
        /// It has to be an asset rather than a runtime <c>Shader.Find</c>: a shader nothing
        /// references is stripped from a player build, so the game renders in the Editor and
        /// comes up blank on a phone.
        /// </remarks>
        private static Material CreateTileMaterial()
        {
            const string path = "Assets/Settings/TileMaterial.mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("[bootstrap] URP unlit shader not found.");
                EditorApplication.Exit(1);
                return null;
            }

            // Unlit: the board is flat colour, and lighting it would cost frame time for no
            // visual gain until real art arrives.
            var material = new Material(shader) { name = "TileMaterial" };
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();

            return material;
        }

        /// <summary>
        /// Creates the board theme asset if it does not exist, and leaves it empty.
        /// </summary>
        /// <remarks>
        /// Empty on purpose. Every field is optional, so an empty theme renders exactly the
        /// placeholder geometry the game ships with today — but the asset exists and is wired
        /// up, so dropping in the first sprite is a change to an asset rather than a change to
        /// the game.
        /// </remarks>
        private static BoardTheme CreateBoardTheme()
        {
            const string path = "Assets/Settings/BoardTheme.asset";

            var existing = AssetDatabase.LoadAssetAtPath<BoardTheme>(path);
            if (existing != null)
            {
                return existing;
            }

            var theme = ScriptableObject.CreateInstance<BoardTheme>();
            theme.name = "BoardTheme";
            AssetDatabase.CreateAsset(theme, path);
            AssetDatabase.SaveAssets();

            return theme;
        }

        private static Camera BuildCamera()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;

            // The menu framing. BoardCameraFitter takes over per level, and GameFlow puts this
            // back whenever a menu is shown, so the two agree on one value.
            camera.orthographicSize = MenuCamera.OrthographicSize;
            camera.backgroundColor = BoardPalette.Background;
            camera.clearFlags = CameraClearFlags.SolidColor;

            return camera;
        }

        internal static string ArgumentOr(string name, string fallback)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == name)
                {
                    return arguments[index + 1];
                }
            }

            return fallback;
        }

        private static void AddPackage(string identifier)
        {
            Debug.Log($"[bootstrap] adding {identifier}");
            AddRequest request = Client.Add(identifier);

            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError($"[bootstrap] failed to add {identifier}: {request.Error?.message}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[bootstrap] added {request.Result.name}@{request.Result.version}");
        }
    }
}
