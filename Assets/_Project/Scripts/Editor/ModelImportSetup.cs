using System;
using UnityEditor;
using UnityEngine;

namespace TheLastBullet.Editor
{
    /// <summary>
    /// Build 2: the 13 FBXs under Assets/_Project/Resources/Models/ ship
    /// with NO .meta files, so Unity would import them on Generic rigs and
    /// the Mixamo clips could not retarget onto the Humanoid avatar (and
    /// ragdolls would have no Humanoid bone map). This runs from
    /// BuildPreprocessor.OnPreprocessBuild — before the build reads the
    /// assets — and flips every .fbx under Resources/Models to Humanoid,
    /// reimporting only the ones that are not already Humanoid.
    ///
    /// Everything is guarded: a failure here logs a warning and the build
    /// continues (with Generic rigs + placeholder fallback at runtime), it
    /// can never throw out of OnPreprocessBuild and break the build.
    ///
    /// What this does NOT do: author ragdolls (Ragdoll Wizard, per model,
    /// in the Editor) or build AnimatorControllers for the clips — those
    /// are still open Editor steps, see Docs/MODEL_PIPELINE.md.
    /// </summary>
    public static class ModelImportSetup
    {
        private const string ModelsFolder = "Assets/_Project/Resources/Models";

        /// <summary>
        /// Sets animationType = Humanoid on every .fbx under Resources/Models
        /// that is not already Humanoid. Returns how many models were fixed.
        /// </summary>
        public static int EnsureHumanoidRigs()
        {
            int fixedCount = 0;
            try
            {
                string[] guids;
                try
                {
                    guids = AssetDatabase.FindAssets(string.Empty, new[] { ModelsFolder });
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ModelImportSetup] FindAssets failed: " + e.Message);
                    return 0;
                }
                if (guids == null) return 0;

                foreach (string guid in guids)
                {
                    try
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrEmpty(path)) continue;
                        if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) continue;

                        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                        if (importer == null) continue;
                        if (importer.animationType == ModelImporterAnimationType.Humanoid) continue;

                        importer.animationType = ModelImporterAnimationType.Humanoid;
                        importer.SaveAndReimport();
                        fixedCount++;
                        Debug.Log("[ModelImportSetup] Set Humanoid rig: " + path);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[ModelImportSetup] Skipping a model import fix: " + e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ModelImportSetup] EnsureHumanoidRigs failed (build continues): " + e.Message);
            }
            return fixedCount;
        }
    }
}
