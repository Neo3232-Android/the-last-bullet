using System;
using System.Collections.Generic;

namespace TheLastBullet.Data
{
    /// <summary>
    /// The 16 zombie types + 5 bosses. Port of the Kotlin ZombieDef table — same numbers.
    /// Speed is in world units per second (player base move speed is ~3.0).
    /// </summary>
    public sealed class ZombieDef
    {
        public ZombieKind Kind { get; }
        public string Name { get; }
        public float Health { get; }
        public float Speed { get; }
        public int Damage { get; }
        public float Radius { get; }
        public int ScoreValue { get; }
        public int CoinValue { get; }
        public string Behavior { get; }

        public ZombieDef(ZombieKind kind, string name, float health, float speed, int damage,
            float radius, int scoreValue, int coinValue, string behavior)
        {
            Kind = kind; Name = name; Health = health; Speed = speed; Damage = damage;
            Radius = radius; ScoreValue = scoreValue; CoinValue = coinValue; Behavior = behavior;
        }
    }

    public sealed class BossDef
    {
        public BossKind Kind { get; }
        public string Name { get; }
        public float Health { get; }
        public float Speed { get; }
        public int Damage { get; }
        public float Radius { get; }
        public int ScoreValue { get; }
        public int CoinValue { get; }
        public string SpecialAttack { get; }

        public BossDef(BossKind kind, string name, float health, float speed, int damage,
            float radius, int scoreValue, int coinValue, string specialAttack)
        {
            Kind = kind; Name = name; Health = health; Speed = speed; Damage = damage;
            Radius = radius; ScoreValue = scoreValue; CoinValue = coinValue; SpecialAttack = specialAttack;
        }
    }

    public static class ZombieData
    {
        public static readonly List<ZombieDef> All = new List<ZombieDef>
        {
            new ZombieDef(ZombieKind.Walker, "Walker", 40f, 0.8f, 10, 0.45f, 10, 5,
                "Shambles straight at the player. The baseline horde."),
            new ZombieDef(ZombieKind.Runner, "Runner", 30f, 2.2f, 10, 0.4f, 15, 7,
                "Fast and relentless. Closes distance quickly; prioritize them."),
            new ZombieDef(ZombieKind.Crawler, "Crawler", 25f, 1.9f, 8, 0.3f, 15, 6,
                "Low to the ground and fast. Harder to hit — smaller target."),
            new ZombieDef(ZombieKind.Brute, "Brute", 220f, 0.5f, 25, 0.7f, 40, 15,
                "Huge tank. Slow, but soaks bullets and hits like a truck."),
            new ZombieDef(ZombieKind.Spitter, "Spitter", 50f, 0.7f, 12, 0.45f, 25, 10,
                "Keeps its distance and lobs goo projectiles at the player."),
            new ZombieDef(ZombieKind.Exploder, "Exploder", 35f, 1.4f, 30, 0.4f, 25, 10,
                "Blows up on death, damaging everything nearby — including other zombies."),
            new ZombieDef(ZombieKind.Leaper, "Leaper", 45f, 1.2f, 15, 0.4f, 30, 12,
                "Walks until in range, then leaps at the player in a fast arc."),
            new ZombieDef(ZombieKind.Screamer, "Screamer", 60f, 0.9f, 8, 0.45f, 35, 12,
                "Periodically screams, summoning 2-3 extra zombies to the fight."),
            new ZombieDef(ZombieKind.ZombieDog, "Zombie Dog", 20f, 3.0f, 8, 0.25f, 15, 6,
                "Very fast and small. Comes in packs; hard to hit at range."),
            new ZombieDef(ZombieKind.Armored, "Armored", 150f, 0.7f, 15, 0.5f, 40, 15,
                "Bullets bounce off its armor — only headshots and its exposed back deal full damage."),
            new ZombieDef(ZombieKind.Bloater, "Bloater", 300f, 0.4f, 20, 0.8f, 60, 30,
                "A bloated tank with huge HP. Drops a shower of bonus coins when killed."),
            new ZombieDef(ZombieKind.Healer, "Healer", 70f, 0.8f, 5, 0.45f, 50, 20,
                "Heals nearby zombies over time. Kill it first or the horde never dies."),
            new ZombieDef(ZombieKind.Shieldbearer, "Shieldbearer", 120f, 0.7f, 12, 0.5f, 45, 18,
                "Carries a frontal shield — bullets from the front bounce off. Flank it."),
            new ZombieDef(ZombieKind.Ghost, "Ghost", 50f, 1.3f, 12, 0.4f, 45, 18,
                "Phases straight through barricades. Your walls mean nothing to it."),
            new ZombieDef(ZombieKind.FireZombie, "Fire Zombie", 60f, 1.0f, 15, 0.45f, 40, 15,
                "Burning — it hurts on touch and leaves a trail of fire behind it."),
            new ZombieDef(ZombieKind.Spawner, "Spawner", 90f, 0.6f, 10, 0.55f, 50, 20,
                "Periodically births crawlers from its own body. Kill it before it multiplies."),
        };

