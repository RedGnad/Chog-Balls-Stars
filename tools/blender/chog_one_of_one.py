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
import make_chog_full as full

X, Y, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))
DARK = (0.06, 0.05, 0.07)

# number -> the look given to it, as traits of the collection (values below or of the other tables).
LOOKS = {
    53: {"Base": "Origin", "Skin": "1:1 Flame", "Eyes": "1:1 Glowing Eye", "Mouth": "Smile",
         "Body": "1:1 Flame Cape"},
    392: {"Base": "Skull", "Skin": "1:1 Ghost Skull", "Eyes": "1:1 Socket Eye", "Head": "1:1 Skull Flames"},
    434: {"Base": "Origin", "Skin": "1:1 Scream", "Eyes": "1:1 Scream Eye", "Mouth": "1:1 Scream Mouth",
          "Head": "1:1 Scream Hands", "Body": "1:1 Scream Coat"},
    494: {"Base": "Origin", "Skin": "1:1 Zombie", "Eyes": "1:1 Zombie Eye", "Mouth": "Grit",
          "Head": "1:1 Brain", "Body": "1:1 Zombie Tee"},
    973: {"Base": "Origin", "Skin": "1:1 Pepe", "Eyes": "Tired", "Body": "1:1 Frog Suit"},
    1006: {"Base": "Origin", "Skin": "1:1 Manga", "Eyes": "1:1 Manga Eye", "Mouth": "Flat", "Head": "1:1 Manga Brows",
           "Body": "1:1 Manga Turtleneck"},
    1159: {"Base": "Origin", "Skin": "1:1 Angel", "Eyes": "Tired", "Mouth": "Flat", "Head": "1:1 Halo",
           "Body": "1:1 Angel Wings"},
    1290: {"Base": "Origin", "Skin": "1:1 Warrior", "Eyes": "1:1 Blue Eye", "Mouth": "Flat",
           "Head": "1:1 Gold Helmet", "Body": "1:1 Warrior Tee"},
    1373: {"Base": "Origin", "Skin": "1:1 Gold", "Eyes": "1:1 Gold Eye", "Mouth": "Smile", "Head": "1:1 Gold Shine"},
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

def round_eye(white, dark, pupil=(0.030, 0.035), **numbers):
    return {"kind": "Round", "colours": {"Dark": dark, "White": white}, "unlit": ("Dark", "White"),
            "numbers": {"PUPIL": pupil, **numbers}}


EYES = {
    "1:1 Glowing Eye": round_eye((1.00, 0.97, 0.70), (1.00, 0.86, 0.36), (0.040, 0.046)),
    "1:1 Socket Eye": round_eye((0.10, 0.07, 0.06), (0.98, 0.55, 0.12), (0.026, 0.030)),
    "1:1 Zombie Eye": round_eye((0.07, 0.07, 0.08), (0.36, 0.95, 0.32), (0.020, 0.024)),
    "1:1 Blue Eye": round_eye((0.93, 0.95, 1.00), (0.30, 0.48, 0.92), (0.040, 0.056), LINE=(0.070, 0.086), WHITE=(0.064, 0.080)),
    "1:1 Gold Eye": round_eye((1.00, 0.93, 0.62), (0.90, 0.68, 0.16), (0.032, 0.050), LINE=(0.056, 0.080), WHITE=(0.050, 0.074)),
    "1:1 Devil Eye": round_eye((0.95, 0.18, 0.18), (0.50, 0.02, 0.05), (0.036, 0.048), LINE=(0.064, 0.080), WHITE=(0.056, 0.072)),
    # (23 October 2026, the ten 1:1 compared with their 1000 px art.) The Scream: two large cream discs in a thin brown line with a small
    # dark pupil, the pupils off to the same side, as painted.
    "1:1 Scream Eye": round_eye((0.98, 0.93, 0.74), (0.40, 0.30, 0.26), (0.016, 0.020), LINE=(0.074, 0.084), WHITE=(0.068, 0.078)),
    # The manga page: a big white disc in a thick black line, a black pupil with a white speck (the eye of an angry hero).
    "1:1 Manga Eye": round_eye((0.98, 0.98, 0.98), (0.05, 0.05, 0.06), (0.030, 0.038), LINE=(0.066, 0.078), WHITE=(0.044, 0.056)),
}


def mouths(table):
    """The mouths of the 1:1 from those of the builder ("table": its MOUTHS with chog_trait_faces')."""
    drool, tooth = table["Drooling"], table["Gold Tooth"]
    return {
        "1:1 Zombie Drool": {**drool, "colours": {**drool["colours"], "Drool": (0.55, 0.90, 0.35)}},
        # The gold tooth of the collection in white: a fang.
        "1:1 Fang": {**tooth, "colours": {**tooth["colours"], "Gold": (0.97, 0.97, 0.97)}},
        # The Scream: a tall open oval in a thin line, much larger than the small "wo" (23 October 2026).
        "1:1 Scream Mouth": {**table["wo"], "colours": {**table["wo"]["colours"], "Line": (0.42, 0.22, 0.20)},
                             "numbers": {"AT": (0.0, -0.086), "SIZE": (0.036, 0.064), "GLINT": (0.0, 0.0, 0.0001)}},
    }


# ---------------------------------------------------------------------------------------------
# Hats.
# ---------------------------------------------------------------------------------------------

# (23 October 2026, from #1159: the halo is a large thick gold ring floating well above the crown, wider than the head; the brow over the open eye is a thick white bar.)
HALO = {"HEIGHT": 0.77, "RADIUS": 0.27, "TUBE": 0.036, "TILT_DEG": 18.0, "AROUND": 16, "SIDES": 5,
        "BROW": ((0.040, 0.090), (0.190, 0.108), 0.026)}


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
    import make_chog_traits as host
    # The brows go on a part of their own: the halo is what the hero throws (the first part), not the strokes on its face.
    brows = maker.part()
    inner, outer, width = numbers.BROW
    for sign in (1.0, -1.0):
        places = [(sign * (inner[0] + (outer[0] - inner[0]) * share), inner[1] + (outer[1] - inner[1]) * share) for share in (0.0, 0.5, 1.0)]
        face_stroke(brows, maker.surface, brows.named("Brow"), places, [width, width * 1.05, width * 0.8], host.DRAWN[2])


# (23 October 2026: the flames of #392 are a great sheet of fire streaming from the back of the head up and to the side, tongues as tall as the skull,
# with a few licking along the jaw; until then nine small tongues 0.20 to 0.30 long in a crown.)
FLAMES = {"COUNT": 9, "FROM_DEG": 60.0, "TO_DEG": 300.0, "LENGTH": (0.30, 0.52), "WIDTH": 0.085, "LEAN_DEG": 62.0}


def blade_loops(foot, along, across, length, width, thick, wave=0.0, sides=8):
    """A pointed blade from "foot" along "along", its flat side square to "across": loops for a shell."""
    flat = along.cross(across).normalized()
    loops = [[foot - along * 0.01]]
    for share in (0.10, 0.28, 0.48, 0.68, 0.86):
        half = width * math.sin(math.pi * min(1.0, share * 1.1)) ** 0.75 * (1.0 - 0.3 * share)
        bend = flat * wave * math.sin(share * math.pi * 1.5) * length
        centre = foot + along * share * length + bend
        loops.append([centre + across * half * math.cos(a) + flat * thick * (1.0 - 0.5 * share) * math.sin(a)
                      for a in (2.0 * math.pi * k / sides for k in range(sides))])
    loops.append([foot + along * length + flat * wave * 0.4 * length])
    return loops


def blade(part, foot, along, across, length, width, thick, material, wave=0.0, outline=0.012, sides=8):
    import make_chog_traits as host
    host.add_loops(part, blade_loops(foot, along, across, length, width, thick, wave, sides), material, outline)


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
                                                  (-0.03, -0.03, 0.05, 0.055), (0.05, -0.02, 0.05, 0.05)),
         # (23 October 2026, from #494: the brain is large, spilling over the brow, blood running from it down the face; the mouth
         # is open with green slime hanging from it in strands; blood speckles the face.) Blood: how many drips, how long (degrees down the head),
         # their width; the strands of slime: where (to the side) and how far down; the speckles: (to the side, above, radius).
         "SCALE": 1.45, "DRIPS": ((0.0, 26.0), (-14.0, 40.0), (13.0, 18.0)), "DRIP_WIDTH": 0.012,
         "SLIME": ((0.040, -0.20, 0.016), (0.100, -0.17, 0.012)),
         "SPECKLES": ((-0.17, 0.10, 0.010), (-0.12, 0.17, 0.007), (0.15, 0.13, 0.008), (-0.20, -0.02, 0.007), (0.20, -0.04, 0.006))}


