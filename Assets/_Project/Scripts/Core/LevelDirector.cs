using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Level flow: owns the current level's waves, the between-wave breather,
    /// level completion (medal, coins, unlock next level), failure, and the
    /// Endless mode that opens after a map's level 10.
    ///
    /// A level = 3 waves (boss levels 5 and 10 = 2 waves + boss). The old
    /// game's "endless waves" survives only as the Endless mode.
    ///
    /// Build 2 modes (all reuse the wave/level machinery below):
    /// Timed (endless waves, win when the timer expires), DefendPoint (hold
    /// one zone for 5 waves), Escort (keep a survivor NPC alive for 5 waves),
    /// BossRush (one boss per wave, 5 bosses), Hardcore (campaign level, no
    /// pickups, tougher scaling), DailyChallenge (today's map + mutation,
    /// 5 waves). Each Start* method self-bootstraps the run through
    /// GameManager.StartEndless (full reset: player, counters, mutation) and
    /// then reconfigures it, so the menu can call them directly; the thin
    /// GameManager wrappers (StartTimed etc.) only need to delegate.
    /// </summary>
    public sealed class LevelDirector : MonoBehaviour
    {
        public const int DefendWaves = 5;
        public const int EscortWaves = 5;
        public const int DailyChallengeWaves = 5;
        public const int BossRushWaves = 5;
        public const int DefaultTimedSeconds = 300;

        public LevelDef CurrentLevel { get; private set; }
        public MapDef CurrentMap { get; private set; }
        public int CurrentWaveInLevel { get; private set; }
        public bool IsEndless { get; private set; }
        public int EndlessWave { get; private set; }

        // ---- mode state (read by the HUD / menu) ----
        /// <summary>Waves to survive in a wave-count mode (Defend/Escort/Daily); 0 otherwise.</summary>
        public int ModeWavesToWin { get; private set; }
        public Vector3 DefendZoneCenter { get; private set; }
        public float DefendZoneRadius { get; private set; }
        public float DefendZoneHealth { get; private set; }
        public float DefendZoneMaxHealth { get; private set; }
        public Vector3 EscortSurvivorPosition =>
            _escortSurvivor != null ? _escortSurvivor.transform.position : Vector3.zero;
        public bool EscortSurvivorAlive => _escortSurvivor != null && EscortHealth > 0f;
        public float EscortHealth { get; private set; }
        public float EscortMaxHealth { get; private set; }
        public int BossRushWave => _bossRushWave;
        public int BossRushTotalWaves => _bossRushTotalWaves;
        public float TimedDurationSec => _timedDurationSec;
        public float TimedTimeLeft
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null || _timedDurationSec <= 0f) return 0f;
                return Math.Max(0f, _timedDurationSec - gm.State.TimeSurvivedSec);
            }
        }
        public bool IsDailyChallengeRun { get; private set; }

        private WaveDirector _waveDirector;
        private float _breatherTimer;
        private bool _betweenWaves;
        private float _eventTimer;
        private float _levelStartTime;
        private float _damageTakenThisLevel;
        private float _damageTakenThisWave;
        private float _lastKnownPlayerHealth = -1f;
        private bool _damageHookActive;
        private bool _levelFinished;
        private bool _lastStandUsed;

        private int _bossRushWave;
        private int _bossRushTotalWaves = BossRushWaves;
        private float _timedDurationSec;
        private GameObject _defendZoneMarker;
        private GameObject _escortSurvivor;
        private List<Vector3> _escortWaypoints = new List<Vector3>();
        private int _escortWaypointIndex;
        private float _escortDamageTimer;
        private readonly List<GameObject> _modeObjects = new List<GameObject>();

        public bool IsWaveQueueEmpty => _waveDirector == null || _waveDirector.IsQueueEmpty;

        private void Awake()
        {
            _waveDirector = gameObject.AddComponent<WaveDirector>();
        }

        public void StartLevel(LevelDef level)
        {
            if (level == null) return;
            CleanupModeState();
            CurrentLevel = level;
            CurrentMap = MapData.Get(level.Map);
            IsEndless = false;
            ResetRunState();
            CurrentWaveInLevel = 1;
            MapBuilder.BuildArena(CurrentMap, level.ZonesOpen);
            _waveDirector.StartLevelWave(level, 1);
            _eventTimer = 20f; // first random event ~20s in
            GameManager.Instance.State.Wave = 1;
            GameManager.Instance.State.Phase = GamePhase.Playing;
        }

        public void StartEndless(MapDef map)
        {
            if (map == null) return;
            CleanupModeState();
            CurrentMap = map;
            CurrentLevel = null;
            IsEndless = true;
            ResetRunState();
            EndlessWave = 1;
            MapBuilder.BuildArena(map, map.Zones.Count);
            _waveDirector.StartEndlessWave(map, 1);
            GameManager.Instance.State.Wave = 1;
            GameManager.Instance.State.Phase = GamePhase.Playing;
        }

        // ---- modes (build 2) ----

        /// <summary>
        /// Timed survival: endless waves, win when the timer expires.
        /// GameManager's own check covers the 300s default; TickMode covers
        /// any other duration. The run is reset through StartEndless first.
        /// </summary>
        public void StartTimed(MapDef map, int seconds)
        {
            var gm = GameManager.Instance;
            if (gm == null || map == null) return;
            CleanupModeState();
            gm.StartEndless(map.Id);
            CurrentMap = map;
            CurrentLevel = null;
            IsEndless = true;
            EndlessWave = 1;
            CurrentWaveInLevel = 1;
            ModeWavesToWin = 0;
            _timedDurationSec = seconds > 0 ? seconds : DefaultTimedSeconds;
            ResetRunState();
            _eventTimer = 20f;
            gm.State.Mode = GameMode.Timed;
            gm.State.Wave = 1;
            gm.State.Phase = GamePhase.Playing;
        }

        /// <summary>
        /// Defend the Point: hold one zone (the map zone closest to the arena
        /// centre) for 5 waves. Zombies inside the zone chew its health down;
        /// lose if the zone falls. NOTE: zombies still path to the player
        /// (ZombieAI has no target override yet) — the zone takes damage by
        /// proximity, so kiting the horde through it is what threatens it.
        /// </summary>
        public void StartDefendPoint(MapDef map)
        {
            var gm = GameManager.Instance;
            if (gm == null || map == null) return;
            CleanupModeState();
            gm.StartEndless(map.Id);
            var level = BuildModeLevel(map, DefendWaves, 1.1f, 1f, 300,
                "Hold the point for 5 waves.");
            SetupLevelRun(level, GameMode.DefendPoint);
            ModeWavesToWin = DefendWaves;
            Vector3 centre = Vector3.zero;
            float radius = 6f;
            if (map.Zones.Count > 0)
            {
                var best = map.Zones[0];
                float bestDist = float.MaxValue;
                foreach (var z in map.Zones)
                {
                    float d = z.Center.sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = z; }
                }
                centre = new Vector3(best.Center.x, 0f, best.Center.y);
                radius = Math.Min(best.Radius, 12f);
            }
            DefendZoneCenter = centre;
            DefendZoneRadius = radius;
            DefendZoneMaxHealth = 500f;
            DefendZoneHealth = DefendZoneMaxHealth;
            SpawnDefendMarker(centre, radius);
        }

        /// <summary>
        /// Escort: a survivor NPC walks the map's zone centres on a loop for
        /// 5 waves. Zombies that reach the survivor hurt them; lose if the
        /// survivor dies. Lightweight by design: a placeholder figure (gold
        /// capsule + head) moved by TickEscort — no new behaviour file.
        /// NOTE: zombies still path to the player; the survivor is threatened
        /// when the horde is kited onto them (true retargeting needs a
        /// ZombieAI target override — see report).
        /// </summary>
        public void StartEscort(MapDef map)
        {
            var gm = GameManager.Instance;
            if (gm == null || map == null) return;
            CleanupModeState();
            gm.StartEndless(map.Id);
            var level = BuildModeLevel(map, EscortWaves, 1f, 1f, 350,
                "Keep the survivor alive for 5 waves.");
            SetupLevelRun(level, GameMode.Escort);
            ModeWavesToWin = EscortWaves;
            _escortWaypoints.Clear();
            foreach (var z in map.Zones)
                _escortWaypoints.Add(new Vector3(z.Center.x, 0f, z.Center.y));
            if (_escortWaypoints.Count == 0) _escortWaypoints.Add(Vector3.zero);
            _escortWaypointIndex = _escortWaypoints.Count > 1 ? 1 : 0;
            EscortMaxHealth = 100f;
            EscortHealth = EscortMaxHealth;
            SpawnSurvivor(_escortWaypoints[0]);
        }

        /// <summary>
        /// Boss Rush: one boss per wave, five bosses, no normal waves. Each
        /// wave is a synthetic 1-wave boss level fed to the WaveDirector, so
        /// the boss spawns through the normal queue.
        /// </summary>
        public void StartBossRush(MapDef map)
        {
            var gm = GameManager.Instance;
            if (gm == null || map == null) return;
            CleanupModeState();
            gm.StartEndless(map.Id);
            _bossRushWave = 1;
            _bossRushTotalWaves = BossRushWaves;
            ModeWavesToWin = 0;
            var boss = ZombieData.Bosses[0];
            SetupLevelRun(BuildBossRushLevel(map, boss.Kind, 1f), GameMode.BossRush);
            CurrentWaveInLevel = 1;
            gm.State.Wave = 1;
        }

        /// <summary>
        /// Hardcore: a campaign level with no pickups (GameManager blocks
        /// drops when Mode is Hardcore) and tougher scaling. Completing it
        /// counts as a normal campaign clear.
        /// </summary>
        public void StartHardcore(MapDef map, int level)
        {
            var gm = GameManager.Instance;
            if (gm == null || map == null) return;
            CleanupModeState();
            gm.StartEndless(map.Id);
            var baseLevel = LevelData.GetLevel(map.Id, level);
            var clone = CloneLevel(baseLevel);
            clone.HealthScale = baseLevel.HealthScale * 1.5f;
            clone.CountScale = baseLevel.CountScale * 1.25f;
            clone.EliteChance = Math.Max(baseLevel.EliteChance, 0.08f);
            clone.CoinReward = baseLevel.CoinReward * 2;
            clone.Name = baseLevel.Name + " (Hardcore)";
            clone.Objective = "Hardcore: " + baseLevel.Objective + " No pickups. Tougher zombies.";
            SetupLevelRun(clone, GameMode.Hardcore);
            ModeWavesToWin = 0;
        }

        /// <summary>Today's map, by day-of-year; the day's mutation is applied by GameManager (TodayMutation).</summary>
        public static MapDef TodayDailyMap() =>
            MapData.All[DayOfYearToday() % MapData.All.Count];

        public static int DayOfYearToday() => DateTime.UtcNow.DayOfYear;

        public static ZombieData.DailyMutation TodayDailyMutation() =>
            ZombieData.MutationForDay(SaveSystem.DayIndexToday());

        /// <summary>Daily Challenge: today's map, 5 waves, today's mutation, streak tracked on win.</summary>
        public void StartDailyChallenge()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var map = TodayDailyMap();
            CleanupModeState();
            gm.StartEndless(map.Id);
            string mutation = gm.TodayMutation != null ? gm.TodayMutation.Name : "none";
            var level = BuildModeLevel(map, DailyChallengeWaves, 1.2f, 1.1f, 250,
                "Daily Challenge: clear 5 waves. Mutation: " + mutation + ".");
            SetupLevelRun(level, GameMode.DailyChallenge);
            ModeWavesToWin = DailyChallengeWaves;
            IsDailyChallengeRun = true;
        }

        /// <summary>Shared setup for the wave-count modes: synthetic level, arena, wave 1, mode + phase.</summary>
        private void SetupLevelRun(LevelDef level, GameMode mode)
        {
            var gm = GameManager.Instance;
            if (gm == null || level == null) return;
            CurrentLevel = level;
            CurrentMap = MapData.Get(level.Map);
            IsEndless = false;
            CurrentWaveInLevel = 1;
            ResetRunState();
            _eventTimer = 20f;
            MapBuilder.BuildArena(CurrentMap, level.ZonesOpen);
            _waveDirector.StartLevelWave(level, 1);
            gm.State.Wave = 1;
            if (level.Level > 0) gm.State.Level = level.Level; // real campaign level (Hardcore); synthetic mode levels leave it
            gm.State.Mode = mode;
            gm.State.Phase = GamePhase.Playing;
        }

        private void ResetRunState()
        {
            var gm = GameManager.Instance;
            _levelFinished = false;
            _levelStartTime = gm != null ? gm.SimTime : 0f;
            _damageTakenThisLevel = 0f;
            _damageTakenThisWave = 0f;
            _lastKnownPlayerHealth = -1f;
            _damageHookActive = false;
            _lastStandUsed = false;
            _betweenWaves = false;
            _breatherTimer = 0f;
        }

        /// <summary>Synthetic level for a wave-count mode. Level 0 marks it: never recorded as a campaign clear.</summary>
        private static LevelDef BuildModeLevel(MapDef map, int waves, float healthScale,
            float countScale, int coinReward, string objective)
        {
            var mix = new Dictionary<ZombieKind, int>(map.ZombieMix);
            if (mix.Count == 0) mix[ZombieKind.Walker] = 10;
            return new LevelDef
            {
                Map = map.Id,
                Level = 0,
                Name = map.Name + " — " + objective,
                WaveCount = waves,
                IsBossLevel = false,
                Boss = BossKind.Charger,
                ZombieMix = mix,
                HealthScale = healthScale,
                CountScale = countScale,
                SpawnIntervalSec = 1.4f,
                EliteChance = 0.02f,
                ZonesOpen = map.Zones.Count,
                Events = new List<LevelEventId>
                    { LevelEventId.Fog, LevelEventId.SupplyDrop, LevelEventId.HordeSurge, LevelEventId.DoubleCoins },
                ParTimeSec = 600,
                CoinReward = coinReward,
                Objective = objective,
            };
        }

        /// <summary>Synthetic 1-wave boss level: empty mix, so the queue holds only the boss.</summary>
        private static LevelDef BuildBossRushLevel(MapDef map, BossKind boss, float healthScale)
        {
            return new LevelDef
            {
                Map = map.Id,
                Level = 0,
                Name = map.Name + " — Boss Rush",
                WaveCount = 1,
                IsBossLevel = true,
                Boss = boss,
                ZombieMix = new Dictionary<ZombieKind, int>(),
                HealthScale = healthScale,
                CountScale = 1f,
                SpawnIntervalSec = 1.5f,
                EliteChance = 0f,
                ZonesOpen = map.Zones.Count,
                Events = new List<LevelEventId> { LevelEventId.SupplyDrop, LevelEventId.DoubleCoins },
                ParTimeSec = 600,
                CoinReward = 500,
                Objective = "Defeat the boss.",
            };
        }

        private static LevelDef CloneLevel(LevelDef src)
        {
            if (src == null) return null;
            return new LevelDef
            {
                Map = src.Map,
                Level = src.Level,
                Name = src.Name,
                WaveCount = src.WaveCount,
                IsBossLevel = src.IsBossLevel,
                Boss = src.Boss,
                ZombieMix = new Dictionary<ZombieKind, int>(src.ZombieMix),
                HealthScale = src.HealthScale,
                CountScale = src.CountScale,
                SpawnIntervalSec = src.SpawnIntervalSec,
                EliteChance = src.EliteChance,
                ZonesOpen = src.ZonesOpen,
                Events = new List<LevelEventId>(src.Events),
                ParTimeSec = src.ParTimeSec,
                CoinReward = src.CoinReward,
                Objective = src.Objective,
            };
        }

        private void SpawnDefendMarker(Vector3 centre, float radius)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (marker == null) return;
            marker.name = "DefendZone";
            marker.transform.position = new Vector3(centre.x, 0.05f, centre.z);
            marker.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);
            var rend = marker.GetComponent<Renderer>();
            if (rend != null)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit != null)
                {
                    var mat = new Material(lit);
                    mat.color = new Color(1f, 0.84f, 0f, 0.3f);
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetFloat("_AlphaClip", 0f);
                    mat.SetFloat("_SrcBlend", 1f);
                    mat.SetFloat("_DstBlend", 10f);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    rend.material = mat;
                }
            }
            var col = marker.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);
            _defendZoneMarker = marker;
            _modeObjects.Add(marker);
        }

        private void SpawnSurvivor(Vector3 pos)
        {
            var go = new GameObject("EscortSurvivor");
            go.transform.position = pos;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            if (body != null)
            {
                body.name = "Body";
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                var bodyRend = body.GetComponent<Renderer>();
                if (bodyRend != null) bodyRend.material.color = new Color(1f, 0.84f, 0f);
                foreach (var c in body.GetComponents<Collider>()) UnityEngine.Object.Destroy(c);
            }
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (head != null)
            {
                head.name = "Head";
                head.transform.SetParent(go.transform, false);
                head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
                head.transform.localScale = Vector3.one * 0.55f;
                var headRend = head.GetComponent<Renderer>();
                if (headRend != null) headRend.material.color = new Color(0.96f, 0.8f, 0.6f);
                foreach (var c in head.GetComponents<Collider>()) UnityEngine.Object.Destroy(c);
            }
            _escortSurvivor = go;
            _modeObjects.Add(go);
        }

        private void CleanupEscortSurvivor()
        {
            if (_escortSurvivor == null) return;
            _modeObjects.Remove(_escortSurvivor);
            UnityEngine.Object.Destroy(_escortSurvivor);
            _escortSurvivor = null;
        }

        private void CleanupModeState()
        {
            foreach (var go in _modeObjects)
                if (go != null) UnityEngine.Object.Destroy(go);
            _modeObjects.Clear();
            _defendZoneMarker = null;
            _escortSurvivor = null;
            _escortWaypoints.Clear();
            _escortWaypointIndex = 0;
            _escortDamageTimer = 0f;
            EscortHealth = 0f;
            EscortMaxHealth = 0f;
            DefendZoneCenter = Vector3.zero;
            DefendZoneRadius = 0f;
            DefendZoneHealth = 0f;
            DefendZoneMaxHealth = 0f;
            ModeWavesToWin = 0;
            _bossRushWave = 0;
            _bossRushTotalWaves = BossRushWaves;
            _timedDurationSec = 0f;
            IsDailyChallengeRun = false;
        }

        /// <summary>Called every fixed step by GameManager.</summary>
        public void Tick(float dt)
        {
            if (GameManager.Instance.State.Phase != GamePhase.Playing) return;

            if (_betweenWaves)
            {
                _breatherTimer -= dt;
                if (_breatherTimer <= 0f)
                {
                    _betweenWaves = false;
                    StartNextWave();
                }
                return;
            }

            _waveDirector.Tick(dt);
            TickMode(dt);
            TickEvents(dt);
        }

        /// <summary>Per-mode rules: timed win, last-stand, defend zone, escort survivor.</summary>
        private void TickMode(float dt)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            // Timed: custom durations (GameManager's own check covers the 300s default).
            if (gm.State.Mode == GameMode.Timed && _timedDurationSec > 0f &&
                gm.State.TimeSurvivedSec >= _timedDurationSec)
            {
                CompleteModeRun(250);
                return;
            }
            // Last-stand: once per run, when the player drops to 25% HP or less.
            if (!_lastStandUsed && gm.Player != null && gm.Player.IsAlive && gm.Player.MaxHealth > 0 &&
                gm.Player.Health <= gm.Player.MaxHealth / 4)
            {
                _lastStandUsed = true;
                Gameplay.MapEvents.Trigger(LevelEventId.LastStand, CurrentMap);
            }
            WatchPlayerHealth();
            if (gm.State.Mode == GameMode.DefendPoint) TickDefendZone(dt);
            if (gm.State.Mode == GameMode.Escort) TickEscort(dt);
        }

        /// <summary>
        /// Damage tracking for medals + flawless waves. GameManager.DamagePlayer
        /// does not call RecordDamageTaken (yet), so health drops are watched
        /// here instead. If the hook ever gets wired, RecordDamageTaken sets
        /// _damageHookActive and the watch stands down, so no hit is counted twice.
        /// </summary>
        private void WatchPlayerHealth()
        {
            var gm = GameManager.Instance;
            if (gm == null || _damageHookActive) return;
            var player = gm.Player;
            if (player == null || !player.IsAlive) return;
            if (_lastKnownPlayerHealth < 0f)
            {
                _lastKnownPlayerHealth = player.Health;
                return;
            }
            if (player.Health < _lastKnownPlayerHealth)
            {
                float dmg = _lastKnownPlayerHealth - player.Health;
                _damageTakenThisLevel += dmg;
                _damageTakenThisWave += dmg;
            }
            _lastKnownPlayerHealth = player.Health;
        }

        private void TickDefendZone(float dt)
        {
            var gm = GameManager.Instance;
            if (gm == null || DefendZoneHealth <= 0f) return;
            int inside = 0;
            foreach (var z in gm.Zombies)
            {
                if (z == null || !z.IsAlive) continue;
                Vector3 d = z.transform.position - DefendZoneCenter;
                d.y = 0f;
                if (d.sqrMagnitude < DefendZoneRadius * DefendZoneRadius) inside++;
            }
            if (inside > 0)
            {
                DefendZoneHealth = Math.Max(0f, DefendZoneHealth - 3f * inside * dt);
                if (DefendZoneHealth <= 0f)
                {
                    gm.Won = false;
                    gm.EndRun(); // the point fell
                }
            }
        }

        private void TickEscort(float dt)
        {
            var gm = GameManager.Instance;
            if (gm == null || _escortSurvivor == null || EscortHealth <= 0f) return;
            // Walk the waypoint path (zone centres, looping).
            if (_escortWaypoints.Count > 0)
            {
                Vector3 target = _escortWaypoints[_escortWaypointIndex];
                Vector3 pos = _escortSurvivor.transform.position;
                Vector3 to = target - pos;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.5f)
                {
                    _escortWaypointIndex = (_escortWaypointIndex + 1) % _escortWaypoints.Count;
                }
                else
                {
                    _escortSurvivor.transform.position = pos + to / dist * 1.2f * dt;
                }
            }
            // Zombies that reach the survivor hurt them (checked twice a second).
            _escortDamageTimer -= dt;
            if (_escortDamageTimer > 0f) return;
            _escortDamageTimer = 0.5f;
            int touching = 0;
            Vector3 sPos = _escortSurvivor.transform.position;
            foreach (var z in gm.Zombies)
            {
                if (z == null || !z.IsAlive) continue;
                if (Vector3.Distance(z.transform.position, sPos) < z.Radius + 1f) touching++;
            }
            if (touching > 0)
            {
                EscortHealth = Math.Max(0f, EscortHealth - 8f * touching);
                if (EscortHealth <= 0f)
                {
                    CleanupEscortSurvivor();
                    gm.Won = false;
                    gm.EndRun(); // the survivor died
                }
            }
        }

        /// <summary>Called by GameManager when the wave queue is empty and nothing is alive.</summary>
        public void OnWaveCleared()
        {
            if (_levelFinished) return;
            // Flawless wave: no damage taken this wave.
            if (_damageTakenThisWave <= 0f)
            {
                var save = SaveSystem.Instance;
                if (save != null)
                {
                    save.RecordMissionProgress("daily-flawless-1", 1);
                    save.UnlockAchievement("flawless-wave");
                }
            }
            _damageTakenThisWave = 0f;
            // Boss Rush: one boss per wave, win after the last.
            if (GameManager.Instance.State.Mode == GameMode.BossRush)
            {
                if (_bossRushWave >= _bossRushTotalWaves)
                {
                    CompleteModeRun(500);
                    return;
                }
                _betweenWaves = true;
                _breatherTimer = 4f;
                GameManager.Instance.State.Phase = GamePhase.BetweenWaves;
                return;
            }
            int totalWaves = CurrentLevel != null ? CurrentLevel.WaveCount : int.MaxValue;
            if (CurrentWaveInLevel >= totalWaves)
            {
                if (ModeWavesToWin > 0)
                {
                    int bonus = GameManager.Instance.State.Mode == GameMode.Escort ? 350 :
                        GameManager.Instance.State.Mode == GameMode.DailyChallenge ? 250 : 300;
                    CompleteModeRun(bonus);
                }
                else
                {
                    CompleteLevel();
                }
                return;
            }
            // Between-wave breather: short pause, shop hook, then the next wave.
            _betweenWaves = true;
            _breatherTimer = 4f;
            GameManager.Instance.State.Phase = GamePhase.BetweenWaves;
        }

        private void StartNextWave()
        {
            _damageTakenThisWave = 0f;
            var gm = GameManager.Instance;
            if (gm.State.Mode == GameMode.BossRush)
            {
                _bossRushWave++;
                CurrentWaveInLevel = _bossRushWave;
                gm.State.Wave = _bossRushWave;
                gm.State.Phase = GamePhase.Playing;
                var boss = ZombieData.Bosses[(_bossRushWave - 1) % ZombieData.Bosses.Count];
                CurrentLevel = BuildBossRushLevel(CurrentMap, boss.Kind,
                    1f + 0.3f * (_bossRushWave - 1));
                _waveDirector.StartLevelWave(CurrentLevel, 1);
                return;
            }
            CurrentWaveInLevel++;
            GameManager.Instance.State.Wave = CurrentWaveInLevel;
            GameManager.Instance.State.Phase = GamePhase.Playing;
            if (IsEndless)
            {
                EndlessWave = CurrentWaveInLevel;
                _waveDirector.StartEndlessWave(CurrentMap, EndlessWave);
            }
            else
            {
                _waveDirector.StartLevelWave(CurrentLevel, CurrentWaveInLevel);
            }
        }

        private void CompleteLevel()
        {
            _levelFinished = true;
            var gm = GameManager.Instance;
            float clearTime = gm.SimTime - _levelStartTime;
            var medal = LevelData.MedalForRun(CurrentLevel, clearTime,
                _damageTakenThisLevel, gm.State.Player.MaxHealth);
            var profile = SaveSystem.Instance.Profile;
            profile.Coins += CurrentLevel.CoinReward;
            // Campaign wins count toward run totals, missions and achievements too.
            profile.TotalRuns++;
            profile.TotalKills += gm.Kills;
            profile.BestWave = Math.Max(profile.BestWave, gm.State.Wave);
            profile.MostKills = Math.Max(profile.MostKills, gm.Kills);
            profile.LongestSurvivalSec = Math.Max(profile.LongestSurvivalSec,
                (int)gm.State.TimeSurvivedSec);
            float acc = gm.ShotsFired > 0 ? 100f * gm.ShotsHit / gm.ShotsFired : 0f;
            if (gm.ShotsFired > 0)
                profile.BestAccuracyPct = Math.Max(profile.BestAccuracyPct, acc);
            SaveSystem.Instance.RecordLevelClear(CurrentLevel.Map, CurrentLevel.Level, medal);
            SaveSystem.Instance.RecordRunProgress(gm.Kills, gm.Headshots, gm.State.Wave,
                gm.RunCoins, (int)gm.State.TimeSurvivedSec);
            SaveSystem.Instance.CheckRunAchievements(gm.State.Wave, gm.Kills, gm.Headshots,
                acc, (int)gm.State.TimeSurvivedSec);
            CleanupModeState();
            gm.State.Phase = GamePhase.GameOver; // level-complete screen reuses the end screen
            gm.Won = true;
            // The UI shows the level-complete panel (medal, coins, next level button).
        }

        /// <summary>Win a wave-count mode: bonus coins, daily streak / rescued ally, then end the run.</summary>
        private void CompleteModeRun(int bonusCoins)
        {
            if (_levelFinished) return;
            _levelFinished = true;
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (bonusCoins > 0) gm.AddCoins(bonusCoins);
            var save = SaveSystem.Instance;
            if (save != null)
            {
                if (gm.State.Mode == GameMode.DailyChallenge) save.RecordDailyChallengeWin();
                if (gm.State.Mode == GameMode.Escort) save.RecordSurvivorRescued();
            }
            gm.Won = true;
            gm.EndRun();
        }

        /// <summary>Called by GameManager.EndRun — records run stats, best wave, accuracy.</summary>
        public void OnRunEnded(bool won)
        {
            var gm = GameManager.Instance;
            var profile = SaveSystem.Instance.Profile;
            profile.TotalRuns++;
            profile.TotalKills += gm.Kills;
            profile.BestWave = Math.Max(profile.BestWave, gm.State.Wave);
            profile.MostKills = Math.Max(profile.MostKills, gm.Kills);
            profile.LongestSurvivalSec = Math.Max(profile.LongestSurvivalSec,
                (int)gm.State.TimeSurvivedSec);
            if (gm.ShotsFired > 0)
            {
                float acc = 100f * gm.ShotsHit / gm.ShotsFired;
                profile.BestAccuracyPct = Math.Max(profile.BestAccuracyPct, acc);
            }
            profile.Coins += gm.RunCoins;
            // Missions + achievements from this run (bosses/elites/flawless are recorded live).
            var save = SaveSystem.Instance;
            if (save != null)
            {
                float acc = gm.ShotsFired > 0 ? 100f * gm.ShotsHit / gm.ShotsFired : 0f;
                save.RecordRunProgress(gm.Kills, gm.Headshots, gm.State.Wave, gm.RunCoins,
                    (int)gm.State.TimeSurvivedSec);
                save.CheckRunAchievements(gm.State.Wave, gm.Kills, gm.Headshots, acc,
                    (int)gm.State.TimeSurvivedSec);
            }
            CleanupModeState();
            SaveSystem.Instance.Save();
        }

        /// <summary>
        /// Direct damage hook for GameManager.DamagePlayer. Accumulates the
        /// hit and disables the health-watch for this run (see WatchPlayerHealth)
        /// so the same hit is never counted twice.
        /// </summary>
        public void RecordDamageTaken(float amount)
        {
            _damageHookActive = true;
            _damageTakenThisLevel += amount;
            _damageTakenThisWave += amount;
        }

        // ---- random events during a level (see Gameplay/MapEvents) ----

        private void TickEvents(float dt)
        {
            if (CurrentLevel == null || CurrentLevel.Events.Count == 0) return;
            _eventTimer -= dt;
            if (_eventTimer > 0f) return;
            _eventTimer = 25f + (float)GameManager.Instance.Rng.NextDouble() * 15f;
            // LastStand is conditional (low HP — see TickMode), never random.
            var pool = new List<LevelEventId>();
            foreach (var e in CurrentLevel.Events)
                if (e != LevelEventId.LastStand) pool.Add(e);
            if (pool.Count == 0) return;
            var ev = pool[GameManager.Instance.Rng.Next(pool.Count)];
            Gameplay.MapEvents.Trigger(ev, CurrentMap);
        }
    }
}