        public static ZombieDef Get(ZombieKind kind)
        {
            foreach (var z in All)
                if (z.Kind == kind) return z;
            return All[0];
        }

        public static readonly List<BossDef> Bosses = new List<BossDef>
        {
            new BossDef(BossKind.Charger, "The Charger", 1500f, 1.0f, 35, 0.9f, 500, 150,
                "Roars, then charges at the player in a straight line at 4x speed. Dodge sideways."),
            new BossDef(BossKind.Summoner, "The Summoner", 1200f, 0.6f, 20, 0.9f, 500, 150,
                "Periodically raises 3-5 Walkers or Runners from the ground around itself."),
            new BossDef(BossKind.SpitterKing, "Spitter King", 1800f, 0.5f, 25, 1.0f, 750, 200,
                "Lobs volleys of explosive goo and leaves acid pools that damage the player on contact."),
            new BossDef(BossKind.TheButcher, "The Butcher", 2500f, 0.7f, 40, 1.1f, 1000, 300,
                "Swings a massive cleaver in wide arcs that hit everything around it. Keep your distance."),
            new BossDef(BossKind.PlagueQueen, "The Plague Queen", 2000f, 0.5f, 25, 1.0f, 1000, 300,
                "Summons swarms of crawlers and sprays poison clouds across the arena."),
        };

        public static BossDef GetBoss(BossKind kind)
        {
            foreach (var b in Bosses)
                if (b.Kind == kind) return b;
            return Bosses[0];
        }

        /// <summary>Boss for a level: cycles Charger -&gt; Summoner -&gt; Spitter King -&gt; The Butcher -&gt; Plague Queen.</summary>
        public static BossDef BossForLevel(int mapIndex, int bossNumber)
        {
            return Bosses[(mapIndex + bossNumber) % Bosses.Count];
        }

        // ---- Elites: 2.5x HP, 1.15x speed, 3x coins, 2x score. ----
        public const float EliteHealthMultiplier = 2.5f;
        public const float EliteSpeedMultiplier = 1.15f;
        public const float EliteCoinMultiplier = 3f;
        public const float EliteScoreMultiplier = 2f;

        /// <summary>Chance a spawn is an Elite. Grows with level, capped at 15%.</summary>
        public static float EliteChanceForLevel(int level) =>
            Math.Min(0.02f + level * 0.005f, 0.15f);

        // ---- Daily mutations: one modifier per day, same on every device. ----
        public sealed class DailyMutation
        {
            public string Name { get; }
            public string Description { get; }
            public float ZombieSpeedMultiplier { get; }
            public float CoinMultiplier { get; }
            public float EliteChanceBonus { get; }
            public float ZombieHealthMultiplier { get; }
            public float ZombieCountMultiplier { get; }

            public DailyMutation(string name, string description, float speed = 1f,
                float coins = 1f, float eliteBonus = 0f, float health = 1f, float count = 1f)
            {
                Name = name; Description = description; ZombieSpeedMultiplier = speed;
                CoinMultiplier = coins; EliteChanceBonus = eliteBonus; ZombieHealthMultiplier = health;
                ZombieCountMultiplier = count;
            }
        }

        public static readonly List<DailyMutation> DailyMutations = new List<DailyMutation>
        {
            new DailyMutation("Faster", "Zombies move 25% faster today.", speed: 1.25f),
            new DailyMutation("DoubleCoins", "All coin drops are doubled today.", coins: 2f),
            new DailyMutation("MoreElites", "Elite zombies appear far more often today.", eliteBonus: 0.10f),
            new DailyMutation("Tougher", "Zombies have 50% more health today.", health: 1.5f),
            new DailyMutation("Swarm", "30% more zombies today, but each has 20% less health.", health: 0.8f, count: 1.3f),
        };

        public static DailyMutation MutationForDay(int dayIndex)
        {
            int i = dayIndex % DailyMutations.Count;
            if (i < 0) i += DailyMutations.Count;
            return DailyMutations[i];
        }
    }
}
