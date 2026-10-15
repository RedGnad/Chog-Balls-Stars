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


# The Radioactive base (30 Genesis, all of Side=Left): the spiky Chog, its colour running from a light
# top to a deeper bottom as if lit from inside (the "Skin" value: Green, Blue, Multi Colored, Red), no
# cream face, the teeth and nose of a skull showing on the lower face, the ribs through the body, and
# eyes that glow in a pale tint of the skin. (bottom, top) colours, read on their pictures.
RADIOACTIVE_SKINS = {
    "Green": ((0.30, 0.78, 0.22), (0.92, 0.98, 0.30)),
    "Blue": ((0.16, 0.48, 0.90), (0.48, 0.93, 0.98)),
    "Multi Colored": ((0.42, 0.86, 0.42), (0.98, 0.56, 0.84)),
    "Red": ((0.86, 0.22, 0.42), (0.99, 0.58, 0.72)),
}
# The tint the eyes of each colour glow in (the white of an eye of the collection, in that colour).
RADIOACTIVE_EYE_TINTS = {
    "Green": (0.95, 1.0, 0.50),
    "Blue": (0.82, 0.98, 1.0),
    "Red": (1.0, 0.86, 0.90),
    "Mixed": (1.0, 0.96, 0.86),
}
# The eyes of the radioactive pictures are those of Side=Left in a colour: value word -> eye value drawn.
RADIOACTIVE_EYE_KINDS = {"Round Eye": "Round Eye", "Retard": "Retard", "Side Eye": "Side Eye", "Happy": "Happy",
                         "Smirk": "Smirk", "Flat": "Flat"}


def radioactive_eyes(table):
    """Entries for "<colour> <kind>": the entry of the plain kind, its whites in the glowing tint."""
    made = {}
    for colour, tint in RADIOACTIVE_EYE_TINTS.items():
        for word, plain in RADIOACTIVE_EYE_KINDS.items():
            entry = table.get(plain)
            if not isinstance(entry, dict):
                continue
            colours = {key: (tint if key == "White" else value) for key, value in entry["colours"].items()}
            made[f"{colour} {word}"] = {**entry, "colours": colours}
    return made


def is_radioactive(traits):
    return bool(traits) and traits.get("Base") == "Radioactive"


def is_skull(traits):
    return bool(traits) and traits.get("Base") == "Skull"


def skin_of(traits):
    """The colours of a Genesis on a base other than Origin: the skin and the face both; a radioactive one
    also the colour its skin runs to at the top ("top")."""
    if is_radioactive(traits):
        colours = RADIOACTIVE_SKINS.get(traits.get("Skin"))
        if colours is None:
            raise SystemExit(f"CHOG_TRAITS no colours recorded for a Radioactive of Skin={traits.get('Skin')}")
        return {"skin": colours[0], "face": colours[0], "top": colours[1]}
    colour = SKULL_SKINS.get(traits.get("Skin"))
    if colour is None:
        raise SystemExit(f"CHOG_TRAITS no bone colour recorded for a Skull of Skin={traits.get('Skin')}")
    return {"skin": colour, "face": colour}


def add_skull_face(part, numbers, surface, full, draw_shape, oval, SKIN, EYE, BLUSH, FACE, cheeks=True):
    """The cheeks, the nose hole and the teeth on the skull. The skull has no cream face: the bone of the
    head is its face. A radioactive head draws no cheek."""
    lift = numbers.FACE_LIFT
    drawn = lift * 1.8
    cheek_side, cheek_up, cheek_width, cheek_height = numbers.CHEEK
    for side in ((1.0, -1.0) if cheeks else ()):
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


# ---------------------------------------------------------------------------------------------
# What the Radioactive Genesis wear on the head (their values of "Head"), read on their pictures
# (.artifacts/chog-body/special/skins.png): ram horns curling back (Red Horn, Purple Horn), ridged horns
# standing up (Demon Horn, Void Horn, devil horn), a third eye on the brow, a blue gem on the brow
# (Diamond), a gold seal on the top (Holy Seal), a beam going up from the brow (Blue Laser), mushrooms
# (Poison Shroom). The whole crown stays: they stand among the spikes.
# ---------------------------------------------------------------------------------------------

