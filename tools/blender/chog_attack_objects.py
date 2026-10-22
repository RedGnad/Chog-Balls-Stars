"""The objects of the basic attacks (15 October 2026): what a hero throws or strikes with, drawn from the
picture of its Genesis, value by value of the trait "Head", or of "Body" for a full costume, which covers
the crown and comes without a hat (docs/ATTACKS-FROM-THE-ART.md, where the
table of what each picture shows lives). export_chog_parts.py builds them into the library of parts the
game reads; the game moves them (Assets/Chog/Match/ChogAttackLook.cs).

Each object is made like the parts of the heroes: closed shells with the dark outline shell of the model
(an inverted hull), flat colours in materials named after the object ("Attack_<Key>_<colour>"), lettering
as flat shapes laid on a face. It is centred on its origin and small (about 0.3 units, the hero's scale
makes it about half a unit in the arena). One that points (the spear, the arrow) points along -Y, the way
the game moves an attack once the axes are turned for Unity.

Hats are not made again: an attack that throws the hat itself (a cap, a bucket hat, the crown, the
plunger) uses the very part the hero wears ("HAT" in ATTACK_OF_HEAD), centred by the game.
"""

import math

import bmesh
from mathutils import Vector

import make_chog_body as base

X, Y, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))
NOWHERE = Vector((0.0, 0.0, 0.0))
FORWARD = -Y
OUTLINE = 0.012
SKIN = "ChogSkin"   # the skin of the hero: the leaf is drawn in it

# How the game moves an object (ChogAttackLook.Style): turning flat like a frisbee, tumbling end over end,
# pointing where it goes, or bobbing as it floats.
SPIN, TUMBLE, POINT, FLOAT = 0, 1, 2, 3

# The value of "Head" -> (the object, how it moves). "HAT": the hat the hero wears, thrown. A hero with
# no hat throws a leaf of its crown ("" below).
ATTACK_OF_HEAD = {
    "": ("Leaf", TUMBLE),
    "Russian Hat": ("Snowball", TUMBLE),
    "Wizard Hat": ("StarYellow", SPIN),
    "Star Cap": ("StarWhite", SPIN),
    "Cloud Cap": ("Cloud", FLOAT),
    "Durag": ("Ribbon", TUMBLE),
    "Bob Hair": ("Curl", TUMBLE),
    "Dollar Cap": ("CoinDollar", TUMBLE),
    "BTC Cap": ("CoinBitcoin", TUMBLE),
    "Buttcoin Cap": ("CoinButtcoin", TUMBLE),
    "Thunder Cap": ("Bolt", POINT),
    "Peace hat": ("Peace", SPIN),
    "Red Peace Cap": ("Peace", SPIN),
    "Japan Headband": ("SunDisc", SPIN),
    "Red Headband": ("RedRing", SPIN),
    "Wizard Beanie": ("YellowRing", SPIN),
    "Spartan Helmet": ("Spear", POINT),
    "RH Beanie": ("Arrow", POINT),
    "Robber": ("MoneyBag", TUMBLE),
    "BTC Beanie": ("Pizza", SPIN),
    "Choggie Beanie": ("MiniChog", TUMBLE),
    "Mon Beanie": ("Yarn", TUMBLE),
    "Rainbow Wig": ("RainbowBall", TUMBLE),
    "Clown Nose": ("Nose", TUMBLE),
    "Plumber": ("HAT", POINT),
}
# Every other value of "Head" that the library dresses: the hat itself, turning flat.
HAT_THROWN = ("HAT", SPIN)

# A full costume covers the crown and never comes with a hat (every recorded Genesis in one of them has
# no "Head"): what it throws comes from the costume, as the art draws it (22 October 2026). The value of
# "Body" -> (the object, how it moves). A Genesis in any other clothing throws what its "Head" gives.
ATTACK_OF_BODY = {
    "Dragon suit": ("Fireball", POINT),
    "Shark Suit": ("WaterDrop", POINT),
    "Banana suit": ("Banana", TUMBLE),
    "Astro Suit": ("Planet", SPIN),
    "Toad green Suit": ("LilyPad", SPIN),
    "Gangster suit": ("MoneyBag", TUMBLE),
}


