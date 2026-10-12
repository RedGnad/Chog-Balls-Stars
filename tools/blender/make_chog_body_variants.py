"""Builds one of three candidate bodies for a Chog hero, to be compared as pictures before any of
them goes into the game. The body in the game is the one of make_chog_body.py: this script does not
replace it and writes nothing where the game or that script write.

Run it without the interface of Blender (5.2):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/make_chog_body_variants.py -- --variant A [--out FOLDER]

It writes ChogBody.blend and ChogBody.fbx into .artifacts/chog-body/variants/<variant>/. Look at the
result with render_chog_body_variants.py, then lay the candidates side by side with
make_chog_body_comparison.py.

The candidates follow the official drawings of Chog kept in .artifacts/chog-reference/, first of all
site_home_chog.png (the whole character from behind). What those drawings say of the body:

- The spikes belong to the head: a crown of flat spikes around a cream face. In the game the head is
  the flat picture of the Genesis, which already shows them, so the body has no spike at all.
- The body is slim and plain, a one-piece suit in the skin colour: a narrow torso that runs straight
  into two legs, no belly, long arms of one thickness.
- The hands are pink mittens clearly larger than the arm, and the feet are pink too.

What the three candidates share, and the present body does not have:

- Several materials in place of one: "ChogSkin" (torso, legs, arms), "ChogGlove" (hands and feet),
  "ChogOutline", and for candidate C "ChogCloth" (a ring at the neck and the torso).
- A dark outline made of geometry: every shape has a second shell around it, slightly larger, whose
  faces look inwards (the "inverted hull" trick). A renderer that hides the faces turned away from the
  camera, as nearly all do, then shows of that shell only what sticks out behind the shape: a line
  around it. The shells belong to the mesh of the part they outline, so they move with it.

What stays as the game expects it: four rigid parts named "Body", "Head", "armLeft" and "armRight"
under one root, the origin of "Body" on the ground between the feet, the origin of "Head" at the neck
(at the same height as today, the picture of the Genesis hangs on it), the origin of each arm at its
shoulder with the arm hanging from it, the soles on the ground, about 1.2 units tall, no skeleton, no
texture.

What moves: the shoulders. The present body has them 0.41 from the middle because its torso is 0.87
wide. A slim Chog has them much nearer the middle, so the origins of "armLeft" and "armRight" are
nearer the middle too (SHOULDER_HALF_WIDTH, SHOULDER_HEIGHT below). The game reads where an arm turns
from the model itself (ChogBodyAnimator.Attach keeps the rotations of the parts, no position), so
this needs no change there; it is said here because the numbers are not those of the present body.
"""

import argparse
import math
import os
import sys
from types import SimpleNamespace

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import make_chog_body as base  # noqa: E402  (the folder of this file must be on the path first)

X, Y, Z, NOWHERE = base.X, base.Y, base.Z, base.NOWHERE
at, lerp = base.at, base.lerp

# Fixed by the game: the height of the neck, the origin of "Head". The picture of the Genesis and
# the name above it are placed from there.
NECK_HEIGHT = base.NECK_HEIGHT

TRIANGLE_LIMIT = 4500        # for the whole model, outline shells included
HEIGHT_RANGE = (1.1, 1.3)    # "about 1.2 units tall"

SKIN, GLOVE, CLOTH, OUTLINE = "ChogSkin", "ChogGlove", "ChogCloth", "ChogOutline"
MATERIAL_ORDER = (SKIN, GLOVE, CLOTH, OUTLINE)
# The colours in the file, for whoever opens it. The game is expected to replace them (the skin by
# the Skin colour of the Genesis); the renders use their own.
FILE_COLOURS = {
    SKIN: (0.36, 0.21, 0.69, 1.0),       # the purple of the default Chog
    GLOVE: (0.72, 0.07, 0.22, 1.0),      # its pink
    CLOTH: (0.80, 0.80, 0.84, 1.0),
    OUTLINE: (0.004, 0.003, 0.006, 1.0),
}

