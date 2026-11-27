using System;
using System.Collections.Generic;

namespace TheLastBullet.Data
{
    /// <summary>Character creator choices. Colors are hex strings ("#RRGGBB").</summary>
    public sealed class CharacterDef
    {
        public string SkinTone = "#C68642";
        public HairStyle HairStyle = HairStyle.Short;
        public string HairColor = "#1A1A1A";
        public string OutfitColor = "#FFD700";
        public Accessory Accessory = Accessory.None;
        public WeaponId StartingWeaponId = WeaponId.Pistol;
        public PerkId StartingPerkId = PerkId.DamageUp;

        public static CharacterDef Default() => new CharacterDef();
    }

    public sealed class OutfitDef
    {
        public string Id { get; }
        public string Name { get; }
        public string OutfitColor { get; }
        public int Price { get; }

        public OutfitDef(string id, string name, string outfitColor, int price)
        {
            Id = id; Name = name; OutfitColor = outfitColor; Price = price;
        }
    }

    /// <summary>
    /// Persisted player profile (JSON in PlayerPrefs on the Unity side).
    /// Port of the Kotlin PlayerProfile — same fields, same rules.
    /// </summary>
    public sealed class PlayerProfile
    {
        public int Coins;
        public int BestWave;
        public int MostKills;
        public int LongestSurvivalSec;
        public float BestAccuracyPct;
        public int TotalKills;
        public int TotalRuns;
        public HashSet<string> UnlockedOutfits = new HashSet<string> { "gold" };
        public HashSet<WeaponId> UnlockedWeapons = new HashSet<WeaponId> { WeaponId.Pistol };
        public HashSet<string> Achievements = new HashSet<string>();
        public int PrestigeLevel;
        public Dictionary<string, string> Medals = new Dictionary<string, string>();
        public Dictionary<WeaponId, int> WeaponMasteryXp = new Dictionary<WeaponId, int>();
        public Dictionary<string, int> DailyMissionProgress = new Dictionary<string, int>();
        public int LastDailyMissionDay = -1;
        public Dictionary<string, int> WeeklyChallengeProgress = new Dictionary<string, int>();
        public int LastWeeklyChallengeWeek = -1;
        public int LoginStreakDays;
        public int LastLoginDay = -1;
        public Dictionary<ZombieKind, int> BestiaryKills = new Dictionary<ZombieKind, int>();
        public Dictionary<BossKind, int> BossBestiaryKills = new Dictionary<BossKind, int>();
        public HashSet<string> SuperPerksUnlocked = new HashSet<string>();

        // ---- Campaign progress (NEW in the Unity version) ----
        /// <summary>Highest unlocked level per map, keyed by map id name. Level 1 is always unlocked.</summary>
        public Dictionary<string, int> UnlockedLevelPerMap = new Dictionary<string, int>();
        /// <summary>Best medal per level, keyed "MapId-Level" (e.g. "CityStreet-5").</summary>
        public Dictionary<string, MedalTier> BestMedalPerLevel = new Dictionary<string, MedalTier>();

        public int UnlockedLevel(MapId map)
        {
            return UnlockedLevelPerMap.TryGetValue(map.ToString(), out int v) ? Math.Max(1, v) : 1;
        }

        /// <summary>Permanent damage bonus from prestige: +10% per level.</summary>
        public float PrestigeDamageBonus() => 1f + 0.10f * PrestigeLevel;

        public int WeaponMasteryLevel(WeaponId weaponId) =>
            WeaponData.MasteryLevelForXp(WeaponMasteryXp.TryGetValue(weaponId, out int xp) ? xp : 0);

        public int BestiaryCompletedCount()
        {
            int n = 0;
            foreach (var e in ProgressionData.Bestiary)
                if ((BestiaryKills.TryGetValue(e.Kind, out int k) ? k : 0) >= e.KillsToComplete) n++;
            return n;
        }

        public int BossBestiaryCompletedCount()
        {
            int n = 0;
            foreach (var e in ProgressionData.BossBestiary)
                if ((BossBestiaryKills.TryGetValue(e.Kind, out int k) ? k : 0) >= e.KillsToComplete) n++;
            return n;
        }
    }

    public static class ProgressionData
    {
        public static readonly List<OutfitDef> Outfits = new List<OutfitDef>
        {
            new OutfitDef("gold", "Gold Standard", "#FFD700", 0),
            new OutfitDef("crimson", "Crimson", "#C0392B", 500),
            new OutfitDef("forest", "Forest", "#27AE60", 500),
            new OutfitDef("midnight", "Midnight", "#2C3E50", 1000),
            new OutfitDef("toxic", "Toxic", "#9B59B6", 1000),
            new OutfitDef("arctic", "Arctic", "#AED6F1", 2000),
            new OutfitDef("inferno", "Inferno", "#E67E22", 3000),
        };

