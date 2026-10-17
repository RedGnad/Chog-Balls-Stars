"""The things a Genesis holds: the trait "Accessory" as 3D parts in the right hand of the hero (17 October 2026).
Until then the trait had no part at all (195 Genesis, every one of them on Side=Left).

make_chog_traits.py imports this module and registers ACCESSORIES and ACCESSORY_KINDS as the table and the
builders of the trait "Accessory" (TRAIT_PARTS); export_chog_parts.py then exports each part like any other.

What the pictures show (1000-point pictures, CHOG numbers): the near hand, a pink mitten raised under the chin,
holds the thing. Side=Left draws the hero three quarter from its right, so the near hand is its right hand: the
part sits on "armRight" and follows the arm the game moves. The game holds the arm down, so the thing comes out
of the front and the top of the mitten, where the camera of the game sees it.

    Green Candle, Red Candle (#28, #59, #132, #396; #70, #71, #141, #352): a box of the candle's colour, a thin
                 wick of the same colour out of each end, held up through the fist
    Cigar (#26, #131, #175, #178): a brown cigar, its lit end red, held level out of the fist
    SB (#45, #152, #517, #985): a white takeaway cup with a lid and the round green mark of a coffee chain
    Coin (#591, #695, #864, #1472): a gold coin held up on the top of the fist
    Knife (#1002, #1149, #1770, #1930): a dark handle in the fist, a grey blade up
    Heart (#23): the fingers make a heart: a small white heart over the fist
    Fwog (#74, #192, #198, #485): a small green frog in the fist, big dark eyes, a pink open mouth
    PEPE (#103, #252, #1129): the frog of the meme, round white eyes looking up, red lips, in a blue shirt

Every thing is a closed shell with the dark outline of the model, as the other parts with volume; sizes are
counted against the size of the hand (HAND_SIZE of the body), so they follow it.
"""

import math

from mathutils import Vector

import chog_attack_objects as shapes_of
import make_chog_body as base

X, Y, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))
FRONT = -Y
SIDE = -1.0                  # the right arm: its hand is on -X
OUTLINE = 0.009              # the outline of a held thing, thinner than the body's (0.024): it is small
FINE = 0.005                 # the outline of a thin piece (a wick, a band)

# Where the fist holds a thing, against the middle of the hand: how far to its front and how far up, in sizes
# of the hand (the mitten is 0.50 of its size deep, half of it to the front).
GRIP = {"FRONT": 0.42, "UP": 0.10}

# Sizes are those of the pictures against the mitten, which draw the things large: a candle as long as two
# hands, a cup as tall as one. (First try, 17 October 2026: two thirds of these; from the game's camera a
# candle or a cigar was a speck beside the hand.)
CANDLE = {
    "LEAN": (0.30, 0.22),        # how far the candle leans to the front and outwards, against up
    "BODY": (-0.45, 0.80, 0.21), # along it from the grip: where the box starts and ends, half its width (hand sizes)
    "WICK": (0.32, 0.24, 0.045), # how far the wick passes the top and the bottom, its radius (hand sizes)
}
CIGAR = {
    "AXIS": (0.80, 0.50, 0.28),  # to the front, outwards, up
    "FROM": -0.25,               # where it starts behind the grip (hand sizes)
    "LENGTH": 1.15,
    "RADIUS": 0.13,
    "BAND": (0.10, 0.26),        # where the band is along it, from its start (hand sizes)
    "TIP": 0.14,                 # the lit end (hand sizes)
}
CUP = {
    "DOWN": 0.35,                # how far its bottom is under the grip (hand sizes)
    "TALL": 0.85,
    "RADIUS": (0.22, 0.28),      # at the bottom and at the top
    "LID": (0.11, 0.30, 0.26),   # its height, its radius at its rim and at its top
    "MARK": (0.34, 0.14, 0.07),  # how high on the cup its middle is (share of TALL), the radius of the green
                                 # disc and of the light ring in it
}
COIN = {"UP": 0.40, "FRONT": 0.06, "RADIUS": 0.36, "THICK": 0.08, "FACING": (0.55, 0.85)}
KNIFE = {
    "HANDLE": (-0.30, 0.28, 0.09),   # along up from the grip: start, end, radius (hand sizes)
    "GUARD": (0.17, 0.04),           # half its width, half its height
    "BLADE": (0.85, 0.15, 0.04),     # its length, half its width, its thickness
}
HEART = {"UP": 0.58, "FRONT": 0.14, "HALF_WIDTH": 0.30, "THICK": 0.09}
FROG = {
    "UP": 0.40, "FRONT": 0.18,
    "HEAD": (0.34, 0.25),        # half its width and half its height
    "EYES": (0.15, 0.17, 0.11, 0.07),     # to each side, above the middle of the head, radius of the ball, of the pupil
    "MOUTH": (-0.08, 0.09, 0.05),         # below the middle, half width, half height
}


