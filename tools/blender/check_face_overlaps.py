"""Finds where two surfaces of the head lie too close along the line of sight, from the angles the owner looks at it
(22 October 2026, the "little white bits" and "textures mixing" seen at the hairline and the edges of the face).

Run it without the interface of Blender (5.2), one Blender at a time, under the heavy-job lock:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/check_face_overlaps.py -- --genesis 75 [--eps 0.0015] [--grid 260] [--image FILE]

It builds the Genesis (make_chog_traits.make_one), then for each view (the stage of the home screen, three quarters from
either side, the front, the game angle) casts a grid of parallel rays at the mesh of the head and follows each ray through
every surface it meets. Two consecutive hits of different materials less than --eps apart (the depth step of a 16-bit depth
buffer at the distance of the stage is 0.0008, of a 24-bit one 0.000003) are a place where the picture can flicker or
show the wrong material: they are counted by pair of materials and by view, and --image writes the map of the first view.
"""

import argparse
import math
import os
import sys
import tempfile
from collections import Counter

import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import make_chog_traits as traits  # noqa: E402

# (name, turn from the front in degrees, elevation in degrees)
VIEWS = (("stage", 42.0, 10.0), ("stage mirrored", -42.0, 10.0), ("front", 0.0, 4.0), ("game", 20.0, 73.0),
         ("side", 90.0, 4.0))


def material_name(object_, index):
    slots = object_.data.materials
    return slots[index].name if 0 <= index < len(slots) and slots[index] else "?"


