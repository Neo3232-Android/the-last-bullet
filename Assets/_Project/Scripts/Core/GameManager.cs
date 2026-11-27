using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Audio;
using TheLastBullet.Data;
using TheLastBullet.Gameplay;
using TheLastBullet.UI;

namespace TheLastBullet.Core
{
    /// <summary>
    /// The simulation owner. Port of the Kotlin GameEngine — same rules, same
    /// numbers — rewritten for Unity: entities are GameObjects, the fixed
    /// 1/60s step runs in FixedUpdate, and juice (slow-mo, hit-stop, shake)
    /// drives Time.timeScale / the camera rig.
    ///
    /// Coordinate map: the Kotlin engine's 2D (x, y) becomes Unity (x, 0, z).
    /// A Kotlin angle a (radians, atan2(y, x)) becomes a yaw of 90 - a degrees.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const float Step = 1f / 60f;
        public const float TimedModeDurationSec = 300f;
        public const float DeathAnimSec = 0.6f;
        public const float SpawnRiseSec = 0.5f;
        public const float BaseCritDamageMult = 2f;
        public const int XpPerKill = 10;
        public const int XpPerBossKill = 100;
        public const float KillStreakWindowSec = 4f;

        public GameState State { get; private set; } = new GameState();
        public LevelDef CurrentLevel { get; private set; }
        public LevelDirector LevelDirector { get; private set; }
        public PlayerController Player { get; private set; }
        public Camera MainCamera { get; private set; }
        public JuiceController Juice { get; private set; }
        public System.Random Rng { get; private set; } = new System.Random();

        public List<ZombieAI> Zombies { get; } = new List<ZombieAI>();
        public List<Bullet> Bullets { get; } = new List<Bullet>();
        public List<Pickup> Pickups { get; } = new List<Pickup>();
        public List<BarricadeEntity> Barricades { get; } = new List<BarricadeEntity>();
        public List<TurretEntity> Turrets { get; } = new List<TurretEntity>();
        public List<TrapEntity> Traps { get; } = new List<TrapEntity>();

        public int Score { get; private set; }
        public int RunCoins { get; private set; }
        public int Kills { get; private set; }
        public int Headshots { get; private set; }
        public int ShotsFired { get; private set; }
        public int ShotsHit { get; private set; }
        public int KillStreak { get; private set; }
        public List<PerkId> PendingPerkChoices { get; private set; } = new List<PerkId>();
        public bool Won { get; set; }
        public bool IsSlowMo { get; private set; }

        public float ParticleScale { get; private set; } = 1f;
        public int EntityCap { get; private set; } = 110;

        private float _simTime;
        private float _slowMoUntil;
        private float _instaKillUntil;
        private float _coinMultiplierUntil;
        private float _streakTimer;
        private float _hitStopUntil;
        private float _fpsEstimate = 60f;
        private bool _paused;
        private bool _secondWindUsed;
        private MapDef _map;
        private Vector3 _lastKillPos;
        private bool _hasLastKillPos;

