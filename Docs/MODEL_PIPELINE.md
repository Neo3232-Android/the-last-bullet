# Model Pipeline — The Last Bullet

## Build 2: what ships

Build 2 bundles a curated subset of the library as FBX — **13 files,
85.5 MB on disk** — under `Assets/_Project/Resources/Models/`. Only
`Resources/` assets enter the headless build (the scene carries no
serialized references), so this folder is the whole model budget. The
other 22 character FBXs stay in `Assets/_Project/Models/Characters/`
(unbundled source) and no environment GLBs are bundled at all.

| File | Resources path | Size | Notes |
|---|---|---|---|
| `zombie-walker.fbx` | `Models/Zombies/zombie-walker` | 6.3 MB | 8 clips, Mixamo skeleton |
| `zombie-runner.fbx` | `Models/Zombies/zombie-runner` | 6.3 MB | 8 clips |
| `zombie-crawler.fbx` | `Models/Zombies/zombie-crawler` | 5.4 MB | 6 clips (Crawl, no Walk/Run) |
| `zombie-brute.fbx` | `Models/Zombies/zombie-brute` | 5.6 MB | 8 clips |
| `zombie-bloated.fbx` | `Models/Zombies/zombie-bloated` | 6.0 MB | 8 clips |
| `zombie-nurse.fbx` | `Models/Zombies/zombie-nurse` | 6.4 MB | 8 clips |
| `zombie-office-worker.fbx` | `Models/Zombies/zombie-office-worker` | 6.3 MB | 8 clips |
| `zombie-prisoner.fbx` | `Models/Zombies/zombie-prisoner` | 5.9 MB | 8 clips |
| `zombie-rotten.fbx` | `Models/Zombies/zombie-rotten` | 5.3 MB | 8 clips |
| `zombie-soldier.fbx` | `Models/Zombies/zombie-soldier` | 6.1 MB | 8 clips |
| `zombie-woman.fbx` | `Models/Zombies/zombie-woman` | 6.2 MB | 8 clips |
| `zombie-worker.fbx` | `Models/Zombies/zombie-worker` | 6.6 MB | 8 clips |
| `mannequin-man.fbx` | `Models/Player/mannequin-man` | 13.2 MB | 36 clips, 41k tris, player base |

Zombies total 72.3 MB; player 13.2 MB. All 12 zombies share one 65-bone
Mixamo skeleton (+ jaw), so Mixamo clips retarget onto all of them once
the rig is Humanoid (see below).

Deliberately NOT bundled in build 2: KayKit (17 MB each) and Quaternius
(8–9 MB each) players, the other vinish people (business-man etc.,
1 MB each), mannequin-woman (no gender choice in the creator — one
player model), the Shambler (not Mixamo; its walk/run clips live in
separate anim FBXs), and every environment GLB.

## The library (source, not shipped)

287 GLB files + 2 HDRIs, 295 MB, at `~/workspace/the-last-bullet/assets/models/`
(full inventory + credits in `MODELS.md` there). Licenses: vinish.dev models
CC BY 4.0 (credit required on the credits screen), Quaternius via Poly Pizza
CC0, KayKit CC0, SummerEngine free with attribution.

Best per category:
- Zombies: **zombie-worker** (91,042 tris, 8 clips).
- Player: **mannequin-man / mannequin-woman** (41k tris, 36 clips).
- Weapons: **assault-rifle** (13,447 tris).
- Vehicles: **raider-4x4** (147,902 tris — most detailed model in the library).
- Lighting: **cobblestone-street-night HDRI**.

## Step 1 — Convert characters (Blender batch, done 2026-11-27)

```bash
blender --background --python Tools/convert-glb-to-fbx.py -- \
  ~/workspace/the-last-bullet/assets/models \
  Assets/_Project/Models/Characters
```

The script (`Tools/convert-glb-to-fbx.py`) walks the source tree for
`*.glb` under `characters/` / `zombies/` / `player/`, imports each GLB,
**keeps Mixamo bone names intact** (no renaming — the Humanoid avatar
maps by bone name), bakes the animation clips, exports FBX with the same
base name. 35/35 converted; log in
`Assets/_Project/Models/Characters/conversion-log.txt`. The 13 build-2
files were then moved with `git mv` into `Resources/Models/`.

