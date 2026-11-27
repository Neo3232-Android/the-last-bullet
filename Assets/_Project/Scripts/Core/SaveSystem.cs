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
            public int LoginStreakDays;
            public int LastLoginDay;
            public List<IntPair> BestiaryKills = new List<IntPair>();
            public List<IntPair> BossBestiaryKills = new List<IntPair>();
            public List<string> SuperPerksUnlocked = new List<string>();
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
                Achievements = new List<string>(Profile.Achievements),
            };
            foreach (var w in Profile.UnlockedWeapons) dto.UnlockedWeapons.Add((int)w);
            foreach (var kv in Profile.WeaponMasteryXp)
                dto.WeaponMasteryXp.Add(new IntPair { Key = (int)kv.Key, Value = kv.Value });
            foreach (var kv in Profile.DailyMissionProgress)
                dto.DailyMissionProgress.Add(new StringPair { Key = kv.Key, Value = kv.Value.ToString() });
            foreach (var kv in Profile.WeeklyChallengeProgress)
                dto.WeeklyChallengeProgress.Add(new StringPair { Key = kv.Key, Value = kv.Value.ToString() });
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
            };
            if (dto.UnlockedOutfits != null) p.UnlockedOutfits = new HashSet<string>(dto.UnlockedOutfits);
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
    }
}
