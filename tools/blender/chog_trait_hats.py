"""More hat shapes, and the clothing of the most worn Body values, for make_chog_traits.py. Pictures
only: nothing here goes into the game.

make_chog_traits.py imports this module, adds its tables to its own (HATS, HATS_OF_SIDE, CLOTHES,
CLOTHES_OF_SIDE) and its builders to HAT_SHAPES and CLOTH_KINDS, then calls bind() with itself, so
the builders here use its helpers (add_loops, CapCrown, add_dome, build_tee, ...) when they run.

Which shape a value of "Head" is: tools/genesis-art/trait_coverage.py, HEAD_SHAPES. Each value was
assigned there from its pictures (on each Side where it is worn), never from its name: "Balls Cap"
or "Dollar Cap" are bucket hats, "MCGA" or "NEET" are caps, "Mon Cap" is a bucket hat on the pictures
of Side=Right and a cap on those of Side=Left. The shapes are built in the order of the Genesis they
cover: Cap 592, BucketCap 490, Beanie 129 (built before), Ushanka 67, Headband 65, CurlyWig 63,
Crown 54 (built before), Durag 50, WizardHat 47, SpartanHelmet 40. With these ten, 1,850 of the 1,969
Genesis wear the right hat shape, the 253 without a hat included.

Colours: read on the layer of each value rebuilt from all its pictures (the pixels that agree across
the Genesis that wear it and differ from the median of their Side), measured in sRGB, 14 October 2026.
Prints (15 October 2026): the word of each cap in the font built into Blender ("print", one line or
several), the logos of lay_logo (Bitcoin B, Monad mark, star, peace sign, bolt, cloud) on caps and bucket
hats, the patches of lay_patch on the beanies. What the pictures show and no part draws is said in the
entry ("print_missing"); make_chog_traits.py writes it into parts.json, and the review sheets print it.

What these shapes ask of the camera of the game (73 degrees above, head turned up by 20): nothing
reaches forward over the brow. The fur of the ushanka, the band, the curls and the helmet stand less
far off the head at the front than at the sides and the back.
"""

import math
from types import SimpleNamespace

from mathutils import Vector

import make_chog_full as full

base = full.base
X, Y, Z, NOWHERE = base.X, base.Y, base.Z, base.NOWHERE

KIT = None   # make_chog_traits, given by bind()


def bind(kit):
    """Gives the builders the helpers of make_chog_traits.py (the module itself)."""
    global KIT
    KIT = kit


# ---------------------------------------------------------------------------------------------
# What the new shapes share: a shell that follows the egg of the head, whose rim can be higher at
# the front than at the sides (to leave the face open), as rows from the rim up to the top.
# Angles seen from above are those of CapCrown in make_chog_traits.py: 0 is the left of the
# character (+X), 270 degrees the front (-Y). "From the front" is 0 at the front, 180 at the back.
# A polar angle is counted from straight up.
# ---------------------------------------------------------------------------------------------

def from_front(angle):
    """Degrees from the front (0) round to the back (180) of an angle seen from above."""
    degrees = math.degrees(angle) % 360.0
    return abs((degrees - 270.0 + 180.0) % 360.0 - 180.0)


def interpolate(table, at):
    """The value of a table of (at, value) pairs, straight lines between them."""
    if at <= table[0][0]:
        return table[0][1]
    for (first, value), (second, then) in zip(table, table[1:]):
        if at <= second:
            return value + (then - value) * (at - first) / (second - first)
    return table[-1][1]


def on_head(surface, angle, polar, size):
    """A point of the egg of the head made "size" times larger, at an angle seen from above and a
    polar angle (radians)."""
    half = surface.half
    return surface.middle + Vector((half.x * size * math.sin(polar) * math.cos(angle),
                                    half.y * size * math.sin(polar) * math.sin(angle),
                                    half.z * size * math.cos(polar)))


def head_normal(surface, angle, polar):
    """The way out of the egg of the head at that place."""
    point = on_head(surface, angle, polar, 1.0) - surface.middle
    half = surface.half
    return Vector((point.x / half.x ** 2, point.y / half.y ** 2, point.z / half.z ** 2)).normalized()


def dome(part, surface, material, sides, rim, rows, size, outline, tuck=0.90, swell=None):
    """A closed shell over the head: a row tucked inside the head under the rim, the rim, "rows" - 1
    rows up to the top, the top. "rim" is a table (degrees from the front, polar degrees of the
    rim); "size" how much larger than the head it is; "swell(angle, polar, row)", when given, is
    added to the size of each point (curls, puffs). Returns the faces of each band, from the rim."""
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(rim, from_front(angle))) for angle in angles]
    loops = [[on_head(surface, angle, polar, tuck) for angle, polar in zip(angles, rims)]]
    for row in range(rows):
        share = row / rows
        loop = []
        for angle, polar in zip(angles, rims):
            here = polar * (1.0 - share)
            loop.append(on_head(surface, angle, here, size + (swell(angle, here, row) if swell else 0.0)))
        loops.append(loop)
    loops.append([on_head(surface, 0.0, 0.0, size)])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * rows + [(1.0,)]
    bands, _ = KIT.add_loops(part, loops, material, outline, shares=shares)
    return bands


def slab(part, path, wide, thick, material, outline, sides=6):
    """A flat strip along a path of points: "wide" and "thick" give, for each point, the half width
    (a vector) and the half thickness (a vector) of its section. Both ends are closed."""
    rings = [(point, across, through) for point, across, through in zip(path, wide, thick)]
    return part.shell(rings, sides, material, outline)


def disc_on(part, surface, centre, out, radius, material, segments=8, inner=None):
    """A flat disc (a ring when "inner" is given), or a star with "inner" as the radius of its
    hollows and "segments" its points times two, laid at "centre" facing "out"."""
    first = out.cross(Z if abs(out.dot(Z)) < 0.9 else X).normalized()
    second = out.cross(first).normalized()
    edge = []
    for index in range(segments):
        angle = 2.0 * math.pi * index / segments + math.pi * 0.5
        reach = radius if inner is None or index % 2 == 0 else inner
        edge.append(centre + (first * math.cos(angle) + second * math.sin(angle)) * reach)
    return full.add_skin(part, surface, [[centre + out * 0.002], edge], material)


def paint_where(part, faces, rule):
    """Gives each face the material that "rule(centre of the face)" names (None: unchanged)."""
    for face in faces:
        name = rule(face.calc_center_median())
        if name:
            part.paint([face], part.named(name))


# ---------------------------------------------------------------------------------------------
# Ushanka (Russian Hat): a cloth crown, a thick roll of fur around the brow, two flaps of fur
# or leather hanging beside the face. Side=Right draws it tan with fur flaps; Side=Left cream with
# dark leather flaps and two white snaps on each.
# ---------------------------------------------------------------------------------------------
USHANKA = {
    "SIDES": 16,
    # The roll of fur: polar degrees of its lower and upper edge, by degrees from the front, and
    # how much larger than the head it is at its middle. Lower at the back, as a fur hat sits.
    # (The front roll is kept over the brow and close to the head: the eyes lie just under it.)
    "FUR_LOWER": ((0.0, 76.0), (90.0, 86.0), (180.0, 92.0)),
    "FUR_UPPER": ((0.0, 52.0), (90.0, 58.0), (180.0, 60.0)),
    "FUR_SIZE": ((0.0, 1.16), (90.0, 1.24), (180.0, 1.24)),
    "FUR_WAVES": (9, 0.035),          # bumps of the fur around the head, how deep against its size
    # The crown: how much larger than the head, how many rows from inside the fur to the top.
    "CROWN_SIZE": 1.10,
    "CROWN_ROWS": 3,
    # An ear flap: degrees from the front, polar degrees where it starts (inside the fur) and ends,
    # how far it stands off the head at its top and at its foot, half width, half thickness.
    "FLAP": (66.0, 80.0, 132.0, 1.10, 1.20, 0.072, 0.022),
    "SNAPS": ((0.45, 0.013), (0.78, 0.013)),   # along the flap from its top, radius
    "OUTLINE_THICKNESS": 0.020,
}