        public sealed class AchievementDef
        {
            public string Id { get; }
            public string Name { get; }
            public string Description { get; }
            public int CoinReward { get; }
            public AchievementDef(string id, string name, string description, int coinReward)
            {
                Id = id; Name = name; Description = description; CoinReward = coinReward;
            }
        }

        public static readonly List<AchievementDef> Achievements = new List<AchievementDef>
        {
            new AchievementDef("first-blood", "First Blood", "Kill your first zombie.", 50),
            new AchievementDef("wave-5", "Wave 5 Survivor", "Reach wave 5 in a single run.", 100),
            new AchievementDef("wave-10", "Double Digits", "Reach wave 10 in a single run.", 200),
            new AchievementDef("wave-20", "Unstoppable", "Reach wave 20 in a single run.", 500),
            new AchievementDef("boss-slayer", "Boss Slayer", "Kill your first boss.", 150),
            new AchievementDef("headhunter", "Headhunter", "Land 100 headshots in a single run.", 150),
            new AchievementDef("thousand-kills", "Zombie Horde", "Kill 1,000 zombies in total.", 300),
            new AchievementDef("sharpshooter", "Sharpshooter", "Finish a run with 60% accuracy or better.", 200),
            new AchievementDef("coin-hoarder", "Coin Hoarder", "Hold 5,000 coins at once.", 250),
            new AchievementDef("survivor-10min", "Survivor", "Survive 10 minutes in a single run.", 200),
            new AchievementDef("flawless-wave", "Flawless Wave", "Clear a wave without taking any damage.", 100),
            new AchievementDef("prestiged", "Reborn", "Prestige for the first time.", 500),
        };

        public sealed class DailyMission
        {
            public string Id { get; }
            public string Description { get; }
            public int Target { get; }
            public int Reward { get; }
            public DailyMission(string id, string description, int target, int reward)
            {
                Id = id; Description = description; Target = target; Reward = reward;
            }
        }

        public static readonly List<DailyMission> DailyMissionPool = new List<DailyMission>
        {
            new DailyMission("daily-kills-50", "Kill 50 zombies", 50, 100),
            new DailyMission("daily-kills-150", "Kill 150 zombies", 150, 200),
            new DailyMission("daily-headshots-25", "Land 25 headshots", 25, 150),
            new DailyMission("daily-waves-5", "Reach wave 5 in a single run", 5, 150),
            new DailyMission("daily-coins-500", "Earn 500 coins", 500, 150),
            new DailyMission("daily-boss-1", "Kill a boss", 1, 200),
            new DailyMission("daily-flawless-1", "Clear a wave without taking damage", 1, 150),
            new DailyMission("daily-elite-3", "Kill 3 elite zombies", 3, 200),
        };

        /// <summary>3 fresh missions every day, deterministic per day (same on every device).</summary>
        public static List<DailyMission> DailyMissionsForDay(int dayIndex)
        {
            var pool = new List<DailyMission>(DailyMissionPool);
            Shuffle(pool, new Random(dayIndex));
            return pool.GetRange(0, 3);
        }

        public sealed class WeeklyChallenge
        {
            public string Id { get; }
            public string Description { get; }
            public int Target { get; }
            public int Reward { get; }
            public WeeklyChallenge(string id, string description, int target, int reward)
            {
                Id = id; Description = description; Target = target; Reward = reward;
            }
        }

        public static readonly List<WeeklyChallenge> WeeklyChallengePool = new List<WeeklyChallenge>
        {
            new WeeklyChallenge("weekly-kills-1000", "Kill 1,000 zombies this week", 1000, 500),
            new WeeklyChallenge("weekly-wave-15", "Reach wave 15 in a single run", 15, 400),
            new WeeklyChallenge("weekly-bosses-5", "Kill 5 bosses this week", 5, 400),
            new WeeklyChallenge("weekly-coins-5000", "Earn 5,000 coins this week", 5000, 400),
            new WeeklyChallenge("weekly-headshots-200", "Land 200 headshots this week", 200, 350),
            new WeeklyChallenge("weekly-survive-30min", "Survive 30 minutes in total this week", 30, 300),
            new WeeklyChallenge("weekly-dailies-5", "Complete 5 daily missions this week", 5, 300),
        };

        public static List<WeeklyChallenge> WeeklyChallengesForWeek(int weekIndex)
        {
            var pool = new List<WeeklyChallenge>(WeeklyChallengePool);
            Shuffle(pool, new Random(weekIndex * 31));
            return pool.GetRange(0, 3);
        }

        public sealed class LoginReward
        {
            public int Day { get; }
            public int Coins { get; }
            public string Description { get; }
            public LoginReward(int day, int coins, string description)
            {
                Day = day; Coins = coins; Description = description;
            }
        }