# What a player reads under the hero: the object, in words.
WORDS = {
    "Leaf": "SPIKES", "Snowball": "SNOWBALLS", "StarYellow": "STARS", "StarWhite": "STARS", "Cloud": "CLOUDS",
    "Ribbon": "ITS DURAG", "Curl": "CURLS", "CoinDollar": "DOLLAR COINS", "CoinBitcoin": "BITCOINS",
    "CoinButtcoin": "BUTTCOINS", "Bolt": "LIGHTNING", "Peace": "PEACE SIGNS", "SunDisc": "RISING SUNS",
    "RedRing": "ITS HEADBAND", "YellowRing": "MAGIC RINGS", "Spear": "SPEARS", "Arrow": "ARROWS",
    "MoneyBag": "MONEY BAGS", "Pizza": "PIZZA", "MiniChog": "LITTLE CHOGS", "Yarn": "BALLS OF YARN",
    "RainbowBall": "RAINBOWS", "Nose": "CLOWN NOSES", "Bone": "BONES",
    "Fireball": "FIREBALLS", "WaterDrop": "WATER", "Banana": "BANANAS", "Planet": "LITTLE PLANETS",
    "LilyPad": "LILY PADS",
}


def label_of(head_value, skull=False, body_value=None):
    """The line under a hero: what it throws, and from which trait."""
    if not head_value and body_value in ATTACK_OF_BODY:
        return "THROWS " + WORDS[ATTACK_OF_BODY[body_value][0]] + " FROM ITS " + body_value.upper()
    key, _ = attack_of(head_value)
    if not head_value:
        return "THROWS BONES" if skull else "THROWS THE SPIKES OF ITS CROWN"
    if key == "HAT":
        return "THROWS ITS " + head_value.upper()
    return "THROWS " + WORDS[key] + " FROM ITS " + head_value.upper()


def attack_of(head_value):
    """(object key, style) of a value of "Head" ("" or None for no hat)."""
    return ATTACK_OF_HEAD.get(head_value or "", HAT_THROWN)


def attack_of_costume(head_value, body_value):
    """(object key, style) a full costume gives a Genesis without a hat, or None."""
    if head_value:
        return None
    return ATTACK_OF_BODY.get(body_value or "")


# ---------------------------------------------------------------------------------------------
# Shapes.
# ---------------------------------------------------------------------------------------------

def ball(shapes, centre, radius, material, outline=OUTLINE, stacks=8, sides=12, tall=1.0):
    rings = base.ball_rings(centre, Z, X, radius * tall, radius, radius, stacks)
    shapes.shell(rings, sides, material, outline)


def disc(shapes, centre, axis, radius, thick, material, outline=OUTLINE, sides=24):
    """A flat cylinder across "axis", its rim rounded a little."""
    hint = X if abs(axis.normalized().dot(X)) < 0.9 else Y
    stations = [(0.0, radius * 0.92), (thick * 0.2, radius), (thick * 0.8, radius), (thick, radius * 0.92)]
    rings = base.tube_rings(centre - axis.normalized() * thick * 0.5, axis, hint, stations)
    shapes.shell(rings, sides, material, outline)


def torus(shapes, centre, axis, radius, tube, material, outline=OUTLINE, segments=24, sides=8):
    axis = axis.normalized()
    u, v = base.frame_across(axis, X if abs(axis.dot(X)) < 0.9 else Y)
    rings = []
    for index in range(segments + 1):
        angle = 2.0 * math.pi * index / segments
        outward = u * math.cos(angle) + v * math.sin(angle)
        rings.append((centre + outward * radius, axis * tube, outward * tube))
    shapes.shell(rings, sides, material, outline)


def tube(shapes, start, axis, stations, material, outline=OUTLINE, sides=10):
    hint = X if abs(axis.normalized().dot(X)) < 0.9 else Z
    shapes.shell(base.tube_rings(start, axis, hint, stations), sides, material, outline)


