"""Builds the body of a Chog hero from nothing and exports it for the game.

Run it without the interface of Blender (5.2):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/make_chog_body.py -- [--out .artifacts/chog-body]

It writes ChogBody.fbx (for Unity) and ChogBody.blend (to look at, and for the renders) into the
output folder. Nothing is read: the same numbers always give the same model. Look at the result with
render_chog_body.py and measure the exported file with check_chog_body.py.

The model is made of four rigid parts under one root, as game/Assets/Chog/Match/ChogBodyAnimator.cs
expects: "Body" (belly, legs, feet and the spikes of the back), "Head" (a neck stub: the game hides it
and shows the real picture of the Genesis there), "armLeft" and "armRight" (arm and hand). There is no
skeleton, no texture and one material: the game replaces its colour by the Skin colour of the Genesis.

Axes. Here the Chog looks along -Y, +Z is up and its LEFT hand is at +X (the usual way in Blender).
The export turns that into the axes of Unity: looking along +Z, +Y up, left hand at -X.
"""

import argparse
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

# ---------------------------------------------------------------------------------------------
# Proportions: change these, run the script again, then render_chog_body.py to look at the result.
# One unit is one unit of the game. The class models of the sample are about 1.2 units tall.
# The game looks at a hero from 73 degrees above the horizon and covers its top with the picture of
# its Genesis, so what tells a Chog from a capsule is what sticks out of its outline seen from above:
# the feet in front, a hand on each side, the spikes behind. They are large on purpose.
# ---------------------------------------------------------------------------------------------

# Neck: where the head goes. The origin of "Head" is at this height, in the middle of the body.
NECK_HEIGHT = 1.03
NECK_STUB_HEIGHT = 0.17      # the stub above the neck; the whole model is NECK_HEIGHT + this tall
NECK_WIDTH = 0.30

# The torso: one ring per line, from between the legs to the neck.
# height, width, depth, how far the ring is pushed forward of the line of the back
TORSO_RINGS = (
    (0.17, 0.00, 0.00, 0.000),   # closed between the legs
    (0.20, 0.38, 0.32, 0.005),
    (0.27, 0.63, 0.52, 0.015),
    (0.37, 0.81, 0.66, 0.030),
    (0.49, 0.87, 0.71, 0.040),   # the belly, the widest ring
    (0.61, 0.85, 0.69, 0.035),
    (0.72, 0.80, 0.64, 0.025),
    (0.81, 0.74, 0.58, 0.012),
    (0.89, 0.66, 0.51, 0.000),   # the shoulders start to round
    (0.95, 0.56, 0.44, 0.000),
    (1.00, 0.44, 0.37, 0.000),
    (NECK_HEIGHT, NECK_WIDTH + 0.02, NECK_WIDTH + 0.01, 0.000),
    (NECK_HEIGHT + 0.02, 0.00, 0.00, 0.000),   # closed under the neck stub, which the game hides
)

# Shoulders: the origin of each arm, the point an arm turns about. A ball of SHOULDER_RADIUS sits on
# that point and belongs to the arm: a ball looks the same however it is turned, so the arm never
# leaves a hole in the body. It must reach into the torso (see the "shoulder" line of the report).
SHOULDER_HEIGHT = 0.85
SHOULDER_HALF_WIDTH = 0.41   # from the middle of the body to one shoulder
SHOULDER_RADIUS = 0.125

# Arms and hands. An arm hangs from its shoulder, leaning away from the body by ARM_SPREAD_DEG.
# It is a sleeve that opens like a bell towards the hand, and the hand is a mitten.
ARM_LENGTH = 0.40            # shoulder to the middle of the hand
ARM_SPREAD_DEG = 24.0
ARM_FORWARD_DEG = 8.0        # the hands rest a little ahead of the shoulders
UPPER_ARM_RADIUS = 0.104
SLEEVE_END_RADIUS = 0.132    # the open end of the sleeve, at the wrist
HAND_SIZE = 0.33             # across the hand

