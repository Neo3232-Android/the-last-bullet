using System;
using System.Collections.Generic;
using UnityEngine;
using TheLastBullet.Data;
using TheLastBullet.Ragdoll;

namespace TheLastBullet.Core
{
    /// <summary>
    /// Build 2 model loader. The 13 FBXs bundled in the APK live under
    /// Assets/_Project/Resources/Models/ (12 zombies + the mannequin-man
    /// player base) and are loaded here with Resources.Load, because the
    /// scene carries no serialized references and only Resources assets
    /// (or code references) enter a headless build.
    ///
    /// Every method returns null — never throws — when a model is missing,
    /// so callers fall back to the placeholder figures. Tint is a
    /// material-color multiplier on the FBX's own textures (near-white, so
    /// the model's own colours survive); the Ghost also goes transparent.
    ///
    /// Clip names verified from the FBX binaries (2026-11-27): every zombie
    /// carries Attack, Bite, Death, Hit, Idle, Run, Scream, Walk — except
    /// the crawler, which carries Attack, Bite, Crawl, Death, Hit, Idle.
    /// mannequin-man carries 36 clips (Walk, Run, Idle, Death, Punch,
    /// Kick, Block, ...). Clips are played at runtime by ModelAnimator
    /// (legacy Animation, keyword-matched) — no Editor-authored
    /// AnimatorController is needed. The Animator's locomotion parameter
    /// is "Speed", which ZombieAI and PlayerController already drive.
    /// </summary>
    public static class ModelLibrary
    {
        /// <summary>Animator float parameter the game code drives for locomotion.</summary>
        public const string SpeedParam = "Speed";

        private const string PlayerPath = "Models/Player/mannequin-man";
        private const string ZombieFolder = "Models/Zombies/";

        private struct ModelEntry
        {
            public string Path;
            public float Scale;
            public Color Tint;
            public bool Transparent;

            public ModelEntry(string path, float scale, Color tint, bool transparent)
            {
                Path = path;
                Scale = scale;
                Tint = tint;
                Transparent = transparent;
            }
        }

        // Direct 1:1 kinds use their own FBX at the game's KindScale with no
        // tint (white multiplier). The other 9 kinds reuse the closest FBX
        // with a distinct tint/scale so all 16 kinds read differently.
        private static readonly Dictionary<ZombieKind, ModelEntry> _zombies =
            new Dictionary<ZombieKind, ModelEntry>
            {
                { ZombieKind.Walker,       new ModelEntry(ZombieFolder + "zombie-walker",        1.0f, Color.white,                            false) },
                { ZombieKind.Runner,       new ModelEntry(ZombieFolder + "zombie-runner",        1.0f, Color.white,                            false) },
                { ZombieKind.Crawler,      new ModelEntry(ZombieFolder + "zombie-crawler",       0.7f, Color.white,                            false) },
                { ZombieKind.Brute,        new ModelEntry(ZombieFolder + "zombie-brute",         1.4f, Color.white,                            false) },
                { ZombieKind.Bloater,      new ModelEntry(ZombieFolder + "zombie-bloated",       1.6f, Color.white,                            false) },
                { ZombieKind.Armored,      new ModelEntry(ZombieFolder + "zombie-soldier",       1.15f, new Color(0.70f, 0.75f, 0.85f),       false) },
                { ZombieKind.Healer,       new ModelEntry(ZombieFolder + "zombie-nurse",         1.0f, new Color(0.65f, 1.0f, 0.65f),         false) },
                // Reuses — closest model, distinct tint/scale:
                { ZombieKind.Spitter,      new ModelEntry(ZombieFolder + "zombie-rotten",        1.0f, new Color(0.65f, 1.0f, 0.65f),         false) }, // diseased, spits acid
                { ZombieKind.Exploder,     new ModelEntry(ZombieFolder + "zombie-bloated",       1.2f, new Color(1.0f, 0.55f, 0.45f),         false) }, // swollen with explosives
                { ZombieKind.Leaper,       new ModelEntry(ZombieFolder + "zombie-runner",        1.0f, new Color(0.65f, 0.80f, 1.0f),         false) }, // fast, athletic
                { ZombieKind.Screamer,     new ModelEntry(ZombieFolder + "zombie-woman",         1.0f, new Color(0.90f, 0.90f, 1.0f),         false) }, // pale banshee
                { ZombieKind.ZombieDog,    new ModelEntry(ZombieFolder + "zombie-crawler",       0.6f, new Color(0.85f, 0.70f, 0.55f),         false) }, // low, quadruped-ish
                { ZombieKind.Shieldbearer, new ModelEntry(ZombieFolder + "zombie-prisoner",      1.1f, new Color(0.60f, 0.65f, 0.75f),         false) }, // bulky, slate
                { ZombieKind.Ghost,        new ModelEntry(ZombieFolder + "zombie-woman",         1.0f, new Color(0.70f, 0.85f, 1.0f, 0.45f),  true) },  // pale blue, see-through
                { ZombieKind.FireZombie,   new ModelEntry(ZombieFolder + "zombie-worker",        1.0f, new Color(1.0f, 0.60f, 0.40f),         false) }, // burning orange
                { ZombieKind.Spawner,      new ModelEntry(ZombieFolder + "zombie-office-worker", 1.1f, new Color(0.85f, 0.65f, 1.0f),         false) }, // spawns more of itself
            };

