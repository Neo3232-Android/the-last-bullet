using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Gameplay
{
    /// <summary>
    /// Buildables bought with coins mid-run. Plain C# classes (not
    /// MonoBehaviours) — GameManager updates turrets/traps; each owns a
    /// small procedural view GameObject. Stage 2 swaps the views for the
    /// real prop models (sandbag barricade, guard-tower turret, traps).
    /// </summary>
    public sealed class BarricadeEntity
    {
        public Vector3 Position { get; }
        public float Width { get; } = 2f;
        public float Depth { get; } = 1f;
        public float Health { get; set; }
        public float MaxHealth { get; }
        public int Tier { get; }
        private GameObject _view;

        public BarricadeEntity(Vector3 pos, float health, int tier)
        {
            Position = pos;
            Health = health;
            MaxHealth = health;
            Tier = tier;
            _view = new GameObject("Barricade_T" + tier);
            _view.layer = LayerMask.NameToLayer("Barricade");
            _view.transform.position = pos;
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Planks";
            box.transform.SetParent(_view.transform, false);
            box.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            box.transform.localScale = new Vector3(Width, 1f, Depth);
            box.GetComponent<Renderer>().material.color =
                tier >= 2 ? new Color(0.5f, 0.55f, 0.6f) : new Color(0.55f, 0.38f, 0.2f);
            var col = box.AddComponent<BoxCollider>();
            col.size = new Vector3(Width, 1f, Depth);
        }

        public void DestroyView()
        {
            if (_view != null) Object.Destroy(_view);
            _view = null;
        }
    }

    public sealed class TurretEntity
    {
        public Vector3 Position { get; }
        public float Radius { get; } = 0.5f;
        public float Range { get; } = 6f;
        public float FireCooldown { get; set; }
        public float Damage { get; } = 20f;
        public float FireRate { get; } = 2f;
        private GameObject _view;

        public TurretEntity(Vector3 pos)
        {
            Position = pos;
            _view = new GameObject("Turret");
            _view.layer = LayerMask.NameToLayer("Barricade");
            _view.transform.position = pos;
            var base_ = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            base_.name = "Base";
            base_.transform.SetParent(_view.transform, false);
            base_.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            base_.transform.localScale = new Vector3(1f, 0.8f, 1f);
            base_.GetComponent<Renderer>().material.color = new Color(0.3f, 0.32f, 0.35f);
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            barrel.transform.SetParent(_view.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 1f, 0.3f);
            barrel.transform.localScale = new Vector3(0.15f, 0.15f, 0.9f);
            barrel.GetComponent<Renderer>().material.color = Color.black;
        }

        public void DestroyView()
        {
            if (_view != null) Object.Destroy(_view);
            _view = null;
        }
    }

    public sealed class TrapEntity
    {
        public TrapKind Kind { get; }
        public Vector3 Position { get; }
        public float Radius { get; } = 0.8f;
        public float Damage { get; }
        public int UsesLeft { get; set; }
        public float Cooldown { get; set; }
        private GameObject _view;

        public TrapEntity(TrapKind kind, Vector3 pos, float damage, int uses)
        {
            Kind = kind;
            Position = pos;
            Damage = damage;
            UsesLeft = uses;
            _view = new GameObject("Trap_" + kind);
            _view.layer = LayerMask.NameToLayer("Ground");
            _view.transform.position = pos;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Disc";
            disc.transform.SetParent(_view.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            disc.transform.localScale = new Vector3(Radius * 2f, 0.1f, Radius * 2f);
            disc.GetComponent<Renderer>().material.color = kind == TrapKind.Spike
                ? new Color(0.7f, 0.7f, 0.75f)
                : kind == TrapKind.Mine
                    ? new Color(0.2f, 0.2f, 0.2f)
                    : new Color(0.2f, 0.6f, 1f);
        }

        public void DestroyView()
        {
            if (_view != null) Object.Destroy(_view);
            _view = null;
        }
    }
}
