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
#
# Batch 4 (15 October 2026): an item may also say how wide and thick the leaf is against the leaf of
# SPIKES ("width", fifth). The spikes the pictures draw beside a hat are small points, about as wide as
# they are long: a leaf as wide as on the bare head does not fit under a rim without being turned down
# (the wings of batch 3). And the leaves a Genesis keeps are decided per value of "Head", from its
# pictures (HEAD_VALUES below), no longer per hat shape: the "LEAVES" of a shape is only the default.
# ---------------------------------------------------------------------------------------------

def leaf(share=1.0, turn=None, back=None, bend=0.0, width=1.0):
    """What becomes of one line of SPIKES under a hat: see above."""
    return (share, turn, back, bend, width)


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
    "SIDES": 24,
    # (22 October 2026, the owner: "not tall enough and does not match". On the 67 pictures the fur crown stands high above the head,
    # a tall cylinder of fur whose lower edge is a thick wavy roll across the brow, with the ear flaps hanging down beside the face
    # to the jaw: fur flaps on Side=Right, dark leather flaps with snaps on Side=Left. Until then a low cap with a thin roll and short
    # flaps laid on the head.)
    # The crown is a tall oval cylinder, as it is worn: its lower edge by degrees from the front (height above the neck in the axes
    # of the head: just over the brow, higher at the sides, low at the back), how far it flares from the head to the wall, the wall
    # (half width, half depth, how far its middle stands behind the middle of the head), the height of the top of the wall, and how
    # high its rounded top rises above that.
    "LOW": ((0.0, 0.292), (60.0, 0.275), (110.0, 0.235), (180.0, 0.170)),
    "FLARE": 0.26,
    "BULGE": 0.06,        # the wall swells a little at its middle, as a fur crown does
    "WALL": (0.350, 0.275, 0.035),
    "TOP": 0.520,
    "DOME": 0.070,
    # The rows of the wall as shares of its height from the lower edge (the first, tucked on the head, the next ones the roll of fur),
    # the shares at which the colour of the fur gives way to that of the crown (the first bands are fur), and the bumps of the fur round
    # the lower edge (how many, how deep against the wall).
    # (22 October 2026 on the first try, 0.0 to 0.20 and 3 bands: the owner saw the roll swallowed from the front, a plain cylinder. Now the roll
    # is taller (0.30 of the wall), stands out of the wall by ROLL and its fur bumps are deeper, so it shows from the front and three quarters.)
    "ROWS": (0.0, 0.06, 0.15, 0.26, 0.36, 0.62, 1.0),
    "FUR_BANDS": 4,
    "ROLL": 0.11,
    "FUR_WAVES": (8, 0.045),
    "WAVE_Z": 0.012,     # the lower edge rises and falls with the bumps of the fur (a fluffy edge)
    # An ear flap: how far out of the middle at its top, at its middle and at its foot (it hangs nearly upright beside the face), how
    # far ahead of the middle of the head, its heights (top, then foot), half width at the top and at the foot, half thickness.
    "FLAP": ((0.325, 0.340, 0.335, 0.325), -0.015, (0.330, 0.195, 0.055, -0.075), (0.098, 0.088, 0.078, 0.055), 0.030),
    # Where a flap is leather (the colour "Snap" is given: Side=Left), a rim of fur behind it: how much wider and longer the rim is,
    # and how far behind the leather it lies.
    "FLAP_RIM": (0.020, 0.024, 0.014),
    "SNAPS": ((0.42, 0.015), (0.74, 0.015)),   # along the flap from its top, radius
    "OUTLINE_THICKNESS": 0.020,
    # The fur hat covers the head down to the brow and over the ears: no leaf comes out of it at the
    # top, and the teeth of hair on the brow are under the fur. The pictures of Side=Right show spikes beside and behind it (Side=Left,
    # framed on the face, none): the lowest pair of the star, behind the flaps, the pair of the back row and the leaf
    # straight back stay, brought out under the fur, which sits low: they point down more than under a cap.
    "FRINGE": False,
    "LEAVES": (GONE, GONE, GONE, leaf(1.3, 128.0, 30.0, 0.45), GONE, leaf(1.3, 130.0, 45.0, 0.45),
               leaf(1.3, None, 120.0, 0.45)),
}


def build_ushanka(maker):
    numbers = maker.numbers(USHANKA)
    part, surface = maker.part(), maker.surface
    sides, outline = numbers.SIDES, numbers.OUTLINE_THICKNESS
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    middle, half = surface.middle, surface.half
    wall_wide, wall_deep, wall_back = numbers.WALL
    waves, depth = numbers.FUR_WAVES

    def head_radii(z):
        reach = math.sqrt(max(1.0 - ((z - middle.z) / half.z) ** 2, 0.0))
        return half.x * reach, half.y * reach

    def point(angle, z, wide, deep, centre_y):
        return Vector((math.cos(angle) * wide, centre_y + math.sin(angle) * deep, z))

    # The crown: rows from the lower edge, tucked on the head, flaring out to the wall, up the wall, then a rounded top.
    loops = []
    for share in numbers.ROWS:
        loop = []
        for angle in angles:
            low = interpolate(numbers.LOW, from_front(angle)) + numbers.WAVE_Z * math.cos(waves * angle) * (1.0 - share)
            z = low + (numbers.TOP - low) * share
            head_wide, head_deep = head_radii(z)
            grow = math.sin(0.5 * math.pi * min(1.0, share / numbers.FLARE))
            barrel = 1.0 + numbers.BULGE * math.sin(math.pi * share) * grow \
                + numbers.ROLL * math.sin(math.pi * min(1.0, share / (numbers.FLARE * 1.45)))
            wide = (head_wide * 1.02 + (wall_wide - head_wide * 1.02) * grow) * barrel
            deep = (head_deep * 1.02 + (wall_deep - head_deep * 1.02) * grow) * barrel
            centre_y = wall_back * grow
            if share < 0.45:
                bump = 1.0 + depth * grow * math.cos(waves * angle)
                wide, deep = wide * bump, deep * bump
            loop.append(point(angle, z, wide, deep, centre_y))
        loops.append(loop)
    for lift in (30.0, 60.0):
        phi = math.radians(lift)
        loops.append([point(angle, numbers.TOP + numbers.DOME * math.sin(phi), wall_wide * math.cos(phi), wall_deep * math.cos(phi),
                            wall_back) for angle in angles])
    loops.append([Vector((0.0, wall_back, numbers.TOP + numbers.DOME))])
    shares = [(0.0,) * sides] + [(1.0,) * sides] * (len(loops) - 2) + [(1.0,)]
    bands, _ = KIT.add_loops(part, loops, part.named("Crown"), outline, shares=shares)
    for band in bands[:numbers.FUR_BANDS]:
        part.paint(band, part.named("Fur"))

    # The flaps, hanging beside the face.
    out_x, ahead, heights, widths, half_thick = numbers.FLAP
    colours = maker.entry["colours"]
    leather = "Snap" in colours
    for side in (1.0, -1.0):
        def flap(wider, longer, behind):
            path, wide, thick = [], [], []
            last = len(heights) - 1
            for index, (z, half_wide) in enumerate(zip(heights, widths)):
                x = side * (out_x[min(index, len(out_x) - 1)] - behind)
                foot = index + 1 if index == last else index
                path.append(Vector((x, middle.y + ahead, z - (longer if index == last else 0.0))))
                rounded = 1.0 if index < last else 0.55
                wide.append(Y * (half_wide + wider) * rounded)
                thick.append(X * side * half_thick)
            return path, wide, thick

        slab(part, *flap(0.0, 0.0, 0.0), part.named("Flap"), outline)
        if leather:
            wider, longer, behind = numbers.FLAP_RIM
            slab(part, *flap(wider, longer, behind), part.named("Fur"), outline)
            for along, radius in numbers.SNAPS:
                index = min(len(heights) - 1, int(round(along * (len(heights) - 1))))
                centre = Vector((side * (out_x[min(index, len(out_x) - 1)] + half_thick + 0.004), middle.y + ahead, heights[index]))
                disc_on(part, surface, centre, X * side, radius, part.named("Snap"))


# ---------------------------------------------------------------------------------------------
# Headband: a band around the brow, under the leaves, a darker line along its lower edge, and,
# for the Japanese one, a red sun in the middle of the front.
# ---------------------------------------------------------------------------------------------
HEADBAND = {
    # The band passes under the spikes of the head, which keeps its whole crown: the crossings of the leaves
    # are kept in parts.json, not refused (16 October 2026).
    "THROUGH": True,
    "SIDES": 18,
    # Polar degrees of its lower and upper edge, by degrees from the front. On the pictures the band
    # lies on the brow just above the eyes, the spikes standing above it.
    # (Until 15 October 2026: 74 to 52 at the front, 66 to 46 at the back, higher on the head. The roots
    # of the upper pairs of leaves then lay under it and every leaf but the top one went through it.)
    # At the front its upper edge covers the dark line at the top of the face (54 degrees); at the sides
    # it is lower, under the roots of the upper pair of leaves.
    # (22 October 2026, the owner: the bands sit too high, the art crosses the brow just above the eyes, the lower edge on the top of the
    # eyes. The face was lowered on 22 October (its top 0.29 above the neck, 0.40 before) and the bands stayed where they were: lower
    # edge 82 degrees at the front, 87 now, upper edge 50, 58 now; 5 degrees lower all round.)
    "LOWER": ((0.0, 87.0), (90.0, 85.0), (180.0, 81.0)),
    "UPPER": ((0.0, 58.0), (35.0, 60.0), (70.0, 66.0), (180.0, 64.0)),
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
# leaving the face open. Its colour is laid by place: "Top" over "Curl" (Bob Hair in batch 3: a red
# top over yellow curls, which the pictures do not draw), or the colours of "sweep" in an arch from the
# right of the character over the top to its left (Rainbow Wig). Bob Hair is now yellow curls and a red
# band tied around them ("BAND", batch 4).
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
    # A band tied around the hair (Bob Hair, batch 4), when the colours give "Band": polar degrees of
    # its lower and upper edge by degrees from the front; how far it stands off the hair (or the head
    # where there is no hair); how many waves its two edges make around the head and how deep they are
    # (degrees); the share of it, from its lower edge, in the colour "Line"; points around the head.
    "BAND": None,
    # Big clumps in place of the curls (batch 6, 15 October 2026, after the owner found the hair unlike the
    # pictures: they draw a few large clumps with a curly outline, not a pile of small balls): one line per
    # clump, degrees from the front (on both sides but 0 and 180), polar degrees, radius, and how far out
    # its middle is (against the head). None: the curls above. LOBE_SHAPE: see add_hair_lobe.
    "LOBES": None,
    "LOBE_SHAPE": (10, 4, 5, 0.10),
    "OUTLINE_THICKNESS": 0.020,
    # Hair, not a hat: it replaces the crown of leaves (the owner, 15 October 2026). (Several pictures of
    # Bob Hair still draw the tip of one spike above the hair, at the far side; it is left out.)
    "LEAVES": NO_CROWN,
    # The curls of the hairline take the place of the teeth of hair the base draws on the brow.
    "FRINGE": False,
}


def add_hair_band(part, surface, numbers, mass_size, rims, lined):
    """The band tied around the hair (BAND of CURLY_WIG): a closed strip whose two edges wave, lying on
    the hair where there is hair and on the brow where the hair stops, a darker line along its lower
    edge."""
    lower, upper, lift, waves, depth, line, sides = numbers.BAND
    hair_sides = numbers.SIDES

    def hair_edge(angle):
        """The polar angle of the edge of the hair at this angle seen from above (the rims of the mass
        are given at its own points: straight lines between them)."""
        place = (angle % (2.0 * math.pi)) / (2.0 * math.pi) * hair_sides
        first = int(place) % hair_sides
        return rims[first] + (rims[(first + 1) % hair_sides] - rims[first]) * (place - int(place))

    def size_at(angle, polar):
        """How far from the middle the band lies: on the hair down to its edge, then down onto the head
        over 8 degrees. (First: half way down at the edge itself; the rim of the hair, as large as the
        hair, came through the band as yellow streaks.)"""
        amount = max(0.0, min(1.0, (hair_edge(angle) + math.radians(8.0) - polar) / math.radians(8.0)))
        return 1.0 + (mass_size(angle) - 1.0) * amount + lift

    rows = [[], [], [], [], []]
    for index in range(sides):
        angle = 2.0 * math.pi * index / sides
        front = from_front(angle)
        wave = 0.5 + 0.5 * math.cos(waves * angle)
        low = math.radians(interpolate(lower, front) + depth * wave)
        high = math.radians(interpolate(upper, front) - depth * (1.0 - wave))
        middle = low + (high - low) * line
        for row, (polar, extra) in enumerate(((low, -lift - 0.04), (low, 0.0), (middle, 0.012), (high, 0.0),
                                              (high, -lift - 0.04))):
            rows[row].append(on_head(surface, angle, polar, size_at(angle, polar) + extra))
    none, whole = (0.0,) * sides, (1.0,) * sides
    bands, _ = KIT.add_loops(part, rows, part.named("Band"), numbers.OUTLINE_THICKNESS,
                             shares=[none, whole, whole, whole, none])
    if lined:
        part.paint(bands[0] + bands[1], part.named("Line"))


