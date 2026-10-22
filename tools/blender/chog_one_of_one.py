"""The ten Genesis of the base "1:1": each is a character of its own, drawn once, and its metadata names no
trait but its base and its tier. Each is given here the traits the builder knows (skin, eyes, mouth, hat,
clothing), read on its picture, and the parts no other Genesis wears are built here: a cape, wings, a
halo, flames, a brain, a gold helmet, a trident, the eyes of a frog suit.

make_chog_traits.py registers these tables (its traits_of gives a 1:1 the look written here) and
export_chog_parts.py exports them as it does every other Genesis.

What each picture shows (read on the 1000 px pictures, 16 October 2026):

- #53    a Chog of fire: orange running to yellow at the top, glowing yellow eyes, a small smile, a red cape
         with a brown collar and a round gold clasp.
- #392   a skull in flames: bone, black sockets lit orange inside, the teeth, flames at the back of the head.
- #434   The Scream: a peach face in a dark blue-purple crown, round eyes, an open "O" mouth.
- #494   a zombie: a pale green face, black sockets with green pupils, the brain out on the top of the head,
         a mouth of teeth (the pictures drool green: the gritted teeth of the collection read better), a
         purple shirt.
- #973   Pepe in a frog suit: the green hood with two frog eyes on top, a beige face with the heavy lids of
         Pepe, a frown.
- #1006  a manga page: grey and white only, round eyes, a flat mouth, a black turtleneck.
- #1159  an angel: cream, a gold halo, white wings, one tired eye.
- #1290  a warrior: a purple crown, a gold helmet with a red gem, big blue eyes, a fur collar.
- #1373  a Chog of gold: gold all over, golden eyes, a small smile.
- #1376  a devil: red, two red horns, red eyes, a fang, a purple trident.

Not drawn: the painted style of #434 and its hands on its cheeks, the cracks of the skull of #392, the
stripes painted on the face of #1290, the drawn screentone of #1006 (the game moves the arms; a pose
held by the arms would not stay).
"""

import math
from types import SimpleNamespace

from mathutils import Vector

import make_chog_body as base

X, Y, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))
DARK = (0.06, 0.05, 0.07)

# number -> the look given to it, as traits of the collection (values below or of the other tables).
LOOKS = {
    53: {"Base": "Origin", "Skin": "1:1 Flame", "Eyes": "1:1 Glowing Eye", "Mouth": "Smile",
         "Body": "1:1 Flame Cape"},
    392: {"Base": "Skull", "Skin": "1:1 Ghost Skull", "Eyes": "1:1 Socket Eye", "Head": "1:1 Skull Flames"},
    434: {"Base": "Origin", "Skin": "1:1 Scream", "Eyes": "Round Eye", "Mouth": "wo", "Body": "1:1 Scream Coat"},
    494: {"Base": "Origin", "Skin": "1:1 Zombie", "Eyes": "1:1 Zombie Eye", "Mouth": "Grit",
          "Head": "1:1 Brain", "Body": "1:1 Zombie Tee"},
    973: {"Base": "Origin", "Skin": "1:1 Pepe", "Eyes": "Tired", "Mouth": "1:1 Pepe Frown", "Body": "1:1 Frog Suit"},
    1006: {"Base": "Origin", "Skin": "1:1 Manga", "Eyes": "Round Eye", "Mouth": "Flat",
           "Body": "1:1 Manga Turtleneck"},
    1159: {"Base": "Origin", "Skin": "1:1 Angel", "Eyes": "Tired", "Mouth": "Flat", "Head": "1:1 Halo",
           "Body": "1:1 Angel Wings"},
    1290: {"Base": "Origin", "Skin": "1:1 Warrior", "Eyes": "1:1 Blue Eye", "Mouth": "Flat",
           "Head": "1:1 Gold Helmet", "Body": "1:1 Warrior Tee"},
    1373: {"Base": "Origin", "Skin": "1:1 Gold", "Eyes": "1:1 Gold Eye", "Mouth": "Smile"},
    1376: {"Base": "Origin", "Skin": "1:1 Devil", "Eyes": "1:1 Devil Eye", "Mouth": "1:1 Fang",
           "Head": "1:1 Devil Horns", "Body": "1:1 Devil Suit"},
}
# Whose hands and feet take the colour of the skin, not the pink mittens of a Chog.
HANDS_OF_SKIN = {53, 1006, 1159, 1373, 1376}
# Whose cheeks are not pink: the manga page is grey and white only.
BLUSH = {1006: (0.66, 0.66, 0.68)}

