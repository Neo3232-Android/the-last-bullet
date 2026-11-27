using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Audio
{
    /// <summary>
    /// Procedural sound synth — port of the Kotlin synth. Every sound is
    /// generated in code with AudioClip.Create (sine/noise envelopes), so the
    /// game ships zero audio files. A tiny pool of AudioSources plays them;
    /// nothing here needs a serialized asset, so it works headless too.
    /// </summary>
    public sealed class SynthAudio : MonoBehaviour
    {
        public static SynthAudio Instance { get; private set; }

        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private bool _muted;

        public bool Muted
        {
            get => _muted;
            set { _muted = value; if (_muted) StopAll(); }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            for (int i = 0; i < 8; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool.Add(src);
            }
        }

        private void StopAll()
        {
            foreach (var src in _pool) src.Stop();
        }

        // ---- public API (called by GameManager / PlayerController / WeaponSystem / Bullet) ----

        public void Shoot(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Shotgun: Play("shotgun", Tone(140f, 0.18f, 0.5f, NoiseMix: 0.7f)); break;
                case WeaponId.Katana: Play("katana", Tone(900f, 0.12f, 0.3f, SlideTo: 300f)); break;
                case WeaponId.Flamethrower: Play("flame", Tone(220f, 0.15f, 0.35f, NoiseMix: 0.9f)); break;
                case WeaponId.GrenadeLauncher: Play("grenade", Tone(90f, 0.3f, 0.6f, NoiseMix: 0.4f)); break;
                case WeaponId.FreezeRay: Play("freeze", Tone(1200f, 0.2f, 0.25f, SlideTo: 1800f)); break;
                case WeaponId.Minigun: Play("minigun", Tone(180f, 0.07f, 0.4f, NoiseMix: 0.5f)); break;
                case WeaponId.Railgun: Play("railgun", Tone(1500f, 0.25f, 0.5f, SlideTo: 120f)); break;
                case WeaponId.TeslaCoil: Play("tesla", Tone(700f, 0.15f, 0.4f, SlideTo: 200f, NoiseMix: 0.6f)); break;
                case WeaponId.SawbladeLauncher: Play("saw", Tone(400f, 0.12f, 0.35f, SlideTo: 900f)); break;
                case WeaponId.BoomerangBlade: Play("boomerang", Tone(500f, 0.12f, 0.3f, SlideTo: 1000f)); break;
                case WeaponId.AcidGun: Play("acid", Tone(300f, 0.15f, 0.3f, SlideTo: 150f, NoiseMix: 0.5f)); break;
                case WeaponId.ToxicSprayer: Play("toxic", Tone(260f, 0.18f, 0.3f, NoiseMix: 0.7f)); break;
                case WeaponId.PlasmaCannon: Play("plasma", Tone(200f, 0.3f, 0.6f, SlideTo: 80f, NoiseMix: 0.3f)); break;
                case WeaponId.Smg: Play("smg", Tone(220f, 0.08f, 0.4f, NoiseMix: 0.5f)); break;
                case WeaponId.Rifle: Play("rifle", Tone(160f, 0.14f, 0.45f, NoiseMix: 0.6f)); break;
                case WeaponId.Crossbow: Play("crossbow", Tone(600f, 0.1f, 0.35f, SlideTo: 200f)); break;
                case WeaponId.DualPistols: Play("dual", Tone(190f, 0.1f, 0.4f, NoiseMix: 0.5f)); break;
                default: Play("pistol", Tone(200f, 0.1f, 0.4f, NoiseMix: 0.5f)); break; // Pistol
            }
        }

        public void Reload() => Play("reload", Tone(500f, 0.18f, 0.3f, SlideTo: 900f));
        public void Hit() => Play("hit", Tone(110f, 0.12f, 0.5f, NoiseMix: 0.4f));
        public void ZombieDie() => Play("zdie", Tone(160f, 0.25f, 0.4f, SlideTo: 60f, NoiseMix: 0.4f));
        public void BossDie() => Play("bdie", Tone(80f, 0.6f, 0.7f, SlideTo: 30f, NoiseMix: 0.5f));
        public void Pickup() => Play("pickup", Tone(660f, 0.12f, 0.3f, SlideTo: 990f));
        public void WaveStart() => Play("wave", Tone(330f, 0.25f, 0.4f, SlideTo: 660f));
        public void LevelComplete() => Play("level", Tone(523f, 0.4f, 0.4f, SlideTo: 784f));
        public void GameOver() => Play("over", Tone(220f, 0.8f, 0.5f, SlideTo: 55f));
        public void Headshot() => Play("headshot", Tone(1400f, 0.08f, 0.35f, SlideTo: 700f));
        public void UiClick() => Play("click", Tone(800f, 0.05f, 0.25f));

        // ---- synthesis ----

        /// <summary>
        /// Builds (and caches) a short clip: a sine tone sliding from
        /// freq to slideTo, mixed with optional noise, with an exponential
        /// decay envelope. Cached by a stable key per call site.
        /// </summary>
        private AudioClip Tone(float freq, float duration, float volume,
            float SlideTo = -1f, float NoiseMix = 0f)
        {
            string key = $"tone_{freq}_{duration}_{volume}_{SlideTo}_{NoiseMix}";
            if (_clips.TryGetValue(key, out var cached)) return cached;

            int sampleRate = 44100;
            int samples = Mathf.Max(1, (int)(sampleRate * duration));
            var clip = AudioClip.Create(key, samples, 1, sampleRate, false);
            var data = new float[samples];
            float endFreq = SlideTo > 0f ? SlideTo : freq;
            var rng = new System.Random(key.GetHashCode());
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float f = Mathf.Lerp(freq, endFreq, t);
                phase += 2f * Mathf.PI * f / sampleRate;
                float env = Mathf.Exp(-4f * t);
                float sine = Mathf.Sin(phase);
                float noise = (float)(rng.NextDouble() * 2 - 1);
                data[i] = (sine * (1f - NoiseMix) + noise * NoiseMix) * env * volume;
            }
            clip.SetData(data, 0);
            _clips[key] = clip;
            return clip;
        }

        private void Play(string key, AudioClip clip)
        {
            if (_muted || clip == null) return;
            foreach (var src in _pool)
            {
                if (!src.isPlaying)
                {
                    src.clip = clip;
                    src.Play();
                    return;
                }
            }
            // All busy: steal the first source (short sounds, inaudible).
            _pool[0].clip = clip;
            _pool[0].Play();
        }
    }
}