FULL = ((1.0, None, None),) * 7


def _out(surface, side, up):
    return (surface.front(side, up, 0.01) - surface.front(side, up, 0.0)).normalized()


def build_ram_horns(maker):
    """Two horns from the top of the head, curling back, down and forward beside it."""
    for side in (1.0, -1.0):
        part = maker.part()
        root = maker.surface.towards(Vector((0.62 * side, 0.0, 0.78)).normalized())
        radius0 = 0.080
        centre = root - Z * radius0 + X * side * 0.01
        rings = []
        for step in range(15):
            t = step / 14.0
            angle = t * 1.15 * math.pi
            r = radius0 * (1.0 - 0.45 * t)
            point = centre + Z * r * math.cos(angle) + Y * r * math.sin(angle) + X * side * (0.15 * t)
            ahead = -Z * math.sin(angle) + Y * math.cos(angle) + X * side * 0.15
            first, second = base.frame_across(ahead, X)
            thick = 0.046 * (1.0 - 0.70 * t) * (1.08 if step % 2 == 0 else 0.94)
            rings.append((point, first * thick, second * thick))
        rings.append((rings[-1][0] + (rings[-1][0] - rings[-2][0]) * 0.6, Vector(), Vector()))
        part.shell(rings, 8, part.named("Horn"), 0.012)


def build_ridged_horns(maker):
    """Two horns standing up and out, ridged, slightly bent outwards."""
    for side in (1.0, -1.0):
        part = maker.part()
        away = Vector((0.55 * side, 0.08, 0.83)).normalized()
        root = maker.surface.towards(away) - away * 0.03
        rings = []
        for step in range(9):
            t = step / 8.0
            point = root + away * 0.24 * t + X * side * 0.05 * t * t
            first, second = base.frame_across(away, X)
            thick = 0.052 * (1.0 - 0.85 * t) * (1.12 if step % 2 == 1 else 0.96)
            rings.append((point, first * thick, second * thick))
        rings.append((root + away * 0.265 + X * side * 0.05, Vector(), Vector()))
        part.shell(rings, 8, part.named("Horn"), 0.012)


def build_third_eye(maker):
    import make_chog_traits as traits_module
    part, surface = maker.part(), maker.surface
    lift = 0.026
    traits_module.draw_shape(part, surface, part.named("Dark"), traits_module.oval((0.0, 0.135), 0.040, 0.026, 16), lift)
    traits_module.draw_shape(part, surface, part.named("White"), traits_module.oval((0.0, 0.135), 0.032, 0.019, 16),
                             lift * 1.3)
    traits_module.draw_shape(part, surface, part.named("Dark"), traits_module.oval((0.0, 0.134), 0.011, 0.016, 10),
                             lift * 1.6)


def build_gem(maker):
    part, surface = maker.part(), maker.surface
    out = _out(surface, 0.0, 0.13)
    centre = surface.front(0.0, 0.13, 0.012)
    first, second = base.frame_across(out, Z)
    rings = [(centre - out * 0.004, first * 0.030, second * 0.036), (centre + out * 0.014, first * 0.034, second * 0.040),
             (centre + out * 0.026, first * 0.020, second * 0.024), (centre + out * 0.030, Vector(), Vector())]
    part.shell(rings, 6, part.named("Gem"), 0.010, smooth=False)


def build_seal(maker):
    part, surface = maker.part(), maker.surface
    top = surface.towards(Vector((0.0, 0.05, 1.0)).normalized())
    for radius, tube, key in ((0.075, 0.016, "Gold"), (0.042, 0.010, "Gold")):
        rings = []
        for step in range(25):
            angle = 2.0 * math.pi * step / 24
            outward = X * math.cos(angle) + Y * math.sin(angle)
            rings.append((top + Z * 0.03 + outward * radius, Z * tube, outward * tube))
        part.shell(rings, 6, part.named(key), 0.010)
    for step in range(4):
        angle = math.pi * 0.5 * step + math.pi * 0.25
        outward = X * math.cos(angle) + Y * math.sin(angle)
        stations = [(0.0, 0.016), (0.05, 0.0)]
        part.shell(base.tube_rings(top + Z * 0.03 + outward * 0.085, outward, Z, stations), 6, part.named("Gold"), 0.010)