# The colours of their skins (skin, face; "top" for a skin that runs to another colour at the top).
SKINS = {
    "1:1 Flame": {"skin": (0.97, 0.46, 0.10), "top": (1.00, 0.82, 0.22), "face": (0.99, 0.62, 0.17)},
    "1:1 Scream": {"skin": (0.30, 0.22, 0.70), "face": (0.95, 0.80, 0.58)},
    "1:1 Zombie": {"skin": (0.40, 0.28, 0.68), "face": (0.84, 0.90, 0.76)},
    "1:1 Pepe": {"skin": (0.40, 0.28, 0.68), "face": (0.93, 0.84, 0.72)},
    "1:1 Manga": {"skin": (0.78, 0.78, 0.79), "face": (0.97, 0.97, 0.97)},
    "1:1 Angel": {"skin": (0.95, 0.88, 0.72), "face": (0.98, 0.93, 0.83)},
    "1:1 Warrior": {"skin": (0.62, 0.30, 0.86), "face": (0.98, 0.80, 0.84)},
    "1:1 Gold": {"skin": (0.95, 0.70, 0.10), "top": (1.00, 0.88, 0.40), "face": (1.00, 0.82, 0.30)},
    "1:1 Devil": {"skin": (0.86, 0.14, 0.16), "face": (0.98, 0.64, 0.64)},
}
# The skull of #392, in the bone colour of its picture (chog_trait_bases.SKULL_SKINS).
SKULL_SKINS = {"1:1 Ghost Skull": (0.95, 0.91, 0.80)}


def look_of(number, traits):
    """The traits a 1:1 Genesis is built with: its look, its tier kept."""
    look = LOOKS.get(int(number))
    if look is None:
        raise SystemExit(f"CHOG_ONE CHOG #{number} is a 1:1 Genesis with no look written in chog_one_of_one.py")
    return {"Side": "Right", **look, "Tier": traits.get("Tier", "Legendary"), "OneOfOne": str(number)}


def number_of(traits):
    """The number of a 1:1 Genesis from the traits look_of gave it, or None for any other."""
    value = (traits or {}).get("OneOfOne")
    return int(value) if value else None


# ---------------------------------------------------------------------------------------------
# Eyes and mouths: kinds the builder has, in the colours of the pictures.
# ---------------------------------------------------------------------------------------------

def round_eye(white, dark, pupil=(0.030, 0.035)):
    return {"kind": "Round", "colours": {"Dark": dark, "White": white}, "unlit": ("Dark", "White"),
            "numbers": {"PUPIL": pupil}}


EYES = {
    "1:1 Glowing Eye": round_eye((1.00, 0.97, 0.70), (1.00, 0.86, 0.36), (0.040, 0.046)),
    "1:1 Socket Eye": round_eye((0.10, 0.07, 0.06), (0.98, 0.55, 0.12), (0.026, 0.030)),
    "1:1 Zombie Eye": round_eye((0.07, 0.07, 0.08), (0.36, 0.95, 0.32), (0.020, 0.024)),
    "1:1 Blue Eye": round_eye((0.93, 0.95, 1.00), (0.30, 0.48, 0.92), (0.036, 0.042)),
    "1:1 Gold Eye": round_eye((1.00, 0.93, 0.62), (0.90, 0.68, 0.16), (0.020, 0.024)),
    "1:1 Devil Eye": round_eye((0.95, 0.18, 0.18), (0.50, 0.02, 0.05)),
}