# ---------------------------------------------------------------------------------------------
# Proportions of candidate A, the one that follows site_home_chog.png. One unit is one unit of the
# game. On that drawing the body, from the neck to the soles, is 600 points tall: the torso is 275
# wide (0.46 of the height), the legs 260 long (0.43), an arm about 95 thick (0.16) and 330 long
# (0.55), a fist about 110 across. The neck is at 1.03 here, so those are 0.47, 0.44, 0.16, 0.56.
# ---------------------------------------------------------------------------------------------
NUMBERS = {
    # Neck: the stub above it is the part "Head", which the game hides.
    "NECK_STUB_HEIGHT": 0.17,    # the whole model is NECK_HEIGHT + this tall
    "NECK_WIDTH": 0.24,

    # The torso: one ring per line, from between the legs to the neck: height, width, depth.
    # No belly: its sides are nearly upright, a little wider at the hips than at the chest, as on
    # the drawing, and it stays as wide as the legs down to where they part.
    "TORSO_RINGS": (
        (0.395, 0.00, 0.00),     # closed between the legs
        (0.405, 0.30, 0.20),
        (0.425, 0.44, 0.30),
        (0.460, 0.50, 0.33),
        (0.530, 0.50, 0.34),     # the hips
        (0.700, 0.48, 0.34),
        (0.880, 0.45, 0.33),     # the chest
        (0.960, 0.41, 0.30),
        (1.005, 0.33, 0.25),
        (NECK_HEIGHT, 0.26, 0.25),
        (NECK_HEIGHT + 0.02, 0.00, 0.00),   # closed under the neck stub
    ),
    # Above this height the torso is "ChogCloth". None: no cloth. It must be the height of a ring.
    "CLOTH_FROM": None,
    # A ring around the neck, in "ChogCloth": height of its middle, half its height, half its
    # width, half its depth. None: no ring.
    "COLLAR": None,

    # Shoulders: the origin of each arm, the point an arm turns about. A ball of SHOULDER_RADIUS
    # sits on that point and belongs to the arm: a ball looks the same however it is turned, so the
    # arm never leaves a hole in the body. It must reach into the torso.
    "SHOULDER_HEIGHT": 0.87,
    "SHOULDER_HALF_WIDTH": 0.225,
    "SHOULDER_RADIUS": 0.095,

    # Arms and hands. An arm hangs from its shoulder, leaning away from the body by ARM_SPREAD_DEG.
    # It is a sleeve of one thickness, and the hand is a mitten larger than the sleeve.
    "ARM_LENGTH": 0.50,          # shoulder to the middle of the hand
    "ARM_SPREAD_DEG": 19.0,
    "ARM_FORWARD_DEG": 6.0,
    "ARM_RADIUS": 0.082,
    "HAND_SIZE": 0.24,           # across the hand
    "HAND_LONG": 0.56,           # the hand against HAND_SIZE: along the arm,
    "HAND_WIDE": 0.50,           # across,
    "HAND_THICK": 0.46,          # and from palm to back
    "THUMB": True,

    # Legs and feet (part of "Body": they do not move on their own). A leg is a straight tube, as
    # deep as the torso lets it be, whose outer side carries on the side of the torso.
    "LEG_HALF_SPACING": 0.150,   # from the middle of the body to one leg, at the hip
    "LEG_STANCE": 0.015,         # how much further from the middle the ankle is than the hip
    "LEG_HALF_WIDTH": 0.095,
    "LEG_HALF_DEPTH": 0.115,
    "LEG_TOP": 0.52,             # the legs end inside the torso
    "LEG_ENTERS_TORSO": 0.46,    # the height of the ring where the torso is as wide as the legs
    # A foot is pink like the hands: a sock up the ankle, and the foot itself ahead of it.
    "SOCK_HEIGHT": 0.10,
    "SOCK_THICKNESS": 0.014,     # how far the sock stands off the leg
    "FOOT_LENGTH": 0.33,
    "FOOT_WIDTH": 0.24,
    "FOOT_HEIGHT": 0.13,
    "FOOT_HEEL_BACK": 0.125,     # how far the heel is behind the leg
    "FOOT_TOE_OUT_DEG": 18.0,    # how far the toes point away from each other
    # A foot from heel to toe: where along its length, width and height against the largest.
    "FOOT_RINGS": base.FOOT_RINGS,

    # How far an outline shell stands off the shape it surrounds. A hero is about 46 points of a phone
    # screen per unit of the model, so 0.034 is a line of about one and a half points.
    "OUTLINE_THICKNESS": 0.032,
    "OUTLINE_THUMB": 0.5,        # the thumb is small: its outline is this much of the others

    # How finely the round shapes are cut. More sides: smoother outline, more triangles.
    "TORSO_SIDES": 20,
    "LIMB_SIDES": 12,
    "HAND_SIDES": 14,
    "HAND_STACKS": 7,
    "FOOT_SIDES": 14,
    "NECK_SIDES": 12,
    "COLLAR_SIDES": 18,
}

