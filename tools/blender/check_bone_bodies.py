"""Checks that the ribs of a skeleton are built only under the bodies of BONES_SHOW (22 October 2026, the owner: "skeleton lines on the body even when
they wear a T-shirt"). Run it without the interface of Blender (5.2), under the heavy-job lock:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 --python tools/blender/check_bone_bodies.py

For every Skull and Radioactive Genesis it builds the hero and counts the faces of its Body part that are drawn in the material of the ribs (the dark of a skull,
the deep tone of a radioactive skin), then prints, for each value of "Body", how many Genesis have ribs and how many have none, and ends with an error when a
body outside BONES_SHOW has ribs or a body of BONES_SHOW has none.
"""

import os
import sys
from collections import defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import bpy  # noqa: E402

import chog_trait_bases as bases  # noqa: E402
import make_chog_traits as traits  # noqa: E402


def main():
    found = defaultdict(lambda: [0, 0])
    wrong = []
    for line in open(traits.METADATA, encoding="utf-8"):
        import json
        record = json.loads(line)
        genesis = {str(item["trait_type"]): str(item["value"]) for item in json.loads(record["raw_json"]).get("attributes", [])}
        genesis["number"] = str(record["metadata_id"])
        if genesis.get("Base") not in ("Skull", "Radioactive"):
            continue
        traits.build(genesis)
        body = bpy.data.objects["Body"]
        names = [slot.material.name if slot.material else "" for slot in body.material_slots]
        material = "ChogEye" if genesis["Base"] == "Skull" else "ChogFace"
        ribs = sum(1 for polygon in body.data.polygons if names[polygon.material_index] == material)
        value = genesis.get("Body", "None")
        found[(genesis["Base"], value)][1 if ribs else 0] += 1
        should = genesis.get("Body") in bases.BONES_SHOW or genesis.get("Body") is None
        if bool(ribs) != should:
            wrong.append(f"#{genesis['number']} {genesis['Base']} Body={value}: ribs {ribs} faces, should be {'present' if should else 'absent'}")
    for (base, value), (without, with_ribs) in sorted(found.items()):
        print(f"BONES {base:11s} {value:22s} without ribs {without:2d}  with ribs {with_ribs:2d}")
    print(f"BONES wrong: {len(wrong)}")
    for item in wrong:
        print("BONES   " + item)
    if wrong:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