def mouths(table):
    """The mouths of the 1:1 from those of the builder ("table": its MOUTHS with chog_trait_faces')."""
    drool, frown, tooth = table["Drooling"], table["Sad"], table["Gold Tooth"]
    return {
        "1:1 Zombie Drool": {**drool, "colours": {**drool["colours"], "Drool": (0.55, 0.90, 0.35)}},
        "1:1 Pepe Frown": {**frown, "colours": {"Line": (0.78, 0.30, 0.32)}},
        # The gold tooth of the collection in white: a fang.
        "1:1 Fang": {**tooth, "colours": {**tooth["colours"], "Gold": (0.97, 0.97, 0.97)}},
    }


# ---------------------------------------------------------------------------------------------
# Hats.
# ---------------------------------------------------------------------------------------------

HALO = {"HEIGHT": 0.80, "RADIUS": 0.20, "TUBE": 0.024, "TILT_DEG": 18.0, "AROUND": 20, "SIDES": 6}


def torus_shell(part, centre, axis, radius, tube, around, sides, material, outline):
    """A ring: a closed tube round "axis", made of rows around it, the last one joined to the first."""
    first = (Y if abs(axis.dot(Y)) < 0.9 else X).cross(axis).normalized()
    second = axis.cross(first).normalized()
    mesh = part.mesh
    rows = []
    for index in range(around):
        angle = 2.0 * math.pi * index / around
        out = first * math.cos(angle) + second * math.sin(angle)
        middle = centre + out * radius
        rows.append([mesh.verts.new(middle + out * tube * math.cos(2.0 * math.pi * k / sides)
                                    + axis * tube * math.sin(2.0 * math.pi * k / sides)) for k in range(sides)])
    faces = []
    for index in range(around):
        lower, upper = rows[index], rows[(index + 1) % around]
        for k in range(sides):
            after = (k + 1) % sides
            face = mesh.faces.new((lower[k], lower[after], upper[after], upper[k]))
            face.smooth = True
            faces.append(face)
    part.paint(faces, material)
    if outline > 0.0:
        part.outlined.append((faces, outline, None))
    return faces


def build_halo(maker):
    """#1159: a gold ring floating over the crown, leaning back a little."""
    numbers = SimpleNamespace(**HALO)
    part = maker.part()
    head = maker.head
    tilt = math.radians(numbers.TILT_DEG)
    axis = (Z * math.cos(tilt) + Y * math.sin(tilt)).normalized()
    centre = Z * (head.HEAD_MIDDLE + numbers.HEIGHT - 0.215) + Y * 0.03
    torus_shell(part, centre, axis, numbers.RADIUS, numbers.TUBE, numbers.AROUND, numbers.SIDES,
                part.named("Halo"), 0.012)


FLAMES = {"COUNT": 9, "FROM_DEG": 70.0, "TO_DEG": 290.0, "LENGTH": (0.20, 0.30), "WIDTH": 0.060, "LEAN_DEG": 55.0}


def blade_loops(foot, along, across, length, width, thick, wave=0.0):
    """A pointed blade from "foot" along "along", its flat side square to "across": loops for a shell."""
    flat = along.cross(across).normalized()
    loops = [[foot - along * 0.01]]
    for share in (0.10, 0.28, 0.48, 0.68, 0.86):
        half = width * math.sin(math.pi * min(1.0, share * 1.1)) ** 0.75 * (1.0 - 0.3 * share)
        bend = flat * wave * math.sin(share * math.pi * 1.5) * length
        centre = foot + along * share * length + bend
        loops.append([centre + across * half * math.cos(a) + flat * thick * (1.0 - 0.5 * share) * math.sin(a)
                      for a in (2.0 * math.pi * k / 8 for k in range(8))])
    loops.append([foot + along * length + flat * wave * 0.4 * length])
    return loops


def blade(part, foot, along, across, length, width, thick, material, wave=0.0, outline=0.012):
    import make_chog_traits as host
    host.add_loops(part, blade_loops(foot, along, across, length, width, thick, wave), material, outline)