def build_ushanka(maker):
    numbers = maker.numbers(USHANKA)
    part, surface = maker.part(), maker.surface
    sides, outline = numbers.SIDES, numbers.OUTLINE_THICKNESS
    angles = [2.0 * math.pi * index / sides for index in range(sides)]

    # The fur: four rows, both edges on the head, the two middle rows standing off it.
    count, depth = numbers.FUR_WAVES
    loops = []
    for row, (share_up, sized) in enumerate(((0.0, 0.0), (0.30, 1.0), (0.72, 1.0), (1.0, 0.0))):
        loop = []
        for angle in angles:
            front = from_front(angle)
            low, high = math.radians(interpolate(numbers.FUR_LOWER, front)), math.radians(
                interpolate(numbers.FUR_UPPER, front))
            polar = low + (high - low) * share_up
            size = 1.0 + (interpolate(numbers.FUR_SIZE, front) - 1.0) * sized
            size *= 1.0 + depth * sized * math.cos(count * angle + row * math.pi)
            loop.append(on_head(surface, angle, polar, size))
        loops.append(loop)
    none, whole = (0.0,) * sides, (1.0,) * sides
    KIT.add_loops(part, loops, part.named("Fur"), outline, shares=[none, whole, whole, none])

    # The crown, from inside the fur to the top.
    crown_rim = tuple((front, polar + 6.0) for front, polar in numbers.FUR_UPPER)
    dome(part, surface, part.named("Crown"), sides, crown_rim, numbers.CROWN_ROWS, numbers.CROWN_SIZE, outline,
         tuck=1.0)

    # The flaps, beside the face.
    degrees, start, end, size_top, size_foot, half_wide, half_thick = numbers.FLAP
    colours = maker.entry["colours"]
    for side in (1.0, -1.0):
        angle = math.radians(270.0 + side * degrees)
        stations = 4
        path, wide, thick = [], [], []
        for index in range(stations):
            share = index / (stations - 1)
            polar = math.radians(start + (end - start) * share)
            path.append(on_head(surface, angle, polar, size_top + (size_foot - size_top) * share))
            across = Vector((-math.sin(angle), math.cos(angle), 0.0))
            rounded = 1.0 if index < stations - 1 else 0.55
            wide.append(across * half_wide * rounded)
            thick.append(head_normal(surface, angle, polar) * half_thick)
        slab(part, path, wide, thick, part.named("Flap"), outline)
        if "Snap" in colours:
            for along, radius in numbers.SNAPS:
                polar = math.radians(start + (end - start) * along)
                out = head_normal(surface, angle, polar)
                centre = on_head(surface, angle, polar, size_top + (size_foot - size_top) * along) + out * (
                    half_thick + 0.004)
                disc_on(part, surface, centre, out, radius, part.named("Snap"))


# ---------------------------------------------------------------------------------------------
# Headband: a band around the brow, under the leaves, a darker line along its lower edge, and,
# for the Japanese one, a red sun in the middle of the front.
# ---------------------------------------------------------------------------------------------
HEADBAND = {
    "SIDES": 18,
    "LOWER": ((0.0, 74.0), (90.0, 72.0), (180.0, 66.0)),
    "UPPER": ((0.0, 52.0), (90.0, 50.0), (180.0, 46.0)),
    "SIZE": ((0.0, 1.075), (90.0, 1.10), (180.0, 1.10)),
    "LINE": 0.30,                # the share of the band, from its lower edge, in the colour "Line"
    "SUN": (0.046, 0.008),       # radius, lift off the band
    "OUTLINE_THICKNESS": 0.016,
}


def build_headband(maker):
    numbers = maker.numbers(HEADBAND)
    part, surface = maker.part(), maker.surface
    sides = numbers.SIDES
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rows = ((0.0, 0.0), (numbers.LINE, 1.0), (0.85, 1.0), (1.0, 0.0))
    loops = []
    for share_up, sized in rows:
        loop = []
        for angle in angles:
            front = from_front(angle)
            low, high = math.radians(interpolate(numbers.LOWER, front)), math.radians(interpolate(numbers.UPPER, front))
            size = 1.0 + (interpolate(numbers.SIZE, front) - 1.0) * sized
            loop.append(on_head(surface, angle, low + (high - low) * share_up, size))
        loops.append(loop)
    none, whole = (0.0,) * sides, (1.0,) * sides
    bands, _ = KIT.add_loops(part, loops, part.named("Band"), numbers.OUTLINE_THICKNESS,
                             shares=[none, whole, whole, none])
    if "Line" in maker.entry["colours"]:
        part.paint(bands[0], part.named("Line"))
    if "Sun" in maker.entry["colours"]:
        front = math.radians(270.0)
        low, high = math.radians(interpolate(numbers.LOWER, 0.0)), math.radians(interpolate(numbers.UPPER, 0.0))
        polar = low + (high - low) * 0.55
        radius, lift = numbers.SUN
        size = interpolate(numbers.SIZE, 0.0)
        out = head_normal(surface, front, polar)
        disc_on(part, surface, on_head(surface, front, polar, size) + out * lift, out, radius, part.named("Sun"),
                segments=10)


# ---------------------------------------------------------------------------------------------
# Curly wig (Bob Hair, Rainbow Wig): curls over the top, the sides and the back of the head,
# leaving the face open. Its colour is laid by place: "Top" over "Curl" (Bob Hair: a red top over
# yellow curls), or the colours of "sweep" in an arch from the right of the character over the top
# to its left (Rainbow Wig).
# ---------------------------------------------------------------------------------------------
CURLY_WIG = {
    "SIDES": 24,
    # Polar degrees of the edge of the curls, by degrees from the front: over the brow at the front,
    # down to the ears at the sides and at the back.
    "RIM": ((0.0, 54.0), (30.0, 62.0), (62.0, 108.0), (180.0, 112.0)),
    "ROWS": 5,
    # How much larger than the head the mass of curls is, by degrees from the front: close over the
    # brow (the camera of the game looks down on the face), a big puff at the sides and at the back,
    # as the pictures draw it, twice as wide as the face.
    # (First: 1.14 all around with shallow waves. On the bust it was a thin painted cap under the
    # leaves, and from above a zigzag of two colours: no hair.)
    "SIZE": ((0.0, 1.16), (40.0, 1.34), (90.0, 1.46), (180.0, 1.42)),
    # The curls: lobes around the head, how deep a valley between two lobes is against the size;
    # each row of lobes is shifted by half a lobe from the one under it.
    "CURLS": (8, 0.16),
    "TOP_POLAR": 40.0,           # with "Top": faces nearer the top than this take it
    "OUTLINE_THICKNESS": 0.022,
}


