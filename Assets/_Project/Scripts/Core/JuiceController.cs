using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Camera juice: smooth follow, trauma-based screen shake, zoom punch,
    /// and lightweight particle bursts (one pooled ParticleSystem, Emit-based
    /// — no per-frame allocations). Lives on the Main Camera.
    ///
    /// Camera: angled top-down view of the arena (~55 degrees), following the
    /// player. This is a full 3D scene — the camera can pitch/zoom for boss
    /// intros and slow-mo moments.
    /// </summary>
    public sealed class JuiceController : MonoBehaviour
    {
        public float ZoomPunch { get; set; }

        private Camera _camera;
        private Transform _followTarget;
        private float _trauma;
        private float _baseFov = 55f;
        private float _zoomPunchAmount;
        private ParticleSystem _particles;
        private ParticleSystem.MainModule _particleMain;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
            _baseFov = _camera != null ? _camera.fieldOfView : 55f;

            // One pooled particle system for ALL bursts (goo, explosions, pickups).
            var go = new GameObject("JuiceParticles");
            go.transform.SetParent(transform, false);
            _particles = go.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.7f;
            main.startSpeed = 4f;
            main.startSize = 0.25f;
            main.maxParticles = 2000;
            var emission = _particles.emission;
            emission.enabled = false;
            var shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            _particleMain = main;
        }

        /// <summary>Called every frame (unscaled) by GameManager.</summary>
        public void Tick(float dt)
        {
            _trauma = Mathf.Max(0f, _trauma - dt * 1.6f);
            ZoomPunch = Mathf.Max(0f, ZoomPunch - dt * 1.5f);
            _zoomPunchAmount = Mathf.Lerp(_zoomPunchAmount, ZoomPunch, dt * 8f);

            if (_camera == null) return;
            _camera.fieldOfView = _baseFov - _zoomPunchAmount * 8f;

            // Follow the player, smooth.
            var gm = GameManager.Instance;
            if (gm != null && gm.Player != null)
            {
                Vector3 target = gm.Player.transform.position;
                Vector3 desired = target + new Vector3(0f, 26f, -14f);
                _camera.transform.position = Vector3.Lerp(
                    _camera.transform.position, desired, 1f - Mathf.Exp(-5f * dt));
                _camera.transform.LookAt(target + new Vector3(0f, 0f, 4f));

                // Shake: random offset scaled by trauma squared.
                if (_trauma > 0f)
                {
                    float s = _trauma * _trauma;
                    _camera.transform.position += new Vector3(
                        (Mathf.PerlinNoise(Time.realtimeSinceStartup * 47f, 0f) - 0.5f) * s,
                        (Mathf.PerlinNoise(0f, Time.realtimeSinceStartup * 39f) - 0.5f) * s,
                        0f);
                }
            }
        }

        public void AddShake(float amount) => _trauma = Mathf.Min(1f, _trauma + amount);

        public void SpawnGoo(Vector3 pos, int count)
        {
            Emit(pos, count, new Color(0.55f, 0f, 0f), 0.7f);
        }

        public void SpawnExplosion(Vector3 pos, int count)
        {
            Emit(pos, count, new Color(1f, 0.42f, 0f), 0.9f);
            Emit(pos, count / 2, new Color(1f, 0.85f, 0.2f), 0.5f);
        }

        public void SpawnPickupSparkle(Vector3 pos, int count)
        {
            Emit(pos, count, new Color(1f, 0.84f, 0f), 0.6f);
        }

        private void Emit(Vector3 pos, int count, Color color, float lifetime)
        {
            if (_particles == null || count <= 0) return;
            float scale = GameManager.Instance != null
                ? QualityData.Get(GameManager.Instance.State.Quality).ParticleScale
                : 1f;
            int scaled = Mathf.Max(1, (int)(count * scale));
            var emit = new ParticleSystem.EmitParams
            {
                position = pos,
                startColor = color,
                startLifetime = lifetime,
                startSize = 0.25f,
            };
            // Random directions: emit in a burst by varying the velocity per particle.
            for (int i = 0; i < scaled; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(1f, 4f);
                emit.velocity = new Vector3(Mathf.Cos(a) * speed, Random.Range(1f, 3f), Mathf.Sin(a) * speed);
                _particles.Emit(emit, 1);
            }
        }
    }
}