def build_brain(maker):
    """#494: the brain out on the top of the head, a cluster of pink lobes with a groove of darker pink, the blood that runs from it, the slime
    that hangs from the mouth and the speckles on the face."""
    import make_chog_traits as host
    numbers = SimpleNamespace(**BRAIN)
    part = maker.part()
    surface = maker.surface
    out = Vector(numbers.WHERE).normalized()
    middle = maker.surface.towards(out) + out * 0.02
    sideways = out.cross(Z).normalized() if abs(out.dot(Z)) < 0.98 else X
    forward = out.cross(sideways).normalized()
    for index, (dx, dy, dz, radius) in enumerate(numbers.LOBES):
        centre = middle + (sideways * dx + forward * dy + out * dz) * numbers.SCALE
        radius *= 1.15 * numbers.SCALE
        key = "Groove" if index % 3 == 2 else "Brain"
        part.shell(base.ball_rings(centre, out, sideways, radius * 0.85, radius, radius * 1.05, 5), 9,
                   part.named(key), 0.012)
    # The blood, the speckles and the slime go on a part of their own: the brain is what the hero throws (the first part).
    part = maker.part()
    # The blood running down the side of the head from under the brain.
    for turn_deg, length_deg in numbers.DRIPS:
        places = []
        for step in range(5):
            share = step / 4.0
            way = (out + forward * (0.35 * share) + sideways * math.radians(turn_deg) * 1.2 * share - Z * 1.1 * share).normalized()
            point = surface.towards(way)
            places.append((point + (point - surface.middle).normalized() * 0.010, 0.020 * (1.0 - 0.6 * share) * numbers.DRIP_WIDTH / 0.012))
        lower, upper = [], []
        for index, (point, width) in enumerate(places):
            ahead = places[min(index + 1, 4)][0] - places[max(index - 1, 0)][0]
            normal = (point - surface.middle).normalized()
            across = ahead.cross(normal).normalized() * width * 0.5
            lower.append(point - across)
            upper.append(point + across)
        full.add_skin(part, surface, [lower, upper], part.named("Blood"), around=False)
    for side, above, radius in numbers.SPECKLES:
        face_patch(part, surface, part.named("Blood"), (side, above), radius, radius, host.DRAWN[0], 8)
    for side, bottom, width in numbers.SLIME:
        face_stroke(part, surface, part.named("Slime"), [(side, -0.095), (side + 0.004, (bottom - 0.095) * 0.5 - 0.095), (side, bottom)],
                    [width, width * 0.9, width * 1.25], host.DRAWN[1])
        face_patch(part, surface, part.named("Slime"), (side, bottom - width * 0.4), width * 0.75, width * 0.9, host.DRAWN[1], 8)


