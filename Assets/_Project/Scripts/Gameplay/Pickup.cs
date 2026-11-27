using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// A pickup on the ground. Magnet-pulls toward the player and is collected
    /// on touch (GameManager.ApplyPickup). Visual: a small glowing sphere,
    /// color-coded by kind, bobbing and spinning.
    /// </summary>
    public sealed class Pickup : MonoBehaviour
    {
        public PickupKind Kind { get; private set; }
        public float Radius { get; private set; } = 0.4f;

        private float _bobPhase;

        public void Setup(PickupKind kind)
        {
            Kind = kind;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.7f;
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            visual.GetComponent<Renderer>().material.color = ColorFor(kind);
            Destroy(visual.GetComponent<Collider>());
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private static Color ColorFor(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Health: return new Color(0.15f, 0.85f, 0.35f);
                case PickupKind.Ammo: return new Color(1f, 0.84f, 0f);
                case PickupKind.Grenade: return new Color(0.2f, 0.2f, 0.2f);
                case PickupKind.Molotov: return new Color(1f, 0.45f, 0.1f);
                case PickupKind.FreezeBomb: return new Color(0.4f, 0.75f, 1f);
                case PickupKind.InstaKill: return new Color(1f, 0.2f, 0.6f);
                default: return Color.white;
            }
        }

        private void Update()
        {
            _bobPhase += Time.deltaTime * 3f;
            transform.position += Vector3.up * Mathf.Sin(_bobPhase) * 0.15f * Time.deltaTime;
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);
        }
    }
}