        public static readonly List<LoginReward> LoginRewards = new List<LoginReward>
        {
            new LoginReward(1, 100, "Welcome back! Day 1 bonus."),
            new LoginReward(2, 150, "Day 2 — the streak grows."),
            new LoginReward(3, 200, "Day 3 — bonus coins."),
            new LoginReward(4, 250, "Day 4 — keep it going."),
            new LoginReward(5, 300, "Day 5 — almost there."),
            new LoginReward(6, 400, "Day 6 — big bonus."),
            new LoginReward(7, 500, "Day 7 — jackpot! The cycle starts over tomorrow."),
        };

        public static LoginReward LoginRewardForDay(int dayInCycle) =>
            LoginRewards[(dayInCycle - 1) % LoginRewards.Count];

        public static int LoginStreakBonus(int streakDays) => Math.Min(streakDays * 10, 200);

        public sealed class BestiaryEntry
        {
            public ZombieKind Kind { get; }
            public int KillsToComplete { get; }
            public int CoinReward { get; }
            public BestiaryEntry(ZombieKind kind, int killsToComplete, int coinReward)
            {
                Kind = kind; KillsToComplete = killsToComplete; CoinReward = coinReward;
            }
        }

        public static readonly List<BestiaryEntry> Bestiary = new List<BestiaryEntry>
        {
            new BestiaryEntry(ZombieKind.Walker, 100, 50),
            new BestiaryEntry(ZombieKind.Runner, 50, 100),
            new BestiaryEntry(ZombieKind.Crawler, 50, 100),
            new BestiaryEntry(ZombieKind.Brute, 25, 150),
            new BestiaryEntry(ZombieKind.Spitter, 25, 150),
            new BestiaryEntry(ZombieKind.Exploder, 25, 150),
            new BestiaryEntry(ZombieKind.Leaper, 25, 150),
            new BestiaryEntry(ZombieKind.Screamer, 20, 200),
            new BestiaryEntry(ZombieKind.ZombieDog, 50, 100),
            new BestiaryEntry(ZombieKind.Armored, 25, 200),
            new BestiaryEntry(ZombieKind.Bloater, 15, 250),
            new BestiaryEntry(ZombieKind.Healer, 15, 250),
            new BestiaryEntry(ZombieKind.Shieldbearer, 20, 200),
            new BestiaryEntry(ZombieKind.Ghost, 20, 250),
            new BestiaryEntry(ZombieKind.FireZombie, 20, 200),
            new BestiaryEntry(ZombieKind.Spawner, 15, 250),
        };

        public sealed class BossBestiaryEntry
        {
            public BossKind Kind { get; }
            public int KillsToComplete { get; }
            public int CoinReward { get; }
            public BossBestiaryEntry(BossKind kind, int killsToComplete, int coinReward)
            {
                Kind = kind; KillsToComplete = killsToComplete; CoinReward = coinReward;
            }
        }

        public static readonly List<BossBestiaryEntry> BossBestiary = new List<BossBestiaryEntry>
        {
            new BossBestiaryEntry(BossKind.Charger, 3, 300),
            new BossBestiaryEntry(BossKind.Summoner, 3, 300),
            new BossBestiaryEntry(BossKind.SpitterKing, 3, 400),
            new BossBestiaryEntry(BossKind.TheButcher, 2, 500),
            new BossBestiaryEntry(BossKind.PlagueQueen, 2, 500),
        };

        public sealed class SuperPerkDef
        {
            public string Id { get; }
            public string Name { get; }
            public string Description { get; }
            public string Effect { get; }
            public SuperPerkDef(string id, string name, string description, string effect)
            {
                Id = id; Name = name; Description = description; Effect = effect;
            }
        }

        public static readonly List<SuperPerkDef> SuperPerks = new List<SuperPerkDef>
        {
            new SuperPerkDef("second-wind", "Second Wind",
                "Gold perk. Once per run, survive a killing blow and revive at 50% HP.",
                "Revive once per run at 50% HP."),
            new SuperPerkDef("bullet-time", "Bullet Time",
                "Gold perk. Every headshot briefly slows time.",
                "Headshots trigger 1s of slow motion."),
            new SuperPerkDef("vampire", "Vampire",
                "Gold perk. Every kill heals 1 HP.",
                "Kills restore 1 HP."),
            new SuperPerkDef("lucky-charm", "Lucky Charm",
                "Gold perk. Elite zombies appear twice as often and drop better loot.",
                "Elite chance doubled."),
            new SuperPerkDef("adrenaline-rush", "Adrenaline Rush",
                "Gold perk. Every kill briefly boosts move speed.",
                "+20% move speed for 2s after each kill."),
            new SuperPerkDef("overkill", "Overkill",
                "Gold perk. Excess damage explodes, damaging nearby zombies.",
                "Overkill damage explodes in a small radius."),
        };

        private static void Shuffle<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