# (23 October 2026, from #1290: the helmet is a Y of gold: a wide band across the brow whose ends rise at the temples, and a flat stem down the middle of the face
# between the eyes to the nose; a red gem in a gold setting stands on the band, tall as a kite. The face has two red-pink stripes of war paint down each cheek and the
# eyes are large and shiny. Until then a headband with two blades in a V and a small ball of a gem.)
HELMET_CREST = {"GEM": (0.060, 0.082, 0.026), "STEM": ((0.0, 0.150), (0.0, 0.040), (0.0, -0.020)), "STEM_WIDTH": (0.052, 0.040, 0.026),
                "PAINT": ((0.205, 0.100), (0.200, -0.090)), "PAINT_WIDTH": 0.020,
                "GLINTS": ((-0.026, 0.034, 0.014),)}


def build_gold_helmet(maker):
    """#1290: the band of a headband in gold, a stem down the face between the eyes, a red gem in a gold setting, the war paint and the glints of the eyes."""
    import chog_trait_hats as hats_module
    import make_chog_traits as host
    hats_module.build_headband(maker)
    numbers = SimpleNamespace(**HELMET_CREST)
    part = maker.part()
    surface = maker.surface
    # The gem on the middle of the band: a gold setting, then the red kite.
    front = surface.front(0.0, 0.19, 0.03)
    out = (front - surface.middle).normalized()
    wide, tall, thick = numbers.GEM
    up = (Z - out * Z.dot(out)).normalized()
    across = up.cross(out).normalized()
    setting = front + out * 0.014 + up * tall * 0.60
    part.shell(base.ball_rings(setting, up, across, tall * 1.10, wide * 1.35, thick * 1.2, 4), 6, part.named("Band"), 0.010, smooth=False)
    part.shell(base.ball_rings(setting + out * 0.012, up, across, tall, wide, thick * 1.3, 4), 6, part.named("Gem"), 0.008, smooth=False)
    # The stem, the war paint and the glints go on a part of their own: the helmet is what the hero throws (the first part).
    part = maker.part()
    # The stem, a flat strip of gold on the face, from the band down to the bridge of the nose.
    face_stroke(part, surface, part.named("Band"), list(numbers.STEM), list(numbers.STEM_WIDTH), host.DRAWN[3])
    # The war paint: a vertical stripe down each cheek, outside the eye.
    start, stop = numbers.PAINT
    for sign in (1.0, -1.0):
        face_stroke(part, surface, part.named("Paint"), [(sign * start[0], start[1]), (sign * (start[0] + stop[0]) * 0.5, (start[1] + stop[1]) * 0.5),
                                                          (sign * stop[0], stop[1])],
                    [numbers.PAINT_WIDTH, numbers.PAINT_WIDTH * 1.15, numbers.PAINT_WIDTH * 0.7], host.DRAWN[0])
        for side, above, radius in numbers.GLINTS:
            face_patch(part, surface, part.named("Glint"), (sign * 0.112 + side * sign, 0.006 + above), radius, radius * 1.2, host.DRAWN[3], 8)


