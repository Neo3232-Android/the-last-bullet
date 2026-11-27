using UnityEngine;
using TheLastBullet.Audio;
using TheLastBullet.Core;
using TheLastBullet.Data;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// Drone companion (meta-shop purchase, upgraded to Lv5). A small glowing
    /// orb that orbits the player and auto-fires at the nearest zombie in
    /// range. Damage / fire rate / range scale with DroneLevel. Created by
    /// GameManager.SpawnPlayer when the profile owns a drone; a child of the
    /// player GameObject, so it is cleaned up with the player. Never throws:
    /// every step is null-guarded so a missing target can never break a run.
    /// </summary>
    public sealed class DroneCompanion : MonoBehaviour
    {
        private float _fireTimer;
        private float _orbitPhase;
        private GameObject _orb;

        public int Level { get; private set; } = 1;

        public void Setup(int level)
        {
            Level = Mathf.Clamp(level, 1, 5);
            // Visual: small gold orb hovering beside the player.
            _orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _orb.name = "DroneOrb";
            _orb.transform.SetParent(transform, false);
            _orb.transform.localScale = Vector3.one * 0.35f;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null)
            {
                var mat = new Material(lit);
                mat.color = new Color(1f, 0.84f, 0f);
                var renderer = _orb.GetComponent<Renderer>();
                if (renderer != null) renderer.material = mat;
            }
            var col = _orb.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            _fireTimer = 0.5f;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null || !gm.Player.IsAlive) return;
            if (gm.State.Phase != GamePhase.Playing) return;

            // Orbit the player (local, since this is a child of the player).
            _orbitPhase += Time.deltaTime * 2.2f;
            transform.localPosition = new Vector3(
                Mathf.Cos(_orbitPhase) * 1.2f,
                1.5f + Mathf.Sin(_orbitPhase * 2f) * 0.15f,
                Mathf.Sin(_orbitPhase) * 1.2f);

            _fireTimer -= Time.deltaTime;
            if (_fireTimer > 0f) return;
            _fireTimer = FireInterval();

            // Nearest alive zombie in range.
            ZombieAI nearest = null;
            float bestDist = Range();
            foreach (var z in gm.Zombies)
            {
                if (z == null || !z.IsAlive) continue;
                float d = Vector3.Distance(transform.position, z.transform.position);
                if (d < bestDist) { bestDist = d; nearest = z; }
            }
            if (nearest == null) return;

            Vector3 to = nearest.transform.position - transform.position;
            float angle = Mathf.Atan2(to.z, to.x);
            gm.SpawnPlayerBullet(transform.position, angle, 18f, Damage(), 2f);
            SynthAudio.Instance.Shoot(WeaponId.Pistol);
        }

        private float Damage() => 15f + 10f * Level;
        private float FireInterval() => Mathf.Max(0.4f, 1.0f - 0.1f * Level);
        private float Range() => 10f + Level;
    }
}
