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
        /// <summary>
        /// Last applied glow flag. QualityData.GlowEnabled otherwise has no
        /// consumer anywhere — model / placeholder code that owns glow
        /// materials reads this instead of re-deriving the tier.
        /// </summary>
        public static bool GlowEnabled { get; private set; } = true;

        public static void Apply(Quality quality)
        {
            QualityDef def = QualityData.Get(quality);
            GlowEnabled = def.GlowEnabled;

            Application.targetFrameRate = 60;
            QualitySettings.SetQualityLevel((int)quality, true);
            QualitySettings.vSyncCount = 0; // mobile: rely on targetFrameRate, not vsync

            // Per-tier raster levers — these are what actually separate the
            // tiers on a phone, beyond the particle/entity caps in QualityData:
            // MSAA, per-pixel light count, texture resolution, shadow distance.
            // Low: no AA, no pixel lights, halved textures, no shadows.
            // Medium: no AA, 1 pixel light, full textures, no shadows.
            // High: 2x MSAA, 2 pixel lights, shadows on.
            // Ultra: 4x MSAA, 4 pixel lights, shadows on, 1.5x particles
            //   (QualityData) and the 160 entity cap.
            switch (quality)
            {
                case Quality.Low:
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.pixelLightCount = 0;
                    QualitySettings.masterTextureLimit = 1;
                    break;
                case Quality.Medium:
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.pixelLightCount = 1;
                    QualitySettings.masterTextureLimit = 0;
                    break;
                case Quality.High:
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.pixelLightCount = 2;
                    QualitySettings.masterTextureLimit = 0;
                    break;
                default: // Ultra
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.pixelLightCount = 4;
                    QualitySettings.masterTextureLimit = 0;
                    break;
            }
            QualitySettings.shadowDistance = def.ShadowsEnabled ? 40f : 0f;

            // URP: render scale + shadows are the two biggest mobile levers.
            var urp = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                // Ultra keeps renderScale 1.0 (QualityData): supersampling
                // above 1.0 costs more frames than the extra sharpness is worth
                // on a phone; Ultra's edge is particles, entities, AA, lights.
                urp.renderScale = def.ResolutionScale;
                urp.shadowDistance = def.ShadowsEnabled ? 40f : 0f;
                // URP 17: supportsMainLightShadows is read-only on the asset.
                // The runtime shadow toggle is the built-in quality setting
                // (fully qualified: URP also defines a ShadowQuality enum).
                QualitySettings.shadows = def.ShadowsEnabled ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;
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