# ---------------------------------------------------------------------------------------------
# Clothing.
# ---------------------------------------------------------------------------------------------

# (23 October 2026: the collar of #53 is a thick brown band round the neck with a round gold clasp, the red cape wide over the shoulders: the collar
# was 0.040 tall and the clasp 0.026, hidden by the head.)
CAPE = {"COLLAR": (0.075, 0.034), "FROM_DEG": 150.0, "THICK": 0.012, "HEM": 0.06, "FLARE": 0.10, "ROWS": 7,
        "CLASP": 0.040}


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
        rows.append(host.ring_points(Z * height, X * (half_width * 0.86 + numbers.COLLAR[1]),
                                     Y * (half_depth * 0.90 + numbers.COLLAR[1]), 16))
    host.add_loops(part, rows, part.named("Collar"), 0.010)
    half_width, half_depth = host.torso_section(body, top)
    clasp = Vector((0.0, -(half_depth * 0.90 + numbers.COLLAR[1]) - 0.008, top - 0.01))
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


# (23 October 2026, from #1159: the wings are large, layered white feathers fanning out behind the shoulders, as tall as the head; until then four feathers
# 0.36 to 0.56 long, 0.075 wide, in a blade of 8 sides.)
WINGS = {"ROOT": (0.07, 0.13), "FEATHERS": ((14.0, 0.66), (40.0, 0.76), (66.0, 0.72), (92.0, 0.58), (118.0, 0.42)),
         "WIDTH": 0.140, "THICK": 0.020, "BACK_DEG": 35.0}


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
            blade(part, root + along * 0.02, along, across, length, numbers.WIDTH, numbers.THICK, part.named(key), sides=6)


TRIDENT = {"BELOW": 0.30, "ABOVE": 0.52, "SHAFT": 0.016, "PRONG": 0.10, "SPREAD": 0.055}


def build_devil_suit(maker):
    """#1376: a dark red shirt, and a purple trident standing in the right hand."""
    import make_chog_traits as host
    host.build_tee(maker)
    add_devil_wings(maker)
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
    if maker.entry.get("pepe"):
        add_pepe(maker, part)


PEPE = {
    # (23 October 2026, from #973: Pepe has a huge pair of thick pink-red lips in a frown across the lower face, and the frog suit has a ruff of
    # yellow-green points round the neck.) The lips: the places of their line (to the side, above), their width, the dark line round them.
    "LIPS": ((-0.165, -0.150), (-0.100, -0.112), (-0.034, -0.098), (0.034, -0.098), (0.100, -0.112), (0.165, -0.150)),
    "LIPS_WIDTH": 0.046, "LIPS_LINE": 0.060,
    "NECK": (13, 0.010, 0.140, 0.040, 125.0),     # points, how far up the shoulders it stands, length, radius, degrees each side of the front
}


def add_pepe(maker, head_part):
    """The lips of Pepe on the face and the ruff of points round the neck."""
    import make_chog_traits as host
    numbers = SimpleNamespace(**PEPE)
    surface = maker.surface
    places = list(numbers.LIPS)
    for widths, material, lift in (([numbers.LIPS_LINE] * len(places), "LipLine", host.DRAWN[0]),
                                   ([numbers.LIPS_WIDTH] * len(places), "Lip", host.DRAWN[1])):
        face_stroke(head_part, surface, head_part.named(material), places, widths, lift)
    count, up, length, radius, spread = numbers.NECK
    torso, body = maker.parts[0][0], maker.body
    height = body.SHOULDER_HEIGHT + up
    half_width, half_depth = host.torso_section(body, height)
    for index in range(count):
        angle = math.radians(-spread + 2.0 * spread * index / (count - 1))
        out = Vector((math.sin(angle), -math.cos(angle), 0.0))
        start = Vector((out.x * (half_width + 0.01), out.y * (half_depth + 0.01), height))
        way = (out * 1.0 + Z * 0.55).normalized()
        torso.shell(base.tube_rings(start, way, Z, ((0.0, radius), (length * 0.5, radius * 0.7), (length, 0.0))), 4,
                    torso.named("Spike"), 0.0)


