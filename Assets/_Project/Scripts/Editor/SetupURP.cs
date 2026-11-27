using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TheLastBullet.Editor
{
    /// <summary>
    /// One-time URP setup. Creates the Universal Render Pipeline asset +
    /// renderer data under Assets/_Project/URP/ and assigns them, so the
    /// project renders with URP 17.3 even though it was scaffolded without
    /// ever opening the Editor. BuildScript calls EnsureURPAsset() on every
    /// headless build, so the first GitHub Actions run creates it too.
    /// Post-processing (bloom/vignette) is tuned later in the Editor.
    /// </summary>
    public static class SetupURP
    {
        private const string Folder = "Assets/_Project/URP";
        private const string PipelinePath = Folder + "/UniversalRenderPipelineAsset.asset";
        private const string RendererPath = Folder + "/UniversalRendererData.asset";

        [MenuItem("The Last Bullet/Setup URP")]
        public static void CreateURPAssetMenu() => EnsureURPAsset();

        public static UniversalRenderPipelineAsset EnsureURPAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (existing != null)
            {
                GraphicsSettings.renderPipelineAsset = existing;
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project", "URP");

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);

            // URP 17: SetRenderer() is internal (not public API). The public
            // factory attaches the renderer when the asset is created.
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.renderScale = 1f;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 50f;
            AssetDatabase.CreateAsset(pipeline, PipelinePath);

            GraphicsSettings.renderPipelineAsset = pipeline;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("URP asset created at " + PipelinePath);
            return pipeline;
        }
    }
}