def prism(shapes, points, origin, u, v, thick, material, outline=OUTLINE):
    """A flat shape of "points" (x, y in the plane of u and v, in order around it) given a thickness
    along u x v."""
    mesh = shapes.mesh
    normal = u.cross(v).normalized()
    top = [mesh.verts.new(origin + u * a + v * b + normal * thick * 0.5) for a, b in points]
    bottom = [mesh.verts.new(origin + u * a + v * b - normal * thick * 0.5) for a, b in points]
    faces = [mesh.faces.new(top), mesh.faces.new(list(reversed(bottom)))]
    count = len(points)
    for index in range(count):
        after = (index + 1) % count
        faces.append(mesh.faces.new((bottom[index], bottom[after], top[after], top[index])))
    for face in faces:
        face.smooth = False
    shapes.paint(faces, material)
    if outline > 0.0:
        shapes.outlined.append((faces, outline, None))
    return faces


def star_points(outer, inner, tips=5, turn=90.0):
    points = []
    for index in range(tips * 2):
        radius = outer if index % 2 == 0 else inner
        angle = math.radians(turn + 180.0 * index / tips)
        points.append((math.cos(angle) * radius, math.sin(angle) * radius))
    return points


def glyph(shapes, word_pieces, word, centre, u, v, height, material, lift=0.002):
    """A word laid flat on a face, as the prints of the caps are (make_chog_traits.word_pieces):
    "height" is the height of a capital, the plane is u (along the word) and v (up the letters)."""
    pieces = word_pieces(word, 0.02, 1.0)
    capitals = 0.70
    scale = height / capitals
    normal = u.cross(v).normalized()
    mesh = shapes.mesh
    made = {}
    for point in pieces.verts:
        x, y = point.co.x, point.co.y - capitals * 0.5
        made[point] = mesh.verts.new(centre + u * x * scale + v * y * scale + normal * lift)
    faces = []
    for face in pieces.faces:
        new = mesh.faces.new([made[point] for point in face.verts])
        new.smooth = False
        faces.append(new)
    pieces.free()
    shapes.paint(faces, material)
    shapes.open_faces.extend(faces)
    return faces


# ---------------------------------------------------------------------------------------------
# The objects: key -> (colours, unlit colours, builder, colour of the impact).
# ---------------------------------------------------------------------------------------------

def build_snowball(shapes, word_pieces):
    ball(shapes, NOWHERE, 0.13, shapes.named("Snow"))
    for x, y, z, r in ((0.09, -0.06, 0.05, 0.05), (-0.08, 0.04, 0.07, 0.045), (0.02, 0.10, -0.06, 0.05)):
        ball(shapes, Vector((x, y, z)), r, shapes.named("Shade"), outline=0.0, stacks=5, sides=8)


def build_star(shapes, word_pieces):
    prism(shapes, star_points(0.17, 0.075), NOWHERE, X, Y, 0.06, shapes.named("Star"))


def build_cloud(shapes, word_pieces):
    for x, y, z, r in ((0.0, 0.0, 0.03, 0.10), (-0.10, 0.0, -0.01, 0.075), (0.10, 0.0, -0.01, 0.08),
                       (0.04, -0.04, 0.08, 0.065), (-0.05, 0.03, 0.07, 0.06)):
        ball(shapes, Vector((x, y, z)), r, shapes.named("Cloud"), tall=0.85)


def build_ribbon(shapes, word_pieces):
    rings = []
    for index in range(13):
        t = index / 12.0
        centre = Vector(((t - 0.5) * 0.42, 0.0, math.sin(t * math.pi * 2.0) * 0.05))
        twist = t * math.pi * 0.9
        wide = (Y * math.cos(twist) + Z * math.sin(twist)) * 0.055
        thin = (Z * math.cos(twist) - Y * math.sin(twist)) * 0.012
        rings.append((centre, wide, thin))
    shapes.shell(rings, 6, shapes.named("Cloth"), OUTLINE * 0.8)