# ---------------------------------------------------------------------------------------------
# What the ten lacked against their 1000 px pictures (23 October 2026, the owner: "the 1:1 are not worked enough").
# ---------------------------------------------------------------------------------------------

def face_patch(part, surface, material, middle, half_width, half_height, lift, segments=14, squareness=2.0):
    """A flat oval laid on the face: its middle (to the side, above), half width, half height."""
    full.add_skin(part, surface, full.patch_loops(surface, middle, half_width, half_height, squareness, 0.0, lift, 1, segments),
                  material)


def face_stroke(part, surface, material, places, widths, lift):
    """A stroke on the face through places (to the side, above), "widths" its width at each."""
    import make_chog_traits as host
    host.draw_line(part, surface, material, places, widths, lift)


SCREAM_HANDS = {"AT": (0.250, -0.060), "OFF": 0.040, "SIZE": (0.052, 0.088, 0.046), "THUMB": (0.040, 0.020, 0.032)}


def build_scream_hands(maker):
    """#434: the two hands of The Scream pressed to its cheeks, the pink of the picture, fingers up the cheek (a part of the
    head: the arms of the hero still move)."""
    numbers = SimpleNamespace(**SCREAM_HANDS)
    part, surface = maker.part(), maker.surface
    for sign in (1.0, -1.0):
        place = surface.front(sign * numbers.AT[0], numbers.AT[1], numbers.OFF)
        out = KIT_outward(surface, sign * numbers.AT[0], numbers.AT[1])
        wide, tall, thick = numbers.SIZE
        up = (Z - out * Z.dot(out)).normalized()
        across = up.cross(out).normalized()
        part.shell(base.ball_rings(place + out * thick * 0.4, up, across, tall, wide, thick, 5), 9, part.named("Hand"), 0.012)
        # The thumb, towards the face.
        reach, size, drop = numbers.THUMB
        thumb = place + out * thick * 0.5 - across * sign * reach - up * drop
        part.shell(base.ball_rings(thumb, up, across, size * 1.3, size, size, 3), 7, part.named("Hand"), 0.010)


def KIT_outward(surface, side, up):
    import make_chog_traits as host
    return host.outward(surface, side, up)


MANGA_BROWS = {
    # A thick black brow over each eye, angry: it slants down towards the middle. The middle of its inner end (to the side, above), the outer end,
    # its width at the inner end, in the middle, at the outer end.
    "INNER": (0.040, 0.082), "OUTER": (0.190, 0.128), "WIDTH": (0.036, 0.030, 0.014),
    # The grey shade under each eye, a crescent: its middle, half width, half height.
    "SHADE": (0.118, -0.062, 0.062, 0.020),
}


def build_manga_brows(maker):
    """#1006: the angry black brows of the manga page and the grey shading under the eyes."""
    numbers = SimpleNamespace(**MANGA_BROWS)
    part, surface = maker.part(), maker.surface
    import make_chog_traits as host
    for sign in (1.0, -1.0):
        inner, outer = numbers.INNER, numbers.OUTER
        places = [(sign * (inner[0] + (outer[0] - inner[0]) * share), inner[1] + (outer[1] - inner[1]) * share + 0.014 * math.sin(math.pi * share))
                  for share in (0.0, 0.25, 0.5, 0.75, 1.0)]
        wide_in, wide_mid, wide_out = numbers.WIDTH
        widths = [wide_in, (wide_in + wide_mid) * 0.5, wide_mid, (wide_mid + wide_out) * 0.5, wide_out]
        face_stroke(part, surface, part.named("Brow"), places, widths, host.DRAWN[2])
        side, up, half_width, half_height = numbers.SHADE
        face_patch(part, surface, part.named("Shade"), (sign * side, up), half_width, half_height, host.DRAWN[0], 10)


WARRIOR_FUR = {"COUNT": 15, "HEIGHT": 0.550, "LENGTH": 0.125, "RADIUS": 0.034, "SPREAD_DEG": 150.0}


