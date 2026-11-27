using System;
using NUnit.Framework;
using TheLastBullet.Data;

namespace TheLastBullet.Tests
{
    /// <summary>
    /// EditMode tests for the pure data layer — no scene, no GameObjects.
    /// These run in GitHub Actions (testMode: EditMode) on every push, so a
    /// balance or data regression fails the build before an APK is produced.
    /// </summary>
    public sealed class GameDataTests
    {
        [Test]
        public void DamageMath_RarityMultipliesBase()
        {
            var pistol = WeaponData.Get(WeaponId.Pistol);
            Assert.AreEqual(pistol.Damage, pistol.EffectiveDamage(), 0.001f); // Common = 1.0x
            Assert.AreEqual(1.15f, WeaponDef.RarityMultiplier(Rarity.Rare), 0.001f);
            Assert.AreEqual(1.30f, WeaponDef.RarityMultiplier(Rarity.Epic), 0.001f);
            Assert.AreEqual(1.50f, WeaponDef.RarityMultiplier(Rarity.Legendary), 0.001f);
            var railgun = WeaponData.Get(WeaponId.Railgun);
            Assert.AreEqual(railgun.Damage * 1.5f, railgun.EffectiveDamage(), 0.001f);
        }

        [Test]
        public void WaveBuilder_CountGrowsWithWaveNumber()
        {
            var map = MapData.Get(MapId.CityStreet);
            var rng = new Random(42);
            var w1 = WaveBuilder.BuildWave(1, map, rng);
            var w10 = WaveBuilder.BuildWave(10, map, rng);
            Assert.Greater(w10.TotalZombies(), w1.TotalZombies());
            Assert.IsTrue(w1.SpawnIntervalSec >= w10.SpawnIntervalSec);
            Assert.IsFalse(w1.IsBossWave);
            Assert.IsTrue(WaveBuilder.BuildWave(5, map, rng).IsBossWave);
            Assert.IsTrue(WaveBuilder.BuildWave(10, map, rng).IsBossWave);
        }

        [Test]
        public void PerkStacking_DamageMultiplierGrows()
        {
            var ps = new PlayerState { Health = 100, MaxHealth = 100 };
            Assert.AreEqual(1f, ps.DamageMultiplier(), 0.001f);
            ps.Perks[PerkId.DamageUp] = 1;
            Assert.AreEqual(1.15f, ps.DamageMultiplier(), 0.001f);
            ps.Perks[PerkId.DamageUp] = 3;
            Assert.AreEqual(1.45f, ps.DamageMultiplier(), 0.001f);
            Assert.AreEqual(3, ps.PerkStacks(PerkId.DamageUp));
            Assert.AreEqual(0, ps.PerkStacks(PerkId.MaxHealth));
        }

        [Test]
        public void MasteryLevels_Thresholds()
        {
            Assert.AreEqual(1, WeaponData.MasteryLevelForXp(0));
            Assert.AreEqual(1, WeaponData.MasteryLevelForXp(-50)); // negative clamps to 0
            Assert.Greater(WeaponData.MasteryLevelForXp(10000), 1);
            Assert.AreEqual(WeaponData.MasteryMaxLevel, WeaponData.MasteryLevelForXp(1000000));
            Assert.AreEqual(1f, WeaponData.MasteryDamageMultiplier(1), 0.001f);
            Assert.AreEqual(1.4f, WeaponData.MasteryDamageMultiplier(5), 0.001f);
        }

        [Test]
        public void MutationRotation_DeterministicPerDay()
        {
            var a = ZombieData.MutationForDay(7);
            var b = ZombieData.MutationForDay(7);
            Assert.AreEqual(a.Name, b.Name); // same day, same mutation, every device
            var names = new System.Collections.Generic.HashSet<string>();
            for (int d = 0; d < 5; d++) names.Add(ZombieData.MutationForDay(d).Name);
            Assert.AreEqual(5, names.Count); // all 5 mutations appear in a 5-day rotation
            Assert.IsNotNull(ZombieData.MutationForDay(-1)); // negative days wrap safely
        }

        [Test]
        public void LevelGeneration_TwoHundredLevels()
        {
            Assert.AreEqual(20, MapData.All.Count);
            Assert.AreEqual(10, LevelData.LevelsPerMap);
            Assert.AreEqual(200, LevelData.TotalLevels);
            foreach (var map in MapData.All)
            {
                Assert.GreaterOrEqual(map.Zones.Count, 3);
                Assert.LessOrEqual(map.Zones.Count, 5);
                for (int lvl = 1; lvl <= 10; lvl++)
                {
                    var def = LevelData.GetLevel(map.Id, lvl);
                    Assert.AreEqual(lvl, def.Level);
                    Assert.AreEqual(map.Id, def.Map);
                    Assert.AreEqual(lvl == 5 || lvl == 10, def.IsBossLevel);
                    Assert.Greater(def.HealthScale, 0f);
                    Assert.Greater(def.CountScale, 0f);
                    Assert.GreaterOrEqual(def.SpawnIntervalSec, 0.5f);
                    Assert.LessOrEqual(def.EliteChance, 0.15f);
                    Assert.GreaterOrEqual(def.ZonesOpen, 1);
                    Assert.LessOrEqual(def.ZonesOpen, map.Zones.Count);
                    Assert.Greater(def.ParTimeSec, 0);
                    Assert.Greater(def.CoinReward, 0);
                    Assert.Greater(def.WaveCount, 0);
                    if (lvl == 1) Assert.AreEqual(0, def.Events.Count); // level 1: no events yet
                    if (lvl == 10) Assert.GreaterOrEqual(def.Events.Count, 2); // level 10: event storm
                }
            }
        }

        [Test]
        public void EliteChance_CapsAtFifteenPercent()
        {
            Assert.AreEqual(0.025f, ZombieData.EliteChanceForLevel(1), 0.001f);
            Assert.AreEqual(0.15f, ZombieData.EliteChanceForLevel(100), 0.001f);
            Assert.AreEqual(2.5f, ZombieData.EliteHealthMultiplier, 0.001f);
            Assert.AreEqual(3f, ZombieData.EliteCoinMultiplier, 0.001f);
        }

        [Test]
        public void MedalForRun_PlatinumRequiresNoDamageAndFast()
        {
            var def = LevelData.GetLevel(MapId.CityStreet, 1);
            Assert.AreEqual(MedalTier.Platinum,
                LevelData.MedalForRun(def, def.ParTimeSec * 0.5f, 0f, 100));
            Assert.AreEqual(MedalTier.Gold,
                LevelData.MedalForRun(def, def.ParTimeSec * 0.9f, 10f, 100));
            Assert.AreEqual(MedalTier.Silver,
                LevelData.MedalForRun(def, def.ParTimeSec * 0.9f, 40f, 100));
            Assert.AreEqual(MedalTier.Bronze,
                LevelData.MedalForRun(def, def.ParTimeSec * 2f, 90f, 100));
        }

        [Test]
        public void XpCurve_IncreasesPerLevel()
        {
            Assert.Greater(PerkData.XpForLevel(2), PerkData.XpForLevel(1));
            Assert.Greater(PerkData.XpForLevel(10), PerkData.XpForLevel(5));
        }
    }
}