def build_curly_wig(maker):
    numbers = maker.numbers(CURLY_WIG)
    part, surface = maker.part(), maker.surface
    count, depth = numbers.CURLS
    sides, rows = numbers.SIDES, numbers.ROWS
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(numbers.RIM, from_front(angle))) for angle in angles]

    def size_at(angle, polar, row):
        """The mass of curls: rounded lobes with narrow valleys between them (a scallop), so the
        outline of the wig reads as curls; less deep near the top, where the rows close in."""
        big = interpolate(numbers.SIZE, from_front(angle))
        lobe = abs(math.sin(count * angle * 0.5 + (row % 2) * math.pi * 0.5))
        fade = 0.45 + 0.55 * math.sin(polar)
        return big * (1.0 - depth * fade * (1.0 - lobe ** 0.5))

    loops = [[on_head(surface, angle, polar, 0.90) for angle, polar in zip(angles, rims)]]
    for row in range(rows):
        share = row / rows
        loops.append([on_head(surface, angle, polar * (1.0 - share), size_at(angle, polar * (1.0 - share), row))
                      for angle, polar in zip(angles, rims)])
    loops.append([on_head(surface, 0.0, 0.0, interpolate(numbers.SIZE, 90.0))])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * rows + [(1.0,)]
    colours = maker.entry["colours"]
    first = "Curl" if "Curl" in colours else maker.entry["sweep"][0]
    bands, _ = KIT.add_loops(part, loops, part.named(first), numbers.OUTLINE_THICKNESS, shares=shares)
    faces = [face for band in bands for face in band]
    middle = surface.middle
    if "Top" in colours:
        def rule(centre):
            away = centre - middle
            polar = math.degrees(math.acos(max(-1.0, min(1.0, away.z / max(away.length, 1e-9)))))
            return "Top" if polar < numbers.TOP_POLAR else None
        paint_where(part, faces, rule)
    sweep = maker.entry.get("sweep")
    if sweep:
        def rule(centre):
            away = centre - middle
            arch = math.degrees(math.atan2(away.z, away.x))      # 0: left of the character, 90: top
            share = (205.0 - min(max(arch, -25.0), 205.0)) / 230.0
            return sweep[min(int(share * len(sweep)), len(sweep) - 1)]
        paint_where(part, faces, rule)


# ---------------------------------------------------------------------------------------------
# Durag: a tight cloth over the top and the back of the head, a seam over the brow, two tails
# hanging at the back.
# ---------------------------------------------------------------------------------------------
DURAG = {
    "SIDES": 18,
    "RIM": ((0.0, 66.0), (50.0, 72.0), (110.0, 100.0), (180.0, 110.0)),
    # (1.06 and 1.085 until 15 October 2026: the teeth of hair the base draws on the brow came through
    # the cloth as small triangles of skin.)
    "SIZE": 1.10,
    "SEAM": (7.0, 1.12),         # polar degrees above the rim, size: the band over the brow
    "ROWS": 2,
    # A tail: degrees from the back to each side, polar degrees where it starts, length, half
    # width, half thickness, how far it leans back.
    "TAIL": (13.0, 104.0, 0.20, 0.034, 0.010, 0.55),
    "OUTLINE_THICKNESS": 0.018,
}


def build_durag(maker):
    numbers = maker.numbers(DURAG)
    part, surface = maker.part(), maker.surface
    sides, outline = numbers.SIDES, numbers.OUTLINE_THICKNESS
    seam_up, seam_size = numbers.SEAM
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(numbers.RIM, from_front(angle))) for angle in angles]
    loops = [[on_head(surface, angle, polar, 0.92) for angle, polar in zip(angles, rims)],
             [on_head(surface, angle, polar, numbers.SIZE) for angle, polar in zip(angles, rims)],
             [on_head(surface, angle, polar - math.radians(seam_up), seam_size) for angle, polar in zip(angles, rims)]]
    for row in range(1, numbers.ROWS + 1):
        share = row / (numbers.ROWS + 1)
        loops.append([on_head(surface, angle, (polar - math.radians(seam_up)) * (1.0 - share), numbers.SIZE)
                      for angle, polar in zip(angles, rims)])
    loops.append([on_head(surface, 0.0, 0.0, numbers.SIZE)])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * (len(loops) - 2) + [(1.0,)]
    bands, _ = KIT.add_loops(part, loops, part.named("Cloth"), outline, shares=shares)
    part.paint(bands[1], part.named("Seam"))

    away, start, length, half_wide, half_thick, lean = numbers.TAIL
    for side in (1.0, -1.0):
        angle = math.radians(90.0 + side * away)
        polar = math.radians(start)
        top = on_head(surface, angle, polar, numbers.SIZE * 0.96)
        out = head_normal(surface, angle, polar)
        down = (-Z + Y * lean).normalized()
        across = out.cross(down).normalized()
        path = [top - down * 0.02, top + down * length * 0.5 + out * 0.02, top + down * length + out * 0.05]
        slab(part, path, [across * half_wide, across * half_wide, across * half_wide * 0.8],
             [out * half_thick] * 3, part.named("Seam"), outline * 0.7, sides=4)


# ---------------------------------------------------------------------------------------------
# Wizard hat: a tall soft cone sitting on the brow, its point bent back, yellow stars on it.
# ---------------------------------------------------------------------------------------------
WIZARD_HAT = {
    "RIM_HALF_WIDTH": 0.318,
    "RIM_HALF_DEPTH": 0.284,
    "SQUARENESS": 2.1,
    # From the rim up: height, size against the rim, how far it is moved back.
    "RINGS": (
        (0.050, 1.040, 0.000),
        (0.130, 1.000, 0.006),
        (0.240, 0.830, 0.030),
        (0.360, 0.600, 0.075),
        (0.470, 0.390, 0.135),
        (0.560, 0.210, 0.205),
    ),
    "TOP": (0.610, 0.270),
    "SIDES": 16,
    # The stars: degrees from the front, height, radius. Five points each.
    "STARS": (
        (0.0, 0.175, 0.046), (52.0, 0.150, 0.042), (-52.0, 0.150, 0.042), (110.0, 0.165, 0.042),
        (-110.0, 0.165, 0.042), (165.0, 0.170, 0.042), (-165.0, 0.170, 0.042), (26.0, 0.300, 0.036),
        (-26.0, 0.300, 0.036), (90.0, 0.310, 0.034), (-90.0, 0.310, 0.034), (150.0, 0.300, 0.034),
        (0.0, 0.420, 0.028), (-125.0, 0.420, 0.026), (125.0, 0.420, 0.026),
    ),
    "STAR_HOLLOW": 0.45,
    "STAR_LIFT": 0.008,
    "OUTLINE_THICKNESS": 0.020,
}


def build_wizard_hat(maker):
    numbers = maker.numbers(WIZARD_HAT)
    part, surface = maker.part(), maker.surface
    wall = KIT.CapCrown(numbers, surface.middle)
    KIT.add_dome(part, wall, part.named("Cloth"), numbers.OUTLINE_THICKNESS)
    for degrees, height, radius in numbers.STARS:
        here, along, upwards, out = wall.frame(degrees, height)
        disc_on(part, surface, here + out * numbers.STAR_LIFT, out, radius, part.named("Star"), segments=10,
                inner=radius * numbers.STAR_HOLLOW)


