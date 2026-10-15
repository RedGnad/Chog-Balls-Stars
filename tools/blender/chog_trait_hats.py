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

Batch 3 (15 October 2026): each hat shape says which leaves of the crown it leaves ("LEAVES", see
leaf() below), hair and hoods replace the crown; three values of "Head" that are not hats (Clown Nose,
Plumber, Robber); the clothing of Side=Left (hoodies, tops without sleeves, jerseys, tees, shirts,
jackets, vests); the prints of shirts and caps (decorate_shirt, lay_logo); the costumes (Gangster,
Shark, Banana, Astro, Dragon, Duck Float).
"""

import math
import random
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
# The leaves a hat leaves (15 October 2026). Under a hat the leaves of the crown went through it: from
# the camera of the game the hero was a hat pierced by spikes. The pictures of the collection draw the
# spikes only where a hat leaves the head bare: beside it and behind it, under its brim or its rim. So
# each hat shape says, in its numbers ("LEAVES"), what becomes of each line of SPIKES of head H3:
#
#     0  the leaf on top                     4  the top of the back row
#     1  the upper pair of the star          5  the pair of the back row
#     2  the middle pair, to the sides       6  the leaf straight back
#     3  the lowest pair, sideways and down
#
# An item is leaf(share, turn, back, bend): its length against the leaf of SPIKES (1 the same, 0 none:
# it is removed, its outline with it, more than 1 longer), the turn around the head (degrees from
# straight up, seen from the front) and the lean back (degrees) that replace those of SPIKES (None:
# its own), to bring a leaf out from under a brim, and how far its point bends back up (against its
# length). A leaf brought under a brim points down and hides under the hat seen from above: it is made
# longer (1.3), so that the spikes still show beside the hat from the camera of the game, as large as
# the pictures draw them, and bent up, so that from the front it does not hang like an ear (seen on
# the first try, 15 October 2026). A value of "Head" may give its own
# in its "numbers". Hair (the curly wigs) replaces the crown: NO_CROWN. A hero with no hat, or with a
# hat that is not modelled, keeps the whole crown. make_chog_traits.leaf_crossings() checks that no
# leaf left goes through its hat: a Genesis whose leaves cross its hat fails its build.
#
# "FRINGE": False in the numbers of a hat removes the teeth of hair the base draws on the brow, where
# the pictures show none under that hat (hair, a helmet).
# ---------------------------------------------------------------------------------------------

def leaf(share=1.0, turn=None, back=None, bend=0.0):
    """What becomes of one line of SPIKES under a hat: see above."""
    return (share, turn, back, bend)


WHOLE, GONE = leaf(), leaf(0.0)
FULL_CROWN = (WHOLE,) * 7
NO_CROWN = (GONE,) * 7


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
    # (Until 15 October 2026: 66 degrees, 80 to 132, 1.10 to 1.20, 0.072 wide. The leaves of the sides
    # hid the flaps, and they lay against the head: from the camera of the game no flap showed. On the
    # pictures they hang beside the face down to the chin, wider than an eye, and splay out.)
    "FLAP": (68.0, 74.0, 140.0, 1.14, 1.34, 0.098, 0.026),
    # Where a flap is leather (the colour "Snap" is given: Side=Left), a rim of fur around it: how much
    # wider and longer the rim is, and how far behind the leather it lies.
    "FLAP_RIM": (0.020, 0.024, 0.012),
    "SNAPS": ((0.42, 0.015), (0.74, 0.015)),   # along the flap from its top, radius
    "OUTLINE_THICKNESS": 0.020,
    # The fur hat covers the head down to the brow and over the ears: no leaf comes out of it at the
    # top. The pictures of Side=Right show spikes beside and behind it (Side=Left, framed on the face,
    # none): the lowest pair of the star, behind the flaps, the pair of the back row and the leaf
    # straight back stay, brought out under the fur, which sits low: they point down more than under a cap.
    "LEAVES": (GONE, GONE, GONE, leaf(1.3, 128.0, 30.0, 0.45), GONE, leaf(1.3, 130.0, 45.0, 0.45),
               leaf(1.3, None, 120.0, 0.45)),
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
        across = Vector((-math.sin(angle), math.cos(angle), 0.0))

        def flap(wider, longer, behind):
            path, wide, thick = [], [], []
            for index in range(stations):
                share = index / (stations - 1)
                polar = math.radians(start + (end + longer - start) * share)
                out = head_normal(surface, angle, polar)
                path.append(on_head(surface, angle, polar, size_top + (size_foot - size_top) * share) - out * behind)
                rounded = 1.0 if index < stations - 1 else 0.55
                wide.append(across * (half_wide + wider) * rounded)
                thick.append(out * half_thick)
            return path, wide, thick

        slab(part, *flap(0.0, 0.0, 0.0), part.named("Flap"), outline)
        if "Snap" in colours:
            wider, longer, behind = numbers.FLAP_RIM
            slab(part, *flap(wider, math.degrees(longer / 0.3), behind), part.named("Fur"), outline)
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
    # Polar degrees of its lower and upper edge, by degrees from the front. On the pictures the band
    # lies on the brow just above the eyes, the spikes standing above it.
    # (Until 15 October 2026: 74 to 52 at the front, 66 to 46 at the back, higher on the head. The roots
    # of the upper pairs of leaves then lay under it and every leaf but the top one went through it.)
    # At the front its upper edge covers the dark line at the top of the face (54 degrees); at the sides
    # it is lower, under the roots of the upper pair of leaves.
    "LOWER": ((0.0, 82.0), (90.0, 80.0), (180.0, 76.0)),
    "UPPER": ((0.0, 50.0), (35.0, 53.0), (70.0, 62.0), (180.0, 60.0)),
    "SIZE": ((0.0, 1.075), (90.0, 1.10), (180.0, 1.10)),
    "LINE": 0.30,                # the share of the band, from its lower edge, in the colour "Line"
    "SUN": (0.046, 0.008),       # radius, lift off the band
    "OUTLINE_THICKNESS": 0.016,
    # The leaf on top and the upper pair stand above the band (the pair raised a little); the middle pair
    # would grow out of the band itself and is removed; the lowest pair and the back row are brought
    # out under it.
    "LEAVES": (WHOLE, leaf(1.0, 24.0), GONE, leaf(1.2, 116.0, None, 0.35), GONE, leaf(1.2, 106.0, 40.0, 0.35),
               leaf(1.2, None, 104.0, 0.35)),
    # The band lies on the brow: the teeth of hair of the base would come through it.
    "FRINGE": False,
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
    "SIDES": 16,
    # Polar degrees of the edge of the hair, by degrees from the front: over the brow at the front,
    # down to the ears at the sides and at the back.
    "RIM": ((0.0, 54.0), (30.0, 62.0), (62.0, 108.0), (180.0, 112.0)),
    "ROWS": 3,
    # How much larger than the head the hair is, by degrees from the front: close over the brow (the
    # camera of the game looks down on the face), a big puff at the sides and at the back, as the
    # pictures draw it, twice as wide as the face. The curls stand on a smooth mass MASS times that size.
    # (First: 1.14 all around with shallow waves: a thin painted cap. Then a mass at this size whose
    # rows were scalloped: the owner, 15 October 2026, "a smooth scalloped cap, not fluffy curls".)
    "SIZE": ((0.0, 1.16), (40.0, 1.34), (90.0, 1.46), (180.0, 1.42)),
    "MASS": 0.88,
    # The curls: round puffs, each its own ball with its dark outline, as the pictures draw the hair (a
    # cloud of round curls, each edged by a line). One line per row: polar degrees, radius of a curl,
    # degrees from the front of each (every one on both sides, but 0 and 180).
    "CURL_ROWS": (
        (0.0, 0.120, (0.0,)),
        (36.0, 0.110, (34.0, 100.0, 160.0)),
        (68.0, 0.105, (50.0, 92.0, 134.0, 180.0)),
        (100.0, 0.100, (70.0, 106.0, 142.0, 180.0)),
    ),
    # Smaller curls along the hairline over the brow: polar degrees, radius, degrees from the front.
    "HAIRLINE": (56.0, 0.060, (12.0, 34.0)),
    # (6 first: on the bust, a honeycomb of hexagons. 8: round, but 2 heroes over 8,000 triangles.)
    "CURL_SIDES": 7,
    # How far a curl is sunk into the mass, against its radius.
    "CURL_SUNK": 0.35,
    "TOP_POLAR": 40.0,           # with "Top": what is nearer the top than this takes it
    "OUTLINE_THICKNESS": 0.020,
    # Hair, not a hat: it replaces the crown of leaves (the owner, 15 October 2026). (Several pictures of
    # Bob Hair still draw the tip of one spike above the hair, at the far side; it is left out.)
    "LEAVES": NO_CROWN,
    # The curls of the hairline take the place of the teeth of hair the base draws on the brow.
    "FRINGE": False,
}


def build_curly_wig(maker):
    numbers = maker.numbers(CURLY_WIG)
    part, surface = maker.part(), maker.surface
    sides, rows = numbers.SIDES, numbers.ROWS
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(numbers.RIM, from_front(angle))) for angle in angles]

    def mass_size(angle):
        return 1.0 + (interpolate(numbers.SIZE, from_front(angle)) - 1.0) * numbers.MASS

    # The mass: a smooth shell from inside the head under the rim to the top.
    loops = [[on_head(surface, angle, polar, 0.90) for angle, polar in zip(angles, rims)]]
    for row in range(rows):
        share = row / rows
        loops.append([on_head(surface, angle, polar * (1.0 - share), mass_size(angle)) for angle, polar in
                      zip(angles, rims)])
    loops.append([on_head(surface, 0.0, 0.0, mass_size(math.radians(0.0)))])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * rows + [(1.0,)]
    colours = maker.entry["colours"]
    sweep = maker.entry.get("sweep")
    first = "Curl" if "Curl" in colours else sweep[0]
    bands, _ = KIT.add_loops(part, loops, part.named(first), numbers.OUTLINE_THICKNESS, shares=shares)
    middle = surface.middle

    def colour_at(centre):
        away = centre - middle
        if "Top" in colours:
            polar = math.degrees(math.acos(max(-1.0, min(1.0, away.z / max(away.length, 1e-9)))))
            return "Top" if polar < numbers.TOP_POLAR else "Curl"
        if sweep:
            arch = math.degrees(math.atan2(away.z, away.x))      # 0: left of the character, 90: top
            share = (205.0 - min(max(arch, -25.0), 205.0)) / 230.0
            return sweep[min(int(share * len(sweep)), len(sweep) - 1)]
        return first

    paint_where(part, [face for band in bands for face in band], colour_at)

    # The curls, on the mass.
    hairline_polar, hairline_radius, hairline_places = numbers.HAIRLINE
    rows_of_curls = list(numbers.CURL_ROWS) + [(hairline_polar, hairline_radius, hairline_places)]
    for polar_deg, radius, places in rows_of_curls:
        for degrees in places:
            for side in ((1.0,) if degrees in (0.0, 180.0) else (1.0, -1.0)):
                angle = math.radians(270.0 + side * degrees)
                polar = math.radians(polar_deg)
                out = head_normal(surface, angle, polar)
                centre = on_head(surface, angle, polar, mass_size(angle)) - out * radius * numbers.CURL_SUNK
                stacks = 4 if radius > 0.08 else 3
                part.shell(base.ball_rings(centre, out, Z if abs(out.z) < 0.9 else X, radius, radius, radius, stacks),
                           numbers.CURL_SIDES, part.named(colour_at(centre)), numbers.OUTLINE_THICKNESS)


# ---------------------------------------------------------------------------------------------
# Durag: a tight cloth over the top and the back of the head, a seam over the brow, two tails
# hanging at the back.
# ---------------------------------------------------------------------------------------------
DURAG = {
    "SIDES": 18,
    # (Until 15 October 2026 the sides came down to 100 degrees, 110 at the back: every leaf of the
    # crown went through the cloth. The pictures draw the spikes of the far side from the cloth down.)
    "RIM": ((0.0, 66.0), (50.0, 72.0), (95.0, 82.0), (140.0, 98.0), (180.0, 106.0)),
    # (1.06 and 1.085 until 15 October 2026: the teeth of hair the base draws on the brow came through
    # the cloth as small triangles of skin.)
    "SIZE": 1.10,
    "SEAM": (7.0, 1.12),         # polar degrees above the rim, size: the band over the brow
    "ROWS": 2,
    # A tail: degrees from the back to each side, polar degrees where it starts, length, half
    # width, half thickness, how far it leans back.
    "TAIL": (13.0, 104.0, 0.20, 0.034, 0.010, 0.55),
    "OUTLINE_THICKNESS": 0.018,
    # Points of hair in the skin colour along the front, coming out under the cloth onto the brow (asked
    # by the owner on 15 October 2026): how many, degrees from the front to the outermost, polar degrees
    # of their roots (under the cloth) and of their tips, half width (degrees), how far off the head
    # (above the face, 0.012, and under the cloth, about 0.027 there).
    "HAIR_POINTS": (7, 46.0, 58.0, 79.0, 5.5, 0.020),
    # The cloth covers the top and the back: the lowest pair of the star stays, brought under its rim at
    # the sides, and the pair of the back row, turned to the sides beside it; the leaf straight back
    # would go through the two tails.
    "LEAVES": (GONE, GONE, GONE, leaf(1.35, 122.0, None, 0.45), GONE, leaf(1.35, 122.0, 30.0, 0.45), GONE),
}


def add_hair_points(part, surface, spec, material):
    """Points of hair laid on the head along the front, from under a hat onto the brow (HAIR_POINTS of
    a hat): flat triangles in "material", part of the head, without an outline."""
    count, spread, root, tip, half_width, lift = spec
    for index in range(count):
        degrees = -spread + 2.0 * spread * index / max(count - 1, 1)
        angle = math.radians(270.0 + degrees)
        corners = []
        for turn, polar in ((-half_width, root), (half_width, root), (0.0, tip)):
            here_angle, here_polar = angle + math.radians(turn), math.radians(polar)
            corners.append(on_head(surface, here_angle, here_polar, 1.0) + head_normal(surface, here_angle, here_polar)
                           * lift)
        full.add_skin(part, surface, [[corners[2]], corners[:2]], material, around=False)


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
    # The pictures draw big spikes beside and behind the hat, from its rim down; none comes out of the
    # cone. Its rim sits as low as that of a cap: the same leaves stay.
    "LEAVES": (GONE, GONE, GONE, leaf(1.3, 116.0, None, 0.35), GONE, leaf(1.3, 106.0, 40.0, 0.35),
               leaf(1.3, None, 104.0, 0.35)),
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
    # down beside the eyes over the cheeks (the guards), and up again behind the ears, where the
    # pictures show the spikes of the back of the head coming out behind the helmet.
    # (Until 15 October 2026 the back came down to 106 degrees and every leaf went through the shell.)
    "RIM": ((0.0, 60.0), (28.0, 61.0), (40.0, 114.0), (95.0, 120.0), (120.0, 92.0), (150.0, 72.0),
            (180.0, 68.0)),
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
    # positive; height above the shell; half width). A brush of horsehair from the brow over the top,
    # as wide on the pictures as a quarter of the helmet. It stops at the top of the back, where the
    # leaf straight back comes out under the rim.
    # (First: 0.145 at most and 0.034 wide, hidden in the leaves. Then up to 0.270 tall and 0.042 wide:
    # from the camera of the game a thin red stick, said the owner on 15 October 2026.)
    "CREST": ((-46.0, 0.050, 0.046), (-30.0, 0.170, 0.066), (-8.0, 0.265, 0.078), (18.0, 0.300, 0.082),
              (38.0, 0.270, 0.080), (54.0, 0.190, 0.070), (64.0, 0.090, 0.056), (70.0, 0.030, 0.044)),
    "CREST_SIDES": 8,
    "OUTLINE_THICKNESS": 0.020,
    # The helmet covers the top, the brow and the cheeks: the spikes the pictures show come out behind
    # it, from the back of the head. The back row stays, brought under the rim at the back.
    "LEAVES": (GONE, GONE, GONE, GONE, GONE, leaf(1.35, 126.0, 55.0, 0.40), leaf(1.35, None, 96.0, 0.40)),
    # The pictures show no teeth of hair on the brow inside the helmet.
    "FRINGE": False,
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
    for pitch_deg, height, half_width in numbers.CREST:
        pitch = math.radians(pitch_deg)
        angle = math.radians(90.0 if pitch_deg >= 0.0 else 270.0)
        polar = abs(pitch)
        out = head_normal(surface, angle, polar)
        foot = on_head(surface, angle, polar, numbers.SIZE) - out * 0.02
        path.append(foot + out * (height + 0.02) * 0.5)
        wide.append(X * half_width)
        thick.append(out * (height + 0.02) * 0.5)
    slab(part, path, wide, thick, part.named("Crest"), outline, sides=numbers.CREST_SIDES)


# ---------------------------------------------------------------------------------------------
# Three values of "Head" that are not hats (15 October 2026): Clown Nose (31 Genesis on the base
# Origin), Plumber (23: a plunger stuck on the head), Robber (24: a knitted balaclava).
# ---------------------------------------------------------------------------------------------
CLOWN_NOSE = {
    # A red ball on the nose: where on the face (to the side, above its middle), how far its middle
    # stands off the face, its radius, its sides and stacks, and the glint on its upper side (degrees
    # up from straight out, its radius).
    "AT": (0.004, -0.040),
    "OFF": 0.022,
    "RADIUS": 0.050,
    "SIDES": 10,
    "STACKS": 5,
    "GLINT": (38.0, 0.013),
    "OUTLINE_THICKNESS": 0.014,
    # Nothing on the head: the whole crown stays, as the pictures show it.
    "LEAVES": FULL_CROWN,
}


def build_clown_nose(maker):
    numbers = maker.numbers(CLOWN_NOSE)
    part, surface = maker.part(), maker.surface
    side, up = numbers.AT
    out = KIT.outward(surface, side, up)
    centre = surface.front(side, up, 0.0) + out * numbers.OFF
    radius = numbers.RADIUS
    part.shell(base.ball_rings(centre, out, Z, radius, radius, radius, numbers.STACKS), numbers.SIDES,
               part.named("Ball"), numbers.OUTLINE_THICKNESS)
    degrees, glint = numbers.GLINT
    upward = (Z - out * Z.dot(out)).normalized()
    towards = (out * math.cos(math.radians(degrees)) + upward * math.sin(math.radians(degrees))).normalized()
    disc_on(part, surface, centre + towards * (radius + 0.002), towards, glint, part.named("Glint"), segments=8)


PLUNGER = {
    # The cup: where it stands on the head (degrees from the front, towards the left of the character;
    # polar degrees), then from its rim on the head up its axis: (height, radius) of each ring.
    "AT": (-18.0, 22.0),
    "CUP": ((-0.030, 0.104), (0.006, 0.118), (0.040, 0.116), (0.078, 0.094), (0.104, 0.056), (0.114, 0.0)),
    "LIP": 1,                    # the rings of the cup up to this one are its darker lip
    "SIDES": 12,
    # The stick: its radius, its length, and the way it leans (towards the left of the character, back).
    "FERRULE": (0.026, 0.034),   # the red collar where the stick enters the cup: radius, length
    "STICK": (0.014, 0.30, 0.95, 0.30),
    "OUTLINE_THICKNESS": 0.016,
    # The cup sits on the top of the head: the leaf on top and the upper pair would go through it.
    "LEAVES": (GONE, GONE, WHOLE, WHOLE, WHOLE, WHOLE, WHOLE),
}


def build_plunger(maker):
    numbers = maker.numbers(PLUNGER)
    part, surface = maker.part(), maker.surface
    degrees, polar_deg = numbers.AT
    angle, polar = math.radians(270.0 + degrees), math.radians(polar_deg)
    axis = head_normal(surface, angle, polar)
    foot = on_head(surface, angle, polar, 1.0)
    first, second = base.frame_across(axis, X)
    rings = [(foot + axis * height, first * radius, second * radius) for height, radius in numbers.CUP]
    rings[-1] = (rings[-1][0], NOWHERE, NOWHERE)
    _, faces = part.shell(rings, numbers.SIDES, part.named("Cup"), numbers.OUTLINE_THICKNESS)
    lip_top = foot + axis * numbers.CUP[numbers.LIP][0]
    part.paint([face for face in faces if (face.calc_center_median() - foot).dot(axis) < (lip_top - foot).dot(axis) - 1e-6],
               part.named("Lip"))
    top = foot + axis * numbers.CUP[-1][0]
    radius, length = numbers.FERRULE
    stick_radius, stick_length, to_left, to_back = numbers.STICK
    way = (axis + X * to_left + Y * to_back).normalized()
    part.shell(base.tube_rings(top - way * 0.01, way, X, ((0.0, radius), (length, radius * 0.7))), 8, part.named("Cup"),
               numbers.OUTLINE_THICKNESS * 0.7)
    part.shell(base.tube_rings(top + way * (length - 0.01), way, X, ((0.0, stick_radius), (stick_length, stick_radius))),
               6, part.named("Stick"), numbers.OUTLINE_THICKNESS * 0.6)


BALACLAVA = {
    # The mask: a knitted shell over the brow, the top, the sides and the back down to the neck
    # (polar degrees of its edge by degrees from the front: the edge runs just above the eyes, then
    # down beside them), and a panel over the face below the eyes and between them. Two eye holes, each
    # ringed by a rolled edge.
    "RIM": ((0.0, 83.0), (26.0, 84.0), (40.0, 128.0), (100.0, 152.0), (180.0, 150.0)),
    "SIZE": 1.11,
    "ROWS": 4,
    "SIDES": 20,
    # The panel over the lower face and the bridge between the eyes, in the coordinates of the face (to
    # the side, above its middle): its edges, how far off the head.
    "LOWER": (-0.26, 0.26, -0.27, -0.072),
    "BRIDGE": (-0.040, 0.040, -0.080, 0.090),
    "PANEL_LIFT": 0.030,
    "PANEL_STEPS": (8, 3),
    # The eye holes: middle (to the side, above), half width, half height, the radius of the rolled edge.
    "HOLE": (0.114, 0.004, 0.072, 0.084, 0.016),
    "HOLE_STATIONS": 14,
    # The knit: darker ribs from the edge up over the top: how many, their half width.
    "RIBS": (14, 0.006),
    "OUTLINE_THICKNESS": 0.020,
    # A knitted mask, tight on the head: the pictures draw the spikes of the far side and of the back
    # coming out of it. Those leaves stay, turned down as under a cap, and come through the knit
    # ("THROUGH": the check of the leaves is told so). The mask covers the mouth: the 24 Robber Genesis
    # have no value of "Mouth" in the metadata anyway; "HIDES" keeps it so if one ever had.
    "LEAVES": (GONE, GONE, GONE, leaf(1.2, 116.0, None, 0.35), GONE, leaf(1.2, 106.0, 40.0, 0.35),
               leaf(1.2, None, 104.0, 0.35)),
    "THROUGH": True,
    "HIDES": ("Mouth",),
}


def build_balaclava(maker):
    numbers = maker.numbers(BALACLAVA)
    part, surface = maker.part(), maker.surface
    sides, rows = numbers.SIDES, numbers.ROWS
    outline = numbers.OUTLINE_THICKNESS
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(numbers.RIM, from_front(angle))) for angle in angles]
    loops = [[on_head(surface, angle, polar, 0.92) for angle, polar in zip(angles, rims)]]
    for row in range(rows):
        share = row / rows
        loops.append([on_head(surface, angle, polar * (1.0 - share), numbers.SIZE) for angle, polar in zip(angles, rims)])
    loops.append([on_head(surface, 0.0, 0.0, numbers.SIZE)])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * rows + [(1.0,)]
    KIT.add_loops(part, loops, part.named("Knit"), outline, shares=shares)
    # The ribs of the knit, from the edge up towards the top.
    count, half = numbers.RIBS
    for index in range(count):
        angle = 2.0 * math.pi * (index + 0.5) / count
        rim = math.radians(interpolate(numbers.RIM, from_front(angle)))
        rows_of_rib = []
        for step in range(4):
            polar = rim * (1.0 - step / 4.0) + math.radians(8.0) * (step / 4.0)
            across = Vector((-math.sin(angle), math.cos(angle), 0.0)) * half
            here = on_head(surface, angle, polar, numbers.SIZE + 0.012)
            rows_of_rib.append([here - across, here + across])
        full.add_skin(part, surface, rows_of_rib, part.named("Rib"), around=False)
    # The panel over the lower face and the bridge between the eyes.
    lift = numbers.PANEL_LIFT
    across_steps, up_steps = numbers.PANEL_STEPS
    for left, right, low, high in (numbers.LOWER, numbers.BRIDGE):
        grid = [[surface.front(left + (right - left) * column / across_steps, low + (high - low) * row / up_steps, lift)
                 for column in range(across_steps + 1)] for row in range(up_steps + 1)]
        full.add_skin(part, surface, grid, part.named("Knit"), around=False)
    # The rolled edge of each eye hole.
    middle_side, middle_up, half_width, half_height, radius = numbers.HOLE
    for sign in (1.0, -1.0):
        path, wide, thick = [], [], []
        for index in range(numbers.HOLE_STATIONS + 1):
            turn = 2.0 * math.pi * index / numbers.HOLE_STATIONS
            side = sign * middle_side + half_width * math.cos(turn)
            up = middle_up + half_height * math.sin(turn)
            out = KIT.outward(surface, side, up)
            point = surface.front(side, up, lift)
            ahead = surface.front(sign * middle_side + half_width * math.cos(turn + 0.01),
                                  middle_up + half_height * math.sin(turn + 0.01), lift) - point
            path.append(point)
            wide.append(out.cross(ahead.normalized()).normalized() * radius)
            thick.append(out * radius * 0.8)
        slab(part, path, wide, thick, part.named("Edge"), outline * 0.8, sides=6)


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


def wall_rect(part, crown, material, left, right, low, high, lift, columns=6):
    """A rectangle on the front of the crown, cut into columns and rows so that it follows the curve of
    the wall (one flat piece across the panel sinks under it in its middle)."""
    xs = [left + (right - left) * index / columns for index in range(columns + 1)]
    rows = max(1, math.ceil((high - low) / 0.02))
    heights = [low + (high - low) * index / rows for index in range(rows + 1)]
    full.add_skin(part, SimpleNamespace(middle=crown.middle), [[crown.front(x, height, lift) for x in xs]
                                                               for height in heights], material, around=False)


def wall_band(part, crown, material, places, half_width, lift):
    """A band on the front of the crown through "places" (across, height): a swirl, a stroke."""
    lower, upper = [], []
    for index, (x, h) in enumerate(places):
        before, after = places[max(index - 1, 0)], places[min(index + 1, len(places) - 1)]
        along = Vector((after[0] - before[0], after[1] - before[1])).normalized()
        normal = Vector((-along.y, along.x)) * half_width
        lower.append(crown.front(x - normal.x, h - normal.y, lift))
        upper.append(crown.front(x + normal.x, h + normal.y, lift))
    full.add_skin(part, SimpleNamespace(middle=crown.middle), [lower, upper], material, around=False)


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
    elif logo == "heart":
        edge = []
        for index in range(20):
            turn = 2.0 * math.pi * index / 20
            edge.append(at(math.sin(turn) ** 3, (13.0 * math.cos(turn) - 5.0 * math.cos(2.0 * turn)
                                                  - 2.0 * math.cos(3.0 * turn) - math.cos(4.0 * turn)) / 16.0 + 0.1))
        wall_shape(part, crown, material, edge, lift, at(0.0, 0.1))
    elif logo == "triangle":
        # An outline of a triangle (the Squid Game mark).
        def corners(reach):
            return [at(reach * math.cos(math.radians(90.0 + 120.0 * index)),
                       reach * math.sin(math.radians(90.0 + 120.0 * index)) - 0.1) for index in range(3)]
        wall_ring(part, crown, material, corners(0.62), corners(1.0), lift)
    elif logo == "ether":
        # The Ether mark: a tall diamond over a chevron.
        wall_shape(part, crown, material, [at(0.0, 1.0), at(0.56, 0.02), at(0.0, -0.30), at(-0.56, 0.02)], lift,
                   at(0.0, 0.25))
        wall_shape(part, crown, material, [at(0.0, -0.44), at(0.56, -0.12), at(0.0, -1.0), at(-0.56, -0.12)], lift,
                   at(0.0, -0.5))
    elif logo == "bow":
        # A bow: two triangles meeting at a knot.
        for sign in (1.0, -1.0):
            wall_shape(part, crown, material, [at(sign * 0.15, 0.0), at(sign * 1.0, 0.55), at(sign * 1.0, -0.55)], lift,
                       at(sign * 0.6, 0.0))
        wall_shape(part, crown, material, [at(math.cos(2.0 * math.pi * index / 8) * 0.25,
                                              math.sin(2.0 * math.pi * index / 8) * 0.25) for index in range(8)],
                   lift * 1.3, at(0.0, 0.0))
    elif logo == "chog":
        # A small spiky head, as the jackets of Side=Left wear it on the chest.
        edge = []
        for index in range(18):
            angle = math.pi * 0.5 + 2.0 * math.pi * index / 18
            reach = 1.0 if index % 2 == 0 else 0.66
            edge.append(at(math.cos(angle) * reach, math.sin(angle) * reach))
        wall_shape(part, crown, material, edge, lift, at(0.0, 0.0))
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
    "ClownNose": build_clown_nose,
    "Plunger": build_plunger,
    "Balaclava": build_balaclava,
}
# The numbers of each shape, where make_chog_traits.py reads its "LEAVES".
HAT_NUMBERS = {
    "Ushanka": USHANKA,
    "Headband": HEADBAND,
    "CurlyWig": CURLY_WIG,
    "Durag": DURAG,
    "WizardHat": WIZARD_HAT,
    "SpartanHelmet": SPARTAN,
    "ClownNose": CLOWN_NOSE,
    "Plunger": PLUNGER,
    "Balaclava": BALACLAVA,
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
    # The bars of the Bitcoin B over and under its first letter (15 October 2026).
    "Buttcoin Cap": {**cap((0.92, 0.52, 0.10), (0.27, 0.19, 0.11), "Buttcoin", INK_WHITE, (0.55, 0.28, 0.06)),
                     "bars": True},
    # "10K" in white on a purple band, "TPS" in purple, a purple frame around both (15 October 2026).
    # The box stands a little higher than a word alone: its foot would hide behind the raised brim.
    "10k TPS Cap": {**cap((0.93, 0.92, 0.92), (0.40, 0.40, 0.40), ("10K", "TPS"), (0.42, 0.22, 0.80)), "box": True,
                    "numbers": {"BRIM_LENGTH": 0.060, "BRIM_RISE_DEG": 35.0, "PRINT_BASELINE": 0.150},
                    "colours": {"Crown": (0.93, 0.92, 0.92), "Brim": (0.40, 0.40, 0.40), "Button": (0.93, 0.92, 0.92),
                                "Print": (0.42, 0.22, 0.80), "Print2": (0.96, 0.95, 0.97)}},
    "JEET Cap": cap((0.12, 0.72, 0.22), (0.45, 0.84, 0.52), "JEET", INK_WHITE, INK_DARK),
    "Jeet Cap": cap((0.12, 0.72, 0.22), (0.45, 0.84, 0.52), "JEET", INK_WHITE, INK_DARK),
    "RETARD Cap": cap((0.82, 0.30, 0.95), (0.84, 0.65, 0.96), "RETARD", (0.98, 0.94, 0.80), INK_DARK),
    "WAGMI Cap": cap((0.88, 0.13, 0.55), (0.91, 0.49, 0.74), "WAGMI", INK_WHITE, INK_DARK),
    "NEET": cap((0.95, 0.91, 0.86), (0.22, 0.25, 0.76), "NEET", INK_WHITE, (0.22, 0.25, 0.76)),
    "MCGA Cap": cap((0.95, 0.07, 0.06), (0.80, 0.07, 0.06), "MCGA", (0.98, 0.72, 0.14), (0.90, 0.42, 0.08)),
    "ETH Cap": cap((0.92, 0.92, 0.93), (0.74, 0.75, 0.78), ink=(0.52, 0.53, 0.58), logo="ether"),
    # Bucket hats, Side=Right, with the logo of their crown (lay_logo).
    "Mon Cap": bucket((0.40, 0.28, 0.65), (0.33, 0.24, 0.60), (0.24, 0.18, 0.52), mark="monad", mark_colour=INK_WHITE),
    # A cloud of curls: a spiral in the colour of the hat on each puff (15 October 2026).
    "Cloud Cap": {**bucket((0.25, 0.36, 0.85), (0.25, 0.36, 0.85), (0.13, 0.20, 0.62), mark="cloud",
                           mark_colour=INK_WHITE), "swirls": True,
                  "colours": {"Crown": (0.25, 0.36, 0.85), "Brim": (0.25, 0.36, 0.85), "Under": (0.13, 0.20, 0.62),
                              "Mark": INK_WHITE, "Swirl": (0.25, 0.36, 0.85)}},
    "Orange Bucket Cap": bucket((0.87, 0.40, 0.19), (0.87, 0.40, 0.19), (0.70, 0.26, 0.12)),
    "Gray Bucket Cap": bucket((0.56, 0.62, 0.67), (0.58, 0.64, 0.68), (0.45, 0.50, 0.55)),
    # Its label, edged by a dark line, says JUST A KID WITH / $1 AND A DREAM (15 October 2026).
    "Dollar Cap": {**bucket((0.15, 0.80, 0.94), (0.15, 0.80, 0.94), (0.09, 0.40, 0.58), mark="label",
                            mark_colour=(0.60, 0.90, 0.95)), "label_words": ("JUST A KID WITH", "$1 AND A DREAM"),
                   "colours": {"Crown": (0.15, 0.80, 0.94), "Brim": (0.15, 0.80, 0.94), "Under": (0.09, 0.40, 0.58),
                               "Mark": (0.60, 0.90, 0.95), "Ink": (0.07, 0.06, 0.07)}},
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
    # Values of "Head" that are not hats (15 October 2026), colours read on their layers.
    "Clown Nose": {
        "shape": "ClownNose",
        "colours": {"Ball": (0.93, 0.07, 0.07), "Glint": (0.98, 0.93, 0.93)},
        "unlit": ("Glint",),
    },
    "Plumber": {
        "shape": "Plunger",
        "colours": {"Cup": (0.89, 0.10, 0.10), "Lip": (0.56, 0.08, 0.09), "Stick": (0.52, 0.27, 0.16)},
    },
    "Robber": {
        "shape": "Balaclava",
        "colours": {"Knit": (0.39, 0.37, 0.41), "Rib": (0.26, 0.24, 0.27), "Edge": (0.46, 0.44, 0.48)},
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
# not drawn: see "print_missing". (So it was in batch 2. Batch 3, 15 October 2026, draws those prints
# and builds the costumes: see "The prints of the shirts of Side=Right" and "The costumes" below,
# which replace these entries; LEFT_OUT is then empty.)
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
    # The lapels (15 October 2026, asked by the owner): a band along each side of the V, in the colour
    # of the jacket, edged by the dark line of the model, as the pictures draw them. Its width at the
    # top and at the foot of the V, its half thickness, the thickness of its outline, stations along it.
    "LAPEL": (0.036, 0.010, 0.005, 0.006, 5),
    # The points of the collar of the shirt, each side of the knot: how far out and how far down.
    "COLLAR_POINTS": (0.046, 0.030),
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
    # The lapels: thin slabs along the two edges of the V, outside it.
    top_width, foot_width, half_thick, line, stations = numbers.LAPEL
    for sign in (1.0, -1.0):
        path, wide, thick = [], [], []
        for index in range(stations):
            share = index / (stations - 1)
            height = numbers.V_TOP + (numbers.V_FOOT - numbers.V_TOP) * share
            edge = numbers.V_HALF_WIDTH * (1.0 - share)
            width = top_width + (foot_width - top_width) * share
            centre = shirt_front(maker, height, sign * (edge + width * 0.5), low)
            outwards = (shirt_front(maker, height, sign * (edge + width * 0.5), low + 0.01) - centre).normalized()
            across = (shirt_front(maker, height, sign * (edge + width), low) - shirt_front(maker, height, sign * edge,
                                                                                            low)) * 0.5
            path.append(centre)
            wide.append(across)
            thick.append(outwards * half_thick)
        slab(torso, path, wide, thick, torso.named("Main"), line, sides=4)
    # The points of the collar of the shirt, over the top of the lapels.
    out, down = numbers.COLLAR_POINTS
    for sign in (1.0, -1.0):
        corners = [shirt_front(maker, numbers.V_TOP + 0.004, sign * 0.012, high + 0.002),
                   shirt_front(maker, numbers.V_TOP + 0.004, sign * out, high + 0.002),
                   shirt_front(maker, numbers.V_TOP - down, sign * 0.026, high + 0.002)]
        full.add_skin(torso, middle, [[corners[2]], corners[:2]], torso.named("Shirt"), around=False)


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


# ---------------------------------------------------------------------------------------------
# What is laid on the front of a shirt (15 October 2026), for every cut built on the shirt of
# make_chog_traits.py, which calls decorate_shirt(): a word or a number ("print"), a logo ("logo"),
# the squares of a pixel pattern ("pixels"), sewn patches ("patches"). Flat shapes, no outline.
# ---------------------------------------------------------------------------------------------
SHIRT_PRINT = {
    "PRINT_CUT": 0.040,          # the width of the pieces a word is cut into, to follow the torso
    "PRINT_SPACING": 1.12,
    "PRINT_BOLD": 0.034,         # how much the strokes are thickened (units of the font)
    "PRINT_LINE": 0.040,         # and how much more for the dark line around them
    "PRINT_LIFT": (0.004, 0.007),
    "PIXEL_LIFT": 0.003,
}


class ShirtPanel:
    """The front of the torso seen as the front panel of a hat, for lay_word and lay_logo of the
    caps: front(across, height, lift) is a point of the shirt, "across" to the left of the character
    from "offset"."""

    def __init__(self, maker, offset=0.0, middle_height=0.42):
        self.maker, self.offset = maker, offset
        self.middle = Vector((0.0, 0.0, middle_height))
        self.numbers = SimpleNamespace(**SHIRT_PRINT)

    def front(self, side, height, lift):
        return shirt_front(self.maker, height, side + self.offset, lift)


def shirt_square(torso, maker, material, across, height, half, turn_deg, lift, tall=None):
    """A square (a patch, a pixel), or a rectangle "tall" high, laid on the front of the shirt."""
    turn = math.radians(turn_deg)
    corners = []
    for x, y in ((-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0)):
        y_half = half if tall is None else tall
        side = across + half * x * math.cos(turn) - y_half * y * math.sin(turn)
        up = height + half * x * math.sin(turn) + y_half * y * math.cos(turn)
        corners.append(shirt_front(maker, up, side, lift))
    middle = SimpleNamespace(middle=Vector((0.0, 0.0, height)))
    full.add_skin(torso, middle, [corners[:2], [corners[3], corners[2]]], material, around=False)


def shirt_band(torso, maker, material, places, half_width, lift):
    """A band laid on the front of the shirt through "places" (across, height)."""
    lower, upper = [], []
    for index, (across, height) in enumerate(places):
        before, after = places[max(index - 1, 0)], places[min(index + 1, len(places) - 1)]
        along = Vector((after[0] - before[0], after[1] - before[1])).normalized()
        normal = Vector((-along.y, along.x)) * half_width
        lower.append(shirt_front(maker, height - normal.y, across - normal.x, lift))
        upper.append(shirt_front(maker, height + normal.y, across + normal.x, lift))
    middle = SimpleNamespace(middle=Vector((0.0, 0.0, sum(height for _, height in places) / len(places))))
    full.add_skin(torso, middle, [lower, upper], material, around=False)


def open_front(torso, maker, half_opening, fur_radius):
    """The front of a robe left open (Kings Robe): the faces of the shirt across the middle of the front
    are removed, so the body shows in its skin colour, and a roll of fur runs down each edge."""
    import bmesh
    gone = [face for face in torso.mesh.faces
            if face.calc_center_median().y < -0.01 and abs(face.calc_center_median().x) < half_opening]
    bmesh.ops.delete(torso.mesh, geom=gone, context="FACES")
    numbers = maker.numbers(KIT.TEE)
    heights = [numbers.HEM + (numbers.COLLAR[1] - numbers.HEM) * index / 5 for index in range(6)]
    for sign in (1.0, -1.0):
        path, wide, thick = [], [], []
        for height in heights:
            half_width, _ = KIT.torso_section(maker.body, height)
            edge = min(half_opening * 1.02, half_width * 0.6)
            centre = shirt_front(maker, height, sign * edge, fur_radius * 0.4)
            out = (shirt_front(maker, height, sign * edge, fur_radius * 0.4 + 0.01) - centre).normalized()
            path.append(centre)
            wide.append(out.cross(Z).normalized() * fur_radius)
            thick.append(out * fur_radius * 0.8)
        slab(torso, path, wide, thick, torso.named("Fur"), 0.010, sides=6)


def lay_flower(torso, maker, material, across, height, size, lift):
    """A flower of five round petals, as the Hawaiian polo is printed."""
    edge = []
    for index in range(15):
        turn = 2.0 * math.pi * index / 15
        reach = size * (0.62 + 0.38 * abs(math.cos(2.5 * turn)))
        edge.append(shirt_front(maker, height + reach * math.sin(turn), across + reach * math.cos(turn), lift))
    middle = SimpleNamespace(middle=Vector((0.0, 0.0, height)))
    full.add_skin(torso, middle, [[shirt_front(maker, height, across, lift)], edge], material)


def lay_zigzag(torso, maker, material, low, high, count, lift):
    """A band of triangles all around the shirt between two heights, points up (the cape of Wizard)."""
    middle = SimpleNamespace(middle=Vector((0.0, 0.0, (low + high) * 0.5)))
    numbers = maker.numbers(KIT.TEE)
    for index in range(count):
        corners = []
        for share, height in ((0.0, low), (1.0, low), (0.5, high)):
            angle = 2.0 * math.pi * (index + share) / count
            half_width, half_depth = KIT.torso_section(maker.body, height)
            reach = numbers.LIFT + lift
            corners.append(Vector((math.cos(angle) * (half_width + reach), math.sin(angle) * (half_depth + reach), height)))
        full.add_skin(torso, middle, [[corners[2]], corners[:2]], material, around=False)


def lay_shirt_print(torso, maker, spec, colours):
    """The word or the lines of "print" on the chest: spec is (lines, height of the middle, height of
    a capital, widest it may be, across). Its colour is "Print", edged by "PrintLine" when given."""
    lines, middle, capitals, widest, across = spec
    lines = (lines,) if isinstance(lines, str) else tuple(lines)
    panel = ShirtPanel(maker, across, middle)
    numbers = panel.numbers
    scale = capitals / KIT.FONT_CAPITALS
    wide = max(KIT.word_width(line, numbers.PRINT_BOLD, numbers.PRINT_SPACING) for line in lines)
    scale = min(scale, widest / wide)
    step = KIT.FONT_CAPITALS * scale * 1.30
    low, high = numbers.PRINT_LIFT
    top = middle + (len(lines) - 1) * step * 0.5 - KIT.FONT_CAPITALS * scale * 0.5
    for index, line in enumerate(lines):
        baseline = top - index * step
        if "PrintLine" in colours:
            KIT.lay_word(torso, panel, line, torso.named("PrintLine"), numbers.PRINT_BOLD + numbers.PRINT_LINE, low,
                         baseline, scale, resolution=1)
        KIT.lay_word(torso, panel, line, torso.named("Print"), numbers.PRINT_BOLD, high, baseline, scale)


def decorate_shirt(maker, torso):
    """What the entry of a shirt lays on its front: see above. Called by make_chog_traits.build_tee."""
    entry, colours = maker.entry, maker.entry["colours"]
    lift = SHIRT_PRINT["PIXEL_LIFT"]
    if entry.get("open_front"):
        open_front(torso, maker, *entry["open_front"])
    for across, height, half, tall in entry.get("labels", ()):
        shirt_square(torso, maker, torso.named("Label"), across, height, half, 0.0, lift, tall=tall)
    for across, height, half, turn in entry.get("patches", ()):
        shirt_square(torso, maker, torso.named("Patch"), across, height, half, turn, lift)
    if entry.get("sash"):
        places, half_width = entry["sash"]
        shirt_band(torso, maker, torso.named("Sash"), places, half_width, lift * 2.0)
    for places, half_width in entry.get("swirls", ()):
        shirt_band(torso, maker, torso.named("Swirl"), places, half_width, lift)
    for across, height, size in entry.get("flowers", ()):
        lay_flower(torso, maker, torso.named("Flower"), across, height, size, lift)
    if entry.get("zigzag"):
        low, high, count = entry["zigzag"]
        lay_zigzag(torso, maker, torso.named("Trim"), low, high, count, lift)
    for logo, across, height, size, key in entry.get("logos", ()):
        lay_logo(torso, ShirtPanel(maker, across, height), logo, torso.named(key), (height, size),
                 SHIRT_PRINT["PRINT_LIFT"][1])
    if entry.get("pixels"):
        # A pattern of small squares, as the "glitch" and pixel prints are drawn: the same places on
        # every shirt (a fixed sequence), on the front of the torso.
        count, half = entry["pixels"]
        sequence = random.Random(20261015)
        for _ in range(count):
            across, height = sequence.uniform(-0.12, 0.12), sequence.uniform(0.27, 0.56)
            shirt_square(torso, maker, torso.named("Pixel"), across, height, half * sequence.uniform(0.7, 1.3), 0.0,
                         lift)
    if entry.get("logo"):
        logo, across, height, size = entry["logo"]
        lay_logo(torso, ShirtPanel(maker, across, height), logo, torso.named("Logo"), (height, size),
                 SHIRT_PRINT["PRINT_LIFT"][1])
    if entry.get("print"):
        lay_shirt_print(torso, maker, entry["print"], colours)


# ---------------------------------------------------------------------------------------------
# Hoodies (Side=Left, 15 October 2026). Hood down: the shirt with long sleeves, a thick roll of the
# hood around the back of the neck and over the shoulders, and two drawstrings hanging from the front
# of the collar, as the pictures draw them. Hood up (Yellow Hoodie): the same, and a hood over the
# head, framing the face, in place of the crown of leaves (the entry gives "head": its LEAVES).
# ---------------------------------------------------------------------------------------------
HOODIE = {
    # The roll: the height of its middle, how far it stands out of the torso at that height (to the
    # sides, behind), its radius at its two ends and in the middle of the back, how far around the
    # neck it goes from the back to each end (degrees), stations along it, sides of its section.
    "ROLL_HEIGHT": 0.566,
    "ROLL_OUT": (0.034, 0.042),
    "ROLL_RADIUS": (0.022, 0.044),
    "ROLL_AROUND_DEG": 140.0,
    "ROLL_STATIONS": 9,
    "ROLL_SIDES": 6,
    # The drawstrings: to each side of the middle, from which height to which, half width.
    "STRINGS": (0.030, 0.570, 0.452, 0.0055),
    "OUTLINE_THICKNESS": 0.014,
}

# The hood up, on the head (axes of "Head", as the hats): polar degrees of the edge of its opening by
# degrees from the front (over the brow, down beside the face to the jaw, down to the neck behind),
# how much larger than the head it is, rows, sides, and the roll along the edge of the opening (its
# radius, and how far around from the front it runs).
HOOD = {
    "RIM": ((0.0, 52.0), (35.0, 62.0), (62.0, 116.0), (110.0, 148.0), (180.0, 156.0)),
    "SIZE": ((0.0, 1.12), (60.0, 1.20), (180.0, 1.22)),
    "ROWS": 4,
    "SIDES": 18,
    "EDGE": (0.034, 74.0),
    "EDGE_STATIONS": 11,
    "OUTLINE_THICKNESS": 0.020,
}


def build_hood_up(maker):
    """A hood over the head (a part on "Head"), its opening framing the face, a roll along the edge."""
    numbers = SimpleNamespace(**{**HOOD, **maker.entry.get("hood_numbers", {})})
    part, surface = maker.part(on="Head"), maker.surface
    sides, rows = numbers.SIDES, numbers.ROWS
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(numbers.RIM, from_front(angle))) for angle in angles]
    sizes = [interpolate(numbers.SIZE, from_front(angle)) for angle in angles]
    loops = [[on_head(surface, angle, polar, 0.90) for angle, polar in zip(angles, rims)]]
    for row in range(rows):
        share = row / rows
        loops.append([on_head(surface, angle, polar * (1.0 - share), size) for angle, polar, size in
                      zip(angles, rims, sizes)])
    loops.append([on_head(surface, 0.0, 0.0, interpolate(numbers.SIZE, 90.0))])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * rows + [(1.0,)]
    KIT.add_loops(part, loops, part.named("Hood"), numbers.OUTLINE_THICKNESS, shares=shares)
    # The roll along the edge of the opening, from the jaw on one side over the brow to the other.
    radius, around = numbers.EDGE
    path, wide, thick = [], [], []
    for index in range(numbers.EDGE_STATIONS):
        degrees = -around + 2.0 * around * index / (numbers.EDGE_STATIONS - 1)
        angle = math.radians(270.0 + degrees)
        polar = math.radians(interpolate(numbers.RIM, abs(degrees)))
        out = head_normal(surface, angle, polar)
        path.append(on_head(surface, angle, polar, interpolate(numbers.SIZE, abs(degrees))) - out * radius * 0.4)
        along = (on_head(surface, angle + 0.01, polar, 1.0) - on_head(surface, angle - 0.01, polar, 1.0)).normalized()
        across = out.cross(along).normalized()
        wide.append(across * radius)
        thick.append(out * radius)
    rim_colour = "Rim" if "Rim" in maker.entry["colours"] else "Hood"
    slab(part, path, wide, thick, part.named(rim_colour), numbers.OUTLINE_THICKNESS, sides=6)


def build_hoodie(maker):
    """A hoodie: the cut "Track" (long sleeves; stripes down them when the entry gives "Stripe"), the
    roll of the hood around the neck and two drawstrings, or the hood up when the entry says "hood": "up"."""
    build_track(maker)
    numbers = SimpleNamespace(**HOODIE)
    torso, body = maker.parts[0][0], maker.body
    if maker.entry.get("hood") == "up":
        build_hood_up(maker)
    else:
        height = numbers.ROLL_HEIGHT
        half_width, half_depth = KIT.torso_section(body, height)
        out_side, out_back = numbers.ROLL_OUT
        thin, thick_radius = numbers.ROLL_RADIUS
        path, wide, thick = [], [], []
        for index in range(numbers.ROLL_STATIONS):
            share = -1.0 + 2.0 * index / (numbers.ROLL_STATIONS - 1)
            angle = math.radians(share * numbers.ROLL_AROUND_DEG)     # 0: the back, + to the left
            middle = Vector((math.sin(angle) * (half_width + out_side), math.cos(angle) * (half_depth + out_back),
                             height + 0.012 * math.cos(angle)))
            radius = thin + (thick_radius - thin) * math.cos(share * math.pi * 0.5) ** 2
            outward = Vector((math.sin(angle) / (half_width + out_side), math.cos(angle) / (half_depth + out_back),
                              0.0)).normalized()
            path.append(middle)
            wide.append(outward * radius)
            thick.append(Z * radius * 0.85)
        slab(torso, path, wide, thick, torso.named("Main"), numbers.OUTLINE_THICKNESS, sides=numbers.ROLL_SIDES)
    # The drawstrings.
    across, top, foot, half = numbers.STRINGS
    string = torso.named("String") if "String" in maker.entry["colours"] else torso.named("Collar")
    for side in (1.0, -1.0):
        rows = [[shirt_front(maker, height, side * across + edge, 0.006) for edge in (-half, half)]
                for height in (top, (top + foot) * 0.5, foot)]
        full.add_skin(torso, SimpleNamespace(middle=Vector((0.0, 0.0, 0.5))), rows, string, around=False)


# ---------------------------------------------------------------------------------------------
# Costumes (15 October 2026): a suit with long sleeves and a hood up over the head (Gangster suit,
# Shark Suit, Banana suit, Dragon suit), a space suit with the ring of its helmet (Astro Suit), and a
# rubber duck float around the body (Duck Float). These Genesis wear no value of "Head": the hood
# replaces the crown of leaves (the entry gives "head": its LEAVES), as the pictures show.
# ---------------------------------------------------------------------------------------------
COSTUME = {
    # The belly in another colour (shark, dragon): half its width, its lower and upper edge.
    "BELLY": (0.085, 0.300, 0.540),
    # The teeth of the shark along the edge of the hood: how many, from how far round on each side
    # (degrees from the front), how long, how wide at their foot (degrees).
    "TEETH": (11, 70.0, 0.050, 6.0),
    # The fin on the back of the hood: polar degrees where it starts and ends, its height, half thickness.
    "FIN": (24.0, 96.0, 0.150, 0.016),
    # The horns of the dragon: degrees from the front, polar degrees, length, radius at the foot.
    "HORNS": (34.0, 40.0, 0.130, 0.040),
    # The tip of the banana: how far the top of the hood rises, then the stalk (length, radius).
    "PEAK": 0.32,
    "STALK": (0.080, 0.030),
    # The bandana over the lower face: in the coordinates of the face, its upper corners (to the side,
    # above), its point below the chin, how far off the head, its dots.
    "BANDANA": (0.27, -0.040, -0.300, 0.032),
    "BANDANA_DOTS": ((-0.12, -0.085), (0.0, -0.090), (0.12, -0.085), (-0.06, -0.150), (0.06, -0.150),
                     (0.0, -0.210), (-0.17, -0.060), (0.17, -0.060)),
    # The wings of the dragon, on the back: the shoulder they leave from (to the side, height), their
    # span, their height, their thickness, and the way they spread (to the side, back, up).
    # (First: 0.20 spread mostly to the sides, in the red of the suit: hidden behind the arms.)
    "WINGS": (0.06, 0.54, 0.27, 0.20, 0.010),
    "WING_WAY": (0.75, 0.95, 0.45),
    "OUTLINE_THICKNESS": 0.014,
}


def build_costume(maker):
    """A costume with a hood up: the suit ("Track" with long sleeves), its belly, the hood over the head,
    and what the entry adds ("teeth", "fin", "horns", "peak", "bandana", "wings")."""
    entry = maker.entry
    numbers = SimpleNamespace(**COSTUME)
    build_track(maker)
    torso = maker.parts[0][0]
    colours = entry["colours"]
    if "Belly" in colours:
        half, low, high = numbers.BELLY
        rows = []
        for index in range(5):
            height = low + (high - low) * index / 4
            wide = half * math.sin(math.pi * (0.15 + 0.7 * index / 4))
            rows.append([shirt_front(maker, height, side, 0.004) for side in (-wide, -wide * 0.33, wide * 0.33, wide)])
        full.add_skin(torso, SimpleNamespace(middle=Vector((0.0, 0.0, (low + high) * 0.5))), rows, torso.named("Belly"),
                      around=False)
    if entry.get("wings"):
        side_at, height, span, tall, thick = numbers.WINGS
        to_side, to_back, to_up = numbers.WING_WAY
        for sign in (1.0, -1.0):
            root = Vector((sign * side_at, 0.10, height))
            out = Vector((sign * to_side, to_back, to_up)).normalized()
            up = Vector((0.0, 0.25, 1.0)).normalized()
            # A bat wing: its top edge, then three points along its lower edge, scalloped.
            outline_points = [root, root + out * span + up * tall * 0.6, root + out * span * 0.95,
                              root + out * span * 0.62 + up * tall * 0.05, root + out * span * 0.45 - up * tall * 0.15,
                              root + out * span * 0.28 - up * tall * 0.02, root + out * span * 0.12 - up * tall * 0.22]
            normal = out.cross(up).normalized()
            front = [point + normal * thick for point in outline_points]
            back = [point - normal * thick for point in outline_points]
            KIT.add_loops(torso, [back, front], torso.named("Wing"), numbers.OUTLINE_THICKNESS)
    build_hood_up(maker)
    hood = maker.parts[-1][0]
    surface = maker.surface
    hood_numbers = SimpleNamespace(**{**HOOD, **entry.get("hood_numbers", {})})
    if entry.get("teeth"):
        count, around, length, half_deg = numbers.TEETH
        for index in range(count):
            degrees = -around + 2.0 * around * index / (count - 1)
            angle = math.radians(270.0 + degrees)
            polar = math.radians(interpolate(hood_numbers.RIM, abs(degrees)))
            size = interpolate(hood_numbers.SIZE, abs(degrees))
            corners = [on_head(surface, angle + math.radians(turn), polar, size * 0.99) for turn in (-half_deg, half_deg)]
            # The point of a tooth: towards the middle of the face, along the head.
            middle_of_face = surface.front(0.0, 0.0, 0.0)
            foot = (corners[0] + corners[1]) * 0.5
            tip = foot + (middle_of_face - foot).normalized() * length + head_normal(surface, angle, polar) * 0.004
            full.add_skin(hood, surface, [[tip], corners], hood.named("Tooth"), around=False)
    if entry.get("fin"):
        start, end, height, half_thick = numbers.FIN
        angle = math.radians(90.0)
        path, wide, thick = [], [], []
        for index in range(5):
            share = index / 4
            polar = math.radians(start + (end - start) * share)
            out = head_normal(surface, angle, polar)
            rise = height * math.sin(math.pi * min(1.0, share * 1.25)) ** 0.8 * (1.0 - 0.6 * share)
            foot = on_head(surface, angle, polar, interpolate(hood_numbers.SIZE, 180.0)) - out * 0.02
            path.append(foot + out * (rise + 0.02) * 0.5)
            wide.append(X * half_thick)
            thick.append(out * (rise + 0.02) * 0.5)
        slab(hood, path, wide, thick, hood.named("Hood"), numbers.OUTLINE_THICKNESS, sides=4)
    if entry.get("horns"):
        degrees, polar_deg, length, radius = numbers.HORNS
        for sign in (1.0, -1.0):
            angle, polar = math.radians(270.0 + sign * degrees), math.radians(polar_deg)
            out = head_normal(surface, angle, polar)
            foot = on_head(surface, angle, polar, interpolate(hood_numbers.SIZE, degrees)) - out * 0.02
            way = (out + Z * 0.6).normalized()
            hood.shell(base.tube_rings(foot, way, X, ((0.0, radius), (length * 0.5, radius * 0.75), (length, radius * 0.15))),
                       7, hood.named("Horn"), numbers.OUTLINE_THICKNESS)
    if entry.get("peak"):
        # The banana: a tall point rising from the top of the hood, its stalk at the tip.
        length, radius = numbers.STALK
        top = on_head(surface, 0.0, 0.0, interpolate(hood_numbers.SIZE, 90.0))
        rise = numbers.PEAK
        hood.shell(base.tube_rings(top - Z * 0.10, Z, X, ((0.0, 0.20), (0.10, 0.17), (rise * 0.55, 0.10),
                                                         (rise, 0.045))), 10, hood.named("Hood"), numbers.OUTLINE_THICKNESS)
        hood.shell(base.tube_rings(top + Z * (rise - 0.01), (Z - Y * 0.3).normalized(), X,
                                   ((0.0, radius), (length, radius * 0.8))), 6, hood.named("Tip"),
                   numbers.OUTLINE_THICKNESS * 0.8)
    if entry.get("bandana"):
        # A triangle of cloth over the lower face, from below the eyes to a point under the chin, with
        # white dots: it covers the mouth ("HIDES" in "head").
        half, top, point, lift = numbers.BANDANA
        rows = []
        for index in range(5):
            share = index / 4
            height = top + (point - top) * share
            wide = half * (1.0 - share) + 0.012
            rows.append([surface.front(side, height, lift) for side in (-wide, -wide * 0.5, 0.0, wide * 0.5, wide)])
        full.add_skin(hood, surface, rows, hood.named("Bandana"), around=False)
        for side, height in numbers.BANDANA_DOTS:
            full.add_skin(hood, surface, full.patch_loops(surface, (side, height), 0.010, 0.010, 2.0, 0.0, lift + 0.003, 1, 6),
                          hood.named("Dot"))


ASTRO = {
    # The ring of the helmet around the face: polar degrees of the opening by degrees from the front
    # (it goes all round the face), how much larger than the head, its radius.
    "RING": ((0.0, 50.0), (45.0, 66.0), (75.0, 110.0), (110.0, 160.0), (180.0, 170.0)),
    "RING_SIZE": 1.12,
    "RING_RADIUS": 0.040,
    "RING_STATIONS": 18,
    "RING_AROUND_DEG": 118.0,
    # The collar of the suit around the neck: height, how far it stands out, its radius.
    "COLLAR": (0.585, 0.030, 0.034),
    "OUTLINE_THICKNESS": 0.016,
}


def build_astro(maker):
    """The space suit: white suit with long sleeves (grey stripes down them), a thick collar, and the
    ring of the helmet around the face. The glass of the helmet is not drawn: the face and the crown
    show as through it."""
    numbers = SimpleNamespace(**ASTRO)
    build_track(maker)
    torso, body = maker.parts[0][0], maker.body
    height, out_by, radius = numbers.COLLAR
    half_width, half_depth = KIT.torso_section(body, height)
    path, wide, thick = [], [], []
    for index in range(17):
        angle = 2.0 * math.pi * index / 16
        outward = Vector((math.cos(angle) / (half_width + out_by), math.sin(angle) / (half_depth + out_by), 0.0)).normalized()
        path.append(Vector((math.cos(angle) * (half_width + out_by), math.sin(angle) * (half_depth + out_by), height)))
        wide.append(outward * radius)
        thick.append(Z * radius)
    slab(torso, path, wide, thick, torso.named("Ring"), numbers.OUTLINE_THICKNESS, sides=6)
    ring = maker.part(on="Head")
    surface = maker.surface
    path, wide, thick = [], [], []
    for index in range(numbers.RING_STATIONS + 1):
        degrees = -numbers.RING_AROUND_DEG + 2.0 * numbers.RING_AROUND_DEG * index / numbers.RING_STATIONS
        angle = math.radians(270.0 + degrees)
        polar = math.radians(interpolate(numbers.RING, abs(degrees)))
        out = head_normal(surface, angle, polar)
        point = on_head(surface, angle, polar, numbers.RING_SIZE)
        ahead = on_head(surface, angle + 0.01, polar, numbers.RING_SIZE) - on_head(surface, angle - 0.01, polar,
                                                                                    numbers.RING_SIZE)
        path.append(point)
        wide.append(out.cross(ahead.normalized()).normalized() * numbers.RING_RADIUS)
        thick.append(out * numbers.RING_RADIUS)
    slab(ring, path, wide, thick, ring.named("Ring"), numbers.OUTLINE_THICKNESS, sides=6)


DUCK = {
    # The float: a ring around the body (height, how far its middle stands out of the torso, the radius
    # of its tube), and the head of the duck on it (degrees round from the front towards the left of the
    # character, its radius, the beak: length, radius).
    "RING": (0.300, 0.060, 0.062),
    "RING_SIDES": (18, 8),
    "HEAD": (-42.0, 0.085),
    "BEAK": (0.075, 0.034),
    "EYE": 0.014,
    "OUTLINE_THICKNESS": 0.016,
}


def build_duck_float(maker):
    """A rubber duck float around the waist: a yellow ring, the head of the duck with its beak and eyes.
    No clothing: the body is bare, as drawn."""
    numbers = SimpleNamespace(**DUCK)
    float_part, body = maker.part(on="Body"), maker.body
    height, out_by, radius = numbers.RING
    around, sides = numbers.RING_SIDES
    half_width, half_depth = KIT.torso_section(body, height)
    loops = []
    for index in range(around):
        angle = 2.0 * math.pi * index / around
        middle = Vector((math.cos(angle) * (half_width + out_by), math.sin(angle) * (half_depth + out_by), height))
        outward = Vector((math.cos(angle), math.sin(angle), 0.0))
        loops.append([middle + (outward * math.cos(2.0 * math.pi * step / sides) + Z * math.sin(2.0 * math.pi * step / sides))
                      * radius for step in range(sides)])
    # A torus: the rings joined round, the last to the first.
    mesh = float_part.mesh
    rows = [[mesh.verts.new(point) for point in loop] for loop in loops]
    faces = []
    for index in range(around):
        lower, upper = rows[index], rows[(index + 1) % around]
        for step in range(sides):
            after = (step + 1) % sides
            faces.append(mesh.faces.new((lower[step], lower[after], upper[after], upper[step])))
    for face in faces:
        face.smooth = True
    float_part.paint(faces, float_part.named("Duck"))
    float_part.outlined.append((faces, numbers.OUTLINE_THICKNESS, None))
    degrees, head_radius = numbers.HEAD
    angle = math.radians(270.0 + degrees)
    way = Vector((math.cos(angle), math.sin(angle), 0.0))
    centre = Vector((way.x * (half_width + out_by + radius * 0.4), way.y * (half_depth + out_by + radius * 0.4),
                     height + radius + head_radius * 0.75))
    float_part.shell(base.ball_rings(centre, Z, X, head_radius, head_radius, head_radius, 5), 10, float_part.named("Duck"),
                     numbers.OUTLINE_THICKNESS)
    length, beak = numbers.BEAK
    float_part.shell(base.tube_rings(centre + way * head_radius * 0.75, way, Z, ((0.0, beak), (length * 0.6, beak * 0.8),
                                                                              (length, beak * 0.3))), 8,
                     float_part.named("Beak"), numbers.OUTLINE_THICKNESS * 0.8)
    across = Z.cross(way).normalized()
    for sign in (1.0, -1.0):
        eye = centre + way * head_radius * 0.55 + across * sign * head_radius * 0.55 + Z * head_radius * 0.40
        disc_on(float_part, SimpleNamespace(middle=centre), eye + (eye - centre).normalized() * 0.004,
                (eye - centre).normalized(), numbers.EYE, float_part.named("Eye"), segments=8)


CLOTH_KINDS = {"Jacket": build_jacket, "Track": build_track, "Hoodie": build_hoodie, "Costume": build_costume,
               "Astro": build_astro, "DuckFloat": build_duck_float}


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
    # (Added 15 October 2026: 18 Genesis, missed by the first pass. Tie read darker than the jacket.)
    "Cyan tuxedo": jacket((0.06, 0.89, 0.90), WHITE, (0.06, 0.55, 0.56)),
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

def with_(entry, colours=None, **keys):
    """An entry of CLOTHES with more colours and more keys (a print, a logo, pixels, patches, a hood)."""
    return {**entry, **keys, "colours": {**entry["colours"], **(colours or {})}}


def hoodie(main, string=(0.91, 0.89, 0.87), hood="down", **keys):
    """A hoodie of Side=Left: long sleeves, the hood down (or up), drawstrings. "keys" go to the entry."""
    entry = {"kind": "Hoodie", "colours": {"Main": main, "Collar": shade(main), "String": string},
             "numbers": {"STEP": 0.076, "SLEEVE": LONG_SLEEVE}, "hood": hood}
    return {**entry, **keys}


def jersey(main):
    """A basketball jersey of Side=Left: no sleeves, a white collar, the number 143 on the chest."""
    return with_(tee(main, collar=WHITE, SLEEVE=0.0, COLLAR=(0.566, 0.593)),
                 colours={"Print": WHITE, "PrintLine": INK_DARK}, print=("143", 0.430, 0.085, 0.200, 0.0))


# Side=Left (15 October 2026): the shirts, hoodies, tops without sleeves, jerseys and jackets worn by
# 6 to 18 Genesis each, that only the pictures of Side=Left draw. Colours read on the layer of each value
# rebuilt from all its pictures (as above). The pictures are framed on the face: what shows is the
# collar, the chest and the top of the sleeves.
WORDS_THAT_CHANGE = "a word that changes from one Genesis to another (HODL, IDGAF, purple-pilled...)"
SLEEVELESS = {"SLEEVE": 0.0, "COLLAR": (0.566, 0.593)}
PATCHES = ((0.050, 0.405, 0.030, 12.0), (-0.085, 0.500, 0.022, -10.0))
CLOTHES.update({
    # Hoodies, the hood down: a roll around the neck, white drawstrings.
    "Orange Hoodie": hoodie((0.91, 0.33, 0.12)),
    # The Monad mark in white on the chest, on the right of the character (the left of the pictures).
    "Mon Hoodie": with_(hoodie((0.50, 0.40, 0.87)), colours={"Collar": (0.34, 0.27, 0.72), "Logo": (0.97, 0.97, 0.98)},
                        logo=("monad", -0.085, 0.500, 0.030)),
    # Dark grey, two yellow stripes down the sleeves.
    "Electro Hoodie": with_(hoodie((0.34, 0.38, 0.43), string=(0.80, 0.92, 0.95)), colours={"Stripe": (0.94, 0.76, 0.18)}),
    # Cyan and pink cloth printed with squares, as pixels.
    "Glitch Hoodie": with_(hoodie((0.07, 0.82, 0.96)), colours={"Pixel": (0.05, 0.50, 0.88)}, pixels=(26, 0.010)),
    "Sky Hoodie": hoodie((0.06, 0.70, 0.92)),
    "Peach Hoodie": with_(hoodie((0.96, 0.75, 0.70)), colours={"Pixel": (0.88, 0.57, 0.52)}, pixels=(26, 0.010)),
    # The hood up, over the head: no leaf comes out of it (the pictures show none); the teeth of hair
    # of the brow show under its edge, as drawn.
    "Yellow Hoodie": with_(hoodie((0.95, 0.75, 0.19), hood="up"), colours={"Hood": (0.95, 0.75, 0.19),
                                                                        "Rim": (0.84, 0.55, 0.12)},
                           head={"LEAVES": NO_CROWN}),
    # Tops without sleeves.
    "Red Sleeveless": tee((0.80, 0.29, 0.24), **SLEEVELESS),
    "Orange Sleeveless": tee((0.95, 0.54, 0.17), **SLEEVELESS),
    "White Sleeveless": tee((0.95, 0.95, 0.95), collar=(0.78, 0.78, 0.80), **SLEEVELESS),
    "Blue Sleeveless": tee((0.50, 0.64, 0.95), **SLEEVELESS),
    # Jerseys: 143 on the chest.
    "Yellow 143 Jersey": jersey((0.95, 0.83, 0.49)),
    "Blue 143 Jersey": jersey((0.51, 0.64, 0.95)),
    "Red 143 Jersey": jersey((0.83, 0.30, 0.26)),
    # Tees and shirts in one colour. The tees carry a word that is not the same on every Genesis.
    "blue tee": tee((0.37, 0.61, 0.92), missing=WORDS_THAT_CHANGE),
    "red tee": tee((0.95, 0.32, 0.12), missing=WORDS_THAT_CHANGE),
    "dark tee": tee((0.25, 0.25, 0.28), collar=(0.16, 0.16, 0.18), missing=WORDS_THAT_CHANGE),
    "white tee": tee((0.95, 0.95, 0.95), collar=(0.80, 0.80, 0.82), missing=WORDS_THAT_CHANGE),
    "Grey Shirt": tee((0.74, 0.80, 0.83)),
    "Purple Shirt": tee((0.88, 0.67, 0.92)),
    "White Shirt": tee((0.95, 0.95, 0.95), collar=(0.80, 0.80, 0.82)),
    "Pink Shirt": tee((0.95, 0.57, 0.57)),
    "Cyan Shirt": tee((0.49, 0.92, 0.87)),
    # Tees with their word.
    "Zero THOTS": with_(tee((0.71, 0.51, 0.90)), colours={"Print": (0.97, 0.93, 0.99)},
                        print=(("ZERO", "THOTS"), 0.425, 0.050, 0.200, 0.0)),
    "Virgin": with_(tee((0.92, 0.56, 0.41)), colours={"Print": (0.99, 0.93, 0.88)},
                    print=("VIRGIN", 0.420, 0.055, 0.210, 0.0)),
    "BBC": with_(tee((0.42, 0.68, 0.95)), colours={"Print": (0.97, 0.97, 0.97), "PrintLine": (0.12, 0.18, 0.25)},
                 print=("BBC", 0.420, 0.075, 0.200, 0.0)),
    # Worn shirts with sewn patches.
    "Brown Homeless Shirt": with_(tee((0.76, 0.59, 0.44)), colours={"Patch": (0.54, 0.66, 0.75)}, patches=PATCHES),
    "Grey Homeless Shirt": with_(tee((0.58, 0.64, 0.69)), colours={"Patch": (0.40, 0.45, 0.50)}, patches=PATCHES),
    "Blue Homeless Shirt": with_(tee((0.55, 0.61, 0.86)), colours={"Patch": (0.40, 0.47, 0.70)}, patches=PATCHES),
    # Track jackets: two stripes down each sleeve, a zip, a small spiky head on the chest.
    "dark jacket": with_(track((0.24, 0.21, 0.40), stripe=(0.97, 0.97, 0.99), zip_colour=(0.90, 0.89, 0.88),
                               SLEEVE=LONG_SLEEVE), colours={"Logo": (0.97, 0.97, 0.99)},
                         logo=("chog", 0.075, 0.480, 0.020)),
    "Red jacket": with_(track((0.79, 0.13, 0.13), stripe=(0.97, 0.70, 0.27), zip_colour=(0.92, 0.91, 0.89),
                              SLEEVE=LONG_SLEEVE), colours={"Logo": (0.97, 0.70, 0.27)},
                        logo=("chog", 0.075, 0.480, 0.020)),
    "Blue jacket": with_(track((0.21, 0.27, 0.70), stripe=(0.96, 0.97, 0.99), zip_colour=(0.92, 0.90, 0.85),
                               SLEEVE=LONG_SLEEVE), colours={"Logo": (0.96, 0.97, 0.99)},
                         logo=("chog", 0.075, 0.480, 0.020)),
    # Padded vests: no sleeves, the padding as darker bands.
    "pink vest": {"kind": "Tee", "colours": {"Main": (0.81, 0.43, 0.91), "Stripe": (0.70, 0.36, 0.82),
                                             "Collar": (0.44, 0.23, 0.51)},
                  "stripes": True, "numbers": {"SLEEVE": 0.0, "STEP": 0.046}},
    "purple vest": {"kind": "Tee", "colours": {"Main": (0.68, 0.54, 0.90), "Stripe": (0.45, 0.78, 0.93),
                                               "Collar": (0.45, 0.36, 0.70)},
                    "stripes": True, "numbers": {"SLEEVE": 0.0, "STEP": 0.046}},
    "dark vest": {"kind": "Tee", "colours": {"Main": (0.28, 0.25, 0.33), "Stripe": (0.22, 0.20, 0.27),
                                             "Collar": (0.17, 0.15, 0.21)},
                  "stripes": True, "numbers": {"SLEEVE": 0.0, "STEP": 0.046}},
    "Army Vest": {**tee((0.50, 0.53, 0.33), collar=(0.36, 0.37, 0.22), **SLEEVELESS),
                  "print_missing": "the camouflage and the flag patch"},
})

def printed(name, colours=None, **keys):
    """An entry of CLOTHES given what was missing on it (a print, a logo...): "print_missing" is dropped."""
    entry = {key: value for key, value in CLOTHES[name].items() if key != "print_missing"}
    return with_(entry, colours, **keys)


def arc(across, height, radius, start_deg, stop_deg, steps=6):
    """Places along an arc of a circle on the front of a shirt (for swirls)."""
    return [(across + radius * math.cos(math.radians(start_deg + (stop_deg - start_deg) * index / steps)),
             height + radius * math.sin(math.radians(start_deg + (stop_deg - start_deg) * index / steps)))
            for index in range(steps + 1)]


# The prints of the shirts of Side=Right (15 October 2026), placed as the pictures draw them on the
# chest; the pictures of Side=Right show the left of the character at their right.
INK_BLACK = (0.08, 0.06, 0.07)
CLOTHES.update({
    "Inmate": printed("Inmate", {"Label": (0.83, 0.39, 0.10), "Print": (0.30, 0.15, 0.06)},
                      labels=((0.060, 0.330, 0.062, 0.020),), print=("INMATE", 0.326, 0.026, 0.110, 0.060)),
    "Six Seven": printed("Six Seven", {"Print": INK_BLACK, "PrintLine": WHITE}, print=("67", 0.395, 0.110, 0.200, 0.0)),
    "Chug Life": printed("Chug Life", {"Print": INK_BLACK}, print=("CHUG LIFE", 0.420, 0.046, 0.250, 0.0)),
    "CWO": printed("CWO", {"Print": (0.96, 0.67, 0.25), "PrintLine": INK_DARK}, print=("CWO", 0.410, 0.075, 0.220, 0.0)),
    "MILF Lover": printed("MILF Lover", {"Print": (0.97, 0.97, 0.97), "PrintLine": INK_DARK, "Heart": (0.95, 0.09, 0.09)},
                          print=("MILF", 0.420, 0.054, 0.150, -0.030),
                          logos=(("heart", 0.088, 0.422, 0.024, "Heart"),)),
    "Hawaiian Polo": printed("Hawaiian Polo", {"Flower": (0.98, 0.88, 0.88)},
                             flowers=((-0.090, 0.505, 0.026), (0.050, 0.535, 0.024), (0.105, 0.425, 0.026),
                                      (-0.025, 0.410, 0.025), (-0.110, 0.330, 0.023), (0.040, 0.305, 0.026),
                                      (0.125, 0.285, 0.020))),
    "Yellow Yukata": printed("Yellow Yukata", {"Swirl": (0.99, 0.75, 0.38)},
                             swirls=((arc(-0.07, 0.48, 0.035, 0.0, 270.0), 0.006), (arc(0.06, 0.52, 0.030, 90.0, 360.0), 0.006),
                                     (arc(0.09, 0.38, 0.035, 180.0, 450.0), 0.006),
                                     (arc(-0.06, 0.34, 0.032, -90.0, 180.0), 0.006),
                                     (arc(0.00, 0.43, 0.022, 0.0, 300.0), 0.005))),
    "Squid Game Guard": printed("Squid Game Guard", {"Logo": (0.97, 0.97, 0.97)},
                                logos=(("triangle", 0.075, 0.395, 0.030, "Logo"),)),
    "Jesus": printed("Jesus", {"Sash": (0.80, 0.22, 0.14)},
                     sash=(((0.118, 0.565), (0.055, 0.485), (-0.020, 0.405), (-0.100, 0.310)), 0.034)),
    # Its front is open: the body shows in its skin colour between two rolls of white fur.
    "Kings Robe": printed("Kings Robe", {"Fur": (0.96, 0.91, 0.89)}, open_front=(0.058, 0.022)),
    # Yellow triangles along the edge of the cape and a dark bow at the neck.
    "Wizard": printed("Wizard", {"Trim": (0.92, 0.68, 0.10), "Bow": (0.09, 0.11, 0.27)}, zigzag=(0.442, 0.478, 16),
                      logos=(("bow", 0.0, 0.552, 0.030, "Bow"),)),
    # The green track suit of the players, its number on the chest (18 Genesis).
    "Squid Game": with_(track((0.12, 0.53, 0.43), zip_colour=(0.96, 0.97, 0.97), SLEEVE=LONG_SLEEVE),
                        {"Print": (0.96, 0.97, 0.97)}, print=("069", 0.400, 0.060, 0.200, 0.030)),
})

def costume(main, hood_colours, head=None, **keys):
    """A costume of COSTUME: the suit in "main" with long sleeves, the hood in "hood_colours" (Hood, and
    Rim for the edge of its opening), what replaces the leaves and the teeth of hair under the hood."""
    entry = {"kind": "Costume", "colours": {"Main": main, "Collar": shade(main), **hood_colours},
             "numbers": {"STEP": 0.076, "SLEEVE": LONG_SLEEVE},
             "head": head or {"LEAVES": NO_CROWN, "FRINGE": False}}
    return {**entry, **keys}


# The costumes (15 October 2026), colours read on their layers. None of their Genesis wears a value of
# "Head". (Until then: LEFT_OUT.)
CLOTHES.update({
    # A dark hood up and a red bandana with white dots over the mouth and the chin (its 41 Genesis have
    # no value of "Mouth" in the metadata).
    "Gangster suit": costume((0.23, 0.23, 0.36), {"Hood": (0.25, 0.25, 0.38), "Rim": (0.32, 0.30, 0.40),
                                                  "Bandana": (0.88, 0.07, 0.07), "Dot": (0.96, 0.91, 0.86)},
                             head={"LEAVES": NO_CROWN, "FRINGE": False, "HIDES": ("Mouth",)}, bandana=True,
                             hood_numbers={"SIZE": ((0.0, 1.14), (60.0, 1.24), (180.0, 1.26))}),
    # A blue shark: white belly, white teeth all round the face, a fin on the back of the hood.
    "Shark Suit": costume((0.27, 0.71, 0.92), {"Hood": (0.27, 0.71, 0.92), "Rim": (0.23, 0.55, 0.80),
                                               "Belly": (0.92, 0.95, 0.97), "Tooth": (0.97, 0.97, 0.97)},
                          teeth=True, fin=True),
    # A banana: yellow, its point rising over the head, a green stalk at the tip.
    "Banana suit": costume((0.95, 0.67, 0.06), {"Hood": (0.95, 0.67, 0.06), "Rim": (0.89, 0.53, 0.05),
                                                "Tip": (0.55, 0.72, 0.12)}, peak=True),
    # A red dragon: tan belly and horns, small wings on the back.
    "Dragon suit": costume((0.92, 0.27, 0.17), {"Hood": (0.92, 0.27, 0.17), "Rim": (0.76, 0.19, 0.11),
                                                "Belly": (0.95, 0.70, 0.46), "Horn": (0.95, 0.76, 0.50),
                                                "Wing": (0.66, 0.13, 0.10)}, horns=True, wings=True),
    # The space suit: white, grey stripes down the sleeves, the ring of the helmet around the face. Its
    # glass is not drawn; the pictures show the crown inside it, so the crown stays.
    "Astro Suit": {"kind": "Astro", "colours": {"Main": (0.90, 0.88, 0.84), "Collar": (0.70, 0.70, 0.72),
                                                "Stripe": (0.52, 0.56, 0.69), "Ring": (0.72, 0.73, 0.76)},
                   "numbers": {"STEP": 0.076, "SLEEVE": LONG_SLEEVE},
                   # The ring passes beside the face where the lowest pair of leaves grows: that pair is
                   # removed (the pictures show the spikes of the top inside the helmet).
                   "head": {"LEAVES": (WHOLE, WHOLE, WHOLE, GONE, WHOLE, WHOLE, WHOLE)},
                   "print_missing": "the glass of the helmet and the flag patch"},
    "Duck Float": {"kind": "DuckFloat", "colours": {"Duck": (0.93, 0.68, 0.20), "Beak": (0.92, 0.48, 0.12),
                                                    "Eye": (0.10, 0.07, 0.04)}},
})
for name in ("Gangster suit", "Shark Suit", "Banana suit", "Astro Suit", "Dragon suit", "Duck Float"):
    LEFT_OUT.pop(name, None)

CLOTHES_OF_SIDE = {
    "Left": {
        "Tuxedo": jacket((0.27, 0.25, 0.30), WHITE, (0.86, 0.12, 0.12)),
    },
}