# Legs and feet (part of "Body": they do not move on their own).
LEG_HALF_SPACING = 0.215     # from the middle of the body to one leg
LEG_RADIUS = 0.135
LEG_TOP = 0.34               # the legs end inside the belly
FOOT_LENGTH = 0.58
FOOT_WIDTH = 0.34
FOOT_HEIGHT = 0.21
FOOT_HEEL_BACK = 0.11        # how far the heel is behind the leg
FOOT_TOE_OUT_DEG = 17.0      # how far the toes point away from each other
# A foot from heel to toe: where along its length, width and height against the largest.
FOOT_RINGS = (
    (0.07, 0.62, 0.58),
    (0.24, 0.88, 0.80),
    (0.46, 1.00, 0.94),
    (0.68, 1.00, 1.00),
    (0.86, 0.80, 0.86),
    (0.96, 0.46, 0.58),
)

# Crest: the spikes of the hair, carried on down the back.
CREST_SPIKE_LENGTH = 0.40    # the largest spike
CREST_SPIKE_WIDTH = 0.33
CREST_SPIKE_THICKNESS = 0.23
CREST_SPIKE_BELLY = 0.62     # how wide a spike still is half way to its point, against its root
CREST_SPIKE_HOOK = 0.22      # how much the point curls up, against the length
# One line per spike: height on the back, turn to the side in degrees (0 is straight back, a
# value other than 0 gives one spike on each side), tilt up in degrees, size against the largest.
CREST_SPIKES = (
    (0.94, 0.0, 40.0, 0.80),   # nape
    (0.85, 34.0, 28.0, 0.78),  # behind the shoulders
    (0.71, 0.0, 16.0, 1.00),   # middle of the back
    (0.57, 40.0, 6.0, 0.85),   # flanks
    (0.43, 0.0, -6.0, 0.90),   # low on the back
)

# How finely the round shapes are cut. More sides: smoother outline, more triangles.
TORSO_SIDES = 24
LIMB_SIDES = 12
HAND_SIDES = 14
HAND_STACKS = 7
FOOT_SIDES = 14
SPIKE_SIDES = 4              # four makes a spike with a ridge along its top, lit on one side
NECK_SIDES = 14
TRIANGLE_LIMIT = 3000

# The colour in the file. The game replaces it; the renders use their own colours.
FILE_COLOUR = (0.36, 0.21, 0.69, 1.0)   # the purple of the "Origin" skin
MATERIAL_NAME = "ChogSkin"
ROOT_NAME = "ChogBodyRoot"

X = Vector((1.0, 0.0, 0.0))
Y = Vector((0.0, 1.0, 0.0))
Z = Vector((0.0, 0.0, 1.0))
NOWHERE = Vector((0.0, 0.0, 0.0))


def at(side, forward, up):
    """A point given the way one speaks of a body: to its left, ahead of it, above the ground."""
    return Vector((side, -forward, up))


def lerp(start, end, amount):
    return start + (end - start) * amount


# ---------------------------------------------------------------------------------------------
# Shapes. Every shape is a closed shell of rings of points joined in order, like a lathe:
# a ring is (centre, first half axis, second half axis); two zero half axes make a single point.
# The first half axis crossed with the second must point the way the rings advance, so that
# the faces look outwards.
# ---------------------------------------------------------------------------------------------

def add_shell(mesh, rings, sides, smooth=True):
    """Adds one closed shell to "mesh" and returns its points. An end ring that is not a point is
    closed by a flat face."""
    loops = []
    for centre, half_u, half_v in rings:
        if half_u.length < 1e-9 and half_v.length < 1e-9:
            loops.append([mesh.verts.new(centre)])
            continue
        loop = []
        for index in range(sides):
            angle = 2.0 * math.pi * index / sides
            loop.append(mesh.verts.new(centre + half_u * math.cos(angle) + half_v * math.sin(angle)))
        loops.append(loop)

    for lower, upper in zip(loops, loops[1:]):
        for index in range(sides):
            after = (index + 1) % sides
            if len(lower) == 1 and len(upper) == 1:
                continue
            if len(lower) == 1:
                corners = (lower[0], upper[after], upper[index])
            elif len(upper) == 1:
                corners = (lower[index], lower[after], upper[0])
            else:
                corners = (lower[index], lower[after], upper[after], upper[index])
            mesh.faces.new(corners).smooth = smooth

    if len(loops[0]) > 1:
        mesh.faces.new(list(reversed(loops[0]))).smooth = False
    if len(loops[-1]) > 1:
        mesh.faces.new(loops[-1]).smooth = False
    return [point for loop in loops for point in loop]


def frame_across(axis, wide_hint):
    """Two unit vectors across "axis": the first as close to "wide_hint" as it can be."""
    axis = axis.normalized()
    first = (wide_hint - axis * wide_hint.dot(axis)).normalized()
    second = axis.cross(first)
    return first, second