# What candidate B changes: the same character, pushed to be read 70 points tall from 73 degrees
# above. From there the picture of the head covers the top of the body, so what counts is what
# stands out around it: the hands and the feet.
PUSHED = {
    # Shorter legs under a torso a little wider, so the larger hands and feet do not dwarf it.
    "TORSO_RINGS": (
        (0.305, 0.00, 0.00),
        (0.315, 0.32, 0.22),
        (0.335, 0.48, 0.32),
        (0.370, 0.54, 0.35),
        (0.440, 0.54, 0.36),     # the hips
        (0.660, 0.52, 0.36),
        (0.870, 0.49, 0.35),     # the chest
        (0.955, 0.44, 0.31),
        (1.005, 0.35, 0.26),
        (NECK_HEIGHT, 0.26, 0.25),
        (NECK_HEIGHT + 0.02, 0.00, 0.00),
    ),
    "LEG_TOP": 0.43,
    "LEG_ENTERS_TORSO": 0.37,
    "LEG_HALF_SPACING": 0.160,
    "LEG_STANCE": 0.055,         # a slightly wider stance
    "LEG_HALF_WIDTH": 0.105,
    "LEG_HALF_DEPTH": 0.125,
    "SOCK_HEIGHT": 0.11,
    "FOOT_LENGTH": 0.42,
    "FOOT_WIDTH": 0.29,
    "FOOT_HEIGHT": 0.18,
    "FOOT_HEEL_BACK": 0.15,
    "FOOT_TOE_OUT_DEG": 22.0,
    # Arms held away from the body: seen from above the hands stay outside the picture of the head,
    # which is 0.91 units across at the scale of the model, and clear of the feet (at 36 degrees and
    # 8 forward, a hand and the foot under it were one pink patch at the size of a phone).
    "SHOULDER_HALF_WIDTH": 0.25,
    "SHOULDER_RADIUS": 0.100,
    "ARM_LENGTH": 0.45,
    "ARM_SPREAD_DEG": 41.0,
    "ARM_FORWARD_DEG": 3.0,
    "ARM_RADIUS": 0.085,
    "HAND_SIZE": 0.32,
}

VARIANTS = {
    "A": {
        "about": "faithful to site_home_chog.png: a slim one-piece body, two legs, long arms, pink mittens "
                 "and pink feet",
        "numbers": {},
    },
    "B": {
        "about": "the same character pushed to be read small from above: shorter legs, a wider stance, larger "
                 "hands and feet, arms held away from the body",
        "numbers": PUSHED,
    },
    "C": {
        "about": "B with a ring at the neck and the torso in a cloth material, to take the colour of the "
                 "clothing of the Genesis later",
        "numbers": {
            **PUSHED,
            "CLOTH_FROM": 0.440,
            "COLLAR": (0.975, 0.075, 0.285, 0.245),
        },
    },
}