def build_curl(shapes, word_pieces):
    ball(shapes, NOWHERE, 0.095, shapes.named("Hair"))
    for index in range(7):
        angle = 2.0 * math.pi * index / 7
        at = Vector((math.cos(angle) * 0.085, math.sin(angle) * 0.085, ((index % 3) - 1) * 0.03))
        ball(shapes, at, 0.055, shapes.named("Hair"), outline=OUTLINE * 0.8, stacks=6, sides=9)


def coin(shapes, word_pieces, mark):
    disc(shapes, NOWHERE, Y, 0.14, 0.035, shapes.named("Coin"))
    torus(shapes, NOWHERE, Y, 0.125, 0.012, shapes.named("Rim"), outline=0.0, segments=28, sides=6)
    # The mark on both faces, each read from its own side: along the word u, up the letters Z, and
    # u x Z pointing out of the face.
    for side in (1.0, -1.0):
        glyph(shapes, word_pieces, mark, Y * (0.0175 * side), -X * side, Z, 0.15, shapes.named("Mark"))


def build_coin_dollar(shapes, word_pieces):
    coin(shapes, word_pieces, "$")


def build_coin_bitcoin(shapes, word_pieces):
    coin(shapes, word_pieces, "B")
    # The two short bars of the Bitcoin B, above and below, on each face.
    for side in (1.0, -1.0):
        for dx in (-0.018, 0.018):
            for dz in (0.085, -0.085):
                prism(shapes, ((-0.006, -0.013), (0.006, -0.013), (0.006, 0.013), (-0.006, 0.013)),
                      Vector((dx, 0.019 * side, dz)), X, Z, 0.004, shapes.named("Mark"), outline=0.0)


def build_coin_buttcoin(shapes, word_pieces):
    coin(shapes, word_pieces, "B")


def build_bolt(shapes, word_pieces):
    # Pointing along -Y: the tip of the bolt leads.
    points = ((0.03, 0.20), (-0.07, 0.01), (-0.005, 0.01), (-0.04, -0.20), (0.07, 0.03), (0.005, 0.03))
    prism(shapes, [(x, -y) for x, y in points], NOWHERE, X, Y, 0.05, shapes.named("Bolt"))


def build_peace(shapes, word_pieces):
    torus(shapes, NOWHERE, Z, 0.13, 0.022, shapes.named("Sign"))
    bar = ((-0.016, -0.12), (0.016, -0.12), (0.016, 0.12), (-0.016, 0.12))
    prism(shapes, bar, NOWHERE, X, Y, 0.03, shapes.named("Sign"))
    # The two arms, from the middle down to the ring at 45 degrees on each side.
    for side in (1.0, -1.0):
        along = Vector((math.sin(math.radians(45.0)) * side, -math.cos(math.radians(45.0)), 0.0))
        across = Z.cross(along)
        arm = ((-0.014, 0.0), (0.014, 0.0), (0.014, 0.125), (-0.014, 0.125))
        prism(shapes, arm, NOWHERE, across, along, 0.03, shapes.named("Sign"))


def build_sun_disc(shapes, word_pieces):
    disc(shapes, NOWHERE, Z, 0.14, 0.03, shapes.named("White"))
    for side in (1.0, -1.0):
        disc(shapes, Z * 0.016 * side, Z, 0.065, 0.004, shapes.named("Sun"), outline=0.0, sides=20)


def build_red_ring(shapes, word_pieces):
    torus(shapes, NOWHERE, Z, 0.12, 0.03, shapes.named("Band"))


def build_yellow_ring(shapes, word_pieces):
    torus(shapes, NOWHERE, Z, 0.12, 0.028, shapes.named("Ring"))


def build_spear(shapes, word_pieces):
    tube(shapes, Y * 0.30, FORWARD, [(0.0, 0.014), (0.40, 0.014)], shapes.named("Shaft"), sides=8)
    tube(shapes, Y * -0.10, FORWARD, [(0.0, 0.03), (0.04, 0.045), (0.16, 0.0)], shapes.named("Tip"), sides=6)