def ball_rings(centre, axis, wide_hint, along, wide, thick, stacks):
    """An egg: "along" its axis, "wide" towards the hint, "thick" the third way (all half sizes)."""
    first, second = frame_across(axis, wide_hint)
    axis = axis.normalized()
    rings = []
    for index in range(stacks + 1):
        angle = math.pi * index / stacks
        swell = 0.0 if index in (0, stacks) else math.sin(angle)
        rings.append((centre - axis * along * math.cos(angle), first * wide * swell, second * thick * swell))
    return rings


def tube_rings(start, axis, wide_hint, stations):
    """A round tube along "axis" from "start": "stations" is a list of (distance, radius)."""
    first, second = frame_across(axis, wide_hint)
    axis = axis.normalized()
    return [(start + axis * distance, first * radius, second * radius) for distance, radius in stations]


# ---------------------------------------------------------------------------------------------
# The torso: an egg, widest at the belly. Its section at any height is an oval.
# ---------------------------------------------------------------------------------------------

def torso_section(height):
    """Half width, half depth and forward shift of the torso at a height above the ground."""
    rings = TORSO_RINGS
    if height <= rings[0][0]:
        return rings[0][1] * 0.5, rings[0][2] * 0.5, rings[0][3]
    for lower, upper in zip(rings, rings[1:]):
        if height <= upper[0]:
            amount = (height - lower[0]) / (upper[0] - lower[0])
            return (lerp(lower[1], upper[1], amount) * 0.5, lerp(lower[2], upper[2], amount) * 0.5,
                    lerp(lower[3], upper[3], amount))
    return rings[-1][1] * 0.5, rings[-1][2] * 0.5, rings[-1][3]


def add_torso(mesh):
    rings = [(at(0.0, forward, height), X * width * 0.5, Y * depth * 0.5)
             for height, width, depth, forward in TORSO_RINGS]
    add_shell(mesh, rings, TORSO_SIDES)


def add_legs_and_feet(mesh):
    for side in (1.0, -1.0):
        ankle = at(side * LEG_HALF_SPACING, 0.0, FOOT_HEIGHT * 0.4)
        add_shell(mesh, tube_rings(ankle, Z, X, ((0.0, LEG_RADIUS), (LEG_TOP - ankle.z, LEG_RADIUS * 1.06))),
                  LIMB_SIDES)

        # A foot is a long egg lying on the ground, larger at the toes, which are turned outwards.
        turn = Matrix.Rotation(math.radians(FOOT_TOE_OUT_DEG) * side, 3, "Z")
        across, ahead = turn @ X, turn @ -Y
        heel = at(side * LEG_HALF_SPACING, 0.0, 0.0) - ahead * FOOT_HEEL_BACK
        rings = [(heel + Z * FOOT_HEIGHT * 0.26, NOWHERE, NOWHERE)]
        for along, width, height in FOOT_RINGS:
            top = FOOT_HEIGHT * height
            rings.append((heel + ahead * FOOT_LENGTH * along + Z * top * 0.42,
                          across * FOOT_WIDTH * 0.5 * width, Z * top * 0.58))
        rings.append((heel + ahead * FOOT_LENGTH + Z * FOOT_HEIGHT * 0.30, NOWHERE, NOWHERE))
        # What goes under the ground is brought back onto it: that is the flat sole.
        for point in add_shell(mesh, rings, FOOT_SIDES):
            point.co.z = max(point.co.z, 0.0)


def add_crest(mesh):
    """The spikes of the back: broad and flat-faced, each one curling up a little."""
    for height, turn_deg, tilt_deg, size in CREST_SPIKES:
        for side in ((1.0, -1.0) if turn_deg else (1.0,)):
            turn, tilt = math.radians(turn_deg) * side, math.radians(tilt_deg)
            half_width, half_depth, forward = torso_section(height)
            # Where the back of the torso is, at that height and that turn.
            root = Vector((half_width * math.sin(turn), -forward + half_depth * math.cos(turn), height))
            away = Vector((math.sin(turn) * math.cos(tilt), math.cos(turn) * math.cos(tilt), math.sin(tilt)))
            length = CREST_SPIKE_LENGTH * size
            half_wide, half_thick = CREST_SPIKE_WIDTH * size * 0.5, CREST_SPIKE_THICKNESS * size * 0.5
            wide = Z.cross(away).normalized()
            thick = away.cross(wide)
            sunk = 0.07   # the spike starts this far inside the torso
            hook = thick * length * CREST_SPIKE_HOOK
            rings = [
                (root - away * sunk, wide * half_wide, thick * half_thick),
                (root + away * length * 0.5 + hook * 0.25, wide * half_wide * CREST_SPIKE_BELLY,
                 thick * half_thick * CREST_SPIKE_BELLY),
                (root + away * length + hook, NOWHERE, NOWHERE),
            ]
            add_shell(mesh, rings, SPIKE_SIDES, smooth=False)


