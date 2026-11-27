# Model Pipeline — The Last Bullet

## The library

287 GLB files + 2 HDRIs, 295 MB, at `~/workspace/the-last-bullet/assets/models/`
(full inventory + credits in `MODELS.md` there). All 20 maps have dedicated
environment sets. Licenses: vinish.dev models CC BY 4.0 (credit required on
the credits screen), Quaternius via Poly Pizza CC0, KayKit CC0, SummerEngine
free with attribution.

Best per category:
- Zombies: **zombie-worker** (91,042 tris, 8 clips) — all 12 zombies share one
  65-bone Mixamo skeleton (+ jaw), so Mixamo clips retarget onto all of them.
- Player: **mannequin-man / mannequin-woman** (41k tris, 36 clips).
- Weapons: **assault-rifle** (13,447 tris).
- Vehicles: **raider-4x4** (147,902 tris — most detailed model in the library).
- Lighting: **cobblestone-street-night HDRI**.

## The problem: Unity does not import GLB natively

Two routes, used for different things:

| Route | For | Why |
|---|---|---|
| **Blender GLB → FBX (primary for characters)** | Zombies + player | Unity's Humanoid avatar system + Mixamo retargeting only works with FBX. glTFast imports are **Generic-rig only** — Humanoid retargeting is NOT available for a glTFast import, and its animation import is legacy-only. FBX is what the Humanoid system is built around. |
| **glTFast (fast path for statics)** | Props, environments, weapons | No conversion needed; `com.unity.cloud.gltfast` 6.9.1 is already in the manifest. Static meshes don't need retargeting. |

## Step 1 — Convert characters (Blender batch)

Blender is **not** installed on this machine, so the conversion has not run
yet. On any machine with Blender 4.x:

```bash
blender --background --python Tools/convert-glb-to-fbx.py -- \
  ~/workspace/the-last-bullet/assets/models \
  Assets/_Project/Models/Characters
```

The script (`Tools/convert-glb-to-fbx.py`):
- Walks the source tree for `*.glb` under `characters/` / `zombies/` / `player/`.
- Imports each GLB, **keeps Mixamo bone names intact** (no renaming — the
  Humanoid avatar maps by bone name), bakes the animation clips, exports FBX
  with the same base name.
- Skips files already converted (safe to re-run).
- Writes a `conversion-log.txt` next to the output.

After conversion, import the FBXs into Unity, set Rig → **Humanoid**, and
let Unity auto-map the Mixamo bones. Then author ragdolls with the Ragdoll
Wizard (GameObject → 3D Object → Ragdoll...) on each model root —
`RagdollController` switches to the ragdoll at death.

## Step 2 — Import statics with glTFast

Point glTFast at the GLB path (or copy the needed GLBs into
`Assets/_Project/Models/Environments/<map>/`). No conversion, no bone work.
Stage-2 code (`MapBuilder.PopulateZone()`) is the hook that instantiates the
environment sets per map zone.

## Step 3 — Material merging / texture atlasing (before shipping)

**Flag:** vehicles and mall shops are material-heavy — **52–75 materials per
vehicle**. On Low quality that many draw calls will hurt. Before shipping:

1. In Blender (or with Unity's Mesh Baker / a simple editor script), merge
   each vehicle's materials into 1–3 atlased materials.
2. Downscale textures per quality tier (Low: 512px, Medium: 1024px,
   High/Ultra: full res) — `QualitySettingsApplier` already scales the URP
   render scale; texture budgets are set per import in the Editor.
3. Do the same pass for mall shop props (second-heaviest set).

## Step 4 — Per-map APK budget (80–150 MB)

The APK must **not** bundle the whole 295 MB library. Rules:

- Bundle **only the models a map needs** (its environment set + the shared
  zombie/player/weapon sets). The 20 maps share characters and weapons, so
  the shared set is paid once; environment sets are per-map.
- Addressables / AssetBundles per map is the long-term answer; stage 1 ships
  a single APK with the first maps' sets and adds the rest as maps unlock
  (or a second APK/AAB split later).
- Texture downscaling (above) is the biggest lever — the library's textures
  are the bulk of the 295 MB.

## Credits screen

All CC BY 4.0 models (vinish.dev) must be credited in-game. The credits list
is in `MODELS.md` — the menu's credits panel reads from it (stage 2).