def add_hair_lobe(part, centre, axis, radius, material, numbers):
    """One clump of hair (LOBES of CURLY_WIG): a ball whose outline waves, as the pictures draw a curly
    clump, "radius" large, its axis pointing out of the head. LOBE_SHAPE: points around, rings from pole to
    pole, how many waves around it and how deep (against its radius)."""
    sides, stacks, waves, depth = numbers.LOBE_SHAPE
    first, second = base.frame_across(axis, Z if abs(axis.normalized().z) < 0.9 else X)
    axis = axis.normalized()
    loops = [[centre - axis * radius]]
    for ring in range(1, stacks):
        polar = math.pi * ring / stacks
        loop = []
        for index in range(sides):
            around = 2.0 * math.pi * index / sides
            reach = radius * (1.0 + depth * math.cos(waves * around + ring * 0.7) * math.sin(polar))
            loop.append(centre + (-axis * math.cos(polar) + (first * math.cos(around) + second * math.sin(around))
                                  * math.sin(polar)) * reach)
        loops.append(loop)
    loops.append([centre + axis * radius])
    KIT.add_loops(part, loops, material, numbers.OUTLINE_THICKNESS)


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
            if arch < -90.0:
                arch += 360.0                                     # below the right shoulder: the first colour
            share = (205.0 - min(max(arch, -25.0), 205.0)) / 230.0
            return sweep[min(int(share * len(sweep)), len(sweep) - 1)]
        return first

    paint_where(part, [face for band in bands for face in band], colour_at)

    if numbers.BAND:
        add_hair_band(part, surface, numbers, mass_size, rims, "Line" in colours)

    if numbers.LOBES:
        # Big clumps with a curly outline, in place of the small curls (15 October 2026, batch 6).
        for degrees, polar_deg, radius, size in numbers.LOBES:
            for side in ((1.0,) if degrees in (0.0, 180.0) else (1.0, -1.0)):
                angle = math.radians(270.0 + side * degrees)
                polar = math.radians(polar_deg)
                out = head_normal(surface, angle, polar)
                centre = on_head(surface, angle, polar, size)
                add_hair_lobe(part, centre, out, radius, part.named(colour_at(centre)), numbers)
        return

    # The curls, on the mass.
    rows_of_curls = list(numbers.CURL_ROWS)
    if numbers.HAIRLINE:
        rows_of_curls.append(numbers.HAIRLINE)
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
    # (Batch 4: lower at the back, 98 and 106 degrees until then, so that the pair of the back row comes
    # out under it beside the tails, as the pictures draw three spikes down the far side.)
    # (22 October 2026, with the headbands: the face was lowered and the cloth stayed on the old brow, 66 degrees at the front, leaving a strip of
    # forehead and the teeth of hair above the eyes. The art, #51 and #153: the front edge of the cloth lies just above the eyes. 82 now.)
    "RIM": ((0.0, 82.0), (45.0, 82.0), (95.0, 84.0), (135.0, 86.0), (160.0, 94.0), (180.0, 100.0)),
    # (1.06 and 1.085 until 15 October 2026: the teeth of hair the base draws on the brow came through
    # the cloth as small triangles of skin.)
    "SIZE": 1.10,
    "SEAM": (7.0, 1.12),         # polar degrees above the rim, size: the band over the brow
    "ROWS": 2,
    # A tail: degrees from the back to each side, polar degrees where it starts, length, half
    # width, half thickness, how far it leans back.
    "TAIL": (13.0, 98.0, 0.20, 0.034, 0.010, 0.55),   # (104 degrees until batch 4, with the lower rim)
    "OUTLINE_THICKNESS": 0.018,
    # Points of hair in the skin colour along the front, coming out under the cloth onto the brow (asked
    # by the owner on 15 October 2026): how many, degrees from the front to the outermost, polar degrees
    # of their roots (under the cloth) and of their tips, half width (degrees), how far off the head
    # (above the face, 0.012, and under the cloth, about 0.027 there).
    "HAIR_POINTS": None,     # (7, 46.0, 58.0, 79.0, 5.5, 0.020) until 22 October 2026: under the new rim they would show nothing
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
# Spartan helmet (rebuilt in batch 4, 15 October 2026). The pictures of all 40 Genesis that wear it
# (#21, #331, #1903 and the others looked at) draw a full helmet: it covers the head and comes down to
# the jaw on both sides of the face (the cheek guards), a nose guard comes down the middle between the
# eyes, the eyes look out of two openings under its brow, the mouth and the chin show between the
# guards, a light trim runs along every edge, a tall red plume runs from the brow over the top to the
# back, and the spikes of the head come out behind it, on the far side, from the top to the cheek.
# (Batch 3 was a cap whose edge stood far above the eyes, with a slit and a thin crest: "badly made",
# said the owner.)
# ---------------------------------------------------------------------------------------------
SPARTAN = {
    # Where the shell has its points around the head, seen from above: degrees from the front, the same
    # on both sides; finer at the front, where the openings are. The lower edge of the shell at each:
    # polar degrees (0 is the top of the head). Over the eyes it lies just above them (their top is at
    # about 85 degrees); between them it comes down as the nose guard to the middle of the eyes; beside
    # them it drops to the jaw (the cheek guards, from the outer corner of the eyes, 35 degrees, to the
    # ears); behind the ears it rises again, so the back of the head is open, where the leaves come out
    # and stand beside the helmet on the busts. (The cheek guards first went on to 118 degrees: the
    # leaves then hid behind them.)
    "EDGE": ((0.0, 103.0), (5.0, 102.0), (9.0, 96.0), (13.0, 80.0), (20.0, 78.0), (27.0, 79.0), (32.0, 84.0),
             (35.0, 126.0), (40.0, 142.0), (58.0, 148.0), (80.0, 146.0), (92.0, 138.0), (102.0, 112.0),
             (112.0, 84.0), (124.0, 68.0), (140.0, 62.0), (160.0, 58.0), (180.0, 56.0)),
    # It stands clear of everything drawn on the face (up to 0.040 off the head): a thick, round helmet.
    "SIZE": 1.16,
    # The trim along every edge: a band this many degrees wide above the edge, in its own colour.
    "TRIM_DEG": 6.0,
    # The rows of the shell above the trim, up to the top: the share of the polar angle of the edge.
    "ROWS": (0.78, 0.55, 0.32, 0.12),
    # The crest: along the middle of the head, (degrees from straight up, towards the back when positive;
    # height above the shell; half width). Wider and taller than in batch 3, and it ends where the back
    # of the helmet ends.
    "CREST": ((-50.0, 0.060, 0.052), (-36.0, 0.190, 0.074), (-14.0, 0.290, 0.088), (12.0, 0.330, 0.092),
              (28.0, 0.290, 0.088), (40.0, 0.190, 0.078), (48.0, 0.080, 0.060), (53.0, 0.020, 0.046)),
    "CREST_SIDES": 8,
    "OUTLINE_THICKNESS": 0.020,
    # The back of the head is open: the pair of the back row and the leaf straight back stand there,
    # the lowest pair of the star (which would go through a cheek guard) is turned back beside them.
    # Short and narrow, as the points drawn behind the helmet.
    # (They are longer than on a bare head: the helmet stands 0.05 off the head, and the pictures draw them
    # large beside it.)
    # (22 October 2026, the owner: two spikes pointed DOWN at the back like a bug, and the sides did not show as the art draws them: 3 or 4
    # short spikes out of the rear edge of the helmet, pointing out and a little up. The lowest pair was turned to 118 degrees (down) and the back
    # row to 100 (down), 1.6 and 1.3 times as long as a leaf of the crown. Now two pairs out of the rear edge (92 and 80 degrees, each tried against the shell, leaf_crossings), 1.45 times a leaf of the crown, as long as the pictures draw them beside the shell, the points bending up; the leaf straight back is gone, it hung down the nape whatever its lean, the head being turned up.)
    "LEAVES": (GONE, GONE, GONE, leaf(1.45, 92.0, 55.0, 0.55, 0.62), GONE, leaf(1.45, 80.0, 70.0, 0.55, 0.58), GONE),
    # The pictures show no teeth of hair on the brow inside the helmet.
    "FRINGE": False,
    # Where a tip must show from above and from the game camera: the two sides behind the helmet and straight back.
    "SECTORS": (135, -135),
}


def build_spartan(maker):
    numbers = maker.numbers(SPARTAN)
    part, surface = maker.part(), maker.surface
    outline = numbers.OUTLINE_THICKNESS
    # The points around the head: the degrees of EDGE on both sides (0 to 180, then on round to 360), as
    # angles seen from above.
    degrees = [value for value, _ in numbers.EDGE] + [360.0 - value for value, _ in reversed(numbers.EDGE[1:-1])]
    edge = {value: polar for value, polar in numbers.EDGE}
    angles = [math.radians(270.0 + value) for value in degrees]
    rims = [math.radians(edge[value if value <= 180.0 else 360.0 - value]) for value in degrees]
    count = len(angles)
    # The shell: tucked inside the head under the edge, the edge, the line of trim, the rows, the top.
    loops = [[on_head(surface, angle, polar, 0.92) for angle, polar in zip(angles, rims)],
             [on_head(surface, angle, polar, numbers.SIZE) for angle, polar in zip(angles, rims)],
             [on_head(surface, angle, polar - math.radians(numbers.TRIM_DEG), numbers.SIZE)
              for angle, polar in zip(angles, rims)]]
    for share in numbers.ROWS:
        loops.append([on_head(surface, angle, polar * share, numbers.SIZE) for angle, polar in zip(angles, rims)])
    loops.append([on_head(surface, 0.0, 0.0, numbers.SIZE)])
    shares = [(0.0,) * count] + [(1.0,) * count] * (len(loops) - 2) + [(1.0,)]
    bands, _ = KIT.add_loops(part, loops, part.named("Gold"), outline, shares=shares)
    if "Trim" in maker.entry["colours"]:
        part.paint(bands[1], part.named("Trim"))

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
    # (22 October 2026, the owner on the hero stage: "the red nose looks weird, like a button". The art, 12 of the 31 Genesis (#210, #248,
    # #195, #920...): a round red ball with a dark outline and a white glint, ON the nose of the face, nothing flat around it. Until
    # then the ball stood 0.022 off the face with an outline shell, which showed from the side as a dark crescent and a thin ring, a
    # button's rim. Now the ball is half sunk (its middle 0.004 off the face), no shell: the outline is the dark of a patch laid on the
    # face under it, EDGE wider than the ball, which the face meets round the ball as a clean outline.)
    # (22 October 2026, the owner liked it but wants it "a tiny bit more out": the middle 0.014 off the face, 0.004 before.)
    "OFF": 0.014,
    "RADIUS": 0.052,
    "SIDES": 12,
    "STACKS": 6,
    "GLINT": (38.0, 0.013),
    "EDGE": 0.0075,
    "OUTLINE_THICKNESS": 0.0,
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
    reach = radius + numbers.EDGE
    full.add_skin(part, surface, full.patch_loops(surface, (side, up), reach, reach, 2.0, 0.0, KIT.DRAWN[2], 1, 20),
                  part.named("Edge"))
    degrees, glint = numbers.GLINT
    upward = (Z - out * Z.dot(out)).normalized()
    towards = (out * math.cos(math.radians(degrees)) + upward * math.sin(math.radians(degrees))).normalized()
    disc_on(part, surface, centre + towards * (radius + 0.002), towards, glint, part.named("Glint"), segments=8)


PLUNGER = {
    # The cup: where it stands on the head (degrees from the front, towards the left of the character;
    # polar degrees), how upright it stands (0 square to the head there, 1 straight up), then from its rim
    # on the head up its axis: (height, radius) of each ring. (Polar 22 and square to the head until
    # 16 October 2026: the cup sat on the top of the skull where the upper spikes were, and those spikes
    # were taken out. The pictures put it at the front of the top, the spikes all around it.)
    "AT": (20.0, 38.0),
    "UPRIGHT": 0.45,
    "CUP": ((-0.030, 0.104), (0.006, 0.118), (0.040, 0.116), (0.078, 0.094), (0.104, 0.056), (0.114, 0.0)),
    "LIP": 1,                    # the rings of the cup up to this one are its darker lip
    "SIDES": 12,
    # The stick: its radius, its length, and the way it leans (towards the left of the character, back).
    "FERRULE": (0.026, 0.034),   # the red collar where the stick enters the cup: radius, length
    "STICK": (0.014, 0.30, 2.40, 0.10),
    "OUTLINE_THICKNESS": 0.016,
    # The cup sits at the front of the top, before the crown: every spike stays.
    "LEAVES": FULL_CROWN,
}


def build_plunger(maker):
    numbers = maker.numbers(PLUNGER)
    part, surface = maker.part(), maker.surface
    degrees, polar_deg = numbers.AT
    angle, polar = math.radians(270.0 + degrees), math.radians(polar_deg)
    axis = (head_normal(surface, angle, polar) * (1.0 - numbers.UPRIGHT) + Z * numbers.UPRIGHT).normalized()
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
    # 20 around and 14 ribs until batch 7: with the whole crown through it, a robber in a heavy robe went
    # over 8,000 triangles.
    "SIDES": 14,
    # The panel over the lower face and the bridge between the eyes, in the coordinates of the face (to
    # the side, above its middle): its edges, how far off the head.
    "LOWER": (-0.26, 0.26, -0.27, -0.072),
    "BRIDGE": (-0.040, 0.040, -0.080, 0.090),
    "PANEL_LIFT": 0.030,
    "PANEL_STEPS": (8, 3),
    # The eye holes: middle (to the side, above), half width, half height, the radius of the rolled edge.
    "HOLE": (0.114, 0.004, 0.072, 0.084, 0.016),
    "HOLE_STATIONS": 12,
    # The knit: darker ribs from the edge up over the top: how many, their half width.
    "RIBS": (10, 0.006),
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
    # Its 0 carries a short upright bar, as drawn ("zero_bar", batch 4: the round 0 read "1OK").
    "10k TPS Cap": {**cap((0.93, 0.92, 0.92), (0.40, 0.40, 0.40), ("10K", "TPS"), (0.42, 0.22, 0.80)), "box": True,
                    "zero_bar": True,
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
        "colours": {"Crown": (0.78, 0.56, 0.41), "Fur": (0.94, 0.83, 0.70), "Flap": (0.86, 0.70, 0.57)},
    },
    "Red Headband": {
        "shape": "Headband",
        "colours": {"Band": (0.91, 0.22, 0.20), "Line": (0.68, 0.12, 0.13)},
    },
    "Japan Headband": {
        "shape": "Headband",
        "colours": {"Band": (0.96, 0.96, 0.96), "Line": (0.74, 0.74, 0.77), "Sun": (0.91, 0.19, 0.18)},
    },
    # Yellow curls over the top and in big puffs beside the face, and a red band tied around the hair
    # over the brow, both its edges waving (batch 4: batch 3 drew a red top over the curls; "it is hair
    # with a headband", said the owner). The 45 Genesis that wear it, all looked at, draw the same yellow
    # and the same red whatever their skin. The band is above the eyes, so a visor stays under it.
    "Bob Hair": {
        "shape": "CurlyWig",
        "colours": {"Curl": (0.95, 0.74, 0.10), "Band": (0.92, 0.16, 0.12), "Line": (0.64, 0.07, 0.07)},
        # (First try of batch 4: the band from 53 to 79 degrees over the brow, the hair stopping at 54
        # under it. On the bust a thick red shelf hid the hair above it, where the pictures show a big
        # yellow top over a narrow band.)
        "numbers": {"RIM": ((0.0, 66.0), (30.0, 70.0), (55.0, 112.0), (180.0, 116.0)),
                    "SIZE": ((0.0, 1.18), (40.0, 1.36), (90.0, 1.48), (180.0, 1.44)),
                    # Clumps above the band (the top, over the brow, behind) and below it (the big puffs
                    # beside the face at the height of the cheeks, and behind); none where the band lies,
                    # which would hide it. (Until batch 6: 15 small curls, a pile of balls.)
                    "LOBES": ((0.0, 26.0, 0.17, 1.10), (55.0, 40.0, 0.16, 1.20), (125.0, 42.0, 0.17, 1.20),
                              (180.0, 45.0, 0.17, 1.18), (78.0, 102.0, 0.17, 1.26), (118.0, 106.0, 0.17, 1.28),
                              (180.0, 108.0, 0.17, 1.24)),
                    "HAIRLINE": None,
                    # The band of the pictures is wide, its upper edge a frill of many small waves.
                    "BAND": (((0.0, 81.0), (40.0, 81.0), (90.0, 84.0), (180.0, 86.0)),
                             ((0.0, 60.0), (40.0, 61.0), (90.0, 63.0), (180.0, 65.0)),
                             0.10, 18, 2.5, 0.24, 36)},
    },
    # A big afro in six colours, from the right of the character over the top to its left.
    "Rainbow Wig": {
        "shape": "CurlyWig",
        "colours": {"Red": (0.85, 0.13, 0.12), "Orange": (0.85, 0.45, 0.12), "Yellow": (0.95, 0.88, 0.14),
                    "Green": (0.21, 0.85, 0.09), "Blue": (0.09, 0.50, 0.93), "Purple": (0.58, 0.10, 0.94)},
        "sweep": ("Red", "Orange", "Yellow", "Green", "Blue", "Purple"),
        # Eleven big clumps, one colour each, in an arch from beside one shoulder over the top to the other
        # (red, orange, yellow, green, blue, purple around the face, as drawn),
        # as the pictures draw blocks of colour larger than the face. (Until batch 6: small curls in
        # stripes.)
        "numbers": {"SIZE": ((0.0, 1.20), (35.0, 1.55), (90.0, 1.78), (180.0, 1.72)),
                    "LOBES": ((25.0, 18.0, 0.24, 1.32), (60.0, 48.0, 0.24, 1.45), (95.0, 85.0, 0.24, 1.55),
                              (115.0, 122.0, 0.22, 1.45), (140.0, 50.0, 0.24, 1.40), (180.0, 90.0, 0.24, 1.40))},
    },
    "Durag": {
        "shape": "Durag",
        "colours": {"Cloth": (0.37, 0.34, 0.41), "Seam": (0.24, 0.22, 0.28)},
    },
    "Wizard Hat": {
        "shape": "WizardHat",
        "colours": {"Cloth": (0.08, 0.14, 0.47), "Star": (0.95, 0.68, 0.07)},
    },
    # (Batch 4: the trim is the light yellow line the pictures draw along every edge; it was a darker
    # brown, which did not show.)
    "Spartan Helmet": {
        "shape": "SpartanHelmet",
        "colours": {"Gold": (0.89, 0.62, 0.30), "Trim": (0.98, 0.82, 0.42), "Crest": (0.95, 0.06, 0.06)},
    },
    # Values of "Head" that are not hats (15 October 2026), colours read on their layers.
    "Clown Nose": {
        "shape": "ClownNose",
        "colours": {"Ball": (0.93, 0.07, 0.07), "Glint": (0.98, 0.93, 0.93), "Edge": (0.05, 0.03, 0.04)},
        "unlit": ("Glint", "Edge"),
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
        # The one Genesis of Side=Right (#1324, Skull) wears the white CHOG cap with a pink brim of
        # "Choggie Cap", not the purple MAKE CHOG GREAT AGAIN cap of Side=Left (batch 4).
        "Chog Cap": cap((0.92, 0.90, 0.84), (0.83, 0.25, 0.49), "CHOG", (0.42, 0.22, 0.74), INK_DARK),
    },
}