# ---------------------------------------------------------------------------------------------
# Spartan helmet: a gold shell over the head, open in front of the eyes, the nose and the mouth,
# with cheek guards down the sides of the face, a nose guard between the eyes and a red crest from
# the brow over the top to the back.
# ---------------------------------------------------------------------------------------------
SPARTAN = {
    "SIDES": 24,
    # Polar degrees of the edge by degrees from the front: over the brow just above the eyes, then
    # down beside the eyes over the cheeks (the guards), lower at the sides than at the back.
    "RIM": ((0.0, 60.0), (28.0, 61.0), (40.0, 114.0), (95.0, 122.0), (180.0, 106.0)),
    # It stands clear of everything drawn on the face (up to 0.040 off the head).
    # (First: 1.075, a rim over the eyes at 66 degrees and guards from 38 degrees. The shell then ran
    # through the cream face and the eyes: the bust showed gold patches over the face.)
    "SIZE": 1.17,
    "ROWS": 3,
    # The trim along the opening, in its own colour: how far up from the rim, against the first row.
    "TRIM": 0.30,
    # The nose guard: polar degrees where it starts and ends, half width, half thickness.
    # (First: down to 92 degrees, over the nose.)
    "NOSE_GUARD": (58.0, 84.0, 0.022, 0.012),
    # The crest: along the middle of the head, (degrees from straight up, towards the back when
    # positive; height above the shell), and its half width. Tall enough to stand out of the leaves
    # of the head that grow at the same place. (First: 0.145 at most and 0.034 wide: hidden in them.)
    "CREST": ((-42.0, 0.060), (-24.0, 0.170), (0.0, 0.250), (30.0, 0.270), (62.0, 0.240), (95.0, 0.150),
              (118.0, 0.050)),
    "CREST_HALF_WIDTH": 0.042,
    "CREST_SIDES": 6,
    "OUTLINE_THICKNESS": 0.020,
}


def build_spartan(maker):
    numbers = maker.numbers(SPARTAN)
    part, surface = maker.part(), maker.surface
    outline = numbers.OUTLINE_THICKNESS
    sides = numbers.SIDES
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(numbers.RIM, from_front(angle))) for angle in angles]
    # The shell: tucked inside the head under the rim, the rim, a row just above it (the trim), then
    # rows up to the top.
    loops = [[on_head(surface, angle, polar, 0.92) for angle, polar in zip(angles, rims)],
             [on_head(surface, angle, polar, numbers.SIZE) for angle, polar in zip(angles, rims)]]
    trim_row = [on_head(surface, angle, polar * (1.0 - numbers.TRIM / numbers.ROWS), numbers.SIZE)
                for angle, polar in zip(angles, rims)]
    loops.append(trim_row)
    for row in range(1, numbers.ROWS):
        share = row / numbers.ROWS
        loops.append([on_head(surface, angle, polar * (1.0 - share), numbers.SIZE) for angle, polar in zip(angles, rims)])
    loops.append([on_head(surface, 0.0, 0.0, numbers.SIZE)])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * (len(loops) - 2) + [(1.0,)]
    bands, _ = KIT.add_loops(part, loops, part.named("Gold"), outline, shares=shares)
    if "Trim" in maker.entry["colours"]:
        part.paint(bands[1], part.named("Trim"))

    start, end, half_wide, half_thick = numbers.NOSE_GUARD
    front = math.radians(270.0)
    path, wide, thick = [], [], []
    for index, polar_deg in enumerate((start, (start + end) * 0.5, end)):
        polar = math.radians(polar_deg)
        out = head_normal(surface, front, polar)
        path.append(on_head(surface, front, polar, numbers.SIZE) - out * (0.01 if index == 0 else 0.0))
        wide.append(X * half_wide * (1.0 if index < 2 else 0.8))
        thick.append(out * half_thick)
    slab(part, path, wide, thick, part.named("Gold"), outline * 0.7, sides=4)

    # The crest: rings across the middle of the head, each from inside the shell to its height.
    path, wide, thick = [], [], []
    for pitch_deg, height in numbers.CREST:
        pitch = math.radians(pitch_deg)
        angle = math.radians(90.0 if pitch_deg >= 0.0 else 270.0)
        polar = abs(pitch)
        out = head_normal(surface, angle, polar)
        foot = on_head(surface, angle, polar, numbers.SIZE) - out * 0.02
        path.append(foot + out * (height + 0.02) * 0.5)
        wide.append(X * numbers.CREST_HALF_WIDTH)
        thick.append(out * (height + 0.02) * 0.5)
    slab(part, path, wide, thick, part.named("Crest"), outline, sides=numbers.CREST_SIDES)


# ---------------------------------------------------------------------------------------------
# Logos on the front of the crown of a cap or a bucket hat (added 15 October 2026): flat shapes laid
# on the wall of the hat, made of geometry. Each is drawn in a square of half size "size" around the
# height "height" of the middle of the front. Read on the pictures: the Bitcoin B of "BTC Cap", the
# Monad mark of "Mon Cap" (both Sides), the star of "Star Cap", the peace sign of "Peace hat" and "Red
# Peace Cap", the bolt of "Thunder Cap", the cloud of "Cloud Cap".
# ---------------------------------------------------------------------------------------------

def wall_shape(part, crown, material, edge, lift, middle):
    """A flat shape on the front of the crown: its edge as places (across, height), seen whole from
    "middle"."""
    full.add_skin(part, SimpleNamespace(middle=crown.middle),
                  [[crown.front(middle[0], middle[1], lift)], [crown.front(x, h, lift) for x, h in edge]], material)


def wall_ring(part, crown, material, inner, outer, lift):
    """A ring on the front of the crown between two closed edges of as many places."""
    full.add_skin(part, SimpleNamespace(middle=crown.middle),
                  [[crown.front(x, h, lift) for x, h in inner], [crown.front(x, h, lift) for x, h in outer]], material)


def wall_bar(part, crown, material, start, stop, half_width, lift):
    """A straight bar on the front of the crown from one place to another."""
    along = (stop[0] - start[0], stop[1] - start[1])
    length = math.hypot(*along) or 1.0
    across = (-along[1] / length * half_width, along[0] / length * half_width)
    edge = [(start[0] - across[0], start[1] - across[1]), (stop[0] - across[0], stop[1] - across[1]),
            (stop[0] + across[0], stop[1] + across[1]), (start[0] + across[0], start[1] + across[1])]
    wall_shape(part, crown, material, edge, lift, ((start[0] + stop[0]) * 0.5, (start[1] + stop[1]) * 0.5))


def lay_logo(part, crown, logo, material, place, lift):
    """A logo on the front of the crown of a hat: "bitcoin", "monad", "star", "peace", "bolt" or "cloud"."""
    height, size = place

    def at(x, y):
        return (x * size, height + y * size)

    if logo == "bitcoin":
        capitals = 1.55 * size
        scale = capitals / KIT.FONT_CAPITALS
        KIT.lay_word(part, crown, "B", material, crown.numbers.PRINT_BOLD * 0.6, lift, height - capitals * 0.5, scale)
        for x in (-0.16, 0.12):
            for low, high in ((0.70, 1.02), (-1.02, -0.70)):
                wall_bar(part, crown, material, at(x, low), at(x, high), size * 0.07, lift)
    elif logo == "monad":
        # A thick rounded square, turned a little (about 12 degrees on the pictures).
        turn = math.radians(12.0)

        def square(scale):
            points = []
            for index in range(16):
                angle = 2.0 * math.pi * index / 16
                x, y = KIT.power(math.cos(angle), 3.2) * scale, KIT.power(math.sin(angle), 3.2) * scale
                points.append(at(x * math.cos(turn) - y * math.sin(turn), x * math.sin(turn) + y * math.cos(turn)))
            return points
        wall_ring(part, crown, material, square(0.36), square(0.78), lift)
    elif logo == "star":
        edge = []
        for index in range(10):
            angle = math.pi * 0.5 + math.pi * index / 5
            reach = 1.0 if index % 2 == 0 else 0.42
            edge.append(at(math.cos(angle) * reach, math.sin(angle) * reach - 0.06))
        wall_shape(part, crown, material, edge, lift, at(0.0, -0.06))
    elif logo == "peace":
        segments = 18
        outer = [at(math.cos(2.0 * math.pi * index / segments), math.sin(2.0 * math.pi * index / segments))
                 for index in range(segments)]
        inner = [at(0.80 * math.cos(2.0 * math.pi * index / segments), 0.80 * math.sin(2.0 * math.pi * index / segments))
                 for index in range(segments)]
        wall_ring(part, crown, material, inner, outer, lift)
        thick = size * 0.09
        wall_bar(part, crown, material, at(0.0, 0.84), at(0.0, -0.84), thick, lift)
        for x in (-1.0, 1.0):
            wall_bar(part, crown, material, at(0.0, 0.0), at(x * 0.60, -0.60), thick, lift)
    elif logo == "bolt":
        # Two pieces that overlap where the bolt turns: a slanted slab, then a point.
        wall_shape(part, crown, material, [at(0.05, 1.0), at(0.55, 1.0), at(0.15, 0.08), at(-0.38, 0.08)], lift,
                   at(0.10, 0.55))
        wall_shape(part, crown, material, [at(-0.12, 0.18), at(0.48, 0.18), at(-0.36, -1.0)], lift, at(0.0, 0.0))
    elif logo == "cloud":
        # Three round puffs on a flat base, each its own shape. (First try: one outline around the
        # whole cloud, filled from its middle: on the bust a notch of the crown showed through it.)
        for cx, cy, r in ((-0.55, -0.05, 0.42), (0.05, 0.18, 0.55), (0.60, -0.02, 0.40)):
            wall_shape(part, crown, material, [at(cx + r * math.cos(2.0 * math.pi * index / 12),
                                                  cy + r * math.sin(2.0 * math.pi * index / 12)) for index in range(12)],
                       lift, at(cx, cy))
        base_edge = ([at(-0.95 + 1.90 * index / 6, -0.42) for index in range(7)]
                     + [at(0.95 - 1.90 * index / 6, -0.05) for index in range(7)])
        wall_shape(part, crown, material, base_edge, lift, at(0.0, -0.235))
    else:
        raise SystemExit(f"CHOG_TRAITS no logo named {logo!r}")


