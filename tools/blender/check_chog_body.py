"""Measures the exported Chog body against what the game expects of it (game/README.md, "The body of
a hero, and where the Chog body goes").

Run it without the interface of Blender (5.2), after make_chog_body.py:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/check_chog_body.py -- [--fbx .artifacts/chog-body/ChogBody.fbx]

It reads ChogBody.fbx twice and writes nothing:

1. It opens the file in an empty scene of Blender and prints the parts, their origins, the height of
   the whole and the triangles of each part.
2. It reads the numbers written in the file itself, before any program interprets them: the axes the
   file declares, the place, turn and scale of each part, where the toes and the left arm are. From
   those it says what Unity is expected to show. That is a deduction from the file and from the rule
   Unity follows for every FBX (it mirrors X). Only an import in Unity can confirm it.

Every line starts with CHOG_CHECK. The last line is "CHOG_CHECK passed" or the list of what failed, and
the script then ends with an error.
"""

import argparse
import math
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_FBX = os.path.normpath(os.path.join(HERE, "..", "..", ".artifacts", "chog-body", "ChogBody.fbx"))

PART_NAMES = ("Body", "Head", "armLeft", "armRight")
TRIANGLE_LIMIT = 3000
HEIGHT_RANGE = (1.1, 1.3)    # "about 1.2 units tall"
CLOSE = 0.002                # two lengths nearer than this are the same

failures = []


def say(text):
    print(f"CHOG_CHECK {text}")


def expect(condition, text):
    if not condition:
        failures.append(text)
        say(f"FAILED: {text}")


def numbers(values):
    # Adding zero to a rounded number writes "-0.000" as "+0.000".
    return " ".join(f"{round(value, 3) + 0.0:+.3f}" for value in values)


# ---------------------------------------------------------------------------------------------
# 1. The file opened in Blender.
# ---------------------------------------------------------------------------------------------