        // ---- lifecycle ----

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LevelDirector = gameObject.AddComponent<LevelDirector>();
            EnsureCameraRig();
            QualitySettingsApplier.Apply(
                SaveSystem.Instance != null ? SaveSystem.Instance.QualitySetting : Quality.High);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.0001f)
            {
                float instantFps = 1f / dt;
                _fpsEstimate = _fpsEstimate * 0.95f + instantFps * 0.05f;
            }
            // Hit-stop: real-time freeze, always ends.
            if (Time.timeScale == 0f && Time.realtimeSinceStartup >= _hitStopUntil)
                Time.timeScale = IsSlowMo ? 0.4f : 1f;
            if (IsSlowMo && Time.timeScale > 0f && _simTime >= _slowMoUntil)
            {
                IsSlowMo = false;
                Time.timeScale = 1f;
            }
            if (Juice != null) Juice.Tick(dt);
        }

        private void FixedUpdate()
        {
            if (_paused) return;
            if (State.Phase != GamePhase.Playing) return;
            if (PendingPerkChoices.Count > 0) return;

            _simTime += Step;
            State.TimeSurvivedSec += Step;
            if (_streakTimer > 0f)
            {
                _streakTimer -= Step;
                if (_streakTimer <= 0f) KillStreak = 0;
            }
            if (State.Mode == GameMode.Timed && State.TimeSurvivedSec >= TimedModeDurationSec)
            {
                Won = true;
                EndRun();
                return;
            }

            LevelDirector.Tick(Step);
            UpdateTurrets(Step);
            UpdateTraps(Step);
            UpdatePickups(Step);
            SeparateZombies();
            CleanupCorpses();

            if (LevelDirector.IsWaveQueueEmpty && AliveZombieCount() == 0 &&
                State.Phase == GamePhase.Playing)
            {
                int bonus = 100 + State.Wave * 10;
                Score += bonus;
                LevelDirector.OnWaveCleared();
            }
        }

        public float FpsEstimate => _fpsEstimate;
        public float SimTime => _simTime;
        public bool IsPaused => _paused;
        public ZombieData.DailyMutation TodayMutation { get; private set; }
        public bool IsInstaKillActive => _simTime < _instaKillUntil;
        public void RegisterHeadshot() => Headshots++;
        public void AddCoins(int amount) { RunCoins += amount; SyncCoins(); }

        public float CoinMultiplierNow()
        {
            float mult = TodayMutation != null ? TodayMutation.CoinMultiplier : 1f;
            if (_simTime < _coinMultiplierUntil) mult *= 2f;
            return mult;
        }

        public void SetCoinMultiplier(float mult, float durationSec)
        {
            if (mult > 1f) _coinMultiplierUntil = _simTime + durationSec;
        }
        public Vector3 LastKillPos => _lastKillPos;
        public bool HasLastKillPos => _hasLastKillPos;

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;

        public int AliveZombieCount()
        {
            int n = 0;
            foreach (var z in Zombies)
                if (z.IsAlive) n++;
            return n;
        }

        public void ApplyQualityCaps(QualityDef def)
        {
            ParticleScale = def.ParticleScale;
            EntityCap = def.EntityCap;
        }

        // ---- run setup ----

        public void StartCampaignLevel(MapId mapId, int level)
        {
            _map = MapData.Get(mapId);
            CurrentLevel = LevelData.GetLevel(mapId, level);
            StartRun(GameMode.Endless, mapId);
            LevelDirector.StartLevel(CurrentLevel);
        }

        public void StartEndless(MapId mapId)
        {
            _map = MapData.Get(mapId);
            CurrentLevel = null;
            StartRun(GameMode.Endless, mapId);
            LevelDirector.StartEndless(_map);
        }

        private void StartRun(GameMode mode, MapId mapId)
        {
            Rng = new System.Random(Guid.NewGuid().GetHashCode());
            ClearEntities();
            Score = 0; RunCoins = 0; Kills = 0; Headshots = 0;
            ShotsFired = 0; ShotsHit = 0; KillStreak = 0; _streakTimer = 0f;
            PendingPerkChoices = new List<PerkId>();
            Won = false; IsSlowMo = false; Time.timeScale = 1f;
            _simTime = 0f; _slowMoUntil = 0f; _instaKillUntil = 0f; _coinMultiplierUntil = 0f;
            TodayMutation = ZombieData.MutationForDay(
                (int)(DateTime.UtcNow.Date - new DateTime(2026, 1, 1)).TotalDays);
            _secondWindUsed = false; _hasLastKillPos = false;

            var profile = SaveSystem.Instance != null ? SaveSystem.Instance.Profile : new PlayerProfile();
            var character = SaveSystem.Instance != null ? SaveSystem.Instance.Character : CharacterDef.Default();
            var ps = new PlayerState
            {
                Health = 100,
                MaxHealth = 100,
                MoveSpeed = 3.0f,
                WeaponId = character.StartingWeaponId,
                Perks = new Dictionary<PerkId, int> { { character.StartingPerkId, 1 } },
            };
            State = new GameState
            {
                Phase = GamePhase.Playing,
                Mode = mode,
                MapId = mapId,
                Level = CurrentLevel != null ? CurrentLevel.Level : 1,
                Wave = 1,
                Player = ps,
                Quality = SaveSystem.Instance != null ? SaveSystem.Instance.QualitySetting : Quality.High,
            };
            if (profile.PrestigeLevel > 0)
            {
                // Prestige damage bonus applies through the damage multiplier path.
                State.Player.DamageBonus = profile.PrestigeDamageBonus() - 1f;
            }
            SpawnPlayer(ps, character);
        }

        private void ClearEntities()
        {
            foreach (var z in Zombies)
                if (z != null) Destroy(z.gameObject);
            Zombies.Clear();
            foreach (var b in Bullets)
                if (b != null) Destroy(b.gameObject);
            Bullets.Clear();
            foreach (var p in Pickups)
                if (p != null) Destroy(p.gameObject);
            Pickups.Clear();
            foreach (var b in Barricades) b.DestroyView();
            Barricades.Clear();
            foreach (var t in Turrets) t.DestroyView();
            Turrets.Clear();
            foreach (var t in Traps) t.DestroyView();
            Traps.Clear();
            if (Player != null) Destroy(Player.gameObject);
            Player = null;
        }

        private void SpawnPlayer(PlayerState ps, CharacterDef character)
        {
            var go = new GameObject("Player");
            go.layer = LayerMask.NameToLayer("Player");
            go.transform.position = Vector3.zero;
            Player = go.AddComponent<PlayerController>();
            Player.Setup(ps, character);
        }

        private void EnsureCameraRig()
        {
            if (Camera.main != null)
            {
                MainCamera = Camera.main;
            }
            else
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                MainCamera = camGo.AddComponent<Camera>();
                MainCamera.clearFlags = CameraClearFlags.SolidColor;
                MainCamera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            }
            Juice = MainCamera.GetComponent<JuiceController>();
            if (Juice == null) Juice = MainCamera.gameObject.AddComponent<JuiceController>();

            if (FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            // EventSystem for UI input (created once, persists).
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                DontDestroyOnLoad(es);
            }
        }

        // ---- combat resolution (port of the Kotlin engine) ----

        public static float AngleToYaw(float angleRad) => 90f - angleRad * Mathf.Rad2Deg;

        public static Vector3 AngleToDir(float angleRad) =>
            new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));

        public bool RollCrit()
        {
            float chance = State.Player.CritChance();
            if (State.Player.Mods.Contains(WeaponMod.Scope)) chance += 0.10f;
            return Rng.NextDouble() < chance;
        }

        public float ScoreMultiplier() => Mathf.Min(1f + KillStreak * 0.05f, 3f);

        public void RegisterShotFired() => ShotsFired++;
        public void RegisterShotHit() => ShotsHit++;

        public void DamageZombie(ZombieAI z, float amount, bool isCrit, Vector3 hitDir)
        {
            if (z == null || !z.IsAlive) return;
            if (z.BossIntroTimer > 0f) return; // boss intro: roaring, cannot be damaged yet
            z.Health -= amount;
            z.HitFlashUntil = _simTime + 0.08f;
            if (z.Health > 0f) return;
            KillZombie(z, isCrit, hitDir);
        }

        private void KillZombie(ZombieAI z, bool isCrit, Vector3 hitDir)
        {
            z.BeginDeath();
            int scoreValue = z.IsBoss
                ? ZombieData.GetBoss(z.BossKind.Value).ScoreValue
                : ZombieData.Get(z.Kind.Value).ScoreValue;
            int coinValue = z.IsBoss
                ? ZombieData.GetBoss(z.BossKind.Value).CoinValue
                : ZombieData.Get(z.Kind.Value).CoinValue;
            Score += (int)(scoreValue * ScoreMultiplier());
            RunCoins += (int)(coinValue * CoinMultiplierNow());
            Kills++;
            KillStreak++;
            _streakTimer = KillStreakWindowSec;
            GrantXp(z.IsBoss ? XpPerBossKill : XpPerKill);
            _lastKillPos = z.transform.position;
            _hasLastKillPos = true;
            TriggerHitStop(z.IsBoss ? 0.08f : 0.03f);
            Juice.SpawnGoo(z.transform.position, z.IsBoss ? 24 : 8);
            if (z.IsBoss)
            {
                TriggerSlowMo(1.2f);
                Juice.AddShake(1f);
                SynthAudio.Instance.BossDie();
            }
            else
            {
                SynthAudio.Instance.ZombieDie();
            }
            if (z.Kind.HasValue && z.Kind.Value == ZombieKind.Exploder)
            {
                Juice.AddShake(0.5f);
                ExplodeAt(z.transform.position, 2f, 30f);
            }
            // Vampire super perk: every kill heals 1 HP.
            if (SaveSystem.Instance != null &&
                SaveSystem.Instance.Profile.SuperPerksUnlocked.Contains("vampire") &&
                Player != null && Player.IsAlive)
            {
                Player.Heal(1);
            }
            MaybeDropPickup(z.transform.position);
            SyncCoins();
        }

        public void ExplodeAt(Vector3 pos, float radius, float damage)
        {
            Juice.SpawnExplosion(pos, 14);
            Juice.AddShake(0.35f);
            foreach (var z in Zombies)
            {
                if (!z.IsAlive) continue;
                if (Vector3.Distance(z.transform.position, pos) < radius + z.Radius)
                    DamageZombie(z, damage, false, (z.transform.position - pos).normalized);
            }
        }

        public void DamagePlayer(int amount)
        {
            if (Player == null || !Player.IsAlive) return;
            if (Player.DodgeRollTimer > 0f) return; // dodge roll grants brief invulnerability
            Player.Health -= amount;
            Player.HitFlashUntil = _simTime + 0.1f;
            KillStreak = 0;
            _streakTimer = 0f;
            Juice.AddShake(0.4f);
            SynthAudio.Instance.Hit();
            if (Player.Health <= 0)
            {
                // Second Wind super perk: once per run, revive at 50% HP.
                if (!_secondWindUsed && SaveSystem.Instance != null &&
                    SaveSystem.Instance.Profile.SuperPerksUnlocked.Contains("second-wind"))
                {
                    _secondWindUsed = true;
                    Player.Health = Player.MaxHealth / 2;
                    TriggerSlowMo(1f);
                    Juice.SpawnGoo(Player.transform.position, 20);
                    return;
                }
                Player.Health = 0;
                Player.Die();
                EndRun();
            }
        }

        private void GrantXp(int amount)
        {
            var ps = State.Player;
            int xp = ps.Xp + amount;
            int level = ps.Level;
            bool leveled = false;
            while (xp >= PerkData.XpForLevel(level))
            {
                xp -= PerkData.XpForLevel(level);
                level++;
                leveled = true;
            }
            ps.Xp = xp;
            ps.Level = level;
            ps.Kills = Kills;
            ps.Headshots = Headshots;
            if (leveled) PendingPerkChoices = RollPerkChoices();
        }

        private List<PerkId> RollPerkChoices()
        {
            var ps = State.Player;
            var available = new List<PerkId>();
            foreach (PerkId id in Enum.GetValues(typeof(PerkId)))
                if (ps.PerkStacks(id) < PerkData.Get(id).MaxStacks)
                    available.Add(id);
            // Fisher-Yates shuffle with the run seed.
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                var tmp = available[i];
                available[i] = available[j];
                available[j] = tmp;
            }
            return available.GetRange(0, Math.Min(3, available.Count));
        }

        public bool ApplyPerk(PerkId id)
        {
            if (!PendingPerkChoices.Contains(id)) return false;
            var ps = State.Player;
            int stacks = ps.PerkStacks(id) + 1;
            ps.Perks[id] = stacks;
            if (id == PerkId.MaxHealth)
            {
                ps.MaxHealth += 25;
                ps.Health = Math.Min(ps.Health + 25, ps.MaxHealth);
                if (Player != null)
                {
                    Player.MaxHealth = ps.MaxHealth;
                    Player.Health = ps.Health;
                }
            }
            PendingPerkChoices = new List<PerkId>();
            SynthAudio.Instance.UiClick();
            return true;
        }

        public void TriggerSlowMo(float duration)
        {
            _slowMoUntil = _simTime + duration;
            IsSlowMo = true;
            if (Time.timeScale > 0f) Time.timeScale = 0.4f;
        }

        public void TriggerHitStop(float duration)
        {
            _hitStopUntil = Time.realtimeSinceStartup + duration;
            Time.timeScale = 0f;
        }

        private void MaybeDropPickup(Vector3 pos)
        {
            if (State.Mode == GameMode.Hardcore) return;
            double roll = Rng.NextDouble();
            PickupKind kind;
            if (roll < 0.04) kind = PickupKind.Health;
            else if (roll < 0.08) kind = PickupKind.Ammo;
            else if (roll < 0.09) kind = PickupKind.Grenade;
            else if (roll < 0.10) kind = PickupKind.FreezeBomb;
            else if (roll < 0.105) kind = PickupKind.InstaKill;
            else return;
            SpawnPickup(kind, pos);
        }

        public void SpawnPickup(PickupKind kind, Vector3 pos)
        {
            var go = new GameObject("Pickup_" + kind);
            go.layer = LayerMask.NameToLayer("Pickup");
            go.transform.position = pos;
            var pickup = go.AddComponent<Pickup>();
            pickup.Setup(kind);
            Pickups.Add(pickup);
        }

        public void SpawnZombie(ZombieKind kind, Vector3 pos, bool isElite, float healthScale, float speedScale)
        {
            if (AliveZombieCount() >= EntityCap) return;
            if (TodayMutation != null)
            {
                healthScale *= TodayMutation.ZombieHealthMultiplier;
                speedScale *= TodayMutation.ZombieSpeedMultiplier;
            }
            var def = ZombieData.Get(kind);
            var go = new GameObject("Zombie_" + kind);
            go.layer = LayerMask.NameToLayer("Zombie");
            go.transform.position = pos;
            var zombie = go.AddComponent<ZombieAI>();
            zombie.Setup(kind, def, healthScale, speedScale, isElite);
            Zombies.Add(zombie);
        }

        public void SpawnBoss(BossKind kind, Vector3 pos, float healthScale)
        {
            var def = ZombieData.GetBoss(kind);
            var go = new GameObject("Boss_" + kind);
            go.layer = LayerMask.NameToLayer("Zombie");
            go.transform.position = pos;
            var zombie = go.AddComponent<ZombieAI>();
            zombie.Setup(kind, def, healthScale);
            zombie.BossIntroTimer = 2f; // roars for 2s, cannot be damaged yet, camera punches in
            if (Juice != null) Juice.ZoomPunch = 1f;
            Zombies.Add(zombie);
        }

        /// <summary>
        /// Summon a zombie right next to a position (Screamer scream, Summoner
        /// and Plague Queen specials, Graveyard hazard). Forced kind or a
        /// random kind from the current map's mix.
        /// </summary>
        public void SummonNear(Vector3 pos, ZombieKind? forcedKind)
        {
            var map = LevelDirector != null ? LevelDirector.CurrentMap : null;
            ZombieKind kind;
            if (forcedKind.HasValue)
            {
                kind = forcedKind.Value;
            }
            else if (map != null && map.ZombieMix.Count > 0)
            {
                var kinds = new List<ZombieKind>(map.ZombieMix.Keys);
                kind = kinds[Rng.Next(kinds.Count)];
            }
            else
            {
                kind = ZombieKind.Walker;
            }
            float angle = (float)(Rng.NextDouble() * Math.PI * 2);
            Vector3 spawnPos = pos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.5f;
            float healthScale = CurrentLevel != null ? CurrentLevel.HealthScale : 1f;
            SpawnZombie(kind, spawnPos, false, healthScale, 1f);
        }

        public void SpawnEnemyBullet(Vector3 pos, float angleRad, float speed, float damage, float lifeSec)
        {
            var go = new GameObject("EnemyBullet");
            go.layer = LayerMask.NameToLayer("Bullet");
            go.transform.position = pos;
            var bullet = go.AddComponent<Bullet>();
            bullet.Launch(pos, AngleToDir(angleRad) * speed, damage, 0, false,
                false, 0f, 0f, lifeSec, false);
            Bullets.Add(bullet);
        }

        // ---- buildables (port of the Kotlin shop actions) ----

        public bool BuyWeapon(WeaponId id)
        {
            var def = WeaponData.Get(id);
            if (RunCoins < def.Price) return false;
            RunCoins -= def.Price;
            if (Player == null) return false;
            if (State.Player.HasSecondWeapon() && Player.SecondaryWeaponId == null)
            {
                Player.SecondaryWeaponId = id;
            }
            else
            {
                Player.SecondaryWeaponId = Player.WeaponId;
                Player.WeaponId = id;
                Player.AmmoInMag = EffectiveMagSize(id);
                Player.ReloadingUntil = 0f;
            }
            State.Player.WeaponId = Player.WeaponId;
            State.Player.SecondaryWeaponId = Player.SecondaryWeaponId;
            SyncCoins();
            SynthAudio.Instance.UiClick();
            return true;
        }

        public bool SwapWeapon()
        {
            if (Player == null || Player.SecondaryWeaponId == null) return false;
            if (!State.Player.HasSecondWeapon()) return false;
            var secondary = Player.SecondaryWeaponId.Value;
            Player.SecondaryWeaponId = Player.WeaponId;
            Player.WeaponId = secondary;
            Player.AmmoInMag = EffectiveMagSize(secondary);
            Player.ReloadingUntil = 0f;
            Player.WeaponSwapTimer = 0.4f;
            State.Player.WeaponId = Player.WeaponId;
            State.Player.SecondaryWeaponId = Player.SecondaryWeaponId;
            SynthAudio.Instance.Reload();
            return true;
        }

        public int EffectiveMagSize(WeaponId weaponId)
        {
            var def = WeaponData.Get(weaponId);
            if (def.MagSize <= 0) return 0;
            int size = (int)(def.MagSize * State.Player.MagMultiplier());
            if (State.Player.Mods.Contains(WeaponMod.ExtendedMag)) size = (int)(size * 1.5f);
            return Math.Max(1, size);
        }

        public float EffectiveReloadSec(WeaponDef def)
        {
            float t = def.ReloadTimeSec * State.Player.ReloadMultiplier();
            if (State.Player.Mods.Contains(WeaponMod.FasterReload)) t *= 0.7f;
            return t;
        }

        public float EffectiveSpreadDeg(WeaponDef def)
        {
            float s = def.SpreadDegrees;
            if (State.Player.Mods.Contains(WeaponMod.Scope)) s *= 0.5f;
            return s;
        }

        public bool PlaceBarricade(Vector3 pos, int tier = 1)
        {
            int cost = 100 * tier;
            if (RunCoins < cost) return false;
            RunCoins -= cost;
            float health = tier >= 2 ? 300f : 150f;
            Barricades.Add(new BarricadeEntity(pos, health, tier));
            SyncCoins();
            SynthAudio.Instance.UiClick();
            return true;
        }

        public bool PlaceTurret(Vector3 pos)
        {
            if (RunCoins < 300) return false;
            RunCoins -= 300;
            Turrets.Add(new TurretEntity(pos));
            SyncCoins();
            SynthAudio.Instance.UiClick();
            return true;
        }

        public bool PlaceTrap(TrapKind kind, Vector3 pos)
        {
            int cost = kind == TrapKind.Spike ? 75 : kind == TrapKind.Mine ? 120 : 150;
            if (RunCoins < cost) return false;
            RunCoins -= cost;
            float damage = kind == TrapKind.Spike ? 10f : kind == TrapKind.Mine ? 80f : 15f;
            int uses = kind == TrapKind.Spike ? 10 : kind == TrapKind.Mine ? 1 : 20;
            Traps.Add(new TrapEntity(kind, pos, damage, uses));
            SyncCoins();
            SynthAudio.Instance.UiClick();
            return true;
        }

        // ---- per-frame updates for non-MonoBehaviour entities ----

        private void UpdateTurrets(float dt)
        {
            foreach (var t in Turrets)
            {
                t.FireCooldown -= dt;
                if (t.FireCooldown > 0f) continue;
                ZombieAI target = null;
                float best = float.MaxValue;
                foreach (var z in Zombies)
                {
                    if (!z.IsAlive) continue;
                    float d = Vector3.Distance(z.transform.position, t.Position);
                    if (d <= t.Range && d < best) { best = d; target = z; }
                }
                if (target == null) continue;
                t.FireCooldown = 1f / t.FireRate;
                Vector3 dir = (target.transform.position - t.Position).normalized;
                var go = new GameObject("TurretBullet");
                go.layer = LayerMask.NameToLayer("Bullet");
                go.transform.position = t.Position;
                var bullet = go.AddComponent<Bullet>();
                bullet.Launch(t.Position, dir * 14f, t.Damage, 0, true, false, 0f, 0f, 2f, false);
                Bullets.Add(bullet);
            }
        }

        private void UpdateTraps(float dt)
        {
            var spent = new List<TrapEntity>();
            foreach (var trap in Traps)
            {
                trap.Cooldown = Math.Max(0f, trap.Cooldown - dt);
                if (trap.Cooldown > 0f) continue;
                ZombieAI victim = null;
                foreach (var z in Zombies)
                {
                    if (!z.IsAlive) continue;
                    if (Vector3.Distance(z.transform.position, trap.Position) < z.Radius + trap.Radius)
                    { victim = z; break; }
                }
                if (victim == null) continue;
                switch (trap.Kind)
                {
                    case TrapKind.Spike:
                        DamageZombie(victim, trap.Damage, false, Vector3.up);
                        trap.UsesLeft--;
                        trap.Cooldown = 0.5f;
                        break;
                    case TrapKind.Mine:
                        ExplodeAt(trap.Position, 2.5f, trap.Damage);
                        spent.Add(trap);
                        break;
                    case TrapKind.ElectricFence:
                        DamageZombie(victim, trap.Damage, false, Vector3.up);
                        trap.UsesLeft--;
                        trap.Cooldown = 1f;
                        break;
                }
                if (trap.UsesLeft <= 0 && trap.Kind != TrapKind.Mine) spent.Add(trap);
            }
            foreach (var t in spent)
            {
                t.DestroyView();
                Traps.Remove(t);
            }
        }

        /// <summary>
        /// Horde separation: zombies push each other apart so they don't stack
        /// into one pile. O(n^2) over alive zombies, like the Kotlin engine.
        /// Only positional — Rigidbody velocity stays in ZombieAI.
        /// </summary>
        private void SeparateZombies()
        {
            for (int i = 0; i < Zombies.Count; i++)
            {
                var a = Zombies[i];
                if (!a.IsAlive) continue;
                for (int j = i + 1; j < Zombies.Count; j++)
                {
                    var b = Zombies[j];
                    if (!b.IsAlive) continue;
                    Vector3 delta = a.transform.position - b.transform.position;
                    float minDist = a.Radius + b.Radius;
                    float distSq = delta.x * delta.x + delta.z * delta.z;
                    if (distSq >= minDist * minDist || distSq < 0.0001f) continue;
                    float dist = Mathf.Sqrt(distSq);
                    Vector3 push = new Vector3(delta.x / dist, 0f, delta.z / dist)
                        * (minDist - dist) * 0.5f;
                    a.transform.position += push;
                    b.transform.position -= push;
                }
            }
        }

        private void UpdatePickups(float dt)
        {
            float magnetRadius = 2.5f * State.Player.MagnetRadiusMultiplier();
            var collected = new List<Pickup>();
            foreach (var p in Pickups)
            {
                if (Player == null) break;
                float dist = Vector3.Distance(p.transform.position, Player.transform.position);
                if (dist < magnetRadius)
                {
                    Vector3 dir = (Player.transform.position - p.transform.position).normalized;
                    p.transform.position += dir * 6f * dt;
                }
                if (Vector3.Distance(p.transform.position, Player.transform.position) <
                    p.Radius + Player.Radius)
                {
                    ApplyPickup(p);
                    collected.Add(p);
                }
            }
            foreach (var p in collected)
            {
                Pickups.Remove(p);
                Destroy(p.gameObject);
            }
        }

        private void ApplyPickup(Pickup p)
        {
            switch (p.Kind)
            {
                case PickupKind.Health:
                    if (Player != null) Player.Heal(30);
                    break;
                case PickupKind.Ammo:
                    if (Player != null)
                    {
                        Player.AmmoInMag = EffectiveMagSize(Player.WeaponId);
                        Player.ReloadingUntil = 0f;
                    }
                    break;
                case PickupKind.Grenade:
                    ExplodeAt(Player.transform.position, 4f, 150f);
                    break;
                case PickupKind.Molotov:
                    ExplodeAt(Player.transform.position, 3f, 100f);
                    foreach (var z in Zombies)
                    {
                        if (!z.IsAlive) continue;
                        if (Vector3.Distance(z.transform.position, Player.transform.position) < 3f + z.Radius)
                        {
                            z.SlowUntil = _simTime + 2f;
                            z.SlowFactor = 0.5f;
                            z.BurnUntil = _simTime + 3f;
                            z.BurnDps = 10f;
                        }
                    }
                    break;
                case PickupKind.FreezeBomb:
                    foreach (var z in Zombies)
                    {
                        z.SlowUntil = _simTime + 3f;
                        z.SlowFactor = 0.25f;
                        z.FrozenUntil = _simTime + 3f;
                    }
                    break;
                case PickupKind.InstaKill:
                    _instaKillUntil = _simTime + 5f;
                    break;
            }
            SynthAudio.Instance.Pickup();
        }

        private void CleanupCorpses()
        {
            // Ragdolls stay visible ~4s (the wow factor), then fade out.
            var dead = new List<ZombieAI>();
            foreach (var z in Zombies)
                if (z.DeathState == DeathState.Falling && _simTime - z.DeathTime > 4f)
                    dead.Add(z);
            foreach (var z in dead)
            {
                Zombies.Remove(z);
                Destroy(z.gameObject);
            }
        }

        private void SyncCoins()
        {
            State.Player.Coins = RunCoins;
        }

        public void EndRun()
        {
            State.Phase = GamePhase.GameOver;
            State.Score = Score;
            State.Player.Coins = RunCoins;
            State.Player.Kills = Kills;
            State.Player.Headshots = Headshots;
            SynthAudio.Instance.GameOver();
            LevelDirector.OnRunEnded(Won);
        }
    }
}
