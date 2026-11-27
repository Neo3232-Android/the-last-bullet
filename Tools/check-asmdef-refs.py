#!/usr/bin/env python3
"""Check every asmdef's references cover the package namespaces its files use.
Assembly references are NOT transitive in Unity: an asmdef must list every
package assembly its own files use, even if another asmdef already lists it.
Run from the project root: python3 Tools/check-asmdef-refs.py
Exits non-zero if any required reference is missing."""
import json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(ROOT, "Assets", "_Project", "Scripts")

# namespace prefix -> assembly name (None = auto-available, no asmdef entry needed)
NS_TO_ASM = [
    ("UnityEngine.Rendering.Universal", "Unity.RenderPipelines.Universal.Runtime"),
    ("TMPro", "Unity.TextMeshPro"),
    ("UnityEngine.UI", "UnityEngine.UI"),
    ("UnityEngine.InputSystem", "Unity.InputSystem"),
    ("GLTFast", "GLTFast"),
    ("Unity.Animation.Rigging", "Unity.Animation.Rigging"),
    ("UnityEngine.TestRunner", "UnityEngine.TestRunner"),
    ("NUnit.Framework", "UnityEngine.TestRunner"),
    ("UnityEditor", None),
    ("UnityEngine.Rendering", None),
    ("UnityEngine", None),
    ("System", None),
    ("TheLastBullet", None),  # own assemblies, referenced by name below
]

def asm_for(ns):
    for prefix, asm in NS_TO_ASM:
        if ns == prefix or ns.startswith(prefix + "."):
            return asm
    return None  # unknown namespace: ignore (System.* etc. handled above)

def find_asmdefs():
    out = []
    for dirpath, _, files in os.walk(SCRIPTS):
        for f in files:
            if f.endswith(".asmdef"):
                out.append(os.path.join(dirpath, f))
    return sorted(out)

def files_for(asmdef_path, all_asmdefs):
    """All .cs files under the asmdef's folder, excluding subfolders that have
    their own asmdef."""
    base = os.path.dirname(asmdef_path)
    owned = set(os.path.dirname(p) for p in all_asmdefs if p != asmdef_path)
    result = []
    for dirpath, dirnames, files in os.walk(base):
        # skip subtrees owned by another asmdef
        if any(dirpath == o or dirpath.startswith(o + os.sep) for o in owned):
            dirnames[:] = []
            continue
        for f in files:
            if f.endswith(".cs"):
                result.append(os.path.join(dirpath, f))
    return sorted(result)

def main():
    asmdefs = find_asmdefs()
    problems = 0
    for path in asmdefs:
        data = json.load(open(path))
        refs = set(data.get("references", []))
        name = data.get("name", os.path.basename(path))
        for cs in files_for(path, asmdefs):
            src = open(cs, encoding="utf-8", errors="ignore").read()
            for ns in sorted(set(re.findall(r"^\s*using\s+([A-Za-z0-9_.]+)\s*;", src, re.M))):
                need = asm_for(ns)
                if need is None:
                    continue
                # own-assembly reference is by asmdef name
                if ns.startswith("TheLastBullet"):
                    need = "TheLastBullet"
                if need != "TheLastBullet" and need not in refs and need != name:
                    print(f"MISSING: {name} needs '{need}' (used as '{ns}' in {os.path.relpath(cs, ROOT)})")
                    problems += 1
                if need == "TheLastBullet" and name != "TheLastBullet" and "TheLastBullet" not in refs:
                    print(f"MISSING: {name} needs 'TheLastBullet' (used in {os.path.relpath(cs, ROOT)})")
                    problems += 1
            # Fully-qualified references (no using directive) that live in a
            # package assembly: InputSystemUIInputModule is in the
            # UnityEngine.InputSystem.UI NAMESPACE (Input System 1.1+,
            # verified against the 1.14.2 package source) but the
            # Unity.InputSystem ASSEMBLY. Build #15 failed on the missing
            # assembly reference (CS0234) and build #16 on the wrong
            # namespace (the old UnityEngine.EventSystems one no longer
            # exists), so the checker must catch both even without a using
            # directive.
            if re.search(r"EventSystems\.InputSystemUIInputModule", src):
                print(f"WRONG NAMESPACE: {name} uses 'UnityEngine.EventSystems.InputSystemUIInputModule' in {os.path.relpath(cs, ROOT)} — the type moved to UnityEngine.InputSystem.UI in Input System 1.1+")
                problems += 1
            if re.search(r"\bInputSystemUIInputModule\b", src) and "Unity.InputSystem" not in refs:
                print(f"MISSING: {name} needs 'Unity.InputSystem' (fully-qualified InputSystemUIInputModule in {os.path.relpath(cs, ROOT)})")
                problems += 1
            if re.search(r"UnityEngine\.InputSystem\.", src) and "Unity.InputSystem" not in refs:
                print(f"MISSING: {name} needs 'Unity.InputSystem' (fully-qualified UnityEngine.InputSystem.* in {os.path.relpath(cs, ROOT)})")
                problems += 1
    if problems:
        print(f"\n{problems} missing reference(s) — the Unity build will fail with CS0234/CS0246.")
        return 1
    print("OK: every asmdef references all package assemblies its files use.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
