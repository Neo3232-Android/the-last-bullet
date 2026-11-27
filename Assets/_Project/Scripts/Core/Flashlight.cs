using UnityEngine;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Flashlight for dark maps: a warm spotlight mounted above the player,
    /// aimed along the aim stick (not the movement facing), enabled only
    /// when the map IsDark. On dark maps the sun is dimmed so the beam
    /// matters. One spotlight is cheap on every quality tier, so it is not
    /// quality-gated. Created by GameManager.SpawnPlayer; driven by
    /// PlayerController (SetAim each frame).
    /// </summary>
    public sealed class Flashlight : MonoBehaviour
    {
        private Light _spot;
        private Light _sun;
        private float _sunBaseIntensity = 1.1f;
        private bool _dark;

        public bool IsDark => _dark;

        public void Setup(bool isDark, Light sun = null)
        {
            _dark = isDark;
            if (sun != null) _sun = sun;
            if (_spot == null)
            {
                var go = new GameObject("Flashlight");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                _spot = go.AddComponent<Light>();
                _spot.type = LightType.Spot;
                _spot.color = new Color(1f, 0.95f, 0.8f);
                _spot.intensity = 3.5f;
                _spot.range = 14f;
                _spot.spotAngle = 55f;
            }
            ApplyDark();
        }

        /// <summary>
        /// Aim the beam along aimAngle (world angle in the XZ plane), given
        /// the player's facing angle. The spot is a child of the player, so
        /// the local yaw is the difference between facing and aim.
        /// </summary>
        public void SetAim(float aimAngleRad, float facingAngleRad)
        {
            if (_spot == null) return;
            float localYawDeg = (facingAngleRad - aimAngleRad) * Mathf.Rad2Deg;
            _spot.transform.localRotation = Quaternion.Euler(0f, localYawDeg, 0f);
        }

        public void SetDark(bool isDark)
        {
            _dark = isDark;
            ApplyDark();
        }

        private void ApplyDark()
        {
            if (_spot != null) _spot.enabled = _dark;
            if (_sun != null) _sun.intensity = _dark ? 0.25f : _sunBaseIntensity;
        }
    }
}