def build_warrior_tee(maker):
    """#1290: a shirt with the collar of fur of the picture, a ruff of yellow and orange points all round the neck."""
    import make_chog_traits as host
    host.build_tee(maker)
    numbers = SimpleNamespace(**WARRIOR_FUR)
    torso, body = maker.parts[0][0], maker.body
    half_width, half_depth = host.torso_section(body, numbers.HEIGHT)
    for index in range(numbers.COUNT):
        angle = math.radians(-numbers.SPREAD_DEG + 2.0 * numbers.SPREAD_DEG * index / (numbers.COUNT - 1))
        out = Vector((math.sin(angle), -math.cos(angle), 0.0))
        start = Vector((out.x * (half_width + 0.012), out.y * (half_depth + 0.012), numbers.HEIGHT))
        way = (out * 1.0 + Z * 0.50).normalized()
        torso.shell(base.tube_rings(start, way, Z, ((0.0, numbers.RADIUS), (numbers.LENGTH * 0.55, numbers.RADIUS * 0.75),
                                                    (numbers.LENGTH, 0.0))), 4, torso.named("Fur" if index % 2 == 0 else "FurShade"), 0.0)


GOLD_SHINE = {
    # The streaks of light of the gold Chog (#1373: it is polished, with a white-yellow light running along its edges): the places of each
    # (to the side, above), its width; the glints in the eyes: to the side of the middle of each eye, above it, radius.
    "STREAKS": (((-0.14, 0.150), (-0.04, 0.178), (0.06, 0.176)), ((-0.208, 0.020), (-0.222, -0.030), (-0.214, -0.075))),
    "WIDTHS": (0.014, 0.020),
    "GLINTS": ((-0.020, 0.030, 0.014), (0.016, -0.026, 0.008)),
}


def build_gold_shine(maker):
    """#1373: the white-yellow streaks of light on the polished gold and the glints of its eyes."""
    import make_chog_traits as host
    numbers = SimpleNamespace(**GOLD_SHINE)
    part, surface = maker.part(), maker.surface
    for places, width in zip(numbers.STREAKS, numbers.WIDTHS):
        face_stroke(part, surface, part.named("Shine"), list(places), [width * 0.5, width, width * 0.4], host.DRAWN[2])
    for sign in (1.0, -1.0):
        for side, above, radius in numbers.GLINTS:
            face_patch(part, surface, part.named("Shine"), (sign * (0.112 + side), 0.006 + above), radius, radius * 1.2, host.DRAWN[3], 8)


DEVIL_HORNS = {
    # (23 October 2026, from #1376: the horns are glossy red drops standing on the brow, curving outward, with a white light along each; the eyes are large and
    # glossy with a white glint. Until then the ridged horns of the radioactive Chogs in dark red.) Where each stands (to the side, above) on the face, how far
    # out of the head it leans, its height, the radius profile along it.
    "AT": (0.185, 0.190), "LEAN": 0.60, "PROFILE": ((0.0, 0.078), (0.060, 0.088), (0.120, 0.064), (0.180, 0.032), (0.235, 0.0)),
    "GLINTS": ((-0.022, 0.030, 0.013),),
}


def build_devil_horns(maker):
    """#1376: two glossy red drop horns on the brow and the white glints of the eyes."""
    import make_chog_traits as host
    numbers = SimpleNamespace(**DEVIL_HORNS)
    part, surface = maker.part(), maker.surface
    for sign in (1.0, -1.0):
        root = surface.front(sign * numbers.AT[0], numbers.AT[1], 0.0)
        out = host.outward(surface, sign * numbers.AT[0], numbers.AT[1])
        way = (Z * 1.0 + X * sign * numbers.LEAN + out * 0.25).normalized()
        part.shell(base.tube_rings(root - way * 0.03, way, out, numbers.PROFILE), 8, part.named("Horn"), 0.012)
        for side, above, radius in numbers.GLINTS:
            face_patch(part, surface, part.named("Glint"), (sign * (0.112 + side), 0.006 + above), radius, radius * 1.2, host.DRAWN[3], 8)


DEVIL_WINGS = {
    # The bat wings and the tail of the devil (the pictures draw dark red wings behind it and a tail with an arrow tip). Each wing: where it leaves the back (to the side,
    # behind, height), its way out (to the side, back, up), its span and height, the points of its edge (how far out, how high, shares of the span and the height).
    "ROOT": (0.06, 0.13, 0.46), "WAY": (1.0, 0.25, 0.30), "UP": (0.0, 0.30, 0.95), "SPAN": 0.56, "TALL": 0.40, "THICK": 0.010,
    "EDGE": ((0.0, 0.0), (0.38, 0.92), (0.98, 0.70), (0.80, 0.34), (0.96, 0.02), (0.64, 0.0), (0.66, -0.40), (0.38, -0.16), (0.12, -0.28)),
    "TAIL": ((0.0, 0.14, 0.24), (0.0, 0.24, 0.17), (0.07, 0.31, 0.19), (0.12, 0.34, 0.29)), "TAIL_RADIUS": (0.030, 0.026, 0.020, 0.014),
    "ARROW": (0.060, 0.070, 0.012),
}


