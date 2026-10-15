"""The bases other than Origin (batch 7, 16 October 2026, pictures only): the Skull base first.

What the pictures of the 60 Skull Genesis show (sheets of .artifacts/chog-body/special/, made with
tools/genesis-art/trait_sheet.py Base Skull and Skin ...): a skull in the place of the head, bald and
round, no spike and no cream face, the whole head in one bone colour (white, beige, grey, purple, jade,
blue, metal, black, red or yellow, the value of "Skin"); the pink cheeks of a Chog; a small dark nose
hole; a row of teeth across the lower face in place of the mouth (no Skull Genesis has a "Mouth"); the
eyes of its "Eyes" value drawn as on any Chog; under the head a skeleton, its ribs and spine showing,
when no clothing covers it. Hats sit on the skull as on a head.

make_chog_traits.build() asks here: no leaf of the crown, no teeth of hair on the brow, and in place of
the cream face, add_skull_face(); the body gets add_ribs(). The colours are SKULL_SKINS, by "Skin".
"""

import math

from mathutils import Vector

import make_chog_body as base

X, Y, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))

# Bone colours of the Skull Genesis, read on their pictures (the skull, out of its shade), by "Skin".
SKULL_SKINS = {
    "White": (0.95, 0.95, 0.93),
    "Beige": (0.93, 0.88, 0.76),
    "Grey": (0.62, 0.62, 0.66),
    "Purple": (0.60, 0.53, 0.82),
    "Jade": (0.55, 0.76, 0.64),
    "Blue": (0.46, 0.56, 0.78),
    "Metal": (0.36, 0.38, 0.45),
    "Black": (0.20, 0.20, 0.23),
    "Red": (0.86, 0.36, 0.36),
    "Yellow": (0.95, 0.84, 0.40),
}

# The face of a skull, against the middle of the face of H3 (to the side, above), as add_face places
# its parts: the teeth where the mouth would be, wider; the nose hole between the eyes and the teeth.
TEETH = {"MIDDLE_UP": -0.094, "HALF_WIDTH": 0.108, "HALF_HEIGHT": 0.038, "COUNT": 6, "GAP": 0.007}
NOSE = {"UP": -0.035, "HALF_WIDTH": 0.020, "HALF_HEIGHT": 0.016}

# Ribs: how many, from which height of the torso to which (shares of its height), how far around the
# front they go (degrees either side of straight ahead), their thickness.
RIBS = {"COUNT": 5, "FROM": 0.34, "TO": 0.80, "SPREAD_DEG": 64.0, "THICK": 0.014}


def is_skull(traits):
    return bool(traits) and traits.get("Base") == "Skull"


def skin_of(traits):
    """The bone colour of a Skull Genesis: the skin and the face both."""
    colour = SKULL_SKINS.get(traits.get("Skin"))
    if colour is None:
        raise SystemExit(f"CHOG_TRAITS no bone colour recorded for a Skull of Skin={traits.get('Skin')}")
    return {"skin": colour, "face": colour}


