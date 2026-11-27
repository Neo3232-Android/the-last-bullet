using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Persistent profile storage. Unity's JsonUtility cannot serialize
    /// Dictionaries/HashSets, so the profile is converted to a flat DTO of
    /// lists for save/load. Port of the Kotlin SharedPreferences store.
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        public const string PrefsKey = "the-last-bullet-profile-v1";
        public const string CharacterKey = "the-last-bullet-character-v1";
        public const string SettingsKey = "the-last-bullet-settings-v1";

        public PlayerProfile Profile { get; private set; } = new PlayerProfile();
        public CharacterDef Character { get; private set; } = CharacterDef.Default();
        public Quality QualitySetting { get; set; } = Quality.High;
        public bool Muted { get; set; }
        public bool ShowFps { get; set; }

        [Serializable]
        private sealed class IntPair { public int Key; public int Value; }

        [Serializable]
        private sealed class StringPair { public string Key; public string Value; }

        [Serializable]
        private sealed class LevelProgressEntry { public string Map; public int Unlocked; }

        [Serializable]
        private sealed class MedalEntry { public string Key; public int Medal; }

        [Serializable]
        private sealed class ProfileDto
        {
            public int Coins;
            public int BestWave;
            public int MostKills;
            public int LongestSurvivalSec;
            public float BestAccuracyPct;
            public int TotalKills;
            public int TotalRuns;
            public List<string> UnlockedOutfits = new List<string>();
            public List<int> UnlockedWeapons = new List<int>();
            public List<string> Achievements = new List<string>();
            public int PrestigeLevel;
            public List<IntPair> WeaponMasteryXp = new List<IntPair>();
            public List<StringPair> DailyMissionProgress = new List<StringPair>();
            public int LastDailyMissionDay;
            public List<StringPair> WeeklyChallengeProgress = new List<StringPair>();
            public int LastWeeklyChallengeWeek;
            public List<string> CompletedDailyMissions = new List<string>();
            public List<string> CompletedWeeklyChallenges = new List<string>();
            public int LoginStreakDays;
            public int LastLoginDay;
            public List<IntPair> BestiaryKills = new List<IntPair>();
            public List<IntPair> BossBestiaryKills = new List<IntPair>();
            public List<string> SuperPerksUnlocked = new List<string>();
            public List<StringPair> MetaUpgradeLevels = new List<StringPair>();
            public bool HasDroneCompanion;
            public int DroneLevel;
            public int RescuedAllies;
            public int DailyChallengeStreak;
            public int LastDailyChallengeDay;
            public int TotalBossKills;
            public int TotalEliteKills;
            public List<LevelProgressEntry> LevelProgress = new List<LevelProgressEntry>();
            public List<MedalEntry> Medals = new List<MedalEntry>();
        }

        [Serializable]
        private sealed class CharacterDto
        {
            public string SkinTone;
            public int HairStyle;
            public string HairColor;
            public string OutfitColor;
            public int Accessory;
            public int StartingWeaponId;
            public int StartingPerkId;
        }

        [Serializable]
        private sealed class SettingsDto
        {
            public int Quality;
            public bool Muted;
            public bool ShowFps;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Load();
        }

        public void Save()
        {
            var dto = new ProfileDto
            {
                Coins = Profile.Coins,
                BestWave = Profile.BestWave,
                MostKills = Profile.MostKills,
                LongestSurvivalSec = Profile.LongestSurvivalSec,
                BestAccuracyPct = Profile.BestAccuracyPct,
                TotalKills = Profile.TotalKills,
                TotalRuns = Profile.TotalRuns,
                UnlockedOutfits = new List<string>(Profile.UnlockedOutfits),
                PrestigeLevel = Profile.PrestigeLevel,
                LastDailyMissionDay = Profile.LastDailyMissionDay,
                LastWeeklyChallengeWeek = Profile.LastWeeklyChallengeWeek,
                LoginStreakDays = Profile.LoginStreakDays,
                LastLoginDay = Profile.LastLoginDay,
                SuperPerksUnlocked = new List<string>(Profile.SuperPerksUnlocked),
                CompletedDailyMissions = new List<string>(Profile.CompletedDailyMissions),
                CompletedWeeklyChallenges = new List<string>(Profile.CompletedWeeklyChallenges),
                HasDroneCompanion = Profile.HasDroneCompanion,
                DroneLevel = Profile.DroneLevel,
                RescuedAllies = Profile.RescuedAllies,
                DailyChallengeStreak = Profile.DailyChallengeStreak,
                LastDailyChallengeDay = Profile.LastDailyChallengeDay,
                TotalBossKills = Profile.TotalBossKills,
                TotalEliteKills = Profile.TotalEliteKills,
                Achievements = new List<string>(Profile.Achievements),
            };
            foreach (var w in Profile.UnlockedWeapons) dto.UnlockedWeapons.Add((int)w);
            foreach (var kv in Profile.WeaponMasteryXp)
                dto.WeaponMasteryXp.Add(new IntPair { Key = (int)kv.Key, Value = kv.Value });
            foreach (var kv in Profile.DailyMissionProgress)
                dto.DailyMissionProgress.Add(new StringPair { Key = kv.Key, Value = kv.Value.ToString() });
            foreach (var kv in Profile.WeeklyChallengeProgress)
                dto.WeeklyChallengeProgress.Add(new StringPair { Key = kv.Key, Value = kv.Value.ToString() });
            foreach (var kv in Profile.MetaUpgradeLevels)
                dto.MetaUpgradeLevels.Add(new StringPair { Key = kv.Key, Value = kv.Value.ToString() });
            foreach (var kv in Profile.BestiaryKills)
                dto.BestiaryKills.Add(new IntPair { Key = (int)kv.Key, Value = kv.Value });
            foreach (var kv in Profile.BossBestiaryKills)
                dto.BossBestiaryKills.Add(new IntPair { Key = (int)kv.Key, Value = kv.Value });
            foreach (var kv in Profile.UnlockedLevelPerMap)
                dto.LevelProgress.Add(new LevelProgressEntry { Map = kv.Key, Unlocked = kv.Value });
            foreach (var kv in Profile.BestMedalPerLevel)
                dto.Medals.Add(new MedalEntry { Key = kv.Key, Medal = (int)kv.Value });

            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(dto));

            var c = new CharacterDto
            {
                SkinTone = Character.SkinTone,
                HairStyle = (int)Character.HairStyle,
                HairColor = Character.HairColor,
                OutfitColor = Character.OutfitColor,
                Accessory = (int)Character.Accessory,
                StartingWeaponId = (int)Character.StartingWeaponId,
                StartingPerkId = (int)Character.StartingPerkId,
            };
            PlayerPrefs.SetString(CharacterKey, JsonUtility.ToJson(c));

            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(new SettingsDto
            {
                Quality = (int)QualitySetting,
                Muted = Muted,
                ShowFps = ShowFps,
            }));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    var dto = JsonUtility.FromJson<ProfileDto>(json);
                    if (dto != null) ApplyDto(dto);
                }
                string cjson = PlayerPrefs.GetString(CharacterKey, string.Empty);
                if (!string.IsNullOrEmpty(cjson))
                {
                    var c = JsonUtility.FromJson<CharacterDto>(cjson);
                    if (c != null)
                    {
                        Character.SkinTone = c.SkinTone;
                        Character.HairStyle = (HairStyle)c.HairStyle;
                        Character.HairColor = c.HairColor;
                        Character.OutfitColor = c.OutfitColor;
                        Character.Accessory = (Accessory)c.Accessory;
                        Character.StartingWeaponId = (WeaponId)c.StartingWeaponId;
                        Character.StartingPerkId = (PerkId)c.StartingPerkId;
                    }
                }
                string sjson = PlayerPrefs.GetString(SettingsKey, string.Empty);
                if (!string.IsNullOrEmpty(sjson))
                {
                    var s = JsonUtility.FromJson<SettingsDto>(sjson);
                    if (s != null)
                    {
                        QualitySetting = (Quality)s.Quality;
                        Muted = s.Muted;
                        ShowFps = s.ShowFps;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Failed to load profile, starting fresh: " + e.Message);
                Profile = new PlayerProfile();
            }
        }

        private void ApplyDto(ProfileDto dto)
        {
            var p = new PlayerProfile
            {
                Coins = dto.Coins,
                BestWave = dto.BestWave,
                MostKills = dto.MostKills,
                LongestSurvivalSec = dto.LongestSurvivalSec,
                BestAccuracyPct = dto.BestAccuracyPct,
                TotalKills = dto.TotalKills,
                TotalRuns = dto.TotalRuns,
                PrestigeLevel = dto.PrestigeLevel,
                LastDailyMissionDay = dto.LastDailyMissionDay,
                LastWeeklyChallengeWeek = dto.LastWeeklyChallengeWeek,
                LoginStreakDays = dto.LoginStreakDays,
                LastLoginDay = dto.LastLoginDay,
                HasDroneCompanion = dto.HasDroneCompanion,
                DroneLevel = dto.DroneLevel,
                RescuedAllies = dto.RescuedAllies,
                DailyChallengeStreak = dto.DailyChallengeStreak,
                LastDailyChallengeDay = dto.LastDailyChallengeDay,
                TotalBossKills = dto.TotalBossKills,
                TotalEliteKills = dto.TotalEliteKills,
            };
            if (dto.UnlockedOutfits != null) p.UnlockedOutfits = new HashSet<string>(dto.UnlockedOutfits);
            if (dto.CompletedDailyMissions != null) p.CompletedDailyMissions = new HashSet<string>(dto.CompletedDailyMissions);
            if (dto.CompletedWeeklyChallenges != null) p.CompletedWeeklyChallenges = new HashSet<string>(dto.CompletedWeeklyChallenges);
            if (dto.UnlockedWeapons != null)
                foreach (int w in dto.UnlockedWeapons) p.UnlockedWeapons.Add((WeaponId)w);
            if (dto.Achievements != null) p.Achievements = new HashSet<string>(dto.Achievements);
            if (dto.SuperPerksUnlocked != null) p.SuperPerksUnlocked = new HashSet<string>(dto.SuperPerksUnlocked);
            if (dto.WeaponMasteryXp != null)
                foreach (var kv in dto.WeaponMasteryXp) p.WeaponMasteryXp[(WeaponId)kv.Key] = kv.Value;
            if (dto.DailyMissionProgress != null)
                foreach (var kv in dto.DailyMissionProgress)
                    if (int.TryParse(kv.Value, out int v)) p.DailyMissionProgress[kv.Key] = v;
            if (dto.WeeklyChallengeProgress != null)
                foreach (var kv in dto.WeeklyChallengeProgress)
                    if (int.TryParse(kv.Value, out int v)) p.WeeklyChallengeProgress[kv.Key] = v;
            if (dto.MetaUpgradeLevels != null)
                foreach (var kv in dto.MetaUpgradeLevels)
                    if (int.TryParse(kv.Value, out int v)) p.MetaUpgradeLevels[kv.Key] = v;
            if (dto.BestiaryKills != null)
                foreach (var kv in dto.BestiaryKills) p.BestiaryKills[(ZombieKind)kv.Key] = kv.Value;
            if (dto.BossBestiaryKills != null)
                foreach (var kv in dto.BossBestiaryKills) p.BossBestiaryKills[(BossKind)kv.Key] = kv.Value;
            if (dto.LevelProgress != null)
                foreach (var e in dto.LevelProgress) p.UnlockedLevelPerMap[e.Map] = e.Unlocked;
            if (dto.Medals != null)
                foreach (var e in dto.Medals) p.BestMedalPerLevel[e.Key] = (MedalTier)e.Medal;
            Profile = p;
        }

        /// <summary>Record a level clear: unlock the next level, keep the best medal.</summary>
        public void RecordLevelClear(MapId map, int level, MedalTier medal)
        {
            string key = map + "-" + level;
            if (!Profile.BestMedalPerLevel.TryGetValue(key, out MedalTier best) || (int)medal > (int)best)
                Profile.BestMedalPerLevel[key] = medal;
            string mapKey = map.ToString();
            int unlocked = Profile.UnlockedLevel(map);
            if (level + 1 > unlocked)
                Profile.UnlockedLevelPerMap[mapKey] = level + 1;
            Save();
        }

        // ---- day / week clock (same epoch as GameManager's daily mutation) ----

        /// <summary>Days since 2026-01-01 (UTC). Matches GameManager.TodayMutation's day index.</summary>
        public static int DayIndexToday() =>
            (int)(DateTime.UtcNow.Date - new DateTime(2026, 1, 1)).TotalDays;

        public static int WeekIndexToday() => DayIndexToday() / 7;

        // ---- daily missions + weekly challenges ----

        /// <summary>Roll fresh missions when the day/week has turned over; no-op otherwise.</summary>
        public void RollMissionsIfNeeded()
        {
            int today = DayIndexToday();
            bool changed = false;
            if (Profile.LastDailyMissionDay != today)
            {
                Profile.LastDailyMissionDay = today;
                Profile.DailyMissionProgress.Clear();
                Profile.CompletedDailyMissions.Clear();
                changed = true;
            }
            int week = WeekIndexToday();
            if (Profile.LastWeeklyChallengeWeek != week)
            {
                Profile.LastWeeklyChallengeWeek = week;
                Profile.WeeklyChallengeProgress.Clear();
                Profile.CompletedWeeklyChallenges.Clear();
                changed = true;
            }
            if (changed) Save();
        }

        public List<ProgressionData.DailyMission> TodaysDailyMissions() =>
            ProgressionData.DailyMissionsForDay(DayIndexToday());

        public List<ProgressionData.WeeklyChallenge> ThisWeeksChallenges() =>
            ProgressionData.WeeklyChallengesForWeek(WeekIndexToday());

        private bool _checkingMissions;

        /// <summary>
        /// Add progress to one mission/challenge. Only today's dailies and this
        /// week's challenges accept progress (stale ids are ignored). When a
        /// mission completes, its coin reward is granted once. Returns true if
        /// this call completed the mission.
        /// </summary>
        public bool RecordMissionProgress(string id, int amount, bool isBest = false)
        {
            if (string.IsNullOrEmpty(id) || amount == 0) return false;
            RollMissionsIfNeeded();

            Dictionary<string, int> dict = null;
            HashSet<string> completed = null;
            int target = 0;
            int reward = 0;
            bool isDaily = false;
            foreach (var m in TodaysDailyMissions())
            {
                if (m.Id != id) continue;
                dict = Profile.DailyMissionProgress;
                completed = Profile.CompletedDailyMissions;
                target = m.Target; reward = m.Reward; isDaily = true;
                break;
            }
            if (dict == null)
            {
                foreach (var c in ThisWeeksChallenges())
                {
                    if (c.Id != id) continue;
                    dict = Profile.WeeklyChallengeProgress;
                    completed = Profile.CompletedWeeklyChallenges;
                    target = c.Target; reward = c.Reward;
                    break;
                }
            }
            if (dict == null || completed == null) return false;

            int current = dict.TryGetValue(id, out int v) ? v : 0;
            int next = isBest ? Math.Max(current, amount) : current + amount;
            if (next == current && !completed.Contains(id)) { dict[id] = next; return false; }
            dict[id] = next;
            bool completedNow = false;
            if (next >= target && !completed.Contains(id))
            {
                completed.Add(id);
                Profile.Coins += reward;
                completedNow = true;
                // Completing a daily also counts toward "complete 5 dailies this week".
                if (isDaily && !_checkingMissions)
                {
                    _checkingMissions = true;
                    try { RecordMissionProgress("weekly-dailies-5", 1); }
                    finally { _checkingMissions = false; }
                }
            }
            Save();
            return completedNow;
        }

        /// <summary>
        /// Fan-out for a finished run: feeds kills/headshots/best wave/coins/
        /// survival into today's dailies and this week's challenges with the
        /// right semantics (waves and survival are best-in-run, the rest add
        /// up). Bosses, elites and flawless waves are recorded live at kill
        /// time (see RecordBossKill / RecordKill). Returns the ids completed
        /// by this run, if any.
        /// </summary>
        public List<string> RecordRunProgress(int kills, int headshots, int bestWave,
            int coinsEarned, int survivalSec)
        {
            var done = new List<string>();
            RollMissionsIfNeeded();
            foreach (var m in TodaysDailyMissions())
                RecordForMission(m.Id, m.Target, kills, headshots, bestWave, coinsEarned, survivalSec, done);
            foreach (var c in ThisWeeksChallenges())
                RecordForMission(c.Id, c.Target, kills, headshots, bestWave, coinsEarned, survivalSec, done);
            return done;
        }

        private void RecordForMission(string id, int target, int kills, int headshots,
            int bestWave, int coinsEarned, int survivalSec, List<string> done)
        {
            bool isBest = false;
            int amount;
            if (id.Contains("kills")) amount = kills;
            else if (id.Contains("headshots")) amount = headshots;
            else if (id.Contains("waves") || id.Contains("wave-")) { amount = bestWave; isBest = true; }
            else if (id.Contains("coins")) amount = coinsEarned;
            else if (id.Contains("survive")) amount = survivalSec / 60; // weekly total minutes: adds up across runs
            else return; // bosses / elites / flawless / dailies: recorded live, not per run
            if (amount <= 0) return;
            if (RecordMissionProgress(id, amount, isBest)) done.Add(id);
        }

        // ---- daily login reward ----

        /// <summary>Coins granted by the most recent login reward (0 if none claimed yet).</summary>
        public int LastLoginRewardGrantedCoins { get; private set; }

        /// <summary>
        /// Grant today's login reward if it has not been claimed yet. The
        /// streak grows on consecutive days and resets after a missed day.
        /// Returns the reward, or null if already claimed today.
        /// </summary>
        public ProgressionData.LoginReward GrantLoginRewardIfDue()
        {
            int today = DayIndexToday();
            if (Profile.LastLoginDay == today) return null;
            int streak = Profile.LastLoginDay == today - 1 ? Profile.LoginStreakDays + 1 : 1;
            Profile.LoginStreakDays = streak;
            Profile.LastLoginDay = today;
            var reward = ProgressionData.LoginRewardForDay(
                (streak - 1) % ProgressionData.LoginRewards.Count + 1);
            int total = reward.Coins + ProgressionData.LoginStreakBonus(streak);
            Profile.Coins += total;
            LastLoginRewardGrantedCoins = total;
            Save();
            return reward;
        }

        // ---- bestiary ----

        /// <summary>
        /// Record one zombie kill for the bestiary. Returns coins granted if
        /// this kill completed the entry, 0 otherwise. Also feeds the elite
        /// daily mission when the kill was an elite.
        /// </summary>
        public int RecordKill(ZombieKind kind, bool isElite)
        {
            int before = Profile.BestiaryKills.TryGetValue(kind, out int k) ? k : 0;
            Profile.BestiaryKills[kind] = before + 1;
            if (isElite)
            {
                Profile.TotalEliteKills++;
                RecordMissionProgress("daily-elite-3", 1);
            }
            int granted = 0;
            foreach (var e in ProgressionData.Bestiary)
            {
                if (e.Kind == kind && before < e.KillsToComplete && before + 1 >= e.KillsToComplete)
                {
                    Profile.Coins += e.CoinReward;
                    granted += e.CoinReward;
                }
            }
            Save();
            return granted;
        }

        /// <summary>
        /// Record one boss kill for the boss bestiary. Returns coins granted
        /// if this kill completed the entry, 0 otherwise. Also feeds the boss
        /// missions and unlocks the Boss Slayer achievement on the first kill.
        /// </summary>
        public int RecordBossKill(BossKind kind)
        {
            int before = Profile.BossBestiaryKills.TryGetValue(kind, out int k) ? k : 0;
            Profile.BossBestiaryKills[kind] = before + 1;
            Profile.TotalBossKills++;
            RecordMissionProgress("daily-boss-1", 1);
            RecordMissionProgress("weekly-bosses-5", 1);
            if (Profile.TotalBossKills == 1) UnlockAchievement("boss-slayer");
            int granted = 0;
            foreach (var e in ProgressionData.BossBestiary)
            {
                if (e.Kind == kind && before < e.KillsToComplete && before + 1 >= e.KillsToComplete)
                {
                    Profile.Coins += e.CoinReward;
                    granted += e.CoinReward;
                }
            }
            Save();
            return granted;
        }

        // ---- weapon mastery + gold skins ----

        /// <summary>Add mastery XP for a weapon. Returns the weapon's new mastery level.</summary>
        public int AddWeaponMasteryXp(WeaponId weapon, int xp)
        {
            if (xp > 0)
            {
                int before = Profile.WeaponMasteryXp.TryGetValue(weapon, out int v) ? v : 0;
                Profile.WeaponMasteryXp[weapon] = before + xp;
                Save();
            }
            return Profile.WeaponMasteryLevel(weapon);
        }

        // ---- achievements ----

        /// <summary>Unlock an achievement once, granting its coins. Returns true if newly unlocked.</summary>
        public bool UnlockAchievement(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            ProgressionData.AchievementDef def = null;
            foreach (var a in ProgressionData.Achievements)
                if (a.Id == id) { def = a; break; }
            if (def == null) return false;
            if (!Profile.Achievements.Add(id)) return false;
            Profile.Coins += def.CoinReward;
            Save();
            return true;
        }

        /// <summary>Check the run-based achievements at the end of a run. Returns newly unlocked ids.</summary>
        public List<string> CheckRunAchievements(int bestWave, int runKills, int runHeadshots,
            float accuracyPct, int survivalSec)
        {
            var unlocked = new List<string>();
            if (bestWave >= 5 && UnlockAchievement("wave-5")) unlocked.Add("wave-5");
            if (bestWave >= 10 && UnlockAchievement("wave-10")) unlocked.Add("wave-10");
            if (bestWave >= 20 && UnlockAchievement("wave-20")) unlocked.Add("wave-20");
            if (runHeadshots >= 100 && UnlockAchievement("headhunter")) unlocked.Add("headhunter");
            if (Profile.TotalKills >= 1000 && UnlockAchievement("thousand-kills")) unlocked.Add("thousand-kills");
            if (accuracyPct >= 60f && runKills > 0 && UnlockAchievement("sharpshooter")) unlocked.Add("sharpshooter");
            if (survivalSec >= 600 && UnlockAchievement("survivor-10min")) unlocked.Add("survivor-10min");
            if (Profile.TotalKills >= 1 && UnlockAchievement("first-blood")) unlocked.Add("first-blood");
            if (Profile.Coins >= 5000 && UnlockAchievement("coin-hoarder")) unlocked.Add("coin-hoarder");
            return unlocked;
        }

        // ---- meta shop ----

        /// <summary>Cost of the next level of a meta upgrade, or -1 if unknown / maxed.</summary>
        public int MetaUpgradeCost(string id)
        {
            var def = ProgressionData.GetMetaUpgrade(id);
            if (def == null) return -1;
            int level = Profile.MetaUpgradeLevel(id);
            if (level >= def.MaxLevel) return -1;
            return def.CostForLevel(level);
        }

        /// <summary>Buy one level of a meta upgrade with coins. Returns false if unaffordable/maxed/unknown.</summary>
        public bool BuyMetaUpgrade(string id)
        {
            int cost = MetaUpgradeCost(id);
            if (cost < 0 || Profile.Coins < cost) return false;
            Profile.Coins -= cost;
            Profile.MetaUpgradeLevels[id] = Profile.MetaUpgradeLevel(id) + 1;
            Save();
            return true;
        }

        /// <summary>Total bonus from a meta upgrade (fraction, except health which is flat HP).</summary>
        public float MetaUpgradeBonus(string id)
        {
            var def = ProgressionData.GetMetaUpgrade(id);
            if (def == null) return 0f;
            return Profile.MetaUpgradeLevel(id) * def.BonusPerLevel;
        }

        // ---- prestige ----

        /// <summary>Prestige needs something to reset: at least one bought meta upgrade.</summary>
        public bool CanPrestige()
        {
            foreach (var kv in Profile.MetaUpgradeLevels)
                if (kv.Value > 0) return true;
            return false;
        }

        /// <summary>Reset meta upgrades for +1 prestige level (permanent +10% damage per level).</summary>
        public bool Prestige()
        {
            if (!CanPrestige()) return false;
            Profile.MetaUpgradeLevels.Clear();
            Profile.PrestigeLevel++;
            UnlockAchievement("prestiged");
            Save();
            return true;
        }

        // ---- super perks ----

        public List<ProgressionData.SuperPerkDef> AvailableSuperPerks()
        {
            var list = new List<ProgressionData.SuperPerkDef>();
            foreach (var p in ProgressionData.SuperPerks)
                if (!Profile.SuperPerksUnlocked.Contains(p.Id)) list.Add(p);
            return list;
        }

        /// <summary>
        /// Rare gold level-up offer: one random super perk the player does not
        /// own yet, or an empty list when all are owned. Call at level-up
        /// (see GameManager hook) and offer it alongside the normal perks.
        /// </summary>
        public List<ProgressionData.SuperPerkDef> RollSuperPerkOffer()
        {
            var available = AvailableSuperPerks();
            var offer = new List<ProgressionData.SuperPerkDef>();
            if (available.Count == 0) return offer;
            offer.Add(available[new System.Random().Next(available.Count)]);
            return offer;
        }

        /// <summary>Permanently unlock a super perk (it then applies every run — see GameManager).</summary>
        public bool UnlockSuperPerk(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            bool known = false;
            foreach (var p in ProgressionData.SuperPerks)
                if (p.Id == id) { known = true; break; }
            if (!known) return false;
            if (!Profile.SuperPerksUnlocked.Add(id)) return false;
            Save();
            return true;
        }

        // ---- drone companion + survivor allies ----

        public const int DroneCompanionCost = 1500;
        public const int DroneMaxLevel = 3;

        /// <summary>Buy the drone companion (once). It shoots alongside the player in-run.</summary>
        public bool BuyDroneCompanion()
        {
            if (Profile.HasDroneCompanion || Profile.Coins < DroneCompanionCost) return false;
            Profile.Coins -= DroneCompanionCost;
            Profile.HasDroneCompanion = true;
            Profile.DroneLevel = 1;
            Save();
            return true;
        }

        public bool UpgradeDrone()
        {
            if (!Profile.HasDroneCompanion || Profile.DroneLevel >= DroneMaxLevel) return false;
            int cost = 500 * (Profile.DroneLevel + 1);
            if (Profile.Coins < cost) return false;
            Profile.Coins -= cost;
            Profile.DroneLevel++;
            Save();
            return true;
        }

        /// <summary>Record a rescued survivor (Escort win). Allies join the roster.</summary>
        public void RecordSurvivorRescued()
        {
            Profile.RescuedAllies++;
            Save();
        }

        // ---- daily challenge streak ----

        /// <summary>Record a Daily Challenge win for the streak. Returns the current streak.</summary>
        public int RecordDailyChallengeWin()
        {
            int today = DayIndexToday();
            if (Profile.LastDailyChallengeDay == today) return Profile.DailyChallengeStreak;
            int streak = Profile.LastDailyChallengeDay == today - 1
                ? Profile.DailyChallengeStreak + 1 : 1;
            Profile.DailyChallengeStreak = streak;
            Profile.LastDailyChallengeDay = today;
            Save();
            return streak;
        }
    }
}
