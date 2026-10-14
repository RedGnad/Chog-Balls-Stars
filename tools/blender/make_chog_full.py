"""Builds a complete Chog character, head included, in one of three head styles, to be compared as
pictures. Nothing here goes into the game: the body of the game is the one of make_chog_body.py.

Run it without the interface of Blender (5.2):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/make_chog_full.py -- --style H1 [--out FOLDER]

It writes ChogBody.blend and ChogBody.fbx into .artifacts/chog-body/full/<style>/. Look at the result
with render_chog_full.py, then lay the styles side by side with make_chog_full_comparison.py.

What a Chog head is, read on the official drawings kept in .artifacts/chog-reference/
(site_tools_jump.png for the front, site_home_chog.png for the back, body.png and face.png for the
layers of the picture maker):

- a big head, clearly wider than the body, about half the height of the character;
- a crown of large spikes in the skin colour, the signature: on body.png seven broad leaves over the
  top and the sides of the head, none under it, the lowest pair pointing sideways and a little down;
- a cream face that is the lower front of the head, between a rounded square and an oval, a little
  wider at the brow than at the chin, with small teeth of hair coming down onto the brow;
- on the face two dark eyes, two pink cheeks, a small pink nose and a thin smile.

The three styles differ by the crown, and the third by the way the head is held:

- H1  seven thick flat blades in one plane: the star the drawings show from the front.
- H2  round spikes pointing out of the top and the back half of the head in every direction.
- H3  made for the camera of the game, which looks down from 73 degrees and so sees the top and the
      back of the head: the star of H1 in thicker blades, a second row of blades lying nearly flat
      over the back, and the head turned up by 20 degrees so that the face shows from above.

The body is candidate B of make_chog_body_variants.py (slim suit, pink mittens and feet, arms held
out) at BODY_SCALE of its size so that the head dominates, with hands and feet kept larger than that.

What stays as the game expects it: four rigid parts named "Body", "Head", "armLeft" and "armRight"
under one root, the origin of "Body" on the ground between the feet, the origin of "Head" at the
neck, the origin of each arm at its shoulder with the arm hanging from it, the soles on the ground,
no skeleton, no texture. The head is now the part named "Head", not a stub to hide. The dark outline
is the same second shell as in the candidates; the face, the eyes, the cheeks, the nose and the mouth
are thin skins laid on the head, each in its own flat material.

Materials: "ChogSkin" (body, head, spikes), "ChogFace" (cream; also the glint of each eye),
"ChogGlove" (hands and feet), "ChogEye" (eyes and mouth), "ChogBlush" (cheeks and nose),
"ChogOutline" (the outline shells and the line around the face).
"""

import argparse
import math
import os
import sys
from types import SimpleNamespace

import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import make_chog_body_variants as variants  # noqa: E402  (the folder of this file must be on the path first)

base = variants.base
X, Y, Z, NOWHERE = base.X, base.Y, base.Z, base.NOWHERE
at, lerp = base.at, base.lerp

TRIANGLE_LIMIT = 7000        # for the whole model, outline shells included

SKIN, FACE, GLOVE, EYE, BLUSH, OUTLINE = "ChogSkin", "ChogFace", "ChogGlove", "ChogEye", "ChogBlush", "ChogOutline"
MATERIAL_ORDER = (SKIN, FACE, GLOVE, EYE, BLUSH, OUTLINE)
# The colours in the file, for whoever opens it (the numbers Blender computes with). The game is
# expected to replace the skin by the Skin colour of the Genesis; the renders use their own.
FILE_COLOURS = {
    SKIN: (0.36, 0.21, 0.69, 1.0),       # the purple of the default Chog
    FACE: (0.97, 0.90, 0.62, 1.0),       # its cream
    GLOVE: (0.72, 0.07, 0.22, 1.0),      # its pink
    EYE: (0.006, 0.004, 0.012, 1.0),
    BLUSH: (0.72, 0.07, 0.22, 1.0),
    OUTLINE: (0.004, 0.003, 0.006, 1.0),
}

