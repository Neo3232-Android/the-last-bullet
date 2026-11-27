using UnityEngine;
using TheLastBullet.Audio;
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

        // ---- floating damage numbers (pooled TextMesh billboards) ----
        private const int DamageNumberPoolSize = 16;
        private const float DamageNumberLife = 0.8f;
        private sealed class DamageNumber
        {
            public TextMesh Text;
            public float Life;
            public Color BaseColor;
        }
        private DamageNumber[] _damageNumbers;
        private int _damageNumberCursor;

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

            TickDamageNumbers(dt);
        }

        /// <summary>Floating damage number over a hit (crits are gold, bigger).</summary>
        public void SpawnDamageNumber(Vector3 pos, int amount, bool isCrit)
        {
            if (amount <= 0) return;
            EnsureDamageNumberPool();
            if (_damageNumbers == null) return;
            var slot = _damageNumbers[_damageNumberCursor];
            _damageNumberCursor = (_damageNumberCursor + 1) % DamageNumberPoolSize;
            if (slot == null || slot.Text == null) return;
            slot.Text.text = amount.ToString();
            slot.BaseColor = isCrit ? new Color(1f, 0.84f, 0f) : Color.white;
            slot.Text.color = slot.BaseColor;
            slot.Text.characterSize = isCrit ? 0.11f : 0.08f;
            // Small random offset so overlapping hits don't stack exactly.
            var rng = GameManager.Instance != null ? GameManager.Instance.Rng : null;
            float ox = rng != null ? (float)(rng.NextDouble() - 0.5) * 0.6f : 0f;
            float oz = rng != null ? (float)(rng.NextDouble() - 0.5) * 0.6f : 0f;
            slot.Text.transform.position = pos + new Vector3(ox, 1.6f, oz);
            slot.Life = DamageNumberLife;
            slot.Text.gameObject.SetActive(true);
        }

        private void EnsureDamageNumberPool()
        {
            if (_damageNumbers != null) return;
            _damageNumbers = new DamageNumber[DamageNumberPoolSize];
            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            for (int i = 0; i < DamageNumberPoolSize; i++)
            {
                var go = new GameObject("DamageNumber_" + i);
                go.transform.SetParent(transform, false);
                var tm = go.AddComponent<TextMesh>();
                tm.font = font;
                tm.fontSize = 48;
                tm.characterSize = 0.08f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = Color.white;
                go.SetActive(false);
                _damageNumbers[i] = new DamageNumber { Text = tm, Life = 0f, BaseColor = Color.white };
            }
        }

        private void TickDamageNumbers(float dt)
        {
            if (_damageNumbers == null) return;
            foreach (var slot in _damageNumbers)
            {
                if (slot == null || slot.Text == null || slot.Life <= 0f) continue;
                slot.Life -= dt;
                if (slot.Life <= 0f)
                {
                    slot.Text.gameObject.SetActive(false);
                    continue;
                }
                // Rise + fade; billboard to face the camera.
                slot.Text.transform.position += new Vector3(0f, 1.2f * dt, 0f);
                if (_camera != null)
                    slot.Text.transform.rotation = _camera.transform.rotation;
                float a = Mathf.Clamp01(slot.Life / DamageNumberLife);
                var c = slot.BaseColor;
                slot.Text.color = new Color(c.r, c.g, c.b, a);
            }
        }

        public void AddShake(float amount) => _trauma = Mathf.Min(1f, _trauma + amount);

        /// <summary>Last kill noted via NoteKill (HUD combo-feed hook — data only, no UI).</summary>
        public bool LastKillHeadshot { get; private set; }
        public int LastKillStreak { get; private set; }
        public float LastKillTime { get; private set; }
        public int KillsNoted { get; private set; }

        /// <summary>
        /// Kill-feed / combo hook. The HUD workstream reads LastKillHeadshot /
        /// LastKillStreak / LastKillTime; this method only stores the data.
        /// </summary>
        public void NoteKill(bool headshot, int streak)
        {
            LastKillHeadshot = headshot;
            LastKillStreak = streak;
            LastKillTime = Time.realtimeSinceStartup;
            KillsNoted++;
        }

        /// <summary>
        /// Hit-stop: freezes the frame briefly. Delegates to GameManager, which
        /// owns the resume timer (GameManager.Update restores timeScale) — so
        /// this can never fight the slow-mo / hit-stop state machine.
        /// </summary>
        public void AddHitStop(float seconds)
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.TriggerHitStop(seconds);
            // No GameManager: do nothing. Setting timeScale = 0 here with no
            // owner to restore it would freeze the game permanently.
        }

        /// <summary>
        /// Boss arrival: camera punches in, the screen shakes, dust kicks up,
        /// and the boss's intro sting + roar plays (SynthAudio.PlayBossIntro:
        /// recorded roar per BossKind, synth fallback).
        /// </summary>
        public void BossIntro(Vector3 bossPos, BossKind? kind = null)
        {
            ZoomPunch = 1f;
            AddShake(0.6f);
            SpawnDust(bossPos + Vector3.up * 0.5f, 10);
            if (kind.HasValue)
            {
                var audio = SynthAudio.Instance;
                if (audio != null) audio.PlayBossIntro(kind.Value);
            }
        }

        /// <summary>Boss kill: slow-mo + zoom punch + big shake.</summary>
        public void BossKillSlowMo()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.TriggerSlowMo(1.2f);
            ZoomPunch = 1f;
            AddShake(1f);
        }

        /// <summary>Grey dust puff (spawn surfacing, landings, footsteps).</summary>
        public void SpawnDust(Vector3 pos, int count)
        {
            Emit(pos, count, new Color(0.6f, 0.58f, 0.55f), 0.8f, 0.3f);
        }

        /// <summary>Small grey puff for a footstep. Call site: whoever drives the walk cycle.</summary>
        public void SpawnDustStep(Vector3 pos)
        {
            Emit(pos, 3, new Color(0.6f, 0.58f, 0.55f), 0.5f, 0.22f);
        }

        /// <summary>Bright red impact burst — distinct from the darker goo.</summary>
        public void SpawnBloodHit(Vector3 pos)
        {
            Emit(pos, 8, new Color(0.75f, 0.05f, 0.05f), 0.6f, 0.2f);
        }

        /// <summary>White/blue ice shards for a frozen zombie shattering.</summary>
        public void SpawnShatter(Vector3 pos, int count)
        {
            Emit(pos, count, new Color(0.75f, 0.9f, 1f), 0.7f, 0.22f);
            Emit(pos, count / 2, new Color(0.5f, 0.75f, 1f), 0.5f, 0.18f);
        }

        /// <summary>Tiny yellow burst, low arc — a spent casing hitting the floor.</summary>
        public void SpawnShellCasing(Vector3 pos)
        {
            Emit(pos, 3, new Color(1f, 0.8f, 0.2f), 0.4f, 0.08f, 0.25f);
        }

        /// <summary>Bright yellow-white flash at the muzzle. Size scales with the weapon.</summary>
        public void SpawnMuzzleFlash(Vector3 pos, float size)
        {
            Emit(pos, 6, new Color(1f, 0.95f, 0.6f), 0.15f, size);
        }

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

        private void Emit(Vector3 pos, int count, Color color, float lifetime, float size = 0.25f, float upBias = 1f)
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
                startSize = size,
            };
            // Random directions: emit in a burst by varying the velocity per particle.
            for (int i = 0; i < scaled; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(1f, 4f);
                emit.velocity = new Vector3(Mathf.Cos(a) * speed, Random.Range(1f, 3f) * upBias, Mathf.Sin(a) * speed);
                _particles.Emit(emit, 1);
            }
        }
    }
}
