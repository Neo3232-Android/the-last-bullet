# Models

Real 3D models for The Last Bullet live here, converted from the GLB library
at `~/workspace/the-last-bullet/assets/models/` (287 GLBs + 2 HDRIs, 295 MB —
see `MODELS.md` in that folder for the full inventory and credits).

## Pipeline

1. **Characters (zombies + player):** Blender batch converts GLB → FBX
   (`Tools/convert-glb-to-fbx.py`). FBX is the primary character pipeline
   because Unity's Humanoid avatar system + Mixamo retargeting only works
   with FBX — glTFast imports are Generic-rig only and cannot retarget.
   Keep Mixamo bone names intact.
2. **Static props / environments:** import the GLBs directly with glTFast
   (`com.unity.cloud.gltfast` 6.9.1) — no conversion needed, fast path.
3. **Ragdolls:** author in the Editor with the Ragdoll Wizard
   (GameObject → 3D Object → Ragdoll...) on each character model root, then
   `RagdollController` (in `Scripts/Ragdoll/`) switches to it at death.
4. **Before shipping:** merge materials / atlas textures for the
   material-heavy sets (vehicles: 52–75 materials each; mall shops).
   The APK bundles a per-map selection with downscaled textures, NOT the
   whole library (80–150 MB budget).

## What goes where

- `Characters/` — FBX zombies (12) + player characters (mannequin-man/woman)
- `Weapons/` — FBX weapon models
- `Environments/` — glTFast-imported GLBs, one folder per map (20 maps)
- `Props/` — barricades, turrets, traps, pickups

Only the models a map needs are included in that map's build — see
`Docs/MODEL_PIPELINE.md`.