# ---------------------------------------------------------------------------------------------
# The body: candidate B, BODY_SCALE times its size, then the numbers below in place of the scaled
# ones. One unit is one unit of the game.
# ---------------------------------------------------------------------------------------------
# (First tries at 0.50: the torso was a fifth of the width of the crown, a doll under a mask. On
# body.png the torso is more than half as wide as the crown.)
BODY_SCALE = 0.58
LENGTHS = (
    "NECK_STUB_HEIGHT", "NECK_WIDTH", "SHOULDER_HEIGHT", "SHOULDER_HALF_WIDTH", "SHOULDER_RADIUS",
    "ARM_LENGTH", "ARM_RADIUS", "HAND_SIZE",
    "LEG_HALF_SPACING", "LEG_STANCE", "LEG_HALF_WIDTH", "LEG_HALF_DEPTH", "LEG_TOP", "LEG_ENTERS_TORSO",
    "SOCK_HEIGHT", "SOCK_THICKNESS", "FOOT_LENGTH", "FOOT_WIDTH", "FOOT_HEIGHT", "FOOT_HEEL_BACK",
)
NECK_HEIGHT = variants.NECK_HEIGHT * BODY_SCALE      # the origin of "Head"
BODY_CHANGES = {
    # Under a head this large a hand or a foot at that scale is a dot: they stay larger, and the
    # arms are longer, held out and a little forward, so the hands show in front of the crown when
    # the character is seen from above.
    "SHOULDER_RADIUS": 0.062,
    "ARM_LENGTH": 0.30,
    "ARM_SPREAD_DEG": 48.0,
    "ARM_FORWARD_DEG": 14.0,
    "ARM_RADIUS": 0.054,
    "HAND_SIZE": 0.22,
    "SOCK_THICKNESS": 0.010,
    "FOOT_LENGTH": 0.29,
    "FOOT_WIDTH": 0.19,
    "FOOT_HEIGHT": 0.115,
    "FOOT_HEEL_BACK": 0.09,
    "OUTLINE_THICKNESS": 0.024,
}

# ---------------------------------------------------------------------------------------------
# The head, in the axes of the part "Head": its origin is the neck, +Z is up, the face looks
# along -Y. Heights are counted from the neck, places on the face from the middle of the face.
# ---------------------------------------------------------------------------------------------
HEAD = {
    # The head itself: an egg lying on its side. Height of its middle above the neck, then half its
    # width, half its height, half its depth.
    "HEAD_MIDDLE": 0.215,
    "HEAD_HALF_WIDTH": 0.30,
    "HEAD_HALF_HEIGHT": 0.25,
    "HEAD_HALF_DEPTH": 0.26,
    "HEAD_SIDES": 20,
    "HEAD_STACKS": 10,
    # How far the head is turned up, about the line through its ears. 0: it looks straight ahead.
    "HEAD_PITCH_DEG": 0.0,

    # The face: where its middle is against the middle of the head (to the left, up), half its
    # width, half its height, how square it is (2 is an oval, more is closer to a rectangle with
    # round corners), how much wider the brow is than the chin, how far it stands off the head,
    # and the width of the dark line around it.
    # (First try: a face in the middle of the front, with skin all around it. The head then showed
    # as a hood around a screen. On face.png the face is the lower front of the head: its chin is
    # the bottom of the head, and the hair is above it.)
    "FACE_MIDDLE": (0.0, -0.050),
    "FACE_HALF_WIDTH": 0.225,
    "FACE_HALF_HEIGHT": 0.178,
    "FACE_SQUARENESS": 3.0,
    "FACE_TAPER": 0.10,
    "FACE_LIFT": 0.012,
    "FACE_LINE": 0.018,
    "FACE_RINGS": 4,
    "FACE_SEGMENTS": 24,
    # What is on the face. Each: to the side of the middle of the face, above it, half width, half
    # height. The eyes and the cheeks are drawn on both sides.
    "EYE": (0.095, 0.020, 0.038, 0.054),
    "EYE_GLINT": (0.012, 0.020, 0.012),      # to the side and above the middle of the eye, radius
    "CHEEK": (0.165, -0.065, 0.045, 0.032),
    "NOSE": (0.0, -0.040, 0.020, 0.013),
    "MOUTH": (0.0, -0.090, 0.045, 0.020, 0.009),   # middle, half width, how much it sags, thickness
    # Small teeth of hair coming down onto the brow, as face.png draws them: how many, how far each
    # comes down, how wide. 0 teeth: none.
    "FRINGE_TEETH": 5,
    "FRINGE_DROP": 0.040,
    "FRINGE_WIDTH": 0.070,

    # The crown. One line per spike: turn around the head seen from the front, in degrees from
    # straight up (a value other than 0 or 180 gives one spike on each side); how far it leans back
    # out of that plane (90 is straight back); length; width at the root; thickness at the root; and
    # which way its flat sides look: "front" (a blade of the star seen from the front), "up" (a
    # blade lying flat, seen from above) or "round" (a cone).
    # On body.png the crown has seven spikes, over the top and the sides of the head only: the
    # lowest pair points sideways and a little down, and under it the jaw is bare. (First tries had
    # nine all around, two of them under the face: a sunflower, not a Chog.)
    "SPIKES": (),
    "SPIKE_SUNK": 0.10,          # a spike starts this far inside the head
    "SPIKE_CONE_SIDES": 7,
    "OUTLINE_THICKNESS": 0.024,
}