def numbers_of(variant):
    unknown = set(VARIANTS[variant]["numbers"]) - set(NUMBERS)
    if unknown:
        raise SystemExit(f"CHOG_VARIANT candidate {variant} sets numbers that do not exist: {sorted(unknown)}")
    return SimpleNamespace(**{**NUMBERS, **VARIANTS[variant]["numbers"]})


# ---------------------------------------------------------------------------------------------
# A part being built: its shapes, which material each one takes and which ones get an outline.
# ---------------------------------------------------------------------------------------------

class PartShapes:
    def __init__(self):
        self.mesh = bmesh.new()
        self.outlined = []       # (faces of one closed shell, thickness of its outline)

    def paint(self, faces, material):
        for face in faces:
            face.material_index = MATERIAL_ORDER.index(material)

    def shell(self, rings, sides, material, outline, smooth=True, fade=None):
        """One closed shell, as base.add_shell makes it. "outline" is the thickness of its outline
        shell, 0 for none; "fade", when given, says for a point how much of that thickness it gets
        (1 all of it, 0 none), for a shape that ends inside another. Returns its points and faces."""
        points = base.add_shell(self.mesh, rings, sides, smooth=smooth)
        faces = list({face for point in points for face in point.link_faces})
        self.paint(faces, material)
        if outline > 0.0:
            self.outlined.append((faces, outline, fade))
        return points, faces

    def add_outlines(self, on_ground):
        """The inverted hull: a copy of each shell, pushed out along the normals, its faces turned
        inside out. "on_ground" keeps the copy above the ground, as the soles are."""
        mesh = self.mesh
        for faces, thickness, fade in self.outlined:
            points = list({point for face in faces for point in face.verts})
            edges = list({edge for face in faces for edge in face.edges})
            copy = bmesh.ops.duplicate(mesh, geom=points + edges + faces)
            twins = copy["vert_map"]
            for point in points:
                twin = twins[point]
                twin.co = point.co + point.normal * thickness * (fade(point.co) if fade else 1.0)
                if on_ground:
                    twin.co.z = max(twin.co.z, 0.0)
            hull = [item for item in copy["geom"] if isinstance(item, bmesh.types.BMFace)]
            if len(hull) != len(faces):
                raise SystemExit("CHOG_VARIANT an outline shell does not have the faces of its shape")
            bmesh.ops.reverse_faces(mesh, faces=hull)
            for face in hull:
                face.smooth = True
            self.paint(hull, OUTLINE)

    def finish(self, name, origin, parent, materials, on_ground=False):
        """Turns the shapes into the object of the part, as base.make_part does, outlines included."""
        mesh = self.mesh
        bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces[:])
        mesh.normal_update()
        self.add_outlines(on_ground)
        bmesh.ops.triangulate(mesh, faces=mesh.faces[:])

        # The part carries only the materials it uses, in the order of MATERIAL_ORDER.
        used = sorted({face.material_index for face in mesh.faces})
        for face in mesh.faces:
            face.material_index = used.index(face.material_index)
        data = bpy.data.meshes.new(name)
        mesh.to_mesh(data)
        mesh.free()
        for index in used:
            data.materials.append(materials[MATERIAL_ORDER[index]])
        data.update()

        part = bpy.data.objects.new(name, data)
        bpy.context.scene.collection.objects.link(part)
        part.parent = parent
        part.location = origin
        return part


# ---------------------------------------------------------------------------------------------
# Shapes. Every shape is a closed shell of rings, as in make_chog_body.py, whose add_shell,
# tube_rings and ball_rings are used.
# ---------------------------------------------------------------------------------------------