def build_arrow(shapes, word_pieces):
    tube(shapes, Y * 0.24, FORWARD, [(0.0, 0.011), (0.40, 0.011)], shapes.named("Shaft"), sides=8)
    tube(shapes, Y * -0.16, FORWARD, [(0.0, 0.026), (0.09, 0.0)], shapes.named("Tip"), sides=6)
    for u in (X, Z):
        vane = ((0.0, 0.0), (0.06, 0.03), (0.06, 0.13), (0.0, 0.10))
        prism(shapes, vane, Y * 0.12, u, Y, 0.006, shapes.named("Feather"), outline=0.006)
        prism(shapes, [(-a, b) for a, b in vane], Y * 0.12, u, Y, 0.006, shapes.named("Feather"), outline=0.006)


def build_money_bag(shapes, word_pieces):
    ball(shapes, Vector((0.0, 0.0, -0.03)), 0.12, shapes.named("Bag"), tall=0.95)
    tube(shapes, Z * 0.06, Z, [(0.0, 0.05), (0.06, 0.035), (0.09, 0.06)], shapes.named("Bag"), sides=10)
    torus(shapes, Z * 0.085, Z, 0.042, 0.012, shapes.named("Tie"), segments=16, sides=6)
    glyph(shapes, word_pieces, "$", Vector((0.0, -0.118, -0.03)), X, Z, 0.11, shapes.named("Mark"))


def build_pizza(shapes, word_pieces):
    slice_points = ((0.0, -0.17), (0.12, 0.10), (-0.12, 0.10))
    prism(shapes, slice_points, NOWHERE, X, Y, 0.025, shapes.named("Cheese"))
    tube(shapes, Vector((-0.13, 0.11, 0.0)), X, [(0.0, 0.022), (0.26, 0.022)], shapes.named("Crust"), sides=8)
    for x, y in ((0.0, 0.03), (-0.04, -0.04), (0.045, 0.06)):
        disc(shapes, Vector((x, y, 0.014)), Z, 0.022, 0.006, shapes.named("Pepperoni"), outline=0.0, sides=12)


def build_mini_chog(shapes, word_pieces):
    ball(shapes, NOWHERE, 0.09, shapes.named("Skin"))
    for index in range(8):
        angle = 2.0 * math.pi * index / 8 + 0.3
        away = Vector((math.cos(angle), 0.35, math.sin(angle))).normalized()
        tube(shapes, away * 0.07, away, [(0.0, 0.035), (0.07, 0.0)], shapes.named("Skin"), outline=OUTLINE * 0.8, sides=6)
    disc(shapes, Vector((0.0, -0.07, -0.005)), Y, 0.055, 0.03, shapes.named("Face"), outline=0.0, sides=16)
    for x in (-0.02, 0.02):
        disc(shapes, Vector((x, -0.087, 0.005)), Y, 0.009, 0.004, shapes.named("Eye"), outline=0.0, sides=8)


def build_yarn(shapes, word_pieces):
    ball(shapes, NOWHERE, 0.12, shapes.named("Wool"))
    for index, angle in enumerate((-50.0, 0.0, 50.0)):
        axis = Vector((math.sin(math.radians(angle)), 0.0, math.cos(math.radians(angle))))
        torus(shapes, NOWHERE, axis, 0.118, 0.012, shapes.named("Stripe"), outline=0.0, segments=24, sides=5)


def build_rainbow_ball(shapes, word_pieces):
    bands = ("Red", "Orange", "Yellow", "Green", "Blue", "Purple")
    radius = 0.13
    for index, band in enumerate(bands):
        low = -radius + 2.0 * radius * index / len(bands)
        high = -radius + 2.0 * radius * (index + 1) / len(bands)
        stations = []
        for step in range(4):
            z = low + (high - low) * step / 3.0
            stations.append((z - low, math.sqrt(max(radius * radius - z * z, 0.0001))))
        tube(shapes, Z * low, Z, stations, shapes.named(band), outline=OUTLINE, sides=14)


def build_nose(shapes, word_pieces):
    ball(shapes, NOWHERE, 0.11, shapes.named("Red"))
    ball(shapes, Vector((0.035, -0.07, 0.05)), 0.025, shapes.named("Glint"), outline=0.0, stacks=4, sides=6)


