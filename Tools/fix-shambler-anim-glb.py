"""Fix the SummerEngine shambler animation-only GLBs.

The extractor that built these files wrote a wrong accessor index for the
skin's inverseBindMatrices: it points at accessor 7 (a VEC3 animation
accessor, 32 keyframes) instead of accessor 0 (the MAT4 accessor with
24 matrices, one per joint). Blender's glTF importer then crashes with
IndexError in pick_bind_pose/convert_matrix.

Fix: rewrite the JSON chunk with inverseBindMatrices: 0. The change is a
single character ("7" -> "0"), so the JSON chunk length — and therefore the
whole file layout — is unchanged; the BIN chunk is untouched.
"""
import sys

for path in sys.argv[1:]:
    with open(path, "rb") as f:
        data = bytearray(f.read())
    assert data[:4] == b"glTF", path
    jlen = int.from_bytes(data[12:16], "little")
    js = data[20:20 + jlen]
    old = b'"inverseBindMatrices":7'
    assert old in js, f"pattern not found in {path}"
    js = js.replace(old, b'"inverseBindMatrices":0')
    assert len(js) == jlen, "JSON length changed!"
    data[20:20 + jlen] = js
    with open(path, "wb") as f:
        f.write(data)
    print(f"fixed: {path} (inverseBindMatrices 7 -> 0)")