def torso_half_width(numbers, height):
    """Half the width of the torso at a height above the ground."""
    rings = numbers.TORSO_RINGS
    if height <= rings[0][0]:
        return rings[0][1] * 0.5
    for lower, upper in zip(rings, rings[1:]):
        if height <= upper[0]:
            return lerp(lower[1], upper[1], (height - lower[0]) / (upper[0] - lower[0])) * 0.5
    return rings[-1][1] * 0.5


def add_torso(part, numbers):
    rings = [(Z * height, X * width * 0.5, Y * depth * 0.5) for height, width, depth in numbers.TORSO_RINGS]
    _, faces = part.shell(rings, numbers.TORSO_SIDES, SKIN, numbers.OUTLINE_THICKNESS)
    if numbers.CLOTH_FROM is not None:
        if not any(abs(ring[0] - numbers.CLOTH_FROM) < 1e-6 for ring in numbers.TORSO_RINGS):
            raise SystemExit("CHOG_VARIANT CLOTH_FROM is not the height of a ring of the torso")
        part.paint([face for face in faces if face.calc_center_median().z > numbers.CLOTH_FROM], CLOTH)


def add_collar(part, numbers):
    """A ring around the neck: a flat ball the neck goes through, so only its rim shows."""
    if numbers.COLLAR is None:
        return
    middle, half_height, half_width, half_depth = numbers.COLLAR
    part.shell(base.ball_rings(Z * middle, Z, X, half_height, half_width, half_depth, 6),
               numbers.COLLAR_SIDES, CLOTH, numbers.OUTLINE_THICKNESS)


def add_legs_and_feet(part, numbers):
    for side in (1.0, -1.0):
        hip = at(side * numbers.LEG_HALF_SPACING, 0.0, numbers.LEG_TOP)
        ankle = at(side * (numbers.LEG_HALF_SPACING + numbers.LEG_STANCE), 0.0, 0.0)

        def on_leg(height):
            """The middle of the leg at a height above the ground."""
            return lerp(ankle, hip, height / numbers.LEG_TOP)

        # The leg starts inside the sock and ends inside the torso. From where it enters the torso
        # its outline thins to nothing, so that no line crosses the hip: the suit is one piece.
        sock, enters = numbers.SOCK_HEIGHT, numbers.LEG_ENTERS_TORSO
        across, deep = X * numbers.LEG_HALF_WIDTH, Y * numbers.LEG_HALF_DEPTH

        def inside_torso(point):
            return max(0.0, min(1.0, (numbers.LEG_TOP - point.z) / (numbers.LEG_TOP - enters)))

        part.shell([(on_leg(sock * 0.5), across, deep), (on_leg(enters), across, deep), (hip, across, deep)],
                   numbers.LIMB_SIDES, SKIN, numbers.OUTLINE_THICKNESS, fade=inside_torso)

        # The sock: a sleeve around the bottom of the leg, closing onto it at its top.
        thick = numbers.SOCK_THICKNESS
        wider, deeper = X * (numbers.LEG_HALF_WIDTH + thick), Y * (numbers.LEG_HALF_DEPTH + thick)
        part.shell([(on_leg(0.0), wider, deeper), (on_leg(sock), wider, deeper),
                    (on_leg(sock + thick), across * 0.9, deep * 0.9)],
                   numbers.LIMB_SIDES, GLOVE, numbers.OUTLINE_THICKNESS)

        # A foot is a long egg lying on the ground, larger at the toes, which are turned outwards.
        turn = Matrix.Rotation(math.radians(numbers.FOOT_TOE_OUT_DEG) * side, 3, "Z")
        across, ahead = turn @ X, turn @ -Y
        heel = ankle - ahead * numbers.FOOT_HEEL_BACK
        rings = [(heel + Z * numbers.FOOT_HEIGHT * 0.26, NOWHERE, NOWHERE)]
        for along, width, height in numbers.FOOT_RINGS:
            top = numbers.FOOT_HEIGHT * height
            rings.append((heel + ahead * numbers.FOOT_LENGTH * along + Z * top * 0.42,
                          across * numbers.FOOT_WIDTH * 0.5 * width, Z * top * 0.58))
        rings.append((heel + ahead * numbers.FOOT_LENGTH + Z * numbers.FOOT_HEIGHT * 0.30, NOWHERE, NOWHERE))
        points, _ = part.shell(rings, numbers.FOOT_SIDES, GLOVE, numbers.OUTLINE_THICKNESS)
        # What goes under the ground is brought back onto it: that is the flat sole.
        for point in points:
            point.co.z = max(point.co.z, 0.0)