# ---------------------------------------------------------------------------------------------
# Batch 4 (15 October 2026): what the pictures of each value of "Head" show, value by value and Side
# by Side, and so which leaves of the crown it keeps and how they stand. The owner, after batch 3: "you
# did not do it case by case: some beanies do not let the spikes through at the back". Every value
# below was looked at on the 256-point pictures of five Genesis per Side (all of them when fewer),
# and on the 1000-point pictures of those; the CHOG numbers are in "looked_at". The full notes, with
# what each line builds, are in tools/blender/head-values-from-the-art.md.
#
# What the pictures show, in short:
# - Side=Left is framed on the face and its head is drawn all in the cream of the face, its spikes
#   too (a row of cream points on top of a bare head, #15, #17). Under a cap or a beanie of Side=Left
#   NO spike shows anywhere: not beside the hat, not behind it. So these keep no leaf.
# - Side=Right draws the head in the skin colour behind the cream face: the spikes stand out on the
#   far side of the head (the left of the picture). Under a cap or a bucket hat: two to four SHORT
#   points stacked down the far side from under the rim to the cheek, pointing out sideways, about as
#   wide as long; the uppermost often shows behind the crown of the hat. Never through the hat, never
#   turned down like an ear, never above the hat.
# - The skull and radioactive bases (Skull: BOOB Cap, ETH Cap, Peace Cap, 8 Ball Beanie, Jeet Cap, the
#   Right BTC Cap and Chog Cap) draw no spike at all; those bases are not built (the head is Origin's).
# ---------------------------------------------------------------------------------------------

# Until the row under the rim (15 October 2026, after batch 4), the hats of Side=Right kept or moved pairs
# of the seven lines of the crown (SIDE_POINTS, BIG_SIDE_POINTS, CROWN_POINTS, DURAG_POINTS, USHANKA_POINTS,
# in the history of this file): from behind and from above that made two spikes on each side and none at
# the back. They now take RIM_ROW and its kin (below), planted under the real rim of each hat.
# Through the knitted mask, as drawn ("THROUGH"). Until batch 7 (15 October 2026) four leaves placed by hand
# from the one angle of the pictures (2 or 3 points near the top of the far side), made longer and turned
# out to pass the check of the tips all around: from behind they read as a collar at mid height, unlike the
# bare head the owner compared them with (#17, Plumber). A knitted mask is pulled over the spiky head, so the
# whole crown of a bare head comes out through it, each leaf where it grows; the pictures, which show only
# the far side, draw those of the far side.
ROBBER_POINTS = FULL_CROWN


def seen(looked_at, art, leaves, under_rim=None, **numbers):
    """One line of HEAD_VALUES: the CHOG numbers whose pictures were looked at, what they show, the
    leaves kept (see leaf()), the row of leaves planted under the rim of the hat (UNDER_RIM of
    make_chog_traits.py: {} for its numbers, a dict for some of them replaced, None for none), and other
    numbers of the shape this value replaces."""
    return {"looked_at": tuple(looked_at), "art": art, "numbers": {"LEAVES": leaves, **numbers},
            "under_rim": under_rim}


# The crown of the two headbands (22 October 2026, the owner: "the yellow hair spikes poke through the band at the top"). Until then the whole
# crown stood, its crossings with the band only noted (THROUGH): the upper pair, the middle pair, the lowest pair and the back row all
# go through a band 0.09 to 0.12 tall across the brow. Each line was tried alone against the new band (probe: leaf_crossings): the
# top leaf clears it; the upper pair clears it turned in to 24 degrees from straight up (it was 38); the middle pair cannot (gone); the lowest
# pair clears it brought out below at 130 degrees; the top of the back row cannot at its own lean, leaning 36 degrees; the pair of
# the back row at 125 degrees and 40 back; the leaf straight back leaning to 104. No leaf crosses the band now (THROUGH is off).
BAND_CROWN = (WHOLE, leaf(1.0, 24.0), GONE, leaf(1.2, 130.0, None, 0.35), leaf(1.0, None, 36.0), leaf(1.2, 125.0, 40.0, 0.35),
              leaf(1.2, None, 104.0, 0.35))


# The row under the rim (15 October 2026, after batch 4): every hat of Side=Right that leaves the
# spikes out under it takes it in place of the leaves of SIDE_POINTS and the like below, which kept
# pairs of the seven lines of the crown: from behind and from above, two spikes on each side and none
# at the back, or a collar of spikes at the neck. The same row for all, its length and its span changed
# only where the pictures draw them otherwise.
RIM_ROW = {}
BIG_RIM_ROW = {"LENGTH": 1.10, "WIDTH": 0.90}
# Under a bucket hat the brim goes all around and dips: a row as long as under a cap stays under it, from
# the camera of the game and from behind (seen on batch 5, 15 October 2026), where the pictures draw two
# or three spikes out from under the brim. Longer, so their points pass its edge.
BUCKET_RIM_ROW = {"LENGTH": 1.40, "WIDTH": 0.70, "SWEEP_DEG": 30.0}
# Under the fur of the ushanka, and the helmet: their flaps and cheek guards cover the sides of the head down
# to the jaw, so the row starts behind them, and the check wants it seen behind and three quarters behind.
# Its second row (make_chog_traits.UNDER_RIM, "EXTRA_ROWS") only behind: the flaps hide the sides, and with a
# heavy robe or the duck float the whole row went over 8,000 triangles (#766, #1644, 16 October 2026).
USHANKA_RIM_ROW = {"ANGLES": (94.0, 116.0, 138.0, 160.0, 180.0), "SWEEP_DEG": 18.0, "SECTORS": (135, -135),
                   "EXTRA_ROWS": ({"ANGLES": (127.0, 149.0, 171.0), "BELOW_DEG": 20.0, "DROOP_DEG": 30.0,
                                   "LENGTH": 0.80, "WIDTH": 0.85, "LIFT": 0.12, "SIDES": 4},)}


# What the pictures of Side=Left show around a hat (15 October 2026, after batch 5): they are framed closer,
# and the head behind the face, in the skin colour and spiky as on Side=Right, fills the picture beside
# and behind the hat, often in the colour of the backdrop. Batch 4 read it as the backdrop: no spike.
LEFT_HEAD = ("the head behind the face is the skin colour and spiky, as on Side=Right: framed closer, its "
             "mass fills the picture beside and behind the hat, often the colour of the backdrop (read as "
             "no spike until batch 5)")