def build_bone(shapes, word_pieces):
    """A bone, for a skull without a hat (it has no spike to throw): a shaft and two knobs at each end."""
    tube(shapes, Vector((-0.12, 0.0, 0.0)), X, [(0.0, 0.024), (0.24, 0.024)], shapes.named("Bone"), sides=10)
    for end in (-0.13, 0.13):
        for offset in (-0.026, 0.026):
            ball(shapes, Vector((end, offset, 0.0)), 0.034, shapes.named("Bone"), stacks=6, sides=10)


def build_leaf(shapes, word_pieces):
    """A leaf of the crown, as add_leaf_crown makes one, pointing along -Y, in the skin of the hero."""
    profile = ((0.00, 0.90, 0.92), (0.12, 0.99, 1.00), (0.25, 1.00, 0.95), (0.46, 0.79, 0.70),
               (0.69, 0.49, 0.42), (0.89, 0.19, 0.17))
    length, width, thick = 0.34, 0.15, 0.10
    root = Y * (length * 0.45)
    curl = Z * length * 0.07
    rings = []
    for along, wider, thicker in profile:
        rings.append((root + FORWARD * length * along + curl * along * along, X * width * 0.5 * wider,
                      Z * thick * 0.5 * thicker))
    rings.append((root + FORWARD * length + curl, NOWHERE, NOWHERE))
    shapes.shell(rings, 6, SKIN, OUTLINE)


def build_fireball(shapes, word_pieces):
    """The fire a dragon breathes, pointing along -Y: a hot yellow head and a flame trailing behind it in
    tongues of orange and red (drawn unlit, it glows)."""
    tube(shapes, Y * -0.12, -FORWARD, [(0.0, 0.03), (0.03, 0.085), (0.08, 0.105), (0.15, 0.085), (0.24, 0.045),
                                        (0.32, 0.0)], shapes.named("Flame"), sides=12)
    ball(shapes, Y * -0.075, 0.075, shapes.named("Core"), outline=0.0, stacks=6, sides=10)
    for angle in (90.0, 210.0, 330.0):
        out = X * math.cos(math.radians(angle)) + Z * math.sin(math.radians(angle))
        axis = (-FORWARD + out * 0.55).normalized()
        tube(shapes, Y * 0.02 + out * 0.05, axis, [(0.0, 0.04), (0.10, 0.025), (0.19, 0.0)],
             shapes.named("Tongue"), outline=OUTLINE * 0.8, sides=8)


def build_water_drop(shapes, word_pieces):
    """A ball of sea water, pointing along -Y: round in front, drawn out behind, a glint and two drops."""
    tube(shapes, Y * -0.12, -FORWARD, [(0.0, 0.03), (0.03, 0.08), (0.09, 0.105), (0.16, 0.08), (0.25, 0.035),
                                        (0.30, 0.0)], shapes.named("Water"), sides=12)
    ball(shapes, Vector((0.045, -0.06, 0.06)), 0.028, shapes.named("Glint"), outline=0.0, stacks=4, sides=6)
    for x, y, z, r in ((0.07, 0.24, 0.03, 0.03), (-0.06, 0.28, -0.02, 0.022)):
        ball(shapes, Vector((x, y, z)), r, shapes.named("Water"), outline=OUTLINE * 0.7, stacks=5, sides=8)


def build_banana(shapes, word_pieces):
    """A banana: a curved, slightly squared fruit, thin at both ends, a brown stem and a dark tip."""
    rings = []
    radius, sweep, steps = 0.22, math.radians(110.0), 12
    for index in range(steps + 1):
        t = index / steps
        angle = -sweep * 0.5 + sweep * t
        centre = Vector((math.sin(angle) * radius, 0.0, (math.cos(angle) - 1.0) * radius + radius * 0.16))
        outward = Vector((math.sin(angle), 0.0, math.cos(angle)))
        thick = 0.012 + 0.046 * math.sin(math.pi * min(1.0, max(0.0, 0.08 + t * 0.84)))
        rings.append((centre, outward * thick, Y * thick * 0.92))
    shapes.shell(rings, 6, shapes.named("Peel"), OUTLINE)
    start = Vector((math.sin(-sweep * 0.5) * radius, 0.0, (math.cos(sweep * 0.5) - 1.0) * radius + radius * 0.16))
    along = Vector((-math.cos(sweep * 0.5), 0.0, -math.sin(sweep * 0.5)))
    tube(shapes, start, along, [(0.0, 0.014), (0.045, 0.012)], shapes.named("Stem"), sides=6)
    end = Vector((-start.x, 0.0, start.z))
    ball(shapes, end, 0.014, shapes.named("Stem"), outline=0.0, stacks=4, sides=6)


