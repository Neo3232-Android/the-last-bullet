using System;
using System.Collections.Generic;

namespace TheLastBullet.Data
{
    /// <summary>
    /// THE LEVEL SYSTEM — the big Unity-version change.
    ///
    /// The old game was "20 maps, endless waves". This version is a real
    /// campaign: every map is BIG (40–90 units, 3–5 named zones with their own
    /// points of interest) and has 10 LEVELS to climb through.
    ///
    /// * 20 maps x 10 levels = 200 LEVELS total.
    /// * Each level = 3 waves (boss levels 5 and 10 = 2 waves + boss).
    /// * Levels escalate: new zombie kinds, new zones opening, new events,
    ///   harder mixes — not just bigger wave numbers.
    /// * Completing level 10 on a map unlocks ENDLESS on that map.
    /// * Medals per level: Bronze (complete) / Silver (under par time) /
    ///   Gold (under par + 75% HP left) / Platinum (under 80% par, no damage).
    /// </summary>
    public sealed class LevelDef
    {
        public MapId Map;
        public int Level; // 1..10
        public string Name;
        public int WaveCount;
        public bool IsBossLevel;
        public BossKind Boss;
        public Dictionary<ZombieKind, int> ZombieMix;
        public float HealthScale;
        public float CountScale;
        public float SpawnIntervalSec;
        public float EliteChance;
        public int ZonesOpen;
        public List<LevelEventId> Events;
        public int ParTimeSec;
        public int CoinReward;
        public string Objective;
    }

    public static class LevelData
    {
        public const int LevelsPerMap = 10;
        public const int TotalLevels = 200; // 20 maps x 10 levels
        public const int WavesPerLevel = 3;

        /// <summary>
        /// Zombie kinds introduced per level (cumulative). A level's mix is the
        /// map's own weights for the kinds it knows, plus weight 3 for newly
        /// introduced kinds the map doesn't list.
        /// </summary>
        private static readonly Dictionary<int, ZombieKind[]> KindsIntroducedByLevel =
            new Dictionary<int, ZombieKind[]>
            {
                { 1, new[] { ZombieKind.Walker } },
                { 2, new[] { ZombieKind.Runner } },
                { 3, new[] { ZombieKind.Crawler, ZombieKind.ZombieDog } },
                { 4, new[] { ZombieKind.Spitter, ZombieKind.Exploder } },
                { 5, new[] { ZombieKind.Brute } },
                { 6, new[] { ZombieKind.Armored } },
                { 7, new[] { ZombieKind.Screamer, ZombieKind.Healer } },
                { 8, new[] { ZombieKind.Leaper, ZombieKind.Shieldbearer } },
                { 9, new[] { ZombieKind.Ghost, ZombieKind.FireZombie, ZombieKind.Spawner } },
                { 10, new ZombieKind[0] },
            };

        /// <summary>Events unlocked per level (cumulative list is the level's active set).</summary>
        private static readonly Dictionary<int, LevelEventId[]> EventsByLevel =
            new Dictionary<int, LevelEventId[]>
            {
                { 1, new LevelEventId[0] },
                { 2, new[] { LevelEventId.Fog } },
                { 3, new[] { LevelEventId.SupplyDrop } },
                { 4, new[] { LevelEventId.HordeSurge } },
                { 5, new[] { LevelEventId.DoubleCoins } },
                { 6, new[] { LevelEventId.MapHazard } },
                { 7, new[] { LevelEventId.LastStand } },
                { 8, new[] { LevelEventId.Fog, LevelEventId.HordeSurge } },
                { 9, new[] { LevelEventId.SupplyDrop, LevelEventId.MapHazard } },
                { 10, new[] { LevelEventId.DoubleCoins, LevelEventId.HordeSurge, LevelEventId.MapHazard } },
            };

