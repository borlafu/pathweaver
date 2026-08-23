using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Pathweaver.EditorTools
{
    /// <summary>
    /// Builds the Android player from the command line.
    /// </summary>
    /// <remarks>
    /// Player settings are applied here rather than left in the Editor UI, so a build is
    /// reproducible and the settings that matter are reviewable in a diff. The release
    /// configuration proper — bundle output, IL2CPP, signing, 16 KB pages — belongs to
    /// #29; this produces something installable on a phone.
    /// </remarks>
    internal static class AndroidBuild
    {
        private const string ApplicationIdentifier = "es.borlafu.pathweaver";
        private const string ProductName = "Pathweaver";
        private const string CompanyName = "borlafu";

        /// <summary>
        /// Applies the player settings the game requires.
        /// </summary>
        internal static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationIdentifier);

            // Google Play requires API 36 for new submissions from 31 August 2026.
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;

            // 24 covers effectively every device still receiving apps, and avoids carrying
            // compatibility work for versions nobody runs.
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;

            // Portrait only: the whole input model assumes one thumb on a held phone.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // The 1.5 second cold-boot target in PRD section 1.2 does not survive a splash
            // screen. Unity 6 allows turning it off on a Personal licence.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // Nothing here needs the frame rate tied to the display; the governor manages it.
            QualitySettings.vSyncCount = 0;

            Debug.Log(
                $"[build] configured {ApplicationIdentifier}, target API " +
                $"{(int)PlayerSettings.Android.targetSdkVersion}, splash " +
                $"{(PlayerSettings.SplashScreen.show ? "on" : "off")}");
        }

        /// <summary>
        /// Applies release signing from the environment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read from environment variables, never from the Editor's saved settings. Unity stores
        /// keystore passwords in ProjectSettings.asset, which is a committed file, so entering them
        /// in Publishing Settings is enough to put them in a git diff.
        /// </para>
        /// <para>
        /// A missing variable is not an error here. Development builds are signed with Unity's debug
        /// key and are the normal case for on-device testing; refusing to build without release
        /// credentials would make the everyday path need secrets it does not use.
        /// </para>
        /// </remarks>
        /// <returns>Whether release signing was configured.</returns>
        private static bool TryConfigureSigning()
        {
            var keystore = Environment.GetEnvironmentVariable("PATHWEAVER_KEYSTORE");
            var keystorePassword = Environment.GetEnvironmentVariable("PATHWEAVER_KEYSTORE_PASS");
            var alias = Environment.GetEnvironmentVariable("PATHWEAVER_KEY_ALIAS");
            var aliasPassword = Environment.GetEnvironmentVariable("PATHWEAVER_KEY_PASS");

            if (string.IsNullOrEmpty(keystore)
                || string.IsNullOrEmpty(keystorePassword)
                || string.IsNullOrEmpty(alias)
                || string.IsNullOrEmpty(aliasPassword))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[build] no release credentials in the environment; using the debug key");
                return false;
            }

            if (!File.Exists(keystore))
            {
                // Worth failing on: the intent to sign was expressed, so quietly falling back to a
                // debug key would produce an artefact that looks releasable and is not.
                Debug.LogError($"[build] keystore not found at {keystore}");
                EditorApplication.Exit(1);
                return false;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPassword;

            Debug.Log($"[build] signing with alias \"{alias}\" from {Path.GetFileName(keystore)}");
            return true;
        }

        /// <summary>
        /// Builds a signed Android App Bundle for upload to Google Play.
        /// </summary>
        /// <remarks>
        /// A bundle rather than an APK because Play requires one for new apps, and it lets Google
        /// deliver per-device slices rather than one package carrying every variant — which is also
        /// the largest single lever on the download size the PRD caps at 85 MB.
        /// </remarks>
        internal static void BuildAab()
        {
            ConfigurePlayerSettings();

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // Strips managed code that nothing references. The largest saving available before art
            // exists, and the one most likely to break something, which is why the release build is
            // tested rather than assumed.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);

            PlayerSettings.Android.optimizedFramePacing = true;

            var signed = TryConfigureSigning();
            if (!signed)
            {
                Debug.LogWarning(
                    "[build] the bundle will carry a debug key and Play will reject it");
            }

            EditorUserBuildSettings.buildAppBundle = true;

            var outputPath = ArgumentOr("-aabOutput", "Artifacts/pathweaver.aab");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Game.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,

                // No Development flag: a release bundle carries no debug symbols and no profiler.
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[build] failed: {summary.result}, {summary.totalErrors} error(s)");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[build] wrote {outputPath} in {summary.totalTime}");
        }

        /// <summary>
        /// Builds an installable APK for on-device testing.
        /// </summary>
        internal static void BuildApk()
        {
            ConfigurePlayerSettings();

            // ARM64 only. Play requires 64-bit, and dropping ARMv7 halves the native
            // payload for a device population that no longer needs it.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // IL2CPP, because Mono cannot target ARM64 and 32-bit ARMv7 is not a usable
            // fallback: devices from the Pixel 7 onward dropped 32-bit support entirely, so
            // an ARMv7 build would not install on a modern phone. Pairing Mono with ARM64
            // fails the build outright with "Target architecture not specified".
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            var outputPath = ArgumentOr("-apkOutput", "Artifacts/pathweaver.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // Development builds keep Unity's debug key, which adb accepts and Play does not.
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Game.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[build] failed: {summary.result}, {summary.totalErrors} error(s)");
                EditorApplication.Exit(1);
                return;
            }

            var megabytes = summary.totalSize / (1024f * 1024f);
            Debug.Log($"[build] wrote {outputPath}, {megabytes:F1} MB, in {summary.totalTime}");
        }

        private static string ArgumentOr(string name, string fallback)
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
    }
}