def build_planet(shapes, word_pieces):
    """A little planet from space, as an astronaut would bring back: a ball and its tilted ring."""
    ball(shapes, NOWHERE, 0.10, shapes.named("Planet"))
    for x, y, z, r in ((0.04, -0.08, 0.04, 0.022), (-0.05, -0.07, -0.03, 0.016)):
        ball(shapes, Vector((x, y, z)), r, shapes.named("Crater"), outline=0.0, stacks=4, sides=6)
    tilt = Vector((0.35, 0.0, 1.0)).normalized()
    torus(shapes, NOWHERE, tilt, 0.165, 0.016, shapes.named("Ring"), segments=28, sides=6)


def build_lily_pad(shapes, word_pieces):
    """The pad a toad sits on: a round green leaf with its notch, and a pink flower on it."""
    points = [(0.0, 0.0)]
    for index in range(19):
        angle = math.radians(105.0 + 330.0 * index / 18)
        points.append((math.cos(angle) * 0.16, math.sin(angle) * 0.16))
    prism(shapes, points, NOWHERE, X, Y, 0.025, shapes.named("Pad"))
    for index in range(5):
        angle = 2.0 * math.pi * index / 5
        at = Vector((math.cos(angle) * 0.03 - 0.02, math.sin(angle) * 0.03 - 0.03, 0.035))
        ball(shapes, at, 0.026, shapes.named("Flower"), outline=OUTLINE * 0.6, stacks=4, sides=6, tall=0.7)
    ball(shapes, Vector((-0.02, -0.03, 0.05)), 0.016, shapes.named("Heart"), outline=0.0, stacks=4, sides=6)


