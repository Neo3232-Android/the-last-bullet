#!/usr/bin/env python3
"""
Batch-convert The Last Bullet character GLBs to FBX with Blender.

Why FBX: Unity's Humanoid avatar system + Mixamo retargeting only works with
FBX. glTFast imports are Generic-rig only and cannot retarget, so characters
(zombies + player) go through this converter; static props/environments use
glTFast directly and skip conversion.

Usage (Blender 4.x required — NOT installed on the build VM yet):
    blender --background --python convert-glb-to-fbx.py -- \
        ~/workspace/the-last-bullet/assets/models \
        Assets/_Project/Models/Characters

Rules:
- Only converts GLBs under characters/, zombies/, player/ subfolders.
- Keeps Mixamo bone names intact (no renaming — Humanoid maps by bone name).
- Bakes animation clips into the FBX.
- Skips files already converted (safe to re-run).
- Writes conversion-log.txt next to the output folder.
"""
import os
import sys

# Blender runs this script with its own Python; bpy is only importable there.
try:
    import bpy
except ImportError:
    print("ERROR: this script must be run inside Blender:")
    print("  blender --background --python convert-glb-to-fbx.py -- <src> <dst>")
    sys.exit(1)

CHARACTER_DIRS = ("characters", "zombies", "player", "character", "zombie")


def is_character_glb(path, src_root):
    rel = os.path.relpath(path, src_root).replace(os.sep, "/").lower()
    parts = rel.split("/")
    return any(p in CHARACTER_DIRS for p in parts[:-1])


def convert_one(src, dst):
    # Fresh scene per file so nothing leaks between conversions.
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    # FBX export: bake animations, keep the armature + meshes, no bone renames.
    bpy.ops.export_scene.fbx(
        filepath=dst,
        use_selection=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=True,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        add_leaf_bones=False,          # leaf bones break Mixamo mapping
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
    )


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) < 2:
        print("Usage: blender --background --python convert-glb-to-fbx.py -- <src_models_dir> <dst_dir>")
        sys.exit(1)
    src_root, dst_root = argv[0], argv[1]
    os.makedirs(dst_root, exist_ok=True)

    log_lines = []
    converted = skipped = 0
    for dirpath, _dirs, files in os.walk(src_root):
        for name in sorted(files):
            if not name.lower().endswith(".glb"):
                continue
            src = os.path.join(dirpath, name)
            if not is_character_glb(src, src_root):
                continue
            rel = os.path.relpath(src, src_root)
            dst = os.path.join(dst_root, os.path.splitext(rel)[0] + ".fbx")
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            if os.path.exists(dst):
                log_lines.append(f"SKIP (exists): {rel}")
                skipped += 1
                continue
            try:
                convert_one(src, dst)
                log_lines.append(f"OK: {rel} -> {os.path.relpath(dst, dst_root)}")
                converted += 1
            except Exception as exc:  # noqa: BLE001 — keep converting the rest
                log_lines.append(f"FAIL: {rel}: {exc}")

    log_path = os.path.join(dst_root, "conversion-log.txt")
    with open(log_path, "w", encoding="utf-8") as f:
        f.write("\n".join(log_lines))
        f.write(f"\n\nConverted: {converted}, skipped: {skipped}\n")
    print(f"Done. Converted: {converted}, skipped: {skipped}. Log: {log_path}")


main()