def hand_of(body):
    """The middle of the right hand in the axes of its arm, and the size of the hand."""
    spread, ahead = math.radians(body.ARM_SPREAD_DEG), math.radians(body.ARM_FORWARD_DEG)
    down = Vector((SIDE * math.sin(spread), -math.sin(ahead), -math.cos(spread) * math.cos(ahead))).normalized()
    return down * body.ARM_LENGTH, body.HAND_SIZE


def grip_of(body):
    """Where the fist holds a thing, and the size of the hand."""
    hand, size = hand_of(body)
    return hand + FRONT * size * GRIP["FRONT"] + Z * size * GRIP["UP"], size


def flat_disc(part, centre, axis, radius, material, sides=8):
    """A flat round shape facing "axis", one face with no outline (a print, a pupil): few triangles."""
    axis = axis.normalized()
    first, second = base.frame_across(axis, Z if abs(axis.dot(Z)) < 0.9 else X)
    mesh = part.mesh
    points = [mesh.verts.new(centre + (first * math.cos(2.0 * math.pi * index / sides)
                                       + second * math.sin(2.0 * math.pi * index / sides)) * radius)
              for index in range(sides)]
    face = mesh.faces.new(points)
    face.normal_update()
    if face.normal.dot(axis) < 0.0:
        face.normal_flip()
    face.smooth = False
    part.paint([face], material)
    part.open_faces.append(face)


def box(part, start, axis, length, half_width, material, outline):
    """A long box along "axis", faces flat, one face to the front."""
    axis = axis.normalized()
    first, second = base.frame_across(axis, X * SIDE)
    # add_shell puts the 4 points of a ring at 0, 90, 180 and 270 degrees of its two half axes: those turned
    # by 45 degrees, so a face of the box looks to the side and one ahead.
    radius = half_width * math.sqrt(2.0)
    turned_first = (first + second).normalized() * radius
    turned_second = (second - first).normalized() * radius
    rings = [(start + axis * distance, turned_first, turned_second) for distance in (0.0, length)]
    return part.shell(rings, 4, material, outline, smooth=False)


def build_candle(maker):
    """A candle of a price chart: a box, a thin wick out of each end, held up through the fist."""
    numbers = maker.numbers(CANDLE)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    front, outwards = numbers.LEAN
    axis = (Z + FRONT * front + X * SIDE * outwards).normalized()
    start, stop, half = numbers.BODY
    box(part, grip + axis * start * size, axis, (stop - start) * size, half * size, part.named("Body"), OUTLINE)
    above, below, radius = numbers.WICK
    for begin, end in ((stop - 0.02, stop + above), (start - below, start + 0.02)):
        shapes_of.tube(part, grip + axis * begin * size, axis,
                       [(0.0, radius * size), ((end - begin) * size, radius * size)], part.named("Wick"),
                       outline=FINE, sides=6)


def build_cigar(maker):
    """A cigar held level out of the fist: brown, a light band near the fist, its lit end red."""
    numbers = maker.numbers(CIGAR)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    front, outwards, up = numbers.AXIS
    axis = (FRONT * front + X * SIDE * outwards + Z * up).normalized()
    radius, length = numbers.RADIUS * size, numbers.LENGTH * size
    start = grip + axis * numbers.FROM * size
    tip = numbers.TIP * size
    shapes_of.tube(part, start, axis, [(0.0, radius * 0.55), (radius * 0.5, radius), (length - tip, radius)],
                   part.named("Leaf"), outline=OUTLINE, sides=8)
    shapes_of.tube(part, start + axis * (length - tip - 0.002), axis, [(0.0, radius), (tip, radius * 0.92)],
                   part.named("Tip"), outline=OUTLINE, sides=8)
    band_from, band_to = numbers.BAND
    shapes_of.tube(part, start + axis * band_from * size, axis,
                   [(0.0, radius * 1.08), ((band_to - band_from) * size, radius * 1.08)], part.named("Band"),
                   outline=0.0, sides=8)