def add_neck_stub(part, numbers):
    """In the axes of "Head": its origin is the neck. The game hides this part."""
    radius = numbers.NECK_WIDTH * 0.5 * 0.92
    profile = ((-0.24, 1.00), (0.50, 0.93), (0.76, 0.82), (0.93, 0.52))
    rings = [(Z * numbers.NECK_STUB_HEIGHT * height, X * radius * wide, Y * radius * wide)
             for height, wide in profile]
    rings.append((Z * numbers.NECK_STUB_HEIGHT, NOWHERE, NOWHERE))
    part.shell(rings, numbers.NECK_SIDES, SKIN, numbers.OUTLINE_THICKNESS)


def add_arm(part, numbers, side):
    """In the axes of the arm: its origin is the shoulder and it hangs down from there."""
    spread, ahead = math.radians(numbers.ARM_SPREAD_DEG), math.radians(numbers.ARM_FORWARD_DEG)
    down = Vector((side * math.sin(spread), -math.sin(ahead), -math.cos(spread) * math.cos(ahead))).normalized()
    ball, radius = numbers.SHOULDER_RADIUS, numbers.ARM_RADIUS
    hand_size = numbers.HAND_SIZE
    wrist = numbers.ARM_LENGTH - hand_size * 0.36

    # The ball of the shoulder, then the sleeve leaving it, of one thickness down to the wrist.
    lean = math.radians(45.0)
    part.shell(base.tube_rings(NOWHERE, down, -Y, (
        (-ball, 0.0),
        (-ball * math.cos(lean), ball * math.sin(lean)),
        (0.0, ball),
        (ball * 0.6, lerp(ball, radius, 0.6)),
        (ball * 1.2, radius),
        (wrist - 0.012, radius),
        (wrist + 0.012, radius * 0.90),
    )), numbers.LIMB_SIDES, SKIN, numbers.OUTLINE_THICKNESS)

    # The hand is a mitten: a ball larger than the sleeve, and a thumb on its front.
    hand = down * numbers.ARM_LENGTH
    part.shell(base.ball_rings(hand, down, -Y, hand_size * numbers.HAND_LONG, hand_size * numbers.HAND_WIDE,
                               hand_size * numbers.HAND_THICK, numbers.HAND_STACKS),
               numbers.HAND_SIDES, GLOVE, numbers.OUTLINE_THICKNESS)
    if numbers.THUMB:
        thumb = hand - Y * hand_size * 0.40 - down * hand_size * 0.10
        part.shell(base.ball_rings(thumb, (down - Y * 1.4).normalized(), X, hand_size * 0.30, hand_size * 0.20,
                                   hand_size * 0.20, 4),
                   6, GLOVE, numbers.OUTLINE_THICKNESS * numbers.OUTLINE_THUMB)


# ---------------------------------------------------------------------------------------------
# Objects and report.
# ---------------------------------------------------------------------------------------------

