using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Applies a Quality level to the running game. Best practice for mobile URP:
    /// render scale + shadow on/off on the URP asset, quality level on
    /// QualitySettings, 60fps target, and the entity/particle caps handed to
    /// the simulation. Safe to call before the URP asset exists (skips those bits).
    /// </summary>
    public static class QualitySettingsApplier
    {
        public static void Apply(Quality quality)
        {
            QualityDef def = QualityData.Get(quality);

            Application.targetFrameRate = 60;
            QualitySettings.SetQualityLevel((int)quality, true);
            QualitySettings.vSyncCount = 0; // mobile: rely on targetFrameRate, not vsync

            // URP: render scale + shadows are the two biggest mobile levers.
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = def.ResolutionScale;
                urp.shadowDistance = def.ShadowsEnabled ? 40f : 0f;
                // URP's main-light shadow toggle lives on the asset:
                urp.supportsMainLightShadows = def.ShadowsEnabled;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.ApplyQualityCaps(def);
        }

        public static QualityDef Current()
        {
            Quality q = SaveSystem.Instance != null
                ? SaveSystem.Instance.QualitySetting
                : Quality.High;
            return QualityData.Get(q);
        }
    }
}