# The patch sewn on the dome of a beanie, as the pictures of Side=Left draw it on the upper part of the
# beanie towards the right of the character: degrees from the front (towards its left), height, half
# size, how far it stands off the dome.
# (First: -34 degrees and 0.235 high: on the bust of Side=Left it sat at the edge of the frame.)
PATCH = (-24.0, 0.200, 0.044, 0.006)


def lay_patch(part, wall, patch, colours, named):
    """A flat patch laid on the dome of a beanie: "pizza" (a slice: the colours Patch, Crust, Dot),
    "feather" (Patch), "chog" (a small spiky head: Patch, and Dot for its eyes)."""
    degrees, height, size, lift = PATCH
    here, along, upwards, out = wall.frame(degrees, height)
    centre = here + out * lift
    flat = SimpleNamespace(middle=wall.middle)

    def at(x, y, more=0.0):
        return centre + along * x * size + upwards * y * size + out * more

    def shape(material, edge, middle, more=0.0):
        full.add_skin(part, flat, [[at(*middle, more)], [at(x, y, more) for x, y in edge]], material)

    if patch == "pizza":
        shape(named("Patch"), [(-0.85, 0.75), (0.85, 0.75), (0.0, -1.0)], (0.0, 0.2))
        shape(named("Crust"), [(-0.95, 0.62), (0.95, 0.62), (0.95, 1.0), (-0.95, 1.0)], (0.0, 0.81), 0.001)
        for x, y in ((-0.22, 0.30), (0.20, -0.05)):
            shape(named("Dot"), [(x + 0.17 * math.cos(2.0 * math.pi * index / 8), y + 0.17 * math.sin(2.0 * math.pi * index / 8))
                                 for index in range(8)], (x, y), 0.001)
    elif patch == "feather":
        edge = []
        for index in range(12):
            angle = 2.0 * math.pi * index / 12
            x, y = 0.30 * math.cos(angle), 1.0 * math.sin(angle)
            turn = math.radians(-30.0)
            edge.append((x * math.cos(turn) - y * math.sin(turn), x * math.sin(turn) + y * math.cos(turn)))
        shape(named("Patch"), edge, (0.0, 0.0))
    elif patch == "chog":
        edge = []
        for index in range(14):
            angle = math.pi * 0.5 + 2.0 * math.pi * index / 14
            reach = 1.0 if index % 2 == 0 else 0.68
            edge.append((math.cos(angle) * reach, math.sin(angle) * reach - 0.05))
        shape(named("Patch"), edge, (0.0, -0.05))
        for x in (-0.25, 0.25):
            shape(named("Dot"), [(x + 0.10 * math.cos(2.0 * math.pi * index / 6), 0.05 + 0.13 * math.sin(2.0 * math.pi * index / 6))
                                 for index in range(6)], (x, 0.05), 0.001)
    else:
        raise SystemExit(f"CHOG_TRAITS no patch named {patch!r}")


HAT_SHAPES = {
    "Ushanka": build_ushanka,
    "Headband": build_headband,
    "CurlyWig": build_curly_wig,
    "Durag": build_durag,
    "WizardHat": build_wizard_hat,
    "SpartanHelmet": build_spartan,
}


# ---------------------------------------------------------------------------------------------
# Every value of "Head" whose shape is built, with its colours. make_chog_traits.py keeps the
# entries it had (TISM Cap, MMGA Cap, Blue Bucket Cap, Balls Cap, Crown, Wizard Beanie).
# ---------------------------------------------------------------------------------------------

def cap(crown, brim, word=None, ink=None, line=None, logo=None, missing=""):
    """A cap of the collection: a crown, a brim, and on its front a word ("word", one string or a tuple
    of lines, in "ink", edged in "line" when given) or a logo of lay_logo (in "ink"). "missing" says what
    the pictures show on its front that is not drawn."""
    entry = {"shape": "Cap", "colours": {"Crown": crown, "Brim": brim, "Button": crown},
             "numbers": {"BRIM_LENGTH": 0.060, "BRIM_RISE_DEG": 35.0}}
    if word:
        entry["print"] = word
    if logo:
        entry["logo"] = logo
    if ink:
        entry["colours"]["Print"] = ink
    if line:
        entry["colours"]["PrintLine"] = line
    if missing:
        entry["print_missing"] = missing
    return entry


def bucket(crown, brim, under, mark=None, mark_colour=None, missing=""):
    entry = {"shape": "BucketCap", "colours": {"Crown": crown, "Brim": brim, "Under": under}}
    if mark:
        entry["mark"] = mark
        entry["colours"]["Mark"] = mark_colour
    if missing:
        entry["print_missing"] = missing
    return entry


INK_DARK = (0.06, 0.05, 0.06)
INK_WHITE = (0.96, 0.95, 0.94)


def beanie(dome_colour, cuff, rib, patch=None, patch_colours=None, missing=""):
    entry = {"shape": "Beanie", "colours": {"Dome": dome_colour, "Cuff": cuff, "Rib": rib},
             "numbers": {"PRINTS": ()}}
    if patch:
        entry["patch"] = patch
        entry["colours"].update(patch_colours)
    if missing:
        entry["print_missing"] = missing
    return entry


