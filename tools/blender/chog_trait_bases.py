"""The bases other than Origin (batch 7, 15 October 2026, pictures only): the Skull base first.

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
from types import SimpleNamespace

from mathutils import Vector

import chog_trait_hats as hats_module
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


# The skull's jaw, read on the Skull pictures (CHOG #81, #846, #1000, #1104, #1207, #1642): a wide round
# cranium over a jaw much narrower than it, the outline stepping in at the cheekbones, by the nose; the
# row of teeth is about as wide as the jaw. Heights in halves of the head's height from its middle (-1 its
# bottom): the head narrows from FROM down to TO, by NARROW of its width. (First: the round ball of the
# Chog with teeth drawn on it, a face more than a skull.)
JAW = {"FROM": -0.30, "TO": -0.45, "NARROW": 0.38}


def narrow_jaw(part, numbers):
    """Narrows the lower head of a skull into its jaw: every point of the part (the head and what is drawn
    on it), before its outline is made, so the teeth and the nose stay on it. The eyes and anything worn
    higher are not touched."""
    for point in part.mesh.verts:
        height = (point.co.z - numbers.HEAD_MIDDLE) / numbers.HEAD_HALF_HEIGHT
        share = min(1.0, max(0.0, (JAW["FROM"] - height) / (JAW["FROM"] - JAW["TO"])))
        share = share * share * (3.0 - 2.0 * share)
        point.co.x *= 1.0 - JAW["NARROW"] * share


def bone_hands(parts, materials, skin, glove):
    """A skeleton's hands and feet are bone, not the pink mittens of a Chog: the material of the gloves
    becomes the skin (the bone colour) on the body and the arms."""
    for part in parts:
        for index, slot in enumerate(part.data.materials):
            if slot is not None and slot.name == glove:
                part.data.materials[index] = materials[skin]


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
# (Diamond), a gold seal above the brow (Holy Seal), a beam going up from the brow (Blue Laser), mushrooms
# (Poison Shroom). The whole crown stays: they stand among the spikes. (Until 16 October 2026 the horns
# grew out of the top of the skull, where the spikes of the crown were, and those spikes were taken out:
# the owner saw horns on the crown where the pictures put them on the brow, and a head without its
# spikes. They now grow from the front of the head, before the crown, which keeps every spike.)
# ---------------------------------------------------------------------------------------------

FULL = ((1.0, None, None),) * 7


def _out(surface, side, up):
    return (surface.front(side, up, 0.01) - surface.front(side, up, 0.0)).normalized()


RAM_HORN = {
    # Read on CHOG #275, #473, #672, #1067, #1200 (Red Horn) and #31, #1745 (Purple Horn): each horn is
    # about as thick at its root as a sixth of the head, rises well above the crown, then curls out to the
    # side and down, a little to the back; its rings show as ridges. (First: a curl of 0.08 and a thickness
    # of 0.046, small horns beside the pictures; then curled back, hidden from the front.)
    # Where each horn grows: at the upper corner of the brow, in the coordinates of the face (to the side,
    # above its middle), sunk this far into the head. The pictures draw the root there, in front of the
    # spikes, which show all around (16 October 2026; the root was on the top of the head until then).
    "ROOT": (0.18, 0.19),
    "SUNK": 0.02,
    # From the brow it first rises straight up this far, so that the curl stands above the head, as drawn.
    "RISE": 0.13,
    "RISE_RIDGES": 4,
    "CURL": 0.135,            # radius of the curl where it starts; it tightens to TIGHTEN of it at the tip
    "TIGHTEN": 0.62,
    "SWEEP_DEG": 205.0,       # how far it turns, from straight up
    "THICK": 0.078,           # half its thickness at the root
    "TIP": 0.34,              # share of THICK left at the tip
    "OUT": 0.10,              # how far the tip drifts out, sideways
    "BACK": 0.35,             # the curl goes out to the side, turned this much towards the back
    # Rings along the curl and points around each (22 and 10 until 16 October 2026: with the whole crown
    # kept, #31 went over 8,000 triangles).
    "RIDGES": 14,
    "SIDES": 8,
}


def build_ram_horns(maker):
    """Two big ridged horns from the upper corners of the brow, rising up, then curling out to the side and
    down, as the pictures draw them (RAM_HORN)."""
    numbers = SimpleNamespace(**RAM_HORN)
    for side in (1.0, -1.0):
        part = maker.part()
        across, up = numbers.ROOT
        root = maker.surface.front(across * side, up, -numbers.SUNK)
        back = (X * side + Y * numbers.BACK).normalized()
        across = back.cross(Z).normalized()     # square to the plane of the curl
        rings = []
        # The straight rise, its ridges as thick as those at the start of the curl.
        for step in range(numbers.RISE_RIDGES):
            first, second = base.frame_across(Z, across)
            thick = numbers.THICK * (1.12 if step % 2 == 0 else 0.88)
            rings.append((root + Z * numbers.RISE * step / numbers.RISE_RIDGES, first * thick, second * thick))
        start = root + Z * numbers.RISE
        centre = start + back * numbers.CURL
        sweep = math.radians(numbers.SWEEP_DEG)
        for step in range(numbers.RIDGES + 1):
            t = step / numbers.RIDGES
            angle = t * sweep
            r = numbers.CURL * (1.0 - (1.0 - numbers.TIGHTEN) * t)
            point = centre - back * r * math.cos(angle) + Z * r * math.sin(angle) + X * side * numbers.OUT * t * t
            ahead = (back * math.sin(angle) + Z * math.cos(angle) + X * side * numbers.OUT * 2.0 * t).normalized()
            first, second = base.frame_across(ahead, across)
            thick = numbers.THICK * (1.0 - (1.0 - numbers.TIP) * t) * (1.12 if (step + numbers.RISE_RIDGES) % 2 == 0
                                                                       else 0.88)
            rings.append((point, first * thick, second * thick))
        rings.append((rings[-1][0] + (rings[-1][0] - rings[-2][0]) * 0.7, Vector(), Vector()))
        part.shell(rings, numbers.SIDES, part.named("Horn"), 0.012)


DEMON_HORNS = {
    # Read on CHOG #343, #625, #1460 (Demon Horn), #140, #1222 (Void Horn) and #1225 (devil horn): five
    # ridged horns among the spikes, three on the top of the brow (one in the middle, one each side) and
    # one low on each side of the head, curving up. Each: where its root is on the face (to the side, above
    # its middle; mirrored when the side is not 0), how far it leans out against straight up (0 up, 1 as
    # the head faces there), its length, its half thickness at the root, how far its tip bends up.
    "HORNS": (
        ((0.00, 0.27), 0.45, 0.17, 0.050, 0.00),
        ((0.17, 0.24), 0.55, 0.17, 0.050, 0.02),
        ((0.27, 0.06), 0.90, 0.16, 0.050, 0.08),
    ),
    "SUNK": 0.025,
    "RINGS": 8,
    "SIDES": 6,                      # (7 on the first try: #1222, in a hoodie, went 31 triangles over 8,000)
}


def build_demon_horns(maker):
    """Five ridged horns standing among the spikes (DEMON_HORNS); the crown keeps every spike. A value may
    give its own horns ("horns" of its entry, as HORNS: the 1:1 devil has one pair)."""
    numbers = SimpleNamespace(**DEMON_HORNS)
    surface = maker.surface
    for (across, up), out, length, thick, bend in maker.entry.get("horns", numbers.HORNS):
        for side in ((1.0,) if across == 0.0 else (1.0, -1.0)):
            part = maker.part()
            root = surface.front(across * side, up, -numbers.SUNK)
            facing = _out(surface, across * side, up)
            away = (Z * (1.0 - out) + facing * out).normalized()
            rings = []
            for step in range(numbers.RINGS + 1):
                t = step / numbers.RINGS
                point = root + away * length * t + Z * length * bend * t * t
                ahead = (away + Z * 2.0 * bend * t).normalized()
                first, second = base.frame_across(ahead, X)
                size = thick * (1.0 - 0.85 * t) * (1.12 if step % 2 == 1 else 0.96)
                rings.append((point, first * size, second * size))
            rings.append((root + away * length * 1.1 + Z * length * bend * 1.2, Vector(), Vector()))
            part.shell(rings, numbers.SIDES, part.named("Horn"), 0.012)


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


HOLY_SEAL = {
    # Read on CHOG #415, #743 and #1813 (Holy Seal): a large gold wheel standing upright above the brow,
    # in front of the spikes, its lower edge over the top of the face; a lattice inside it and short rays
    # around it, each crossed near its tip. (Until 16 October 2026: a small ring lying on the top of the
    # skull, the upper spikes taken out.) Where it stands: above the middle of the face, then raised and
    # brought ahead of that point; how far it leans back (from the camera of the game it is seen better).
    "UP": 0.27,
    "RAISE": 0.085,
    "AHEAD": 0.075,
    "LEAN_BACK_DEG": 15.0,
    "RADIUS": 0.115,
    "TUBE": 0.014,
    "SEGMENTS": 16,
    "BARS": (-0.05, 0.0, 0.05),      # the lattice: bars across and up, at these distances from the middle
    "BAR": 0.007,
    "RAYS": 8,
    "RAY": (0.055, 0.014),           # length and half width at the root
    "CROSS": (0.70, 0.022, 0.006),   # the bar across a ray: where along it, half length, radius
}


def build_seal(maker):
    """Holy Seal (HOLY_SEAL): a gold wheel standing on the brow, before the crown, which keeps every spike."""
    numbers = SimpleNamespace(**HOLY_SEAL)
    part, surface = maker.part(), maker.surface
    lean = math.radians(numbers.LEAN_BACK_DEG)
    facing = (-Y * math.cos(lean) + Z * math.sin(lean)).normalized()
    upward = (Z * math.cos(lean) + Y * math.sin(lean)).normalized()
    centre = surface.front(0.0, numbers.UP, 0.0) + Z * numbers.RAISE - Y * numbers.AHEAD
    gold = part.named("Gold")
    rings = []
    for step in range(numbers.SEGMENTS + 1):
        angle = 2.0 * math.pi * step / numbers.SEGMENTS
        outward = X * math.cos(angle) + upward * math.sin(angle)
        rings.append((centre + outward * numbers.RADIUS, outward * numbers.TUBE, facing * numbers.TUBE))
    part.shell(rings, 5, gold, 0.010)
    for offset in numbers.BARS:
        reach = math.sqrt(numbers.RADIUS ** 2 - offset ** 2)
        for along, across in ((X, upward), (upward, X)):
            start = centre + across * offset - along * reach
            part.shell(base.tube_rings(start, along, facing, [(0.0, numbers.BAR), (2.0 * reach, numbers.BAR)]), 4,
                       gold, 0.0)
    length, wide = numbers.RAY
    where, half, radius = numbers.CROSS
    for step in range(numbers.RAYS):
        angle = 2.0 * math.pi * step / numbers.RAYS + math.pi * 0.5
        outward = X * math.cos(angle) + upward * math.sin(angle)
        root = centre + outward * (numbers.RADIUS + numbers.TUBE * 0.5)
        part.shell(base.tube_rings(root, outward, facing, [(0.0, wide), (length * 0.55, wide * 0.8), (length, 0.0)]),
                   5, gold, 0.010)
        sideways = outward.cross(facing).normalized()
        middle = root + outward * length * where
        part.shell(base.tube_rings(middle - sideways * half, sideways, facing, [(0.0, radius), (2.0 * half, radius)]),
                   4, gold, 0.0)


HEAD_LASER = {
    # Read on CHOG #772 (Blue Laser, the only one): a wide pale blue beam leaving the middle of the brow from
    # a white glow, straight out of the head. (A thin beam straight up until 16 October 2026, which the bust
    # showed leaning the other way; then one at 45 degrees to the side, which the owner saw slanting along
    # the head where the picture has it come straight out of it.)
    "AT": 0.13,                       # above the middle of the face
    "UP": 0.12,                       # how far it is turned up from straight out of the head
    "BEAM": (0.026, 0.47),            # radius, length
    "GLOW": (0.040, 0.026),           # the white glow on the brow where it leaves: half width, half height
}


def build_head_laser(maker):
    """Blue Laser (HEAD_LASER): the beam of the brow and its glow."""
    numbers = SimpleNamespace(**HEAD_LASER)
    part, surface = maker.part(), maker.surface
    start = surface.front(0.0, numbers.AT, 0.0)
    way = (_out(surface, 0.0, numbers.AT) + Z * numbers.UP).normalized()
    radius, length = numbers.BEAM
    part.shell(base.tube_rings(start, way, Z, [(0.0, radius * 0.8), (0.04, radius), (length - 0.02, radius),
                                                (length, 0.0)]), 8, part.named("Beam"), 0.0)
    import make_chog_traits as traits_module
    wide, high = numbers.GLOW
    traits_module.draw_shape(part, surface, part.named("Glow"), traits_module.oval((0.0, numbers.AT), wide, high, 14),
                             0.026)


POISON_SHROOM = {
    # Read on CHOG #1100 (Poison Shroom, the only one): mushrooms growing out of the head among the spikes,
    # pale green caps spotted orange on pale stems: a tall one at the top (on the right of the character), a
    # smaller one leaning out at the top on its left, a small orange one spotted green low on its right side.
    # (Until 16 October 2026: two purple caps on the top of the skull, the upper spikes taken out.) Each:
    # where its root is on the face (to the side, above its middle), how far it leans out (0 up, 1 as the
    # head faces there), the height of its stem, its radius, the radius of its cap, whether the cap is the
    # orange one.
    "SHROOMS": (
        ((-0.05, 0.27), 0.20, 0.17, 0.022, 0.110, False),
        ((0.18, 0.23), 0.55, 0.12, 0.018, 0.085, False),
        ((-0.27, 0.06), 0.85, 0.07, 0.014, 0.055, True),
    ),
    "SUNK": 0.02,
    "CAP_SIDES": 10,
    "SPOTS": 5,                      # around each cap, halfway down it
    "SPOT": (0.016, 0.006),          # half width and half thickness of a spot, against a cap of 0.1
}


def build_mushrooms(maker):
    """Poison Shroom (POISON_SHROOM): spotted mushrooms before the crown, which keeps every spike."""
    numbers = SimpleNamespace(**POISON_SHROOM)
    surface = maker.surface
    for (across, up), out, height, stem_radius, cap, orange in numbers.SHROOMS:
        part = maker.part()
        root = surface.front(across, up, -numbers.SUNK)
        stem = (Z * (1.0 - out) + _out(surface, across, up) * out).normalized()
        part.shell(base.tube_rings(root, stem, X, [(0.0, stem_radius * 1.15), (height, stem_radius)]), 8,
                   part.named("Stem"), 0.010)
        top = root + stem * height
        cap_key, spot_key = ("Spot", "Cap") if orange else ("Cap", "Spot")
        part.shell(base.ball_rings(top + stem * cap * 0.2, stem, X, cap * 0.55, cap, cap, 6), numbers.CAP_SIDES,
                   part.named(cap_key), 0.012)
        first, second = base.frame_across(stem, X)
        wide, thick = numbers.SPOT
        for step in range(numbers.SPOTS):
            angle = 2.0 * math.pi * step / numbers.SPOTS + 0.4
            polar = math.radians(55.0 if step % 2 == 0 else 35.0)
            radial = first * math.cos(angle) + second * math.sin(angle)
            normal = (stem * math.cos(polar) * 1.8 + radial * math.sin(polar)).normalized()
            point = (top + stem * cap * 0.2 + stem * cap * 0.55 * math.cos(polar) + radial * cap * math.sin(polar)
                     + normal * thick * 0.5 * cap / 0.1)
            size = cap / 0.1
            part.shell(base.ball_rings(point, normal, radial, thick * size, wide * size, wide * size, 3), 6,
                       part.named(spot_key), 0.0)


BANDANA_KNOT = {
    # Read on CHOG #874 and #1192 (Red Bandana): the band of a headband over the brow, tied in a knot on the
    # upper front of the head, two short flaps standing out of the knot.
    "WHERE": (0.42, -0.30, 0.86),     # direction from the middle of the head (side, front, up), side to the left
    "KNOT": 0.034,                    # radius of the knot
    "FLAP": (0.080, 0.034, 0.012),    # half length, half width, half thickness of each flap
    "FLAP_SPREAD_DEG": 38.0,          # each flap from straight out of the head
}


def build_bandana(maker):
    """Red Bandana: the band of a headband (chog_trait_hats.build_headband) and its knot with two flaps."""
    hats_module.build_headband(maker)
    numbers = SimpleNamespace(**BANDANA_KNOT)
    part = maker.part()
    out = Vector(numbers.WHERE).normalized()
    knot = maker.surface.towards(out) + out * numbers.KNOT * 0.6
    part.shell(base.ball_rings(knot, out, X, numbers.KNOT, numbers.KNOT * 1.15, numbers.KNOT, 6), 10,
               part.named("Band"), 0.012)
    sideways = out.cross(Z).normalized()
    for side in (1.0, -1.0):
        turn = math.radians(numbers.FLAP_SPREAD_DEG) * side
        along = (out * math.cos(turn) + sideways * math.sin(turn)).normalized()
        middle = knot + along * (numbers.KNOT + numbers.FLAP[0] * 0.8)
        length, width, thick = numbers.FLAP
        part.shell(base.ball_rings(middle, along, Z, length, width, thick, 6), 10, part.named("Band"), 0.012)


HAT_SHAPES = {"RamHorns": build_ram_horns, "Bandana": build_bandana, "DemonHorns": build_demon_horns,
              "ThirdEye": build_third_eye,
              "Gem": build_gem, "Seal": build_seal, "HeadLaser": build_head_laser, "Mushrooms": build_mushrooms}
# Every one of these stands before the crown, which keeps all its spikes (the build refuses a leaf through an
# object, so a part that grew into a spike would fail its Genesis).
HAT_NUMBERS = {name: {"LEAVES": FULL, "OUTLINE_THICKNESS": 0.012} for name in HAT_SHAPES}
# The bandana lies where a headband does (chog_trait_hats.HEADBAND), under the whole crown, as the headbands
# do since 16 October 2026: its crossings with the leaves are kept, not refused.
HAT_NUMBERS["Bandana"] = {**hats_module.HEADBAND, "LEAVES": FULL}
HATS = {
    "Red Bandana": {"shape": "Bandana", "colours": {"Band": (0.86, 0.20, 0.18), "Line": (0.62, 0.11, 0.12)}},
    "Red Horn": {"shape": "RamHorns", "colours": {"Horn": (0.62, 0.30, 0.24)}},
    "Purple Horn": {"shape": "RamHorns", "colours": {"Horn": (0.56, 0.42, 0.86)}},
    "Demon Horn": {"shape": "DemonHorns", "colours": {"Horn": (0.95, 0.52, 0.32)}},
    "Void Horn": {"shape": "DemonHorns", "colours": {"Horn": (0.52, 0.36, 0.86)}},
    "devil horn": {"shape": "DemonHorns", "colours": {"Horn": (0.90, 0.26, 0.26)}},
    "3rd Eye": {"shape": "ThirdEye", "colours": {"Dark": (0.07, 0.05, 0.10), "White": (0.98, 0.97, 0.94)},
                "unlit": ("Dark", "White")},
    "Diamond": {"shape": "Gem", "colours": {"Gem": (0.36, 0.76, 0.98)}, "unlit": ("Gem",)},
    "Holy Seal": {"shape": "Seal", "colours": {"Gold": (0.98, 0.80, 0.22)}},
    "Blue Laser": {"shape": "HeadLaser", "colours": {"Beam": (0.55, 0.92, 1.0), "Glow": (0.97, 0.99, 1.0)},
                   "unlit": ("Beam", "Glow")},
    "Poison Shroom": {"shape": "Mushrooms", "colours": {"Stem": (0.80, 0.90, 0.78), "Cap": (0.62, 0.84, 0.70),
                                                        "Spot": (0.95, 0.58, 0.24)}},
}
