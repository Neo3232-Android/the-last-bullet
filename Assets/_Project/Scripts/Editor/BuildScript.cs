using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheLastBullet.Editor
{
    /// <summary>
    /// Headless build entry points for GitHub Actions (game-ci unity-builder
    /// calls BuildAndroidApk via -executeMethod) and for the Editor menu.
    /// Every PlayerSettings value the game needs is enforced here in code, so
    /// the build does not depend on anyone having clicked through the Editor.
    /// </summary>
    public static class BuildScript
    {
        private const string BundleId = "com.neo.thelastbullet";
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("The Last Bullet/Build Android APK")]
        public static void BuildAndroidApk()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/TheLastBullet.apk");
        }

        [MenuItem("The Last Bullet/Build Android AAB (Play Store)")]
        public static void BuildAndroidAab()
        {
            EditorUserBuildSettings.buildAppBundle = true;
            Build(BuildTarget.Android, "Builds/Android/TheLastBullet.aab");
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            ApplyPlayerSettings();
            EnsureURP();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, target);

            var report = BuildPipeline.BuildPlayer(
                new[] { ScenePath }, outputPath, target, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.Exception(
                    $"Build failed: {report.summary.result} ({report.summary.totalErrors} errors). " +
                    "See the build log for the first error.");
            }
            Debug.Log($"Build succeeded: {outputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
        }

        /// <summary>Enforces the exact Android + quality configuration.</summary>
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Neo Games";
            PlayerSettings.productName = "The Last Bullet";
            PlayerSettings.applicationIdentifier = BundleId;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            // Both input handlers: the UI uses the new Input System
            // (InputSystemUIInputModule on the EventSystem) while the
            // twin-stick controls read the old Input Manager (Input.touches).
            // "Both" keeps both alive; either one alone kills the other.
            PlayerSettings.activeInputHandler = InputHandler.Both;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Target API 35 (Android 15): the UBA builders ship Android SDK 35
            // pre-installed, and the Mac builder cannot install Platform 36
            // on demand (build 13 died at "Installing Android SDK Platform 36").
            // API 35 is the Play Store minimum for new apps (Aug 2025); API 36
            // only becomes required in Aug 2026, so revisit then for release.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Medium);
            // No keystore in the repo — Actions builds a debug APK; release
            // signing is added later with the user's own keystore (see README).
        }

        /// <summary>Creates the URP asset if missing and assigns it as the render pipeline.</summary>
        private static void EnsureURP()
        {
            var pipeline = SetupURP.EnsureURPAsset();
            GraphicsSettings.renderPipelineAsset = pipeline;
        }
    }
}
