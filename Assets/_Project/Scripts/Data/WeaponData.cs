using System.Collections.Generic;

namespace TheLastBullet.Data
{
    /// <summary>
    /// All 20 weapons. Port of the Kotlin WeaponDef table — same numbers.
    /// Damage is per bullet/pellet unless stated. magSize 0 = no ammo (melee).
    /// pierceCount = how many extra zombies a shot passes through.
    /// </summary>
    public sealed class WeaponDef
    {
        public WeaponId Id { get; }
        public string Name { get; }
        public float Damage { get; }
        public float FireRatePerSec { get; }
        public int MagSize { get; }
        public float ReloadTimeSec { get; }
        public float SpreadDegrees { get; }
        public int PierceCount { get; }
        public int Price { get; }
        public string Description { get; }
        public Rarity Rarity { get; }

        public WeaponDef(WeaponId id, string name, float damage, float fireRatePerSec,
            int magSize, float reloadTimeSec, float spreadDegrees, int pierceCount,
            int price, string description, Rarity rarity)
        {
            Id = id; Name = name; Damage = damage; FireRatePerSec = fireRatePerSec;
            MagSize = magSize; ReloadTimeSec = reloadTimeSec; SpreadDegrees = spreadDegrees;
            PierceCount = pierceCount; Price = price; Description = description; Rarity = rarity;
        }

        public float EffectiveDamage() => Damage * RarityMultiplier(Rarity);