# key -> (colours as seen on screen, keys drawn unlit, builder, the colour of what is left where it hits).
OBJECTS = {
    "Snowball": ({"Snow": (0.96, 0.98, 1.0), "Shade": (0.80, 0.88, 0.97)}, (), build_snowball, (0.90, 0.95, 1.0)),
    "StarYellow": ({"Star": (1.0, 0.83, 0.20)}, (), build_star, (1.0, 0.83, 0.20)),
    "StarWhite": ({"Star": (0.98, 0.98, 0.95)}, (), build_star, (0.98, 0.98, 0.95)),
    "Cloud": ({"Cloud": (0.98, 0.98, 1.0)}, (), build_cloud, (0.75, 0.85, 1.0)),
    "Ribbon": ({"Cloth": (0.25, 0.25, 0.29)}, (), build_ribbon, (0.40, 0.40, 0.46)),
    "Curl": ({"Hair": (0.97, 0.77, 0.19)}, (), build_curl, (0.97, 0.77, 0.19)),
    "CoinDollar": ({"Coin": (0.98, 0.76, 0.20), "Rim": (0.85, 0.58, 0.12), "Mark": (0.62, 0.38, 0.06)}, (),
                   build_coin_dollar, (0.98, 0.76, 0.20)),
    "CoinBitcoin": ({"Coin": (0.97, 0.58, 0.10), "Rim": (0.82, 0.44, 0.05), "Mark": (1.0, 1.0, 1.0)}, ("Mark",),
                    build_coin_bitcoin, (0.97, 0.58, 0.10)),
    "CoinButtcoin": ({"Coin": (0.95, 0.55, 0.20), "Rim": (0.80, 0.40, 0.12), "Mark": (1.0, 1.0, 1.0)}, ("Mark",),
                     build_coin_buttcoin, (0.95, 0.55, 0.20)),
    "Bolt": ({"Bolt": (1.0, 0.85, 0.12)}, ("Bolt",), build_bolt, (1.0, 0.92, 0.35)),
    "Peace": ({"Sign": (0.98, 0.98, 0.97)}, (), build_peace, (0.98, 0.98, 0.97)),
    "SunDisc": ({"White": (0.98, 0.98, 0.97), "Sun": (0.86, 0.12, 0.16)}, (), build_sun_disc, (0.86, 0.12, 0.16)),
    "RedRing": ({"Band": (0.86, 0.13, 0.15)}, (), build_red_ring, (0.86, 0.13, 0.15)),
    "YellowRing": ({"Ring": (1.0, 0.84, 0.22)}, (), build_yellow_ring, (1.0, 0.84, 0.22)),
    "Spear": ({"Shaft": (0.55, 0.36, 0.18), "Tip": (0.86, 0.66, 0.30)}, (), build_spear, (0.95, 0.80, 0.45)),
    "Arrow": ({"Shaft": (0.80, 0.64, 0.46), "Tip": (0.35, 0.35, 0.40), "Feather": (0.10, 0.10, 0.12)}, (),
              build_arrow, (0.20, 0.20, 0.22)),
    "MoneyBag": ({"Bag": (0.80, 0.66, 0.42), "Tie": (0.45, 0.30, 0.16), "Mark": (0.18, 0.42, 0.22)}, (),
                 build_money_bag, (0.98, 0.76, 0.20)),
    "Pizza": ({"Cheese": (0.98, 0.80, 0.35), "Crust": (0.80, 0.52, 0.22), "Pepperoni": (0.78, 0.20, 0.16)}, (),
              build_pizza, (0.98, 0.80, 0.35)),
    "MiniChog": ({"Skin": (0.72, 0.65, 0.95), "Face": (0.99, 0.96, 0.85), "Eye": (0.08, 0.06, 0.10)}, ("Eye",),
                 build_mini_chog, (0.72, 0.65, 0.95)),
    "Yarn": ({"Wool": (0.50, 0.78, 0.94), "Stripe": (0.48, 0.36, 0.92)}, (), build_yarn, (0.50, 0.78, 0.94)),
    "RainbowBall": ({"Red": (0.90, 0.16, 0.16), "Orange": (0.97, 0.55, 0.12), "Yellow": (0.98, 0.86, 0.18),
                     "Green": (0.24, 0.75, 0.30), "Blue": (0.20, 0.50, 0.92), "Purple": (0.55, 0.28, 0.82)}, (),
                    build_rainbow_ball, (0.98, 0.86, 0.18)),
    "Nose": ({"Red": (0.91, 0.15, 0.17), "Glint": (1.0, 1.0, 1.0)}, ("Glint",), build_nose, (0.91, 0.15, 0.17)),
    "Bone": ({"Bone": (0.95, 0.94, 0.88)}, (), build_bone, (0.95, 0.94, 0.88)),
    "Fireball": ({"Flame": (1.0, 0.50, 0.10), "Core": (1.0, 0.93, 0.45), "Tongue": (0.93, 0.22, 0.10)},
                 ("Flame", "Core", "Tongue"), build_fireball, (1.0, 0.55, 0.15)),
    "WaterDrop": ({"Water": (0.30, 0.66, 0.98), "Glint": (1.0, 1.0, 1.0)}, ("Glint",), build_water_drop,
                  (0.45, 0.75, 1.0)),
    "Banana": ({"Peel": (1.0, 0.85, 0.22), "Stem": (0.40, 0.26, 0.12)}, (), build_banana, (1.0, 0.88, 0.35)),
    "Planet": ({"Planet": (0.98, 0.60, 0.30), "Crater": (0.85, 0.45, 0.22), "Ring": (0.96, 0.92, 0.78)}, (),
               build_planet, (0.98, 0.92, 0.78)),
    "LilyPad": ({"Pad": (0.30, 0.72, 0.35), "Flower": (0.98, 0.62, 0.78), "Heart": (1.0, 0.86, 0.30)}, (),
                build_lily_pad, (0.30, 0.72, 0.35)),
}
