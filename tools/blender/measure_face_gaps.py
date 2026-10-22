"""Measures how far what is drawn on a head stands off it, and whether anything shows through it (18 October
2026, after the owner saw the faces "detached from the head, like a sheet hanging in the air"). Pictures and
numbers only: nothing here goes into the game.

Run it without the interface of Blender (5.2), one Blender at a time:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/measure_face_gaps.py -- --genesis 75,1287,23 [--into FOLDER]

With --into it reads FOLDER/<number>/ChogBody.blend (make_chog_traits.py, make_chog_traits_many.py); without,
it builds each Genesis. For every triangle drawn on the head (the cream face, its dark line, the cheeks and the
nose, the teeth of hair, and every flat triangle of an Eyes or Mouth part) it takes fifteen points and casts a
ray from each, along the way out of the middle of the head:

- inwards, to the faceted head (the egg of the head as built, its points on the egg, its faces inside it): how
  far the point stands off the head ("gap"), its mean and its largest;
- outwards: whether the head is above the point ("head pokes": the head shows through it), and for what lies
  on the face, whether the cream face is above it ("face pokes": the face shows through it);
- inwards to the cream face: how far an eye or a mouth stands off the face.

A line per part and material, "GAPS #<number> <part>:<material> ...". The dark line's skirt goes into the head
by design: its pokes are expected. Parts with volume (glasses, beams, the stream of Rainbow Puke) go into the
head where they start: their pokes are not drawn things showing through.
"""

import argparse
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

import make_chog_traits as traits  # noqa: E402


def main():
    parser = argparse.ArgumentParser(description="Measures how far what is drawn on a head stands off it.")
    parser.add_argument("--genesis", required=True, help="numbers shown in the names, separated by commas")
    parser.add_argument("--into", default=None, help="a folder of built Genesis to read instead of building")
    parser.add_argument("--head-set", action="append", default=[], metavar="NAME=VALUE", help="a number of the head put in place "
                        "of its own, to try (22 October 2026), for instance FACE_LIFT=0.0065")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    numbers = traits.head_numbers(traits.numbers_given(arguments.head_set) or None)
    middle = Vector((0.0, 0.0, numbers.HEAD_MIDDLE))
    half = Vector((numbers.HEAD_HALF_WIDTH, numbers.HEAD_HALF_DEPTH, numbers.HEAD_HALF_HEIGHT))
    untilt = Matrix.Rotation(math.radians(numbers.HEAD_PITCH_DEG), 3, "X")

    def before_turn(point):
        return middle + untilt @ (point - middle)

    def off_egg(point):
        """How far a point (before the head is turned up) is off the egg of the head, along the way out of
        its middle: negative inside."""
        offset = point - middle
        reach = math.sqrt(sum((offset[index] / half[index]) ** 2 for index in range(3)))
        return offset.length - offset.length / reach if reach else -1.0

    for number in [word.strip() for word in arguments.genesis.split(",") if word.strip()]:
        if arguments.into:
            bpy.ops.wm.open_mainfile(filepath=os.path.join(arguments.into, number, "ChogBody.blend"))
        else:
            traits.build(traits.traits_of(number), None, traits.numbers_given(arguments.head_set) or None)
        head = bpy.data.objects["Head"]
        head_faces, drawn = [], []
        for thing in [head] + [child for child in bpy.data.objects if child.parent == head and child.type == "MESH"]:
            if thing is not head and not thing.name.startswith(("Eyes", "Mouth")):
                continue
            names = [slot.material.name if slot.material else "" for slot in thing.material_slots]
            for polygon in thing.data.polygons:
                corners = [thing.data.vertices[index].co.copy() for index in polygon.vertices]
                material = names[polygon.material_index]
                lifts = [off_egg(before_turn(corner)) for corner in corners]
                centre = sum(corners, Vector()) / 3.0
                out = (centre - middle).normalized()
                if thing is head and all(abs(lift) < 2e-4 for lift in lifts):
                    head_faces.append(corners)
                elif material == "ChogOutline":
                    if thing is head and all(-0.02 < lift < 0.012 for lift in lifts) and polygon.normal.dot(out) > 0.0:
                        drawn.append(("Head:" + material, corners))
                elif all(-0.0005 < lift < 0.06 for lift in lifts) and (thing is head or abs(polygon.normal.dot(out)) > 0.6):
                    drawn.append((thing.name.split("_")[0] + "_" + thing.name.split("_")[1] + ":" + material.split("_")[-1]
                                  if thing is not head else "Head:" + material, corners))

        def tree_of(triangles):
            points, faces = [], []
            for corners in triangles:
                faces.append(tuple(range(len(points), len(points) + 3)))
                points.extend(corners)
            return BVHTree.FromPolygons(points, faces) if faces else None

        head_tree = tree_of(head_faces)
        face_tree = tree_of([corners for label, corners in drawn if label == "Head:ChogFace"])
        found = {}
        for label, corners in drawn:
            on_face = label not in ("Head:ChogFace", "Head:ChogOutline")
            for i in range(5):
                for j in range(5 - i):
                    point = (corners[0] * i + corners[1] * j + corners[2] * (4 - i - j)) / 4.0
                    out = (point - middle).normalized()
                    seen = found.setdefault(label, {"n": 0, "gap": 0.0, "sum": 0.0, "head": 0, "face": 0, "off_face": 0.0})
                    seen["n"] += 1
                    hit = head_tree.ray_cast(point + out * 1e-6, -out, 0.2)
                    if hit[0] is not None:
                        seen["gap"] = max(seen["gap"], hit[3])
                        seen["sum"] += hit[3]
                    if head_tree.ray_cast(point, out, 0.05)[0] is not None:
                        seen["head"] += 1
                        if label == "Head:ChogFace" and seen["head"] <= 6:
                            print(f"GAPS #{number} head pokes the face at {tuple(round(c, 3) for c in point)} (head axes before turning up)")
                    if face_tree is not None and on_face:
                        if face_tree.ray_cast(point, out, 0.05)[0] is not None:
                            seen["face"] += 1
                        below = face_tree.ray_cast(point + out * 1e-6, -out, 0.2)
                        if below[0] is not None:
                            seen["off_face"] = max(seen["off_face"], below[3])
        for label, seen in sorted(found.items()):
            print(f"GAPS #{number} {label:30s} points {seen['n']:4d}  off the head: mean {seen['sum'] / max(1, seen['n']):.4f} "
                  f"largest {seen['gap']:.4f}  head pokes {seen['head']:3d}  off the face: largest {seen['off_face']:.4f}  "
                  f"face pokes {seen['face']}", flush=True)


if __name__ == "__main__":
    main()
