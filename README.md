# The Last Bullet

Top-down zombie survival shooter — full real 3D — built with **Unity 6.3 LTS**
(6000.3.29f1) and URP. Twin-stick manual aim, 20 weapons, 16 zombie kinds +
5 bosses, 20 big eventful maps × 10 levels = **200 levels**, ragdoll deaths
for zombies AND the player, character creator, progression, daily missions,
login rewards, bestiary, super perks. Portrait, offline, 60fps, Low → Ultra
quality settings.

This is the Unity rewrite of the Kotlin prototype (kept as reference in
`~/workspace/the-last-bullet/` — do not modify it). The design (weapons,
zombies, maps, progression) is ported 1:1; the engine is all Unity.

## Tech choices (best in everything — compared, not defaulted)

| Choice | Winner | Why it wins |
|---|---|---|
| Unity version | **6000.3.29f1 (Unity 6.3 LTS)** | Supported until Dec 2027; 6.0 LTS support ended Oct 2026; 6.7 LTS doesn't exist yet |
| Render pipeline | **URP 17.3.0** | HDRP doesn't support Android mobile; Built-in is legacy |
| Physics | **Built-in PhysX** | Havok is DOTS-only and heavier — loses for this game |
| Animation | **Mecanim Humanoid + Mixamo retargeting** | glTFast imports are Generic-rig only — no Humanoid retargeting; Blender GLB→FBX is the character pipeline |
| Ragdoll | **Ragdoll Wizard + `RagdollController`** | Editor-authored joints, runtime switch — replaces the old custom verlet plan |
| UI | **uGUI + TextMeshPro** | Input System package present; twin sticks hand-rolled as touch zones (no serialized `.inputactions` asset — headless-safe) |
| Audio | **Procedural synth** | Zero audio files; every sound generated in code |
| Build | **game-ci unity-builder v4** | Free for public repos; activation via `unity-request-activation-file@v2` |
| Android target | **API 36 (Android 16), min 26, ARM64, IL2CPP** | Play Store requires API 36 for new apps since Aug 2026 |

## Project layout

```
Assets/_Project/
  Scenes/Main.unity        # Minimal scene: camera + light only. Bootstrap wires everything at runtime.
  Scripts/
    Core/                  # Bootstrap, GameManager, SaveSystem, LevelDirector, WaveDirector, MapBuilder, JuiceController
    Data/                  # Weapon/Zombie/Map/Perk/GameState/Progression/Level data (the design, ported)
    Gameplay/              # PlayerController, ZombieAI, WeaponSystem, Bullet, Pickup, Buildables, MapEvents
    Ragdoll/               # RagdollController (runtime ragdoll switch)
    Audio/                 # SynthAudio (procedural sounds)
    UI/                    # TwinStickInput, HUDController, MenuController
    Editor/                # BuildScript, SetupURP (headless builds + URP setup)
    Tests/EditMode/        # GameDataTests (NUnit, runs in CI before the APK build)
  Models/                  # Converted FBX characters + glTFast GLB environments (see Docs/MODEL_PIPELINE.md)
Packages/manifest.json     # Pinned package versions (URP 17.3.0, Input System 1.14.2, glTFast 6.9.1, ...)
ProjectSettings/           # Android, com.neo.thelastbullet, portrait, 60fps, IL2CPP, ARM64
.github/workflows/         # android-build.yml, activation.yml
Docs/MODEL_PIPELINE.md     # GLB → FBX plan, material merging, APK size budget
Tools/convert-glb-to-fbx.py# Blender batch converter (GLB → FBX, keeps Mixamo bone names)
```

## The level system

20 maps × 10 levels = **200 levels**. Each level is 3 waves (boss levels 5
and 10 are 2 waves + a boss). Clearing level 10 unlocks **Endless** on that map.

- **Big maps:** arenas 40–90 world units with 3–5 named zones (points of
  interest) that open progressively as levels rise.
- **Escalation:** health ×(1 + 0.12/level), counts ×(1 + 0.25/level), faster
  spawns, elites from level 6, new zombie kinds introduced level by level.
- **Events:** fog, supply drops, horde surges, double-coins, map hazards,
  last stand, nightfall (dark maps), rainstorm (outdoor maps) — plus a
  per-map signature hazard (Graveyard graves burst open, Casino jackpot,
  Mall shutters drop free barricades, ...).
- **Medals:** Bronze / Silver / Gold / Platinum per level (par time + damage
  taken). Progress persists in the player profile.

## Setup (first time)

### 1. Create the repo (on the Neo3232-Android GitHub account)

- GitHub → New repository → **the-last-bullet** (public — the free plan's
  game-ci builds are unlimited for public repos)
- Push this project to it. Git LFS is already configured (`.gitattributes`)
  for `*.glb *.fbx *.hdr *.exr *.png *.mp3 *.wav *.unitypackage`.

### 2. Unity license activation (one-time, ~5 minutes)

Unity Personal is free (under $200k revenue, no runtime fee, no royalties).
The CI build needs a license file:

1. Create a Unity ID at https://id.unity.com (free).
2. In the repo: **Actions → "Request Unity activation file" → Run workflow**.
   Download the `.alf` file from the run's artifacts.
3. Upload the `.alf` at https://license.unity3d.com/manual, choose
   **Unity Personal**, download the `.ulf` license file.
4. In the repo: **Settings → Secrets and variables → Actions → New repository secret**:
   - `UNITY_LICENSE` — the **full contents** of the `.ulf` file (paste the whole file)
   - `UNITY_EMAIL` — your Unity ID email
   - `UNITY_PASSWORD` — your Unity ID password
5. Push a commit (or re-run the build workflow) — the APK builds.

Never commit a `.ulf` file or password to the repo.

### 3. Build

Push to `main` (or run the workflow manually) — the workflow runs the
EditMode tests first, then builds the APK with
`TheLastBullet.Editor.BuildScript.BuildAndroidApk` and uploads it as an
artifact (`the-last-bullet-apk`, 30-day retention). Install the APK on the
phone to playtest.

Release (Play Store) signing with a keystore is a later step — the CI build
is currently an unsigned debug APK.

## Models

The model library (287 GLBs + 2 HDRIs, 295 MB) lives outside this repo at
`~/workspace/the-last-bullet/assets/models/` — see `Docs/MODEL_PIPELINE.md`
for the conversion plan (Blender GLB→FBX for characters, glTFast for
statics), material merging, and the per-map APK size budget (80–150 MB).

## Status

- Stage 1 (research): done — versions and packages verified.
- Stage 2 (scaffold): done — data/core/gameplay/UI/editor/tests all written.
- Stage 3 (GitHub): workflows + README ready; **repo creation + secrets
  still need the user** (steps above).
- Stage 4 (models): plan ready; conversion not yet run (needs Blender).
- **Not yet verified:** no C# compiler or Unity Editor exists on this
  machine — the code is written carefully but **unverified until the first
  GitHub Actions build**. Expect the first build to surface a few errors;
  fix them in the workflow log.