HATS = {
    # Caps, Side=Right. The words are those the pictures print, in the font built into Blender (the
    # pictures draw them by hand); their colours read on the pictures (15 October 2026).
    "Horny Cap": cap((0.13, 0.28, 0.91), (0.57, 0.56, 0.88), "HORNY", (0.98, 0.80, 0.12), INK_DARK),
    "Choggie Cap": cap((0.92, 0.90, 0.84), (0.83, 0.25, 0.49), "CHOG", (0.42, 0.22, 0.74), INK_DARK),
    "Boob Cap": cap((0.95, 0.43, 0.12), (0.88, 0.38, 0.12), "BOOB", INK_WHITE, INK_DARK),
    "BOOB Cap": cap((0.95, 0.43, 0.12), (0.88, 0.38, 0.12), "BOOB", INK_WHITE, INK_DARK),
    # Caps, Side=Left: the front panel is larger on these pictures, the shape is the same.
    "BTC Cap": cap((0.92, 0.53, 0.10), (0.27, 0.19, 0.11), ink=INK_WHITE, logo="bitcoin"),
    "MCGA": cap((0.65, 0.36, 0.92), (0.47, 0.26, 0.70), ("MAKE CHOG", "GREAT AGAIN"), INK_WHITE),
    "Chog Cap": cap((0.65, 0.36, 0.92), (0.47, 0.26, 0.70), ("MAKE CHOG", "GREAT AGAIN"), INK_WHITE),
    "MEGA Chog": cap((0.92, 0.93, 0.83), (0.94, 0.30, 0.07), ("MEGA", "CHOG"), INK_DARK),
    "Buttcoin Cap": cap((0.92, 0.52, 0.10), (0.27, 0.19, 0.11), "Buttcoin", INK_WHITE, (0.55, 0.28, 0.06),
                        missing="the bars of the Bitcoin B of its first letter"),
    "10k TPS Cap": cap((0.93, 0.92, 0.92), (0.40, 0.40, 0.40), ("10K", "TPS"), (0.42, 0.22, 0.80),
                       missing="the purple box around the words"),
    "JEET Cap": cap((0.12, 0.72, 0.22), (0.45, 0.84, 0.52), "JEET", INK_WHITE, INK_DARK),
    "Jeet Cap": cap((0.12, 0.72, 0.22), (0.45, 0.84, 0.52), "JEET", INK_WHITE, INK_DARK),
    "RETARD Cap": cap((0.82, 0.30, 0.95), (0.84, 0.65, 0.96), "RETARD", (0.98, 0.94, 0.80), INK_DARK),
    "WAGMI Cap": cap((0.88, 0.13, 0.55), (0.91, 0.49, 0.74), "WAGMI", INK_WHITE, INK_DARK),
    "NEET": cap((0.95, 0.91, 0.86), (0.22, 0.25, 0.76), "NEET", INK_WHITE, (0.22, 0.25, 0.76)),
    "MCGA Cap": cap((0.95, 0.07, 0.06), (0.80, 0.07, 0.06), "MCGA", (0.98, 0.72, 0.14), (0.90, 0.42, 0.08)),
    "ETH Cap": cap((0.92, 0.92, 0.93), (0.74, 0.75, 0.78), missing="the Ether logo in grey"),
    # Bucket hats, Side=Right, with the logo of their crown (lay_logo).
    "Mon Cap": bucket((0.40, 0.28, 0.65), (0.33, 0.24, 0.60), (0.24, 0.18, 0.52), mark="monad", mark_colour=INK_WHITE),
    "Cloud Cap": bucket((0.25, 0.36, 0.85), (0.25, 0.36, 0.85), (0.13, 0.20, 0.62), mark="cloud", mark_colour=INK_WHITE,
                        missing="the swirls of the cloud (a plain cloud is drawn)"),
    "Orange Bucket Cap": bucket((0.87, 0.40, 0.19), (0.87, 0.40, 0.19), (0.70, 0.26, 0.12)),
    "Gray Bucket Cap": bucket((0.56, 0.62, 0.67), (0.58, 0.64, 0.68), (0.45, 0.50, 0.55)),
    "Dollar Cap": bucket((0.15, 0.80, 0.94), (0.15, 0.80, 0.94), (0.09, 0.40, 0.58), mark="label",
                         mark_colour=(0.60, 0.90, 0.95), missing="the words of its label"),
    "Thunder Cap": bucket((0.08, 0.57, 0.85), (0.08, 0.57, 0.85), (0.03, 0.41, 0.67), mark="bolt",
                          mark_colour=(0.98, 0.78, 0.10)),
    "Star Cap": bucket((0.40, 0.40, 0.40), (0.40, 0.40, 0.40), (0.26, 0.26, 0.26), mark="star", mark_colour=INK_WHITE),
    "Peace hat": bucket((0.51, 0.65, 0.59), (0.50, 0.63, 0.57), (0.20, 0.25, 0.22), mark="peace", mark_colour=INK_WHITE),
    "Red Peace Cap": bucket((0.76, 0.21, 0.26), (0.76, 0.21, 0.26), (0.55, 0.10, 0.12), mark="peace",
                            mark_colour=INK_WHITE),
    "Peace Cap": bucket((0.76, 0.21, 0.26), (0.76, 0.21, 0.26), (0.55, 0.10, 0.12), mark="peace", mark_colour=INK_WHITE),
    # Beanies, Side=Left (the yellow rings are those of the Wizard Beanie only).
    "Choggie Beanie": beanie((0.33, 0.32, 0.30), (0.32, 0.31, 0.29), (0.18, 0.17, 0.16), "chog",
                             {"Patch": (0.66, 0.58, 0.92), "Dot": (0.15, 0.12, 0.25)}),
    "BTC Beanie": beanie((0.95, 0.29, 0.06), (0.91, 0.28, 0.07), (0.57, 0.21, 0.09), "pizza",
                         {"Patch": (0.98, 0.80, 0.30), "Crust": (0.86, 0.52, 0.18), "Dot": (0.88, 0.16, 0.12)}),
    "RH Beanie": beanie((0.79, 0.93, 0.06), (0.77, 0.92, 0.05), (0.57, 0.70, 0.06), "feather",
                        {"Patch": (0.08, 0.08, 0.09)}),
    # Its cuff is striped blue and purple: the ribs carry the purple, as wide as the blue between them.
    # (First: the ribs of the other beanies, 0.007 wide: the cuff read plain blue.)
    "Mon Beanie": {**beanie((0.11, 0.61, 0.95), (0.11, 0.61, 0.95), (0.58, 0.12, 0.91)),
                   "numbers": {"PRINTS": (), "RIBS": (24, 0.040, 0.004)}},
    "8 Ball Beanie": beanie((0.22, 0.22, 0.23), (0.20, 0.20, 0.21), (0.10, 0.10, 0.11), missing="an 8 ball"),
    # The new shapes.
    "Russian Hat": {
        "shape": "Ushanka",
        "colours": {"Crown": (0.80, 0.59, 0.44), "Fur": (0.89, 0.76, 0.64), "Flap": (0.86, 0.70, 0.57)},
    },
    "Red Headband": {
        "shape": "Headband",
        "colours": {"Band": (0.91, 0.22, 0.20), "Line": (0.68, 0.12, 0.13)},
    },
    "Japan Headband": {
        "shape": "Headband",
        "colours": {"Band": (0.96, 0.96, 0.96), "Line": (0.74, 0.74, 0.77), "Sun": (0.91, 0.19, 0.18)},
    },
    # Yellow curls framing the face down to the chin, a red top over the crown.
    "Bob Hair": {
        "shape": "CurlyWig",
        "colours": {"Curl": (0.95, 0.76, 0.10), "Top": (0.93, 0.14, 0.11)},
        "numbers": {"RIM": ((0.0, 54.0), (30.0, 64.0), (55.0, 112.0), (180.0, 116.0)),
                    "SIZE": ((0.0, 1.16), (40.0, 1.42), (90.0, 1.58), (180.0, 1.52))},
    },
    # A big afro in six colours, from the right of the character over the top to its left.
    "Rainbow Wig": {
        "shape": "CurlyWig",
        "colours": {"Red": (0.85, 0.13, 0.12), "Orange": (0.85, 0.45, 0.12), "Yellow": (0.95, 0.88, 0.14),
                    "Green": (0.21, 0.85, 0.09), "Blue": (0.09, 0.50, 0.93), "Purple": (0.58, 0.10, 0.94)},
        "sweep": ("Red", "Orange", "Yellow", "Green", "Blue", "Purple"),
        "numbers": {"SIZE": ((0.0, 1.20), (35.0, 1.55), (90.0, 1.78), (180.0, 1.72))},
    },
    "Durag": {
        "shape": "Durag",
        "colours": {"Cloth": (0.37, 0.34, 0.41), "Seam": (0.24, 0.22, 0.28)},
    },
    "Wizard Hat": {
        "shape": "WizardHat",
        "colours": {"Cloth": (0.08, 0.14, 0.47), "Star": (0.95, 0.68, 0.07)},
    },
    "Spartan Helmet": {
        "shape": "SpartanHelmet",
        "colours": {"Gold": (0.89, 0.64, 0.32), "Trim": (0.80, 0.47, 0.20), "Crest": (0.95, 0.06, 0.06)},
    },
}