STYLES = {
    "H1": {
        "about": "the star of the drawings: seven thick flat blades in one plane over the top and the sides "
                 "of a rounded head",
        "head": {
            "SPIKES": (
                (0.0, 10.0, 0.38, 0.30, 0.12, "front"),
                (36.0, 10.0, 0.36, 0.30, 0.12, "front"),
                (70.0, 10.0, 0.36, 0.30, 0.12, "front"),
                (101.0, 12.0, 0.40, 0.32, 0.12, "front"),
            ),
        },
    },
    "H2": {
        "about": "a hedgehog: round spikes pointing out of the top and the back half of a round head",
        "head": {
            "SPIKES": (
                (0.0, 20.0, 0.31, 0.25, 0.25, "round"),
                (38.0, 20.0, 0.30, 0.25, 0.25, "round"),
                (76.0, 20.0, 0.30, 0.25, 0.25, "round"),
                (106.0, 24.0, 0.30, 0.25, 0.25, "round"),
                (0.0, 55.0, 0.29, 0.25, 0.25, "round"),
                (50.0, 55.0, 0.29, 0.25, 0.25, "round"),
                (100.0, 55.0, 0.28, 0.24, 0.24, "round"),
                (0.0, 90.0, 0.26, 0.24, 0.24, "round"),
            ),
        },
    },
    "H3": {
        "about": "for the camera of the game: the star of H1 in thicker blades, a second row of flat blades "
                 "fanning out over the back, the head turned up",
        "head": {
            # The camera of the game sees the top and the back of the head. So the head looks up by
            # 20 degrees, which shows the face from above; the front row is the star of the
            # drawings, thick enough to be seen edge on; and a back row lies nearly flat behind it,
            # its blades wide as seen from above. From the camera the picture is then laid out as
            # the drawings are: the face below, the fan of spikes above it.
            # (First tries: a ring of blades behind the face that reached under the head. Seen from
            # above and behind, the top was a bald dome and the lower blades hung like tentacles.)
            "HEAD_PITCH_DEG": 20.0,
            "SPIKES": (
                (0.0, 12.0, 0.36, 0.30, 0.15, "front"),
                (38.0, 12.0, 0.36, 0.30, 0.15, "front"),
                (74.0, 12.0, 0.36, 0.30, 0.15, "front"),
                (103.0, 14.0, 0.36, 0.30, 0.15, "front"),
                (0.0, 52.0, 0.34, 0.30, 0.16, "up"),
                (52.0, 52.0, 0.34, 0.30, 0.16, "up"),
                (0.0, 88.0, 0.26, 0.28, 0.17, "up"),
            ),
        },
    },
}


def body_numbers():
    source = {**variants.NUMBERS, **variants.PUSHED}
    scaled = dict(source)
    for name in LENGTHS:
        scaled[name] = source[name] * BODY_SCALE
    scaled["TORSO_RINGS"] = tuple(tuple(value * BODY_SCALE for value in ring) for ring in source["TORSO_RINGS"])
    unknown = set(BODY_CHANGES) - set(source)
    if unknown:
        raise SystemExit(f"CHOG_FULL the body sets numbers that do not exist: {sorted(unknown)}")
    scaled.update(BODY_CHANGES)
    return SimpleNamespace(**scaled)