def build_head_laser(maker):
    part, surface = maker.part(), maker.surface
    start = surface.front(0.0, 0.13, 0.0)
    up = (Z * 0.9 - Y * 0.4).normalized()
    part.shell(base.tube_rings(start, up, X, [(0.0, 0.018), (0.45, 0.016), (0.47, 0.0)]), 8, part.named("Beam"), 0.0)


def build_mushrooms(maker):
    for side, lean, height, cap in ((0.35, 0.25, 0.15, 0.10), (-0.25, -0.2, 0.11, 0.075)):
        part = maker.part()
        away = Vector((side, 0.15, 0.9)).normalized()
        root = maker.surface.towards(away) - away * 0.02
        stem = (away + X * lean).normalized()
        part.shell(base.tube_rings(root, stem, X, [(0.0, 0.018), (height, 0.016)]), 8, part.named("Stem"), 0.010)
        top = root + stem * height
        part.shell(base.ball_rings(top + stem * cap * 0.2, stem, X, cap * 0.55, cap, cap, 6), 12, part.named("Cap"), 0.012)


HAT_SHAPES = {"RamHorns": build_ram_horns, "RidgedHorns": build_ridged_horns, "ThirdEye": build_third_eye,
              "Gem": build_gem, "Seal": build_seal, "HeadLaser": build_head_laser, "Mushrooms": build_mushrooms}
# The lines of the crown (make_chog_traits.SPIKES: 0 top, 1 upper pair, 2 middle pair, 3 lowest pair, 4 top of
# the back row, 5 back pair, 6 straight back) that an object stands in the place of are left out: the
# horns grow where those spikes would (the build refuses a leaf through an object).
GONE_LINE = (0.0, None, None)


def _crown_without(*lines):
    return tuple(GONE_LINE if line in lines else FULL[line] for line in range(7))


HAT_NUMBERS = {name: {"LEAVES": FULL, "OUTLINE_THICKNESS": 0.012} for name in HAT_SHAPES}
HAT_NUMBERS["RidgedHorns"]["LEAVES"] = _crown_without(0, 1, 2)
HAT_NUMBERS["RamHorns"]["LEAVES"] = _crown_without(0, 1, 2, 3)
# The ram horns curl down the sides, where the side spikes were: tips are looked for behind only.
HAT_NUMBERS["RamHorns"]["SECTORS"] = (135, -135, 180)
HAT_NUMBERS["Seal"]["LEAVES"] = _crown_without(0, 1)
HAT_NUMBERS["Mushrooms"]["LEAVES"] = _crown_without(0, 1)
HATS = {
    "Red Horn": {"shape": "RamHorns", "colours": {"Horn": (0.62, 0.30, 0.24)}},
    "Purple Horn": {"shape": "RamHorns", "colours": {"Horn": (0.56, 0.42, 0.86)}},
    "Demon Horn": {"shape": "RidgedHorns", "colours": {"Horn": (0.95, 0.52, 0.32)}},
    "Void Horn": {"shape": "RidgedHorns", "colours": {"Horn": (0.52, 0.36, 0.86)}},
    "devil horn": {"shape": "RidgedHorns", "colours": {"Horn": (0.90, 0.26, 0.26)}},
    "3rd Eye": {"shape": "ThirdEye", "colours": {"Dark": (0.07, 0.05, 0.10), "White": (0.98, 0.97, 0.94)},
                "unlit": ("Dark", "White")},
    "Diamond": {"shape": "Gem", "colours": {"Gem": (0.36, 0.76, 0.98)}, "unlit": ("Gem",)},
    "Holy Seal": {"shape": "Seal", "colours": {"Gold": (0.98, 0.80, 0.22)}},
    "Blue Laser": {"shape": "HeadLaser", "colours": {"Beam": (0.55, 0.92, 1.0)}, "unlit": ("Beam",)},
    "Poison Shroom": {"shape": "Mushrooms", "colours": {"Stem": (0.95, 0.92, 0.85), "Cap": (0.62, 0.30, 0.82)}},
}
