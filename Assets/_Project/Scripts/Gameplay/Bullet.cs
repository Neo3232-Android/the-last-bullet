using UnityEngine;
using TheLastBullet.Core;
using TheLastBullet.Data;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// A projectile. Moves in FixedUpdate and resolves hits with manual
    /// sphere checks against the zombie list (cheaper and more deterministic
    /// than physics collisions at horde scale — same approach as the Kotlin
    /// engine's updateBullets). Head sphere first (crit), then body.
    ///
    /// Special behaviours ride on the bullet: explosive AoE, slow, DoT,
    /// chain lightning, bouncing blades, boomerang return.
    /// </summary>
    public sealed class Bullet : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public float Damage { get; private set; }
        public int PierceLeft { get; private set; }
        public bool IsPlayerBullet { get; private set; }
        public bool IsCrit { get; private set; }
        public bool IsExplosive { get; set; }
        public float AoeRadius { get; set; }
        public float SlowDuration { get; set; }
        public float LifeSec { get; private set; }
        public float DotDps { get; set; }
        public float DotDuration { get; set; }
        public int ChainCount { get; set; }
        public int BounceCount { get; set; }
        public bool IsBoomerang { get; set; }

        private float _radius = 0.12f;
        private float _age;
        private bool _returning;
        private GameObject _visual;

        public void Launch(Vector3 pos, Vector3 velocity, float damage, int pierce,
            bool isPlayerBullet, bool isCrit, float aoeRadius, float slowDuration,
            float lifeSec, bool isExplosive)
        {
            transform.position = pos;
            Velocity = velocity;
            Damage = damage;
            PierceLeft = pierce;
            IsPlayerBullet = isPlayerBullet;
            IsCrit = isCrit;
            AoeRadius = aoeRadius;
            SlowDuration = slowDuration;
            LifeSec = lifeSec;
            IsExplosive = isExplosive;
            _age = 0f;
            _returning = false;

            // Tiny glowing tracer so shots are visible.
            _visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _visual.name = "Tracer";
            _visual.transform.SetParent(transform, false);
            _visual.transform.localScale = Vector3.one * 0.24f;
            _visual.GetComponent<Renderer>().material.color =
                isPlayerBullet ? new Color(1f, 0.84f, 0f) : new Color(0.4f, 0.9f, 0.3f);
            Destroy(_visual.GetComponent<Collider>());
        }

        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Destroy(gameObject); return; }
            float dt = Time.fixedDeltaTime;
            _age += dt;
            LifeSec -= dt;

            if (IsBoomerang && !_returning && _age > 0.8f)
            {
                _returning = true;
                if (gm.Player != null)
                    Velocity = (gm.Player.transform.position - transform.position).normalized * Velocity.magnitude;
            }
            if (IsBoomerang && _returning && gm.Player != null)
            {
                Vector3 toPlayer = gm.Player.transform.position - transform.position;
                if (toPlayer.magnitude < 0.6f) { DestroyBullet(); return; }
                Velocity = toPlayer.normalized * Velocity.magnitude;
            }

            transform.position += Velocity * dt;

            var map = gm.LevelDirector.CurrentMap;
            float hw = map != null ? map.ArenaWidth / 2f + 1f : 30f;
            float hh = map != null ? map.ArenaHeight / 2f + 1f : 40f;
            Vector3 p = transform.position;
            if (LifeSec <= 0f || p.x < -hw || p.x > hw || p.z < -hh || p.z > hh)
            {
                if (IsExplosive && IsPlayerBullet)
                    gm.ExplodeAt(p, AoeRadius, Damage);
                DestroyBullet();
                return;
            }

            if (IsPlayerBullet)
            {
                // Head sphere first (crit), then body — nearest overlapping zombie.
                ZombieAI target = null;
                bool headshot = false;
                float best = float.MaxValue;
                foreach (var z in gm.Zombies)
                {
                    if (!z.IsAlive) continue;
                    float dHead = Vector3.Distance(p, z.HeadWorldPosition);
                    if (dHead < _radius + z.HeadRadius && dHead < best)
                    {
                        target = z; headshot = true; best = dHead;
                        continue;
                    }
                    float dBody = Vector3.Distance(p, z.transform.position + Vector3.up * 0.9f);
                    if (dBody < _radius + z.Radius && dBody < best)
                    {
                        target = z; headshot = false; best = dBody;
                    }
                }
                if (target != null)
                {
                    gm.RegisterShotHit();
                    bool crit = IsCrit || headshot;
                    if (headshot) gm.RegisterHeadshot();
                    if (IsExplosive)
                    {
                        gm.ExplodeAt(p, AoeRadius, Damage);
                        DestroyBullet();
                        return;
                    }
                    ApplyHit(target, crit);
                    if (PierceLeft > 0) PierceLeft--;
                    else if (BounceCount > 0) BounceToNext(target);
                    else DestroyBullet();
                }
            }
            else
            {
                // Enemy goo: only the player can be hit.
                var player = gm.Player;
                if (player != null && player.IsAlive &&
                    Vector3.Distance(p, player.transform.position + Vector3.up * 0.9f) <
                    _radius + player.Radius)
                {
                    gm.DamagePlayer(Mathf.RoundToInt(Damage));
                    DestroyBullet();
                }
            }
        }

        private void ApplyHit(ZombieAI target, bool crit)
        {
            var gm = GameManager.Instance;
            float amount = Damage;
            if (crit)
            {
                amount *= GameManager.BaseCritDamageMult;
                Audio.SynthAudio.Instance.Headshot();
            }
            // Armored zombies shrug off body shots; crits hit the weak point.
            if (target.Kind.HasValue && target.Kind.Value == ZombieKind.Armored && !crit)
                amount *= 0.5f;
            if (gm.IsInstaKillActive) amount = float.MaxValue;

            Vector3 hitDir = (target.transform.position - transform.position).normalized;
            gm.DamageZombie(target, amount, crit, hitDir);
            if (!target.IsAlive) return;

            if (SlowDuration > 0f)
            {
                target.SlowUntil = gm.SimTime + SlowDuration;
                target.SlowFactor = 0.5f;
            }
            if (DotDps > 0f)
            {
                target.BurnUntil = gm.SimTime + DotDuration;
                target.BurnDps = DotDps;
            }
            if (ChainCount > 0)
            {
                int chained = 0;
                foreach (var z in gm.Zombies)
                {
                    if (chained >= ChainCount) break;
                    if (!z.IsAlive || z == target) continue;
                    if (Vector3.Distance(z.transform.position, target.transform.position) < 3f)
                    {
                        gm.DamageZombie(z, Damage * 0.7f, false,
                            (z.transform.position - target.transform.position).normalized);
                        chained++;
                    }
                }
            }
        }

        private void BounceToNext(ZombieAI from)
        {
            var gm = GameManager.Instance;
            ZombieAI next = null;
            float best = float.MaxValue;
            foreach (var z in gm.Zombies)
            {
                if (!z.IsAlive || z == from) continue;
                float d = Vector3.Distance(z.transform.position, from.transform.position);
                if (d < 4f && d < best) { best = d; next = z; }
            }
            if (next == null) { DestroyBullet(); return; }
            BounceCount--;
            Vector3 dir = (next.transform.position - transform.position).normalized;
            Velocity = dir * Velocity.magnitude;
        }

        private void DestroyBullet()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.Bullets.Remove(this);
            Destroy(gameObject);
        }
    }
}
