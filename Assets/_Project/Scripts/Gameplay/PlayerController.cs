using UnityEngine;
using TheLastBullet.Core;
using TheLastBullet.Data;
using TheLastBullet.Ragdoll;
using TheLastBullet.UI;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// The player. Twin-stick: left thumb moves, right thumb aims and fires.
    /// Manual aim only — nothing auto-fires. Port of the Kotlin PlayerEntity
    /// + updatePlayer() rules (dodge roll, reload, minigun spin-up, walk cycle).
    ///
    /// Stage 1 drives a placeholder capsule figure (outfit color applied from
    /// the character creator). Stage 2 swaps it for the real FBX model via
    /// SetModel() — same Animator/RagdollController hooks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        public const float WalkCycleRate = 5.2f;

        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public WeaponId WeaponId { get; set; }
        public WeaponId? SecondaryWeaponId { get; set; }
        public int AmmoInMag { get; set; }
        public float ReloadingUntil { get; set; }
        public float ReloadStartedAt { get; set; }
        public float ReloadDuration { get; set; }
        public float FireTimer { get; set; }
        public float SpinUp { get; set; }
        public float AimAngle { get; private set; }
        public float FacingAngle { get; private set; }
        public float WalkPhase { get; private set; }
        public float DodgeRollTimer { get; set; }
        public Vector3 DodgeRollDir { get; private set; }
        public float WeaponSwapTimer { get; set; }
        public float HitFlashUntil { get; set; }
        public DeathState DeathState { get; private set; } = DeathState.None;
        public float Radius { get; private set; } = 0.45f;

        public bool IsAlive => DeathState == DeathState.None;
        public bool IsReloading => ReloadingUntil > 0f;
        public bool IsLowHealth => Health <= MaxHealth / 4;

        /// <summary>Flashlight (dark maps), set by GameManager.SpawnPlayer.</summary>
        public Flashlight Flashlight { get; set; }

        private Rigidbody _rb;
        private CapsuleCollider _collider;
        private Animator _animator;
        private ModelAnimator _modelAnimator;
        private RagdollController _ragdoll;
        private WeaponSystem _weapons;
        private PlayerState _state;
        private GameObject _placeholderModel;
        private Renderer[] _placeholderRenderers;
        private Vector2 _moveInput;
        private Vector2 _aimInput;
        private bool _firing;

        public void Setup(PlayerState state, CharacterDef character)
        {
            _state = state;
            Health = state.Health;
            MaxHealth = state.MaxHealth;
            WeaponId = state.WeaponId;
            SecondaryWeaponId = state.SecondaryWeaponId;
            AmmoInMag = GameManager.Instance.EffectiveMagSize(WeaponId);

            _rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _collider = GetComponent<CapsuleCollider>() ?? gameObject.AddComponent<CapsuleCollider>();
            _collider.radius = Radius;
            _collider.height = 1.8f;
            _collider.center = new Vector3(0f, 0.9f, 0f);

            _weapons = gameObject.AddComponent<WeaponSystem>();
            _animator = GetComponentInChildren<Animator>();
            _ragdoll = GetComponentInChildren<RagdollController>();
            BuildPlaceholderModel(character);

            // Stage 2: swap the placeholder for the real mannequin-man FBX
            // when it is bundled; otherwise keep the placeholder.
            var realModel = ModelLibrary.InstantiatePlayerModel();
            if (realModel != null)
            {
                SetModel(realModel);
                // Creator's outfit colour tints the figure (single-material
                // mannequin, so the whole figure takes the outfit colour).
                if (ColorUtility.TryParseHtmlString(character.OutfitColor, out Color outfit))
                    ModelLibrary.ApplyOutfitTint(realModel, outfit);
            }
        }

        /// <summary>Stage 2: replace the placeholder with the real FBX model.</summary>
        public void SetModel(GameObject modelRoot)
        {
            if (_placeholderModel != null) Destroy(_placeholderModel);
            modelRoot.transform.SetParent(transform, false);
            modelRoot.transform.localPosition = Vector3.zero;
            _animator = modelRoot.GetComponentInChildren<Animator>();
            _modelAnimator = modelRoot.GetComponentInChildren<ModelAnimator>();
            _ragdoll = modelRoot.GetComponentInChildren<RagdollController>();
        }

        private void BuildPlaceholderModel(CharacterDef character)
        {
            _placeholderModel = new GameObject("PlaceholderModel");
            _placeholderModel.transform.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(_placeholderModel.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
            if (ColorUtility.TryParseHtmlString(character.OutfitColor, out Color outfit))
                body.GetComponent<Renderer>().material.color = outfit;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(_placeholderModel.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;
            if (ColorUtility.TryParseHtmlString(character.SkinTone, out Color skin))
                head.GetComponent<Renderer>().material.color = skin;

            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "Gun";
            gun.transform.SetParent(_placeholderModel.transform, false);
            gun.transform.localPosition = new Vector3(0.3f, 1.1f, 0.4f);
            gun.transform.localScale = new Vector3(0.12f, 0.12f, 0.7f);
            gun.GetComponent<Renderer>().material.color = Color.black;

            _placeholderRenderers = _placeholderModel.GetComponentsInChildren<Renderer>();
            // Placeholder colliders would double-hit; the root CapsuleCollider is the only one.
            foreach (var c in _placeholderModel.GetComponentsInChildren<Collider>())
                Destroy(c);
        }

        public void SetMoveInput(Vector2 move) => _moveInput = move;
        public void SetAimInput(Vector2 aim, bool firing)
        {
            _aimInput = aim;
            _firing = firing;
        }

        public void Heal(int amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        public void DodgeRoll()
        {
            if (!IsAlive || DodgeRollTimer > 0f) return;
            Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);
            DodgeRollDir = move.sqrMagnitude > 0.001f
                ? move.normalized
                : GameManager.AngleToDir(FacingAngle);
            DodgeRollTimer = 0.35f;
            Audio.SynthAudio.Instance.DodgeRoll();
        }

        public void StartReload()
        {
            var def = WeaponData.Get(WeaponId);
            if (def.MagSize <= 0 || IsReloading) return;
            ReloadStartedAt = GameManager.Instance.SimTime;
            ReloadDuration = GameManager.Instance.EffectiveReloadSec(def);
            ReloadingUntil = ReloadStartedAt + ReloadDuration;
            Audio.SynthAudio.Instance.Reload();
        }

        public void SwapWeapon() => GameManager.Instance.SwapWeapon();

        public void Die()
        {
            if (DeathState != DeathState.None) return;
            DeathState = DeathState.Falling;
            _rb.velocity = Vector3.zero;
            _rb.isKinematic = true;
            if (_modelAnimator != null) _modelAnimator.PlayDeath();
            if (_ragdoll != null) _ragdoll.EnableRagdoll(Vector3.back);
            else if (_placeholderModel != null)
                _placeholderModel.transform.Rotate(-90f, 0f, 0f); // fall over
        }

        private void FixedUpdate()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State.Phase != GamePhase.Playing || !IsAlive) return;
            float dt = Time.fixedDeltaTime;

            DodgeRollTimer = Mathf.Max(0f, DodgeRollTimer - dt);
            WeaponSwapTimer = Mathf.Max(0f, WeaponSwapTimer - dt);

            // Input: touch sticks (null-safe — no UI in headless/CI runs).
            var sticks = TwinStickInput.Instance;
            if (sticks != null)
            {
                _moveInput = sticks.MoveInput;
                _aimInput = sticks.AimInput;
                _firing = sticks.Firing;
            }

            // Movement + walk cycle. Faces movement; gun arm uses aim angle.
            float speed = _state.MoveSpeed * _state.MoveSpeedMultiplier();
            Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (move.sqrMagnitude > 1f) move.Normalize();
            Vector3 vel = DodgeRollTimer > 0f
                ? DodgeRollDir * speed * 2.2f
                : move * speed;
            _rb.velocity = new Vector3(vel.x, _rb.velocity.y, vel.z);

            // Clamp to the arena.
            var map = gm.LevelDirector.CurrentMap;
            if (map != null)
            {
                Vector3 p = transform.position;
                p.x = Mathf.Clamp(p.x, -map.ArenaWidth / 2f + Radius, map.ArenaWidth / 2f - Radius);
                p.z = Mathf.Clamp(p.z, -map.ArenaHeight / 2f + Radius, map.ArenaHeight / 2f - Radius);
                transform.position = p;
            }

            if (move.sqrMagnitude > 0.001f)
            {
                if (DodgeRollTimer <= 0f)
                    FacingAngle = Mathf.Atan2(move.z, move.x);
                // Footstep on each step: WalkPhase crosses a multiple of PI.
                float prevPhase = WalkPhase;
                WalkPhase += dt * move.magnitude * speed * WalkCycleRate;
                if (Mathf.Floor(WalkPhase / Mathf.PI) != Mathf.Floor(prevPhase / Mathf.PI))
                    Audio.SynthAudio.Instance.Footstep(false);
                transform.rotation = Quaternion.Euler(0f, GameManager.AngleToYaw(FacingAngle), 0f);
            }

            // Aim.
            if (_aimInput.sqrMagnitude > 0.04f)
                AimAngle = Mathf.Atan2(_aimInput.y, _aimInput.x);
            // Flashlight follows the aim stick.
            if (Flashlight != null) Flashlight.SetAim(AimAngle, FacingAngle);

            // Reload completion.
            if (IsReloading && gm.SimTime >= ReloadingUntil)
            {
                AmmoInMag = gm.EffectiveMagSize(WeaponId);
                ReloadingUntil = 0f;
            }

            // Firing.
            FireTimer -= dt;
            var def = WeaponData.Get(WeaponId);
            if (def.Id == WeaponId.Minigun)
            {
                if (_firing && _aimInput.sqrMagnitude > 0.04f)
                    SpinUp = Mathf.Min(0.5f, SpinUp + dt);
                else
                    SpinUp = 0f;
            }
            if (_firing && _aimInput.sqrMagnitude > 0.04f && FireTimer <= 0f &&
                !IsReloading && (def.MagSize == 0 || AmmoInMag > 0))
            {
                _weapons.Fire(def, AimAngle);
            }
            if (AmmoInMag <= 0 && def.MagSize > 0 && !IsReloading)
                StartReload();

            // Animator params (guarded — null until the real model lands).
            if (_animator != null)
            {
                _animator.SetFloat("Speed", move.magnitude * speed);
                _animator.SetBool("IsFiring", _firing);
            }
            if (_modelAnimator != null)
                _modelAnimator.SetSpeed(move.magnitude * speed);

            // Hit flash on the placeholder.
            if (_placeholderRenderers != null && gm.SimTime < HitFlashUntil)
            {
                foreach (var r in _placeholderRenderers)
                    r.material.color = Color.Lerp(r.material.color, Color.white, 0.5f);
            }
        }
    }
}