def open_in_blender(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.wm.fbx_import(filepath=path)
        return "wm.fbx_import"
    except (AttributeError, RuntimeError):
        bpy.ops.import_scene.fbx(filepath=path)
        return "import_scene.fbx"


def check_in_blender(path):
    how = open_in_blender(path)
    bpy.context.view_layer.update()
    things = list(bpy.context.scene.objects)
    say(f"opened in Blender {bpy.app.version_string} with {how}: {len(things)} objects")

    by_name = {thing.name: thing for thing in things}
    meshes = [thing for thing in things if thing.type == "MESH"]
    expect(sorted(thing.name for thing in meshes) == sorted(PART_NAMES),
           f"the parts drawn are {sorted(thing.name for thing in meshes)}, expected {sorted(PART_NAMES)}")
    expect(not [thing for thing in things if thing.type == "ARMATURE"], "the file has a skeleton")

    total = 0
    low, high = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    materials = set()
    for name in PART_NAMES:
        part = by_name.get(name)
        if part is None or part.type != "MESH":
            continue
        triangles = sum(len(face.vertices) - 2 for face in part.data.polygons)
        total += triangles
        part_low = 1e9
        for point in part.data.vertices:
            world = part.matrix_world @ point.co
            low = Vector(map(min, low, world))
            high = Vector(map(max, high, world))
            part_low = min(part_low, world.z)
        materials.update(slot.material.name for slot in part.material_slots if slot.material)
        place = part.matrix_world.translation
        turn = part.matrix_world.to_euler()
        scale = part.matrix_world.to_scale()
        say(f"part {name:9s} under {part.parent.name if part.parent else 'nothing':13s} "
            f"triangles {triangles:5d}  origin (Blender x y z) {numbers(place)}  "
            f"turn {numbers(math.degrees(angle) for angle in turn)}  scale {numbers(scale)}")
        expect(all(abs(value - 1.0) < 1e-4 for value in scale), f"{name} is not at scale 1")

    # The file is written with +Y up, its points included, and Blender has +Z up: it stands each part
    # back up by a turn of 90 degrees about X. That turn is Blender's, not the file's (see part 2).
    say("a turn of +90 about X above is how Blender stands up a file written with +Y up")
    say(f"triangles {total} in all (limit {TRIANGLE_LIMIT})")
    say(f"box: {high.x - low.x:.3f} wide, {high.y - low.y:.3f} deep, {high.z - low.z:.3f} tall; "
        f"lowest point {low.z:+.4f}, highest {high.z:+.4f}")
    say(f"materials: {sorted(materials)}")
    expect(total < TRIANGLE_LIMIT, f"{total} triangles, the limit is {TRIANGLE_LIMIT}")
    expect(HEIGHT_RANGE[0] <= high.z - low.z <= HEIGHT_RANGE[1], f"the model is {high.z - low.z:.3f} tall")
    expect(abs(low.z) < CLOSE, f"the feet are not on the ground: lowest point {low.z:+.4f}")
    expect(len(materials) == 1, f"{len(materials)} materials, expected one")

    if all(name in by_name for name in PART_NAMES):
        body, head = by_name["Body"].matrix_world.translation, by_name["Head"].matrix_world.translation
        left, right = by_name["armLeft"].matrix_world.translation, by_name["armRight"].matrix_world.translation
        expect(body.length < CLOSE, "the origin of Body is not at the feet, on the origin")
        expect(abs(head.x) < CLOSE and abs(head.y) < CLOSE and head.z > left.z,
               "the origin of Head is not above the shoulders, in the middle")
        expect(abs(left.x + right.x) < CLOSE and abs(left.z - right.z) < CLOSE and abs(left.y - right.y) < CLOSE,
               "the shoulders are not at the same height on each side")
        expect(left.x > 0.0 > right.x, "armLeft is not on the left (+X here, the Chog looking along -Y)")
        # An arm hangs: all of it is lower than a little above its origin.
        for name in ("armLeft", "armRight"):
            arm = by_name[name]
            top = max((arm.matrix_world @ point.co).z for point in arm.data.vertices)
            bottom = min((arm.matrix_world @ point.co).z for point in arm.data.vertices)
            origin = arm.matrix_world.translation.z
            say(f"{name}: from {bottom - origin:+.3f} to {top - origin:+.3f} around its origin, in height")
            expect(origin - bottom > 2.0 * (top - origin), f"{name} does not hang down from its origin")


# ---------------------------------------------------------------------------------------------
# 2. The numbers written in the file.
# ---------------------------------------------------------------------------------------------

def children(element, name):
    return [child for child in element.elems if child.id == name]


def properties(element):
    found = {}
    for block in children(element, b"Properties70"):
        for line in children(block, b"P"):
            found[line.props[0].decode()] = list(line.props[4:])
    return found


def short_name(element):
    return element.props[1].split(b"\x00\x01")[0].decode()


def check_in_file(path):
    try:
        from io_scene_fbx import parse_fbx
    except ImportError as error:
        say(f"the numbers of the file are not read: {error}")
        failures.append("the reader of FBX files of Blender is missing")
        return

    tree, version = parse_fbx.parse(path)
    top = {element.id: element for element in tree.elems}
    settings = properties(top[b"GlobalSettings"])
    axes = "XYZ"

    def axis(name):
        return ("+" if settings[name + "Sign"][0] > 0 else "-") + axes[settings[name][0]]

    unit = settings["UnitScaleFactor"][0]
    say(f"file: FBX {version}, up {axis('UpAxis')}, front {axis('FrontAxis')}, side {axis('CoordAxis')}, "
        f"one unit is {unit:g} cm")
    expect(axis("UpAxis") == "+Y" and axis("FrontAxis") == "+Z" and axis("CoordAxis") == "+X",
           "the file does not declare +Y up, +Z front, +X side")
    # Unity turns the lengths of a file into metres: what is written is multiplied by this.
    to_metres = unit / 100.0

    models, geometries = {}, {}
    for element in top[b"Objects"].elems:
        if element.id == b"Model":
            models[element.props[0]] = element
        elif element.id == b"Geometry":
            geometries[element.props[0]] = element
    expect(not [1 for element in top[b"Objects"].elems if element.id == b"Deformer"], "the file has a skin")

    parent_of, geometry_of = {}, {}
    for line in children(top[b"Connections"], b"C"):
        if line.props[0] != b"OO":
            continue
        child, parent = line.props[1], line.props[2]
        if child in models and parent in models:
            parent_of[child] = parent
        elif child in geometries and parent in models:
            geometry_of[parent] = child

    places = {}
    for key, model in models.items():
        name, values = short_name(model), properties(model)
        place = Vector(values.get("Lcl Translation", (0.0, 0.0, 0.0))) * to_metres
        turn = values.get("Lcl Rotation", [0.0, 0.0, 0.0])
        scale = values.get("Lcl Scaling", [1.0, 1.0, 1.0])
        extra = [label for label in ("PreRotation", "PostRotation", "GeometricTranslation", "GeometricRotation",
                                     "GeometricScaling") if any(abs(value) > 1e-6 for value in values.get(label, []))]
        under = short_name(models[parent_of[key]]) if key in parent_of else "nothing"
        say(f"file: {name:12s} ({model.props[2].decode()}) under {under:12s} place {numbers(place)}  "
            f"turn {numbers(turn)}  scale {numbers(scale)}" + (f"  also {extra}" if extra else ""))
        expect(all(abs(value) < 1e-4 for value in turn), f"{name} is written turned by {numbers(turn)}")
        expect(all(abs(value - 1.0) < 1e-4 for value in scale), f"{name} is written at scale {numbers(scale)}")
        expect(not extra, f"{name} carries {extra}")
        # Every part is right under the root, which stays where it is: its place is its place in the model.
        places[name] = place

    lows, highs = {}, {}
    for key, model in models.items():
        if key not in geometry_of:
            continue
        name = short_name(model)
        flat = children(geometries[geometry_of[key]], b"Vertices")[0].props[0]
        points = [places[name] + Vector(flat[index:index + 3]) * to_metres for index in range(0, len(flat), 3)]
        lows[name] = Vector(min(point[index] for point in points) for index in range(3))
        highs[name] = Vector(max(point[index] for point in points) for index in range(3))

    if not all(name in lows for name in PART_NAMES):
        expect(False, f"the file draws {sorted(lows)}, expected {sorted(PART_NAMES)}")
        return

    low = Vector(min(lows[name][index] for name in PART_NAMES) for index in range(3))
    high = Vector(max(highs[name][index] for name in PART_NAMES) for index in range(3))
    say(f"file: the model spans x {low.x:+.3f} to {high.x:+.3f}, y {low.y:+.3f} to {high.y:+.3f}, "
        f"z {low.z:+.3f} to {high.z:+.3f} (metres)")

    # Unity mirrors X when it reads an FBX and keeps Y and Z.
    def in_unity(point):
        return Vector((-point.x, point.y, point.z))

    say("expected in Unity (deduced from the file, not seen in Unity):")
    for name in PART_NAMES:
        say(f"  {name:9s} origin (x y z) {numbers(in_unity(places[name]))}, turn 0 0 0, scale 1 1 1")
    say(f"  height {high.y - low.y:.3f}, lowest point y {low.y:+.4f}")
    say(f"  the Body goes from z {lows['Body'].z:+.3f} to z {highs['Body'].z:+.3f}")
    say(f"  armLeft is at x {in_unity(places['armLeft']).x:+.3f}, armRight at x {in_unity(places['armRight']).x:+.3f}")

    expect(abs(low.y) < CLOSE, f"in the file the lowest point is at y {low.y:+.4f}, not on the ground")
    expect(HEIGHT_RANGE[0] <= high.y - low.y <= HEIGHT_RANGE[1], f"in the file the model is {high.y - low.y:.3f} tall")
    expect(in_unity(places["armLeft"]).x < 0.0 < in_unity(places["armRight"]).x,
           "armLeft would not be at -X in Unity")
    expect(places["Head"].y > places["armLeft"].y > 0.0, "in the file the neck is not above the shoulders")
    expect(lows["armLeft"].y < places["armLeft"].y - 0.2, "in the file the arms do not hang along -Y")

    # Which way it looks: the feet are the only part of the Body near the ground, and they point ahead.
    body = geometries[geometry_of[next(key for key, model in models.items() if short_name(model) == "Body")]]
    flat = children(body, b"Vertices")[0].props[0]
    feet = [flat[index + 2] * to_metres for index in range(0, len(flat), 3) if flat[index + 1] * to_metres < 0.05]
    say(f"  the soles of the feet go from z {min(feet):+.3f} to z {max(feet):+.3f}: "
        f"{'the toes are at +Z, it looks along +Z' if max(feet) > -min(feet) else 'the toes are at -Z'}")
    expect(max(feet) > -min(feet) * 2.0, "the toes would not point along +Z in Unity")


def main():
    parser = argparse.ArgumentParser(description="Measures the exported Chog body.")
    parser.add_argument("--fbx", default=DEFAULT_FBX)
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    path = os.path.abspath(arguments.fbx)
    if not os.path.isfile(path):
        raise SystemExit(f"CHOG_CHECK no file at {path}: run make_chog_body.py first")
    say(f"{path} ({os.path.getsize(path)} bytes)")

    check_in_blender(path)
    check_in_file(path)

    if failures:
        raise SystemExit("CHOG_CHECK FAILED: " + "; ".join(failures))
    say("passed")


if __name__ == "__main__":
    main()