        public static float RarityMultiplier(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return 1.15f;
                case Rarity.Epic: return 1.30f;
                case Rarity.Legendary: return 1.50f;
                default: return 1.0f;
            }
        }
    }

    public static class WeaponData
    {
        public static readonly List<WeaponDef> All = new List<WeaponDef>
        {
            new WeaponDef(WeaponId.Pistol, "Pistol", 25f, 4f, 12, 1.2f, 2f, 0, 0,
                "Reliable sidearm. Infinite ammo — you always have something to shoot.", Rarity.Common),
            new WeaponDef(WeaponId.Shotgun, "Shotgun", 18f, 1.1f, 6, 2.0f, 18f, 0, 400,
                "Fires 6 pellets in a wide spread. Devastating up close, weak at range.", Rarity.Common),
            new WeaponDef(WeaponId.Smg, "SMG", 14f, 11f, 32, 1.4f, 6f, 0, 600,
                "Bullet hose. Low damage per shot, huge fire rate, big magazine.", Rarity.Common),
            new WeaponDef(WeaponId.Rifle, "Rifle", 70f, 1.6f, 8, 1.8f, 0.5f, 1, 800,
                "Long-range precision. High damage, pierces one extra zombie.", Rarity.Rare),
            new WeaponDef(WeaponId.Crossbow, "Crossbow", 90f, 1.0f, 5, 2.2f, 1f, 4, 700,
                "Bolts pierce a whole line of zombies. Slow, but every shot counts.", Rarity.Rare),
            new WeaponDef(WeaponId.GrenadeLauncher, "Grenade Launcher", 120f, 0.8f, 4, 2.5f, 3f, 0, 1200,
                "Lobs explosive shells that damage everything in a blast radius.", Rarity.Epic),
            new WeaponDef(WeaponId.Flamethrower, "Flamethrower", 12f, 10f, 100, 2.0f, 25f, 0, 1000,
                "Melts a cone in front of you. Short range, constant burn damage.", Rarity.Rare),
            new WeaponDef(WeaponId.Minigun, "Minigun", 16f, 14f, 120, 3.0f, 8f, 0, 1500,
                "Spins up before firing, then shreds everything. Huge magazine, slow reload.", Rarity.Epic),
            new WeaponDef(WeaponId.Railgun, "Railgun", 200f, 0.7f, 3, 2.8f, 0f, 99, 1800,
                "A slug that pierces every zombie in a line. Slow, rare, unforgettable.", Rarity.Legendary),
            new WeaponDef(WeaponId.FreezeRay, "Freeze Ray", 8f, 6f, 40, 1.6f, 4f, 0, 900,
                "Low damage, but every hit slows zombies — crowd control in a beam.", Rarity.Rare),
            new WeaponDef(WeaponId.DualPistols, "Dual Pistols", 22f, 6f, 20, 1.5f, 5f, 0, 750,
                "Two pistols, twice the firepower. Fires alternating shots.", Rarity.Rare),
            new WeaponDef(WeaponId.Katana, "Katana", 80f, 2.2f, 0, 0f, 90f, 3, 500,
                "Melee swing in a wide arc. No ammo, no reload — just steel.", Rarity.Epic),
            new WeaponDef(WeaponId.AcidGun, "Acid Gun", 10f, 5f, 24, 1.8f, 5f, 0, 850,
                "Fires acid that keeps burning — damage over time stacks on every hit.", Rarity.Rare),
            new WeaponDef(WeaponId.TeslaCoil, "Tesla Coil", 45f, 1.4f, 8, 2.0f, 0f, 3, 1400,
                "Lightning arcs from the first zombie hit to up to 3 nearby zombies.", Rarity.Epic),
            new WeaponDef(WeaponId.HarpoonGun, "Harpoon Gun", 160f, 0.8f, 4, 2.4f, 0f, 2, 1100,
                "A harpoon that hits like a truck. Huge single-shot damage, slow reload.", Rarity.Epic),
            new WeaponDef(WeaponId.NailGun, "Nail Gun", 9f, 13f, 48, 1.5f, 7f, 0, 650,
                "Nails everywhere. Rapid, cheap, and surprisingly deadly up close.", Rarity.Common),
            new WeaponDef(WeaponId.PlasmaCannon, "Plasma Cannon", 150f, 0.6f, 3, 3.0f, 0f, 0, 1700,
                "Slow orbs of plasma that explode on impact. Massive area damage.", Rarity.Legendary),
            new WeaponDef(WeaponId.SawbladeLauncher, "Sawblade Launcher", 55f, 1.2f, 6, 2.2f, 2f, 5, 950,
                "Sawblades bounce between zombies, shredding everything they touch.", Rarity.Rare),
            new WeaponDef(WeaponId.ToxicSprayer, "Toxic Sprayer", 8f, 8f, 60, 1.9f, 30f, 0, 900,
                "A wide cloud of poison. Zombies caught in it keep taking damage.", Rarity.Rare),
            new WeaponDef(WeaponId.BoomerangBlade, "Boomerang Blade", 60f, 1.5f, 0, 0f, 0f, 4, 1000,
                "A spinning blade that hits zombies on the way out and again on the way back.", Rarity.Epic),
        };

        public static WeaponDef Get(WeaponId id)
        {
            foreach (var w in All)
                if (w.Id == id) return w;
            return All[0];
        }

        // ---- Mastery: XP per weapon, 5 levels, +10% damage per level above 1. ----
        public const int MasteryMaxLevel = 5;

        public static int MasteryXpForLevel(int level) => 100 * level;

        public static int MasteryLevelForXp(int xp)
        {
            int level = 1;
            int remaining = xp < 0 ? 0 : xp;
            while (level < MasteryMaxLevel && remaining >= MasteryXpForLevel(level))
            {
                remaining -= MasteryXpForLevel(level);
                level++;
            }
            return level;
        }

        public static float MasteryDamageMultiplier(int level) =>
            1f + 0.10f * (level - 1 < 0 ? 0 : level - 1);

        public static readonly Dictionary<WeaponId, string> MasteryTraits =
            new Dictionary<WeaponId, string>
            {
                { WeaponId.Pistol, "Hair Trigger" },
                { WeaponId.Shotgun, "Dragon's Breath" },
                { WeaponId.Smg, "Extended Drum" },
                { WeaponId.Rifle, "One Shot, One Kill" },
                { WeaponId.Crossbow, "Splitting Bolts" },
                { WeaponId.GrenadeLauncher, "Cluster Shells" },
                { WeaponId.Flamethrower, "Blue Flame" },
                { WeaponId.Minigun, "Overdrive" },
                { WeaponId.Railgun, "Piercing Overload" },
                { WeaponId.FreezeRay, "Deep Freeze" },
                { WeaponId.DualPistols, "Fan the Hammer" },
                { WeaponId.Katana, "Iaido Master" },
                { WeaponId.AcidGun, "Corrosive Bath" },
                { WeaponId.TeslaCoil, "Storm Surge" },
                { WeaponId.HarpoonGun, "Whale Hunter" },
                { WeaponId.NailGun, "Nail Storm" },
                { WeaponId.PlasmaCannon, "Supernova" },
                { WeaponId.SawbladeLauncher, "Razor Hurricane" },
                { WeaponId.ToxicSprayer, "Plague Cloud" },
                { WeaponId.BoomerangBlade, "Twin Return" },
            };

        public static readonly Dictionary<WeaponId, string> MasteryGoldSkins =
            new Dictionary<WeaponId, string>
            {
                { WeaponId.Pistol, "Gold Pistol" },
                { WeaponId.Shotgun, "Gilded Shotgun" },
                { WeaponId.Smg, "Gold SMG" },
                { WeaponId.Rifle, "Gold Rifle" },
                { WeaponId.Crossbow, "Gold Crossbow" },
                { WeaponId.GrenadeLauncher, "Gold Grenade Launcher" },
                { WeaponId.Flamethrower, "Gold Flamethrower" },
                { WeaponId.Minigun, "Gold Minigun" },
                { WeaponId.Railgun, "Gold Railgun" },
                { WeaponId.FreezeRay, "Gold Freeze Ray" },
                { WeaponId.DualPistols, "Gold Dual Pistols" },
                { WeaponId.Katana, "Gold Katana" },
                { WeaponId.AcidGun, "Gold Acid Gun" },
                { WeaponId.TeslaCoil, "Gold Tesla Coil" },
                { WeaponId.HarpoonGun, "Gold Harpoon Gun" },
                { WeaponId.NailGun, "Gold Nail Gun" },
                { WeaponId.PlasmaCannon, "Gold Plasma Cannon" },
                { WeaponId.SawbladeLauncher, "Gold Sawblade Launcher" },
                { WeaponId.ToxicSprayer, "Gold Toxic Sprayer" },
                { WeaponId.BoomerangBlade, "Gold Boomerang Blade" },
            };

        // ---- Fusions: two maxed guns become a super-weapon. ----
        public sealed class FusionDef
        {
            public WeaponId First { get; }
            public WeaponId Second { get; }
            public string ResultName { get; }
            public string Description { get; }

            public FusionDef(WeaponId first, WeaponId second, string resultName, string description)
            {
                First = first; Second = second; ResultName = resultName; Description = description;
            }
        }

        public static readonly List<FusionDef> Fusions = new List<FusionDef>
        {
            new FusionDef(WeaponId.Rifle, WeaponId.Railgun, "Longshot Annihilator",
                "A railgun with a rifle's fire rate. Pierces everything, never misses."),
            new FusionDef(WeaponId.Shotgun, WeaponId.GrenadeLauncher, "Boomstick",
                "Every pellet explodes. Close range becomes a no-go zone for zombies."),
            new FusionDef(WeaponId.Smg, WeaponId.Minigun, "Bullet Storm",
                "Maximum fire rate, no spin-up. A wall of lead."),
            new FusionDef(WeaponId.Flamethrower, WeaponId.FreezeRay, "Thermal Cyclone",
                "Alternating fire and ice: burns, then shatters frozen zombies."),
            new FusionDef(WeaponId.Crossbow, WeaponId.Katana, "Ronin Bow",
                "Piercing bolts plus a blade sweep — deadly at any range."),
            new FusionDef(WeaponId.Pistol, WeaponId.DualPistols, "Dead Man's Hand",
                "Four pistols' worth of fury. Critical hits explode."),
            new FusionDef(WeaponId.TeslaCoil, WeaponId.PlasmaCannon, "Storm God",
                "Lightning-charged plasma orbs that chain between zombies on impact."),
            new FusionDef(WeaponId.AcidGun, WeaponId.ToxicSprayer, "Plaguebringer",
                "Corrosive poison clouds that melt the whole horde where they stand."),
        };
    }
}