def build_cup(maker):
    """A takeaway coffee cup: white, wider at the top, a grey lid, the round green mark on its front."""
    numbers = maker.numbers(CUP)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    bottom = grip - Z * numbers.DOWN * size
    tall = numbers.TALL * size
    low, high = (value * size for value in numbers.RADIUS)
    shapes_of.tube(part, bottom, Z, [(0.0, low * 0.94), (0.004, low), (tall, high)], part.named("Cup"),
                   outline=OUTLINE, sides=10)
    lid_tall, lid_rim, lid_top = (value * size for value in numbers.LID)
    shapes_of.tube(part, bottom + Z * (tall - 0.004), Z, [(0.0, lid_rim), (lid_tall * 0.45, lid_rim),
                                                         (lid_tall, lid_top), (lid_tall + 0.002, lid_top * 0.6)],
                   part.named("Lid"), outline=OUTLINE, sides=10)
    share, mark, ring = numbers.MARK
    height = tall * share
    radius_there = low + (high - low) * share
    centre = bottom + Z * height + FRONT * (radius_there + 0.0015)
    flat_disc(part, centre, FRONT, mark * size, part.named("Mark"), sides=10)
    flat_disc(part, centre + FRONT * 0.0015, FRONT, ring * size, part.named("Cup"), sides=8)


def build_coin(maker):
    """A gold coin held up on the top of the fist, its face to the front and a little up."""
    numbers = maker.numbers(COIN)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    centre = grip + Z * numbers.UP * size + FRONT * numbers.FRONT * size
    front, up = numbers.FACING
    axis = (FRONT * front + Z * up).normalized()
    radius, thick = numbers.RADIUS * size, numbers.THICK * size
    shapes_of.disc(part, centre, axis, radius, thick, part.named("Gold"), outline=OUTLINE, sides=12)
    shapes_of.torus(part, centre + axis * thick * 0.5, axis, radius * 0.72, thick * 0.20, part.named("Rim"),
                    outline=0.0, segments=12, sides=3)


def build_knife(maker):
    """A small knife: a dark handle in the fist, a grey blade pointing up, flat to the front."""
    numbers = maker.numbers(KNIFE)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    start, stop, radius = numbers.HANDLE
    shapes_of.tube(part, grip + Z * start * size, Z, [(0.0, radius * size), ((stop - start) * size, radius * size)],
                   part.named("Handle"), outline=OUTLINE, sides=8)
    half_wide, half_tall = numbers.GUARD
    guard = grip + Z * stop * size
    shapes_of.prism(part, ((-half_wide * size, -half_tall * size), (half_wide * size, -half_tall * size),
                           (half_wide * size, half_tall * size), (-half_wide * size, half_tall * size)),
                    guard, X, Z, 0.05 * size, part.named("Handle"), outline=FINE)
    length, half, thick = numbers.BLADE
    length, half = length * size, half * size
    blade = ((-half, 0.0), (half, 0.0), (half, length * 0.62), (0.0, length), (-half * 0.6, length * 0.78))
    shapes_of.prism(part, blade, guard + Z * half_tall * size, X, Z, thick * size, part.named("Blade"), outline=FINE)


def heart_points(half_width, segments=20):
    """The edge of a heart, point down, its middle at the origin."""
    scale = half_width / 16.0
    points = []
    for index in range(segments):
        angle = 2.0 * math.pi * index / segments
        x = 16.0 * math.sin(angle) ** 3
        y = 13.0 * math.cos(angle) - 5.0 * math.cos(2 * angle) - 2.0 * math.cos(3 * angle) - math.cos(4 * angle) + 2.5
        points.append((x * scale, y * scale))
    # prism wants the points counter-clockwise seen from the front (u x v): the curve runs clockwise.
    return list(reversed(points))


def build_heart(maker):
    """The fingers make a heart: a small white heart over the fist."""
    numbers = maker.numbers(HEART)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    centre = grip + Z * numbers.UP * size + FRONT * numbers.FRONT * size
    shapes_of.prism(part, heart_points(numbers.HALF_WIDTH * size), centre, X, Z, numbers.THICK * size,
                    part.named("Heart"), outline=OUTLINE)


def frog_head(part, centre, size, numbers, skin, white, dark):
    """The round head of a frog, two eyes on its top front. Returns the head's half width and height."""
    half_width, half_height = (value * size for value in numbers.HEAD)
    rings = base.ball_rings(centre, Z, X, half_height, half_width, half_width * 0.85, 5)
    part.shell(rings, 9, skin, OUTLINE)
    across, above, ball, pupil = numbers.EYES
    for side in (1.0, -1.0):
        eye = centre + X * side * across * size + Z * above * size + FRONT * half_width * 0.35
        part.shell(base.ball_rings(eye, Z, X, ball * size, ball * size, ball * size, 3), 6, white, OUTLINE * 0.7)
        yield eye, ball * size, pupil * size, side
    return