def build_skull_flames(maker):
    """#392: tongues of fire round the back and the top of the skull, orange with a yellow heart."""
    numbers = SimpleNamespace(**FLAMES)
    part = maker.part()
    surface = maker.surface
    for index in range(numbers.COUNT):
        share = index / (numbers.COUNT - 1)
        around = math.radians(numbers.FROM_DEG + (numbers.TO_DEG - numbers.FROM_DEG) * share)
        # 0 degrees is the front of the head (-Y), 180 its back.
        out = Vector((math.sin(around), math.cos(around), 0.0))
        rise = 0.35 + 0.55 * math.sin(math.pi * share)
        direction = (out * (1.0 - rise * 0.5) + Z * rise).normalized()
        foot = surface.towards(direction) - direction * 0.02
        along = (direction * math.cos(math.radians(numbers.LEAN_DEG)) + Z * math.sin(math.radians(numbers.LEAN_DEG))
                 + Y * 0.25).normalized()
        across = along.cross(out).normalized() if abs(along.dot(out)) < 0.95 else X
        length = numbers.LENGTH[0] + (numbers.LENGTH[1] - numbers.LENGTH[0]) * math.sin(math.pi * share)
        wave = 0.10 if index % 2 else -0.10
        blade(part, foot, along, across, length, numbers.WIDTH, 0.016, part.named("Fire"), wave)
        blade(part, foot + along * 0.03 - out * 0.004, along, across, length * 0.62, numbers.WIDTH * 0.55, 0.020,
              part.named("Core"), wave, 0.0)


BRAIN = {"WHERE": (-0.66, -0.34, 0.66), "LOBES": ((0.0, 0.0, 0.0, 0.075), (0.06, 0.04, 0.02, 0.065),
                                                  (-0.05, 0.05, 0.01, 0.06), (0.02, -0.06, 0.0, 0.06),
                                                  (-0.03, -0.03, 0.05, 0.055), (0.05, -0.02, 0.05, 0.05))}


def build_brain(maker):
    """#494: the brain out on the top of the head, a cluster of pink lobes with a groove of darker pink."""
    numbers = SimpleNamespace(**BRAIN)
    part = maker.part()
    out = Vector(numbers.WHERE).normalized()
    middle = maker.surface.towards(out) + out * 0.02
    sideways = out.cross(Z).normalized() if abs(out.dot(Z)) < 0.98 else X
    forward = out.cross(sideways).normalized()
    for index, (dx, dy, dz, radius) in enumerate(numbers.LOBES):
        centre = middle + sideways * dx + forward * dy + out * dz
        radius *= 1.15
        key = "Groove" if index % 3 == 2 else "Brain"
        part.shell(base.ball_rings(centre, out, sideways, radius * 0.85, radius, radius * 1.05, 6), 10,
                   part.named(key), 0.012)


HELMET_CREST = {"HEIGHT": 0.17, "SPREAD_DEG": 28.0, "WIDTH": 0.045, "GEM": 0.034}


def build_gold_helmet(maker):
    """#1290: the band of a headband in gold, a crest rising from its front in a V, a red gem between."""
    import chog_trait_hats as hats_module
    hats_module.build_headband(maker)
    numbers = SimpleNamespace(**HELMET_CREST)
    part = maker.part()
    surface = maker.surface
    front = surface.front(0.0, 0.17, 0.03)
    out = (front - surface.middle).normalized()
    for side in (1.0, -1.0):
        spread = math.radians(numbers.SPREAD_DEG) * side
        along = (Z * math.cos(spread) + X * math.sin(spread) - Y * 0.15).normalized()
        across = along.cross(out).normalized()
        blade(part, front, along, across, numbers.HEIGHT, numbers.WIDTH, 0.014, part.named("Band"))
    gem = front + out * 0.02 + Z * 0.02
    part.shell(base.ball_rings(gem, out, X, numbers.GEM * 0.6, numbers.GEM, numbers.GEM * 1.25, 4), 6,
               part.named("Gem"), 0.010, smooth=False)


# ---------------------------------------------------------------------------------------------
# Clothing.
# ---------------------------------------------------------------------------------------------

CAPE = {"COLLAR": (0.040, 0.020), "FROM_DEG": 120.0, "THICK": 0.012, "HEM": 0.06, "FLARE": 0.07, "ROWS": 7,
        "CLASP": 0.026}


