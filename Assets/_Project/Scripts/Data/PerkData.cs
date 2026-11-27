using System.Collections.Generic;

namespace TheLastBullet.Data
{
    /// <summary>
    /// Level-up perks. When the XP bar fills, the player picks 1 of 3 offered
    /// perks. Each perk stacks up to maxStacks times per run.
    /// </summary>
    public sealed class PerkDef
    {
        public PerkId Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int MaxStacks { get; }

        public PerkDef(PerkId id, string name, string description, int maxStacks)
        {
            Id = id; Name = name; Description = description; MaxStacks = maxStacks;
        }
    }

    public static class PerkData
    {
        public static readonly List<PerkDef> All = new List<PerkDef>
        {
            new PerkDef(PerkId.FasterReload, "Fast Hands", "Reload time -15% per stack.", 3),
            new PerkDef(PerkId.BiggerMag, "Extended Mags", "Magazine size +25% per stack.", 3),
            new PerkDef(PerkId.CritChance, "Dead Eye", "Critical hit chance +10% per stack. Crits deal 2x damage.", 5),
            new PerkDef(PerkId.MoveSpeed, "Light Feet", "Move speed +10% per stack.", 3),
            new PerkDef(PerkId.MaxHealth, "Tough Skin", "Max health +25 and heal 25 HP per stack.", 5),
            new PerkDef(PerkId.SecondWeapon, "Second Weapon", "Carry a second weapon and swap between them with a button.", 1),
            new PerkDef(PerkId.Magnet, "Magnet", "Pickup radius +40% per stack. Coins, health and ammo fly to you.", 3),
            new PerkDef(PerkId.DamageUp, "Heavy Rounds", "All weapon damage +15% per stack.", 5),
        };

        public static PerkDef Get(PerkId id)
        {
            foreach (var p in All)
                if (p.Id == id) return p;
            return All[0];
        }

        /// <summary>XP needed to go from level to level + 1.</summary>
        public static int XpForLevel(int level) => 40 + (level - 1) * 25;
    }
}