def add_neck_stub(mesh):
    """In the axes of "Head": its origin is the neck."""
    radius = NECK_WIDTH * 0.5 * 0.92
    # height and radius of each ring, against the height of the stub and the radius at its foot
    profile = ((-0.24, 1.00), (0.50, 0.93), (0.76, 0.82), (0.93, 0.52))
    rings = [(Z * NECK_STUB_HEIGHT * height, X * radius * wide, Y * radius * wide) for height, wide in profile]
    rings.append((Z * NECK_STUB_HEIGHT, NOWHERE, NOWHERE))
    add_shell(mesh, rings, NECK_SIDES)


def add_arm(mesh, side):
    """In the axes of the arm: its origin is the shoulder and it hangs down from there."""
    spread, ahead = math.radians(ARM_SPREAD_DEG), math.radians(ARM_FORWARD_DEG)
    down = Vector((side * math.sin(spread), -math.sin(ahead), -math.cos(spread) * math.cos(ahead))).normalized()
    ball = SHOULDER_RADIUS
    wrist = ARM_LENGTH - HAND_SIZE * 0.36

    # The ball of the shoulder, then the sleeve leaving it and opening towards the hand.
    lean = math.radians(45.0)
    add_shell(mesh, tube_rings(NOWHERE, down, -Y, (
        (-ball, 0.0),
        (-ball * math.cos(lean), ball * math.sin(lean)),
        (0.0, ball),
        (ball * 0.6, ball * 0.86),
        (ball * 1.25, UPPER_ARM_RADIUS),
        (lerp(ball * 1.25, wrist, 0.55), lerp(UPPER_ARM_RADIUS, SLEEVE_END_RADIUS, 0.45)),
        (wrist - 0.012, SLEEVE_END_RADIUS),
        (wrist + 0.012, SLEEVE_END_RADIUS * 0.90),
    )), LIMB_SIDES)

    # The hand is a mitten: a ball a little longer than wide, and a thumb on its front.
    hand = down * ARM_LENGTH
    add_shell(mesh, ball_rings(hand, down, -Y, HAND_SIZE * 0.54, HAND_SIZE * 0.5, HAND_SIZE * 0.46, HAND_STACKS),
              HAND_SIDES)
    thumb = hand - Y * HAND_SIZE * 0.40 - down * HAND_SIZE * 0.10
    add_shell(mesh, ball_rings(thumb, (down - Y * 1.4).normalized(), X, HAND_SIZE * 0.30, HAND_SIZE * 0.20,
                               HAND_SIZE * 0.20, 4), 6)


# ---------------------------------------------------------------------------------------------
# Objects, file and export.
# ---------------------------------------------------------------------------------------------

def make_part(name, origin, fill, parent, material):
    """One part of the model: "fill" draws its shapes in the part's own axes, origin at (0, 0, 0)."""
    shapes = bmesh.new()
    fill(shapes)
    bmesh.ops.recalc_face_normals(shapes, faces=shapes.faces[:])
    bmesh.ops.triangulate(shapes, faces=shapes.faces[:])
    data = bpy.data.meshes.new(name)
    shapes.to_mesh(data)
    shapes.free()
    data.materials.append(material)
    data.update()

    part = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(part)
    part.parent = parent
    part.location = origin
    return part


