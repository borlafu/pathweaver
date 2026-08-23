using System;
using UnityEditor;
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