        // Bosses reuse zombie models at boss scale (placeholder bosses were
        // 2.6x; HeadHeight for bosses is 2.6 in ZombieAI).
        private static readonly Dictionary<BossKind, ModelEntry> _bosses =
            new Dictionary<BossKind, ModelEntry>
            {
                { BossKind.Charger,     new ModelEntry(ZombieFolder + "zombie-brute",  2.2f, Color.white,                       false) },
                { BossKind.Summoner,    new ModelEntry(ZombieFolder + "zombie-woman",  2.2f, new Color(0.85f, 0.65f, 1.0f),    false) },
                { BossKind.SpitterKing, new ModelEntry(ZombieFolder + "zombie-rotten", 2.4f, new Color(0.65f, 1.0f, 0.65f),    false) },
                { BossKind.TheButcher,  new ModelEntry(ZombieFolder + "zombie-worker", 2.6f, new Color(1.0f, 0.50f, 0.45f),    false) },
                { BossKind.PlagueQueen, new ModelEntry(ZombieFolder + "zombie-nurse",  2.4f, new Color(0.70f, 1.0f, 0.60f),    false) },
            };

        private static readonly Dictionary<string, GameObject> _prefabs =
            new Dictionary<string, GameObject>();

        private static bool? _available;

        /// <summary>
        /// True once the player model loads from Resources. Callers use this
        /// to decide between the real model and the placeholder figure.
        /// Probes once and caches the answer.
        /// </summary>
        public static bool ModelsAvailable
        {
            get
            {
                if (!_available.HasValue)
                {
                    try { _available = Resources.Load<GameObject>(PlayerPath) != null; }
                    catch (Exception) { _available = false; }
                }
                return _available.Value;
            }
        }

        /// <summary>Scale to pass to ZombieAI.SetModel for this kind.</summary>
        public static float ScaleFor(ZombieKind kind)
        {
            ModelEntry entry;
            return _zombies.TryGetValue(kind, out entry) ? entry.Scale : 1f;
        }

        /// <summary>Scale to pass to ZombieAI.SetModel for this boss.</summary>
        public static float ScaleFor(BossKind kind)
        {
            ModelEntry entry;
            return _bosses.TryGetValue(kind, out entry) ? entry.Scale : 2.4f;
        }

        /// <summary>Instantiate the FBX for a zombie kind, tinted/scaled per the map. Null if missing.</summary>
        public static GameObject InstantiateZombieModel(ZombieKind kind)
        {
            ModelEntry entry;
            if (!_zombies.TryGetValue(kind, out entry)) return null;
            return InstantiateEntry(entry);
        }

        /// <summary>Instantiate the FBX for a boss kind, tinted/scaled per the map. Null if missing.</summary>
        public static GameObject InstantiateBossModel(BossKind kind)
        {
            ModelEntry entry;
            if (!_bosses.TryGetValue(kind, out entry)) return null;
            return InstantiateEntry(entry);
        }

