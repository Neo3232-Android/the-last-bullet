using UnityEngine;
using TheLastBullet.Core;
using TheLastBullet.Data;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// Firing patterns for all 20 weapons. Port of the Kotlin fireWeapon()
    /// switch — same numbers, same behaviours — plus the special-weapon
    /// hooks (DoT, chain lightning, bouncing blades, boomerang return) that
    /// the Bullet carries. Damage = base x rarity x perks x prestige x mastery.
    /// </summary>
    public sealed class WeaponSystem : MonoBehaviour
    {
        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        public void Fire(WeaponDef def, float aimAngleRad)
        {
            var gm = GameManager.Instance;
            var ps = gm.State.Player;
            float interval = 1f / def.FireRatePerSec;
            if (def.Id == WeaponId.Minigun)
            {
                float ramp = Mathf.Clamp(0.2f + 0.8f * (_player.SpinUp / 0.5f), 0.2f, 1f);
                interval /= ramp;
            }
            _player.FireTimer = interval;

            int masteryLevel = 1;
            if (SaveSystem.Instance != null)
                masteryLevel = SaveSystem.Instance.Profile.WeaponMasteryLevel(def.Id);
            float damage = def.EffectiveDamage()
                * ps.DamageMultiplier()
                * (1f + ps.DamageBonus)
                * WeaponData.MasteryDamageMultiplier(masteryLevel);

            float spreadRad = GameManager.Instance.EffectiveSpreadDeg(def) * Mathf.Deg2Rad;
            Vector3 muzzle = transform.position
                + GameManager.AngleToDir(aimAngleRad) * (_player.Radius + 0.2f)
                + Vector3.up * 1.1f;
            var rng = gm.Rng;

            // Per-weapon muzzle flash + ejected shell casing (Katana is melee).
            if (gm.Juice != null)
            {
                gm.Juice.SpawnMuzzleFlash(muzzle, def.Id == WeaponId.Shotgun ? 0.7f : 0.4f);
                if (def.Id != WeaponId.Katana)
                    gm.Juice.SpawnShellCasing(transform.position + Vector3.up * 1.0f);
            }

            switch (def.Id)
            {
                case WeaponId.Katana:
                    // Melee arc: hit up to pierceCount+1 zombies in front.
                    var inArc = new System.Collections.Generic.List<ZombieAI>();
                    foreach (var z in gm.Zombies)
                    {
                        if (!z.IsAlive) continue;
                        if (Vector3.Distance(z.transform.position, transform.position) > 1.7f + z.Radius) continue;
                        Vector3 to = z.transform.position - transform.position;
                        float ang = Mathf.Atan2(to.z, to.x);
                        float diff = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, aimAngleRad * Mathf.Rad2Deg));
                        if (diff < 45f) inArc.Add(z);
                    }
                    inArc.Sort((a, b) => Vector3.Distance(a.transform.position, transform.position)
                        .CompareTo(Vector3.Distance(b.transform.position, transform.position)));
                    int hits = 0;
                    foreach (var z in inArc)
                    {
                        if (hits > def.PierceCount) break;
                        gm.DamageZombie(z, damage, gm.RollCrit(),
                            (z.transform.position - transform.position).normalized);
                        if (gm.Juice != null)
                            gm.Juice.SpawnDamageNumber(z.transform.position + Vector3.up * 1.2f,
                                Mathf.RoundToInt(damage), false);
                        hits++;
                    }
                    gm.Juice.SpawnGoo(transform.position + GameManager.AngleToDir(aimAngleRad), 6);
                    Audio.SynthAudio.Instance.Shoot(def.Id);
                    break;

                case WeaponId.Shotgun:
                    for (int i = 0; i < 6; i++)
                    {
                        float a = aimAngleRad + ((float)rng.NextDouble() - 0.5f) * spreadRad * 2f;
                        SpawnBullet(muzzle, a, 18f, damage, def.PierceCount, 2f);
                    }
                    _player.AmmoInMag--;
                    gm.RegisterShotFired();
                    Audio.SynthAudio.Instance.Shoot(def.Id);
                    break;

                default:
                    float angle = aimAngleRad + ((float)rng.NextDouble() - 0.5f) * spreadRad;
                    float bulletSpeed = def.Id == WeaponId.Flamethrower ? 6f
                        : def.Id == WeaponId.GrenadeLauncher ? 10f : 18f;
                    float life = def.Id == WeaponId.Flamethrower ? 0.25f : 2f;
                    var bullet = SpawnBullet(muzzle, angle, bulletSpeed, damage, def.PierceCount, life);
                    if (bullet != null)
                    {
                        bullet.IsExplosive = def.Id == WeaponId.GrenadeLauncher || def.Id == WeaponId.PlasmaCannon;
                        bullet.AoeRadius = def.Id == WeaponId.GrenadeLauncher ? 2.5f
                            : def.Id == WeaponId.PlasmaCannon ? 2.0f : 0f;
                        bullet.SlowDuration = def.Id == WeaponId.FreezeRay ? 1.5f : 0f;
                        bullet.DotDps = def.Id == WeaponId.AcidGun ? 8f
                            : def.Id == WeaponId.ToxicSprayer ? 6f : 0f;
                        bullet.DotDuration = (def.Id == WeaponId.AcidGun || def.Id == WeaponId.ToxicSprayer) ? 3f : 0f;
                        bullet.ChainCount = def.Id == WeaponId.TeslaCoil ? 3 : 0;
                        bullet.BounceCount = def.Id == WeaponId.SawbladeLauncher ? 5 : 0;
                        bullet.IsBoomerang = def.Id == WeaponId.BoomerangBlade;
                    }
                    _player.AmmoInMag--;
                    gm.RegisterShotFired();
                    Audio.SynthAudio.Instance.Shoot(def.Id);
                    break;
            }
        }

        private Bullet SpawnBullet(Vector3 pos, float angleRad, float speed, float damage,
            int pierce, float lifeSec)
        {
            var gm = GameManager.Instance;
            var go = new GameObject("Bullet");
            go.layer = LayerMask.NameToLayer("Bullet");
            go.transform.position = pos;
            var bullet = go.AddComponent<Bullet>();
            bullet.Launch(pos, GameManager.AngleToDir(angleRad) * speed, damage, pierce,
                true, gm.RollCrit(), 0f, 0f, lifeSec, false);
            gm.Bullets.Add(bullet);
            return bullet;
        }
    }
}