def add_skull_face(part, numbers, surface, full, draw_shape, oval, SKIN, EYE, BLUSH, FACE):
    """The cheeks, the nose hole and the teeth on the skull. The skull has no cream face: the bone of the
    head is its face."""
    lift = numbers.FACE_LIFT
    drawn = lift * 1.8
    cheek_side, cheek_up, cheek_width, cheek_height = numbers.CHEEK
    for side in (1.0, -1.0):
        full.add_skin(part, surface, full.patch_loops(surface, (side * cheek_side, cheek_up), cheek_width, cheek_height,
                                                      2.0, 0.0, drawn, 1, 12), BLUSH)

    # The nose hole: an upside-down heart, two lobes and a point.
    up, half_w, half_h = NOSE["UP"], NOSE["HALF_WIDTH"], NOSE["HALF_HEIGHT"]
    for side in (1.0, -1.0):
        draw_shape(part, surface, EYE, oval((side * half_w * 0.5, up + half_h * 0.3), half_w * 0.62, half_h * 0.7, 10),
                   drawn)
    draw_shape(part, surface, EYE, [(-half_w, up + half_h * 0.2), (half_w, up + half_h * 0.2), (0.0, up - half_h * 1.3)],
               drawn)

    # The teeth: a dark band, then the teeth on it in the bone colour, the gaps between them dark.
    middle, half_width, half_height = TEETH["MIDDLE_UP"], TEETH["HALF_WIDTH"], TEETH["HALF_HEIGHT"]
    band = []
    for index in range(16):
        angle = 2.0 * math.pi * index / 16
        band.append((math.cos(angle) * (half_width + 0.008) * min(1.0, 1.15 * abs(math.cos(angle)) + 0.35),
                     middle + math.sin(angle) * (half_height + 0.008)))
    draw_shape(part, surface, EYE, band, drawn, middle=(0.0, middle))
    count, gap = TEETH["COUNT"], TEETH["GAP"]
    width = (2.0 * half_width - gap * (count - 1)) / count
    for index in range(count):
        left = -half_width + index * (width + gap)
        # The teeth at the ends are shorter: the row follows the jaw.
        shorten = 0.25 if index in (0, count - 1) else 0.0
        top, bottom = middle + half_height * (1.0 - shorten), middle - half_height * (1.0 - shorten)
        tooth = [(left, bottom), (left + width, bottom), (left + width, top), (left, top)]
        draw_shape(part, surface, SKIN, tooth, lift * 2.6)


def torso_section(numbers, height):
    """Half width and half depth of the torso at a height (TORSO_RINGS: height, width, depth)."""
    rings = numbers.TORSO_RINGS
    if height <= rings[0][0]:
        return rings[0][1] * 0.5, rings[0][2] * 0.5
    for lower, upper in zip(rings, rings[1:]):
        if height <= upper[0]:
            share = (height - lower[0]) / (upper[0] - lower[0])
            return (lower[1] + (upper[1] - lower[1]) * share) * 0.5, (lower[2] + (upper[2] - lower[2]) * share) * 0.5
    return rings[-1][1] * 0.5, rings[-1][2] * 0.5


def add_ribs(part, numbers, material):
    """The ribs and the spine of a skeleton, as dark lines around the front of the torso (and the back for
    the spine), a little off it, so the torso in its bone colour reads as a rib cage."""
    rings = numbers.TORSO_RINGS
    bottom, top = rings[0][0], rings[-1][0]
    thick = RIBS["THICK"]
    for index in range(RIBS["COUNT"]):
        share = RIBS["FROM"] + (RIBS["TO"] - RIBS["FROM"]) * index / max(1, RIBS["COUNT"] - 1)
        height = bottom + (top - bottom) * share
        half_w, half_d = torso_section(numbers, height)
        points = []
        spread = math.radians(RIBS["SPREAD_DEG"] + 6.0 * index)
        for step in range(8):
            # Around the front (-Y), from one side to the other, dipping a little at the middle.
            angle = -spread + 2.0 * spread * step / 7
            dip = 0.018 * math.cos(angle * math.pi / (2.0 * spread))
            points.append(Vector((math.sin(angle) * half_w * 1.03, -math.cos(angle) * half_d * 1.03, height - dip)))
        rings_of = []
        for step, point in enumerate(points):
            ahead = points[min(step + 1, 7)] - points[max(step - 1, 0)]
            first, second = base.frame_across(ahead, Z)
            rings_of.append((point, first * thick, second * thick))
        part.shell(rings_of, 4, material, 0.0)
    # The breastbone, down the middle of the front.
    low, high = bottom + (top - bottom) * (RIBS["FROM"] - 0.05), bottom + (top - bottom) * (RIBS["TO"] + 0.04)
    stations = []
    for step in range(5):
        height = low + (high - low) * step / 4
        _, half_d = torso_section(numbers, height)
        stations.append(Vector((0.0, -half_d * 1.035, height)))
    shell = []
    for step, point in enumerate(stations):
        shell.append((point, X * thick * 1.2, Y * thick * 0.8))
    part.shell(shell, 6, material, 0.0)