        /// <summary>Build all 10 levels for a map. Levels are generated from the map's
        /// mix + the global introduction/event schedules — designed, not random.</summary>
        public static List<LevelDef> BuildLevels(MapDef map)
        {
            int mapIndex = MapData.IndexOf(map.Id);
            var levels = new List<LevelDef>();
            var knownKinds = new HashSet<ZombieKind>();

            for (int level = 1; level <= LevelsPerMap; level++)
            {
                foreach (var k in KindsIntroducedByLevel[level])
                    knownKinds.Add(k);

                // Mix: map weights for known kinds, weight 3 for newly introduced kinds.
                var mix = new Dictionary<ZombieKind, int>();
                foreach (var kind in knownKinds)
                {
                    int weight;
                    if (map.ZombieMix.TryGetValue(kind, out int w)) weight = w;
                    else weight = 3;
                    mix[kind] = weight;
                }

                bool isBossLevel = level == 5 || level == 10;
                var events = new List<LevelEventId>(EventsByLevel[level]);
                // Dark maps get NightFall from level 3; outdoor maps get RainStorm from level 4.
                if (map.IsDark && level >= 3 && !events.Contains(LevelEventId.NightFall))
                    events.Add(LevelEventId.NightFall);
                if (map.IsOutdoor && level >= 4 && !events.Contains(LevelEventId.RainStorm))
                    events.Add(LevelEventId.RainStorm);

                int zonesOpen = Math.Min(map.Zones.Count, 1 + (level - 1) / 2);
                string bossName = isBossLevel
                    ? ZombieData.BossForLevel(mapIndex, level / 5 - 1).Name
                    : string.Empty;

                var def = new LevelDef
                {
                    Map = map.Id,
                    Level = level,
                    Name = map.Name + " — Level " + level + (isBossLevel ? " (" + bossName + ")" : string.Empty),
                    WaveCount = isBossLevel ? 2 : WavesPerLevel,
                    IsBossLevel = isBossLevel,
                    Boss = isBossLevel ? ZombieData.BossForLevel(mapIndex, level / 5 - 1).Kind : BossKind.Charger,
                    ZombieMix = mix,
                    HealthScale = 1f + 0.12f * (level - 1),
                    CountScale = 1f + 0.25f * (level - 1),
                    SpawnIntervalSec = Math.Max(1.6f - 0.08f * level, 0.5f),
                    EliteChance = level >= 6 ? ZombieData.EliteChanceForLevel(level) : 0f,
                    ZonesOpen = zonesOpen,
                    Events = events,
                    ParTimeSec = 150 + 30 * level,
                    CoinReward = 100 + 20 * level,
                    Objective = isBossLevel
                        ? "Clear 2 waves, then kill " + bossName + "."
                        : "Clear all " + WavesPerLevel + " waves.",
                };
                levels.Add(def);
            }
            return levels;
        }

        public static LevelDef GetLevel(MapId mapId, int level)
        {
            var levels = BuildLevels(MapData.Get(mapId));
            if (level < 1) level = 1;
            if (level > LevelsPerMap) level = LevelsPerMap;
            return levels[level - 1];
        }

        /// <summary>Medal earned for a level run. Platinum is the "perfect" run.</summary>
        public static MedalTier MedalForRun(LevelDef def, float clearTimeSec, float damageTaken, float maxHealth)
        {
            if (clearTimeSec <= def.ParTimeSec * 0.8f && damageTaken <= 0f)
                return MedalTier.Platinum;
            if (clearTimeSec <= def.ParTimeSec && damageTaken <= maxHealth * 0.25f)
                return MedalTier.Gold;
            if (clearTimeSec <= def.ParTimeSec)
                return MedalTier.Silver;
            return MedalTier.Bronze;
        }

        /// <summary>Endless mode on a map unlocks after its level 10 is cleared.</summary>
        public static bool IsEndlessUnlocked(PlayerProfile profile, MapId map) =>
            profile.UnlockedLevel(map) > LevelsPerMap;

        /// <summary>Endless waves keep scaling past the campaign (same rule as the old game).</summary>
        public static float EndlessHealthScale(int endlessWave) => 1f + (endlessWave - 1) * 0.12f;
    }
}