# Where a Side draws a value otherwise: these entries replace those of HATS for that Side.
HATS_OF_SIDE = {
    "Left": {
        # A white cap with a purple brim on these pictures, a purple bucket hat on those of Side=Right.
        "Mon Cap": cap((0.95, 0.91, 0.95), (0.50, 0.40, 0.91), ink=(0.45, 0.33, 0.88), logo="monad"),
        # Cream fur, a tan crown, flaps of dark leather with two white snaps each.
        "Russian Hat": {
            "shape": "Ushanka",
            "colours": {"Crown": (0.89, 0.66, 0.52), "Fur": (0.95, 0.90, 0.80), "Flap": (0.32, 0.27, 0.38),
                        "Snap": (0.95, 0.95, 0.95)},
        },
    },
    "Right": {
        # On the three Skull Genesis of Side=Right the brim is orange, not brown.
        "BTC Cap": cap((0.92, 0.53, 0.10), (0.85, 0.45, 0.10), ink=INK_WHITE, logo="bitcoin"),
    },
}


# ---------------------------------------------------------------------------------------------
# Clothing: the 38 values of "Body" worn by 20 Genesis or more, but for the costumes with a hood or
# a helmet (Gangster suit, Banana suit, Shark Suit, Astro Suit, Dragon suit) and the rubber duck
# float (Duck Float): see LEFT_OUT. Three cuts: the shirt of make_chog_traits.py ("Tee"), a jacket
# open on a shirt and a tie ("Jacket"), a jacket with a zip and stripes down the sleeves ("Track").
# What is printed on a shirt (67, CHUG LIFE, CWO, MILF, INMATE, the flowers of the Hawaiian polo) is
# not drawn: see "print_missing".
# ---------------------------------------------------------------------------------------------
LEFT_OUT = {
    "Gangster suit": "a hood over the head and a red bandana over the mouth",
    "Banana suit": "a banana costume whose hood frames the face",
    "Shark Suit": "a shark costume whose hood, with teeth, frames the face",
    "Astro Suit": "a space suit with its helmet",
    "Dragon suit": "a dragon costume with its hood, horns and wings",
    "Duck Float": "a rubber duck float around the body, not clothing",
}

# The front of a jacket: the shirt seen in its V and the tie. Heights above the ground (the collar
# of the shirt of make_chog_traits.py is at 0.580), half width of the V at its top, how far each
# layer stands off the shirt.
JACKET = {
    "V_TOP": 0.580,
    "V_FOOT": 0.455,
    "V_HALF_WIDTH": 0.060,
    "TIE": (0.574, 0.470, 0.011, 0.017),   # top, foot, half width at the knot and at its widest
    "LIFTS": (0.006, 0.010),
}

TRACK = {
    "SLEEVE_STRIPES": (14.0, 0.006),      # degrees each side of the top of the arm, half width
    "ZIP_HALF_WIDTH": 0.006,
    "LIFT": 0.005,
}


def shirt_front(maker, height, side, lift):
    """A point of the front of the torso at a height, "side" to the left of its middle, "lift" off the
    shirt."""
    numbers = maker.numbers(KIT.TEE)
    half_width, half_depth = KIT.torso_section(maker.body, height)
    wide, deep = half_width + numbers.LIFT + lift, half_depth + numbers.LIFT + lift
    across = max(-0.98, min(0.98, side / wide))
    return Vector((side, -deep * math.sqrt(1.0 - across * across), height))


def build_jacket(maker):
    """The shirt of make_chog_traits.py in the colour of the jacket, the shirt in the V of its front
    and a tie: flat shapes laid on the torso, no outline."""
    KIT.build_tee(maker)
    torso = maker.parts[0][0]
    numbers = SimpleNamespace(**JACKET)
    low, high = numbers.LIFTS
    middle = SimpleNamespace(middle=Vector((0.0, 0.0, (numbers.V_TOP + numbers.V_FOOT) * 0.5)))
    rows = []
    steps = 3
    for index in range(steps):
        share = index / steps
        height = numbers.V_TOP + (numbers.V_FOOT - numbers.V_TOP) * share
        wide = numbers.V_HALF_WIDTH * (1.0 - share)
        rows.append([shirt_front(maker, height, side, low) for side in (-wide, 0.0, wide)])
    rows.append([shirt_front(maker, numbers.V_FOOT, 0.0, low)])
    full.add_skin(torso, middle, rows, torso.named("Shirt"), around=False)
    if "Tie" in maker.entry["colours"]:
        top, foot, knot, widest = numbers.TIE
        tie = []
        for height, wide in ((top, knot), (top - 0.022, knot * 0.8), (foot + 0.020, widest)):
            tie.append([shirt_front(maker, height, side, high) for side in (-wide, wide)])
        tie.append([shirt_front(maker, foot, 0.0, high)])
        full.add_skin(torso, middle, tie, torso.named("Tie"), around=False)


def build_track(maker):
    """The shirt of make_chog_traits.py with a zip down the front and, when the entry gives the
    colour "Stripe", two stripes down each sleeve."""
    entry = maker.entry
    striped = "Stripe" in entry["colours"]
    # The shirt alone must not paint its bands in stripes: those of a track jacket run down the arm.
    maker.entry = {**entry, "stripes": False}
    KIT.build_tee(maker)
    maker.entry = entry
    numbers = maker.numbers(KIT.TEE)
    track = SimpleNamespace(**TRACK)
    torso = maker.parts[0][0]
    if "Zip" in entry["colours"]:
        heights = [numbers.HEM + (numbers.COLLAR[0] - numbers.HEM) * index / 4 for index in range(5)]
        rows = [[shirt_front(maker, height, side, track.LIFT) for side in (-track.ZIP_HALF_WIDTH, track.ZIP_HALF_WIDTH)]
                for height in heights]
        full.add_skin(torso, SimpleNamespace(middle=Vector((0.0, 0.0, 0.4))), rows, torso.named("Zip"), around=False)
    if not striped or numbers.SLEEVE <= 0.0:
        return
    body = maker.body
    spread, ahead = math.radians(body.ARM_SPREAD_DEG), math.radians(body.ARM_FORWARD_DEG)
    ball, radius = body.SHOULDER_RADIUS, body.ARM_RADIUS
    turn, half = track.SLEEVE_STRIPES
    for (sleeve, on), side in zip(maker.parts[1:], (1.0, -1.0)):
        down = Vector((side * math.sin(spread), -math.sin(ahead), -math.cos(spread) * math.cos(ahead))).normalized()
        first, second = base.frame_across(down, -Y)
        top = second * -side
        reach = radius + numbers.LIFT + track.LIFT
        for degrees in (-turn, turn):
            way = top * math.cos(math.radians(degrees)) + first * math.sin(math.radians(degrees))
            across = down.cross(way).normalized()
            rows = [[down * distance + way * reach + across * edge for edge in (-half, half)]
                    for distance in (ball * 1.25, numbers.SLEEVE - 0.006)]
            full.add_skin(sleeve, SimpleNamespace(middle=down * ball * 1.25), rows, sleeve.named("Stripe"),
                          around=False)