def add_devil_wings(maker):
    """The two bat wings and the tail with its arrow tip, on the body of the devil."""
    import make_chog_traits as host
    numbers = SimpleNamespace(**DEVIL_WINGS)
    torso = maker.parts[0][0]
    for sign in (1.0, -1.0):
        root = Vector((sign * numbers.ROOT[0], numbers.ROOT[1], numbers.ROOT[2]))
        out = Vector((sign * numbers.WAY[0], numbers.WAY[1], numbers.WAY[2])).normalized()
        up = Vector(numbers.UP).normalized()
        up = (up - out * up.dot(out)).normalized()
        edge = [root + out * numbers.SPAN * across + up * numbers.TALL * high for across, high in numbers.EDGE]
        normal = out.cross(up).normalized()
        host.add_loops(torso, [[point - normal * numbers.THICK for point in edge], [point + normal * numbers.THICK for point in edge]],
                       torso.named("Wing"), 0.010)
    points = [Vector(point) for point in numbers.TAIL]
    rings = []
    for index, (point, radius) in enumerate(zip(points, numbers.TAIL_RADIUS)):
        ahead = points[min(index + 1, len(points) - 1)] - points[max(index - 1, 0)]
        first, second = base.frame_across(ahead, X)
        rings.append((point, first * radius, second * radius))
    torso.shell(rings, 6, torso.named("Wing"), 0.010)
    wide, long, thick = numbers.ARROW
    end = points[-1]
    way = (points[-1] - points[-2]).normalized()
    side = way.cross(X).normalized() if abs(way.dot(X)) < 0.95 else Y
    tip = end + way * long
    diamond = [end + side * wide, tip, end - side * wide, end - way * 0.012]
    normal = way.cross(side).normalized()
    host.add_loops(torso, [[point - normal * thick for point in diamond], [point + normal * thick for point in diamond]],
                   torso.named("Wing"), 0.008)


# ---------------------------------------------------------------------------------------------
# The tables the builder reads.
# ---------------------------------------------------------------------------------------------

HAT_SHAPES = {"Halo": build_halo, "SkullFlames": build_skull_flames, "Brain": build_brain,
              "GoldHelmet": build_gold_helmet, "ScreamHands": build_scream_hands, "MangaBrows": build_manga_brows,
              "GoldShine": build_gold_shine, "DevilHorns": build_devil_horns}
CLOTH_KINDS = {"Cape": build_cape, "Wings": build_wings, "DevilSuit": build_devil_suit, "FrogSuit": build_frog_suit,
               "WarriorTee": build_warrior_tee}


def hats(headband_numbers):
    """The hats of the 1:1 and their numbers (the helmet lies where a headband does; the devil's horns are
    the ridged horns of the radioactive Chogs, one pair of them)."""
    entries = {
        "1:1 Halo": {"shape": "Halo", "colours": {"Halo": (0.99, 0.78, 0.18), "Brow": (0.99, 0.99, 0.97)}, "unlit": ("Brow",)},
        "1:1 Skull Flames": {"shape": "SkullFlames", "colours": {"Fire": (0.98, 0.42, 0.08), "Core": (1.0, 0.86, 0.22)},
                             "unlit": ("Fire", "Core")},
        "1:1 Brain": {"shape": "Brain", "colours": {"Brain": (0.95, 0.52, 0.56), "Groove": (0.82, 0.36, 0.42),
                                                  "Blood": (0.66, 0.09, 0.11), "Slime": (0.55, 0.90, 0.30)},
                      "unlit": ("Blood", "Slime")},
        "1:1 Scream Hands": {"shape": "ScreamHands", "colours": {"Hand": (0.94, 0.38, 0.62)}},
        "1:1 Manga Brows": {"shape": "MangaBrows", "colours": {"Brow": (0.05, 0.05, 0.06), "Shade": (0.55, 0.55, 0.57)},
                            "unlit": ("Brow", "Shade")},
        "1:1 Gold Helmet": {"shape": "GoldHelmet", "colours": {"Band": (0.98, 0.76, 0.16), "Line": (0.78, 0.52, 0.08),
                                                                "Gem": (0.88, 0.10, 0.14), "Paint": (0.93, 0.30, 0.40),
                                                                "Glint": (1.0, 1.0, 1.0)}, "unlit": ("Paint", "Glint")},
        "1:1 Devil Horns": {"shape": "DevilHorns", "colours": {"Horn": (0.98, 0.10, 0.12), "Glint": (1.0, 0.96, 0.96)}, "unlit": ("Glint",)},
        "1:1 Gold Shine": {"shape": "GoldShine", "colours": {"Shine": (1.0, 0.97, 0.76)}, "unlit": ("Shine",)},
    }
    numbers = {
        "Halo": {"OUTLINE_THICKNESS": 0.012},
        "SkullFlames": {"OUTLINE_THICKNESS": 0.012},
        # The brain comes out where the upper and the middle pairs of leaves grow: those are left out.
        "Brain": {"OUTLINE_THICKNESS": 0.012,
                  "LEAVES": tuple((0.0, None, None) if line in (0, 1, 2) else (1.0, None, None) for line in range(7))},
        "GoldHelmet": dict(headband_numbers),
        "ScreamHands": {"OUTLINE_THICKNESS": 0.012},
        "MangaBrows": {"OUTLINE_THICKNESS": 0.012},
        "GoldShine": {"OUTLINE_THICKNESS": 0.012},
        "DevilHorns": {"OUTLINE_THICKNESS": 0.012},
    }
    return entries, numbers