def build_cape(maker):
    """#53: a cape from the shoulders to the heels behind, flaring out, a collar round the neck and a round
    clasp in front."""
    import make_chog_traits as host
    numbers = SimpleNamespace(**CAPE)
    body = maker.body
    part = maker.part(on="Body")
    top = body.SHOULDER_HEIGHT + 0.02
    # The collar: a band round the base of the neck.
    rows = []
    for height in (top - numbers.COLLAR[0] * 0.5, top + numbers.COLLAR[0] * 0.5):
        half_width, half_depth = host.torso_section(body, min(height, top))
        rows.append(host.ring_points(Z * height, X * (half_width * 0.62 + numbers.COLLAR[1]),
                                     Y * (half_depth * 0.70 + numbers.COLLAR[1]), 16))
    host.add_loops(part, rows, part.named("Collar"), 0.010)
    half_width, half_depth = host.torso_section(body, top)
    clasp = Vector((0.0, -(half_depth * 0.70 + numbers.COLLAR[1]) - 0.008, top))
    part.shell(base.ball_rings(clasp, -Y, X, numbers.CLASP * 0.45, numbers.CLASP, numbers.CLASP, 4), 10,
               part.named("Clasp"), 0.008)
    # The cape: a crescent behind the body at each height, wider down to the hem.
    spread = math.radians(numbers.FROM_DEG)
    loops = []
    for row in range(numbers.ROWS + 1):
        share = row / numbers.ROWS
        height = top - (top - numbers.HEM) * share
        half_width, half_depth = host.torso_section(body, max(height, 0.18))
        grow = numbers.FLARE * share
        outer, inner = [], []
        for k in range(13):
            angle = -spread + 2.0 * spread * k / 12
            # 0 is straight behind (+Y).
            direction = Vector((math.sin(angle), math.cos(angle), 0.0))
            reach_x = half_width + 0.035 + grow
            reach_y = half_depth + 0.035 + grow
            point = Z * height + Vector((direction.x * reach_x, direction.y * reach_y, 0.0))
            outer.append(point)
            inner.append(point - direction * numbers.THICK)
        loops.append(outer + list(reversed(inner)))
    host.add_loops(part, loops, part.named("Cape"), 0.012)


WINGS = {"ROOT": (0.07, 0.13), "FEATHERS": ((22.0, 0.50), (50.0, 0.56), (78.0, 0.48), (106.0, 0.36)),
         "WIDTH": 0.075, "THICK": 0.016, "BACK_DEG": 35.0}


def build_wings(maker):
    """#1159: two white wings from the upper back, each a fan of five long feathers."""
    numbers = SimpleNamespace(**WINGS)
    body = maker.body
    for side in (1.0, -1.0):
        part = maker.part(on="Body")
        root = Vector((side * numbers.ROOT[0], numbers.ROOT[1], body.SHOULDER_HEIGHT - 0.05))
        back = math.radians(numbers.BACK_DEG)
        for index, (degrees, length) in enumerate(numbers.FEATHERS):
            angle = math.radians(degrees)
            along = (Z * math.cos(angle) + X * side * math.sin(angle) * math.cos(back) + Y * math.sin(back)).normalized()
            across = along.cross(Y).normalized()
            key = "Feather" if index % 2 == 0 else "Shade"
            blade(part, root + along * 0.02, along, across, length, numbers.WIDTH, numbers.THICK, part.named(key))


TRIDENT = {"BELOW": 0.30, "ABOVE": 0.52, "SHAFT": 0.016, "PRONG": 0.10, "SPREAD": 0.055}