def head_numbers(style):
    unknown = set(STYLES[style]["head"]) - set(HEAD)
    if unknown:
        raise SystemExit(f"CHOG_FULL style {style} sets numbers that do not exist: {sorted(unknown)}")
    return SimpleNamespace(**{**HEAD, **STYLES[style]["head"]})


# ---------------------------------------------------------------------------------------------
# The head.
# ---------------------------------------------------------------------------------------------

class HeadSurface:
    """Where the surface of the head is: the egg of add_head, in the axes of the part "Head"."""

    def __init__(self, numbers):
        self.middle = Z * numbers.HEAD_MIDDLE
        self.half = Vector((numbers.HEAD_HALF_WIDTH, numbers.HEAD_HALF_DEPTH, numbers.HEAD_HALF_HEIGHT))
        self.face_middle = numbers.FACE_MIDDLE

    def front(self, side, up, lift):
        """The point of the front of the head "side" to the left and "up" above the middle of the
        face, pushed "lift" off the head."""
        x, z = self.face_middle[0] + side, self.face_middle[1] + up
        inside = 1.0 - (x / self.half.x) ** 2 - (z / self.half.z) ** 2
        y = -self.half.y * math.sqrt(max(inside, 0.0004))
        outward = Vector((x / self.half.x ** 2, y / self.half.y ** 2, z / self.half.z ** 2)).normalized()
        return self.middle + Vector((x, y, z)) + outward * lift

    def towards(self, direction):
        """The point of the surface of the head in that direction from its middle."""
        reach = 1.0 / math.sqrt(sum((direction[index] / self.half[index]) ** 2 for index in range(3)))
        return self.middle + direction * reach


def add_head(part, numbers):
    part.shell(base.ball_rings(Z * numbers.HEAD_MIDDLE, Z, X, numbers.HEAD_HALF_HEIGHT, numbers.HEAD_HALF_WIDTH,
                               numbers.HEAD_HALF_DEPTH, numbers.HEAD_STACKS),
               numbers.HEAD_SIDES, SKIN, numbers.OUTLINE_THICKNESS)


def add_skin(part, surface, loops, material, around=True):
    """A thin skin laid on the head: "loops" is a list of rows of points, a row of one point being
    the tip of a fan. "around" says that each row closes on itself (a ring), not a strip with two
    ends. The skin is open, so each face is turned away from the middle of the head."""
    mesh = part.mesh
    rows = [[mesh.verts.new(point) for point in loop] for loop in loops]
    faces = []
    for lower, upper in zip(rows, rows[1:]):
        count = max(len(lower), len(upper))
        for index in range(count if around else count - 1):
            after = (index + 1) % count
            if len(lower) == 1:
                corners = (lower[0], upper[index], upper[after])
            elif len(upper) == 1:
                corners = (lower[index], lower[after], upper[0])
            else:
                corners = (lower[index], lower[after], upper[after], upper[index])
            face = mesh.faces.new(corners)
            face.smooth = True
            faces.append(face)
    for face in faces:
        face.normal_update()
        if face.normal.dot(face.calc_center_median() - surface.middle) < 0.0:
            face.normal_flip()
    part.paint(faces, material)
    part.open_faces.extend(faces)
    return faces


def patch_loops(surface, middle, half_width, half_height, squareness, taper, lift, rings, segments):
    """The rows of a rounded patch on the front of the head: its middle, then rings out to its edge."""
    side, up = middle
    loops = [[surface.front(side, up, lift)]]
    for ring in range(1, rings + 1):
        amount = ring / rings
        loop = []
        for index in range(segments):
            angle = 2.0 * math.pi * index / segments
            cosine, sine = math.cos(angle), math.sin(angle)
            x = half_width * math.copysign(abs(cosine) ** (2.0 / squareness), cosine)
            z = half_height * math.copysign(abs(sine) ** (2.0 / squareness), sine)
            x *= 1.0 + taper * z / half_height
            loop.append(surface.front(side + x * amount, up + z * amount, lift))
        loops.append(loop)
    return loops