def build_fwog(maker):
    """A small green frog in the fist: big dark eyes with a glint, a pink open mouth."""
    numbers = maker.numbers(FROG)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    centre = grip + Z * numbers.UP * size + FRONT * numbers.FRONT * size
    for eye, ball, pupil, side in frog_head(part, centre, size, numbers, part.named("Skin"), part.named("Eye"),
                                            part.named("Dark")):
        # Its eyes are dark balls; a light glint on the front of each.
        out = (FRONT + Z * 0.35 + X * side * 0.25).normalized()
        flat_disc(part, eye + out * ball * 1.03, out, ball * 0.32, part.named("Glint"), sides=6)
    below, half_wide, half_tall = numbers.MOUTH
    half_width = numbers.HEAD[0] * size
    mouth = centre + Z * below * size + FRONT * half_width * 0.82
    flat_disc(part, mouth, FRONT, half_wide * size, part.named("Mouth"), sides=8)


def build_pepe(maker):
    """The frog of the meme in the fist: round white eyes with pupils looking up, red lips, a blue shirt."""
    numbers = maker.numbers(FROG)
    part = maker.part(on="armRight")
    grip, size = grip_of(maker.body)
    centre = grip + Z * numbers.UP * size + FRONT * numbers.FRONT * size
    half_width, half_height = (value * size for value in numbers.HEAD)
    # The shirt: a blue body under the head, in the fist.
    part.shell(base.ball_rings(centre - Z * half_height * 1.2, Z, X, half_height * 0.9, half_width * 0.85,
                               half_width * 0.75, 3), 7, part.named("Shirt"), OUTLINE)
    for eye, ball, pupil, side in frog_head(part, centre, size, numbers, part.named("Skin"), part.named("Eye"),
                                            part.named("Dark")):
        # Looking up, as the pictures draw them.
        out = (FRONT + Z * 0.45).normalized()
        flat_disc(part, eye + out * ball * 1.03, out, pupil, part.named("Dark"), sides=6)
    below, half_wide, half_tall = numbers.MOUTH
    lips = centre + Z * below * size + FRONT * half_width * 0.80
    part.shell(base.ball_rings(lips, X, Z, half_wide * size * 2.2, half_tall * size * 0.9, half_tall * size * 0.9, 3),
               6, part.named("Lips"), OUTLINE * 0.7)


GREEN_CANDLE = (0.16, 0.80, 0.18)
RED_CANDLE = (0.86, 0.13, 0.12)
ACCESSORIES = {
    # Colours read on the 1000-point pictures named at the top of this file.
    "Green Candle": {"kind": "Candle", "colours": {"Body": GREEN_CANDLE, "Wick": (0.10, 0.62, 0.12)}},
    "Red Candle": {"kind": "Candle", "colours": {"Body": RED_CANDLE, "Wick": (0.68, 0.08, 0.08)}},
    "Cigar": {"kind": "Cigar", "colours": {"Leaf": (0.48, 0.28, 0.15), "Band": (0.86, 0.66, 0.30),
                                           "Tip": (0.95, 0.24, 0.10)}, "unlit": ("Tip",)},
    "SB": {"kind": "Cup", "colours": {"Cup": (0.96, 0.96, 0.95), "Lid": (0.80, 0.80, 0.82), "Mark": (0.10, 0.45, 0.28)}},
    "Coin": {"kind": "Coin", "colours": {"Gold": (0.95, 0.66, 0.16), "Rim": (0.78, 0.46, 0.08)}},
    "Knife": {"kind": "Knife", "colours": {"Handle": (0.16, 0.15, 0.18), "Blade": (0.78, 0.80, 0.84)}},
    "Heart": {"kind": "Heart", "colours": {"Heart": (0.98, 0.97, 0.96)}},
    "Fwog": {"kind": "Fwog", "colours": {"Skin": (0.20, 0.60, 0.20), "Eye": (0.06, 0.06, 0.07),
                                         "Dark": (0.06, 0.06, 0.07), "Glint": (0.97, 0.97, 0.97),
                                         "Mouth": (0.92, 0.48, 0.55)}, "unlit": ("Glint",)},
    "PEPE": {"kind": "Pepe", "colours": {"Skin": (0.32, 0.62, 0.22), "Eye": (0.97, 0.97, 0.96),
                                         "Dark": (0.06, 0.06, 0.07), "Lips": (0.80, 0.33, 0.22),
                                         "Shirt": (0.12, 0.32, 0.86)}},
}
ACCESSORY_KINDS = {
    "Candle": build_candle, "Cigar": build_cigar, "Cup": build_cup, "Coin": build_coin, "Knife": build_knife,
    "Heart": build_heart, "Fwog": build_fwog, "Pepe": build_pepe,
}