def make_materials():
    materials = {}
    for name in MATERIAL_ORDER:
        material = bpy.data.materials.new(name)
        material.diffuse_color = FILE_COLOURS[name]
        shader = material.node_tree.nodes.get("Principled BSDF") if material.node_tree else None
        if shader is not None:
            shader.inputs["Base Color"].default_value = FILE_COLOURS[name]
            shader.inputs["Roughness"].default_value = 1.0 if name == OUTLINE else 0.9
            shader.inputs["Metallic"].default_value = 0.0
        if name == OUTLINE:
            # Only the inside of an outline shell is to be seen, and it must not put the shape it
            # surrounds in its shadow.
            material.use_backface_culling = True
            if hasattr(material, "use_backface_culling_shadow"):
                material.use_backface_culling_shadow = True
        materials[name] = material
    return materials


def build(variant):
    """Empties the scene and builds the candidate in it. Returns the root, its four parts and the
    numbers it was built from."""
    numbers = numbers_of(variant)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    materials = make_materials()

    root = bpy.data.objects.new(base.ROOT_NAME, None)
    root.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(root)

    body = PartShapes()
    add_torso(body, numbers)
    add_collar(body, numbers)
    add_legs_and_feet(body, numbers)

    head = PartShapes()
    add_neck_stub(head, numbers)

    arms = {}
    for name, side in (("armLeft", 1.0), ("armRight", -1.0)):
        arms[name] = PartShapes()
        add_arm(arms[name], numbers, side)

    shoulder = numbers.SHOULDER_HALF_WIDTH
    parts = [
        body.finish("Body", Vector((0.0, 0.0, 0.0)), root, materials, on_ground=True),
        head.finish("Head", at(0.0, 0.0, NECK_HEIGHT), root, materials),
        arms["armLeft"].finish("armLeft", at(shoulder, 0.0, numbers.SHOULDER_HEIGHT), root, materials),
        arms["armRight"].finish("armRight", at(-shoulder, 0.0, numbers.SHOULDER_HEIGHT), root, materials),
    ]
    # Materials no part uses are not kept in the file.
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)
    bpy.context.view_layer.update()
    return root, parts, numbers