def add_face(part, numbers, surface, eyes=True, mouth=True):
    """The cream face with its dark line, and what is drawn on it. Everything is a thin skin on
    the head, each a little further off it than what it lies on. "eyes" and "mouth" say whether the
    generic ones are drawn: make_chog_traits.py leaves them out, a Genesis brings its own."""
    width, height = numbers.FACE_HALF_WIDTH, numbers.FACE_HALF_HEIGHT
    square, taper, lift, line = numbers.FACE_SQUARENESS, numbers.FACE_TAPER, numbers.FACE_LIFT, numbers.FACE_LINE
    rings, segments = numbers.FACE_RINGS, numbers.FACE_SEGMENTS
    add_skin(part, surface, patch_loops(surface, (0.0, 0.0), width + line, height + line, square, taper,
                                        lift * 0.5, rings, segments), OUTLINE)
    add_skin(part, surface, patch_loops(surface, (0.0, 0.0), width, height, square, taper, lift, rings, segments),
             FACE)

    drawn = lift * 1.8
    eye_side, eye_up, eye_width, eye_height = numbers.EYE
    glint_side, glint_up, glint_radius = numbers.EYE_GLINT
    cheek_side, cheek_up, cheek_width, cheek_height = numbers.CHEEK
    for side in (1.0, -1.0):
        if eyes:
            add_skin(part, surface, patch_loops(surface, (side * eye_side, eye_up), eye_width, eye_height, 2.0, 0.0,
                                                drawn, 2, 12), EYE)
            add_skin(part, surface, patch_loops(surface, (side * eye_side + glint_side, eye_up + glint_up),
                                                glint_radius, glint_radius, 2.0, 0.0, lift * 2.4, 1, 8), FACE)
        add_skin(part, surface, patch_loops(surface, (side * cheek_side, cheek_up), cheek_width, cheek_height, 2.0,
                                            0.0, drawn, 1, 12), BLUSH)
    nose_side, nose_up, nose_width, nose_height = numbers.NOSE
    add_skin(part, surface, patch_loops(surface, (nose_side, nose_up), nose_width, nose_height, 2.0, 0.6, drawn, 1, 8),
             BLUSH)

    # The mouth: a thin line that sags in its middle.
    if mouth:
        mouth_side, mouth_up, mouth_width, sag, thick = numbers.MOUTH
        steps = 8
        upper, lower = [], []
        for index in range(steps + 1):
            along = -1.0 + 2.0 * index / steps
            height_here = mouth_up - sag * (1.0 - along * along)
            fine = thick * (0.35 + 0.65 * (1.0 - along * along))
            upper.append(surface.front(mouth_side + along * mouth_width, height_here + fine * 0.5, drawn))
            lower.append(surface.front(mouth_side + along * mouth_width, height_here - fine * 0.5, drawn))
        add_skin(part, surface, [lower, upper], EYE, around=False)

    # The fringe: teeth of hair on the brow, in the skin colour, over the top of the face.
    teeth = numbers.FRINGE_TEETH
    if teeth:
        brow = height * 0.995
        for index in range(teeth):
            middle = (index - (teeth - 1) * 0.5) * numbers.FRINGE_WIDTH
            half = numbers.FRINGE_WIDTH * 0.5
            add_skin(part, surface, [
                [surface.front(middle, brow - numbers.FRINGE_DROP, lift * 1.5)],
                [surface.front(middle - half, brow + line, lift * 1.5),
                 surface.front(middle + half, brow + line, lift * 1.5)],
            ], SKIN, around=False)


def add_crown(part, numbers, surface):
    for turn_deg, back_deg, length, width, thickness, flat in numbers.SPIKES:
        mirrored = turn_deg not in (0.0, 180.0)
        for side in ((1.0, -1.0) if mirrored else (1.0,)):
            turn, back = math.radians(turn_deg) * side, math.radians(back_deg)
            away = Vector((math.sin(turn) * math.cos(back), math.sin(back), math.cos(turn) * math.cos(back)))
            root = surface.towards(away)
            if flat == "round":
                wide, thick = base.frame_across(away, Z if abs(away.z) < 0.9 else X)
            else:
                # The flat sides look the way "looks" points; the blade is wide across that.
                looks = -Y if flat == "front" else Z
                wide = looks.cross(away).normalized()
                thick = away.cross(wide)
            half_wide, half_thick = width * 0.5, thickness * 0.5
            if flat == "round":
                # A cone with a full root and a blunt point.
                profile = ((0.0, 1.00), (0.42, 0.74), (0.78, 0.36), (0.95, 0.12))
                sides, smooth = numbers.SPIKE_CONE_SIDES, True
            else:
                # A blade: widest a third of the way up, as the leaves of the star are drawn, with a
                # ridge along the middle of each flat side.
                profile = ((0.0, 0.92), (0.30, 1.00), (0.66, 0.56), (0.90, 0.18))
                sides, smooth = 4, False
            rings = [(root - away * numbers.SPIKE_SUNK, wide * half_wide * profile[0][1], thick * half_thick * profile[0][1])]
            for along, size in profile[1:]:
                rings.append((root + away * length * along, wide * half_wide * size, thick * half_thick * size))
            rings.append((root + away * length, NOWHERE, NOWHERE))
            part.shell(rings, sides, SKIN, numbers.OUTLINE_THICKNESS, smooth=smooth)


