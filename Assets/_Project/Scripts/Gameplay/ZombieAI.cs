using UnityEngine;
using TheLastBullet.Core;
using TheLastBullet.Data;
using TheLastBullet.Ragdoll;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// One zombie (or boss). Port of the Kotlin ZombieEntity + updateZombies()
    /// rules: chase, per-kind behaviour, boss specials, attack lunge, burn /
    /// freeze / slow status, spawn rise, boss intro, death → ragdoll.
    ///
    /// Movement is Rigidbody velocity-based. Separation from other zombies is
    /// done by GameManager once per fixed step (cheap O(n^2) over alive).
    /// Stage 1 drives a placeholder capsule figure tinted per kind; stage 2
    /// swaps it for the real FBX model via SetModel().
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class ZombieAI : MonoBehaviour
    {
        public ZombieKind? Kind { get; private set; }
        public BossKind? BossKind { get; private set; }
        public float Health { get; set; }
        public float MaxHealth { get; private set; }
        public float Speed { get; private set; }
        public int Damage { get; private set; }
        public float Radius { get; private set; } = 0.45f;
        public bool IsElite { get; private set; }
        public bool IsBoss => BossKind.HasValue;
        public bool IsAlive => DeathState == DeathState.None;
        public DeathState DeathState { get; private set; } = DeathState.None;
        public float DeathTime { get; private set; }
        public float BossIntroTimer { get; set; }
        public float SpawnProgress { get; private set; }
        public SpawnVariant SpawnVariant { get; private set; } = SpawnVariant.RiseFromGround;
        public float HitFlashUntil { get; set; }
        public float SlowUntil { get; set; }
        public float SlowFactor { get; set; } = 1f;
        public float FrozenUntil { get; set; }
        public float BurnUntil { get; set; }
        public float BurnDps { get; set; }
        public AttackState AttackState { get; private set; } = AttackState.None;
        public float FacingAngle { get; private set; }
        public float WalkPhase { get; private set; }
        public float HeadHeight => IsBoss ? 2.6f : Radius * 2f + 1.1f;

        public Vector3 HeadWorldPosition => transform.position + Vector3.up * HeadHeight;
        public float HeadRadius => Radius * 0.6f;

        private float _attackCooldown;
        private float _attackTimer;
        private float _leapCooldown;
        private float _leapTimer;
        private bool _isLeaping;
        private Vector3 _leapVel;
        private float _screamCooldown;
        private float _summonCooldown;
        private float _spitCooldown;
        private float _chargeTimer;
        private float _chargeCooldown;
        private bool _isCharging;
        private bool _isTelegraphing;
        private Vector3 _chargeVel;
        private Rigidbody _rb;
        private Animator _animator;
        private RagdollController _ragdoll;
        private GameObject _placeholderModel;
        private Renderer[] _placeholderRenderers;
        private float _spawnRiseFromY;

        public void Setup(ZombieKind kind, ZombieDef def, float healthScale, float speedScale, bool isElite)
        {
            Kind = kind;
            BossKind = null;
            InitCommon(def.Health * healthScale, def.Speed * speedScale, def.Damage, def.Radius, isElite);
            BuildPlaceholderModel(KindColor(kind), IsBoss ? 2.2f : KindScale(kind));
        }

        public void Setup(BossKind bossKind, BossDef def, float healthScale)
        {
            BossKind = bossKind;
            Kind = null;
            InitCommon(def.Health * healthScale, def.Speed, def.Damage, def.Radius, false);
            BuildPlaceholderModel(new Color(0.45f, 0.05f, 0.05f), 2.6f);
        }

        private void InitCommon(float health, float speed, int damage, float radius, bool isElite)
        {
            Health = health;
            MaxHealth = health;
            Speed = speed;
            Damage = damage;
            Radius = radius;
            IsElite = isElite;
            if (isElite)
            {
                Health *= ZombieData.EliteHealthMultiplier;
                MaxHealth = Health;
                Speed *= ZombieData.EliteSpeedMultiplier;
            }
            SpawnProgress = 0f;
            var gm = GameManager.Instance;
            double roll = gm != null ? gm.Rng.NextDouble() : 0.5;
            SpawnVariant = roll < 0.6 ? SpawnVariant.RiseFromGround
                : roll < 0.85 ? SpawnVariant.BurstManhole
                : SpawnVariant.DropFromAbove;
            _spawnRiseFromY = SpawnVariant == SpawnVariant.DropFromAbove ? 8f : -1.5f;

            _rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var col = GetComponent<CapsuleCollider>() ?? gameObject.AddComponent<CapsuleCollider>();
            col.radius = Radius;
            col.height = Radius * 2f + 1.1f;
            col.center = new Vector3(0f, col.height / 2f, 0f);
            _animator = GetComponentInChildren<Animator>();
            _ragdoll = GetComponentInChildren<RagdollController>();
        }

        /// <summary>Stage 2: replace the placeholder with the real FBX model.</summary>
        public void SetModel(GameObject modelRoot, float scale)
        {
            if (_placeholderModel != null) Destroy(_placeholderModel);
            modelRoot.transform.SetParent(transform, false);
            modelRoot.transform.localPosition = Vector3.zero;
            modelRoot.transform.localScale = Vector3.one * scale;
            _animator = modelRoot.GetComponentInChildren<Animator>();
            _ragdoll = modelRoot.GetComponentInChildren<RagdollController>();
        }

        private static Color KindColor(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Runner: return new Color(0.69f, 0.23f, 0.18f);
                case ZombieKind.Crawler: return new Color(0.49f, 0.4f, 0.03f);
                case ZombieKind.Brute: return new Color(0.29f, 0.14f, 0.35f);
                case ZombieKind.Spitter: return new Color(0.12f, 0.52f, 0.29f);
                case ZombieKind.Exploder: return new Color(0.75f, 0.22f, 0.17f);
                case ZombieKind.Leaper: return new Color(0.18f, 0.53f, 0.76f);
                case ZombieKind.Screamer: return new Color(0.96f, 0.96f, 0.97f);
                case ZombieKind.ZombieDog: return new Color(0.43f, 0.29f, 0.16f);
                case ZombieKind.Armored: return new Color(0.36f, 0.43f, 0.47f);
                case ZombieKind.Bloater: return new Color(0.49f, 0.4f, 0.03f);
                case ZombieKind.Healer: return new Color(0.15f, 0.68f, 0.38f);
                case ZombieKind.Shieldbearer: return new Color(0.17f, 0.24f, 0.31f);
                case ZombieKind.Ghost: return new Color(0.36f, 0.68f, 0.89f);
                case ZombieKind.FireZombie: return new Color(0.9f, 0.4f, 0.13f);
                case ZombieKind.Spawner: return new Color(0.42f, 0.2f, 0.51f);
                default: return new Color(0.42f, 0.49f, 0.37f); // Walker grey-green
            }
        }

        private static float KindScale(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Brute: return 1.4f;
                case ZombieKind.Bloater: return 1.6f;
                case ZombieKind.ZombieDog: return 0.6f;
                case ZombieKind.Crawler: return 0.7f;
                default: return 1f;
            }
        }

        private void BuildPlaceholderModel(Color color, float scale)
        {
            _placeholderModel = new GameObject("PlaceholderModel");
            _placeholderModel.transform.SetParent(transform, false);
            _placeholderModel.transform.localScale = Vector3.one * scale;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(_placeholderModel.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.GetComponent<Renderer>().material.color = color;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(_placeholderModel.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;
            head.GetComponent<Renderer>().material.color = Color.Lerp(color, Color.black, 0.3f);

            if (IsElite)
            {
                // Golden glow outline for elites.
                var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glow.name = "EliteGlow";
                glow.transform.SetParent(_placeholderModel.transform, false);
                glow.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                glow.transform.localScale = Vector3.one * 1.4f;
                var mat = glow.GetComponent<Renderer>().material;
                mat.color = new Color(1f, 0.84f, 0f, 0.35f);
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_AlphaClip", 0f);
                mat.SetFloat("_SrcBlend", 1f);
                mat.SetFloat("_DstBlend", 10f);
                mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = 3000;
            }

            _placeholderRenderers = _placeholderModel.GetComponentsInChildren<Renderer>();
            foreach (var c in _placeholderModel.GetComponentsInChildren<Collider>())
                Destroy(c);
        }

        public void BeginDeath()
        {
            if (DeathState != DeathState.None) return;
            DeathState = DeathState.Falling;
            DeathTime = GameManager.Instance.SimTime;
            AttackState = AttackState.None;
            _rb.velocity = Vector3.zero;
            if (_ragdoll != null)
            {
                Vector3 dir = Vector3.zero;
                var player = GameManager.Instance.Player;
                if (player != null)
                    dir = (transform.position - player.transform.position).normalized;
                _ragdoll.EnableRagdoll(dir);
            }
            else if (_placeholderModel != null)
            {
                _placeholderModel.transform.Rotate(-90f, 0f, 0f); // fall over
            }
        }

        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State.Phase != GamePhase.Playing) return;
            float dt = Time.fixedDeltaTime;
            float simTime = gm.SimTime;

            // Dying: collapse + fade, no movement or attacks. Ragdoll owns the pose.
            if (DeathState == DeathState.Falling) return;

            // Spawning: rise out of the ground, can't act yet.
            if (SpawnProgress < 1f)
            {
                SpawnProgress = Mathf.Min(1f, SpawnProgress + dt / GameManager.SpawnRiseSec);
                Vector3 p = transform.position;
                p.y = Mathf.Lerp(_spawnRiseFromY, 0f, SpawnProgress);
                transform.position = p;
                return;
            }

            // Boss intro: roaring, can't move or attack yet.
            if (BossIntroTimer > 0f)
            {
                BossIntroTimer -= dt;
                var player = gm.Player;
                if (player != null)
                {
                    Vector3 to = player.transform.position - transform.position;
                    FacingAngle = Mathf.Atan2(to.z, to.x);
                    transform.rotation = Quaternion.Euler(0f, GameManager.AngleToYaw(FacingAngle), 0f);
                }
                return;
            }

            // Burning: damage over time (ticks even while frozen).
            if (simTime < BurnUntil)
            {
                gm.DamageZombie(this, BurnDps * dt, false, Vector3.up);
                if (!IsAlive) return;
            }
            // Frozen: cannot move or attack until thawed.
            if (simTime < FrozenUntil) return;

            _attackCooldown = Mathf.Max(0f, _attackCooldown - dt);
            _leapCooldown = Mathf.Max(0f, _leapCooldown - dt);
            _screamCooldown = Mathf.Max(0f, _screamCooldown - dt);
            _summonCooldown = Mathf.Max(0f, _summonCooldown - dt);
            _spitCooldown = Mathf.Max(0f, _spitCooldown - dt);
            _chargeCooldown = Mathf.Max(0f, _chargeCooldown - dt);
            if (simTime > SlowUntil) SlowFactor = 1f;

            var playerPos = gm.Player != null ? gm.Player.transform.position : transform.position;
            Vector3 toPlayer = playerPos - transform.position;
            float dist = toPlayer.magnitude;
            Vector3 dir = dist > 0.001f ? toPlayer / dist : Vector3.zero;
            FacingAngle = Mathf.Atan2(toPlayer.z, toPlayer.x);
            transform.rotation = Quaternion.Euler(0f, GameManager.AngleToYaw(FacingAngle), 0f);

            UpdateBossSpecials(dt, dir, dist);
            if (!IsAlive) return;

            // Per-kind behaviour (port of the Kotlin updateZombies switch).
            if (Kind.HasValue)
            {
                switch (Kind.Value)
                {
                    case ZombieKind.Spitter:
                        if (dist < 7f)
                        {
                            if (_spitCooldown <= 0f)
                            {
                                _spitCooldown = 2.5f;
                                gm.SpawnEnemyBullet(transform.position + Vector3.up,
                                    FacingAngle, 5f, Damage, 3f);
                            }
                        }
                        else MoveZombie(dir, dt);
                        break;
                    case ZombieKind.Leaper:
                        if (_isLeaping)
                        {
                            _leapTimer -= dt;
                            transform.position += _leapVel * dt;
                            if (_leapTimer <= 0f)
                            {
                                _isLeaping = false;
                                _leapCooldown = 3f;
                            }
                        }
                        else if (dist >= 2.5f && dist <= 5f && _leapCooldown <= 0f)
                        {
                            _isLeaping = true;
                            _leapTimer = 0.45f;
                            _leapVel = dir * 6f;
                        }
                        else MoveZombie(dir, dt);
                        break;
                    case ZombieKind.Screamer:
                        if (_screamCooldown <= 0f)
                        {
                            _screamCooldown = 8f;
                            int n = 2 + gm.Rng.Next(2);
                            for (int i = 0; i < n; i++) gm.SummonNear(transform.position, null);
                        }
                        MoveZombie(dir, dt);
                        break;
                    default:
                        MoveZombie(dir, dt);
                        break;
                }
            }
            else
            {
                MoveZombie(dir, dt);
            }

            // Barricade blocking: a zombie overlapping a barricade chews on it.
            // Ghosts phase straight through barricades.
            BarricadeEntity blocker = null;
            if (!Kind.HasValue || Kind.Value != ZombieKind.Ghost)
            {
                foreach (var b in gm.Barricades)
                {
                    if (Mathf.Abs(transform.position.x - b.Position.x) < b.Width / 2f + Radius &&
                        Mathf.Abs(transform.position.z - b.Position.z) < b.Depth / 2f + Radius)
                    { blocker = b; break; }
                }
            }
            if (blocker != null && _attackCooldown <= 0f)
            {
                _attackCooldown = 1f;
                blocker.Health -= Damage;
                if (blocker.Health <= 0f)
                {
                    blocker.DestroyView();
                    gm.Barricades.Remove(blocker);
                }
            }

            // Attack lunge: windup, then lunge and deal damage once.
            bool inRange = gm.Player != null && gm.Player.IsAlive &&
                Vector3.Distance(transform.position, gm.Player.transform.position) <
                Radius + 0.25f + gm.Player.Radius;
            switch (AttackState)
            {
                case AttackState.None:
                    if (inRange && _attackCooldown <= 0f && blocker == null)
                    {
                        AttackState = AttackState.Windup;
                        _attackTimer = 0.15f;
                    }
                    break;
                case AttackState.Windup:
                    _attackTimer -= dt;
                    if (_attackTimer <= 0f)
                    {
                        AttackState = AttackState.Lunge;
                        _attackTimer = 0.12f;
                        gm.DamagePlayer(Damage);
                    }
                    break;
                case AttackState.Lunge:
                    _attackTimer -= dt;
                    transform.position += dir * Speed * 3f * dt;
                    if (_attackTimer <= 0f)
                    {
                        AttackState = AttackState.None;
                        _attackCooldown = 0.8f;
                    }
                    break;
            }

            if (_animator != null)
                _animator.SetFloat("Speed", _rb.velocity.magnitude);
            if (_placeholderRenderers != null && simTime < HitFlashUntil)
            {
                foreach (var r in _placeholderRenderers)
                    r.material.color = Color.Lerp(r.material.color, Color.white, 0.5f);
            }
        }

        private void MoveZombie(Vector3 dir, float dt)
        {
            if (_isCharging || _isTelegraphing) return;
            float speed = Speed * SlowFactor;
            _rb.velocity = new Vector3(dir.x * speed, _rb.velocity.y, dir.z * speed);
            WalkPhase += dt * speed * PlayerController.WalkCycleRate;
            // Clamp to the arena.
            var map = GameManager.Instance.LevelDirector.CurrentMap;
            if (map != null)
            {
                Vector3 p = transform.position;
                p.x = Mathf.Clamp(p.x, -map.ArenaWidth / 2f + Radius, map.ArenaWidth / 2f - Radius);
                p.z = Mathf.Clamp(p.z, -map.ArenaHeight / 2f + Radius, map.ArenaHeight / 2f - Radius);
                transform.position = p;
            }
        }

        private void UpdateBossSpecials(float dt, Vector3 dir, float dist)
        {
            var gm = GameManager.Instance;
            if (!BossKind.HasValue) return;
            switch (BossKind.Value)
            {
                case BossKind.Charger:
                    if (_isTelegraphing)
                    {
                        _chargeTimer -= dt;
                        if (_chargeTimer <= 0f)
                        {
                            _isTelegraphing = false;
                            _isCharging = true;
                            _chargeTimer = 1f;
                            _chargeVel = dir * Speed * 4f;
                        }
                    }
                    else if (_isCharging)
                    {
                        _chargeTimer -= dt;
                        transform.position += _chargeVel * dt;
                        WalkPhase += dt * Speed * 4f * PlayerController.WalkCycleRate;
                        if (_chargeTimer <= 0f)
                        {
                            _isCharging = false;
                            _chargeCooldown = 5f;
                        }
                    }
                    else if (_chargeCooldown <= 0f && dist > 4f)
                    {
                        _isTelegraphing = true;
                        _chargeTimer = 0.8f;
                    }
                    break;
                case BossKind.Summoner:
                    if (_summonCooldown <= 0f)
                    {
                        _summonCooldown = 10f;
                        int n = 3 + gm.Rng.Next(3);
                        for (int i = 0; i < n; i++) gm.SummonNear(transform.position, null);
                    }
                    break;
                case BossKind.SpitterKing:
                    if (_spitCooldown <= 0f && dist < 10f)
                    {
                        _spitCooldown = 4f;
                        for (int i = 0; i < 3; i++)
                        {
                            float a = FacingAngle + (i - 1) * 0.25f;
                            gm.SpawnEnemyBullet(transform.position + Vector3.up, a, 5f, Damage, 3f);
                        }
                    }
                    break;
                case BossKind.TheButcher:
                    // Massive cleaver arc: everything within reach takes a heavy hit.
                    if (_attackCooldown <= 0f && dist < 2.6f)
                    {
                        _attackCooldown = 2.5f;
                        gm.DamagePlayer(Damage);
                        gm.Juice.AddShake(0.6f);
                    }
                    break;
                case BossKind.PlagueQueen:
                    if (_summonCooldown <= 0f)
                    {
                        _summonCooldown = 12f;
                        int n = 4 + gm.Rng.Next(3);
                        for (int i = 0; i < n; i++)
                            gm.SummonNear(transform.position, ZombieKind.Crawler);
                    }
                    if (_spitCooldown <= 0f && dist < 12f)
                    {
                        _spitCooldown = 5f;
                        for (int i = 0; i < 5; i++)
                        {
                            float a = FacingAngle + (i - 2) * 0.35f;
                            gm.SpawnEnemyBullet(transform.position + Vector3.up, a, 3f, Damage, 4f);
                        }
                    }
                    break;
            }
        }
    }
}
