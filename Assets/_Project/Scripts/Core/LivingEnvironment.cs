using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>Weather the living environment can show. Set via SetWeather.</summary>
    public enum WeatherKind
    {
        Clear,
        Rain,
        Fog
    }

    /// <summary>
    /// Cheap ambient life for the arena: tree sway, fog drift, streetlight
    /// flicker, floating embers, rain splashes. Everything is timer-driven
    /// (no per-frame allocations, no per-frame dictionary lookups) and
    /// quality-gated: a complete no-op on Low. Props are registered by
    /// MapBuilder / GameManager — this class never finds them itself.
    ///
    /// Rain is ground splashes via the Juice particle system (reused bursts
    /// on a timer), not falling drops — a dedicated rain particle system
    /// would read better and is a device-check follow-up.
    /// </summary>
    public sealed class LivingEnvironment : MonoBehaviour
    {
        private readonly List<Transform> _trees = new List<Transform>();
        private readonly List<Quaternion> _treeBaseRot = new List<Quaternion>();
        private readonly List<Light> _lights = new List<Light>();
        private readonly List<float> _lightBaseIntensity = new List<float>();
        private readonly List<Vector3> _fireSources = new List<Vector3>();

        private WeatherKind _weather = WeatherKind.Clear;
        private float _baseFogDensity = 0.01f;
        private bool _fogCaptured;
        private float _time;
        private float _rainTimer;
        private float _emberTimer;
        private float _flickerTimer;
        private float _qualityTimer;
        private Quality _quality = Quality.High;

        public WeatherKind Weather => _weather;

        /// <summary>Register a swaying tree. Called by MapBuilder.PopulateZone.</summary>
        public void RegisterTree(Transform tree)
        {
            if (tree == null) return;
            _trees.Add(tree);
            _treeBaseRot.Add(tree.rotation);
        }

        /// <summary>Register a flickering streetlight. Called by MapBuilder.PopulateZone.</summary>
        public void RegisterLight(Light light)
        {
            if (light == null) return;
            _lights.Add(light);
            _lightBaseIntensity.Add(light.intensity);
        }

        /// <summary>Register a fire source embers float above. Called by MapBuilder.PopulateZone.</summary>
        public void RegisterFireSource(Vector3 pos)
        {
            _fireSources.Add(pos);
        }

        /// <summary>Set the arena weather (RainStorm / Fog level events). Restores fog density when leaving Fog.</summary>
        public void SetWeather(WeatherKind weather)
        {
            if (_weather == WeatherKind.Fog && weather != WeatherKind.Fog && _fogCaptured)
                RenderSettings.fogDensity = _baseFogDensity;
            _weather = weather;
            if (weather == WeatherKind.Fog && !_fogCaptured)
            {
                _baseFogDensity = RenderSettings.fogDensity;
                _fogCaptured = true;
            }
        }

        private void Update()
        {
            // Refresh the cached quality ~once a second — never per frame.
            _qualityTimer -= Time.deltaTime;
            if (_qualityTimer <= 0f)
            {
                _qualityTimer = 1f;
                _quality = QualitySettingsApplier.Current().Id;
            }
            if (_quality == Quality.Low) return; // living environment is a no-op on Low

            _time += Time.deltaTime;
            var gm = GameManager.Instance;
            var juice = gm != null ? gm.Juice : null;

            // Trees sway: small sine rotation around the registered base pose.
            for (int i = _trees.Count - 1; i >= 0; i--)
            {
                var t = _trees[i];
                if (t == null)
                {
                    _trees.RemoveAt(i);
                    _treeBaseRot.RemoveAt(i);
                    continue;
                }
                float sway = Mathf.Sin(_time * 0.9f + i * 1.7f) * 1.5f;
                t.rotation = _treeBaseRot[i] * Quaternion.Euler(sway, 0f, sway * 0.6f);
            }

            // Fog drifts: gentle breathing of the density while Fog weather is on.
            if (_weather == WeatherKind.Fog && RenderSettings.fog)
            {
                if (!_fogCaptured)
                {
                    _baseFogDensity = RenderSettings.fogDensity;
                    _fogCaptured = true;
                }
                RenderSettings.fogDensity = _baseFogDensity * (1f + 0.15f * Mathf.Sin(_time * 0.25f));
            }

            // Streetlight flicker: 10 Hz check, occasional dip to ~half intensity.
            _flickerTimer -= Time.deltaTime;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = 0.1f;
                for (int i = _lights.Count - 1; i >= 0; i--)
                {
                    var l = _lights[i];
                    if (l == null)
                    {
                        _lights.RemoveAt(i);
                        _lightBaseIntensity.RemoveAt(i);
                        continue;
                    }
                    float n = Mathf.PerlinNoise(_time * 3f + i * 13.7f, 0f);
                    l.intensity = _lightBaseIntensity[i] * (n > 0.85f ? 0.55f : 1f);
                }
            }

            // Rain: ground splashes around the player, reused Juice bursts on a timer.
            if (_weather == WeatherKind.Rain && juice != null)
            {
                _rainTimer -= Time.deltaTime;
                if (_rainTimer <= 0f)
                {
                    _rainTimer = _quality == Quality.Medium ? 0.5f : 0.3f;
                    Vector3 c = gm.Player != null ? gm.Player.transform.position : Vector3.zero;
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 p = c + new Vector3(Random.Range(-8f, 8f), 0.3f, Random.Range(-8f, 8f));
                        juice.SpawnDust(p, 1);
                    }
                }
            }

            // Embers floating above fire sources: small orange bursts on a timer.
            if (_fireSources.Count > 0 && juice != null)
            {
                _emberTimer -= Time.deltaTime;
                if (_emberTimer <= 0f)
                {
                    _emberTimer = 0.7f;
                    for (int i = 0; i < _fireSources.Count; i++)
                        juice.SpawnExplosion(_fireSources[i] + Vector3.up * 0.5f, 2);
                }
            }
        }
    }
}
