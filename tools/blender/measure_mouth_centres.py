"""Where each mouth value is drawn across the face, for each Side (22 October 2026, the owner: "some mouths are offset, I do not
know why"). Run it without the interface of Blender (5.2), one Blender at a time, under the heavy-job lock:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/measure_mouth_centres.py

For each value of the table of mouths of each Side it builds a bare Genesis of the base Origin wearing only that mouth, then prints the
middle and the width of the drawn part across the face and its height, in the axes of the head (x to the left of the character, 0 the
middle line of the face where the nose is). A mouth that is not centred on the face leaves its middle away from 0.
"""

import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import make_chog_traits as traits  # noqa: E402


def main():
    numbers = traits.head_numbers()
    middle = Vector((0.0, 0.0, numbers.HEAD_MIDDLE))
    untilt = Matrix.Rotation(math.radians(numbers.HEAD_PITCH_DEG), 3, "X")
    values = []
    for side in ("Right", "Left"):
        table = dict(traits.TRAIT_PARTS["Mouth"][1])
        table.update(traits.OF_SIDE.get("Mouth", {}).get(side, {}))
        for value in sorted(table):
            values.append((side, value))
    for side, value in values:
        try:
            traits.build({"Mouth": value, "Side": side, "Base": "Origin", "number": "0"})
        except SystemExit as stop:
            print(f"MOUTH {side:5s} {value:16s} FAILED {stop}")
            continue
        head = bpy.data.objects["Head"]
        found = [thing for thing in bpy.data.objects if thing.parent == head and thing.name.startswith("Mouth")]
        points = []
        for thing in found:
            for vertex in thing.data.vertices:
                world = thing.matrix_world @ vertex.co
                local = head.matrix_world.inverted() @ world
                points.append(middle + untilt @ (local - middle))
        if not points:
            print(f"MOUTH {side:5s} {value:16s} no part")
            continue
        xs = [point.x for point in points]
        zs = [point.z - numbers.HEAD_MIDDLE for point in points]
        print(f"MOUTH {side:5s} {value:16s} middle x {0.5 * (min(xs) + max(xs)):+.3f}  width {max(xs) - min(xs):.3f}  "
              f"height from {min(zs):+.3f} to {max(zs):+.3f}")


if __name__ == "__main__":
    main()