def turn_head_up(part, numbers):
    """Turns everything of the head about the line through its ears, so the face looks up."""
    if not numbers.HEAD_PITCH_DEG:
        return
    middle = Z * numbers.HEAD_MIDDLE
    turn = Matrix.Rotation(-math.radians(numbers.HEAD_PITCH_DEG), 3, "X")
    for point in part.mesh.verts:
        point.co = middle + turn @ (point.co - middle)


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
            material.use_backface_culling = True
            if hasattr(material, "use_backface_culling_shadow"):
                material.use_backface_culling_shadow = True
        materials[name] = material
    return materials


def build(style):
    """Empties the scene and builds the character in it. Returns the root, its four parts, the
    numbers of its body and those of its head."""
    body_n, head_n = body_numbers(), head_numbers(style)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    materials = make_materials()

    root = bpy.data.objects.new(base.ROOT_NAME, None)
    root.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(root)

    body = variants.PartShapes(MATERIAL_ORDER)
    variants.add_torso(body, body_n)
    variants.add_legs_and_feet(body, body_n)

    head = variants.PartShapes(MATERIAL_ORDER)
    surface = HeadSurface(head_n)
    add_head(head, head_n)
    add_crown(head, head_n, surface)
    add_face(head, head_n, surface)
    turn_head_up(head, head_n)

    arms = {}
    for name, side in (("armLeft", 1.0), ("armRight", -1.0)):
        arms[name] = variants.PartShapes(MATERIAL_ORDER)
        variants.add_arm(arms[name], body_n, side)

    shoulder = body_n.SHOULDER_HALF_WIDTH
    parts = [
        body.finish("Body", Vector((0.0, 0.0, 0.0)), root, materials, on_ground=True),
        head.finish("Head", at(0.0, 0.0, NECK_HEIGHT), root, materials),
        arms["armLeft"].finish("armLeft", at(shoulder, 0.0, body_n.SHOULDER_HEIGHT), root, materials),
        arms["armRight"].finish("armRight", at(-shoulder, 0.0, body_n.SHOULDER_HEIGHT), root, materials),
    ]
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)
    bpy.context.view_layer.update()
    return root, parts, body_n, head_n