def build_devil_suit(maker):
    """#1376: a dark red shirt, and a purple trident standing in the right hand."""
    import make_chog_traits as host
    host.build_tee(maker)
    numbers = SimpleNamespace(**TRIDENT)
    body = maker.body
    spread, ahead = math.radians(body.ARM_SPREAD_DEG), math.radians(body.ARM_FORWARD_DEG)
    down = Vector((-math.sin(spread), -math.sin(ahead), -math.cos(spread) * math.cos(ahead))).normalized()
    hand = down * body.ARM_LENGTH - Y * 0.03
    part = maker.part(on="armRight")
    material = part.named("Trident")
    part.shell(base.tube_rings(hand - Z * numbers.BELOW, Z, X, ((0.0, 0.0), (0.004, numbers.SHAFT),
                                                                (numbers.BELOW + numbers.ABOVE, numbers.SHAFT))),
               8, material, 0.010)
    top = hand + Z * numbers.ABOVE
    part.shell(base.tube_rings(top - X * numbers.SPREAD * 1.1, X, Z, ((0.0, 0.0), (0.004, numbers.SHAFT),
                                                                     (numbers.SPREAD * 2.2, numbers.SHAFT),
                                                                     (numbers.SPREAD * 2.2 + 0.004, 0.0))),
               8, material, 0.010)
    for offset in (-numbers.SPREAD, 0.0, numbers.SPREAD):
        prong = numbers.PRONG * (1.25 if offset == 0.0 else 1.0)
        part.shell(base.tube_rings(top + X * offset, Z, X, ((0.0, numbers.SHAFT), (prong * 0.8, numbers.SHAFT * 0.9),
                                                             (prong, 0.0))), 8, material, 0.010)


FROG_EYES = {"WHERE": ((0.30, -0.10, 0.94), (-0.30, -0.10, 0.94)), "SIZE": 0.085, "PUPIL": 0.030, "OUT": 0.08}


def build_frog_suit(maker):
    """#973: the suit and hood of a costume, two big frog eyes on top of the hood."""
    import chog_trait_hats as hats_module
    hats_module.build_costume(maker)
    # (22 October 2026: the toad suits give their own "frog_eyes", apart; #973 keeps FROG_EYES.)
    numbers = SimpleNamespace(**{**FROG_EYES, **maker.entry.get("frog_eyes", {})})
    part = maker.part()
    for where in numbers.WHERE:
        out = Vector(where).normalized()
        centre = maker.surface.towards(out) + out * numbers.OUT
        part.shell(base.ball_rings(centre, out, X, numbers.SIZE * 0.85, numbers.SIZE, numbers.SIZE, 8), 12,
                   part.named("EyeBall"), 0.012)
        pupil = centre + (out * 0.6 - Y * 0.8).normalized() * numbers.SIZE * 0.92
        part.shell(base.ball_rings(pupil, (out * 0.6 - Y * 0.8).normalized(), X, numbers.PUPIL * 0.4,
                                   numbers.PUPIL * 1.3, numbers.PUPIL, 4), 8, part.named("Pupil"), 0.0)


# ---------------------------------------------------------------------------------------------
# The tables the builder reads.
# ---------------------------------------------------------------------------------------------

HAT_SHAPES = {"Halo": build_halo, "SkullFlames": build_skull_flames, "Brain": build_brain,
              "GoldHelmet": build_gold_helmet}
CLOTH_KINDS = {"Cape": build_cape, "Wings": build_wings, "DevilSuit": build_devil_suit, "FrogSuit": build_frog_suit}


def hats(headband_numbers):
    """The hats of the 1:1 and their numbers (the helmet lies where a headband does; the devil's horns are
    the ridged horns of the radioactive Chogs, one pair of them)."""
    entries = {
        "1:1 Halo": {"shape": "Halo", "colours": {"Halo": (0.99, 0.78, 0.18)}},
        "1:1 Skull Flames": {"shape": "SkullFlames", "colours": {"Fire": (0.98, 0.42, 0.08), "Core": (1.0, 0.86, 0.22)},
                             "unlit": ("Fire", "Core")},
        "1:1 Brain": {"shape": "Brain", "colours": {"Brain": (0.95, 0.52, 0.56), "Groove": (0.82, 0.36, 0.42)}},
        "1:1 Gold Helmet": {"shape": "GoldHelmet", "colours": {"Band": (0.98, 0.76, 0.16), "Line": (0.78, 0.52, 0.08),
                                                                "Gem": (0.88, 0.10, 0.14)}},
        # Two short horns at the upper corners of the brow, curving up, the spikes all around (on the top of
        # the head until 16 October 2026, where the upper spikes were taken out).
        "1:1 Devil Horns": {"shape": "DemonHorns", "colours": {"Horn": (0.58, 0.04, 0.08)},
                            "horns": (((0.15, 0.24), 0.35, 0.18, 0.058, 0.05),)},
    }
    numbers = {
        "Halo": {"OUTLINE_THICKNESS": 0.012},
        "SkullFlames": {"OUTLINE_THICKNESS": 0.012},
        # The brain comes out where the upper and the middle pairs of leaves grow: those are left out.
        "Brain": {"OUTLINE_THICKNESS": 0.012,
                  "LEAVES": tuple((0.0, None, None) if line in (1, 2) else (1.0, None, None) for line in range(7))},
        "GoldHelmet": dict(headband_numbers),
    }
    return entries, numbers


