using UnityEngine;

namespace TheLastBullet.Ragdoll
{
    /// <summary>
    /// Runtime ragdoll switch. The real authoring happens in the Unity Editor
    /// with the Ragdoll Wizard (GameObject &gt; 3D Object &gt; Ragdoll...), which
    /// adds Rigidbodies + CharacterJoints to every Mixamo bone and stores them
    /// under the model root. This component — attached to the model root —
    /// finds those parts once, keeps them kinematic + the Animator in charge
    /// while the character is alive, and on EnableRagdoll hands physics over:
    /// Animator off, bone Rigidbodies live, impulse applied at the hit point.
    ///
    /// Works with the placeholder figures too (they have no bone Rigidbodies,
    /// so EnableRagdoll is a no-op and the caller falls back to the tip-over).
    /// </summary>
    public sealed class RagdollController : MonoBehaviour
    {
        private Animator _animator;
        private Rigidbody[] _boneBodies;
        private CharacterJoint[] _joints;
        private Collider[] _boneColliders;
        private bool _isRagdoll;

        public bool IsRagdoll => _isRagdoll;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _boneBodies = GetComponentsInChildren<Rigidbody>();
            _joints = GetComponentsInChildren<CharacterJoint>();
            _boneColliders = GetComponentsInChildren<Collider>();
            SetBonesKinematic(true);
        }

        private void SetBonesKinematic(bool kinematic)
        {
            if (_boneBodies == null) return;
            foreach (var rb in _boneBodies)
            {
                if (rb == null) continue;
                rb.isKinematic = kinematic;
                rb.detectCollisions = !kinematic;
            }
        }

        /// <summary>
        /// Fling the body into a full PhysX ragdoll. impulse is the direction
        /// of the killing blow (already normalised); strength scales with it.
        /// </summary>
        public void EnableRagdoll(Vector3 impulse)
        {
            if (_isRagdoll) return;
            if (_boneBodies == null || _boneBodies.Length == 0) return;
            _isRagdoll = true;
            if (_animator != null) _animator.enabled = false;
            SetBonesKinematic(false);
            foreach (var rb in _boneBodies)
            {
                if (rb == null) continue;
                // Heavier at the hips, lighter at the extremities — reads as a
                // body collapsing, not a balloon popping.
                float weight = rb.mass;
                rb.AddForce(impulse * 6f * weight, ForceMode.Impulse);
                rb.AddForce(Vector3.up * 2.5f * weight, ForceMode.Impulse);
                rb.AddTorque(new Vector3(
                    Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f))
                    * 2f * weight, ForceMode.Impulse);
            }
        }

        /// <summary>Back to animated control (respawn / pool reuse).</summary>
        public void DisableRagdoll()
        {
            if (!_isRagdoll) return;
            _isRagdoll = false;
            SetBonesKinematic(true);
            if (_animator != null) _animator.enabled = true;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Editor-time helper: called by BuildScript/SetupURP docs. Validates
        /// that the model root actually has ragdoll bones before shipping.
        /// </summary>
        public bool HasRagdollBones() => _boneBodies != null && _boneBodies.Length >= 8;
    }
}