## Step 2 — Humanoid rigs at build time (ModelImportSetup, build 2)

The FBXs ship with **no .meta files**, so Unity would import them on
Generic rigs and the Mixamo clips could not retarget. There is no
manual import step: `Scripts/Editor/ModelImportSetup.cs`
(`TheLastBullet.Editor`) runs from
`BuildPreprocessor.OnPreprocessBuild` — before `SetupURP.EnsureURPAsset`
— and for every `.fbx` under `Assets/_Project/Resources/Models/` sets
`ModelImporter.animationType = Humanoid` via
`AssetImporter.GetAtPath`, calling `SaveAndReimport()` only when it was
not already Humanoid. Everything is try/catch-guarded with
`Debug.LogWarning`, so a failure here can never break the build (the
game then falls back to placeholders at runtime).

## Step 3 — Loading at runtime (ModelLibrary, build 2)

`Scripts/Core/ModelLibrary.cs` (`TheLastBullet.Core`) is the only loader.
It maps kinds to Resources paths + scale + tint, loads with
`Resources.Load<GameObject>` (cached prefab lookup), instantiates, and
never throws — a missing model returns null and the caller keeps the
placeholder figure.

```csharp
public static bool ModelsAvailable { get; }               // probe: player model loads
public static GameObject InstantiateZombieModel(ZombieKind kind);
public static GameObject InstantiateBossModel(BossKind kind);
public static GameObject InstantiatePlayerModel();
public static float ScaleFor(ZombieKind kind);            // scale to pass to ZombieAI.SetModel
public static float ScaleFor(BossKind kind);
public const string SpeedParam = "Speed";                 // Animator param ZombieAI/PlayerController drive
```

After instantiating, ModelLibrary strips the model's colliders (the
ZombieAI / PlayerController root CapsuleCollider is the only gameplay
collider) — except colliders on a bone Rigidbody, which belong to an
Editor-authored ragdoll — and attaches a `RagdollController` to the
model root if it does not already have one.

### Zombie mapping (12 FBXs cover 16 kinds)

Direct (own FBX, no tint): Walker→walker 1.0, Runner→runner 1.0,
Crawler→crawler 0.7, Brute→brute 1.4, Bloater→bloated 1.6,
Armored→soldier 1.15 (steel tint), Healer→nurse 1.0 (green tint).
Reuses (closest model, distinct tint/scale): Spitter→rotten 1.0 green,
Exploder→bloated 1.2 orange-red, Leaper→runner 1.0 blue,
Screamer→woman 1.0 pale, ZombieDog→crawler 0.6 brown,
Shieldbearer→prisoner 1.1 slate, Ghost→woman 1.0 pale blue +
transparent (alpha 0.45), FireZombie→worker 1.0 orange,
Spawner→office-worker 1.1 purple. Tint is a material-color multiplier
on the FBX's own textures (near-white, so the model's colours survive).

### Boss mapping (5 kinds, boss scale 2.2–2.6)

Charger→brute 2.2, Summoner→woman 2.2 purple, SpitterKing→rotten 2.4
green, TheButcher→worker 2.6 dark red, PlagueQueen→nurse 2.4 sickly
green.

### Wiring

- Player: `PlayerController.Setup()` calls
  `ModelLibrary.InstantiatePlayerModel()` after building the
  placeholder and calls `SetModel(model)` when non-null.
- Zombies/bosses: `GameManager.SpawnZombie` / `SpawnBoss` is the call
  site — after `zombie.Setup(...)` the parent adds
  `zombie.SetModel(ModelLibrary.InstantiateZombieModel(kind),
  ModelLibrary.ScaleFor(kind))` (bosses: `InstantiateBossModel` /
  `ScaleFor(BossKind)`), guarded by a null check so a missing model
  keeps the placeholder.

### Clip names (verified from the FBX binaries, 2026-11-27)