def clothes(tee, costume):
    """The clothing of the 1:1 ("tee" and "costume" make entries as chog_trait_hats does)."""
    return {
        "1:1 Flame Cape": {"kind": "Cape", "colours": {"Cape": (0.84, 0.15, 0.12), "Collar": (0.45, 0.22, 0.12),
                                                         "Clasp": (0.96, 0.70, 0.16)}},
        "1:1 Scream Coat": tee((0.20, 0.15, 0.46)),
        "1:1 Zombie Tee": tee((0.45, 0.30, 0.62)),
        "1:1 Manga Turtleneck": {**tee((0.13, 0.13, 0.14), (0.09, 0.09, 0.10)),
                                 "numbers": {"COLLAR": (0.600, 0.640)}},
        "1:1 Warrior Tee": tee((0.45, 0.22, 0.62), (0.96, 0.62, 0.18)),
        "1:1 Angel Wings": {"kind": "Wings", "colours": {"Feather": (0.98, 0.98, 0.97), "Shade": (0.86, 0.87, 0.92)}},
        "1:1 Devil Suit": {"kind": "DevilSuit", "colours": {"Main": (0.42, 0.06, 0.10), "Collar": (0.26, 0.03, 0.06),
                                                            "Trident": (0.56, 0.30, 0.80)}},
        "1:1 Frog Suit": {**costume((0.46, 0.74, 0.20), {"Hood": (0.46, 0.74, 0.20), "Rim": (0.80, 0.86, 0.24),
                                                          "EyeBall": (0.97, 0.97, 0.94), "Pupil": DARK}),
                          "kind": "FrogSuit"},
    }


# ---------------------------------------------------------------------------------------------
# The basic attack: what each throws (chog_attack_objects.py) and the line under it on the menu. Without
# an entry here a hat is thrown itself (the halo, the brain, the flames, the helmet) and a bare head throws
# a spike of its crown in its own colours.
# ---------------------------------------------------------------------------------------------

POINT = 2   # chog_attack_objects.POINT: thrown point first, like the spear


def build_trident_object(shapes, word_pieces):
    """The devil's trident, thrown point first."""
    import chog_attack_objects as attacks
    material = shapes.named("Trident")
    attacks.tube(shapes, Y * 0.30, attacks.FORWARD, [(0.0, 0.014), (0.40, 0.014)], material, sides=8)
    attacks.tube(shapes, Y * -0.10 - X * 0.068, X, [(0.0, 0.0), (0.004, 0.013), (0.132, 0.013), (0.136, 0.0)],
                 material, sides=8)
    for offset in (-0.058, 0.0, 0.058):
        attacks.tube(shapes, Y * -0.10 + X * offset, attacks.FORWARD,
                     [(0.0, 0.013), (0.08, 0.011), (0.13 if offset == 0.0 else 0.10, 0.0)], material, sides=6)


ATTACK_OF_HEAD = {"1:1 Devil Horns": ("Trident", POINT)}
ATTACK_OBJECTS = {"Trident": ({"Trident": (0.56, 0.30, 0.80)}, (), build_trident_object, (0.70, 0.45, 0.95))}
ATTACK_WORDS = {"Trident": "TRIDENTS"}
LINES = {
    53: "THROWS THE FLAMES OF ITS CROWN",
    392: "THROWS ITS FLAMES",
    494: "THROWS ITS BRAIN",
    1159: "THROWS ITS HALO",
    1290: "THROWS ITS HELMET",
    1373: "THROWS GOLDEN SPIKES",
    1376: "THROWS ITS TRIDENT",
}