def report(style, parts, body_n, head_n):
    """Prints what was built and stops if a point of the contract with the game is not met."""
    failures = []
    total = outline_total = 0
    low, high = Vector((1e9, 1e9, 1e9)), Vector((-1e9, -1e9, -1e9))
    for part in parts:
        names = [slot.material.name for slot in part.material_slots]
        triangles = len(part.data.polygons)
        outline = sum(1 for face in part.data.polygons if names[face.material_index] == OUTLINE)
        total += triangles
        outline_total += outline
        part_low, part_high = 1e9, -1e9
        for point in part.data.vertices:
            world = part.matrix_world @ point.co
            low = Vector(map(min, low, world))
            high = Vector(map(max, high, world))
            part_low, part_high = min(part_low, world.z), max(part_high, world.z)
        place = part.matrix_world.translation
        print(f"CHOG_FULL {style} part {part.name:9s} triangles {triangles:5d} (outline {outline:4d})  "
              f"origin (Blender x y z) {place.x:+.3f} {place.y:+.3f} {place.z:+.3f}  materials {names}")
        if any(len(face.vertices) != 3 for face in part.data.polygons):
            failures.append(f"{part.name} has faces that are not triangles")
        if part.name.startswith("arm") and not place.z - part_low > 2.0 * (part_high - place.z):
            failures.append(f"{part.name} does not hang down from its origin")

    size = high - low
    print(f"CHOG_FULL {style} triangles {total} (limit {TRIANGLE_LIMIT}): {total - outline_total} of shapes, "
          f"{outline_total} of outline shells and of the line around the face")
    print(f"CHOG_FULL {style} size, crown and outline included: {size.x:.3f} wide, {size.y:.3f} deep, "
          f"{size.z:.3f} tall; lowest point {low.z:+.4f}")
    head_top = NECK_HEIGHT + head_n.HEAD_MIDDLE + head_n.HEAD_HALF_HEIGHT
    head_tall = 2.0 * head_n.HEAD_HALF_HEIGHT
    print(f"CHOG_FULL {style} the head without its crown is {head_tall:.3f} tall and {2.0 * head_n.HEAD_HALF_WIDTH:.3f} "
          f"wide, on a character {head_top:.3f} tall to the top of the head ({head_tall / head_top:.0%} of it); the "
          f"torso is {max(ring[1] for ring in body_n.TORSO_RINGS):.3f} wide")

    by_name = {part.name: part.matrix_world.translation for part in parts}
    if sorted(by_name) != ["Body", "Head", "armLeft", "armRight"]:
        failures.append(f"the parts are {sorted(by_name)}")
    else:
        if by_name["Body"].length > 1e-6:
            failures.append("the origin of Body is not on the ground, between the feet")
        if (by_name["Head"] - at(0.0, 0.0, NECK_HEIGHT)).length > 1e-6:
            failures.append("the origin of Head is not at the neck")
        left, right = by_name["armLeft"], by_name["armRight"]
        if not (left.x > 0.0 > right.x and abs(left.x + right.x) < 1e-6 and abs(left.z - right.z) < 1e-6
                and abs(left.y) < 1e-6 and abs(right.y) < 1e-6 and left.z < NECK_HEIGHT):
            failures.append("the shoulders are not level, one on each side, under the neck")
        print(f"CHOG_FULL {style} neck (origin of Head) {NECK_HEIGHT:.3f} high; shoulders (origins of the arms) "
              f"{left.x:.3f} from the middle, {left.z:.3f} high")
    if total > TRIANGLE_LIMIT:
        failures.append(f"{total} triangles, the limit is {TRIANGLE_LIMIT}")
    if abs(low.z) > 1e-4:
        failures.append(f"the lowest point is at {low.z:+.4f}, not on the ground")
    reach = (variants.torso_half_width(body_n, body_n.SHOULDER_HEIGHT)
             - (body_n.SHOULDER_HALF_WIDTH - body_n.SHOULDER_RADIUS))
    if reach < 0.02:
        failures.append("the arms do not reach into the torso")

    if failures:
        raise SystemExit(f"CHOG_FULL {style} FAILED: " + "; ".join(failures))
    print(f"CHOG_FULL {style} keeps the contract: four parts, Body on the ground, Head at the neck, each arm "
          "hanging from its shoulder")


def main():
    default_out = os.path.normpath(os.path.join(HERE, "..", "..", ".artifacts", "chog-body", "full"))
    parser = argparse.ArgumentParser(description="Builds a complete Chog character in one head style.")
    parser.add_argument("--style", required=True, choices=sorted(STYLES))
    parser.add_argument("--out", default=None, help="folder for ChogBody.fbx and ChogBody.blend "
                                                    "(default: .artifacts/chog-body/full/<style>)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    folder = os.path.abspath(arguments.out or os.path.join(default_out, arguments.style))

    # Never over the body the game uses, nor over the file it was copied from.
    protected = os.path.normpath(os.path.join(default_out, ".."))
    if os.path.normpath(folder) == protected:
        raise SystemExit(f"CHOG_FULL {folder} holds the body of the game: choose another folder")

    print(f"CHOG_FULL {arguments.style}: {STYLES[arguments.style]['about']}")
    root, parts, body_n, head_n = build(arguments.style)
    report(arguments.style, parts, body_n, head_n)
    blend_path, fbx_path = base.export(root, parts, folder)
    print(f"CHOG_FULL saved {blend_path}")
    print(f"CHOG_FULL saved {fbx_path}")


if __name__ == "__main__":
    main()