# (value of "Head", Side): what its pictures show. The numbers given replace those of the entry of HATS
# for that Side, so the leaves of a value no longer follow its shape: from_the_art() below.
HEAD_VALUES = {
    # Caps and bucket hats of Side=Right.
    ("TISM Cap", "Right"): seen((3, 412, 991, 1467, 1946), "2 to 4 short spikes on the far side, from beside "
                                "the crown down to the cheek, under the rim", NO_CROWN, RIM_ROW),
    ("MMGA Cap", "Right"): seen((5, 211, 678, 1576, 1936), "2 or 3 short spikes on the far side under the "
                                "rim, down to the cheek", NO_CROWN, RIM_ROW),
    ("Horny Cap", "Right"): seen((97, 644, 1033, 1438, 1947), "2 or 3 short spikes stacked on the far side, "
                                 "the first just under the brim", NO_CROWN, RIM_ROW),
    ("Choggie Cap", "Right"): seen((123, 295, 667, 1329, 1922), "2 or 3 short spikes on the far side under "
                                   "the brim", NO_CROWN, RIM_ROW),
    ("Boob Cap", "Right"): seen((114, 633, 1021, 1569, 1939), "2 or 3 short spikes on the far side under the "
                                "brim, down to the cheek", NO_CROWN, RIM_ROW),
    ("Blue Bucket Cap", "Right"): seen((80, 574, 966, 1347, 1961), "2 or 3 short spikes on the far side under "
                                       "the brim", NO_CROWN, BUCKET_RIM_ROW),
    ("Orange Bucket Cap", "Right"): seen((107, 666, 1178, 1711, 1956), "2 or 3 short spikes on the far side "
                                         "under the brim", NO_CROWN, BUCKET_RIM_ROW),
    ("Gray Bucket Cap", "Right"): seen((62, 620, 1003, 1511, 1958), "2 or 3 short spikes on the far side under "
                                       "the brim", NO_CROWN, BUCKET_RIM_ROW),
    ("Balls Cap", "Right"): seen((19, 676, 1195, 1557, 1942), "3 short spikes stacked on the far side from "
                                 "under the brim to the cheek", NO_CROWN, BUCKET_RIM_ROW),
    ("Cloud Cap", "Right"): seen((35, 564, 896, 1321, 1872), "1 or 2 short spikes on the far side under the "
                                 "brim (dark skins: hard to see)", NO_CROWN, BUCKET_RIM_ROW),
    ("Dollar Cap", "Right"): seen((6, 329, 943, 1404, 1888), "2 or 3 short spikes on the far side under the "
                                  "brim", NO_CROWN, BUCKET_RIM_ROW),
    ("Thunder Cap", "Right"): seen((56, 525, 962, 1454, 1964), "2 or 3 short spikes on the far side under the "
                                   "brim; the bolt is on the top front of the crown", NO_CROWN, BUCKET_RIM_ROW),
    ("Star Cap", "Right"): seen((24, 520, 1140, 1542, 1965), "2 short spikes on the far side under the brim",
                                NO_CROWN, BUCKET_RIM_ROW),
    ("Peace hat", "Right"): seen((1, 550, 793, 1546, 1940), "2 or 3 short spikes on the far side under the brim",
                                 NO_CROWN, BUCKET_RIM_ROW),
    ("Red Peace Cap", "Right"): seen((60, 489, 843, 1478, 1762), "2 or 3 short spikes on the far side under the "
                                     "brim", NO_CROWN, BUCKET_RIM_ROW),
    ("Mon Cap", "Right"): seen((8, 461, 792, 1309, 1934), "a purple bucket hat; 1 or 2 short spikes on the far "
                               "side under the brim", NO_CROWN, BUCKET_RIM_ROW),
    # Caps of Side=Left: no spike at all.
    ("Mon Cap", "Left"): seen((361, 864, 1065, 1423, 1761), "a white cap, purple brim; " + LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("BTC Cap", "Left"): seen((162, 492, 702, 1579, 1954), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("MCGA", "Left"): seen((103, 568, 1028, 1472, 1951), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("Chog Cap", "Left"): seen((142, 578, 1129, 1362, 1882), "the MAKE CHOG GREAT AGAIN cap of MCGA; " + LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("Buttcoin Cap", "Left"): seen((203, 516, 970, 1378, 1930), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("MEGA Chog", "Left"): seen((45, 426, 714, 1406, 1891), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("10k TPS Cap", "Left"): seen((26, 575, 980, 1538, 1823), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("JEET Cap", "Left"): seen((63, 563, 1029, 1555, 1963), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("RETARD Cap", "Left"): seen((46, 379, 929, 1505, 1871), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("WAGMI Cap", "Left"): seen((59, 485, 1025, 1528, 1929), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("NEET", "Left"): seen((7, 396, 562, 1022, 1949), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("MCGA Cap", "Left"): seen((36, 528, 677, 1149, 1900), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    # Beanies of Side=Left: the beanie covers the head to the brow, no spike shows, behind it either.
    ("Wizard Beanie", "Left"): seen((70, 565, 969, 1314, 1948), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("Choggie Beanie", "Left"): seen((4, 271, 841, 1422, 1968), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("BTC Beanie", "Left"): seen((2, 517, 1182, 1392, 1604), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("RH Beanie", "Left"): seen((191, 510, 1017, 1602, 1919), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("Mon Beanie", "Left"): seen((71, 765, 1002, 1351, 1801), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    # The other headwear of Side=Right.
    # The crown sits on the spiky head, which keeps its whole crown of spikes around it, as on a bare head; a
    # spike may touch the band (16 October 2026: the owner found the row under the band too sparse, "more
    # like the spikes of the base"; the crossings are kept in parts.json, not refused).
    ("Crown", "Right"): seen((44, 858, 1127, 1358, 1899), "3 or 4 short spikes on the far side, from beside the "
                             "band of the crown down to the cheek; spikes behind its points at the top",
                             FULL_CROWN),
    ("Wizard Hat", "Right"): seen((10, 472, 872, 1495, 1809), "2 or 3 spikes on the far side, from under the hat "
                                  "down to the cheek, larger than under a cap", NO_CROWN, BIG_RIM_ROW),
    ("Durag", "Right"): seen((51, 570, 1001, 1563, 1884), "3 large spikes on the far side, from the height of "
                             "the top of the durag down to the cheek; no point of hair on the brow", NO_CROWN, BIG_RIM_ROW),
    # (22 October 2026, the owner: the crown's spikes must show at the sides and the back below the helmet, as the art draws them, and two
    # pointed DOWN at the back like a bug. Until then a row planted under the real rim of the shell all round the back, pointing down: a collar.
    # Now the leaves of the shape itself: the lowest pair and the back pair brought out beside the helmet, and the leaf straight back.)
    ("Spartan Helmet", "Right"): seen((21, 331, 1081, 1536, 1903), "a full helmet down to the jaw, nose guard, "
                                      "red plume; 3 or 4 spikes out of its rear edge, at the sides and the back, pointing out and a little up",
                                      SPARTAN["LEAVES"]),
    ("Russian Hat", "Right"): seen((39, 322, 830, 1177, 1785), "tan fur hat, fur flaps; 1 or 2 small spikes on "
                                   "the far side, behind the flap, near the top", NO_CROWN, USHANKA_RIM_ROW),
    ("Russian Hat", "Left"): seen((34, 504, 982, 1518, 1876), "cream fur, dark leather flaps with snaps; " + LEFT_HEAD,
        NO_CROWN, USHANKA_RIM_ROW),
    # A headband is tied around the spiky head: the whole crown stays and the band passes under the spikes
    # (16 October 2026, the owner: no reason to leave room for a band; the crossings are kept, not refused).
    ("Red Headband", "Right"): seen((12, 686, 1014, 1790, 1957), "the whole crown above the band and down the far "
                                    "side; a darker line along its lower edge", BAND_CROWN, THROUGH=False),
    ("Japan Headband", "Right"): seen((65, 523, 891, 1435, 1962), "the whole crown above the white band, a red "
                                      "sun on its front", BAND_CROWN, THROUGH=False),
    ("Bob Hair", "Right"): seen((43, 48, 159, 470, 482, 1137, 1380), "yellow curly hair, a red band with waving "
                                "edges tied over the brow; the tip of one spike above the band, far side",
                                NO_CROWN),
    ("Rainbow Wig", "Left"): seen((29, 346, 1276, 1469, 1967), "an afro in blocks of colour behind a face with a "
                                  "curly edge; no spike", NO_CROWN),
    ("Clown Nose", "Right"): seen((18, 406, 920, 1553, 1926), "a red ball on the nose; the whole crown, as on a "
                                  "bare head", FULL_CROWN),
    ("Plumber", "Left"): seen((17, 531, 1325, 1601, 1885), "a red plunger cup at the front of the top; the "
                              "points of the head all around it (drawn in cream, as Side=Left draws a head)",
                              FULL_CROWN),
    ("Robber", "Right"): seen((14, 652, 927, 1692, 1960), "a grey knitted mask over the whole head; 2 or 3 "
                              "spikes come out of it on the far side, near the top", ROBBER_POINTS,
                              THROUGH=True),
    # Skull Genesis (a base not built): skull heads, no spike.
    ("BOOB Cap", "Right"): seen((846, 1079, 1522), "skull heads: no spike", NO_CROWN),
    ("ETH Cap", "Right"): seen((1313, 1889), "skull heads: no spike", NO_CROWN),
    ("Peace Cap", "Right"): seen((1642,), "a skull head: no spike", NO_CROWN),
    ("8 Ball Beanie", "Right"): seen((388,), "a skull head: no spike; an 8 ball on the side of the beanie",
                                     NO_CROWN),
    ("Jeet Cap", "Left"): seen((1221,), LEFT_HEAD,
        NO_CROWN, RIM_ROW),
    ("BTC Cap", "Right"): seen((729, 972, 1372), "skull heads: no spike", NO_CROWN),
    ("Chog Cap", "Right"): seen((1324,), "a skull head under the white CHOG cap of Choggie Cap: no spike",
                                NO_CROWN),
}


def from_the_art(hats, hats_of_side):
    """The entries of "Head" by Side with the numbers of HEAD_VALUES put in: a new table like
    HATS_OF_SIDE, which make_chog_traits.py uses in its place. Each (value, Side) gets its own entry, so
    a value worn on both Sides can keep other leaves on each."""
    by_side = {side: dict(entries) for side, entries in hats_of_side.items()}
    for (value, side), look in HEAD_VALUES.items():
        entry = hats_of_side.get(side, {}).get(value) or hats.get(value)
        if entry is None:
            raise SystemExit(f"CHOG_TRAITS HEAD_VALUES names {value!r}, which has no entry in HATS")
        known = KIT.HAT_NUMBERS[entry["shape"]]
        unknown = set(look["numbers"]) - set(known)
        if unknown:
            raise SystemExit(f"CHOG_TRAITS HEAD_VALUES gives {value!r} numbers its shape has not: {sorted(unknown)}")
        if len(look["numbers"]["LEAVES"]) != len(known["LEAVES"]):
            raise SystemExit(f"CHOG_TRAITS HEAD_VALUES gives {value!r} {len(look['numbers']['LEAVES'])} leaves")
        merged = {**entry, "numbers": {**entry.get("numbers", {}), **look["numbers"]},
                  "from_the_art": {"looked_at": list(look["looked_at"]), "art": look["art"]},
                  "under_rim": look["under_rim"]}
        by_side.setdefault(side, {})[value] = merged
    return by_side


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
# The edge of the opening follows the edge of the face (22 October 2026, make_chog_traits.opening_rim): it
# stands "GAP" off it (the dark line of the face and the roll of the hood), and below the widest point of the
# face the hood closes to the jaw and the neck behind as "RIM_TAIL" says (degrees from the front, polar
# degrees). A "RIM" table of an entry would replace all that. (Typed in by hand until then, for a face 0.25
# wide: (0, 52), (35, 62), (62, 116), (110, 148), (180, 156).)
HOOD = {
    "GAP": 0.022,
    "RIM_TAIL": ((110.0, 148.0), (180.0, 156.0)),
    "SIZE": ((0.0, 1.12), (60.0, 1.20), (180.0, 1.22)),
    "ROWS": 4,
    "SIDES": 18,
    "EDGE": (0.034, 74.0),
    "EDGE_STATIONS": 11,
    "OUTLINE_THICKNESS": 0.020,
}


def hood_numbers_of(maker):
    """The numbers of the hood of an entry, its "RIM" (degrees from the front, polar degrees) following the
    edge of the face unless the entry gives its own."""
    numbers = SimpleNamespace(**{**HOOD, **maker.entry.get("hood_numbers", {})})
    if not hasattr(numbers, "RIM"):
        numbers.RIM = KIT.opening_rim(maker.head, lambda degrees: interpolate(numbers.SIZE, degrees), numbers.GAP,
                                      numbers.RIM_TAIL)
    return numbers


def hood_swell(numbers, degrees, polar_deg):
    """How far a point of the hood is pushed out by the swell of the entry ("SWELL": how far forward, how far up, the spread
    round the head and the middle and spread of the polar angle, in degrees), a smooth lump on the front of the top of the hood
    (the snout of the shark, 22 October 2026: it grows out of the hood, not a disc laid on it). "degrees" is from the front."""
    spec = getattr(numbers, "SWELL", None)
    if not spec:
        return Vector()
    forward, up, spread, middle, tall = spec
    weight = math.exp(-((degrees / spread) ** 2 + ((polar_deg - middle) / tall) ** 2))
    return Vector((0.0, -forward * weight, up * weight))


def build_hood_up(maker):
    """A hood over the head (a part on "Head"), its opening framing the face, a roll along the edge."""
    numbers = hood_numbers_of(maker)
    part, surface = maker.part(on="Head"), maker.surface
    sides, rows = numbers.SIDES, numbers.ROWS
    angles = [2.0 * math.pi * index / sides for index in range(sides)]
    rims = [math.radians(interpolate(numbers.RIM, from_front(angle))) for angle in angles]
    sizes = [interpolate(numbers.SIZE, from_front(angle)) for angle in angles]
    loops = [[on_head(surface, angle, polar, 0.90) for angle, polar in zip(angles, rims)]]
    for row in range(rows):
        share = row / rows
        loops.append([on_head(surface, angle, polar * (1.0 - share), size)
                      + hood_swell(numbers, from_front(angle), math.degrees(polar * (1.0 - share)))
                      for angle, polar, size in zip(angles, rims, sizes)])
    loops.append([on_head(surface, 0.0, 0.0, interpolate(numbers.SIZE, 90.0)) + hood_swell(numbers, 0.0, 0.0)])
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
        path.append(on_head(surface, angle, polar, interpolate(numbers.SIZE, abs(degrees))) - out * radius * 0.4
                    + hood_swell(numbers, abs(degrees), math.degrees(polar)))
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
    # The belly in another colour (shark): half its width, its lower and upper edge.
    # (22 October 2026: 0.085 half wide, a stripe; the pictures, #116 first, draw a broad white belly under the chin.)
    "BELLY": (0.140, 0.130, 0.570),
    # The teeth of the shark (22 October 2026, from #116 and the 25 pictures): white, a dark line round each, in one row on the rim of
    # the opening, the upper ones pointing down and the lower ones pointing up, none across the face (about 8 on top, 6
    # at the bottom on #116). Each is a small blade of its own, made on the rim and pointing into the opening, with an
    # outline shell, not a flat triangle with a dark triangle behind it: those, a ring of fine dark wedges, read as the
    # rays of a sun (the owner, 22 October). How many on top and at the bottom, how long each (top, bottom), half width
    # at the foot, half thickness, thickness of the outline, how far off the head the foot stands, how far out of the
    # rim of the face the foot starts (against its size).
    # (22 October 2026, the owner on the hero stage: the upper teeth stuck out sideways past the hood like a ruff, the lower ones made a
    # necklace on the chest. Now: how many on top and at the bottom, how far each reaches into the opening (polar degrees, the same
    # for all), the span each side of the middle of the face (degrees), half width at the foot, half thickness, outline, how far
    # off the face the tip stands.)
    "TEETH": (8, 6, 17.0, 42.0, 0.032, 0.011, 0.007, 0.014),
    # The fin on top of the head (the main read from above): where its foot starts and ends along the top of the hood
    # (polar degrees from straight up, towards the back), its height, half thickness at the foot, how far its tip is
    # swept back. Not drawn by any picture: no picture shows the top of the hood. (A thin thorn 0.105 high until 22
    # October 2026.)
    "FIN": (6.0, 100.0, 0.170, 0.040, 0.075),
    # The snout of the shark: the hood IS the shark's head, so its front comes forward over the face. It is a smooth swell of the
    # hood itself (hood_numbers "SWELL" of the entry: how far forward, how far up, spread round the head, middle and spread of the
    # polar angle). (22 October 2026, first a separate rounded loft laid on the hood: the owner saw a flat beret on top of it.)
    # The fin on the back of the body, under the hood (a choice, with the tail fin): points of its edge (how far behind the
    # surface of the back, height), half thickness.
    "BACK_FIN": (((-0.020, 0.470), (0.095, 0.420), (0.050, 0.300), (-0.020, 0.200)), 0.020),
    # The tail fin, behind the body: from the back of the torso (how far back, height) the points of its edge, half thickness.
    "TAIL_FIN": (((0.000, 0.250), (0.060, 0.330), (0.200, 0.470), (0.140, 0.300), (0.215, 0.120), (0.080, 0.200),
                  (0.000, 0.150)), 0.020),
    # The pectoral fin on each side of the body: the pictures draw it low at the side of the body, dark blue, swept
    # back and down (#116, lower right of the body), nothing on the arms. Its root (height, how far back), its reach
    # out, back and down, its half thickness. (22 October 2026: at the height of the arms, 0.46, where the owner read it
    # as wings; now at the hip, behind the arm.)
    "PECTORAL": (0.30, 0.050, (0.18, 0.13, 0.12), 0.016),
    # The eyes on the sides of the hood (a choice: no picture shows them, the owner agreed they may help the hood read as a
    # shark): degrees round from the front, polar degrees, radius.
    "EYES": (64.0, 66.0, 0.030),
    # The gills: three short curved lines behind each eye: degrees round from the front, polar degrees from where to where,
    # half width of a line, how far apart the lines are (degrees round).
    "GILLS": (108.0, 52.0, 78.0, 0.0065, 5.5),
    # The bandana over the lower face: in the coordinates of the face, its upper corners (to the side,
    # above), its point below the chin, how far off the head, its dots.
    # Its lift runs from its top edge to its point: close to the face under the eyes, over the band of the hood
    # that closes under the chin (CHIN, 1.10 times the head) at its point (0.034 there let that band show through
    # it from below). It was 0.032 all over until 18 October 2026, 0.020 off the face; the face is now 0.0038 off
    # the head, and the cloth, a grid 0.13 wide, sags up to 0.008 between its points.
    "BANDANA": (0.27, -0.040, -0.300, (0.016, 0.056)),
    "BANDANA_DOTS": ((-0.12, -0.085), (0.0, -0.090), (0.12, -0.085), (-0.06, -0.150), (0.06, -0.150),
                     (0.0, -0.210), (-0.17, -0.060), (0.17, -0.060)),
    # 16 October 2026, the owner: a costume covers the whole body. Trousers in the colour of the suit over
    # each leg, from the ankle up into the suit (how far they stand off the leg); boots over the feet of a
    # whole costume (build_mascot: how much larger than the foot).
    "TROUSERS": 0.016,
    "BOOTS": 1.10,
    # The seat of the trousers between the legs: its height, half its height, half width, half depth.
    "SEAT": (0.200, 0.070, 0.175, 0.135),
    # Under the chin, the hood closes on the face: a band from cheek to cheek (degrees each side of the
    # front, polar degrees of its upper and lower edge, how much larger than the head).
    "CHIN": (62.0, 152.0, 176.0, 1.10),     # (128 at first: it hid the mouth)
    "OUTLINE_THICKNESS": 0.014,
}


def add_trousers(maker, part, material, numbers, sides=12):
    """A leg of trousers over each leg, from the ankle up into the suit."""
    body = maker.body
    for side in (1.0, -1.0):
        hip = Vector((side * body.LEG_HALF_SPACING, 0.0, body.LEG_TOP))
        ankle = Vector((side * (body.LEG_HALF_SPACING + body.LEG_STANCE), 0.0, 0.0))
        on_leg = lambda height: ankle + (hip - ankle) * (height / body.LEG_TOP)
        across = X * (body.LEG_HALF_WIDTH + numbers.TROUSERS)
        deep = Y * (body.LEG_HALF_DEPTH + numbers.TROUSERS)
        part.shell([(on_leg(0.03), across, deep), (on_leg(body.LEG_ENTERS_TORSO), across, deep),
                    (on_leg(body.LEG_TOP), across * 0.9, deep * 0.9)], sides, material, numbers.OUTLINE_THICKNESS)
    # The seat, between the legs and under the suit, so no skin shows there.
    height, half_tall, half_wide, half_deep = numbers.SEAT
    part.shell(base.ball_rings(Z * height, Z, X, half_tall, half_wide, half_deep, 5), 12, material, 0.0)


def add_boots(maker, part, material, larger, sides=12):
    """A shell over each foot, the foot of the body made "larger", in the colour of the costume."""
    from mathutils import Matrix
    body = maker.body
    for side in (1.0, -1.0):
        ankle = Vector((side * (body.LEG_HALF_SPACING + body.LEG_STANCE), 0.0, 0.0))
        turn = Matrix.Rotation(math.radians(body.FOOT_TOE_OUT_DEG) * side, 3, "Z")
        across, ahead = turn @ X, turn @ -Y
        heel = ankle - ahead * body.FOOT_HEEL_BACK * larger
        height, length, width = body.FOOT_HEIGHT * larger, body.FOOT_LENGTH * larger, body.FOOT_WIDTH * larger
        rings = [(heel + Z * height * 0.26, Vector(), Vector())]
        for along, wide, tall in body.FOOT_RINGS:
            top = height * tall
            rings.append((heel + ahead * length * along + Z * top * 0.42, across * width * 0.5 * wide, Z * top * 0.58))
        rings.append((heel + ahead * length + Z * height * 0.30, Vector(), Vector()))
        points, _ = part.shell(rings, sides, material, 0.012)
        for point in points:
            point.co.z = max(point.co.z, 0.0)


def arc_places(head, surface, first, last, count):
    """"count" angles (of face_place) evenly spaced along the edge of the face, as it lies on the head, between the angles
    "first" and "last" (radians): the middle of each of "count" equal parts of the arc."""
    angles = [first + (last - first) * index / 200.0 for index in range(201)]
    points = [surface.front(*KIT.face_place(head, angle, 1.0), 0.0) for angle in angles]
    lengths = [0.0]
    for before, after in zip(points, points[1:]):
        lengths.append(lengths[-1] + (after - before).length)
    chosen = []
    for index in range(count):
        wanted = lengths[-1] * (index + 0.5) / count
        step = next(step for step in range(len(lengths) - 1) if lengths[step + 1] >= wanted)
        share = (wanted - lengths[step]) / max(lengths[step + 1] - lengths[step], 1e-12)
        chosen.append(angles[step] + (angles[step + 1] - angles[step]) * share)
    return chosen


def add_shark_teeth(maker, part, numbers, hood_numbers):
    """The teeth of the shark: two neat rows of white blades ON the rim of the opening, all of one size, evenly spaced, pointing
    into it: the upper ones on the rim of the hood over the face (pointing down), the lower ones on the upper edge of the band
    that closes the hood under the chin (pointing up). Each blade has an outline shell. Their feet are on the rim of the
    hood (in its roll), their tips on the face, so none comes out past the silhouette of the hood (see COSTUME["TEETH"])."""
    surface = maker.surface
    up_count, down_count, reach_deg, spread_deg, half_wide, half_thick, outline, tip_lift = numbers.TEETH
    chin_around, chin_upper, _, chin_size = numbers.CHIN

    def place(degrees, polar, size, lift=0.0):
        angle = math.radians(270.0 + degrees)
        return on_head(surface, angle, math.radians(polar), size) + head_normal(surface, angle, math.radians(polar)) * lift \
            + hood_swell(hood_numbers, abs(degrees), polar)

    for count, lower in ((up_count, False), (down_count, True)):
        for index in range(count):
            degrees = -spread_deg + 2.0 * spread_deg * (index + 0.5) / count
            if lower:
                polar = chin_upper
                foot = place(degrees, polar, chin_size, -0.012)
                tip_polar = polar - reach_deg
            else:
                polar = interpolate(hood_numbers.RIM, abs(degrees))
                foot = place(degrees, polar, interpolate(hood_numbers.SIZE, abs(degrees)), -0.020)
                tip_polar = polar + reach_deg
            tip = place(degrees, tip_polar, 1.0, tip_lift)
            middle = (foot + tip) * 0.5
            along = (place(degrees + 0.5, polar, 1.0) - place(degrees - 0.5, polar, 1.0)).normalized()
            out = head_normal(surface, math.radians(270.0 + degrees), math.radians(polar))
            part.shell([(foot, along * half_wide, out * half_thick), (middle, along * half_wide * 0.62, out * half_thick * 0.8),
                        (tip, Vector(), Vector())], 4, part.named("Tooth"), outline)


def add_pectoral_fins(maker, numbers):
    """A flat dark fin on each side of the body, low, swept back and down: a triangle from the side of the hip."""
    torso, body = maker.parts[0][0], maker.body
    height, back, (out, behind, down), thick = numbers.PECTORAL
    for sign in (1.0, -1.0):
        half_width, half_depth = KIT.torso_section(body, height)
        root = Vector((sign * half_width * 0.90, back, height))
        front, rear = root + Vector((0.0, -0.045, 0.035)), root + Vector((0.0, 0.075, -0.030))
        tip = root + Vector((sign * out, behind, -down))
        edge = [front, tip + Vector((0.0, -0.030, 0.025)), tip, rear]
        normal = (edge[1] - edge[0]).cross(edge[3] - edge[0]).normalized()
        if normal.z < 0.0:
            normal = -normal
        KIT.add_loops(torso, [[point - normal * thick for point in edge], [point + normal * thick for point in edge]],
                      torso.named("Fin"), numbers.OUTLINE_THICKNESS)


def add_tail_fin(maker, numbers):
    """The tail fin of the shark: a flat crescent standing behind the lower back."""
    torso = maker.parts[0][0]
    points, thick = numbers.TAIL_FIN
    _, depth = KIT.torso_section(maker.body, points[0][1])
    edge = [Vector((0.0, depth - 0.02 + behind, height)) for behind, height in points]
    KIT.add_loops(torso, [[point - X * thick for point in edge], [point + X * thick for point in edge]],
                  torso.named("Fin"), numbers.OUTLINE_THICKNESS)


def add_back_fin(maker, numbers):
    """The small fin on the back of the body, under the hood."""
    torso = maker.parts[0][0]
    points, thick = numbers.BACK_FIN
    _, depth = KIT.torso_section(maker.body, 0.35)
    edge = [Vector((0.0, depth + behind, height)) for behind, height in points]
    KIT.add_loops(torso, [[point - X * thick for point in edge], [point + X * thick for point in edge]],
                  torso.named("Fin"), numbers.OUTLINE_THICKNESS)


def add_dorsal_fin(maker, hood, hood_numbers, numbers):
    """The fin on top of the hood: a swept triangle standing on the middle line of the hood, from near the top to the back."""
    surface = maker.surface
    start, stop, height, half_thick, sweep = numbers.FIN
    angle = math.radians(90.0)
    size = interpolate(hood_numbers.SIZE, 150.0)

    def foot(degrees):
        return on_head(surface, angle, math.radians(degrees), size)

    def up(degrees):
        return head_normal(surface, angle, math.radians(degrees))

    middle = start + (stop - start) * 0.45
    edge = [foot(start) - up(start) * 0.03, foot(start + (stop - start) * 0.18) + up(start) * height * 0.45,
            foot(middle) + up(middle) * height + Y * sweep, foot(stop - (stop - start) * 0.28) + up(stop) * height * 0.38,
            foot(stop) - up(stop) * 0.03]
    KIT.add_loops(hood, [[point - X * half_thick for point in edge], [point + X * half_thick for point in edge]],
                  hood.named("Fin"), numbers.OUTLINE_THICKNESS)


def add_shark_head(maker, hood, hood_numbers, numbers):
    """The eyes and the gills on each side of the hood."""
    surface = maker.surface
    degrees, polar, radius = numbers.EYES
    size = interpolate(hood_numbers.SIZE, degrees)
    for sign in (1.0, -1.0):
        angle = math.radians(270.0 + sign * degrees)
        place = on_head(surface, angle, math.radians(polar), size)
        out = head_normal(surface, angle, math.radians(polar))
        disc_on(hood, surface, place + out * 0.004, out, radius, hood.named("Eye"), segments=10)
        # A white glint, so the eye is an eye and not a hole.
        glint = place + out * 0.0065 + (Z - out * Z.dot(out)).normalized() * radius * 0.35 + X * sign * 0.0
        disc_on(hood, surface, glint, out, radius * 0.30, hood.named("Tooth"), segments=6)
    around, low, high, wide, apart = numbers.GILLS
    size = interpolate(hood_numbers.SIZE, around)
    for sign in (1.0, -1.0):
        for line in (-1, 0, 1):
            angle = math.radians(270.0 + sign * (around + line * apart))
            rows = [[], []]
            for share in (0.0, 0.5, 1.0):
                polar = math.radians(low + (high - low) * share)
                point = on_head(surface, angle, polar, size) + head_normal(surface, angle, polar) * 0.004
                along = (on_head(surface, angle + 0.01, polar, 1.0) - on_head(surface, angle - 0.01, polar, 1.0)).normalized()
                rows[0].append(point - along * wide)
                rows[1].append(point + along * wide)
            full.add_skin(hood, surface, rows, hood.named("Gill"), around=False)


def build_costume(maker):
    """A costume with a hood up: the suit ("Track" with long sleeves), its belly, the hood over the head,
    and what the entry adds ("teeth", "fin", "bandana"). The banana and the dragon are whole costumes since
    16 October 2026 (build_mascot)."""
    entry = maker.entry
    numbers = SimpleNamespace(**COSTUME)
    build_track(maker)
    torso = maker.parts[0][0]
    colours = entry["colours"]
    add_trousers(maker, torso, torso.named("Main"), numbers)
    if "Belly" in colours:
        half, low, high = numbers.BELLY
        rows = []
        for index in range(5):
            height = low + (high - low) * index / 4
            wide = half * math.sin(math.pi * (0.15 + 0.7 * index / 4))
            rows.append([shirt_front(maker, height, side, 0.010) for side in (-wide, -wide * 0.33, wide * 0.33, wide)])
        full.add_skin(torso, SimpleNamespace(middle=Vector((0.0, 0.0, (low + high) * 0.5))), rows, torso.named("Belly"),
                      around=False)
    build_hood_up(maker)
    hood = maker.parts[-1][0]
    surface = maker.surface
    hood_numbers = hood_numbers_of(maker)
    around, upper, lower, larger = numbers.CHIN
    rows = []
    for polar_deg in (upper, (upper + lower) * 0.5, lower):
        rows.append([on_head(surface, math.radians(270.0 + degrees), math.radians(polar_deg), larger)
                     for degrees in [-around + 2.0 * around * index / 8 for index in range(9)]])
    full.add_skin(hood, surface, rows, hood.named("Hood"), around=False)
    if entry.get("teeth"):
        add_shark_teeth(maker, hood, numbers, hood_numbers)
        add_pectoral_fins(maker, numbers)
        add_tail_fin(maker, numbers)
        add_back_fin(maker, numbers)
        add_shark_head(maker, hood, hood_numbers, numbers)
    if entry.get("fin"):
        add_dorsal_fin(maker, hood, hood_numbers, numbers)
    if entry.get("bandana"):
        # A triangle of cloth over the lower face, from below the eyes to a point under the chin, with
        # white dots: it covers the mouth ("HIDES" in "head").
        half, top, point, (lift_top, lift_point) = numbers.BANDANA

        def lift_at(height):
            share = min(1.0, max(0.0, (height - top) / (point - top)))
            return lift_top + (lift_point - lift_top) * share

        rows = []
        for index in range(5):
            share = index / 4
            height = top + (point - top) * share
            wide = half * (1.0 - share) + 0.012
            rows.append([surface.front(side, height, lift_at(height)) for side in (-wide, -wide * 0.5, 0.0, wide * 0.5, wide)])
        full.add_skin(hood, surface, rows, hood.named("Bandana"), around=False)
        for side, height in numbers.BANDANA_DOTS:
            full.add_skin(hood, surface, full.patch_loops(surface, (side, height), 0.010, 0.010, 2.0, 0.0, lift_at(height) + 0.0015,
                                                          1, 6), hood.named("Dot"))


# ---------------------------------------------------------------------------------------------
# Whole costumes (16 October 2026, the owner after batch 10: the costume must be whole, as big on the body
# as on the head: "the whole Chog is inside a banana", "the suit is a big dragon body"). One volume on "Body"
# holds the character from above its feet to over its head, as wide round the body as round the head. The
# face looks out of it through an opening framed by a collar on "Head", which follows the face when the
# head turns (see below); the mittens come out of its sides and the feet from under it.
# ---------------------------------------------------------------------------------------------
MASCOT = {
    # The volume, ring by ring from the bottom up: height, half width, how far its middle stands behind
    # (+) the axis of the body, half depth. The head is 0.60 wide (its middle 0.81 high, its top 1.06), the
    # torso 0.31: the volume is about 0.80 wide from the hips to the brow, its front close to the face.
    # (First: 0.80 wide round the face, the face small in it; the pictures draw the banana a third wider
    # than the face. Its end now rises over the head and bends back.)
    "BANANA": ((0.10, 0.20, 0.00, 0.17), (0.20, 0.26, -0.01, 0.22), (0.35, 0.31, -0.01, 0.26),
               (0.50, 0.34, 0.00, 0.28), (0.65, 0.36, 0.02, 0.29), (0.80, 0.37, 0.04, 0.29),
               (0.95, 0.35, 0.05, 0.28), (1.06, 0.30, 0.07, 0.24), (1.15, 0.22, 0.10, 0.17),
               (1.23, 0.14, 0.14, 0.11), (1.29, 0.075, 0.19, 0.06), (1.32, 0.045, 0.22, 0.04)),
    "DRAGON": ((0.10, 0.22, 0.00, 0.20), (0.20, 0.31, -0.01, 0.27), (0.35, 0.37, -0.01, 0.31),
               (0.50, 0.39, 0.00, 0.32), (0.65, 0.40, 0.02, 0.31), (0.80, 0.40, 0.04, 0.30),
               (0.95, 0.37, 0.05, 0.28), (1.05, 0.31, 0.06, 0.23), (1.11, 0.20, 0.07, 0.14),
               (1.145, 0.0, 0.07, 0.0)),
    # A banana has five flat sides with their edges rounded off: points across each side (degrees each side
    # of its middle), the edge between two sides being the rest of its 72 degrees. A dragon is round.
    "FACETS": (5, (-30.0, -15.0, 0.0, 15.0, 30.0)),
    "ROUND": 20,
    # The colours of the banana by height, from the top: green over the head, yellow round the face, deeper
    # to orange at the feet, as the pictures run (#37, #154, #187).
    "BANDS": ((1.12, "Green"), (0.70, "Hood"), (0.42, "Mid"), (-1.0, "Main")),
    # The opening: the oval of the face this much of its dark line larger, the points round it; the
    # collar from inside the head ("TUCK" of its size), out of it at "LIP" off the face, then over the
    # volume: degrees further from the middle of the face than the opening, how far off the volume. The
    # volume itself is open where the collar covers it: "CUT" degrees beyond the opening.
    # (OPENING 0.6 until 22 October 2026: the rim stood 0.011 outside the edge of the face, which reaches the side of the
# head now, and a strip of the skin of the head showed in the slit beside it, yellow on #16. -0.3: the rim 0.005 inside
# the edge of the face, the collar over its dark line.)
    "OPENING": (-0.3, 24),
    "TUCK": 0.86,
    "LIP": 0.020,
    "COLLAR": ((4.0, 0.028), (14.0, 0.028), (26.0, 0.006)),
    "CUT": 10.0,
    # The stalk of the banana from its top, bending back: way, length and radius, its dark end.
    "STALK": ((0.0, 0.75, 0.66), 0.090, 0.030, 0.016),
    # The dragon: the light belly (its middle height, half width, half height); then its head (below); the
    # dark "w" of its scales (where: from a point, a way), wings on its back and its tail.
    # The belly: rows of the volume it covers (a ring of the volume, how many points each side of the front it takes), how far
    # it stands off the volume. (A fan 0.22 by 0.19 round a point until 22 October 2026.)
    "BELLY": ((1, 1), (2, 2), (3, 2), (4, 1)),
    "BELLY_LIFT": 0.026,
    # The ridge down the back (not drawn by any picture: no picture shows the back, 22 October 2026): the heights of its
    # plates, from the top of the head down to the tail, their half width at the foot and their length.
    "RIDGE": (1.10, 1.00, 0.88, 0.74, 0.60, 0.46),
    "RIDGE_SIZE": (0.034, 0.075),
    # The head of the dragon (22 October 2026, the owner: "the hood IS a dragon's head, the face looks out of its open mouth,
    # the yellow bits are teeth"). The pictures (#16, #42, #226): the two dark ovals on top of the hood are the NOSTRILS at
    # the tip of a snout, so the upper jaw comes forward over the face; the four tan pointed things at the corners of the
    # opening are its FANGS (two hang down from the upper rim, two point up from the lower rim), not horns; the opening is
    # the mouth. Until then four blunt tan "horns" pointing away from the face, which were these fangs misread.
    # SNOUT: the upper jaw, a rounded shell from inside the head forward over the brow: rings from the back to the tip,
    # (how far ahead of the middle of the head, how high above it, half width, half height); in the axes of the head
    # before it is turned up. Its underside is the upper lip, over the top of the face. The tip ring is a point.
    "SNOUT": ((-0.02, 0.245, 0.27, 0.10), (-0.12, 0.255, 0.265, 0.095), (-0.20, 0.262, 0.245, 0.085),
              (-0.26, 0.268, 0.21, 0.075), (-0.30, 0.272, 0.16, 0.063), (-0.325, 0.272, 0.09, 0.040),
              (-0.335, 0.272, 0.0, 0.0)),
    "SNOUT_SIDES": 12,
    # The fangs: where on the rim of the opening (degrees round the face, 0 its left, 90 its top; the rim has 24 points), which
    # way they point (-1 down from the upper rim, +1 up from the lower), length, radius at the foot, and how much they lean
    # in towards the middle of the face (against the up-down way).
    "FANG_PLACES": ((45.0, -1.0), (135.0, -1.0), (225.0, 1.0), (315.0, 1.0)),
    "FANG": (0.105, 0.042, 0.55),
    # The eyes (22 October 2026, the owner: "a dragon head with no eyes is not a head"): a plush dragon onesie has two round eyes on
    # the hood above and either side of the snout. A half sunk ball with a dark outline and a dark slit of a pupil facing ahead and
    # up, so it reads from the front, from three quarters and from the game camera. (To each side of the middle, how far ahead of
    # the middle of the head, how high above it, radius, the way the pupil faces (to the side, ahead, up), its half height and
    # half width against the radius.)
    "EYES": (0.185, -0.130, 0.330, 0.052, (0.35, -0.75, 0.55), 0.60, 0.30),
    # The nostrils on the top of the snout: to each side, which ring, half length along the snout, half width.
    "NOSTRILS": (0.062, 4, 0.034, 0.020),
    "SCALES": (((0.0, 0.0, 0.81), (-0.40, -0.30, 0.86)), ((0.0, 0.0, 0.81), (0.42, -0.26, 0.87)),
               ((0.0, 0.0, 0.81), (0.70, 0.25, 0.67)), ((0.0, 0.0, 0.81), (-0.72, 0.20, 0.66)),
               ((0.0, 0.0, 0.81), (0.05, 0.75, 0.66)), ((0.0, 0.0, 0.40), (-0.86, -0.42, 0.10)),
               ((0.0, 0.0, 0.40), (0.88, -0.38, -0.05)), ((0.0, 0.0, 0.30), (0.30, 0.95, 0.10)),
               # (22 October 2026: the pictures scatter the dark "w" over the hood and the chest)
               ((0.0, 0.0, 0.81), (-0.30, -0.22, 0.93)), ((0.0, 0.0, 0.81), (0.32, -0.20, 0.93)),
               ((0.0, 0.0, 0.81), (-0.62, 0.05, 0.78)), ((0.0, 0.0, 0.81), (0.64, 0.02, 0.77))),
    "SCALE": (0.050, 0.034, 0.008),
    # The wings: where they leave the back (to the side, height), their way out (to the side, back, up),
    # span, height, thickness.
    # (First: out to the back and up, seen edge on from the camera of the game, two red spikes. A bat wing
    # of the pictures shows its face from the front: it spreads to the side, its face turned ahead and up.)
    # (Then: from the sides of the back, most of each wing inside the volume.)
    "WING_ROOT": (0.06, 0.74),
    "WING_WAY": (1.00, 0.30, 0.40),
    "WING_UP": (0.0, 0.78, 0.58),   # (0.30, 0.95 until 22 October 2026: the wing stood upright, a line seen from the game camera)
    "WING_SIZE": (0.40, 0.30, 0.012),     # (0.46, 0.32 until 22 October 2026: the pictures draw small wings)
    # The tail from low on the back: places behind the back (to the side, behind, height), radii.
    # (Then straight back from the bottom: seen end on from behind, a lump.) It curls round to the left of
    # the character and up, so it shows beside the body from the front and from behind.
    "TAIL": (((0.0, -0.05, 0.22), (0.0, 0.10, 0.16), (0.13, 0.22, 0.15), (0.25, 0.27, 0.22), (0.33, 0.26, 0.34),
              (0.36, 0.22, 0.42)),
             (0.095, 0.082, 0.066, 0.050, 0.034, 0.0)),
    "CLAWS": (3, 0.045, 0.040, 0.016),    # on each boot: how many, apart, length, radius
    # The edge of a wing as a bat's: (how far out, how high) as shares of its span and height: the shoulder, the wrist,
    # three finger tips with a scallop between them, then back to the body (22 October 2026: until then a ragged
    # seven points). RIBS: the points of it the dark ribs run to.
    "WING_EDGE": ((0.0, 0.0), (0.40, 0.95), (1.00, 0.78), (0.80, 0.38), (0.98, 0.02), (0.66, 0.00), (0.72, -0.42),
                  (0.42, -0.14), (0.14, -0.30)),
    "RIBS": (2, 4, 6),                   # the points of the edge of a wing its dark ribs run to
    "ANKLES": (0.02, 0.16),               # the legs of the suit, from and to these heights
    "OUTLINE_THICKNESS": 0.016,
}
# When the head turns (6 degrees running, 8 attacking, ChogBodyAnimator) the collar slides over the volume
# by about 0.05 and in or out of it by 0.02: it covers the cut edge of the volume from 6 degrees inside it
# to 16 beyond it, and stands 0.028 off it there.


def mascot_axes(maker):
    """From the axes of "Head" before the head is turned up to those of "Body", and back."""
    from mathutils import Matrix
    pitch = math.radians(maker.head.HEAD_PITCH_DEG)
    middle = maker.surface.middle
    neck = Z * full.NECK_HEIGHT
    turn, back = Matrix.Rotation(-pitch, 3, "X"), Matrix.Rotation(pitch, 3, "X")
    to_body = lambda point: neck + middle + turn @ (point - middle)
    to_head = lambda point: middle + back @ (point - neck - middle)
    return to_body, to_head, turn


def mascot_rings(maker, shape):
    """The rings of points of the volume, from the bottom up, and its top point when it closes on one."""
    numbers = SimpleNamespace(**MASCOT)
    rows = numbers.BANANA if shape == "banana" else numbers.DRAGON
    if shape == "banana":
        count, across = numbers.FACETS
        directions = []
        for facet in range(count):
            for degrees in across:
                delta = math.radians(degrees)
                directions.append((math.radians(360.0 / count * facet) + delta, 1.0 / math.cos(delta) / 1.07))
    else:
        directions = [(2.0 * math.pi * index / numbers.ROUND, 1.0) for index in range(numbers.ROUND)]
    rings, top = [], None
    for height, half_width, behind, half_depth in rows:
        if half_width <= 0.0:
            top = Vector((0.0, behind, height))
            continue
        rings.append([Vector((math.sin(angle) * reach * half_width, behind - math.cos(angle) * reach * half_depth,
                              height)) for angle, reach in directions])
    return rings, top


def build_mascot(maker):
    """A whole costume (MASCOT): the sleeves of a suit, boots, the volume round the whole character with the
    opening of the face, the collar on the head, and what the shape adds (the stalk of a banana; the belly,
    horns, nostrils, scales, wings, tail and claws of a dragon)."""
    import bmesh
    from mathutils import Matrix
    from mathutils.bvhtree import BVHTree
    entry, surface, head = maker.entry, maker.surface, maker.head
    numbers = SimpleNamespace(**MASCOT)
    shape = entry["shape"]
    build_track(maker)
    torso = maker.parts[0][0]
    # The shirt under the volume is never seen: only its sleeves stay.
    torso.mesh.free()
    torso.mesh, torso.outlined, torso.open_faces = bmesh.new(), [], []
    # Under the volume only the ankles could show, behind the boots: a short leg of the suit over each.
    body = maker.body
    for sign in (1.0, -1.0):
        hip = Vector((sign * body.LEG_HALF_SPACING, 0.0, body.LEG_TOP))
        ankle = Vector((sign * (body.LEG_HALF_SPACING + body.LEG_STANCE), 0.0, 0.0))
        across = X * (body.LEG_HALF_WIDTH + COSTUME["TROUSERS"])
        deep = Y * (body.LEG_HALF_DEPTH + COSTUME["TROUSERS"])
        torso.shell([(ankle + (hip - ankle) * (height / body.LEG_TOP), across, deep) for height in numbers.ANKLES], 8,
                    torso.named("Main"), 0.0)
    add_boots(maker, torso, torso.named("Main"), COSTUME["BOOTS"])
    to_body, to_head, turn = mascot_axes(maker)
    middle_body = to_body(surface.middle)

    # The opening: the oval of the face, as add_face draws it, and the ways from the middle of the head
    # through it; the middle of the face; a frame across the way the face looks.
    share, points = numbers.OPENING
    # (the edge of the face itself, face_place, with the share of its dark line on top of it)
    grown = 1.0 + head.FACE_LINE * share / head.HEAD_HALF_WIDTH
    rim = []
    for index in range(points):
        angle = 2.0 * math.pi * index / points
        rim.append(surface.front(*KIT.face_place(head, angle, grown), 0.0))
    looking = (surface.front(0.0, 0.0, 0.0) - surface.middle).normalized()
    first, second = base.frame_across(looking, X)

    def flat(way):
        """A way from the middle of the head on a flat map round the face: its angle off the face, round it."""
        way = way.normalized()
        off = math.acos(max(-1.0, min(1.0, way.dot(looking))))
        round_it = math.atan2(way.dot(second), way.dot(first))
        return off * math.cos(round_it), off * math.sin(round_it)

    def beyond(point, degrees):
        """The way from the middle of the head through a point of the opening, turned "degrees" further off
        the face."""
        way = (point - surface.middle).normalized()
        return Matrix.Rotation(math.radians(degrees), 3, looking.cross(way).normalized()) @ way

    def inside(polygon, place):
        x, y = place
        crossings = False
        for (x1, y1), (x2, y2) in zip(polygon, polygon[1:] + polygon[:1]):
            if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
                crossings = not crossings
        return crossings

    cut = [flat(beyond(point, numbers.CUT)) for point in rim]

    # The volume, whole, to find its surface from inside; then built without what the collar covers.
    rings, top = mascot_rings(maker, shape)
    whole = bmesh.new()
    whole_rows = [[whole.verts.new(point) for point in ring] for ring in rings]
    count = len(rings[0])
    for lower, upper in zip(whole_rows, whole_rows[1:]):
        for index in range(count):
            after = (index + 1) % count
            whole.faces.new((lower[index], lower[after], upper[after], upper[index]))
    whole.faces.new(list(reversed(whole_rows[0])))
    if top is not None:
        apex = whole.verts.new(top)
        for index in range(count):
            whole.faces.new((whole_rows[-1][index], whole_rows[-1][(index + 1) % count], apex))
    else:
        whole.faces.new(whole_rows[-1])
    whole.normal_update()      # the tree gives the normals of the faces: without this they are all zero
    tree = BVHTree.FromBMesh(whole)

    def on_volume(origin, way, lift):
        """Where a line from inside the volume leaves it, "lift" off it, and the way out there."""
        hit, normal, _, _ = tree.ray_cast(origin, way.normalized(), 5.0)
        if hit is None or normal.length < 0.5:
            raise SystemExit(f"CHOG_TRAITS {maker.prefix}: a line from {tuple(origin)} does not leave the volume")
        if normal.dot(way) < 0.0:
            normal = -normal
        return hit + normal * lift, normal

    mesh = torso.mesh
    rows = [[mesh.verts.new(point) for point in ring] for ring in rings]
    faces, left_open, axes = [], 0, {}
    middles = [sum(ring, Vector()) / len(ring) for ring in rings]
    for (lower, upper), low_middle, high_middle in zip(zip(rows, rows[1:]), middles, middles[1:]):
        for index in range(count):
            after = (index + 1) % count
            corners = (lower[index], lower[after], upper[after], upper[index])
            centre = sum((corner.co for corner in corners), Vector()) / 4.0
            if inside(cut, flat(to_head(centre) - surface.middle)):
                left_open += 1
                continue
            face = mesh.faces.new(corners)
            faces.append(face)
            axes[face] = (low_middle + high_middle) * 0.5
    faces.append(mesh.faces.new(list(reversed(rows[0]))))
    if top is not None:
        apex = mesh.verts.new(top)
        faces += [mesh.faces.new((rows[-1][index], rows[-1][(index + 1) % count], apex)) for index in range(count)]
    else:
        faces.append(mesh.faces.new(rows[-1]))
    for vertex in [vertex for vertex in mesh.verts if not vertex.link_faces]:
        mesh.verts.remove(vertex)
    if not left_open:
        raise SystemExit(f"CHOG_TRAITS {maker.prefix}: the opening of the face cuts nothing out of the volume")
    for face in faces:
        face.smooth = shape != "banana"
        face.normal_update()
        if face in axes and face.normal.dot(face.calc_center_median() - axes[face]) < 0.0:
            raise SystemExit(f"CHOG_TRAITS {maker.prefix}: a face of the volume looks inwards")
    if shape == "banana":
        for face in faces:
            height = face.calc_center_median().z
            torso.paint([face], torso.named(next(name for below, name in numbers.BANDS if height > below)))
    else:
        torso.paint(faces, torso.named("Main"))
    torso.open_faces.extend(faces)
    torso.outlined.append((faces, numbers.OUTLINE_THICKNESS, None))

    # The collar, on the head: inside the head at the opening, out of it just off the face, then over the
    # volume round the opening.
    collar = maker.part(on="Head")
    loops = [[surface.middle + (point - surface.middle) * numbers.TUCK for point in rim],
             [point + head_normal_at(surface, point) * numbers.LIP for point in rim]]
    for degrees, lift in numbers.COLLAR:
        loops.append([to_head(on_volume(middle_body, turn @ beyond(point, degrees), lift)[0]) for point in rim])
    collar_mesh = collar.mesh
    collar_rows = [[collar_mesh.verts.new(point) for point in loop] for loop in loops]
    bands = []
    for lower, upper in zip(collar_rows, collar_rows[1:]):
        band = [collar_mesh.faces.new((lower[index], lower[(index + 1) % points], upper[(index + 1) % points],
                                       upper[index])) for index in range(points)]
        bands.append(band)
    collar_faces = [face for band in bands for face in band]
    for face in collar_faces:
        face.smooth = True
        face.normal_update()
    probe = bands[-1][0]
    if probe.normal.dot(probe.calc_center_median() - surface.middle) < 0.0:
        for face in collar_faces:
            face.normal_flip()
    if shape == "banana":
        for face in collar_faces:
            height = to_body(face.calc_center_median()).z
            collar.paint([face], collar.named(next(name for below, name in numbers.BANDS if height > below)))
    else:
        collar.paint(collar_faces, collar.named("Hood"))
    shares = {}
    for row, share_of_row in zip(collar_rows, (0.0, 1.0, 0.6, 0.3, 0.0)):
        for vertex in row:
            shares[vertex] = share_of_row
    collar.open_faces.extend(collar_faces)
    collar.outlined.append((collar_faces, numbers.OUTLINE_THICKNESS, shares))

    if shape == "banana":
        way, length, radius, end = numbers.STALK
        way = Vector(way).normalized()
        tip_middle = sum(rings[-1], Vector()) / len(rings[-1])
        torso.shell(base.tube_rings(tip_middle - way * 0.02, way, X, ((0.0, radius), (length * 0.6, radius * 0.92),
                                                                       (length, radius * 0.86))),
                    7, torso.named("Stem"), numbers.OUTLINE_THICKNESS * 0.8)
        torso.shell(base.tube_rings(tip_middle + way * (length - 0.004), way, X, ((0.0, radius * 0.80), (end, radius * 0.7))),
                    7, torso.named("Tip"), numbers.OUTLINE_THICKNESS * 0.6)
        return

    # The dragon.
    # The belly: a plate of the pale colour on the front of the volume, cut from its own points (a plate sampled
    # between them sagged under the facets of the volume: two patches with red between, 22 October 2026).
    belly_rows = []
    for ring_index, half in numbers.BELLY:
        ring, axis = rings[ring_index], middles[ring_index]
        row = {}
        for column in range(-half, half + 1):
            point = ring[column % count]
            out = Vector((point.x - axis.x, point.y - axis.y, 0.0)).normalized()
            row[column] = mesh.verts.new(point + out * numbers.BELLY_LIFT)
        belly_rows.append((half, row))
    belly_faces = []
    for (low_half, low), (high_half, high) in zip(belly_rows, belly_rows[1:]):
        for column in range(-2, 2):
            corners = [low[max(-low_half, min(low_half, column))], low[max(-low_half, min(low_half, column + 1))],
                       high[max(-high_half, min(high_half, column + 1))], high[max(-high_half, min(high_half, column))]]
            corners = [corner for index, corner in enumerate(corners) if corner is not corners[index - 1]]
            if len(corners) >= 3:
                belly_faces.append(mesh.faces.new(corners))
    torso.paint(belly_faces, torso.named("Belly"))
    for face in belly_faces:
        face.smooth = True
    torso.open_faces.extend(belly_faces)
    # The ridge down the back, from the top of the head to the tail: small plates, each a four sided point on the
    # surface of the volume, leaning back.
    ridge_wide, ridge_long = numbers.RIDGE_SIZE
    for height in numbers.RIDGE:
        place, out = on_volume(Vector((0.0, 0.05, height)), Y + Z * (0.9 if height > 1.0 else 0.0), 0.0)
        way = (out + Y * 0.45).normalized()
        torso.shell(base.tube_rings(place - out * 0.012, way, X, ((0.0, ridge_wide), (ridge_long, 0.0))), 4,
                    torso.named("Ridge"), numbers.OUTLINE_THICKNESS * 0.6)
    wide, tall, thick = numbers.SCALE
    for origin, aim in numbers.SCALES:
        origin = Vector(origin)
        place, out = on_volume(origin, Vector(aim), 0.0)
        along = out.cross(Z if abs(out.dot(Z)) < 0.9 else Y).normalized()
        up = out.cross(along).normalized()
        if up.dot(Z) < 0.0:
            up = -up
        path = []
        for step_side, step_up in ((-1.0, 1.0), (-0.5, -1.0), (0.0, 0.25), (0.5, -1.0), (1.0, 1.0)):
            target = place + along * wide * step_side + up * tall * step_up * 0.5
            path.append(on_volume(origin, target - origin, 0.006)[0])
        # A flat stroke on the volume (a strip of four quads, 8 triangles; a closed tube of four sides was 40 and the
        # fourteen marks of the pictures did not fit under the limit of 8,000).
        lower, upper = [], []
        for index, point in enumerate(path):
            ahead = path[min(index + 1, 4)] - path[max(index - 1, 0)]
            across = out.cross(ahead).normalized() * thick
            lower.append(point - across)
            upper.append(point + across)
        full.add_skin(torso, SimpleNamespace(middle=origin), [lower, upper], torso.named("Scale"), around=False)
    side_at, height = numbers.WING_ROOT
    to_side, to_back, to_up = numbers.WING_WAY
    span, wing_tall, wing_thick = numbers.WING_SIZE
    for sign in (1.0, -1.0):
        origin = Vector((0.0, 0.0, height))
        root, _ = on_volume(origin, Vector((sign * side_at, 0.40, 0.0)), -0.020)
        out = Vector((sign * to_side, to_back, to_up)).normalized()
        up = Vector(numbers.WING_UP).normalized()
        up = (up - out * up.dot(out)).normalized()
        edge = [root + out * span * across + up * wing_tall * high for across, high in numbers.WING_EDGE]
        normal = out.cross(up).normalized()
        KIT.add_loops(torso, [[point - normal * wing_thick for point in edge], [point + normal * wing_thick for point in edge]],
                      torso.named("Wing"), numbers.OUTLINE_THICKNESS)
        for index in numbers.RIBS:
            reach = edge[index] - root
            rib_way = reach.normalized()
            slab(torso, [root + rib_way * 0.03, root + reach * 0.96], [normal.cross(rib_way).normalized() * 0.007] * 2,
                 [normal * (wing_thick + 0.004)] * 2, torso.named("Rib"), 0.0, sides=4)
    places, radii = numbers.TAIL
    back_point, _ = on_volume(Vector((0.0, 0.0, places[0][2])), Y, 0.0)
    tail_points = [Vector((x, back_point.y + behind, z)) for x, behind, z in places]
    tail_rings = []
    for index, (point, tail_radius) in enumerate(zip(tail_points, radii)):
        ahead = tail_points[min(index + 1, len(tail_points) - 1)] - tail_points[max(index - 1, 0)]
        across_first, across_second = base.frame_across(ahead, X)
        tail_rings.append((point, across_first * tail_radius, across_second * tail_radius) if tail_radius > 0.0
                          else (point, Vector(), Vector()))
    torso.shell(tail_rings, 7, torso.named("Main"), numbers.OUTLINE_THICKNESS)
    # (Four spikes along the tail until 22 October 2026: no picture draws a tail, nor spikes on it; taken out for the
    # triangle budget the four horns, the nostrils and the scales of the pictures needed.)
    claws, apart, claw_length, claw_radius = numbers.CLAWS
    body = maker.body
    for sign in (1.0, -1.0):
        ankle = Vector((sign * (body.LEG_HALF_SPACING + body.LEG_STANCE), 0.0, 0.0))
        toes = Matrix.Rotation(math.radians(body.FOOT_TOE_OUT_DEG) * sign, 3, "Z") @ -Y
        heel = ankle - toes * body.FOOT_HEEL_BACK * COSTUME["BOOTS"]
        front = heel + toes * body.FOOT_LENGTH * COSTUME["BOOTS"] * 0.97 + Z * body.FOOT_HEIGHT * 0.30
        across = toes.cross(Z).normalized()
        for index in range(claws):
            offset = (index - (claws - 1) * 0.5) * apart
            torso.shell(base.tube_rings(front + across * offset - toes * 0.012, toes, Z,
                                        ((0.0, claw_radius), (claw_length, 0.0))), 5, torso.named("Fang"),
                        numbers.OUTLINE_THICKNESS * 0.5)
    add_dragon_head(maker, collar, rim, numbers)


def oval_on(part, middle, centre, out, along, half_long, half_short, material, segments=10):
    """A flat oval laid at "centre" facing "out", its long way along "along" (a way near the surface)."""
    first = (along - out * along.dot(out)).normalized()
    second = out.cross(first).normalized()
    edge = [centre + first * half_long * math.cos(2.0 * math.pi * index / segments)
            + second * half_short * math.sin(2.0 * math.pi * index / segments) for index in range(segments)]
    return full.add_skin(part, SimpleNamespace(middle=middle), [[centre + out * 0.002], edge], material)


def add_snout(part, surface, snout, sides, material, outline):
    """A rounded snout lofted from inside the head forward over the face: "snout" lists rings from the back to the tip (how far
    ahead of the middle of the head, how high above it, half width, half height), in the axes of the head before it is
    turned up. Returns its rings (centre, first axis, second axis)."""
    middle = surface.middle
    rings = [(middle + Vector((0.0, ahead, up)), X * wide if wide else Vector(), Z * tall if tall else Vector())
             for ahead, up, wide, tall in snout]
    part.shell(rings, sides, material, outline)
    return rings


def add_dragon_head(maker, collar, rim, numbers):
    """The head of the dragon on the part of the head (22 October 2026): the snout over the face (the upper jaw, whose
    underside is the upper lip and whose top carries the two nostrils), and the four fangs on the rim of the opening, which
    is the mouth. See MASCOT["SNOUT"]."""
    surface = maker.surface
    middle = surface.middle
    rings = add_snout(collar, surface, numbers.SNOUT, numbers.SNOUT_SIDES, collar.named("Hood"), numbers.OUTLINE_THICKNESS)
    # The nostrils, two dark ovals with a fill of the colour of the hood, as the pictures draw them, near the tip.
    side, ring, half_long, half_short = numbers.NOSTRILS
    centre, first, second = rings[ring]
    wide, tall = first.length, second.length
    for sign in (1.0, -1.0):
        x = sign * side
        z = tall * math.sqrt(max(0.0, 1.0 - (x / wide) ** 2))
        place = centre + Vector((x, 0.0, z))
        out = (Vector((x / wide ** 2, 0.0, z / tall ** 2)).normalized() - Y * 0.35).normalized()
        oval_on(collar, middle, place, out, -Y, half_long, half_short, collar.named("Nostril"))
        oval_on(collar, middle, place + out * 0.0015, out, -Y, half_long * 0.58, half_short * 0.50, collar.named("Hood"))
    # The eyes: a ball half sunk in the hood on each side of the snout, a dark slit on its front.
    side, ahead, up, ball, facing, slit_tall, slit_wide = numbers.EYES
    for sign in (1.0, -1.0):
        centre = middle + Vector((sign * side, ahead, up))
        collar.shell(base.ball_rings(centre, Z, X, ball, ball, ball * 0.95, 3), 8, collar.named("EyeBall"),
                     numbers.OUTLINE_THICKNESS * 0.8)
        way = Vector((sign * facing[0], facing[1], facing[2])).normalized()
        oval_on(collar, middle, centre + way * (ball * 0.97), way, Z, ball * slit_tall, ball * slit_wide, collar.named("Nostril"))
    # The fangs, on the rim of the opening, pointing into it.
    length, radius, lean = numbers.FANG
    face = surface.front(0.0, 0.0, 0.0)
    for degrees, vertical in numbers.FANG_PLACES:
        index = int(round(degrees / 360.0 * len(rim))) % len(rim)
        out = head_normal_at(surface, rim[index])
        root = rim[index] + out * COSTUME_FANG_LIFT
        inward = Vector((face.x - root.x, 0.0, face.z - root.z)).normalized()
        way = (Z * vertical + inward * lean + out * 0.30).normalized()
        collar.shell(base.tube_rings(root - way * 0.02, way, X, ((0.0, radius), (length * 0.55, radius * 0.62),
                                                                (length, 0.0))), 5, collar.named("Fang"),
                     numbers.OUTLINE_THICKNESS * 0.8)


COSTUME_FANG_LIFT = 0.030


def head_normal_at(surface, point):
    """The way out of the egg of the head at a point on it."""
    local = point - surface.middle
    half = surface.half
    return Vector((local.x / half.x ** 2, local.y / half.y ** 2, local.z / half.z ** 2)).normalized()


# The crown under the glass of the astronaut helmet (22 October 2026): each line of SPIKES, shortened so that the point
# of a leaf (with its outline) stays inside the glass (leaf_crossings fails the build otherwise).
ASTRO_CROWN = (GONE, GONE, GONE, GONE, leaf(0.22), leaf(0.22), leaf(0.26))


ASTRO = {
    # The ring of the helmet around the face: how much larger than the head, its radius (its opening, polar
    # degrees by degrees from the front, goes all round the face).
    # (Until 22 October 2026 the ring followed the edge of the face over the brow and the sides, with the crown of
    # leaves coming out of it: a sunflower. Now a round ring across the way the face looks, a shell behind it.)
    "RING_SIZE": 1.12,
    "RING_RADIUS": 0.046,
    "RING_STATIONS": 16,
    # The helmet (22 October 2026, from the 24 pictures, which draw a round glass helmet round the whole head, the
    # crown and the face inside it, and none of the spikes outside): a shell over the back and the top of the head,
    # closed (sides, rows), in a glass material (drawn on a share of the pixels, with no outline of its own: the dark
    # inside of an outline shell would show through the holes). The crown of the head stays inside it, as short
    # leaves lying under the glass (the pictures draw the hair flat against the helmet, as a zigzag edge).
    # (Until the afternoon of 22 October 2026 an opaque shell as large as the ring, 1.10 times the head, with no
    # crown: from the game camera a white egg.)
    # DOME: points round it, rows from the ring up to the top. DOME_SIZE: how many times the head it is at the top
    # (the room the hair needs under it); it starts at the ring and is that large from DOME_FULL polar degrees
    # (from straight behind the face) up.
    "DOME": (20, 5),
    "DOME_SIZE": 1.20,
    "DOME_FULL": 55.0,
    # The ribbed unit at the side of the helmet, on the right of the character (the left of the pictures): where
    # (to the side, behind the middle of the head, height above its middle), then its stations (along, radius).
    "VENT_AT": (-0.318, 0.035, 0.015),
    "VENT": ((0.0, 0.046), (0.020, 0.064), (0.038, 0.050), (0.056, 0.064), (0.074, 0.050), (0.092, 0.064), (0.112, 0.050)),
    # The hose, on the right of the character, one tube from the tip of the ribbed unit into the chest box (HOSE_WAY: points of
    # its way from the tip down to the shoulder, in the axes of the body at rest; HOSE_CHEST: (to the side, height) points of its
    # way over the front of the chest into the box; its radius, stations per segment). The flag patch, on the top of the right
    # sleeve: from how far down the arm to how far, its half width in degrees round the arm; FLAG_LIFT: how far it stands off the arm's axis beyond ARM_RADIUS. The chest box: half
    # width, from and to what height. The pack on the back: half width, half depth, from and to what height.
    # (22 October 2026, phone size: the helmet hides the chest from the game camera, so the hose 0.020 thick and the flag
    # 0.080 by 0.052 that lay on it were not seen at all. The hose is now 0.030 thick, darker, and its first part hangs
    # outside the helmet where the camera sees it; the flag is on the sleeve, 0.085 long and as wide as the arm, between the
    # helmet and the mitten. At the size of the game both are a few pixels at best: the chest is under the head.)
    "HOSE_WAY": ((-0.44, 0.00, 0.70), (-0.37, -0.02, 0.625), (-0.27, -0.05, 0.600)),
    "HOSE_CHEST": ((-0.20, 0.578), (-0.145, 0.545), (-0.100, 0.505), (-0.090, 0.480)),
    "HOSE_RADIUS": 0.028,
    "HOSE_STEPS": 2,
    "FLAG": (0.105, 0.190, 45.0),
    "FLAG_LIFT": 0.012,
    "CHEST_BOX": (0.105, 0.500, 0.390),
    "PACK": (0.125, 0.052, 0.560, 0.360),
    # The collar of the suit around the neck: height, how far it stands out, its radius.
    "COLLAR": (0.585, 0.030, 0.034),
    "OUTLINE_THICKNESS": 0.016,
}


def build_astro(maker):
    """The space suit of the pictures (22 October 2026): a white suit with long sleeves (grey stripes down them), a thick
    collar, the flag patch on the top of the right sleeve, a box on the chest, the ribbed hose from the helmet's unit
    down to the chest, a pack on the back, white legs and grey boots; and a round helmet: an opaque white shell over the whole
    head, a thick ring round the face, a ribbed unit at its right side. The face is open in the ring, as the pictures draw it,
    and through it show the face and the fringe of the Chog's hair; no leaf of the crown comes out of the helmet."""
    numbers = SimpleNamespace(**ASTRO)
    build_track(maker)
    torso, body = maker.parts[0][0], maker.body
    # The legs and the feet of the suit (22 October 2026): until then the bare skin of the Chog (purple legs, pink feet) came out
    # of a white space suit.
    # (10 sides, not 12, and the ring below on 18 stations: the budget of 8,000 triangles.)
    add_trousers(maker, torso, torso.named("Main"), SimpleNamespace(**COSTUME), sides=10)
    add_boots(maker, torso, torso.named("Boot"), COSTUME["BOOTS"], sides=10)
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

    # The flag patch: a blue field and stripes, on the top of the right sleeve of the character (the left of the pictures,
    # where the flag sits at the shoulder). (Until 22 October 2026 flat on the chest under the chin, where the helmet hides
    # it from the game camera; the top of the sleeve is seen between the helmet and the mitten.) The stripes run along the
    # arm, as the field does at its shoulder end: its half length along the arm, its half width in degrees round the arm.
    sleeve, on_arm = maker.parts[2]
    spread, ahead = math.radians(body.ARM_SPREAD_DEG), math.radians(body.ARM_FORWARD_DEG)
    down = Vector((-math.sin(spread), -math.sin(ahead), -math.cos(spread) * math.cos(ahead))).normalized()
    first, second = base.frame_across(down, -Y)
    top_of_arm = second * 1.0
    flag_start, flag_stop, flag_turn = numbers.FLAG
    reach = body.ARM_RADIUS + numbers.FLAG_LIFT
    stripes = 5
    field_stop = flag_start + (flag_stop - flag_start) * 0.42

    def on_sleeve(distance, degrees, lift=0.0):
        way = top_of_arm * math.cos(math.radians(degrees)) + first * math.sin(math.radians(degrees))
        return down * distance + way * (reach + lift)

    for index in range(stripes):
        low = -flag_turn + 2.0 * flag_turn * index / stripes
        high = -flag_turn + 2.0 * flag_turn * (index + 1) / stripes
        rows = [[on_sleeve(distance, degrees) for degrees in (low, high)] for distance in (flag_start, flag_stop)]
        full.add_skin(sleeve, SimpleNamespace(middle=down * flag_start), rows,
                      sleeve.named("FlagRed" if index % 2 == 0 else "FlagWhite"), around=False)
    rows = [[on_sleeve(distance, degrees, 0.0015) for degrees in (-flag_turn, flag_turn * 0.2)]
            for distance in (flag_start, field_stop)]
    full.add_skin(sleeve, SimpleNamespace(middle=down * flag_start), rows, sleeve.named("FlagBlue"), around=False)

    # The box on the chest and the pack on the back.
    box_wide, box_top, box_foot = numbers.CHEST_BOX
    middle_height = (box_top + box_foot) * 0.5
    _, depth = KIT.torso_section(body, middle_height)
    slab(torso, [Vector((0.0, -depth - 0.012, box_top)), Vector((0.0, -depth - 0.012, box_foot))],
         [X * box_wide] * 2, [Y * 0.016] * 2, torso.named("Panel"), numbers.OUTLINE_THICKNESS, sides=4)
    pack_wide, pack_deep, pack_top, pack_foot = numbers.PACK
    _, depth = KIT.torso_section(body, (pack_top + pack_foot) * 0.5)
    slab(torso, [Vector((0.0, depth + pack_deep * 0.8, pack_top)), Vector((0.0, depth + pack_deep * 0.8, pack_foot))],
         [X * pack_wide] * 2, [Y * pack_deep] * 2, torso.named("Ring"), numbers.OUTLINE_THICKNESS, sides=4)

    # The helmet: a round ring round the face (an ellipse a little larger than the head, in the plane across the way the
    # face looks), the shell over the whole back half of the head behind it, the ribbed unit at the right side.
    ring = maker.part(on="Head")
    surface = maker.surface
    middle, half = surface.middle, surface.half
    looking = (surface.front(0.0, 0.0, 0.0) - middle).normalized()
    up_way = (Z - looking * Z.dot(looking)).normalized()
    side_way = (X - looking * X.dot(looking) - up_way * X.dot(up_way)).normalized()
    size = numbers.RING_SIZE

    def on_shell(psi, turn, scale):
        """A point of the shell larger than the head by "scale": "psi" degrees from the way straight behind the face,
        "turn" round it."""
        way = (-looking * math.cos(math.radians(psi))
               + (side_way * math.cos(turn) + up_way * math.sin(turn)) * math.sin(math.radians(psi)))
        return middle + Vector((half.x * scale * way.x, half.y * scale * way.y, half.z * scale * way.z))

    stations = numbers.RING_STATIONS
    turns = [2.0 * math.pi * index / stations for index in range(stations + 1)]
    path = [on_shell(90.0, turn, size) for turn in turns]
    wide, thick = [], []
    for index, point in enumerate(path):
        radial = (point - middle - looking * (point - middle).dot(looking)).normalized()
        wide.append(radial * numbers.RING_RADIUS)
        thick.append(looking * numbers.RING_RADIUS)
    slab(ring, path, wide, thick, ring.named("Ring"), numbers.OUTLINE_THICKNESS, sides=6)
    # The helmet shell: opaque white, from the ring (tucked into its tube) to the top, widening smoothly to DOME_SIZE.
    sides, rows = numbers.DOME
    angles = [2.0 * math.pi * index / sides for index in range(sides)]

    def dome_scale(psi):
        grow = min(1.0, max(0.0, (90.0 - psi) / max(90.0 - numbers.DOME_FULL, 1e-6)))
        grow = grow * grow * (3.0 - 2.0 * grow)
        return size * 0.90 + (numbers.DOME_SIZE - size * 0.90) * grow

    loops = [[on_shell(90.0, angle, dome_scale(90.0)) for angle in angles]]
    for row in range(1, rows + 1):
        psi = 90.0 * (1.0 - row / rows)
        loops.append([on_shell(psi, angle, dome_scale(psi)) for angle in angles])
    loops[-1] = [on_shell(0.0, 0.0, dome_scale(0.0))]
    KIT.add_loops(ring, loops, ring.named("Helmet"), numbers.OUTLINE_THICKNESS)
    side, behind, up = numbers.VENT_AT
    start = surface.middle + Vector((side, behind, up))
    ring.shell(base.tube_rings(start, Vector((-1.0, 0.0, 0.0)), Y, numbers.VENT), 6, ring.named("Vent"),
               numbers.OUTLINE_THICKNESS * 0.8)
    # The hose (22 October 2026, the owner: "a hose connected to nothing"): ONE ribbed tube from the tip of the ribbed unit down
    # beside the helmet, over the shoulder and across the chest into the top of the box, so both ends are connected. It is built
    # on the head (the unit moves with it) from points of the rest pose, the last ones on the front of the torso; the chest end
    # does not move enough with the turn of the head (6 to 8 degrees about the neck) to leave the box. A smooth curve through the
    # points (Catmull-Rom), ribbed by a swell on every other station.
    to_body, to_head, _ = mascot_axes(maker)
    tip = start + Vector((-numbers.VENT[-1][0], 0.0, 0.0))
    points = [to_body(tip)] + [Vector(point) for point in numbers.HOSE_WAY]
    points += [shirt_front(maker, level, side, numbers.HOSE_RADIUS + 0.002) for side, level in numbers.HOSE_CHEST]
    smooth = []
    for index in range(len(points) - 1):
        before, here, there, after = points[max(index - 1, 0)], points[index], points[index + 1], points[min(index + 2, len(points) - 1)]
        for step in range(numbers.HOSE_STEPS):
            share = step / numbers.HOSE_STEPS
            smooth.append(0.5 * ((2.0 * here) + (-before + there) * share + (2.0 * before - 5.0 * here + 4.0 * there - after) * share ** 2
                                 + (-before + 3.0 * here - 3.0 * there + after) * share ** 3))
    smooth.append(points[-1])
    way = [to_head(point) for point in smooth]
    across, through = [], []
    for index, point in enumerate(way):
        ahead = way[min(index + 1, len(way) - 1)] - way[max(index - 1, 0)]
        first, second = base.frame_across(ahead, Y)
        swell = 1.0 + 0.30 * (index % 2)
        across.append(first * numbers.HOSE_RADIUS * swell)
        through.append(second * numbers.HOSE_RADIUS * swell)
    slab(ring, way, across, through, ring.named("Hose"), numbers.OUTLINE_THICKNESS * 0.7, sides=5)


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
               "Astro": build_astro, "DuckFloat": build_duck_float, "Mascot": build_mascot}


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
COSTUME_SLEEVE = 0.205  # a costume's sleeve goes into the mitten: no skin at the wrist (16 October 2026)
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
             "numbers": {"STEP": 0.076, "SLEEVE": COSTUME_SLEEVE},
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
                                               "Belly": (0.92, 0.95, 0.97), "Tooth": (1.0, 1.0, 1.0),
                                               "Fin": (0.13, 0.36, 0.66), "Eye": (0.04, 0.06, 0.14),
                                               "Gill": (0.12, 0.34, 0.62)},
                          teeth=True, fin=True,
                          hood_numbers={"SWELL": (0.085, 0.030, 40.0, 20.0, 26.0), "SIDES": 24}),
    # A whole banana round the whole Chog (build_mascot, 16 October 2026, after the owner saw a hood over a
    # suit twice): its five sides from yellow round the face to orange at the feet, green over the head, its
    # stalk at the top; colours measured on #37 (until then a hood with a point over a faceted peel).
    "Banana suit": {**costume((0.93, 0.56, 0.04), {"Mid": (0.98, 0.64, 0.08), "Hood": (1.00, 0.71, 0.11),
                                                   "Green": (0.55, 0.66, 0.10), "Stem": (0.40, 0.36, 0.10),
                                                   "Tip": (0.20, 0.14, 0.06)}),
                    "kind": "Mascot", "shape": "banana"},
    # A whole red dragon (build_mascot): its round body as wide as the head, a light belly, tan horns round
    # the face, the nostrils on top, dark scales, wings, a tail and claws; colours measured on #16 (until
    # then a hood with fangs and a snout over a suit).
    "Dragon suit": {**costume((0.96, 0.31, 0.21), {"Hood": (0.96, 0.31, 0.21), "Belly": (1.00, 0.76, 0.52),
                                                   "Fang": (0.96, 0.82, 0.46), "EyeBall": (1.00, 0.95, 0.70), "Wing": (0.99, 0.40, 0.26),
                                                   "Nostril": (0.30, 0.06, 0.05), "Scale": (0.80, 0.18, 0.12),
                                                   "Rib": (0.50, 0.08, 0.06),
                                                   "Ridge": (0.96, 0.82, 0.46)}),
                    "kind": "Mascot", "shape": "dragon"},
    # The space suit: white, grey stripes down the sleeves, the ring of the helmet around the face. Its
    # glass is not drawn; the pictures show the crown inside it, so the crown stays.
    "Astro Suit": {"kind": "Astro", "colours": {"Main": (0.90, 0.88, 0.84), "Collar": (0.70, 0.70, 0.72),
                                                "Stripe": (0.52, 0.56, 0.69), "Ring": (0.72, 0.73, 0.76),
                                                "Helmet": (0.95, 0.94, 0.93), "Boot": (0.66, 0.67, 0.74),
                                                "Vent": (0.64, 0.65, 0.70),
                                                "Panel": (0.52, 0.55, 0.70), "Hose": (0.34, 0.38, 0.50),
                                                "FlagRed": (0.80, 0.14, 0.20), "FlagWhite": (0.96, 0.96, 0.97),
                                                "FlagBlue": (0.16, 0.22, 0.58)},
                   "numbers": {"STEP": 0.076, "SLEEVE": LONG_SLEEVE},
                   # (The helmet was a glass drawn on a share of the pixels until the evening of 22 October 2026: the owner,
                   # "I don't understand why you put transparency and grain, it's useless, just make an astronaut costume".
                   # The glass mode of the shader and of the renders stays, unused.) The pictures draw the hair of the Chog
                   # (its colour, a zigzag edge) inside the ring, above the face, and nothing of it outside the helmet: no
                   # crown, the fringe of the base stays.
                   "head": {"LEAVES": NO_CROWN, "FRINGE": True},
                   "print_missing": "the glass over the face (the pictures draw none: the face is open in the ring)"},
    "Duck Float": {"kind": "DuckFloat", "colours": {"Duck": (0.93, 0.68, 0.20), "Beak": (0.92, 0.48, 0.12),
                                                    "Eye": (0.10, 0.07, 0.04)}},
})
for name in ("Gangster suit", "Shark Suit", "Banana suit", "Astro Suit", "Dragon suit", "Duck Float"):
    LEFT_OUT.pop(name, None)

# The clothing no Genesis wore in 3D until 16 October 2026 (117 Genesis, most of them of the base Origin on
# Side=Left; the owner asked for every Genesis dressed). Each takes a cut already built, in the colours read
# on its pictures (CHOG #64, #25, #32 the toads, #247 the armour, #227 Sci Fi, #216 the yukata, #474 Guko,
# #358 Gojo, #846 the cape, #1311 the robe, #552 the mummy, #978 the vest, #155 the stripes, #772 Boner,
# #275 and #1009 the tanks, #1221 and #1723 the holed jackets, #1289 the suit); what that cut does not draw
# is said in "print_missing".
def toad(main, rim, belly, pupil):
    """A toad costume: the suit and the hood of a costume in one colour, a lighter belly, two big eyes on top
    of the hood (the frog suit of the 1:1, chog_one_of_one.py), the teeth of hair showing under the hood."""
    entry = costume(main, {"Hood": main, "Rim": rim, "Belly": belly, "EyeBall": (0.97, 0.97, 0.95), "Pupil": pupil},
                    head={"LEAVES": NO_CROWN, "FRINGE": True})
    # The two eyes sit at the two upper corners of the hood, wide apart, as a frog's (#25, #32, #242; 22 October 2026: 17 degrees
    # each side of straight up, a pair of eyes nearly touching, until then): 43 degrees each side, a little ahead.
    return {**entry, "kind": "FrogSuit", "frog_eyes": {"WHERE": ((0.68, -0.12, 0.72), (-0.68, -0.12, 0.72))}}


MUMMY_WRAP = {
    # Read again on #552, #816 and #1642 (16 October 2026, the owner: "not really bandages"; until then a
    # striped shirt and two flat strips): the skeleton shows, its ribs between the bandages, wide cream
    # bandages with a dark edge laid over it: one round the jaw over the teeth, two crossing on the chest
    # from each shoulder to the other hip, one round the waist. Each band: its half width, how far off the
    # body, its half thickness.
    "BAND": (0.034, 0.012, 0.007),
    "CROSS": ((80.0, 0.60), (-80.0, 0.33)),       # (degrees round from the front, height) of its two ends
    "WAIST": 0.315,
    # Round the jaw (16 October 2026, the owner: "still badly made on the head"; until then one ring 13 % larger
    # than the head, which stood off it like a flange): tight on the skull over the upper teeth, under the
    # nose, as on #552 and #816. The height of its middle on the face, its half width, how far off the head
    # at the front (over the teeth) and at the back; a second turn over the first, tilted (degrees), a little
    # narrower. (0.040 at the front until 18 October 2026, when the teeth were drawn 0.031 off the head; they
    # are now 0.009 off it.)
    "JAW": (-0.080, 0.032, 0.020, 0.012),
    "JAW_TURN": (7.0, 0.80, 0.008),
    "STATIONS": 13,
}


def build_mummy(maker):
    """A mummy (MUMMY_WRAP): bandages over the skeleton, which keeps its ribs."""
    import chog_trait_bases as bases
    numbers = SimpleNamespace(**MUMMY_WRAP)
    body, surface = maker.body, maker.surface
    torso = maker.part(on="Body")
    wrap = torso.named("Wrap")
    half_wide, off, half_thick = numbers.BAND

    def around_torso(degrees, height):
        half_width, half_depth = KIT.torso_section(body, height)
        angle = math.radians(degrees)
        point = Vector((math.sin(angle) * (half_width + off), -math.cos(angle) * (half_depth + off), height))
        out = Vector((math.sin(angle) / (half_width + off), -math.cos(angle) / (half_depth + off), 0.0)).normalized()
        return point, out

    def band(places):
        path, wide, thick = [], [], []
        for index, (point, out) in enumerate(places):
            ahead = places[min(index + 1, len(places) - 1)][0] - places[max(index - 1, 0)][0]
            across = out.cross(ahead).normalized()
            path.append(point)
            wide.append(across * half_wide)
            thick.append(out * half_thick)
        slab(torso, path, wide, thick, wrap, 0.008, sides=4)

    (from_deg, from_height), (to_deg, to_height) = numbers.CROSS
    for sign in (1.0, -1.0):
        band([around_torso(sign * (from_deg + (to_deg - from_deg) * step / (numbers.STATIONS - 1)),
                           from_height + (to_height - from_height) * step / (numbers.STATIONS - 1))
              for step in range(numbers.STATIONS)])
    band([around_torso(-180.0 + 360.0 * step / 16, numbers.WAIST) for step in range(17)])

    # Round the jaw, then narrowed with the skull's jaw (narrow_jaw), as the head is.
    head = maker.part()
    up, half_band, front_lift, back_lift = numbers.JAW
    tilt_deg, narrower, over = numbers.JAW_TURN
    height = maker.head.FACE_MIDDLE[1] + up
    middle_polar = math.acos(max(-1.0, min(1.0, height / surface.half.z)))
    steps = 24
    for tilt, wide, extra in ((0.0, 1.0, 0.0), (math.radians(tilt_deg), narrower, over)):
        rows = [[], [], [], []]
        for step in range(steps):
            angle = 2.0 * math.pi * step / steps
            facing = math.cos(math.radians(from_front(angle)))
            lift = back_lift + (front_lift - back_lift) * max(0.0, facing) ** 0.6 + extra
            polar = middle_polar - tilt * math.sin(angle)     # the second turn lower at the front
            for row, (edge, off) in enumerate(((-1.0, -0.004), (-1.0, lift), (1.0, lift), (1.0, -0.004))):
                here = polar + edge * half_band * wide / surface.half.z
                rows[row].append(on_head(surface, angle, here, 1.0) + head_normal(surface, angle, here) * off)
        KIT.add_loops(head, rows, head.named("Wrap"), 0.008)
    bases.narrow_jaw(head, maker.head, "Right")     # the three mummies are skulls of Side=Right


CLOTH_KINDS["Mummy"] = build_mummy
STRIPED_TANK = {"kind": "Tee", "stripes": True, "numbers": {"SLEEVE": 0.0, "STEP": 0.046}}
CLOTHES.update({
    "toad blue suit": toad((0.33, 0.56, 0.93), (0.62, 0.80, 1.00), (0.62, 0.92, 0.86), (0.36, 0.78, 0.25)),
    "Toad pink suit": toad((0.93, 0.27, 0.53), (0.98, 0.62, 0.78), (0.98, 0.74, 0.86), (0.30, 0.68, 0.95)),
    "Toad green Suit": toad((0.18, 0.74, 0.56), (0.55, 0.92, 0.75), (0.70, 0.95, 0.82), (0.98, 0.50, 0.15)),
    "Golden Armor": {"kind": "Tee", "colours": {"Main": (0.96, 0.74, 0.22), "Stripe": (0.82, 0.56, 0.12),
                                                "Collar": (0.70, 0.46, 0.10)},
                     "stripes": True, "numbers": {"STEP": 0.046, "SLEEVE": 0.09},
                     "print_missing": "the round plates on the shoulders"},
    "Sci Fi": track((0.98, 0.62, 0.14), stripe=(0.25, 0.85, 0.95), collar=(0.15, 0.15, 0.18),
                    missing="the dark ring of the collar and its lights"),
    "White Yukata": tee((0.97, 0.95, 0.95), collar=(0.92, 0.64, 0.74), SLEEVE=LONG_SLEEVE,
                        missing="the crossed fronts and the pink blossoms"),
    "Guko": tee((0.98, 0.52, 0.10), collar=(0.16, 0.28, 0.72), missing="the blue undershirt, the belt and the emblem"),
    "Gojo": {**tee((0.13, 0.14, 0.24), (0.09, 0.10, 0.17)), "numbers": {"STEP": 0.076, "COLLAR": (0.600, 0.640),
                                                                         "SLEEVE": LONG_SLEEVE},
             "print_missing": "the round button of the high collar"},
    "Wizard Cape": {"kind": "Cape", "colours": {"Cape": (0.17, 0.22, 0.52), "Collar": (0.10, 0.12, 0.32),
                                                "Clasp": (0.96, 0.78, 0.22)},
                    "print_missing": "the yellow pattern of the cape"},
    "Jesus Robe": tee((0.95, 0.94, 0.90), collar=(0.80, 0.16, 0.13), SLEEVE=LONG_SLEEVE,
                      missing="the red sash over the shoulder"),
    "Mummy": {"kind": "Mummy", "colours": {"Wrap": (0.94, 0.91, 0.82)}},
    "CEO Vest": {"kind": "Tee", "colours": {"Main": (0.30, 0.31, 0.35), "Stripe": (0.22, 0.23, 0.27),
                                            "Collar": (0.19, 0.20, 0.24)},
                 "stripes": True, "numbers": {"SLEEVE": 0.0, "STEP": 0.046}},
    "Stripe Shirt": dict(CLOTHES["Stripes"]),
    "Stripe shirts": dict(CLOTHES["Stripes"]),
    "White Tank": tee((0.95, 0.95, 0.92), SLEEVE=0.0, COLLAR=(0.566, 0.593)),
    "Purple Tank": {**STRIPED_TANK, "colours": {"Main": (0.56, 0.52, 0.64), "Stripe": (0.46, 0.42, 0.54),
                                                "Collar": (0.40, 0.36, 0.48)}},
    "Boner": with_({**STRIPED_TANK, "colours": {"Main": (0.74, 0.80, 0.70), "Stripe": (0.64, 0.70, 0.60),
                                                "Collar": (0.55, 0.60, 0.52)}},
                   colours={"Print": (0.10, 0.10, 0.10)}, print=("BONER", 0.400, 0.048, 0.200, 0.0)),
    "Blue Mafia": jacket((0.16, 0.25, 0.62), WHITE, (0.14, 0.22, 0.58)),
    "Orange Holed Jacket": hoodie((0.96, 0.50, 0.14), print_missing="the holes in the cloth"),
    "Blue Holed Jacket": hoodie((0.32, 0.64, 0.95), print_missing="the holes in the cloth"),
})


CLOTHES_OF_SIDE = {
    "Left": {
        "Tuxedo": jacket((0.27, 0.25, 0.30), WHITE, (0.86, 0.12, 0.12)),
    },
}