        /// <summary>Instantiate the mannequin-man player base. Null if missing.</summary>
        public static GameObject InstantiatePlayerModel()
        {
            return InstantiateEntry(new ModelEntry(PlayerPath, 1f, Color.white, false));
        }

        private static GameObject InstantiateEntry(ModelEntry entry)
        {
            try
            {
                GameObject prefab;
                if (!_prefabs.TryGetValue(entry.Path, out prefab) || prefab == null)
                {
                    prefab = Resources.Load<GameObject>(entry.Path);
                    if (prefab == null)
                    {
                        Debug.LogWarning("[ModelLibrary] Missing model: " + entry.Path);
                        return null;
                    }
                    _prefabs[entry.Path] = prefab;
                }

                var instance = (GameObject)UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name;
                instance.transform.localScale = Vector3.one * entry.Scale;
                ApplyTint(instance, entry);
                StripNonRagdollColliders(instance);
                EnsureRagdollController(instance);
                AttachModelAnimator(instance, entry.Path);
                return instance;
            }
            catch (Exception e)
            {
                // A model must never break a spawn — the caller falls back
                // to the placeholder when this returns null.
                Debug.LogWarning("[ModelLibrary] Failed to instantiate " + entry.Path + ": " + e.Message);
                return null;
            }
        }

        private static void ApplyTint(GameObject root, ModelEntry entry)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mat = r.material; // instanced copy — never touches the shared FBX material
                if (mat == null) continue;
                mat.color = entry.Tint;
                if (!entry.Transparent) continue;
                // Ghost: URP Lit in transparent mode, same pattern as the
                // zone rings in MapBuilder.
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
            }
        }

        /// <summary>
        /// The ZombieAI / PlayerController root CapsuleCollider is the only
        /// gameplay collider. Import-time colliders on the model would
        /// double-hit, so they are removed — except colliders sitting on a
        /// bone Rigidbody, which belong to an Editor-authored ragdoll
        /// (Ragdoll Wizard) and must survive for EnableRagdoll to collide.
        /// Today the FBXs carry no Rigidbodies, so everything is stripped.
        /// </summary>
        private static void StripNonRagdollColliders(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>();
            if (colliders == null) return;
            foreach (var c in colliders)
            {
                if (c == null) continue;
                if (c.GetComponent<Rigidbody>() != null) continue;
                UnityEngine.Object.Destroy(c);
            }
        }

        /// <summary>
        /// RagdollController switches to the ragdoll at death; it looks for
        /// Animator / Rigidbodies / CharacterJoints under the model root.
        /// The FBXs ship without ragdoll bones (Wizard authoring is an
        /// Editor step), so until then this is a harmless no-op that keeps
        /// the death fallback (tip-over) working.
        /// </summary>
        private static void EnsureRagdollController(GameObject root)
        {
            if (root.GetComponent<RagdollController>() != null) return;
            root.AddComponent<RagdollController>();
        }

        /// <summary>
        /// Tint a model root with the character creator's outfit colour. The
        /// mannequin is a single mesh / single material, so the whole figure
        /// takes the outfit colour (stylised team-colour read); a proper
        /// multi-material retexture needs an Editor pass.
        /// </summary>
        public static void ApplyOutfitTint(GameObject root, Color tint)
        {
            if (root == null) return;
            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>();
                if (renderers == null) return;
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    var mat = r.material; // instanced copy
                    if (mat == null) continue;
                    mat.color = tint;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ModelLibrary] outfit tint failed: " + e.Message);
            }
        }

        /// <summary>
        /// ModelAnimator plays the FBX's own clips at runtime (legacy
        /// Animation, keyword-matched) and adds a procedural bob. It is a
        /// no-op when no clips exist, so a model without clips still moves.
        /// </summary>
        private static void AttachModelAnimator(GameObject root, string resourcesPath)
        {
            if (root.GetComponent<ModelAnimator>() != null) return;
            var animator = root.AddComponent<ModelAnimator>();
            animator.Init(resourcesPath);
        }
    }
}