Every zombie FBX carries **Attack, Bite, Death, Hit, Idle, Run, Scream,
Walk** — except the crawler, which carries **Attack, Bite, Crawl, Death,
Hit, Idle** (no Walk/Run/Scream). mannequin-man carries 36 clips:
Block, Bow, Cheering, Clapping, Crouch, Dance, Death, Fight Idle, Hit
Reaction, Idle, Jump, Jumping Jacks, Kick, Kneeling, Look Around, Lunge,
Pick Up, Plank, Pointing, Punch, Push, Push Up, Run, Salute, Shrug,
Sitting Idle, Sitting On Ground, Sneak, Squat, Stretch, Talking On
Phone, Thinking, Throw, Walk, Walk Backward, Wave.

## Step 4 — Procedural environments (MapBuilder.PopulateZone, build 2)

The full glTFast environment sets are **not** in build 2: no verified
glTFast API was available on this machine and the GLB library is not
bundled. Instead `PopulateZone()` scatters a small deterministic set of
primitives per open zone (seeded by zone id via a stable djb2 hash, so a
zone always looks the same), chosen by the zone's `ModelSet`:

| ModelSet | Props |
|---|---|
| city, mall, prison, school, casino, subway, hospital, lab, castle, stadium | Buildings (boxes, varied height/colour per set) |
| rooftop | Low AC-unit blocks |
| forest, farm | Trees (cylinder trunk + sphere canopy) |
| beach | Palm trees (taller trunk, green canopy) |
| junkyard, sewer | Barrels (cylinders, rust/steel/olive) |
| graveyard | Gravestones (grey boxes, random yaw) |
| airport, dungeon | Crates (wood cubes, random yaw) |
| desert | Dunes (flattened spheres, sand) |
| snow | Snow mounds (flattened spheres, white) |

Max ~12 props per zone on purpose. Props use the existing URP Lit
null-guarded material pattern (shared materials, one per colour), are
marked static, and never block movement — their primitive colliders
are destroyed — except large buildings, which keep a BoxCollider.
Movement is clamped to the arena in code anyway. The glowing zone-ring
markers from `BuildArena` are untouched.

## NOT in build 2 — follow-ups

- **AnimatorControllers for the clips.** The FBX clips exist and the
  Humanoid rig makes them retargetable, but nothing plays them yet: no
  controller asset references them. Until an Editor-authored controller
  (states Walk/Run/Idle/Attack/Death + a `Speed` float parameter, and
  the player's Walk/Run/Idle/Punch/Death) is assigned, the models stand
  in their bind pose while `ZombieAI`/`PlayerController` drive the
  `Speed` parameter into a null/absent controller. The game still runs
  — both controllers null-guard the Animator — but the models do not
  animate. This is the top unverified item: clip names above are read
  from the FBX binaries, not observed playing in the Unity Editor.
- **Ragdoll authoring.** `RagdollController` is attached at runtime,
  but the FBXs carry no Rigidbodies/CharacterJoints — those come from
  the Ragdoll Wizard (GameObject → 3D Object → Ragdoll...) per model
  root in the Editor. Until then `EnableRagdoll` is a no-op and deaths
  fall back to the tip-over. `ModelLibrary` deliberately keeps
  colliders that sit on a bone Rigidbody, so a future Wizard pass is
  not stripped at runtime.
- **glTFast environment sets.** The per-map GLB sets (weapons, vehicles,
  props, the 20 maps' environment sets) still need a verified glTFast
  load path and per-map selection; `PopulateZone` is the hook.
- **Material merging / texture atlasing (before shipping).** Vehicles
  and mall shops are material-heavy — **52–75 materials per vehicle**.
  Before shipping: merge each vehicle's materials into 1–3 atlased
  materials (Blender, Mesh Baker, or an editor script) and downscale
  textures per quality tier (Low: 512px, Medium: 1024px, High/Ultra:
  full res). `QualitySettingsApplier` already scales the URP render
  scale; texture budgets are set per import in the Editor.
- **APK budget.** The base APK is already ~135 MB (shaders); the 13
  models add ~85 MB on disk (~91 MB was the estimate — actual is
  85.5 MB). If the APK grows past budget, the levers are texture
  downscaling on the FBXs and Addressables/AssetBundles per map
  (long-term answer); the single-APK route only ever bundles the
  shared character set, never the whole 295 MB library.

## Credits screen

All CC BY 4.0 models (vinish.dev) must be credited in-game. The credits list
is in `MODELS.md` — the menu's credits panel reads from it (stage 2).