def build():
    """Empties the scene and builds the model in it. Returns the root and its four parts."""
    bpy.ops.wm.read_factory_settings(use_empty=True)

    material = bpy.data.materials.new(MATERIAL_NAME)
    material.diffuse_color = FILE_COLOUR
    shader = material.node_tree.nodes.get("Principled BSDF") if material.node_tree else None
    if shader is not None:
        shader.inputs["Base Color"].default_value = FILE_COLOUR
        shader.inputs["Roughness"].default_value = 0.9
        shader.inputs["Metallic"].default_value = 0.0

    root = bpy.data.objects.new(ROOT_NAME, None)
    root.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(root)

    def body(mesh):
        add_torso(mesh)
        add_legs_and_feet(mesh)
        add_crest(mesh)

    parts = [
        make_part("Body", Vector((0.0, 0.0, 0.0)), body, root, material),
        make_part("Head", at(0.0, 0.0, NECK_HEIGHT), add_neck_stub, root, material),
        make_part("armLeft", at(SHOULDER_HALF_WIDTH, 0.0, SHOULDER_HEIGHT), lambda mesh: add_arm(mesh, 1.0),
                  root, material),
        make_part("armRight", at(-SHOULDER_HALF_WIDTH, 0.0, SHOULDER_HEIGHT), lambda mesh: add_arm(mesh, -1.0),
                  root, material),
    ]
    bpy.context.view_layer.update()
    return root, parts


def export(root, parts, folder):
    os.makedirs(folder, exist_ok=True)
    blend_path = os.path.join(folder, "ChogBody.blend")
    fbx_path = os.path.join(folder, "ChogBody.fbx")

    # No "ChogBody.blend1" copy beside the file when it is written again.
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=blend_path, check_existing=False)

    for item in bpy.context.scene.objects:
        item.select_set(item is root or item in parts)
    bpy.context.view_layer.objects.active = root

    # Unity wants +Y up and the model looking along +Z; here it is +Z up, looking along -Y.
    # "-Z forward, Y up" is the pair that maps one onto the other once Unity has mirrored X, as it
    # does for every FBX. "bake_space_transform" writes that turn into the points themselves, so
    # no part arrives turned by 90 degrees: the game turns each part about its own axes.
    # "FBX_SCALE_UNITS" says in the file that one unit is a metre, so every part keeps scale 1.
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        check_existing=False,
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        axis_forward="-Z",
        axis_up="Y",
        use_space_transform=True,
        bake_space_transform=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_triangles=False,   # already triangles
        use_tspace=False,
        use_custom_props=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )
    return blend_path, fbx_path


def report(parts):
    total = 0
    low, high = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    for part in parts:
        triangles = sum(len(face.vertices) - 2 for face in part.data.polygons)
        total += triangles
        for point in part.data.vertices:
            world = part.matrix_world @ point.co
            low = Vector(map(min, low, world))
            high = Vector(map(max, high, world))
        place = part.matrix_world.translation
        print(f"CHOG_BODY part {part.name:9s} triangles {triangles:5d}  origin (Blender x y z) "
              f"{place.x:+.3f} {place.y:+.3f} {place.z:+.3f}")
    print(f"CHOG_BODY triangles {total} (limit {TRIANGLE_LIMIT})")
    print(f"CHOG_BODY size: {high.x - low.x:.3f} wide, {high.y - low.y:.3f} deep, {high.z - low.z:.3f} tall; "
          f"lowest point {low.z:+.4f}")
    # The ball of a shoulder must reach into the torso, or the arm would hang beside the body.
    torso_half_width = torso_section(SHOULDER_HEIGHT)[0]
    overlap = torso_half_width - (SHOULDER_HALF_WIDTH - SHOULDER_RADIUS)
    print(f"CHOG_BODY shoulder: the torso is {torso_half_width:.3f} wide from its middle there, the ball of the "
          f"arm reaches in to {SHOULDER_HALF_WIDTH - SHOULDER_RADIUS:.3f}: {overlap:+.3f} inside the torso")
    if overlap < 0.03:
        raise SystemExit("CHOG_BODY the arms do not reach into the torso: move the shoulders in or widen it")
    if total >= TRIANGLE_LIMIT:
        raise SystemExit(f"CHOG_BODY too many triangles: {total}")


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    default_out = os.path.normpath(os.path.join(here, "..", "..", ".artifacts", "chog-body"))
    parser = argparse.ArgumentParser(description="Builds and exports the body of a Chog hero.")
    parser.add_argument("--out", default=default_out, help="folder for ChogBody.fbx and ChogBody.blend")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    root, parts = build()
    report(parts)
    blend_path, fbx_path = export(root, parts, os.path.abspath(arguments.out))
    print(f"CHOG_BODY saved {blend_path}")
    print(f"CHOG_BODY saved {fbx_path}")


if __name__ == "__main__":
    main()