CLOTH_KINDS = {"Jacket": build_jacket, "Track": build_track}


def shade(colour, amount=0.68):
    return tuple(round(channel * amount, 3) for channel in colour)


def tee(main, collar=None, missing="", **numbers):
    """A plain shirt. A plain one needs fewer bands than a striped one: it follows the torso with
    bands twice as tall (STEP), which saves about 200 triangles."""
    entry = {"kind": "Tee", "colours": {"Main": main, "Collar": collar or shade(main)},
             "numbers": {"STEP": 0.076, **numbers}}
    if missing:
        entry["print_missing"] = missing
    return entry


def jacket(main, shirt, tie, collar=None):
    return {"kind": "Jacket", "colours": {"Main": main, "Collar": collar or shirt, "Shirt": shirt, "Tie": tie},
            "numbers": {"STEP": 0.076}}


def track(main, stripe=None, zip_colour=None, collar=None, missing="", **numbers):
    colours = {"Main": main, "Collar": collar or shade(main)}
    if stripe:
        colours["Stripe"] = stripe
    if zip_colour:
        colours["Zip"] = zip_colour
    entry = {"kind": "Track", "colours": colours, "numbers": {"STEP": 0.076, **numbers}}
    if missing:
        entry["print_missing"] = missing
    return entry


LONG_SLEEVE = 0.19     # down the arm to the mitten (ARM_LENGTH 0.30, hand 0.22 across)
WHITE = (0.96, 0.96, 0.96)

CLOTHES = {
    # Jackets on a shirt and a tie. "Tuxedo" on Side=Left has a red tie: CLOTHES_OF_SIDE.
    "Tuxedo": jacket((0.25, 0.25, 0.26), WHITE, (0.12, 0.12, 0.14)),
    "Red Tuxedo": jacket((0.53, 0.07, 0.10), (0.76, 0.95, 0.95), (0.62, 0.08, 0.11)),
    "White tuxedo": jacket((0.95, 0.95, 0.95), (0.90, 0.90, 0.91), (0.86, 0.86, 0.87)),
    "Cream Tuxedo": jacket((0.89, 0.69, 0.52), WHITE, (0.78, 0.58, 0.42)),
    "Pink Tuxedo": jacket((0.90, 0.62, 0.83), WHITE, (0.80, 0.48, 0.72)),
    "Aqua Tuxedo": jacket((0.51, 0.81, 0.96), WHITE, (0.38, 0.70, 0.88)),
    "Blue tuxedo": jacket((0.05, 0.12, 0.64), (0.95, 0.91, 0.65), (0.06, 0.10, 0.48)),
    # Track jackets: white stripes down long sleeves and a zip.
    "Black Trackset": track((0.22, 0.22, 0.24), stripe=WHITE, zip_colour=(0.85, 0.85, 0.86), SLEEVE=LONG_SLEEVE),
    "Red Trackset": track((0.89, 0.21, 0.14), stripe=WHITE, zip_colour=WHITE, SLEEVE=LONG_SLEEVE),
    # A zipped top without stripes; its white triangle is not drawn.
    "Squid Game Guard": track((0.92, 0.11, 0.38), zip_colour=(0.20, 0.10, 0.14), SLEEVE=LONG_SLEEVE,
                              missing="a white triangle on the chest"),
    # The Chinese jackets: a band collar and a row of gold toggles, drawn as one line down the front.
    "Blue Chinese": track((0.22, 0.25, 0.69), zip_colour=(0.93, 0.70, 0.20), collar=(0.14, 0.17, 0.58),
                          SLEEVE=LONG_SLEEVE),
    "Red Chinese": track((0.87, 0.13, 0.13), zip_colour=(0.93, 0.70, 0.20), collar=(0.58, 0.11, 0.12),
                         SLEEVE=LONG_SLEEVE),
    # Shirts in one colour.
    "Farmer Shirt": tee((0.95, 0.65, 0.20)),
    "Inmate": tee((0.94, 0.50, 0.17), missing="the word INMATE"),
    "Six Seven": tee((0.43, 0.95, 0.75), missing="the number 67 in black"),
    "Black Shirt": tee((0.24, 0.24, 0.25), collar=(0.14, 0.14, 0.15)),
    "Chug Life": tee((0.95, 0.53, 0.77), missing="CHUG LIFE in black"),
    "White Pure Shirt": tee((0.95, 0.95, 0.95), collar=(0.80, 0.80, 0.82)),
    "CWO": tee((0.66, 0.44, 0.92), missing="the word CWO in orange"),
    "Yellow Shirt": tee((0.92, 0.83, 0.57)),
    "Red shirt": tee((0.74, 0.32, 0.34)),
    "MILF Lover": tee((0.55, 0.84, 0.94), missing="MILF and a red heart"),
    "Hawaiian Polo": tee((0.80, 0.12, 0.19), collar=(0.62, 0.10, 0.16), missing="the white flowers"),
    "Yellow Yukata": tee((0.93, 0.61, 0.14), collar=(0.86, 0.24, 0.16), SLEEVE=LONG_SLEEVE,
                         missing="the swirls of the cloth"),
    # Without sleeves.
    "Tank Top": tee((0.38, 0.38, 0.18), SLEEVE=0.0, COLLAR=(0.566, 0.593)),
    # Robes and capes: long sleeves, a collar in the colour of the trim.
    "Kings Robe": tee((0.83, 0.06, 0.06), collar=(0.95, 0.91, 0.89), SLEEVE=LONG_SLEEVE, COLLAR=(0.548, 0.596),
                      missing="the open front, in the skin colour, and the white fur along it"),
    "Jesus": tee((0.95, 0.93, 0.92), collar=(0.86, 0.84, 0.83), SLEEVE=LONG_SLEEVE,
                 missing="the red sash over the shoulder"),
    # A short cape over the shoulders on a body left bare: the pictures draw the body in the skin colour
    # under it (CHOG #22, #318, #435). (First: a whole dark blue shirt with long sleeves.)
    "Wizard": tee((0.12, 0.13, 0.42), collar=(0.08, 0.09, 0.30), HEM=0.44, SLEEVE=0.09,
                  missing="the yellow triangles along the cape and its bow at the neck"),
    # Stripes across: the shirt of make_chog_traits.py with its stripes.
    # (Stripes 0.046 tall, as "Prisoner": wider than the bands of a plain shirt, and fewer triangles.)
    "Stripes": {"kind": "Tee", "colours": {"Main": WHITE, "Stripe": (0.90, 0.21, 0.14), "Collar": (0.90, 0.21, 0.14)},
                "stripes": True, "numbers": {"STEP": 0.046}},
    # A padded vest: no sleeves, the padding as darker bands.
    "Black Vest": {"kind": "Tee", "colours": {"Main": (0.29, 0.31, 0.37), "Stripe": (0.21, 0.23, 0.28),
                                              "Collar": (0.19, 0.21, 0.26)},
                   "stripes": True, "numbers": {"SLEEVE": 0.0, "STEP": 0.046}},
}

CLOTHES_OF_SIDE = {
    "Left": {
        "Tuxedo": jacket((0.27, 0.25, 0.30), WHITE, (0.86, 0.12, 0.12)),
    },
}
