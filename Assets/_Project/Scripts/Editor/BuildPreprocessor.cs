using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace TheLastBullet.Editor
{
    /// <summary>
    /// Runs before every player build (including Unity Build Automation,
    /// which never calls BuildScript directly). Enforces the Android +
    /// quality settings and creates the URP asset if it is missing, so the
    /// build does not depend on anyone having clicked through the Editor
    /// or on committed PlayerSettings/GraphicsSettings files.
    /// </summary>
    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            BuildScript.ApplyPlayerSettings();
            ModelImportSetup.EnsureHumanoidRigs();
            SetupURP.EnsureURPAsset(refresh: false);
            SetupURP.EnsureAlwaysIncludedShaders();
        }
    }
}