def with_colours(entry, **colours):
    return {**entry, "colours": {**entry["colours"], **colours}}


def clothes(tee, costume):
    """The clothing of the 1:1 ("tee" and "costume" make entries as chog_trait_hats does)."""
    return {
        "1:1 Flame Cape": {"kind": "Cape", "colours": {"Cape": (0.84, 0.15, 0.12), "Collar": (0.45, 0.22, 0.12),
                                                         "Clasp": (0.96, 0.70, 0.16)}},
        "1:1 Scream Coat": tee((0.20, 0.15, 0.46)),
        "1:1 Zombie Tee": tee((0.45, 0.30, 0.62)),
        "1:1 Manga Turtleneck": {**tee((0.13, 0.13, 0.14), (0.09, 0.09, 0.10)),
                                 "numbers": {"COLLAR": (0.600, 0.640)}},
        "1:1 Warrior Tee": with_colours({**tee((0.45, 0.22, 0.62), (0.96, 0.62, 0.18)), "kind": "WarriorTee"},
                                        Fur=(0.98, 0.78, 0.26), FurShade=(0.92, 0.58, 0.16)),
        "1:1 Angel Wings": {"kind": "Wings", "colours": {"Feather": (0.98, 0.98, 0.97), "Shade": (0.86, 0.87, 0.92)}},
        "1:1 Devil Suit": {"kind": "DevilSuit", "colours": {"Main": (0.42, 0.06, 0.10), "Collar": (0.26, 0.03, 0.06),
                                                            "Trident": (0.56, 0.30, 0.80), "Wing": (0.52, 0.07, 0.12)}},
        "1:1 Frog Suit": {**costume((0.46, 0.74, 0.20), {"Hood": (0.46, 0.74, 0.20), "Rim": (0.80, 0.86, 0.24),
                                                          "EyeBall": (0.97, 0.97, 0.94), "Pupil": DARK,
                                                          "Lip": (0.90, 0.42, 0.44), "LipLine": (0.40, 0.14, 0.16),
                                                          "Spike": (0.78, 0.88, 0.28)}),
                          "kind": "FrogSuit", "pepe": True},
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


TUMBLE = 1   # chog_attack_objects.TUMBLE
# (23 October 2026: the Scream, the manga page and the gold Chog now wear a value of "Head", the strokes and the hands of their faces: they still throw a spike
# of their crown, as before, not those.)
ATTACK_OF_HEAD = {"1:1 Devil Horns": ("Trident", POINT), "1:1 Scream Hands": ("Leaf", TUMBLE), "1:1 Manga Brows": ("Leaf", TUMBLE),
                  "1:1 Gold Shine": ("Leaf", TUMBLE)}
ATTACK_OBJECTS = {"Trident": ({"Trident": (0.56, 0.30, 0.80)}, (), build_trident_object, (0.70, 0.45, 0.95))}
ATTACK_WORDS = {"Trident": "TRIDENTS"}
LINES = {
    434: "THROWS THE SPIKES OF ITS CROWN",
    1006: "THROWS THE SPIKES OF ITS CROWN",
    53: "THROWS THE FLAMES OF ITS CROWN",
    392: "THROWS ITS FLAMES",
    494: "THROWS ITS BRAIN",
    1159: "THROWS ITS HALO",
    1290: "THROWS ITS HELMET",
    1373: "THROWS GOLDEN SPIKES",
    1376: "THROWS ITS TRIDENT",
}
