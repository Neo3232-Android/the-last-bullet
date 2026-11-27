using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Audio
{
    /// <summary>
    /// Recorded-sound loader + player. Loads the 91 real sounds from
    /// Assets/_Project/Resources/Audio/ (SFX/ + Ambience/) lazily via
    /// Resources.Load, cached in a dictionary, and plays them through a
    /// small pool of AudioSources. Where several variants exist for one
    /// purpose (groans, deaths, flesh hits, pistol/shotgun/SMG/rifle shots,
    /// footsteps, coins, UI clicks) one is picked at random each time.
    ///
    /// Every Play* method returns true when a clip was found and played,
    /// false when it was missing — and NEVER throws, so a missing sound can
    /// never crash the game. SynthAudio calls these first and falls back to
    /// its procedural Tone() synth whenever one returns false.
    ///
    /// WIRING: add this component next to SynthAudio (same GameObject, e.g.
    /// in GameManager.Awake via gameObject.AddComponent&lt;RecordedAudio&gt;()).
    /// If this component is absent, SynthAudio silently falls back to synth.
    /// </summary>
    public sealed class RecordedAudio : MonoBehaviour
    {
        public static RecordedAudio Instance { get; private set; }

        private const string SfxPath = "Audio/SFX/";
        private const string AmbiencePath = "Audio/Ambience/";

        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private AudioSource _ambienceSrc;
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
            _ambienceSrc = gameObject.AddComponent<AudioSource>();
            _ambienceSrc.playOnAwake = false;
            _ambienceSrc.loop = true;
            _ambienceSrc.volume = 0.35f;
        }

        public void StopAll()
        {
            foreach (var src in _pool) src.Stop();
            if (_ambienceSrc != null) _ambienceSrc.Stop();
        }

        // ---- loading (lazy, cached, never throws) ----

        private AudioClip LoadSfx(string fileName)
        {
            return Load(SfxPath + fileName);
        }

        private AudioClip LoadAmbience(string fileName)
        {
            return Load(AmbiencePath + fileName);
        }

        private AudioClip Load(string resourcePath)
        {
            if (_cache.TryGetValue(resourcePath, out var cached)) return cached;
            AudioClip clip = null;
            try { clip = Resources.Load<AudioClip>(resourcePath); }
            catch (Exception) { clip = null; }
            _cache[resourcePath] = clip; // cache misses too: no repeated lookups
            return clip;
        }

        private AudioClip Pick(string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0) return null;
            string name = fileNames.Length == 1
                ? fileNames[0]
                : fileNames[UnityEngine.Random.Range(0, fileNames.Length)];
            return LoadSfx(name);
        }

        // ---- playback ----

        private bool PlayClip(AudioClip clip)
        {
            if (_muted || clip == null) return false;
            AudioSource free = null;
            foreach (var src in _pool)
            {
                if (!src.isPlaying) { free = src; break; }
            }
            if (free == null) free = _pool[0]; // all busy: steal the first
            free.pitch = UnityEngine.Random.Range(0.95f, 1.05f); // subtle variation per play
            free.PlayOneShot(clip);
            return true;
        }

        private bool PlaySfx(string fileName) => PlayClip(LoadSfx(fileName));

        private bool PlayVariant(params string[] fileNames) => PlayClip(Pick(fileNames));

        // ---- weapons (all 20 WeaponId values) ----

        public bool PlayWeaponShot(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Pistol: return PlayVariant("pistol_shot_1", "pistol_shot_2", "pistol_shot_3");
                case WeaponId.Shotgun: return PlayVariant("shotgun_blast_1", "shotgun_blast_2");
                case WeaponId.Smg: return PlayVariant("smg_burst_1", "smg_burst_2");
                case WeaponId.Rifle: return PlayVariant("rifle_shot_1", "rifle_shot_2");
                case WeaponId.Crossbow: return PlaySfx("crossbow_shot");
                case WeaponId.GrenadeLauncher: return PlaySfx("grenade_launcher_thump");
                case WeaponId.Flamethrower: return PlaySfx("flamethrower_whoosh");
                case WeaponId.Minigun: return PlaySfx("minigun_fire_1");
                case WeaponId.Railgun: return PlaySfx("railgun_fire");
                case WeaponId.FreezeRay: return PlaySfx("freeze_ray");
                case WeaponId.DualPistols: return PlaySfx("dual_pistols_shot");
                case WeaponId.Katana: return PlaySfx("katana_swing");
                case WeaponId.AcidGun: return PlaySfx("acid_spray");
                case WeaponId.TeslaCoil: return PlaySfx("tesla_zap");
                case WeaponId.HarpoonGun: return PlaySfx("harpoon_shot");
                case WeaponId.NailGun: return PlaySfx("nail_gun");
                case WeaponId.PlasmaCannon: return PlaySfx("plasma_shot");
                case WeaponId.SawbladeLauncher: return PlaySfx("sawblade_throw");
                case WeaponId.ToxicSprayer: return PlaySfx("toxic_spray");
                case WeaponId.BoomerangBlade: return PlaySfx("boomerang_whoosh");
                default: return false;
            }
        }

        public bool PlayReload() => PlaySfx("reload");
        public bool PlayDryFire() => PlaySfx("dry_fire_click");
        public bool PlayWeaponSwitch() => PlaySfx("weapon_switch");

        // ---- zombies (all 16 ZombieKind values) ----

        public bool PlayZombieGroan(ZombieKind kind)
        {
            switch (kind)
            {
                // Walker reuses the generic groan variants for variety.
                case ZombieKind.Walker: return PlayVariant("walker_groan", "zombie_groan_2", "zombie_groan_3", "zombie_groan_4");
                case ZombieKind.Runner: return PlaySfx("runner_snarl");
                case ZombieKind.Crawler: return PlaySfx("crawler_screech");
                case ZombieKind.Brute: return PlaySfx("brute_roar");
                case ZombieKind.Spitter: return PlaySfx("spitter_spit");
                case ZombieKind.Exploder: return PlaySfx("exploder_gurgle");
                case ZombieKind.Leaper: return PlaySfx("leaper_snarl");
                case ZombieKind.Screamer: return PlaySfx("screamer_shriek");
                case ZombieKind.ZombieDog: return PlaySfx("zombie_dog_growl");
                case ZombieKind.Armored: return PlaySfx("armored_clank");
                case ZombieKind.Bloater: return PlaySfx("bloater_gurgle");
                case ZombieKind.Healer: return PlaySfx("healer_chime");
                case ZombieKind.Shieldbearer: return PlaySfx("shieldbearer_clang");
                case ZombieKind.Ghost: return PlaySfx("ghost_whisper");
                case ZombieKind.FireZombie: return PlaySfx("fire_zombie_crackle");
                case ZombieKind.Spawner: return PlaySfx("spawner_growl");
                default: return PlayVariant("zombie_groan_2", "zombie_groan_3", "zombie_groan_4");
            }
        }

        /// <summary>Death cry for a zombie of the given kind. Exploders go out with a bang.</summary>
        public bool PlayZombieDeath(ZombieKind kind)
        {
            if (kind == ZombieKind.Exploder) return PlaySfx("explosion_big");
            return PlayZombieDeath();
        }

        /// <summary>Death cry, kind unknown — a random death variant.</summary>
        public bool PlayZombieDeath() => PlayVariant("zombie_death_1", "zombie_death_2", "zombie_death_3");

        // ---- bosses (all 5 BossKind values) ----

        public bool PlayBossRoar(BossKind kind)
        {
            switch (kind)
            {
                case BossKind.Charger: return PlaySfx("charger_roar");
                case BossKind.Summoner: return PlaySfx("summoner_roar");
                case BossKind.SpitterKing: return PlaySfx("spitter_king_roar");
                case BossKind.TheButcher: return PlaySfx("butcher_roar");
                case BossKind.PlagueQueen: return PlaySfx("plague_queen_roar");
                default: return false;
            }
        }

        public bool PlayBossDeath() => PlaySfx("boss_death");

        /// <summary>Boss intro sting (deep drum hit).</summary>
        public bool PlayBossSting() => PlaySfx("boss_sting");

        /// <summary>Full boss intro: sting + the boss's roar.</summary>
        public bool PlayBossIntro(BossKind kind)
        {
            bool sting = PlayBossSting();
            bool roar = PlayBossRoar(kind);
            return sting || roar;
        }

        // ---- combat / player ----

        public bool PlayFleshHit() => PlayVariant("flesh_hit_1", "flesh_hit_2", "flesh_hit_3");
        public bool PlayHeadshot() => PlaySfx("headshot_ding");
        public bool PlayHurt() => PlaySfx("hurt_grunt");
        public bool PlayPlayerDeath() => PlaySfx("player_death");
        public bool PlayDodgeRoll() => PlaySfx("dodge_roll");
        public bool PlayExplosion() => PlaySfx("explosion_big");

        public bool PlayFootstep(bool onGrass)
        {
            return onGrass
                ? PlayVariant("footstep_grass_1", "footstep_grass_2")
                : PlayVariant("footstep_concrete_1", "footstep_concrete_2");
        }

        // ---- pickups / progression ----

        public bool PlayCoin() => PlayVariant("coin_1", "coin_2");

        /// <summary>
        /// Pickup by kind string: "coin", "health", "ammo", "grenade",
        /// "powerup", "perk". Molotov / freeze bomb / insta-kill have no
        /// dedicated clip and use the generic power-up jingle. Unknown kinds
        /// return false so the caller can fall back.
        /// </summary>
        public bool PlayPickup(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return false;
            switch (kind.Trim().ToLowerInvariant())
            {
                case "coin":
                case "coins": return PlayCoin();
                case "health": return PlaySfx("health_pickup");
                case "ammo": return PlaySfx("ammo_pickup");
                case "grenade": return PlaySfx("grenade_pickup");
                case "powerup":
                case "power_up":
                case "power-up":
                case "molotov":
                case "freeze":
                case "freezebomb":
                case "freeze_bomb":
                case "instakill":
                case "insta_kill": return PlaySfx("power_up");
                case "perk": return PlaySfx("perk_pick");
                default: return false;
            }
        }

        public bool PlayLevelUp() => PlaySfx("level_up");
        public bool PlayPerkPick() => PlaySfx("perk_pick");
        public bool PlayKillStreak() => PlaySfx("kill_streak");

        // ---- events / UI ----

        public bool PlayWaveStart() => PlaySfx("wave_horn");
        public bool PlayHordeAlarm() => PlaySfx("horde_alarm");
        public bool PlaySupplyDrop() => PlaySfx("supply_whistle");
        public bool PlayLevelComplete() => PlaySfx("victory_fanfare");
        public bool PlayGameOver() => PlaySfx("defeat_sting");
        public bool PlayVictorySting() => PlaySfx("victory_fanfare");
        public bool PlayDefeatSting() => PlaySfx("defeat_sting");
        public bool PlayUiClick() => PlayVariant("ui_click_1", "ui_click_2");
        public bool PlayUiConfirm() => PlaySfx("ui_confirm");
        public bool PlayUiBack() => PlaySfx("ui_back");
        public bool PlayUiError() => PlaySfx("ui_error");

        // ---- ambience (looping, per-map) ----

        /// <summary>
        /// Starts the looping ambience for a map theme: "rain" -&gt; rain,
        /// "thunder"/"storm" -&gt; thunder rumble, anything else (incl. null)
        /// -&gt; night wind. Re-calling with the same theme is a no-op.
        /// Call StopAmbience() when returning to the menu.
        /// </summary>
        public bool PlayAmbience(string mapTheme)
        {
            if (_muted || _ambienceSrc == null) return false;
            string theme = (mapTheme ?? "").Trim().ToLowerInvariant();
            string file;
            if (theme == "rain" || theme == "rainstorm" || theme == "rain_storm") file = "rain_loop";
            else if (theme == "thunder" || theme == "storm") file = "thunder_rumble_loop";
            else file = "night_wind_loop"; // "night", "wind", unknown: night wind default

            AudioClip clip = LoadAmbience(file);
            if (clip == null) return false;
            if (_ambienceSrc.isPlaying && _ambienceSrc.clip == clip) return true;
            _ambienceSrc.clip = clip;
            _ambienceSrc.loop = true;
            _ambienceSrc.Play();
            return true;
        }

        public void StopAmbience()
        {
            if (_ambienceSrc == null) return;
            _ambienceSrc.Stop();
            _ambienceSrc.clip = null;
        }
    }
}
