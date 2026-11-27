using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Builds the arena for a level: ground plane, invisible boundary walls,
    /// and zone markers. Stage 1 uses procedural placeholders (dark ground +
    /// glowing zone rings) so the game is playable immediately.
    ///
    /// Stage 2 (models): PopulateZone() instantiates the converted FBX
    /// environment sets per zone from Assets/Models (see
    /// Docs/MODEL_PIPELINE.md) — one set per zone, chosen by the zone's
    /// ModelSet key, with per-map selection so the APK never ships all 295 MB.
    /// </summary>
    public static class MapBuilder
    {
        private static GameObject _arenaRoot;
        private static readonly List<GameObject> _zoneMarkers = new List<GameObject>();

        public static void BuildArena(MapDef map, int zonesOpen)
        {
            ClearArena();
            _arenaRoot = new GameObject("Arena_" + map.Name);

            // Ground: big dark plane sized to the arena.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(_arenaRoot.transform, false);
            ground.transform.localScale = new Vector3(map.ArenaWidth / 10f, 1f, map.ArenaHeight / 10f);
            ground.layer = LayerMask.NameToLayer("Ground");
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.09f, 0.09f, 0.11f);
            groundMat.SetFloat("_Smoothness", 0.1f);
            ground.GetComponent<Renderer>().material = groundMat;

            // Boundary walls (invisible colliders — the player/zombies clamp to
            // the arena in code too, these stop physics bodies from escaping).
            float hw = map.ArenaWidth / 2f;
            float hh = map.ArenaHeight / 2f;
            AddWall(new Vector3(0f, 1f, -hh - 0.5f), new Vector3(map.ArenaWidth + 2f, 2f, 1f));
            AddWall(new Vector3(0f, 1f, hh + 0.5f), new Vector3(map.ArenaWidth + 2f, 2f, 1f));
            AddWall(new Vector3(-hw - 0.5f, 1f, 0f), new Vector3(1f, 2f, map.ArenaHeight + 2f));
            AddWall(new Vector3(hw + 0.5f, 1f, 0f), new Vector3(1f, 2f, map.ArenaHeight + 2f));

            // Zone markers: glowing rings on the ground for open zones.
            // Stage 2 replaces these with the real environment sets.
            for (int i = 0; i < map.Zones.Count; i++)
            {
                var zone = map.Zones[i];
                bool open = i < zonesOpen;
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "Zone_" + zone.Id + (open ? "" : "_Locked");
                marker.transform.SetParent(_arenaRoot.transform, false);
                marker.transform.position = new Vector3(zone.Center.x, 0.02f, zone.Center.y);
                marker.transform.localScale = new Vector3(zone.Radius * 2f, 0.02f, zone.Radius * 2f);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = open
                    ? new Color(1f, 0.84f, 0f, 0.25f)
                    : new Color(0.3f, 0.3f, 0.35f, 0.15f);
                // Transparent ring: use the URP Lit material in transparent mode.
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_AlphaClip", 0f);
                mat.SetFloat("_SrcBlend", 1f);
                mat.SetFloat("_DstBlend", 10f);
                mat.SetFloat("_ZWrite", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                marker.GetComponent<Renderer>().material = mat;
                var col = marker.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                _zoneMarkers.Add(marker);
                if (open) PopulateZone(zone);
            }
        }

        /// <summary>
        /// Stage 2 hook: instantiate a zone's environment set here.
        /// Looks up Assets/Models/&lt;ModelSet&gt;/ prefabs by zone.ModelSet.
        /// Until the FBX conversion lands, zones stay as glowing rings.
        /// </summary>
        public static void PopulateZone(MapZoneDef zone)
        {
            // TODO (Stage 2): load the converted FBX prefabs for zone.ModelSet
            // from Resources/Models or Addressables and scatter them inside
            // the zone radius (buildings, props, hazards). See Docs/MODEL_PIPELINE.md.
        }

        private static void AddWall(Vector3 pos, Vector3 size)
        {
            var wall = new GameObject("BoundaryWall");
            wall.transform.SetParent(_arenaRoot.transform, false);
            wall.transform.position = pos;
            var box = wall.AddComponent<BoxCollider>();
            box.size = size;
            wall.layer = LayerMask.NameToLayer("Ground");
        }

        public static void ClearArena()
        {
            if (_arenaRoot != null) Object.Destroy(_arenaRoot);
            _arenaRoot = null;
            _zoneMarkers.Clear();
        }
    }
}
