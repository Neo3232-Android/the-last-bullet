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

        /// <summary>
        /// The scene carries no serialized UI, so the build strips the shaders
        /// the runtime-created UI needs — uGUI Images render magenta and
        /// TextMeshPro text is invisible. Force-include them in every build
        /// (the same pattern TouchScript uses in its build preprocessor).
        /// Runs in the build preprocessor, so it also applies to UBA builds.
        /// </summary>
        public static void EnsureAlwaysIncludedShaders()
        {
            const string path = "ProjectSettings/GraphicsSettings.asset";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("GraphicsSettings.asset not found at " + path);
                return;
            }
            var so = new SerializedObject(assets[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null)
            {
                Debug.LogWarning("m_AlwaysIncludedShaders not found in GraphicsSettings");
                return;
            }
            // Legacy uGUI Text uses the built-in Arial font + UI/Default Font
            // shader (always in the build); TextMeshPro is gone from the UI,
            // so its shaders are no longer force-included.
            string[] names =
            {
                "UI/Default",
                "UI/Default Font",
                "Sprites/Default",
                "Universal Render Pipeline/Lit",
            };
            bool changed = false;
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning("Shader not found (skipped): " + name);
                    continue;
                }
                bool exists = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;
                int idx = list.arraySize;
                list.arraySize = idx + 1;
                list.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("Added runtime UI shaders to Always Included Shaders.");
            }
        }

        /// <summary>Creates the URP asset + renderer if missing and assigns it.
        /// refresh=false skips AssetDatabase.Refresh (used by the build
        /// preprocessor, where a refresh during the build is not wanted).</summary>
        public static UniversalRenderPipelineAsset EnsureURPAsset(bool refresh = true)
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
            if (refresh)
                AssetDatabase.Refresh();
            Debug.Log("URP asset created at " + PipelinePath);
            return pipeline;
        }
    }
}