def main():
    parser = argparse.ArgumentParser(description="Where two surfaces of the head lie too close along the line of sight.")
    parser.add_argument("--genesis", default="75")
    parser.add_argument("--eps", type=float, default=0.0015)
    parser.add_argument("--grid", type=int, default=260)
    parser.add_argument("--image", default=None)
    parser.add_argument("--decals", action="store_true", help="only measure how far the drawn things stand over the face")
    parser.add_argument("--specks", action="store_true", help="only look for the face in front of a drawn thing (speck_check)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    folder = tempfile.mkdtemp(prefix="chog_overlaps_")
    traits.make_one(arguments.genesis, folder, None, None, None)
    if arguments.decals:
        bpy.context.view_layer.update()
        decals_over_face()
        return
    if arguments.specks:
        bpy.context.view_layer.update()
        speck_check(arguments.grid, arguments.eps)
        return
    head = bpy.data.objects["Head"]
    bpy.context.view_layer.update()
    world = head.matrix_world
    inverse = world.inverted()
    centre = world @ Vector((0.0, 0.0, 0.215))
    result = {}
    for name, turn_deg, elevation_deg in VIEWS:
        turn, elevation = math.radians(turn_deg), math.radians(elevation_deg)
        away = Vector((math.sin(turn) * math.cos(elevation), -math.cos(turn) * math.cos(elevation), math.sin(elevation)))
        way = -away
        right = way.cross(Vector((0.0, 0.0, 1.0))).normalized()
        up = right.cross(way).normalized()
        count = arguments.grid
        half = 0.62
        pairs, pixels, samples, ordered = Counter(), [], [], Counter()
        for row in range(count):
            for column in range(count):
                u = (column + 0.5) / count * 2.0 * half - half
                v = (row + 0.5) / count * 2.0 * half - half
                start = centre + away * 3.0 + right * u + up * v
                origin = inverse @ start
                direction = (inverse.to_3x3() @ way).normalized()
                last = None
                travelled = 0.0
                for _ in range(8):
                    hit, location, normal, face = head.ray_cast(origin, direction)
                    if not hit:
                        break
                    depth = (location - origin).length
                    kind = material_name(head, head.data.polygons[face].material_index)
                    if last is not None and depth + travelled - last[1] < arguments.eps and kind != last[0]:
                        pairs[tuple(sorted((kind, last[0])))] += 1
                        ordered[(last[0], kind)] += 1
                        pixels.append((column, row, kind, last[0]))
                        if {kind, last[0]} == {'ChogFace', 'ChogSkin'} and len(samples) < 12:
                            samples.append((tuple(round(c, 3) for c in location), last[0], kind, round(depth + travelled - last[1], 5)))
                    last = (kind, depth + travelled)
                    step = depth + 1e-6
                    origin = location + direction * 1e-6
                    travelled += step
        result[name] = (pairs, pixels, count)
        for sample in samples:
            print(f"CHOG_OVERLAPS    face/skin at {sample[0]} (head axes), first {sample[1]} then {sample[2]}, gap {sample[3]}")
        print(f"CHOG_OVERLAPS #{arguments.genesis} view {name}: {sum(pairs.values())} close pairs of {count * count} rays")
        for pair, total in pairs.most_common(8):
            print(f"CHOG_OVERLAPS    {pair[0]} / {pair[1]}: {total}")
        # In front first, then behind it within eps: a face (or a decal) in front of the layer it should lie over is a speck.
        for (first, second), total in ordered.most_common(8):
            print(f"CHOG_OVERLAPS      nearest {first}, then {second}: {total}")
    if arguments.image:
        from array import array
        pairs, pixels, count = result[VIEWS[0][0]]
        buffer = array("f", [0.0, 0.0, 0.0, 1.0] * (count * count))
        image = bpy.data.images.new("overlaps", count, count, alpha=False)
        colours = {("ChogFace", "ChogSkin"): (1.0, 0.9, 0.0), ("ChogOutline", "ChogSkin"): (0.7, 0.1, 0.1),
                   ("ChogBlush", "ChogFace"): (0.2, 0.4, 1.0)}
        for column, row, first, second in pixels:
            at = (row * count + column) * 4
            buffer[at:at + 4] = array("f", [*colours.get(tuple(sorted((first, second))), (1.0, 1.0, 1.0)), 1.0])
        image.pixels = list(buffer)
        image.filepath_raw = arguments.image
        image.file_format = "PNG"
        image.save()
        print(f"CHOG_OVERLAPS wrote {arguments.image}")



def speck_check(grid=700, eps=0.012, turn_deg=42.0, elevation_deg=10.0):
    """The specks the owner saw on the face (22 October 2026): a pixel where the first surface met is the cream face (or the
    skin) and, less than eps behind it, a drawn thing (a cheek, an eye, a mouth) is under it. That is the face (or a
    flat triangle of the head) in front of what should lie over it. Casts parallel rays at every object of the scene from
    the angle of the hero stage, in a window round the head. Prints the count per pair of materials."""
    scene = bpy.context.scene
    graph = bpy.context.evaluated_depsgraph_get()
    head = bpy.data.objects["Head"]
    centre = head.matrix_world @ Vector((0.0, 0.0, 0.215))
    turn, elevation = math.radians(turn_deg), math.radians(elevation_deg)
    away = Vector((math.sin(turn) * math.cos(elevation), -math.cos(turn) * math.cos(elevation), math.sin(elevation)))
    way = -away
    right = way.cross(Vector((0.0, 0.0, 1.0))).normalized()
    up = right.cross(way).normalized()
    half = 0.40
    behind, samples = Counter(), []
    for row in range(grid):
        for column in range(grid):
            u = (column + 0.5) / grid * 2.0 * half - half
            v = (row + 0.5) / grid * 2.0 * half - half
            origin = centre + away * 3.0 + right * u + up * v
            first = None
            travelled = 0.0
            for _ in range(5):
                hit, location, normal, face, thing, _matrix = scene.ray_cast(graph, origin, way)
                if not hit:
                    break
                kind = material_name(thing, thing.data.polygons[face].material_index)
                depth = (location - origin).length
                if first is not None and kind != first[0] and depth + travelled - first[1] < eps and \
                        first[0] in ("ChogFace", "ChogSkin") and kind not in ("ChogFace", "ChogSkin", "ChogOutline"):
                    behind[(first[0], kind)] += 1
                    if len(samples) < 8:
                        samples.append((round(u, 3), round(v, 3), first[0], kind, round(depth + travelled - first[1], 5)))
                if first is None:
                    first = (kind, depth)
                origin = location + way * 1e-6
                travelled += depth + 1e-6
    print(f"CHOG_SPECKS {grid * grid} rays, eps {eps}")
    for (front, under), total in behind.most_common(12):
        print(f"CHOG_SPECKS    {front} in front of {under}: {total}")
    for sample in samples:
        print(f"CHOG_SPECKS    at {sample[:2]}: {sample[2]} over {sample[3]} by {sample[4]}")



def decals_over_face(density=24):
    """How far each drawn thing stands over the cream face (22 October 2026: the specks inside the cheeks). Every triangle
    of the cheeks, the nose and of the Eyes and Mouth parts is sampled on a fine grid ("density" steps a side); from each
    sample a ray is cast inwards along the way out of the middle of the head to the cream face. The height of the sample over
    the face, the smallest of them and how many samples lie under the face (where the face shows through) are printed per
    material. All in the axes of the head, which is turned the same way for every surface."""
    from mathutils.bvhtree import BVHTree
    head = bpy.data.objects["Head"]
    middle = head.matrix_world @ Vector((0.0, 0.0, 0.215))
    face_points, face_polygons = [], []
    parts = [head] + [child for child in bpy.data.objects if child.parent == head and child.type == "MESH"]
    for thing in parts:
        names = [slot.material.name if slot.material else "" for slot in thing.material_slots]
        if thing is head:
            for polygon in thing.data.polygons:
                if names[polygon.material_index] == "ChogFace":
                    face_polygons.append(tuple(range(len(face_points), len(face_points) + 3)))
                    face_points.extend(thing.matrix_world @ thing.data.vertices[index].co for index in polygon.vertices)
    tree = BVHTree.FromPolygons(face_points, face_polygons)
    found = {}
    for thing in parts:
        names = [slot.material.name if slot.material else "" for slot in thing.material_slots]
        for polygon in thing.data.polygons:
            material = names[polygon.material_index]
            if thing is head and material not in ("ChogBlush", "ChogSkin"):
                continue
            corners = [thing.matrix_world @ thing.data.vertices[index].co for index in polygon.vertices]
            label = (thing.name.split("_")[1] if thing is not head else "Head") + ":" + material.split("_")[-1]
            seen = found.setdefault(label, {"n": 0, "low": 9.0, "under": 0, "sum": 0.0})
            for i in range(density + 1):
                for j in range(density + 1 - i):
                    point = (corners[0] * i + corners[1] * j + corners[2] * (density - i - j)) / density
                    out = (point - middle).normalized()
                    hit = tree.ray_cast(point + out * 0.05, -out, 0.2)
                    if hit[0] is None:
                        continue
                    over = hit[3] - 0.05
                    seen["n"] += 1
                    seen["sum"] += over
                    seen["low"] = min(seen["low"], over)
                    seen["under"] += over < 0.0
    for label, seen in sorted(found.items()):
        if seen["n"]:
            print(f"CHOG_DECALS {label:28s} samples {seen['n']:6d}  mean over the face {seen['sum'] / seen['n']:+.4f}  "
                  f"lowest {seen['low']:+.5f}  under the face {seen['under']}")


if __name__ == "__main__":
    main()
