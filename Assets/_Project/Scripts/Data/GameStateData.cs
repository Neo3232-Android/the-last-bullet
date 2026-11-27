using System.Collections.Generic;

namespace TheLastBullet.Data
{
    /// <summary>
    /// Graphics quality. The engine scales particles, entity counts, glow,
    /// shadows and render resolution from this setting so low-end phones hold
    /// 60fps while high-end phones get the full treatment.
    /// </summary>
    public sealed class QualityDef
    {
        public Quality Id { get; }
        public float ParticleScale { get; }
        public int EntityCap { get; }
        public bool GlowEnabled { get; }
        public bool ShadowsEnabled { get; }
        public float ResolutionScale { get; }

        public QualityDef(Quality id, float particleScale, int entityCap,
            bool glowEnabled, bool shadowsEnabled, float resolutionScale)
        {
            Id = id; ParticleScale = particleScale; EntityCap = entityCap;
            GlowEnabled = glowEnabled; ShadowsEnabled = shadowsEnabled;
            ResolutionScale = resolutionScale;
        }
    }

    public static class QualityData
    {
        public static readonly Dictionary<Quality, QualityDef> All =
            new Dictionary<Quality, QualityDef>
            {
                { Quality.Low, new QualityDef(Quality.Low, 0.25f, 40, false, false, 0.75f) },
                { Quality.Medium, new QualityDef(Quality.Medium, 0.6f, 70, false, false, 1.0f) },
                { Quality.High, new QualityDef(Quality.High, 1.0f, 110, true, true, 1.0f) },
                { Quality.Ultra, new QualityDef(Quality.Ultra, 1.5f, 160, true, true, 1.0f) },
            };

        public static QualityDef Get(Quality q) => All[q];
    }

    /// <summary>
    /// The player's state within a run. Port of the Kotlin PlayerState,
    /// including every perk multiplier.
    /// </summary>
    public sealed class PlayerState
    {
        public int Health;
        public int MaxHealth;
        public float MoveSpeed = 3.0f;
        public WeaponId WeaponId = WeaponId.Pistol;
        public WeaponId? SecondaryWeaponId;
        public HashSet<WeaponMod> Mods = new HashSet<WeaponMod>();
        public Dictionary<PerkId, int> Perks = new Dictionary<PerkId, int>();
        public int Coins;
        public int Xp;
        public int Level = 1;
        public int Kills;
        public int Headshots;
        /// <summary>Extra damage bonus (prestige). Applied by WeaponSystem on top of DamageMultiplier().</summary>
        public float DamageBonus;

        public int PerkStacks(PerkId id) => Perks.TryGetValue(id, out int v) ? v : 0;

        public float DamageMultiplier() => 1f + 0.15f * PerkStacks(PerkId.DamageUp);
        public float ReloadMultiplier() => 1f - 0.15f * PerkStacks(PerkId.FasterReload);
        public float MagMultiplier() => 1f + 0.25f * PerkStacks(PerkId.BiggerMag);
        public float MoveSpeedMultiplier() => 1f + 0.10f * PerkStacks(PerkId.MoveSpeed);
        public float CritChance() => 0.05f + 0.10f * PerkStacks(PerkId.CritChance);
        public float MagnetRadiusMultiplier() => 1f + 0.40f * PerkStacks(PerkId.Magnet);
        public bool HasSecondWeapon() => PerkStacks(PerkId.SecondWeapon) > 0;
    }

    public sealed class WaveDef
    {
        public int WaveNumber;
        public List<KeyValuePair<ZombieKind, int>> Spawns = new List<KeyValuePair<ZombieKind, int>>();
        public float SpawnIntervalSec;
        public bool IsBossWave;

        public int TotalZombies()
        {
            int total = 0;
            foreach (var s in Spawns) total += s.Value;
            return total;
        }
    }

    public sealed class GameState
    {
        public GamePhase Phase = GamePhase.Menu;
        public GameMode Mode = GameMode.Endless;
        public MapId MapId = MapId.CityStreet;
        public int Level = 1;
        public int Wave = 1;
        public int Score;
        public float TimeSurvivedSec;
        public PlayerState Player = new PlayerState { Health = 100, MaxHealth = 100 };
        public Quality Quality = Quality.High;
    }

    /// <summary>Wave composition. Port of the Kotlin buildWave — same math.</summary>
    public static class WaveBuilder
    {
        public static WaveDef BuildWave(int waveNumber, MapDef map, System.Random random)
        {
            bool isBoss = waveNumber % 5 == 0;
            int baseCount = 6 + waveNumber * 3;
            int totalWeight = 0;
            foreach (var w in map.ZombieMix.Values) totalWeight += w;
            if (totalWeight < 1) totalWeight = 1;

            var def = new WaveDef
            {
                WaveNumber = waveNumber,
                SpawnIntervalSec = System.Math.Max(1.6f - waveNumber * 0.05f, 0.5f),
                IsBossWave = isBoss
            };

            int remaining = baseCount;
            var kinds = new List<ZombieKind>(map.ZombieMix.Keys);
            for (int i = 0; i < kinds.Count; i++)
            {
                int weight = map.ZombieMix[kinds[i]];
                int count = i == kinds.Count - 1
                    ? remaining
                    : System.Math.Max(baseCount * weight / totalWeight, 0);
                if (count > 0)
                    def.Spawns.Add(new KeyValuePair<ZombieKind, int>(kinds[i], count));
                remaining -= count;
            }

            // Late waves add a few brutes/armored even on maps that lack them.
            if (waveNumber >= 8 && random.NextDouble() < 0.5)
                def.Spawns.Add(new KeyValuePair<ZombieKind, int>(ZombieKind.Brute, waveNumber / 8));

            return def;
        }

        /// <summary>Zombie health grows with wave number (same as Kotlin).</summary>
        public static float WaveHealthScale(int waveNumber) => 1f + (waveNumber - 1) * 0.12f;
    }
}
