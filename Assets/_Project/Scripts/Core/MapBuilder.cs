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
    /// Build 2: PopulateZone() scatters procedural primitive props per zone
    /// (buildings, trees, barrels, gravestones, crates, dunes, snow mounds)
    /// chosen by the zone's ModelSet key. The full glTFast environment sets
    /// from the 295 MB library are NOT bundled — see Docs/MODEL_PIPELINE.md.
    /// </summary>
    public static class MapBuilder
    {
        private static GameObject _arenaRoot;
        private static readonly List<GameObject> _zoneMarkers = new List<GameObject>();

        /// <summary>Living-environment driver for the current arena (sway, fog,
        /// flicker, embers, weather). Null until BuildArena runs; GameManager
        /// and MapEvents reach it here so weather events can call SetWeather.</summary>
        public static LivingEnvironment LivingEnvironment { get; private set; }

        public static void BuildArena(MapDef map, int zonesOpen)
        {
            ClearArena();
            _arenaRoot = new GameObject("Arena_" + map.Name);

            // Living environment: sway/fog/flicker/embers/weather for this arena.
            var envGo = new GameObject("LivingEnvironment");
            envGo.transform.SetParent(_arenaRoot.transform, false);
            LivingEnvironment = envGo.AddComponent<LivingEnvironment>();

            // Ground: big dark plane sized to the arena.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(_arenaRoot.transform, false);
            ground.transform.localScale = new Vector3(map.ArenaWidth / 10f, 1f, map.ArenaHeight / 10f);
            ground.layer = LayerMask.NameToLayer("Ground");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit != null)
            {
                var groundMat = new Material(lit);
                groundMat.color = new Color(0.09f, 0.09f, 0.11f);
                groundMat.SetFloat("_Smoothness", 0.1f);
                ground.GetComponent<Renderer>().material = groundMat;
            }
            // else: leave the primitive's default material so a missing
            // shader can never throw here and break the run.

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
                var ringLit = Shader.Find("Universal Render Pipeline/Lit");
                if (ringLit != null)
                {
                    var mat = new Material(ringLit);
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
                }
                var col = marker.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                _zoneMarkers.Add(marker);
                if (open) PopulateZone(zone);
            }
        }

        /// <summary>
        /// Build 2: procedural environment props per zone. The full glTFast
        /// environment sets are NOT bundled (no verified glTFast API on this
        /// machine, and the 295 MB library must stay out of the APK), so
        /// each open zone gets a small deterministic set of primitives
        /// (seeded by zone id, so a zone always looks the same) that fit
        /// its ModelSet theme. Max ~12 props per zone, on purpose — the
        /// arena stays readable and the draw-call budget stays low.
        ///
        /// Props never block movement (their primitive colliders are
        /// destroyed) except large buildings, which keep a BoxCollider so
        /// the player/zombies path around them; movement is clamped to the
        /// arena in code anyway. The zone-ring markers from BuildArena
        /// are untouched.
        /// </summary>
        public static void PopulateZone(MapZoneDef zone)
        {
            if (_arenaRoot == null || zone == null) return;
            var rng = new System.Random(StableSeed(zone.Id));
            switch (zone.ModelSet)
            {
                case "city":
                case "mall":
                case "prison":
                case "school":
                case "casino":
                case "subway":
                case "hospital":
                case "lab":
                case "castle":
                case "stadium":
                case "rooftop":
                    ScatterBuildings(zone, rng);
                    break;
                case "forest":
                case "farm":
                case "beach":
                    ScatterTrees(zone, rng, zone.ModelSet == "beach");
                    break;
                case "junkyard":
                case "sewer":
                    ScatterBarrels(zone, rng);
                    break;
                case "graveyard":
                    ScatterGravestones(zone, rng);
                    break;
                case "airport":
                case "dungeon":
                    ScatterCrates(zone, rng);
                    break;
                case "desert":
                    ScatterDunes(zone, rng);
                    break;
                case "snow":
                    ScatterSnowMounds(zone, rng);
                    break;
                default:
                    ScatterCrates(zone, rng);
                    break;
            }
        }

        // --- Procedural prop scatter (build 2) ---

        // Shared URP Lit materials, one per colour — a handful of colours
        // per zone, so the prop budget stays at a few materials total.
        private static readonly Dictionary<Color, Material> _propMaterials =
            new Dictionary<Color, Material>();

        private static Material PropMaterial(Color color)
        {
            Material mat;
            if (_propMaterials.TryGetValue(color, out mat) && mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            mat = new Material(shader);
            mat.color = color;
            _propMaterials[color] = mat;
            return mat;
        }

        /// <summary>
        /// One primitive prop under the zone's prop root. solid = true keeps
        /// the primitive's collider (large buildings only); otherwise the
        /// collider is destroyed so the prop can never block movement.
        /// </summary>
        private static GameObject MakeProp(GameObject parent, string name, PrimitiveType type,
            Vector3 position, Vector3 scale, Color color, bool solid)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var mat = PropMaterial(color);
            if (mat != null)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.material = mat;
            }
            var col = go.GetComponent<Collider>();
            if (col != null && !solid) Object.Destroy(col);
            go.isStatic = true;
            return go;
        }

        private static GameObject PropRoot(MapZoneDef zone)
        {
            var root = new GameObject("Props_" + zone.Id);
            root.transform.SetParent(_arenaRoot.transform, false);
            return root;
        }

        /// <summary>Deterministic seed from the zone id (string.GetHashCode is not stable across runtimes).</summary>
        private static int StableSeed(string id)
        {
            unchecked
            {
                int hash = 5381;
                if (string.IsNullOrEmpty(id)) return hash;
                foreach (char c in id) hash = ((hash << 5) + hash) + c;
                return hash;
            }
        }

        /// <summary>Random point inside the zone, kept off the exact centre so spawns stay clear.</summary>
        private static Vector3 ZonePoint(MapZoneDef zone, System.Random rng, float minRadius, float maxRadius)
        {
            float angle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            float radius = minRadius + (float)rng.NextDouble() * (maxRadius - minRadius);
            return new Vector3(
                zone.Center.x + Mathf.Cos(angle) * radius,
                0f,
                zone.Center.y + Mathf.Sin(angle) * radius);
        }

        private static void ScatterBuildings(MapZoneDef zone, System.Random rng)
        {
            var root = PropRoot(zone);
            var palette = BuildingPalette(zone.ModelSet);
            int count = 5 + rng.Next(3); // 5-7 buildings
            for (int i = 0; i < count; i++)
            {
                Vector3 p = ZonePoint(zone, rng, 3f, zone.Radius * 0.8f);
                float w = 2f + (float)rng.NextDouble() * 2.5f;
                float d = 2f + (float)rng.NextDouble() * 2.5f;
                // Rooftop props are low AC units/vents, not towers.
                float h = zone.ModelSet == "rooftop"
                    ? 0.8f + (float)rng.NextDouble() * 0.8f
                    : 3f + (float)rng.NextDouble() * 6f;
                var b = MakeProp(root, "Building_" + i, PrimitiveType.Cube,
                    new Vector3(p.x, h / 2f, p.z), new Vector3(w, h, d),
                    palette[rng.Next(palette.Length)], solid: true);
                b.transform.rotation = Quaternion.Euler(0f, rng.Next(4) * 90f, 0f);
            }
        }

        private static Color[] BuildingPalette(string modelSet)
        {
            switch (modelSet)
            {
                case "mall": return new[]
                {
                    new Color(0.70f, 0.55f, 0.40f), new Color(0.60f, 0.50f, 0.55f),
                    new Color(0.75f, 0.65f, 0.45f),
                };
                case "prison": return new[]
                {
                    new Color(0.45f, 0.45f, 0.47f), new Color(0.35f, 0.35f, 0.38f),
                };
                case "school": return new[]
                {
                    new Color(0.75f, 0.70f, 0.55f), new Color(0.55f, 0.40f, 0.32f),
                };
                case "casino": return new[]
                {
                    new Color(0.25f, 0.10f, 0.30f), new Color(0.50f, 0.15f, 0.35f),
                    new Color(0.70f, 0.55f, 0.15f),
                };
                case "subway": return new[]
                {
                    new Color(0.22f, 0.22f, 0.24f), new Color(0.30f, 0.28f, 0.25f),
                };
                case "hospital": return new[]
                {
                    new Color(0.85f, 0.88f, 0.85f), new Color(0.45f, 0.70f, 0.70f),
                };
                case "lab": return new[]
                {
                    new Color(0.85f, 0.88f, 0.85f), new Color(0.50f, 0.75f, 0.50f),
                };
                case "castle": return new[]
                {
                    new Color(0.40f, 0.40f, 0.42f), new Color(0.30f, 0.30f, 0.33f),
                };
                case "stadium": return new[]
                {
                    new Color(0.50f, 0.50f, 0.52f), new Color(0.25f, 0.45f, 0.25f),
                };
                case "rooftop": return new[]
                {
                    new Color(0.15f, 0.15f, 0.17f), new Color(0.45f, 0.47f, 0.50f),
                };
                default: // city
                    return new[]
                    {
                        new Color(0.25f, 0.25f, 0.28f), new Color(0.45f, 0.32f, 0.28f),
                        new Color(0.35f, 0.35f, 0.38f), new Color(0.20f, 0.22f, 0.30f),
                    };
            }
        }

        private static void ScatterTrees(MapZoneDef zone, System.Random rng, bool palms)
        {
            var root = PropRoot(zone);
            var trunkColor = new Color(0.35f, 0.25f, 0.15f);
            var canopyColors = palms
                ? new[] { new Color(0.20f, 0.50f, 0.20f), new Color(0.15f, 0.45f, 0.25f) }
                : new[] { new Color(0.15f, 0.40f, 0.15f), new Color(0.10f, 0.32f, 0.12f), new Color(0.20f, 0.45f, 0.18f) };
            int count = 8 + rng.Next(4); // 8-11 trees
            for (int i = 0; i < count; i++)
            {
                Vector3 p = ZonePoint(zone, rng, 3f, zone.Radius * 0.85f);
                float trunkH = palms ? 2.5f + (float)rng.NextDouble() * 1.5f
                    : 1.5f + (float)rng.NextDouble() * 1.5f;
                // Unity cylinder is 2 units tall, 1 unit across: scale y by h/2.
                MakeProp(root, "Tree_" + i + "_Trunk", PrimitiveType.Cylinder,
                    new Vector3(p.x, trunkH / 2f, p.z), new Vector3(0.5f, trunkH / 2f, 0.5f),
                    trunkColor, solid: false);
                float canopy = 1.6f + (float)rng.NextDouble() * 0.8f;
                var canopyGo = MakeProp(root, "Tree_" + i + "_Canopy", PrimitiveType.Sphere,
                    new Vector3(p.x, trunkH + canopy * 0.4f, p.z),
                    Vector3.one * canopy, canopyColors[rng.Next(canopyColors.Length)], solid: false);
                // The canopy sways; the trunk stays still.
                if (LivingEnvironment != null)
                    LivingEnvironment.RegisterTree(canopyGo.transform);
            }
        }

        private static void ScatterBarrels(MapZoneDef zone, System.Random rng)
        {
            var root = PropRoot(zone);
            var colors = new[]
            {
                new Color(0.45f, 0.25f, 0.12f), // rust
                new Color(0.25f, 0.25f, 0.28f), // dark steel
                new Color(0.35f, 0.35f, 0.15f), // olive
            };
            int count = 6 + rng.Next(4); // 6-9 barrels
            for (int i = 0; i < count; i++)
            {
                Vector3 p = ZonePoint(zone, rng, 3f, zone.Radius * 0.8f);
                MakeProp(root, "Barrel_" + i, PrimitiveType.Cylinder,
                    new Vector3(p.x, 0.55f, p.z), new Vector3(1f, 0.55f, 1f),
                    colors[rng.Next(colors.Length)], solid: false);
            }
        }

        private static void ScatterGravestones(MapZoneDef zone, System.Random rng)
        {
            var root = PropRoot(zone);
            var colors = new[]
            {
                new Color(0.50f, 0.50f, 0.52f), new Color(0.42f, 0.42f, 0.45f),
                new Color(0.58f, 0.58f, 0.60f),
            };
            int count = 8 + rng.Next(4); // 8-11 stones
            for (int i = 0; i < count; i++)
            {
                Vector3 p = ZonePoint(zone, rng, 3f, zone.Radius * 0.85f);
                var stone = MakeProp(root, "Gravestone_" + i, PrimitiveType.Cube,
                    new Vector3(p.x, 0.55f, p.z), new Vector3(0.8f, 1.1f, 0.25f),
                    colors[rng.Next(colors.Length)], solid: false);
                stone.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
        }

        private static void ScatterCrates(MapZoneDef zone, System.Random rng)
        {
            var root = PropRoot(zone);
            var colors = new[]
            {
                new Color(0.50f, 0.35f, 0.18f), new Color(0.45f, 0.30f, 0.15f),
            };
            int count = 6 + rng.Next(4); // 6-9 crates
            for (int i = 0; i < count; i++)
            {
                Vector3 p = ZonePoint(zone, rng, 3f, zone.Radius * 0.8f);
                float s = 1f + (float)rng.NextDouble() * 0.6f;
                var crate = MakeProp(root, "Crate_" + i, PrimitiveType.Cube,
                    new Vector3(p.x, s / 2f, p.z), Vector3.one * s,
                    colors[rng.Next(colors.Length)], solid: false);
                crate.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
        }

        private static void ScatterDunes(MapZoneDef zone, System.Random rng)
        {
            var root = PropRoot(zone);
            var sand = new Color(0.85f, 0.70f, 0.45f);
            int count = 4 + rng.Next(3); // 4-6 dunes
            for (int i = 0; i < count; i++)
            {
                Vector3 p = ZonePoint(zone, rng, 2f, zone.Radius * 0.8f);
                MakeProp(root, "Dune_" + i, PrimitiveType.Sphere,
                    new Vector3(p.x, 0f, p.z),
                    new Vector3(4f + (float)rng.NextDouble() * 3f,
                        0.8f + (float)rng.NextDouble() * 0.6f,
                        3f + (float)rng.NextDouble() * 2f),
                    sand, solid: false);
            }
        }

        private static void ScatterSnowMounds(MapZoneDef zone, System.Random rng)
        {
            var root = PropRoot(zone);
            var snow = new Color(0.92f, 0.93f, 0.96f);
            int count = 5 + rng.Next(3); // 5-7 mounds
            for (int i = 0; i < count; i++)
            {
                Vector3 p = ZonePoint(zone, rng, 2f, zone.Radius * 0.8f);
                MakeProp(root, "SnowMound_" + i, PrimitiveType.Sphere,
                    new Vector3(p.x, 0f, p.z),
                    new Vector3(3f + (float)rng.NextDouble() * 2f,
                        0.7f + (float)rng.NextDouble() * 0.5f,
                        2.5f + (float)rng.NextDouble() * 1.5f),
                    snow, solid: false);
            }
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
            LivingEnvironment = null;
            _zoneMarkers.Clear();
        }
    }
}