def report(variant, parts, numbers):
    """Prints what was built and stops if a point of the contract with the game is not met."""
    failures = []
    total = outline_total = 0
    low, high = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    shapes_low, shapes_high = low.copy(), high.copy()
    for part in parts:
        names = [slot.material.name for slot in part.material_slots]
        triangles = len(part.data.polygons)
        is_outline = [names[face.material_index] == OUTLINE for face in part.data.polygons]
        outline = sum(is_outline)
        total += triangles
        outline_total += outline
        of_shapes = {index for face, hull in zip(part.data.polygons, is_outline) if not hull for index in face.vertices}
        part_low, part_high = 1e9, -1e9
        for point in part.data.vertices:
            world = part.matrix_world @ point.co
            low = Vector(map(min, low, world))
            high = Vector(map(max, high, world))
            part_low, part_high = min(part_low, world.z), max(part_high, world.z)
            if point.index in of_shapes:
                shapes_low = Vector(map(min, shapes_low, world))
                shapes_high = Vector(map(max, shapes_high, world))
        place = part.matrix_world.translation
        print(f"CHOG_VARIANT {variant} part {part.name:9s} triangles {triangles:5d} (outline {outline:4d})  "
              f"origin (Blender x y z) {place.x:+.3f} {place.y:+.3f} {place.z:+.3f}  materials {names}")
        if any(len(face.vertices) != 3 for face in part.data.polygons):
            failures.append(f"{part.name} has faces that are not triangles")
        if part.name.startswith("arm") and not place.z - part_low > 2.0 * (part_high - place.z):
            failures.append(f"{part.name} does not hang down from its origin")

    print(f"CHOG_VARIANT {variant} triangles {total} (limit {TRIANGLE_LIMIT}): {total - outline_total} of shapes, "
          f"{outline_total} of outline")
    size, shapes = high - low, shapes_high - shapes_low
    print(f"CHOG_VARIANT {variant} size with the outline: {size.x:.3f} wide, {size.y:.3f} deep, {size.z:.3f} tall; "
          f"lowest point {low.z:+.4f}")
    print(f"CHOG_VARIANT {variant} size of the shapes alone: {shapes.x:.3f} wide, {shapes.y:.3f} deep, "
          f"{shapes.z:.3f} tall")

    by_name = {part.name: part.matrix_world.translation for part in parts}
    if sorted(by_name) != ["Body", "Head", "armLeft", "armRight"]:
        failures.append(f"the parts are {sorted(by_name)}")
    else:
        if by_name["Body"].length > 1e-6:
            failures.append("the origin of Body is not on the ground, between the feet")
        if (by_name["Head"] - at(0.0, 0.0, NECK_HEIGHT)).length > 1e-6:
            failures.append("the origin of Head moved")
        left, right = by_name["armLeft"], by_name["armRight"]
        if not (left.x > 0.0 > right.x and abs(left.x + right.x) < 1e-6 and abs(left.z - right.z) < 1e-6
                and abs(left.y) < 1e-6 and abs(right.y) < 1e-6 and left.z < NECK_HEIGHT):
            failures.append("the shoulders are not level, one on each side, under the neck")
        present = (base.SHOULDER_HALF_WIDTH, base.SHOULDER_HEIGHT)
        print(f"CHOG_VARIANT {variant} shoulders (origins of the arms): {left.x:.3f} from the middle, {left.z:.3f} "
              f"high; the present body has them {present[0]:.3f} from the middle, {present[1]:.3f} high")
    if total > TRIANGLE_LIMIT:
        failures.append(f"{total} triangles, the limit is {TRIANGLE_LIMIT}")
    if abs(low.z) > 1e-4:
        failures.append(f"the lowest point is at {low.z:+.4f}, not on the ground")
    if not HEIGHT_RANGE[0] <= size.z <= HEIGHT_RANGE[1]:
        failures.append(f"the model is {size.z:.3f} tall")

    # The ball of a shoulder must reach into the torso, or the arm would hang beside the body.
    reach = torso_half_width(numbers, numbers.SHOULDER_HEIGHT) - (numbers.SHOULDER_HALF_WIDTH - numbers.SHOULDER_RADIUS)
    print(f"CHOG_VARIANT {variant} shoulder: the ball of the arm reaches {reach:+.3f} inside the torso")
    if reach < 0.03:
        failures.append("the arms do not reach into the torso")

    if failures:
        raise SystemExit(f"CHOG_VARIANT {variant} FAILED: " + "; ".join(failures))
    print(f"CHOG_VARIANT {variant} keeps the contract: four parts, Body on the ground, Head at the neck, each arm "
          "hanging from its shoulder")


def main():
    default_out = os.path.normpath(os.path.join(HERE, "..", "..", ".artifacts", "chog-body", "variants"))
    parser = argparse.ArgumentParser(description="Builds a candidate body of a Chog hero.")
    parser.add_argument("--variant", required=True, choices=sorted(VARIANTS))
    parser.add_argument("--out", default=None, help="folder for ChogBody.fbx and ChogBody.blend "
                                                    "(default: .artifacts/chog-body/variants/<variant>)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    folder = os.path.abspath(arguments.out or os.path.join(default_out, arguments.variant))

    # Never over the body the game uses, nor over the file it was copied from.
    protected = os.path.normpath(os.path.join(default_out, ".."))
    if os.path.normpath(folder) == protected:
        raise SystemExit(f"CHOG_VARIANT {folder} holds the body of the game: choose another folder")

    print(f"CHOG_VARIANT {arguments.variant}: {VARIANTS[arguments.variant]['about']}")
    root, parts, numbers = build(arguments.variant)
    report(arguments.variant, parts, numbers)
    blend_path, fbx_path = base.export(root, parts, folder)
    print(f"CHOG_VARIANT saved {blend_path}")
    print(f"CHOG_VARIANT saved {fbx_path}")


if __name__ == "__main__":
    main()
