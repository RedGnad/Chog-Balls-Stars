"""Builds a complete Chog on the polished H3 head and dresses it with the traits of one Genesis, each
trait as its own 3D part. Pictures only: nothing here goes into the game.

Run it without the interface of Blender (5.2):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/make_chog_traits.py -- --genesis 1287 [--out FOLDER]
    ... -- --base                             the polished base alone, no trait part
    ... -- --genesis 1287 --hat "MMGA Cap"    the same hero under another entry of HATS
    ... -- --genesis 1287 --set BRIM_LENGTH=0.13 --set BRIM_RISE_DEG=-9 --out FOLDER
                                              the same hero, numbers of its hat replaced (to try some)
    ... -- --coverage                         build nothing: how many Genesis each entry serves, and how
                                              many have their hat, eyes, mouth (and all three) right
    ... -- --budget                           build each entry once and add up the triangles of every
                                              dressed hero on the base Origin (limit 8,000)

It writes ChogBody.blend, ChogBody.fbx and parts.json (what was built, with its triangles) into
.artifacts/chog-body/traits/<number>/ (or traits/base/). Look at the result with
render_chog_traits.py, then lay the pictures on one sheet with make_chog_traits_sheet.py, and the
heroes side by side with make_chog_traits_overview.py.

The base is head H3 of make_chog_full.py, which the owner chose. What changes:

- the blades of the crown keep their places, lengths, widths and thicknesses, and become leaves: a
  round section in place of a diamond, shaded smooth, widest a quarter of the way up, with a blunt
  point that curls back a little. The official drawings (.artifacts/chog-reference/body.png,
  site_tools_jump.png) show soft leaves, not cut gems;
- the face carries no eyes and no mouth. In the collection Eyes and Mouth are traits, so they are
  parts (below). The base keeps the cream face, its dark line, the pink nose, the two pink cheeks
  and the teeth of hair on the brow;
- the cream face is wider (0.25 from its middle in place of 0.225, and wider still at the brow), as
  the pictures of the collection draw it: from one row of spikes to the other. The head under it,
  the leaves and so the outline of the character are those of H3.

A trait part is one object, child of "Head" (or of "Body", "armLeft", "armRight" for clothing) with
no move, turn or scale of its own, so it follows the part it sits on. Its name says what it is and
its materials carry the name of its trait:

    Hat_<Shape>    one shape serves every hat of that shape: it takes a colour set and, optionally, a
                   print or a mark, and its own numbers (how long the brim is, how far it rises).
                   HATS maps a value of the trait "Head" to all that.
    Eyes_<Kind>    EYES maps a value of the trait "Eyes" to a kind and its colours.
    Mouth_<Kind>   MOUTHS maps a value of the trait "Mouth" to a kind and its colours.
    Cloth_<Kind>   CLOTHES maps a value of the trait "Body" to a kind and its colours. A first pass:
                   a shirt is a thin shell over the torso (Cloth_<Kind>, child of "Body") and one
                   over each arm (Cloth_<Kind>_armLeft, Cloth_<Kind>_armRight).

The traits of a Genesis are read from research/evidence/genesis-metadata.jsonl; a value with no entry
below is said (NOT MODELLED) and left out.

The crown of leaves follows the hat (15 October 2026): the numbers of each hat shape say which leaves
stay, which are moved out from under its rim, and which are removed ("LEAVES", chog_trait_hats.leaf);
hair and hoods replace the whole crown. A removed leaf takes its outline shell with it. With no hat, or
a hat that is not modelled, the crown is whole. leaf_crossings() checks every build: a leaf that goes
through its hat fails it.

Things that have volume (hats, glasses, a visor, a bubble, a stream, beams) are shells with the dark
outline of the model, the shared material "ChogOutline". Things that are drawn (eyes, mouths) are
flat coloured shapes laid on the curved face, made of geometry, never of an image.

The four rigid parts the game animates ("Body", "Head", "armLeft", "armRight") and their origins are
those of make_chog_full.py, checked by its report.

Lettering: the word of a print is drawn with the font built into Blender ("Bfont Regular", no file
of this Mac), thickened, cut into small pieces and laid on the panel. Colours: measured on the
1000-point pictures of the Genesis (.artifacts/genesis-art/1000/), given here as they look on the
screen.
"""

import argparse
import ast
import json
import math
import os
import sys
from types import SimpleNamespace

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import make_chog_full as full  # noqa: E402  (the folder of this file must be on the path first)
from render_chog_body import linear  # noqa: E402
import chog_trait_faces as faces  # noqa: E402  (eyes and mouths added on 14 October 2026, registered below)

variants, base = full.variants, full.base
X, Y, Z, NOWHERE = base.X, base.Y, base.Z, base.NOWHERE
at = base.at
SKIN, OUTLINE = full.SKIN, full.OUTLINE

REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
METADATA = os.path.join(REPOSITORY, "research", "evidence", "genesis-metadata.jsonl")
TRAITS_FOLDER = os.path.join(REPOSITORY, ".artifacts", "chog-body", "traits")

TRIANGLE_LIMIT = 8000        # one dressed hero, outline shells included

# ---------------------------------------------------------------------------------------------
# The polished base: head H3, and what replaces its numbers.
# ---------------------------------------------------------------------------------------------
POLISH = {
    # A leaf from its root to its point: where along its length, then its width and its thickness
    # against the largest. The last line is the blunt point.
    "LEAF_PROFILE": (
        (0.00, 0.90, 0.92),
        (0.12, 0.99, 1.00),
        (0.25, 1.00, 0.95),
        (0.46, 0.79, 0.70),
        (0.69, 0.49, 0.42),
        (0.89, 0.19, 0.17),
    ),
    "LEAF_SIDES": 6,             # points around a leaf: its section is an oval
    "LEAF_CURL": 0.07,           # how far the point bends back, against the length
    # Every leaf of the crown against the length H3 gives it (SPIKES), and against its width and
    # thickness. (1.0 until batch 4, 15 October 2026: the owner found the spikes a little long. Tried on
    # the busts of bare heads beside their pictures, #75, #112 and #76, at 1.0, 0.85, 0.75, 0.70 and
    # 0.65: the pictures draw shorter spikes still, small points down the far side; 0.70 makes those
    # points and keeps a crown that reads from the camera of the game. A leaf starts 0.10 inside the
    # head (SPIKE_SUNK), so what stands out of it is 0.08 to 0.15 long in place of 0.16 to 0.26.)
    "LEAF_LENGTH": 0.70,
    "LEAF_WIDTH": 1.0,
    # The nose is a little smaller than on H3: it has to show between a pair of glasses and a grin.
    "NOSE": (0.0, -0.042, 0.018, 0.012),
    # The face spans the head as the pictures draw it, more so at the brow; the cheeks and the teeth
    # of hair follow. (H3: 0.225 wide from its middle, 0.10 wider at the brow.)
    "FACE_HALF_WIDTH": 0.250,
    "FACE_TAPER": 0.22,
    "CHEEK": (0.180, -0.062, 0.046, 0.032),
    "FRINGE_WIDTH": 0.088,
    "FRINGE_DROP": 0.050,
}

# The leaves a hat leaves: each hat shape says it in its numbers ("LEAVES"); what an item means is
# said at leaf() in chog_trait_hats.py (15 October 2026). leaf_crossings() below checks the result.
import chog_trait_hats as hats_and_clothes  # noqa: E402
leaf, WHOLE, GONE = hats_and_clothes.leaf, hats_and_clothes.WHOLE, hats_and_clothes.GONE
FULL_CROWN, NO_CROWN = hats_and_clothes.FULL_CROWN, hats_and_clothes.NO_CROWN

# What a Genesis looks like besides its parts. The colour of each value of "Skin", and of the face
# when it is not the cream of the collection (less yellow than the cream of the official drawings).
SKINS = {
    "Red": {"skin": (0.95, 0.05, 0.05)},
    "Pink": {"skin": (0.95, 0.45, 0.68)},
    "Yellow": {"skin": (0.95, 0.82, 0.10)},
    "Origin": {"skin": (0.36, 0.21, 0.69)},
    "Black": {"skin": (0.17, 0.17, 0.18), "face": (0.36, 0.18, 0.11)},
}
GENESIS_FACE = (0.94, 0.91, 0.85)
GENESIS_BLUSH = (0.83, 0.27, 0.50)
# The flat colour behind a bust is the one recorded for the value of "Background" in this file,
# which derive_trait_colors.py of tools/genesis-art wrote (it is not part of the repository).
BACKGROUNDS = os.path.join(REPOSITORY, ".artifacts", "genesis-art", "background-colours.json")


def background_colour(value):
    """The recorded colour of a value of "Background", as it looks on the screen, or None."""
    if not value or not os.path.isfile(BACKGROUNDS):
        return None
    with open(BACKGROUNDS, encoding="utf-8") as source:
        found = json.load(source).get("backgrounds", {}).get(value)
    return tuple(channel / 255.0 for channel in found[0]) if found else None

# The lifts of what is drawn on the face, off the head: above the cheeks of the base (0.0216).
DRAWN = (0.0245, 0.0277, 0.0307, 0.0337, 0.0367, 0.0397)

# ---------------------------------------------------------------------------------------------
# Hats. A shape is built once; a hat of the collection is a shape, a colour set, a print or a mark,
# and the numbers it replaces ("numbers"), first of all those of its brim.
# ---------------------------------------------------------------------------------------------
# What the camera of the game asks of a hat: it looks down from 73 degrees and the head of H3 looks
# up by 20, so anything that reaches forward over the brow hides the face under it. A brim is
# therefore short and rises towards its edge (BRIM_LENGTH, BRIM_RISE_DEG below, to be set per hat).

# The cap, in the axes of "Head" before the head is turned up, heights counted from the middle of
# the head. Its crown is a dome whose front leans back as one flat panel, where the print goes.
CAP = {
    "RIM_HALF_WIDTH": 0.318,
    "RIM_HALF_DEPTH": 0.282,
    "SQUARENESS": 2.9,           # 2 is an oval seen from above, more is squarer: a flatter front
    # One ring per line, from the rim up: height, size against the rim, how far it is moved back.
    "RINGS": (
        (0.040, 0.99, 0.000),
        (0.092, 1.00, 0.004),
        (0.236, 0.91, 0.030),
        (0.300, 0.77, 0.050),
        (0.345, 0.50, 0.064),
    ),
    "TOP": (0.362, 0.070),       # height of the top, how far it is moved back
    "SIDES": 16,                 # (20 until 15 October 2026: lowered for the triangles of the prints)
    # The button on the top: half its width, half its height.
    "BUTTON": (0.032, 0.014),
    # The brim: at which height it leaves the crown, how far it reaches in its middle, how far it
    # goes around the crown on each side of the front (degrees), how much wider than the crown its
    # edge is, its thickness, how much it rises towards its edge (degrees, negative: it dips), how
    # much lower its ends are than its middle.
    # (First: 0.150 long, dipping by 4 degrees, as wide as the crown; seen from the front it was a
    # thin dark line. Then 0.130 long, dipping by 9 degrees, wider than the crown: a broad band as
    # on the picture of CHOG #1287, but from the camera of the game it hid the whole face. Then four
    # short brims that rise, 0.070 by 20 degrees, 0.065 by 28, 0.060 by 35 and 0.050 by 30: all let
    # the lenses show from above, the third the most of them while the bust still reads as a cap.)
    "BRIM_HEIGHT": 0.062,
    "BRIM_LENGTH": 0.060,
    "BRIM_AROUND_DEG": 70.0,
    "BRIM_WIDER": 1.13,
    "BRIM_THICKNESS": 0.034,
    "BRIM_RISE_DEG": 35.0,
    "BRIM_DROOP": 0.036,
    "BRIM_SECTIONS": 9,
    # How much of the outline each point of a section gets: on the top at the crown, half way, at
    # the edge, then under the brim at the edge, half way, at the crown.
    "BRIM_OUTLINE_SHARES": (0.0, 0.0, 1.0, 1.0, 0.3, 0.0),
    # The print: height of its capitals, height of the line they stand on, how much the strokes are
    # thickened and how much more for the dark line around them (in units of the font, whose
    # capitals are 0.70 tall), the space between letters against the usual, how far each layer
    # stands off the panel, and the width of the pieces the word is cut into to follow the panel.
    # (The word stood on 0.110 and was 0.104 tall; a brim that rises then hid its foot from the front.)
    "PRINT_CAPITALS": 0.096,
    "PRINT_BASELINE": 0.124,
    "PRINT_BOLD": 0.040,
    "PRINT_LINE": 0.040,
    "PRINT_SPACING": 1.22,
    "PRINT_LIFT": (0.004, 0.008),
    # (0.035 until 15 October 2026. With a word on every cap, a cap with a long word and its dark line
    # reached 1,276 triangles and 40 dressed heroes went over 8,000. At 0.070 a piece of the word sags
    # under the curve of the panel by about 0.002, half the lift of the lowest layer: it stays above.)
    "PRINT_CUT": 0.070,
    # A longer print is made smaller: no wider than PRINT_FIT across the panel, its lines no taller
    # together than PRINT_BLOCK, the baselines of two lines PRINT_LEADING capitals apart.
    # (Added 15 October 2026 for the words of the other caps: "BUTTCOIN" or "MAKE CHOG / GREAT AGAIN" at
    # the size of "TISM" ran round the sides of the crown.)
    # (0.44 until 15 October 2026: Buttcoin, RETARD, NEET ran round the corners of the crown, said the
    # owner; the flat front of the crown is about 0.34 wide where the word stands.)
    "PRINT_FIT": 0.34,
    "PRINT_BLOCK": 0.150,
    "PRINT_LEADING": 1.35,
    # A logo in place of a word (chog_trait_hats.py, lay_logo): the height of its middle, half its size.
    "LOGO": (0.185, 0.060),
    "OUTLINE_THICKNESS": 0.020,
    # The leaves under the cap (chog_trait_hats.py, leaf()). The pictures of Side=Right draw two or three
    # spikes beside and behind the cap, under the line of its rim, none above it; those of Side=Left are
    # framed on the face and show none. The crown covers the head above 81 degrees: the top, the upper
    # pairs and the top of the back row are removed; the lowest pair of the star, the pair of the back
    # row and the leaf straight back stay, each turned down just enough to come out under the rim.
    "LEAVES": (GONE, GONE, GONE, leaf(1.3, 116.0, None, 0.35), GONE, leaf(1.3, 106.0, 40.0, 0.35),
               leaf(1.3, None, 104.0, 0.35)),
}
FONT_CAPITALS = 0.70             # height of a capital of "Bfont Regular" at size 1, measured

# The bucket hat: a soft round crown and a brim all around it. The brim dips at the sides and at
# the back as the pictures draw it; at the front it is shorter and rises, for the camera of the game.
BUCKET = {
    "RIM_HALF_WIDTH": 0.316,
    "RIM_HALF_DEPTH": 0.281,
    "SQUARENESS": 2.25,
    "RINGS": (
        (0.062, 0.99, 0.000),
        (0.118, 1.00, 0.004),
        (0.218, 0.93, 0.018),
        (0.288, 0.74, 0.034),
        (0.328, 0.42, 0.046),
    ),
    "TOP": (0.341, 0.050),
    "SIDES": 16,
    "BRIM_HEIGHT": 0.094,
    "BRIM_THICKNESS": 0.026,
    # (First: 0.050 long at the front, rising by 8 degrees. From the camera of the game it still hid
    # the eyes drawn on the face, which lie further back than a pair of glasses. Turned up against
    # the crown, it hides nothing, and from the front it is still a band between crown and eyes.)
    "BRIM_LENGTH": 0.080,        # at the sides and at the back
    "BRIM_DIP_DEG": 30.0,
    "BRIM_FRONT_LENGTH": 0.066,  # in the middle of the front
    "BRIM_FRONT_RISE_DEG": 52.0,
    "BRIM_FRONT_SPREAD": 2.0,    # how narrow the raised front is: more is narrower
    # The mark on the front of the crown: its height, and half its width.
    "MARK_HEIGHT": 0.185,
    "MARK_HALF_WIDTH": 0.060,
    "MARK_LIFT": 0.005,
    "OUTLINE_THICKNESS": 0.020,
    # The pictures draw two or three spikes under the brim, beside and behind the hat. Its crown covers
    # the head as the cap does: the same leaves stay, under the brim that dips at the sides and back.
    "LEAVES": (GONE, GONE, GONE, leaf(1.3, 116.0, None, 0.35), GONE, leaf(1.3, 106.0, 40.0, 0.35),
               leaf(1.3, None, 104.0, 0.35)),
}

# The crown: a band around the top of the head that flares, eight points, gems on the band. It is
# open at the top. (Until 15 October 2026 the leaves of the head stood inside it and came out above
# its points: from the camera of the game, a crown pierced by spikes. The pictures show the crown on
# the top of the head and the spikes beside and behind it, below its band.)
CROWN = {
    "RIM_HALF_WIDTH": 0.318,
    "RIM_HALF_DEPTH": 0.283,
    "SQUARENESS": 2.2,
    # From the rim up: the rim, the top of the line of trim, the top of the band, the tips. It
    # flares, and leans back as it rises: seen from the camera of the game its front then stands
    # behind the brow in place of over it. (First: moved back by 0.012 and 0.024 only; from above
    # the flared front covered the eyes.)
    "RINGS": (
        (0.070, 1.000, 0.000),
        (0.094, 1.006, 0.002),
        (0.190, 1.060, 0.030),
        (0.305, 1.110, 0.056),
    ),
    "SIDES": 16,                 # twice the number of points
    "VALLEY": 0.208,             # height of the dips between the points
    # The floor inside the crown, a low dome just above the top of the head (0.25 above its middle):
    # the height and size of the ring where it meets the inside of the band, of its middle ring, and the
    # height of its top. In the darker gold of the trim. (Until batch 4, 15 October 2026, a flat floor at
    # 0.150, hidden in the head: from the camera of the game the bare top of the head showed inside the
    # crown, as a pink or yellow bowl. The pictures never show the inside of the crown: the floor is
    # gold, the colour of the crown, to show nothing that is not drawn.) None: no floor. (Since 15 October
    # 2026, after batch 4: from above the gold floor made the crown a pie. The crown is a band on a spiky
    # head: inside it stand the short tips of the leaves of the top, as the pictures draw spikes behind
    # its points.)
    "FLOOR": None,
    "BAND_INSIDE": 0.95,         # with no floor: how large the inside of the band is against its outside
    # The gems: where around the crown (degrees from the front, towards the left of the character),
    # at which height, half width, half height, and which material.
    "GEMS": (
        (0.0, 0.140, 0.040, 0.050, "Trim"),
        (0.0, 0.140, 0.029, 0.038, "GemRed"),
        (38.0, 0.132, 0.014, 0.014, "GemBlue"),
        (-38.0, 0.132, 0.014, 0.014, "GemBlue"),
        (76.0, 0.140, 0.014, 0.014, "GemBlue"),
        (-76.0, 0.140, 0.014, 0.014, "GemBlue"),
        (124.0, 0.140, 0.014, 0.014, "GemBlue"),
        (-124.0, 0.140, 0.014, 0.014, "GemBlue"),
        (180.0, 0.140, 0.020, 0.024, "GemRed"),
    ),
    "GEM_LIFT": (0.005, 0.010),  # the rim of a gem and its middle, off the band
    "OUTLINE_THICKNESS": 0.020,
    # No leaf inside the crown; the lowest pair of the star, the pair of the back row and the leaf
    # straight back stay, turned down to come out under its rim (which is higher than that of a cap).
    "LEAVES": (GONE, GONE, GONE, leaf(1.3, 110.0, None, 0.30), GONE, leaf(1.3, 96.0, 40.0, 0.30),
               leaf(1.3, None, 96.0, 0.30)),
}

# The beanie: a dome and a thick cuff around the brow, rings printed on the dome.
BEANIE = {
    "RIM_HALF_WIDTH": 0.314,
    "RIM_HALF_DEPTH": 0.279,
    "SQUARENESS": 2.15,
    "RINGS": (
        (0.066, 1.000, 0.000),
        (0.140, 1.000, 0.005),
        (0.225, 0.910, 0.020),
        (0.295, 0.700, 0.036),
        (0.335, 0.400, 0.048),
    ),
    "TOP": (0.352, 0.052),
    "SIDES": 16,
    # The cuff, a shell of its own around the foot of the dome, so that the dark line of the model
    # runs along its upper edge: its lower and upper edge, how far it stands off the dome (against
    # the size of the rim), how much more in its middle. (First: a step in the dome itself, 0.05
    # wider. It did not show: the picture was a smooth swimming cap.)
    "CUFF": (0.058, 0.148, 0.062, 0.026),
    # The ribs of the cuff: how many around, the width of one, how far it stands off the cuff.
    "RIBS": (24, 0.007, 0.004),
    # The rings printed on the dome: degrees from the front, height, radius.
    "PRINTS": (
        (-26.0, 0.190, 0.030), (26.0, 0.205, 0.030), (70.0, 0.190, 0.028), (-74.0, 0.200, 0.028),
        (0.0, 0.268, 0.027), (-50.0, 0.266, 0.025), (48.0, 0.272, 0.025), (116.0, 0.205, 0.028),
        (-120.0, 0.208, 0.028), (164.0, 0.215, 0.028),
    ),
    "PRINT_INSIDE": 0.56,        # the hole of a ring, against its radius
    "PRINT_LIFT": 0.005,
    "OUTLINE_THICKNESS": 0.020,
    # The beanies are worn on Side=Left only, whose pictures show no spike: the beanie covers the head to
    # the brow. In 3D the leaves left are those under a cap, out under the cuff (the hero is seen from
    # above in the game, where a head with no spike at all no longer reads as a Chog).
    "LEAVES": (GONE, GONE, GONE, leaf(1.3, 116.0, None, 0.35), GONE, leaf(1.3, 106.0, 40.0, 0.35),
               leaf(1.3, None, 104.0, 0.35)),
}

# value of the trait "Head": shape, colours of its materials, print or mark, numbers it replaces.
HATS = {
    "TISM Cap": {
        "shape": "Cap",
        "colours": {
            "Crown": (0.89, 0.16, 0.24),
            "Brim": (0.19, 0.18, 0.25),
            "Button": (0.19, 0.18, 0.25),
            "Print": (0.95, 0.95, 0.95),
            "PrintLine": (0.06, 0.05, 0.06),
        },
        "print": "TISM",
        "numbers": {"BRIM_LENGTH": 0.060, "BRIM_RISE_DEG": 35.0},
    },
    # Not finished art: here to check that the same shape takes another colour set and another word.
    # Colours read on the 256-point picture of CHOG #448, whose letters have serifs ours have not.
    "MMGA Cap": {
        "shape": "Cap",
        "colours": {
            "Crown": (0.75, 0.80, 0.81),
            "Brim": (0.21, 0.50, 0.77),
            "Button": (0.21, 0.50, 0.77),
            "Print": (0.20, 0.06, 0.95),
            "PrintLine": (0.08, 0.05, 0.28),
        },
        "print": "MMGA",
        "numbers": {"BRIM_LENGTH": 0.060, "BRIM_RISE_DEG": 35.0},
    },
    "Blue Bucket Cap": {
        "shape": "BucketCap",
        "colours": {
            "Crown": (0.20, 0.29, 0.73),
            "Brim": (0.20, 0.29, 0.73),
            "Under": (0.13, 0.20, 0.63),
            "Mark": (0.76, 0.76, 0.76),
        },
        "mark": "label",
    },
    # Its name says cap; the pictures show the bucket hat, purple, with a white stitched drawing.
    "Balls Cap": {
        "shape": "BucketCap",
        "colours": {
            "Crown": (0.64, 0.18, 0.76),
            "Brim": (0.64, 0.18, 0.76),
            "Under": (0.46, 0.08, 0.53),
            "Mark": (0.95, 0.95, 0.95),
        },
        "mark": "wave",
    },
    "Crown": {
        "shape": "Crown",
        "colours": {
            "Gold": (0.91, 0.64, 0.16),
            "Trim": (0.80, 0.43, 0.11),
            "GemRed": (0.95, 0.10, 0.11),
            "GemBlue": (0.13, 0.22, 0.90),
        },
    },
    "Wizard Beanie": {
        "shape": "Beanie",
        "colours": {
            "Dome": (0.06, 0.21, 0.90),
            "Cuff": (0.07, 0.22, 0.91),
            "Rib": (0.04, 0.05, 0.20),
            "Print": (0.94, 0.86, 0.09),
        },
    },
}
# Every other value of "Head" whose shape is built, the shapes added after these four, and the
# clothing beyond the first three shirts: chog_trait_hats.py (imported above).
HATS.update(hats_and_clothes.HATS)

# ---------------------------------------------------------------------------------------------
# Eyes. Places are counted from the middle of the face, as in make_chog_full.py.
# ---------------------------------------------------------------------------------------------
ANIME_GLASS = {
    # A lens: to the side of the middle of the face, above it, half width, half height.
    # (First: 0.096 by 0.064, three wide for two tall. On the picture a lens is about four wide for
    # three tall, and the pair spans the face.)
    "LENS": (0.128, 0.018, 0.104, 0.074),
    "LENS_SQUARENESS": 3.2,
    "LENS_TAPER": 0.10,          # how much wider the top of a lens is than its bottom
    "LENS_SEGMENTS": 16,
    "RIM": 0.20,                 # the share of a lens taken by its frame, all around
    "THICKNESS": 0.032,
    "LENS_BULGE": 0.006,         # how far the middle of the white stands out of the frame
    "WRAP": 0.45,                # 0: both lenses look straight ahead, 1: each lies on the head
    "BRIDGE_UP": 0.030,          # above the middle of the lenses
    "BRIDGE": (0.011, 0.009),    # half height, half depth of the bar between the lenses
    "TEMPLE_UP": 0.034,
    "TEMPLE": (0.010, 0.007),    # half height, half width of an arm
    "TEMPLE_END": (1.0, 0.16, 0.12),   # where an arm ends on the head: to the side, back, up
    # Thinner than elsewhere: on the picture the frame is mostly teal, with a fine dark line.
    "OUTLINE_THICKNESS": 0.011,
}

# An eye drawn on the face, as "Normal Eye" and the laser eyes have it: a tall dark oval whose top
# is cut by a slanted brow, lower towards the nose.
DARK_EYE = {
    "AT": (0.112, 0.008),        # to the side of the middle of the face, above it
    "SIZE": (0.043, 0.060),      # half width, half height
    "BROW_AT": 0.50,             # where the brow cuts the eye, against its half height
    "BROW_SLOPE": 0.42,
    "BROW": (0.011, 0.016, 0.004),   # thickness of the brow, how far it runs past the eye outwards and inwards
    "SEGMENTS": 14,
    # The glints: each to the side of the middle of the eye (the same side for both eyes: where the
    # light is), above it, half width, half height.
    "GLINTS": ((-0.012, 0.010, 0.013, 0.017), (-0.016, -0.026, 0.006, 0.007)),
}
LASER = {
    # A beam leaves each eye level with the ground, straight ahead: how long, how thick (half), how
    # far behind the face it starts.
    "BEAM_LENGTH": 0.28,
    "BEAM_RADIUS": 0.019,
    "BEAM_SUNK": 0.03,
    # (First: six sides, two of them the light core. Seen from the front and from above, where those
    # two are what shows, the beam was a pale pink stick; on the pictures it is purple with a thin
    # light line along it.)
    "BEAM_SIDES": 8,
    "BEAM_CORE": (1,),           # which of its sides is the light core: up, towards the left
    "OUTLINE_THICKNESS": 0.010,
}
HAPPY = {
    "AT": (0.110, 0.012),
    "ARC": (0.050, 0.034, 0.017),    # half width, height of the arch, thickness in its middle
    "STEPS": 8,
}
ROUND = {
    "AT": (0.112, 0.006),
    "LINE": (0.060, 0.066),      # half width, half height of the dark disc around the eye
    "WHITE": (0.049, 0.055),
    "PUPIL": (0.033, 0.039),
}
# The visor: one band that wraps the front of the head from one row of leaves to the other.
VISOR = {
    "UP": 0.020,                 # its middle, above the middle of the face
    "HALF_HEIGHT": (0.056, 0.042),   # in the middle of the front, at its ends
    "HALF_WIDTH": 0.338,         # the oval it follows seen from above: half width, half depth
    "HALF_DEPTH": 0.306,
    "AROUND_DEG": 80.0,
    "THICKNESS": 0.024,
    "BULGE": 0.008,
    # The material of each piece across the band, from one end to the other.
    "PIECES": ("Edge", "Edge", "Mid", "Dark", "Dark", "Dark", "Dark", "Mid", "Edge", "Edge"),
    "OUTLINE_THICKNESS": 0.014,
}
EYES = {
    "Anime Glass": {
        "kind": "AnimeGlass",
        "colours": {
            "Frame": (0.25, 0.42, 0.37),
            "Lens": (0.96, 0.96, 0.96),
        },
        "unlit": ("Lens",),      # drawn in its colour whatever the light: the glare of the lenses
    },
    "Normal Eye": {
        "kind": "NormalEye",
        "colours": {"Dark": (0.05, 0.05, 0.05), "Glint": (0.95, 0.95, 0.95)},
        "unlit": ("Dark", "Glint"),
    },
    "Purple laser": {
        "kind": "Laser",
        "colours": {"Dark": (0.05, 0.05, 0.05), "Beam": (0.78, 0.10, 0.85), "Core": (0.94, 0.55, 0.89)},
        "unlit": ("Dark", "Beam", "Core"),
    },
    "Happy": {
        "kind": "Happy",
        "colours": {"Dark": (0.05, 0.05, 0.05)},
        "unlit": ("Dark",),
    },
    "Round": {
        "kind": "Round",
        "colours": {"Dark": (0.05, 0.05, 0.05), "White": (0.95, 0.95, 0.95)},
        "unlit": ("Dark", "White"),
    },
    "Sunset visor": {
        "kind": "Visor",
        "colours": {"Edge": (0.86, 0.44, 0.15), "Mid": (0.58, 0.06, 0.05), "Dark": (0.45, 0.06, 0.11)},
    },
}

# ---------------------------------------------------------------------------------------------
# Mouths.
# ---------------------------------------------------------------------------------------------
GOLD_TOOTH = {
    # The grin: half its width, the height of its corners above the middle of the face, then how
    # far below the corners are the middle of its upper edge, of the line between the two rows of
    # teeth and of its lower edge.
    # (First tries: corners at -0.070 then -0.060 with the teeth lit like the face. The bottom of
    # the face of H3 turns under the head, so the grin was seen edge on and came out grey.)
    "HALF_WIDTH": 0.150,
    "CORNERS_UP": -0.058,
    "SAG_TOP": 0.020,
    "SAG_BITE": 0.050,
    "SAG_BOTTOM": 0.084,
    "LINE": 0.014,               # width of the dark line around the grin
    "BITE_LINE": 0.006,          # width of the dark line between the rows
    # The share of each row, from its top, that is in shade: the upper row, the lower row. Measured
    # on the picture of CHOG #1287: a thin edge under the lip, nearly half of the lower row.
    "SHADE": (0.14, 0.44),
    "STEPS": 10,                 # pieces across the grin (12 until 15 October 2026, for the triangle budget)
    # The gold tooth: where across the grin (-1 left corner as seen, +1 right corner; the left of the
    # character is at +1), half its width at the top and at the bottom, how far it reaches above and
    # below the line between the rows.
    "TOOTH_AT": 0.46,
    "TOOTH": (0.023, 0.019, 0.010, 0.024),
    "TOOTH_LINE": 0.007,
    "TOOTH_LEAN": 0.3,           # how much of the slope of the line between the rows the tooth takes
}
SMILE = {
    "AT": (0.0, -0.078),         # its corners: to the side of the middle of the face, above it
    "HALF_WIDTH": 0.052,
    "SAG": 0.024,
    "THICKNESS": 0.012,
    "STEPS": 8,
}
DROOL = {
    # The mouth: a short line whose middle is higher than its ends; the drool hangs from its end on
    # the left of the character (the right of the picture), with a drop.
    "AT": (0.0, -0.092),
    "HALF_WIDTH": 0.040,
    "ARCH": 0.010,
    "THICKNESS": 0.012,
    "DROOL": (0.034, 0.012, 0.050),      # to the side of the middle of the mouth, width, how far it hangs
    "DROP": (0.010, 0.015),
    "STEPS": 6,
}
# "Drooling" as the pictures of Side=Left draw it: a wide open mouth, a tongue, a thick thread of
# drool running down from it.
DROOL_OPEN = {
    "AT": (0.020, -0.086),       # the middle of the mouth
    "SIZE": (0.052, 0.040),      # half width, half height of the dark line around it
    "LINE": 0.008,
    "TONGUE": (-0.012, 0.036, 0.022),    # below the middle of the mouth, half width, half height
    "DROOL": (-0.026, 0.016, 0.062),     # to the side of the middle of the mouth, width, how far it runs
}
WO = {
    "AT": (-0.012, -0.090),
    "SIZE": (0.018, 0.023),
    "GLINT": (-0.005, 0.007, 0.005),
}
RAINBOW = {
    # The open mouth, drawn: to the side, above the middle of the face, half width, half height.
    "MOUTH": (0.0, -0.092, 0.060, 0.030),
    # The stream leaves the mouth and falls: one section per line, each ahead of the mouth (level
    # with the ground), below it, half its width, its thickness. The first is inside the head.
    "PATH": (
        (-0.030, -0.004, 0.050, 0.030),
        (0.028, 0.000, 0.054, 0.032),
        (0.066, 0.044, 0.064, 0.034),
        (0.088, 0.120, 0.078, 0.036),
        (0.096, 0.215, 0.090, 0.036),
        (0.094, 0.310, 0.098, 0.034),
    ),
    "END": (0.090, 0.345),       # where it ends: ahead, below
    "ARCH": 0.25,                # how much its sides fall away from its middle, against its thickness
    # The stripes, from the right of the character to its left (left to right on the picture).
    "STRIPES": ("Red", "Orange", "Yellow", "Green", "Blue", "Purple"),
    "OUTLINE_THICKNESS": 0.014,
}
BUBBLE = {
    # The bubble: to the side of the middle of the face, above it, how far its middle stands off the
    # face, its radius. Then the knot that holds it to the mouth.
    # (On the picture of CHOG #1078 it is half as wide as the face. That large it hid most of the
    # face from above, so it is two fifths.)
    "AT": (-0.020, -0.100),
    "OFF": 0.085,
    "RADIUS": 0.098,
    "SIDES": 10,                 # (12 and 6 until 15 October 2026: lowered for the triangle budget)
    "STACKS": 5,
    "KNOT": 0.020,
    # The glint: a patch on the upper side: degrees to the right of the character, degrees up, size.
    "GLINT": (12.0, 52.0, 0.044, 0.020),
    "OUTLINE_THICKNESS": 0.018,
}
MOUTHS = {
    "Gold Tooth": {
        "kind": "GoldTooth",
        "colours": {
            "Line": (0.06, 0.05, 0.07),
            "Teeth": (0.95, 0.95, 0.96),
            "Shade": (0.62, 0.67, 0.88),
            "Gold": (0.94, 0.66, 0.05),
            "Shine": (0.96, 0.89, 0.64),
        },
        # Paint: drawn in its colours whatever the light.
        "unlit": ("Line", "Teeth", "Shade", "Gold", "Shine"),
    },
    "normal smile": {
        "kind": "Smile",
        "colours": {"Line": (0.05, 0.05, 0.05)},
        "unlit": ("Line",),
    },
    "Drooling": {
        "kind": "Drool",
        "colours": {"Line": (0.05, 0.05, 0.05), "Drool": (0.56, 0.78, 0.94)},
        "unlit": ("Line", "Drool"),
    },
    "wo": {
        "kind": "Wo",
        "colours": {"Line": (0.05, 0.05, 0.05), "Glint": (0.95, 0.95, 0.95)},
        "unlit": ("Line", "Glint"),
    },
    "Rainbow Puke": {
        "kind": "RainbowPuke",
        "colours": {
            "Dark": (0.05, 0.05, 0.05),
            "Red": (0.96, 0.05, 0.05),
            "Orange": (0.96, 0.45, 0.06),
            "Yellow": (0.95, 0.79, 0.06),
            "Green": (0.05, 0.88, 0.15),
            "Blue": (0.06, 0.40, 0.85),
            "Purple": (0.47, 0.12, 0.82),
        },
        "unlit": ("Dark",),
    },
    "Bubblegum": {
        "kind": "Bubblegum",
        "colours": {"Gum": (0.95, 0.51, 0.92), "Glint": (0.97, 0.90, 0.97)},
        "unlit": ("Glint",),
    },
}

# The pictures of Side=Left are drawn by another hand: some values are not the same drawing there.
# What is here replaces the entry above for a Genesis of that side.
MOUTHS_OF_SIDE = {
    "Left": {
        "Drooling": {
            "kind": "DroolOpen",
            "colours": {"Line": (0.05, 0.05, 0.05), "Inside": (0.30, 0.10, 0.11), "Tongue": (0.82, 0.24, 0.47),
                        "Drool": (0.26, 0.81, 0.93)},
            "unlit": ("Line", "Inside", "Tongue", "Drool"),
        },
    },
}

# ---------------------------------------------------------------------------------------------
# Clothing, a first pass: a shirt in one colour, with stripes if its entry names them.
# ---------------------------------------------------------------------------------------------
TEE = {
    "LIFT": 0.008,               # how far the cloth stands off the body
    "HEM": 0.245,                # height of its lower edge above the ground
    "HEM_FLARE": 0.006,
    "STEP": 0.038,               # height of a band, which is the height of a stripe
    "COLLAR": (0.580, 0.593),    # the band at the neck, in the collar material
    "SLEEVE": 0.125,             # how far a sleeve reaches down the arm from the shoulder; 0: none
    "SLEEVE_FLARE": 0.005,
    # (20 and 12, the sides of the torso and of the arms, until 15 October 2026. With a print on every
    # cap, 40 dressed heroes went over 8,000 triangles; 16 and 10 still clear the body, the shell
    # standing 0.008 off it.)
    "SIDES": 16,
    "ARM_SIDES": 10,
}
CLOTHES = {
    # The pictures show a red shirt with a dark collar and nothing else: no flame is drawn where a
    # picture is cut, so none is invented here.
    "Fire Shirt": {
        "kind": "Tee",
        "colours": {"Main": (0.93, 0.18, 0.17), "Collar": (0.45, 0.07, 0.10)},
    },
    "Prisoner": {
        "kind": "Tee",
        "colours": {"Main": (0.95, 0.95, 0.95), "Stripe": (0.07, 0.07, 0.07), "Collar": (0.07, 0.07, 0.07)},
        "stripes": True,
        # Wider stripes than the bands of a shirt (0.038), as the pictures draw them, and two bands fewer.
        "numbers": {"STEP": 0.046},
    },
    # A jersey: no sleeves. Its number (143) is not drawn.
    "Red 143 Jersey": {
        "kind": "Tee",
        "colours": {"Main": (0.84, 0.31, 0.27), "Collar": (0.95, 0.95, 0.95)},
        "numbers": {"SLEEVE": 0.0, "COLLAR": (0.566, 0.593)},
    },
}
CLOTHES.update(hats_and_clothes.CLOTHES)


def head_numbers(replaced=None):
    """The numbers of the head: H3, polished, and those "replaced" gives (to try some: --head-set)."""
    numbers = {**full.HEAD, **full.STYLES["H3"]["head"], **POLISH}
    unknown = set(replaced or {}) - set(numbers)
    if unknown:
        raise SystemExit(f"CHOG_TRAITS no number of the head named {sorted(unknown)}")
    return SimpleNamespace(**{**numbers, **(replaced or {})})


# ---------------------------------------------------------------------------------------------
# The polished base.
# ---------------------------------------------------------------------------------------------

def add_leaf_crown(part, numbers, surface, leaves=FULL_CROWN):
    """The crown of add_crown of make_chog_full.py, blade for blade, each one a leaf, as "leaves" (one
    item per line of SPIKES, see leaf()) leaves it. Returns each leaf built, for leaf_crossings(): its
    name and its rings (middle, half width, half thickness), from inside the head to its point."""
    if len(leaves) != len(numbers.SPIKES):
        raise SystemExit(f"CHOG_TRAITS LEAVES has {len(leaves)} items for {len(numbers.SPIKES)} lines of SPIKES")
    built = []
    # Up as the ground has it, in the axes of the head before it is turned up: where a leaf bends to.
    upwards = -level_axes(numbers)[1]
    for line, ((turn_deg, back_deg, length, width, thickness, flat), item) in enumerate(zip(numbers.SPIKES, leaves)):
        share, turn_to, back_to = item[:3]
        bend = item[3] if len(item) > 3 else 0.0
        wider = (item[4] if len(item) > 4 else 1.0) * numbers.LEAF_WIDTH
        if share <= 0.0:
            continue
        mirrored = turn_deg not in (0.0, 180.0)
        length *= share * numbers.LEAF_LENGTH
        width, thickness = width * wider, thickness * wider
        turn_deg = turn_deg if turn_to is None else turn_to
        back_deg = back_deg if back_to is None else back_to
        for side in ((1.0, -1.0) if mirrored else (1.0,)):
            turn, back = math.radians(turn_deg) * side, math.radians(back_deg)
            away = Vector((math.sin(turn) * math.cos(back), math.sin(back), math.cos(turn) * math.cos(back)))
            root = surface.towards(away)
            # The flat sides look the way "looks" points; the leaf is wide across that. "thick"
            # then points to the side that is looked at, and the point curls away from it.
            looks = -Y if flat == "front" else Z
            wide = looks.cross(away).normalized()
            thick = away.cross(wide)
            half_wide, half_thick = width * 0.5, thickness * 0.5
            curl = -thick * length * numbers.LEAF_CURL
            # A leaf brought down under a brim bends back up along its length ("bend" of leaf()).
            rise = upwards - away * upwards.dot(away)
            if bend and rise.length > 1e-6:
                curl = curl + rise.normalized() * length * bend

            profile = numbers.LEAF_PROFILE
            rings = [(root - away * numbers.SPIKE_SUNK, wide * half_wide * profile[0][1],
                      thick * half_thick * profile[0][2])]
            for along, wider, thicker in profile[1:]:
                rings.append((root + away * length * along + curl * along * along, wide * half_wide * wider,
                              thick * half_thick * thicker))
            rings.append((root + away * length + curl, NOWHERE, NOWHERE))
            part.shell(rings, numbers.LEAF_SIDES, SKIN, numbers.OUTLINE_THICKNESS, smooth=True)
            where = "" if not mirrored else (" left" if side > 0.0 else " right")
            built.append((f"leaf {line}{where}", rings))
    return built


# The spikes under the rim of a hat (15 October 2026, after batch 4 was seen turned around). Keeping or
# moving some of the seven lines of SPIKES under each hat gave, from behind and from above, a pair of
# spikes on each side and none behind, or a collar of spikes at the neck. A hat is a volume put on a
# spiky head: what it covers is pressed under it, and the spikes rooted just under its rim stick out all
# around, at the sides and at the back alike. So, for a hat whose entry says "under_rim", the leaves of
# the crown are removed and one row of leaves is planted under the real rim of the built hat: at each
# angle the rim is found by casting rays out of the head and seeing where the hat stops covering it.
# The pictures (three quarter, from the front left) then show two to four of them stacked down the far
# side under the rim, as they draw them, and the game camera (above, behind) sees the row fan out
# behind the hat. "ANGLES": degrees from the front where a leaf stands, on both sides (180 once, at the
# back); "BELOW_DEG": how far under the rim its root is (degrees down the head); "LENGTH", "WIDTH", "THICK":
# against the leaf of the back row of a bare head (LEAF_LENGTH included); "SWEEP_DEG": how far it leans
# to the back; "DROOP_DEG": how far down from straight out of the head; "LIFT": how far its point bends
# up, against its length.
UNDER_RIM = {
    "ANGLES": (78.0, 104.0, 130.0, 156.0, 180.0),
    "BELOW_DEG": 9.0,
    "LENGTH": 0.95,
    "WIDTH": 0.62,
    "THICK": 0.60,
    "SWEEP_DEG": 38.0,
    "DROOP_DEG": 0.0,
    "LIFT": 0.22,
}
RIM_REACH = 0.10                 # a ray out of the head that meets the hat within this is covered
RIM_SCAN = (6.0, 160.0, 1.0)     # polar degrees scanned from the top down, and the step


def hat_tree(hat_shapes, surface):
    """A tree of the faces of the hat that lie outside the head (as leaf_crossings counts them)."""
    from mathutils.bvhtree import BVHTree
    points, polygons = [], []
    for shapes in hat_shapes:
        for face in shapes.mesh.faces:
            centre = face.calc_center_median() - surface.middle
            if sum((centre[index] / surface.half[index]) ** 2 for index in range(3)) <= 1.02 ** 2:
                continue
            polygons.append([len(points) + index for index in range(len(face.verts))])
            points.extend(point.co.copy() for point in face.verts)
    return BVHTree.FromPolygons(points, polygons) if polygons else None


def rim_polar(tree, surface, angle):
    """The polar degrees of the lower edge of the hat at an angle seen from above: the last covered
    place of the run of covered places that starts nearest the top (gaps of 3 degrees bridged), or None
    when the hat covers nothing there."""
    first, last, gap = None, None, 0.0
    polar = RIM_SCAN[0]
    while polar <= RIM_SCAN[1]:
        rad = math.radians(polar)
        point = hats_and_clothes.on_head(surface, angle, rad, 1.0)
        out = hats_and_clothes.head_normal(surface, angle, rad)
        covered = tree.ray_cast(point + out * 0.002, out, RIM_REACH)[0] is not None
        if covered:
            first = polar if first is None else first
            last, gap = polar, 0.0
        elif first is not None:
            gap += RIM_SCAN[2]
            if gap > 3.0:
                break
        polar += RIM_SCAN[2]
    return last


def add_rim_leaves(part, numbers, surface, hat_shapes, under):
    """The row of leaves under the rim of a hat (see UNDER_RIM). Returns them as add_leaf_crown does,
    for leaf_crossings(); a leaf that would cross the hat is lowered, and dropped if it still does."""
    under = {**UNDER_RIM, **under}
    tree = hat_tree(hat_shapes, surface)
    if tree is None:
        return []
    _, _, length, width, thickness, _ = numbers.SPIKES[-2]      # the pair of the back row of H3
    length *= under["LENGTH"] * numbers.LEAF_LENGTH
    width *= under["WIDTH"] * numbers.LEAF_WIDTH
    thickness *= under["THICK"] * numbers.LEAF_WIDTH
    ahead, down = level_axes(numbers)
    back, up = -ahead, -down
    profile = numbers.LEAF_PROFILE

    def rings_of(angle, below, droop):
        """The rings of one leaf rooted "below" degrees under the rim at that angle, leaning down by
        "droop" degrees from straight out of the head."""
        polar = math.radians(rim + below)
        root = hats_and_clothes.on_head(surface, angle, polar, 1.0)
        out = hats_and_clothes.head_normal(surface, angle, polar)
        away = out.copy()
        backwards, downwards = back - out * back.dot(out), down - out * down.dot(out)
        if backwards.length > 1e-6:
            away += backwards.normalized() * math.tan(math.radians(under["SWEEP_DEG"]))
        if downwards.length > 1e-6:
            away += downwards.normalized() * math.tan(math.radians(droop))
        away.normalize()
        wide = up.cross(away)
        wide = wide.normalized() if wide.length > 1e-6 else X.copy()
        thick = away.cross(wide)
        rise = up - away * up.dot(away)
        curl = rise.normalized() * length * under["LIFT"] if rise.length > 1e-6 else Vector()
        rings = [(root - away * numbers.SPIKE_SUNK, wide * width * 0.5 * profile[0][1],
                  thick * thickness * 0.5 * profile[0][2])]
        for along, wider, thicker in profile[1:]:
            rings.append((root + away * length * along + curl * along * along, wide * width * 0.5 * wider,
                          thick * thickness * 0.5 * thicker))
        rings.append((root + away * length + curl, NOWHERE, NOWHERE))
        return rings

    built, rims = [], []
    for degrees in under["ANGLES"]:
        for side in ((1.0,) if degrees in (0.0, 180.0) else (1.0, -1.0)):
            angle = math.radians(270.0 + side * degrees)
            rim = rim_polar(tree, surface, angle)
            if rim is None:
                continue
            where = "" if degrees in (0.0, 180.0) else (" left" if side > 0.0 else " right")
            name = f"rim leaf {degrees:.0f}{where}"
            # Lowered, then rooted further down, until it no longer crosses the hat (as leaf_crossings
            # judges it, outline included); dropped when nothing fits.
            placed = None
            for below in (under["BELOW_DEG"], under["BELOW_DEG"] + 6.0, under["BELOW_DEG"] + 12.0):
                for droop in (under["DROOP_DEG"], under["DROOP_DEG"] + 12.0, under["DROOP_DEG"] + 24.0):
                    rings = rings_of(angle, below, droop)
                    if not leaf_crossings([(name, rings)], hat_shapes, numbers, surface):
                        placed = rings
                        break
                if placed:
                    break
            if placed is None:
                print(f"CHOG_TRAITS {name}: no place under the rim that does not cross the hat, left out")
                continue
            part.shell(placed, numbers.LEAF_SIDES, SKIN, numbers.OUTLINE_THICKNESS, smooth=True)
            built.append((name, placed))
            rims.append(f"{degrees:.0f}{where[:2]} {rim:.0f}")
    print(f"CHOG_TRAITS rim row: {len(built)} leaves; rim (polar degrees) at " + ", ".join(rims))
    return built


def head_entry(traits, replaced=None):
    """The entry of "Head" a Genesis gets on its Side, with the numbers "replaced" puts in, or None."""
    entry = entry_for("Head", traits) if traits else None
    if not isinstance(entry, dict):
        return None
    if replaced:
        entry = {**entry, "numbers": {**entry.get("numbers", {}), **replaced}}
    return entry


def rim_leaves_for(part, traits, replaced, numbers, body_n, surface):
    """Plants the row of leaves under the rim of the hat of a Genesis, when its entry asks for it: the
    hat is built once aside (not kept) to find its rim. Returns the leaves, as add_leaf_crown does."""
    entry = head_entry(traits, replaced)
    if entry is None or entry.get("under_rim") is None:
        return []
    _, _, word, builders = TRAIT_PARTS["Head"]
    probe = Maker(f"Probe_{entry[word]}", entry, numbers, body_n, surface)
    builders[entry[word]](probe)
    shapes = [shapes for shapes, on in probe.parts if on == "Head"]
    try:
        return add_rim_leaves(part, numbers, surface, shapes, entry["under_rim"])
    finally:
        for made, _ in probe.parts:
            made.mesh.free()


def cream_head(part):
    """Side=Left draws the whole head in the cream of the face, its points too: the skin colour of the
    head (skull and leaves, so far) becomes the cream. Points of hair under a hood are added after and
    keep the skin colour."""
    skin, cream = part.order.index(SKIN), part.order.index(full.FACE)
    for face in part.mesh.faces:
        if face.material_index == skin:
            face.material_index = cream


def leaf_crossings(leaves, hat_shapes, numbers, surface):
    """The leaves (from add_leaf_crown) that go through a hat, or that a thin part of the hat (a band, a
    brim) goes through: an edge of a leaf, made thicker by its outline, crosses a face of the hat, or an
    edge of the hat crosses a face of that leaf. Only the faces of the hat that lie outside the head
    count (the rows of a hat tucked inside the head and the face that closes it there are not seen).
    Both are in the axes of "Head" before the head is turned up. Returns the names of those leaves."""
    from mathutils.bvhtree import BVHTree

    def cut(tree, segments):
        for start, stop in segments:
            way = stop - start
            if way.length > 1e-9 and tree.ray_cast(start, way.normalized(), way.length)[0] is not None:
                return True
        return False

    points, polygons, hat_edges = [], [], []
    for shapes in hat_shapes:
        for face in shapes.mesh.faces:
            centre = face.calc_center_median() - surface.middle
            if sum((centre[index] / surface.half[index]) ** 2 for index in range(3)) <= 1.02 ** 2:
                continue
            corners = [point.co.copy() for point in face.verts]
            polygons.append([len(points) + index for index in range(len(corners))])
            points.extend(corners)
            hat_edges.extend((corners[index], corners[(index + 1) % len(corners)]) for index in range(len(corners)))
    if not polygons:
        return []
    hat_tree = BVHTree.FromPolygons(points, polygons)
    grow, sides = numbers.OUTLINE_THICKNESS, numbers.LEAF_SIDES
    crossing = []
    for name, rings in leaves:
        loops = []
        for index, (centre, half_wide, half_thick) in enumerate(rings):
            if half_wide.length < 1e-9:
                along = (centre - rings[index - 1][0]).normalized()
                loops.append([centre + along * grow])
                continue
            wide = half_wide.normalized() * (half_wide.length + grow)
            thick = half_thick.normalized() * (half_thick.length + grow)
            loops.append([centre + wide * math.cos(2.0 * math.pi * step / sides)
                          + thick * math.sin(2.0 * math.pi * step / sides) for step in range(sides)])
        segments, leaf_points, leaf_polygons = [], [], []
        for lower, upper in zip(loops, loops[1:]):
            for step in range(sides):
                after = (step + 1) % sides
                segments.append((lower[step % len(lower)], upper[step % len(upper)]))
                if len(upper) > 1:
                    segments.append((upper[step], upper[after]))
                corners = [lower[step], lower[after]] + ([upper[after], upper[step]] if len(upper) > 1 else [upper[0]])
                leaf_polygons.append([len(leaf_points) + index for index in range(len(corners))])
                leaf_points.extend(corners)
        leaf_polygons.append([len(leaf_points) + index for index in range(sides)])
        leaf_points.extend(loops[0])
        if cut(hat_tree, segments) or cut(BVHTree.FromPolygons(leaf_points, leaf_polygons), hat_edges):
            crossing.append(name)
    return crossing


# ---------------------------------------------------------------------------------------------
# What every trait part is made with.
# ---------------------------------------------------------------------------------------------

class TraitShapes(variants.PartShapes):
    """The shapes of one object of a trait, in the axes of the part it sits on. Its materials carry
    the name of the trait ("prefix"), which is also the name of its first object."""

    def __init__(self, name, prefix, colours):
        super().__init__(tuple(f"{prefix}_{key}" for key in colours) + (OUTLINE,))
        self.name = name
        self.prefix = prefix

    def named(self, key):
        return f"{self.prefix}_{key}"


def trait_materials(prefix, colours, unlit, shared):
    """The materials of a trait, in the colours of its colour set. "srgb" keeps the colour as it was
    given, for the renders; "unlit" says it is drawn in that colour whatever the light."""
    materials = {OUTLINE: shared[OUTLINE]}
    for key, colour in colours.items():
        material = bpy.data.materials.new(f"{prefix}_{key}")
        material.diffuse_color = linear(colour)
        shader = material.node_tree.nodes.get("Principled BSDF") if material.node_tree else None
        if shader is not None:
            shader.inputs["Base Color"].default_value = linear(colour)
            shader.inputs["Roughness"].default_value = 0.9
            shader.inputs["Metallic"].default_value = 0.0
        material["srgb"] = list(colour)
        material["unlit"] = key in unlit
        materials[f"{prefix}_{key}"] = material
    return materials


class Maker:
    """What the builder of a trait is given: the numbers of the head and of the body, where the
    surface of the head is, the entry of its table, and a way to open the objects it builds."""

    def __init__(self, prefix, entry, head, body, surface):
        self.prefix, self.entry = prefix, entry
        self.head, self.body, self.surface = head, body, surface
        self.parts = []          # (shapes, name of the part of the body it sits on)

    def numbers(self, defaults):
        """The numbers of a shape, those the entry replaces in their place."""
        unknown = set(self.entry.get("numbers", {})) - set(defaults)
        if unknown:
            raise SystemExit(f"CHOG_TRAITS {self.prefix} sets numbers that do not exist: {sorted(unknown)}")
        return SimpleNamespace(**{**defaults, **self.entry.get("numbers", {})})

    def part(self, on="Head"):
        """A new object of this trait, on a part of the body. The first is named after the trait,
        the others after the trait and the part they sit on."""
        name = self.prefix if not self.parts else f"{self.prefix}_{on}"
        shapes = TraitShapes(name, self.prefix, self.entry["colours"])
        self.parts.append((shapes, on))
        return shapes


def power(value, squareness):
    """The cosine or sine of a rounded square: 2 is a circle, more is squarer."""
    return math.copysign(abs(value) ** (2.0 / squareness), value)


def add_loops(part, loops, material, outline, smooth=True, shares=None):
    """One closed shell from rows of points given one by one, a row of one point being a tip; an end
    row of several points is closed by a flat face. "shares", when given, says for each point of
    each row how much of the thickness of the outline it gets (1 all of it, 0 none): for a shape
    that enters another, or that lies close over another part. Returns the faces of each band
    between two rows, in the order of the points, then those of the flat ends."""
    mesh = part.mesh
    rows = [[mesh.verts.new(point) for point in loop] for loop in loops]
    fade = None
    if shares is not None:
        fade = {point: share for row, row_shares in zip(rows, shares) for point, share in zip(row, row_shares)}
    bands = []
    for lower, upper in zip(rows, rows[1:]):
        count = max(len(lower), len(upper))
        band = []
        for index in range(count):
            after = (index + 1) % count
            if len(lower) == 1:
                corners = (lower[0], upper[after], upper[index])
            elif len(upper) == 1:
                corners = (lower[index], lower[after], upper[0])
            else:
                corners = (lower[index], lower[after], upper[after], upper[index])
            face = mesh.faces.new(corners)
            face.smooth = smooth
            band.append(face)
        bands.append(band)
    ends = []
    if len(rows[0]) > 1:
        ends.append(mesh.faces.new(list(reversed(rows[0]))))
    if len(rows[-1]) > 1:
        ends.append(mesh.faces.new(rows[-1]))
    faces = [face for band in bands for face in band] + ends
    part.paint(faces, material)
    if outline > 0.0:
        part.outlined.append((faces, outline, fade))
    return bands, ends


def ring_points(centre, first, second, sides):
    """The points of an oval: "first" and "second" are its two half axes."""
    return [centre + first * math.cos(2.0 * math.pi * index / sides) + second * math.sin(2.0 * math.pi * index / sides)
            for index in range(sides)]


def outward(surface, side, up):
    """The way the head looks at a place of its front."""
    return (surface.front(side, up, 1.0) - surface.front(side, up, 0.0)).normalized()


def level_axes(head):
    """Straight ahead and straight down as the ground has them, in the axes of "Head" before the
    head is turned up: what is built along them is level once the head looks up."""
    back = Matrix.Rotation(math.radians(head.HEAD_PITCH_DEG), 3, "X")
    return back @ -Y, back @ -Z


def oval(middle, half_width, half_height, segments):
    """The places of the edge of an oval drawn on the face: (to the side, above)."""
    return [(middle[0] + half_width * math.cos(2.0 * math.pi * index / segments),
             middle[1] + half_height * math.sin(2.0 * math.pi * index / segments)) for index in range(segments)]


def draw_shape(part, surface, material, edge, lift, middle=None):
    """A flat shape drawn on the face: "edge" is its outline as places (to the side, above), which
    must be seen whole from "middle" (the mean of the edge when not given)."""
    if middle is None:
        middle = (sum(place[0] for place in edge) / len(edge), sum(place[1] for place in edge) / len(edge))
    full.add_skin(part, surface, [[surface.front(middle[0], middle[1], lift)],
                                  [surface.front(side, up, lift) for side, up in edge]], material)


def draw_line(part, surface, material, places, thickness, lift):
    """A line drawn on the face through "places" (to the side, above); "thickness" gives its width
    at each place."""
    lower, upper = [], []
    for index, (side, up) in enumerate(places):
        before, after = places[max(index - 1, 0)], places[min(index + 1, len(places) - 1)]
        along = Vector((after[0] - before[0], after[1] - before[1]))
        across = Vector((-along.y, along.x)).normalized() * thickness[index] * 0.5
        lower.append(surface.front(side - across.x, up - across.y, lift))
        upper.append(surface.front(side + across.x, up + across.y, lift))
    full.add_skin(part, surface, [lower, upper], material, around=False)


# ---------------------------------------------------------------------------------------------
# Hats.
# ---------------------------------------------------------------------------------------------

class CapCrown:
    """Where the wall of a hat is, in the axes of "Head" before the head is turned up: rings one
    above the other, each a rounded square seen from above."""

    def __init__(self, numbers, middle):
        self.numbers = numbers
        self.middle = middle

    def at_height(self, height):
        """Half width, half depth and move back of the wall at a height."""
        rings = self.numbers.RINGS
        height = min(max(height, rings[0][0]), rings[-1][0])
        for lower, upper in zip(rings, rings[1:]):
            if height <= upper[0]:
                amount = (height - lower[0]) / (upper[0] - lower[0])
                size = lower[1] + (upper[1] - lower[1]) * amount
                back = lower[2] + (upper[2] - lower[2]) * amount
                return self.numbers.RIM_HALF_WIDTH * size, self.numbers.RIM_HALF_DEPTH * size, back
        raise SystemExit("CHOG_TRAITS the rings of a hat are not in order")

    def around(self, height, angle, size=1.0):
        """The point of the wall at a height and an angle seen from above: 0 is the left of the
        character, 270 degrees is the front."""
        half_width, half_depth, back = self.at_height(height)
        square = self.numbers.SQUARENESS
        return self.middle + Vector((half_width * size * power(math.cos(angle), square),
                                     half_depth * size * power(math.sin(angle), square) + back, height))

    def loop(self, height, size=1.0):
        sides = self.numbers.SIDES
        return [self.around(height, 2.0 * math.pi * index / sides, size) for index in range(sides)]

    def on_front(self, side, height):
        """The point of the front of the wall "side" to the left of its middle, at a height: on the
        flat pieces the wall is made of, not on the curve they follow."""
        front = sorted((point for point in self.loop(height) if point.y < self.middle.y + self.at_height(height)[2]),
                       key=lambda point: point.x)
        for first, second in zip(front, front[1:]):
            if first.x <= side <= second.x:
                amount = (side - first.x) / max(second.x - first.x, 1e-9)
                return first + (second - first) * amount
        raise SystemExit(f"CHOG_TRAITS {side:+.3f} is beyond the front of the hat")

    def front(self, side, height, lift):
        """The same point pushed "lift" off the wall."""
        here = self.on_front(side, height)
        small = 0.002
        across = self.on_front(side + small, height) - self.on_front(side - small, height)
        upwards = self.on_front(side, height + small) - self.on_front(side, height - small)
        normal = across.cross(upwards).normalized()
        if normal.y > 0.0:
            normal = -normal
        return here + normal * lift

    def frame(self, degrees, height):
        """A place of the wall given by degrees from the front (towards the left of the character)
        and a height: the point, then the ways along the wall, up the wall and out of it."""
        angle = math.radians(270.0 + degrees)
        small = 0.01
        here = self.around(height, angle)
        along = (self.around(height, angle + small) - self.around(height, angle - small)).normalized()
        low = max(height - small, self.numbers.RINGS[0][0])
        upwards = (self.around(low + 2.0 * small, angle) - self.around(low, angle)).normalized()
        out = along.cross(upwards).normalized()
        if out.dot(here - self.middle) < 0.0:
            out = -out
        return here, along, upwards, out


def lay_disc(part, surface, wall, material, degrees, height, half_width, half_height, lifts, segments=8):
    """A small raised disc on the wall of a hat: a gem, a patch."""
    here, along, upwards, out = wall.frame(degrees, height)
    low, high = lifts
    edge = [here + out * low + along * half_width * math.cos(2.0 * math.pi * index / segments)
            + upwards * half_height * math.sin(2.0 * math.pi * index / segments) for index in range(segments)]
    full.add_skin(part, surface, [[here + out * high], edge], material)


def word_pieces(word, thicker, spacing, resolution=2):
    """The filled outline of a word in the font built into Blender, as flat triangles in the units of
    the font: the word is centred, stands on the line y = 0 and reads towards +x. "resolution" is the
    number of points per curve of a letter."""
    curve = bpy.data.curves.new("ChogPrintWord", "FONT")
    curve.body = word
    curve.size = 1.0
    curve.align_x = "CENTER"
    curve.offset = thicker
    curve.space_character = spacing
    curve.resolution_u = resolution
    if curve.font is None or curve.font.filepath != "<builtin>":
        raise SystemExit("CHOG_TRAITS the font of the print is not the one built into Blender")
    thing = bpy.data.objects.new("ChogPrintWord", curve)
    bpy.context.scene.collection.objects.link(thing)
    bpy.context.view_layer.update()
    filled = bpy.data.meshes.new_from_object(thing.evaluated_get(bpy.context.evaluated_depsgraph_get()))
    pieces = bmesh.new()
    pieces.from_mesh(filled)
    bpy.data.meshes.remove(filled)
    bpy.data.objects.remove(thing)
    bpy.data.curves.remove(curve)
    return pieces


def word_width(word, thicker, spacing):
    """How wide a word is in the units of the font, its strokes thickened by "thicker"."""
    pieces = word_pieces(word, thicker, spacing)
    wide = max(point.co.x for point in pieces.verts) - min(point.co.x for point in pieces.verts)
    pieces.free()
    return wide


def lay_word(part, crown, word, material, thicker, lift, baseline=None, scale=None, resolution=2):
    """Lays a word on the front panel of the cap: cut into narrow pieces, each point then put on the
    panel. "baseline" and "scale" (units of the model per unit of the font) replace those of the
    numbers of the cap when given. Returns how wide the word is."""
    numbers = crown.numbers
    scale = scale or numbers.PRINT_CAPITALS / FONT_CAPITALS
    baseline = numbers.PRINT_BASELINE if baseline is None else baseline
    pieces = word_pieces(word, thicker, numbers.PRINT_SPACING, resolution)
    left = min(point.co.x for point in pieces.verts)
    right = max(point.co.x for point in pieces.verts)
    step = numbers.PRINT_CUT / scale
    cut = math.ceil(left / step) * step
    while cut < right:
        bmesh.ops.bisect_plane(pieces, geom=pieces.verts[:] + pieces.edges[:] + pieces.faces[:], dist=1e-6,
                               plane_co=Vector((cut, 0.0, 0.0)), plane_no=X)
        cut += step
    # Cut it across too where the wall of the hat bends (the heights of its rings): a straight stroke has
    # points only at its ends, and laid across a bend its middle sank under the panel. (Batch 4: the 1 of
    # "10K", whose box stands higher, crossed the ring at 0.236 and showed as "!".)
    low = min(point.co.y for point in pieces.verts)
    high = max(point.co.y for point in pieces.verts)
    for height, _, _ in getattr(numbers, "RINGS", ()):
        across = (height - baseline) / scale
        if low < across < high:
            bmesh.ops.bisect_plane(pieces, geom=pieces.verts[:] + pieces.edges[:] + pieces.faces[:], dist=1e-6,
                                   plane_co=Vector((0.0, across, 0.0)), plane_no=Y)
    bmesh.ops.triangulate(pieces, faces=pieces.faces[:])

    mesh = part.mesh
    placed = {}
    faces = []
    for piece in pieces.faces:
        corners = []
        for point in piece.verts:
            if point.index not in placed:
                placed[point.index] = mesh.verts.new(crown.front(point.co.x * scale, baseline + point.co.y * scale,
                                                                 lift))
            corners.append(placed[point.index])
        face = mesh.faces.new(corners)
        face.smooth = True
        face.normal_update()
        if face.normal.dot(face.calc_center_median() - crown.middle) < 0.0:
            face.normal_flip()
        faces.append(face)
    pieces.free()
    part.paint(faces, material)
    part.open_faces.extend(faces)
    return (right - left) * scale


def add_dome(part, wall, material, outline):
    """The crown of a hat: its rings, closed under the rim inside the head, and its top. Returns the
    faces of each band."""
    numbers = wall.numbers
    loops = [wall.loop(height) for height, _, _ in numbers.RINGS]
    top_height, top_back = numbers.TOP
    loops.append([wall.middle + Vector((0.0, top_back, top_height))])
    bands, _ = add_loops(part, loops, material, outline)
    return bands


def build_cap(maker):
    """The shape "Cap": a crown, a brim, a button, and the word of the print on the front panel."""
    numbers = maker.numbers(CAP)
    part, surface = maker.part(), maker.surface
    crown = CapCrown(numbers, surface.middle)
    add_dome(part, crown, part.named("Crown"), numbers.OUTLINE_THICKNESS)

    top_height, top_back = numbers.TOP
    half_width, half_height = numbers.BUTTON
    part.shell(base.ball_rings(surface.middle + Vector((0.0, top_back, top_height)), Z, X, half_height, half_width,
                               half_width, 3), 8, part.named("Button"), numbers.OUTLINE_THICKNESS * 0.6)

    # The brim: a slab cut across its width into sections, each one a ring of six points going from
    # the crown out to the edge along the top and back under it.
    sections = numbers.BRIM_SECTIONS
    rise = math.tan(math.radians(numbers.BRIM_RISE_DEG))
    half_thick = numbers.BRIM_THICKNESS * 0.5
    loops, shares = [], []
    for index in range(sections):
        across = -1.0 + 2.0 * index / (sections - 1)
        angle = math.radians(270.0 + across * numbers.BRIM_AROUND_DEG)
        # The two end sections lie wholly inside the crown, so the brim comes out of its wall with
        # its full thickness. (First try: each end thinned to a point on the wall, and the outline
        # of that point showed as a dark sliver beside the cap.)
        at_end = index in (0, sections - 1)
        inner = crown.around(numbers.BRIM_HEIGHT, angle, 0.86 if at_end else 0.95)
        reach = numbers.BRIM_LENGTH * (1.0 - across * across) ** 0.6
        edge = crown.around(numbers.BRIM_HEIGHT, angle, 0.92 if at_end else 1.0)
        wider = 1.0 if at_end else numbers.BRIM_WIDER
        edge = Vector((edge.x * wider, edge.y - reach,
                       edge.z + reach * rise - numbers.BRIM_DROOP * across * across))
        inner = inner - Z * numbers.BRIM_DROOP * across * across
        middle = inner + (edge - inner) * 0.62 + Z * 0.004
        loops.append([
            inner + Z * half_thick, middle + Z * half_thick, edge + Z * half_thick * 0.45,
            edge - Z * half_thick * 0.45, middle - Z * half_thick, inner - Z * half_thick,
        ])
        shares.append((0.0,) * 6 if at_end else numbers.BRIM_OUTLINE_SHARES)
    # The outline of the brim is whole along its edge, absent on its top up to the crown and at the
    # two ends inside the crown, and thin under it. (First try: the whole thickness everywhere. The
    # shell then stood off the crown and off the glasses under the brim, and showed as two dark
    # slivers, one on the crown above the brim and one across the lenses. Second try: 0.8 of it
    # half way along the top; the sliver on the crown stayed.)
    add_loops(part, loops, part.named("Brim"), numbers.OUTLINE_THICKNESS, shares=shares)

    word = maker.entry.get("print")
    if word:
        # One word, or several lines one under the other ("print" is then a tuple). The block is made
        # smaller when it would be wider than PRINT_FIT or taller than PRINT_BLOCK, and its middle stays
        # where the middle of a single word of PRINT_CAPITALS is. No dark line when the colour set has
        # no "PrintLine".
        lines = (word,) if isinstance(word, str) else tuple(word)
        low, high = numbers.PRINT_LIFT
        lined = "PrintLine" in maker.entry["colours"]
        boxed = bool(maker.entry.get("box"))
        scale = numbers.PRINT_CAPITALS / FONT_CAPITALS
        widest = max(word_width(line, numbers.PRINT_BOLD, numbers.PRINT_SPACING) for line in lines)
        scale = min(scale, numbers.PRINT_FIT / widest,
                    numbers.PRINT_BLOCK / (FONT_CAPITALS * (1.0 + (len(lines) - 1) * numbers.PRINT_LEADING)))
        capitals = FONT_CAPITALS * scale
        step = capitals * numbers.PRINT_LEADING
        middle = numbers.PRINT_BASELINE + numbers.PRINT_CAPITALS * 0.5
        top = middle + (len(lines) - 1) * step * 0.5 - capitals * 0.5
        widths = []
        for index, line in enumerate(lines):
            baseline = top - index * step
            if lined:
                # The dark line under the letters is drawn with one point per curve: its rounded parts
                # are hidden under the letters (it saved 30 to 90 triangles a cap).
                lay_word(part, crown, line, part.named("PrintLine"), numbers.PRINT_BOLD + numbers.PRINT_LINE, low,
                         baseline, scale, resolution=1)
            # In a box (10k TPS), the first line is drawn light on a band of the colour of the print.
            ink = part.named("Print2") if boxed and index == 0 else part.named("Print")
            wide = lay_word(part, crown, line, ink, numbers.PRINT_BOLD, high, baseline, scale)
            widths.append(wide)
            if maker.entry.get("zero_bar"):
                # A short upright bar inside each 0, as the pictures of "10k TPS Cap" draw it: the round 0
                # of the font beside the 1 read "1OK" (batch 3). Its middle: the left end of the word, plus
                # the width of the word up to that 0, less half the width of a 0. (The font does not centre
                # a word on 0 exactly: "10K" runs from -0.78 to 1.04 of its units.)
                pieces = word_pieces(line, numbers.PRINT_BOLD, numbers.PRINT_SPACING)
                left = min(point.co.x for point in pieces.verts)
                pieces.free()
                zero = word_width("0", numbers.PRINT_BOLD, numbers.PRINT_SPACING)
                for place, letter in enumerate(line):
                    if letter == "0":
                        upto = word_width(line[:place + 1], numbers.PRINT_BOLD, numbers.PRINT_SPACING)
                        x = (left + upto - zero * 0.5) * scale
                        hats_and_clothes.wall_bar(part, crown, ink, (x, baseline + capitals * 0.26),
                                                  (x, baseline + capitals * 0.74), capitals * 0.075, high + 0.001)
            print(f"CHOG_TRAITS the word {line!r} is {wide:.3f} wide, capitals {capitals:.3f}, on a front panel "
                  f"{2.0 * crown.at_height(baseline)[0]:.3f} wide")
        if boxed:
            # The box of "10k TPS" (15 October 2026): a band behind the first line, a frame around both.
            margin, frame = capitals * 0.30, capitals * 0.16
            half = max(widths) * 0.5 + margin
            first_low, block_low = top - capitals * 0.28, top - (len(lines) - 1) * step - margin
            block_high = top + capitals + margin
            rect = hats_and_clothes.wall_rect
            rect(part, crown, part.named("Print"), -half, half, first_low, block_high, low * 0.6)
            rect(part, crown, part.named("Print"), -half - frame, half + frame, block_low - frame, block_low, low * 0.6)
            rect(part, crown, part.named("Print"), -half - frame, half + frame, block_high, block_high + frame, low * 0.6)
            for side in (-1.0, 1.0):
                x = side * (half + frame * 0.5)
                rect(part, crown, part.named("Print"), x - frame * 0.5, x + frame * 0.5, block_low, block_high, low * 0.6,
                     columns=1)
        if maker.entry.get("bars"):
            # The two bars of the Bitcoin B over and under the first letter (Buttcoin, 15 October 2026).
            letter = word_width(lines[0][0], numbers.PRINT_BOLD, numbers.PRINT_SPACING) * scale
            left = -widths[0] * 0.5
            for share in (0.36, 0.66):
                x = left + letter * share
                for low_end, high_end in ((top + capitals * 0.92, top + capitals * 1.22), (top - capitals * 0.22,
                                                                                         top + capitals * 0.08)):
                    hats_and_clothes.wall_bar(part, crown, part.named("Print"), (x, low_end), (x, high_end),
                                              capitals * 0.075, high)
    logo = maker.entry.get("logo")
    if logo:
        hats_and_clothes.lay_logo(part, crown, logo, part.named("Print"), numbers.LOGO, numbers.PRINT_LIFT[1])


def build_bucket(maker):
    """The shape "BucketCap": a round crown, a brim all around it, and a mark on the front."""
    numbers = maker.numbers(BUCKET)
    part, surface = maker.part(), maker.surface
    crown = CapCrown(numbers, surface.middle)
    add_dome(part, crown, part.named("Crown"), numbers.OUTLINE_THICKNESS)

    # The brim: four rings, from the crown out along the top and back under it. At the sides and at
    # the back it dips; in the middle of the front it is shorter and rises.
    sides, height = numbers.SIDES, numbers.BRIM_HEIGHT
    half_thick = numbers.BRIM_THICKNESS * 0.5
    axis = surface.middle + Vector((0.0, crown.at_height(height)[2], height))
    rows = [[], [], [], []]
    for index in range(sides):
        angle = 2.0 * math.pi * index / sides
        front = max(0.0, -math.sin(angle)) ** numbers.BRIM_FRONT_SPREAD
        length = numbers.BRIM_LENGTH + (numbers.BRIM_FRONT_LENGTH - numbers.BRIM_LENGTH) * front
        rise = math.radians(-numbers.BRIM_DIP_DEG + (numbers.BRIM_FRONT_RISE_DEG + numbers.BRIM_DIP_DEG) * front)
        wall = crown.around(height, angle)
        inner = crown.around(height, angle, 0.93)
        away = Vector((wall.x - axis.x, wall.y - axis.y, 0.0)).normalized()
        edge = wall + away * length * math.cos(rise) + Z * length * math.sin(rise)
        rows[0].append(inner + Z * half_thick)
        rows[1].append(edge + Z * half_thick * 0.5)
        rows[2].append(edge - Z * half_thick * 0.5)
        rows[3].append(inner - Z * half_thick)
    none, whole = (0.0,) * sides, (1.0,) * sides
    bands, _ = add_loops(part, rows, part.named("Brim"), numbers.OUTLINE_THICKNESS, shares=[none, whole, whole, none])
    # Under the brim the cloth is in shade where the brim dips; where it is turned up, its underside
    # is what is seen from the front, and it keeps the colour of the hat.
    dips = [max(0.0, -math.sin(2.0 * math.pi * index / sides)) ** numbers.BRIM_FRONT_SPREAD < 0.4
            for index in range(sides)]
    part.paint([face for index, face in enumerate(bands[2]) if dips[index] and dips[(index + 1) % sides]],
               part.named("Under"))

    mark = maker.entry.get("mark")
    wide, lift = numbers.MARK_HALF_WIDTH, numbers.MARK_LIFT
    if mark == "label":
        # A sewn label: a rectangle. With words (Dollar Cap, 15 October 2026), larger, edged by a dark
        # line, the words in two lines.
        words = maker.entry.get("label_words")
        half_wide, half_tall = (0.108, 0.042) if words else (wide * 0.8, 0.022)
        low, high = numbers.MARK_HEIGHT - half_tall, numbers.MARK_HEIGHT + half_tall
        columns = [-half_wide + 2.0 * half_wide * index / 4 for index in range(5)]
        full.add_skin(part, surface, [[crown.front(side, low, lift) for side in columns],
                                      [crown.front(side, high, lift) for side in columns]], part.named("Mark"),
                      around=False)
        if words:
            edge = 0.006
            inner = [(-half_wide, low), (half_wide, low), (half_wide, high), (-half_wide, high)]
            outer = [(-half_wide - edge, low - edge), (half_wide + edge, low - edge), (half_wide + edge, high + edge),
                     (-half_wide - edge, high + edge)]
            hats_and_clothes.wall_ring(part, crown, part.named("Ink"), inner, outer, lift)
            panel = SimpleNamespace(front=crown.front, middle=crown.middle,
                                    numbers=SimpleNamespace(PRINT_CUT=0.07, PRINT_SPACING=1.08))
            widest = max(word_width(line, 0.02, 1.08) for line in words)
            scale = min(0.026 / FONT_CAPITALS, half_wide * 1.8 / widest)
            capitals = FONT_CAPITALS * scale
            for index, line in enumerate(words):
                baseline = numbers.MARK_HEIGHT + half_tall * 0.18 - index * capitals * 1.45
                lay_word(part, panel, line, part.named("Ink"), 0.02, lift + 0.002, baseline, scale, resolution=1)
    elif mark == "wave":
        # A stitched drawing, kept to its lower edge: a line of three humps.
        steps = 12
        lower, upper = [], []
        for index in range(steps + 1):
            along = -1.0 + 2.0 * index / steps
            hump = abs(math.sin(along * 1.5 * math.pi)) * 0.016
            lower.append(crown.front(along * wide, numbers.MARK_HEIGHT - hump - 0.006, lift))
            upper.append(crown.front(along * wide, numbers.MARK_HEIGHT - hump + 0.006, lift))
        full.add_skin(part, surface, [lower, upper], part.named("Mark"), around=False)
    elif mark:
        # A logo (a star, a peace sign, a bolt, a cloud, the Monad mark): chog_trait_hats.py.
        # (First: as large as the label, 0.063 from its middle: on the bust a small mark under the
        # leaves, where the pictures draw a logo half as tall as the crown.)
        hats_and_clothes.lay_logo(part, crown, mark, part.named("Mark"), (numbers.MARK_HEIGHT + 0.018, wide * 1.45),
                                  lift)
        if mark == "cloud" and maker.entry.get("swirls"):
            # The curls of the cloud of Cloud Cap (15 October 2026): a spiral in the colour of the hat
            # on each puff, as the pictures draw a cloud of curls.
            height, size = numbers.MARK_HEIGHT + 0.018, wide * 1.45
            for x, y, radius in ((-0.55, -0.05, 0.42), (0.05, 0.18, 0.55), (0.60, -0.02, 0.40)):
                places = []
                for index in range(9):
                    turn = math.radians(90.0 + 300.0 * index / 8)
                    reach = radius * (0.62 - 0.42 * index / 8)
                    places.append(((x + reach * math.cos(turn)) * size, height + (y + reach * math.sin(turn)) * size))
                hats_and_clothes.wall_band(part, crown, part.named("Swirl"), places, size * 0.06, lift + 0.002)


def build_crown(maker):
    """The shape "Crown": a band that flares, a line of trim at its foot, points, gems."""
    numbers = maker.numbers(CROWN)
    part, surface = maker.part(), maker.surface
    wall = CapCrown(numbers, surface.middle)
    sides = numbers.SIDES
    rim, trim, band, tips = (ring[0] for ring in numbers.RINGS)
    # The points: every other place of the last ring is a tip, the others a dip. A tip stands in
    # the middle of the front.
    points = [wall.around(tips if index % 2 == 0 else numbers.VALLEY, 2.0 * math.pi * index / sides)
              for index in range(sides)]
    whole, none = (1.0,) * sides, (0.0,) * sides
    if numbers.FLOOR is None:
        # An open band: up the outside to the points, down the inside (BAND_INSIDE as large) into the
        # head, so nothing closes it at the top; what is inside it is the head and its leaves.
        inside = numbers.BAND_INSIDE
        inner_points = [wall.around(tips if index % 2 == 0 else numbers.VALLEY, 2.0 * math.pi * index / sides, inside)
                        for index in range(sides)]
        bands, _ = add_loops(part, [wall.loop(rim), wall.loop(trim), wall.loop(band), points, inner_points,
                                    wall.loop(band, inside), wall.loop(rim, inside)], part.named("Gold"),
                             numbers.OUTLINE_THICKNESS, smooth=False,
                             shares=[whole, whole, whole, whole, none, none, none])
        part.paint(bands[0], part.named("Trim"))
    else:
        edge_height, edge_size, middle_height, middle_size, top_height = numbers.FLOOR
        # The middle ring of the floor and its top follow the crown as it leans back.
        lean = Vector((0.0, wall.at_height(middle_height)[2], 0.0))
        bands, _ = add_loops(part, [wall.loop(rim), wall.loop(trim), wall.loop(band), points,
                                    wall.loop(edge_height, edge_size),
                                    [point + Vector((0.0, 0.0, middle_height - edge_height)) for point in
                                     wall.loop(edge_height, middle_size)],
                                    [surface.middle + lean + Vector((0.0, 0.0, top_height))]], part.named("Gold"),
                             numbers.OUTLINE_THICKNESS, smooth=False,
                             shares=[whole, whole, whole, whole, none, none, (0.0,)])
        part.paint(bands[0], part.named("Trim"))
        part.paint(bands[4] + bands[5], part.named("Trim"))
    for degrees, height, half_width, half_height, material in numbers.GEMS:
        low, high = numbers.GEM_LIFT
        if material == "Trim":
            low, high = low * 0.5, low * 0.5      # the setting of a gem: flat, under it
        lay_disc(part, surface, wall, part.named(material), degrees, height, half_width, half_height, (low, high))


def build_beanie(maker):
    """The shape "Beanie": a dome, a thick ribbed cuff around the brow, rings printed on the dome."""
    numbers = maker.numbers(BEANIE)
    part, surface = maker.part(), maker.surface
    wall = CapCrown(numbers, surface.middle)
    add_dome(part, wall, part.named("Dome"), numbers.OUTLINE_THICKNESS)

    low, high, off, bulge = numbers.CUFF
    middle = (low + high) * 0.5
    sides = numbers.SIDES
    whole, none = (1.0,) * sides, (0.0,) * sides
    add_loops(part, [wall.loop(low, 0.95), wall.loop(low, 1.0 + off * 0.8), wall.loop(middle, 1.0 + off + bulge),
                     wall.loop(high, 1.0 + off * 0.8), wall.loop(high, 0.95)], part.named("Cuff"),
              numbers.OUTLINE_THICKNESS, shares=[none, whole, whole, whole, none])
    count, width, lift = numbers.RIBS
    for index in range(count):
        angle = 2.0 * math.pi * (index + 0.5) / count
        half = width * 0.5 / numbers.RIM_HALF_WIDTH
        stood = lift / numbers.RIM_HALF_WIDTH
        rows = [[wall.around(height, angle + side * half, size + stood) for side in (-1.0, 1.0)]
                for height, size in ((low + 0.008, 1.0 + off * 0.86), (middle, 1.0 + off + bulge),
                                     (high - 0.008, 1.0 + off * 0.86))]
        full.add_skin(part, surface, rows, part.named("Rib"), around=False)
    segments = 8
    for degrees, height, radius in numbers.PRINTS:
        here, along, upwards, out = wall.frame(degrees, height)
        rings = []
        for size in (numbers.PRINT_INSIDE, 1.0):
            rings.append([here + out * numbers.PRINT_LIFT
                          + (along * math.cos(2.0 * math.pi * index / segments)
                             + upwards * math.sin(2.0 * math.pi * index / segments)) * radius * size
                          for index in range(segments)])
        full.add_skin(part, surface, rings, part.named("Print"))
    if maker.entry.get("patch"):
        # A sewn patch on the dome (a pizza slice, a feather, a small Chog): chog_trait_hats.py.
        hats_and_clothes.lay_patch(part, wall, maker.entry["patch"], maker.entry["colours"], part.named)


# ---------------------------------------------------------------------------------------------
# Eyes.
# ---------------------------------------------------------------------------------------------

def build_anime_glass(maker):
    """Two thick lenses, each a slab whose front is a frame around a white glass, a bar between them
    and an arm from each to the side of the head."""
    numbers = maker.numbers(ANIME_GLASS)
    part, surface, head = maker.part(), maker.surface, maker.head
    lens_side, lens_up, half_width, half_height = numbers.LENS
    segments, thickness = numbers.LENS_SEGMENTS, numbers.THICKNESS
    frame, glass = part.named("Frame"), part.named("Lens")

    placed = {}
    for side in (1.0, -1.0):
        # The plane of the lens: between looking straight ahead and lying on the head.
        normal = ((-Y) * (1.0 - numbers.WRAP) + outward(surface, side * lens_side, lens_up) * numbers.WRAP).normalized()
        right = (X - normal * X.dot(normal)).normalized()
        up = normal.cross(right)
        centre = surface.front(side * lens_side, lens_up, head.FACE_LIFT) + normal * thickness * 0.5

        def ring(size, forward):
            points = []
            for index in range(segments):
                angle = 2.0 * math.pi * index / segments
                tall = half_height * power(math.sin(angle), numbers.LENS_SQUARENESS)
                across = half_width * power(math.cos(angle), numbers.LENS_SQUARENESS)
                across *= 1.0 + numbers.LENS_TAPER * tall / half_height
                points.append(centre + (right * across + up * tall) * size + normal * forward)
            return points

        front = thickness * 0.5
        bands, ends = add_loops(part, [
            ring(0.94, -front),
            ring(1.0, front),
            ring(1.0 - numbers.RIM, front + numbers.LENS_BULGE * 0.4),
            [centre + normal * (front + numbers.LENS_BULGE)],
        ], frame, numbers.OUTLINE_THICKNESS)
        part.paint(bands[2], glass)
        for face in bands[1] + bands[2] + ends:
            face.smooth = False
        placed[side] = (centre, normal, right, up)

    # The bar between the lenses, a little arched.
    half_tall, half_deep = numbers.BRIDGE
    ends = []
    for side in (1.0, -1.0):
        centre, normal, right, up = placed[side]
        ends.append((centre - right * side * half_width * 0.90 + up * numbers.BRIDGE_UP
                     + normal * (thickness * 0.5 - half_deep), up, normal))
    (start, up_start, normal_start), (end, up_end, normal_end) = ends
    up_middle, normal_middle = (up_start + up_end).normalized(), (normal_start + normal_end).normalized()
    part.shell([
        (start, up_start * half_tall, normal_start * half_deep),
        ((start + end) * 0.5 + up_middle * 0.005 + normal_middle * 0.004, up_middle * half_tall,
         normal_middle * half_deep),
        (end, up_end * half_tall, normal_end * half_deep),
    ], 4, frame, numbers.OUTLINE_THICKNESS * 0.7)

    # An arm from the outer side of each lens back to the side of the head, where it ends inside it.
    half_tall, half_wide = numbers.TEMPLE
    to_side, to_back, to_up = numbers.TEMPLE_END
    for side in (1.0, -1.0):
        centre, normal, right, up = placed[side]
        start = centre + right * side * half_width * 0.92 + up * numbers.TEMPLE_UP - normal * thickness * 0.2
        end = surface.towards(Vector((side * to_side, to_back, to_up)).normalized())
        end = surface.middle + (end - surface.middle) * 0.94
        along = (end - start).normalized()
        across = Z.cross(along).normalized()
        tall = along.cross(across)
        part.shell([(start, tall * half_tall, across * half_wide), (end, tall * half_tall, across * half_wide)],
                   4, frame, numbers.OUTLINE_THICKNESS * 0.7)


def draw_dark_eyes(part, surface, material, glint=None):
    """The two eyes of DARK_EYE. Returns where the middle of each is: (to the side, above)."""
    numbers = SimpleNamespace(**DARK_EYE)
    half_width, half_height = numbers.SIZE
    thick, outwards, inwards = numbers.BROW
    middles = []
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])

        def brow(place):
            """The height of the brow at a place: lower towards the nose."""
            return middle[1] + half_height * numbers.BROW_AT + numbers.BROW_SLOPE * (place - middle[0]) * side

        edge = [(place, min(up, brow(place))) for place, up in oval(middle, half_width, half_height, numbers.SEGMENTS)]
        draw_shape(part, surface, material, edge, DRAWN[0], middle=(middle[0], middle[1] - half_height * 0.15))
        steps = 4
        start, stop = middle[0] - side * (half_width + inwards), middle[0] + side * (half_width + outwards)
        places = [(start + (stop - start) * index / steps, 0.0) for index in range(steps + 1)]
        places = [(place, brow(place)) for place, _ in places]
        draw_line(part, surface, material, places, [thick] * (steps + 1), DRAWN[1])
        if glint:
            for to_side, above, wide, tall in numbers.GLINTS:
                full.add_skin(part, surface, full.patch_loops(surface, (middle[0] + to_side, middle[1] + above), wide,
                                                              tall, 2.0, 0.0, DRAWN[2], 1, 8), glint)
        middles.append(middle)
    return middles


def build_normal_eye(maker):
    """Two dark eyes under a slanted brow, each with two glints."""
    part = maker.part()
    draw_dark_eyes(part, maker.surface, part.named("Dark"), glint=part.named("Glint"))


def build_laser(maker):
    """The dark eyes of "Normal Eye" without their glints, and a short beam leaving each, level with
    the ground and straight ahead. The game draws the long beam itself when the hero attacks."""
    numbers = maker.numbers(LASER)
    part, surface = maker.part(), maker.surface
    ahead, down = level_axes(maker.head)
    radius, sides = numbers.BEAM_RADIUS, numbers.BEAM_SIDES
    for middle in draw_dark_eyes(part, surface, part.named("Dark")):
        start = surface.front(middle[0], middle[1], 0.0)
        stations = ((-numbers.BEAM_SUNK, 0.7), (0.02, 1.0), (numbers.BEAM_LENGTH - 0.03, 1.0),
                    (numbers.BEAM_LENGTH, 0.55))
        loops = [ring_points(start + ahead * distance, X * radius * size, -down * radius * size, sides)
                 for distance, size in stations]
        shares = [(0.0,) * sides] + [(1.0,) * sides] * (len(stations) - 1)
        bands, ends = add_loops(part, loops, part.named("Beam"), numbers.OUTLINE_THICKNESS, shares=shares)
        for band in bands:
            part.paint([band[index] for index in numbers.BEAM_CORE], part.named("Core"))
        part.paint(ends[-1:], part.named("Core"))


def build_happy(maker):
    """Two closed eyes: arches, thick in their middle."""
    numbers = maker.numbers(HAPPY)
    part, surface = maker.part(), maker.surface
    half_width, arch, thick = numbers.ARC
    steps = numbers.STEPS
    for side in (1.0, -1.0):
        places, widths = [], []
        for index in range(steps + 1):
            along = -1.0 + 2.0 * index / steps
            hollow = 1.0 - along * along
            places.append((side * numbers.AT[0] + along * half_width, numbers.AT[1] + arch * (hollow - 0.5)))
            widths.append(thick * (0.45 + 0.55 * hollow ** 0.7))
        draw_line(part, surface, part.named("Dark"), places, widths, DRAWN[0])


def build_round(maker):
    """Two round eyes: a white disc in a dark line, a large dark pupil in its middle."""
    numbers = maker.numbers(ROUND)
    part, surface = maker.part(), maker.surface
    layers = ((numbers.LINE, "Dark", DRAWN[0]), (numbers.WHITE, "White", DRAWN[1]), (numbers.PUPIL, "Dark", DRAWN[2]))
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        for (half_width, half_height), material, lift in layers:
            full.add_skin(part, surface, full.patch_loops(surface, middle, half_width, half_height, 2.0, 0.0, lift,
                                                          2, 14), part.named(material))


def build_visor(maker):
    """One band across the eyes, wrapped around the front of the head, cut into pieces across its
    width that take the colours of its entry from one end to the other."""
    numbers = maker.numbers(VISOR)
    part, surface, head = maker.part(), maker.surface, maker.head
    pieces = numbers.PIECES
    height = head.FACE_MIDDLE[1] + numbers.UP
    middle_tall, end_tall = numbers.HALF_HEIGHT
    loops, shares = [], []
    for index in range(len(pieces) + 1):
        across = -1.0 + 2.0 * index / len(pieces)
        angle = math.radians(across * numbers.AROUND_DEG)
        here = surface.middle + Vector((numbers.HALF_WIDTH * math.sin(angle), -numbers.HALF_DEPTH * math.cos(angle),
                                        height))
        out = Vector((math.sin(angle) / numbers.HALF_WIDTH, -math.cos(angle) / numbers.HALF_DEPTH, 0.0)).normalized()
        tall = middle_tall + (end_tall - middle_tall) * across * across
        back = here - out * numbers.THICKNESS
        loops.append([here + Z * tall, here + out * numbers.BULGE, here - Z * tall,
                      back - Z * tall * 0.96, back + Z * tall * 0.96])
        at_end = index in (0, len(pieces))
        shares.append((0.0,) * 5 if at_end else (1.0, 1.0, 1.0, 0.2, 0.2))
    bands, _ = add_loops(part, loops, part.named(pieces[0]), numbers.OUTLINE_THICKNESS, shares=shares)
    for band, material in zip(bands, pieces):
        part.paint(band, part.named(material))


# ---------------------------------------------------------------------------------------------
# Mouths.
# ---------------------------------------------------------------------------------------------

def build_gold_tooth(maker):
    """A wide grin painted on the face: a dark shape, two rows of teeth on it with a dark line
    between them, the top of each row in shade, and one gold tooth with its own dark line."""
    numbers = maker.numbers(GOLD_TOOTH)
    part, surface = maker.part(), maker.surface
    half_width, corners = numbers.HALF_WIDTH, numbers.CORNERS_UP
    line, bite = numbers.LINE, numbers.BITE_LINE
    steps = numbers.STEPS
    lift_line, lift_teeth, lift_shade, lift_tooth_line, lift_tooth, lift_shine = DRAWN

    def band(material, lift, wide, top, bottom, top_sag, bottom_sag, rows=1, start=-1.0, stop=1.0):
        """A strip across the grin between two curves, each given by the height of its ends and how
        far its middle sags; "start" and "stop" cut it short (-1 and 1 are the corners)."""
        loops = []
        for row in range(rows + 1):
            share = row / rows
            loop = []
            for index in range(steps + 1):
                along = start + (stop - start) * index / steps
                hollow = 1.0 - along * along
                upper = top - top_sag * hollow
                lower = bottom - bottom_sag * hollow
                loop.append(surface.front(along * wide, lower + (upper - lower) * share, lift))
            loops.append(loop)
        full.add_skin(part, surface, loops, material, around=False)

    # The dark shape: the grin made larger by the width of the line, with pointed corners.
    band(part.named("Line"), lift_line, half_width + line * 1.6, corners + line, corners + line,
         numbers.SAG_TOP, numbers.SAG_BOTTOM + 2.0 * line, rows=3)
    # The two rows of teeth; between them the dark shape shows as the line where they meet. They
    # stop short of the corners, which stay dark.
    reach = 0.965
    rows = (
        (numbers.SAG_TOP, numbers.SAG_BITE - bite * 0.5),
        (numbers.SAG_BITE + bite * 0.5, numbers.SAG_BOTTOM),
    )
    for (top_sag, bottom_sag), shade in zip(rows, numbers.SHADE):
        band(part.named("Teeth"), lift_teeth, half_width, corners, corners, top_sag, bottom_sag, rows=2,
             start=-reach, stop=reach)
        shade_sag = top_sag + (bottom_sag - top_sag) * shade
        band(part.named("Shade"), lift_shade, half_width, corners, corners, top_sag, shade_sag, start=-reach,
             stop=reach)

    # The gold tooth, across the line between the rows.
    half_top, half_bottom, above, below = numbers.TOOTH
    where = numbers.TOOTH_AT * half_width
    middle = corners - numbers.SAG_BITE * (1.0 - numbers.TOOTH_AT ** 2)
    # The line between the rows climbs towards the corner: the tooth leans with it.
    slope = 2.0 * numbers.SAG_BITE * numbers.TOOTH_AT / half_width * numbers.TOOTH_LEAN

    def tooth(material, lift, grow, size=1.0, shift=(0.0, 0.0)):
        corners_of = []
        for across, tall in ((-half_bottom, -below), (half_bottom, -below), (half_top, above), (-half_top, above)):
            across = across * size + math.copysign(grow, across) + shift[0]
            tall = tall * size + math.copysign(grow, tall) + shift[1]
            corners_of.append(surface.front(where + across, middle + tall + across * slope, lift))
        full.add_skin(part, surface, [corners_of[:2], [corners_of[3], corners_of[2]]], material, around=False)

    tooth(part.named("Line"), lift_tooth_line, numbers.TOOTH_LINE)
    tooth(part.named("Gold"), lift_tooth, 0.0)
    tooth(part.named("Shine"), lift_shine, 0.0, size=0.34, shift=(-half_top * 0.36, above * 0.30))


def build_smile(maker):
    """A thin line that sags in its middle."""
    numbers = maker.numbers(SMILE)
    part = maker.part()
    places, widths = [], []
    for index in range(numbers.STEPS + 1):
        along = -1.0 + 2.0 * index / numbers.STEPS
        hollow = 1.0 - along * along
        places.append((numbers.AT[0] + along * numbers.HALF_WIDTH, numbers.AT[1] - numbers.SAG * hollow))
        widths.append(numbers.THICKNESS * (0.45 + 0.55 * hollow))
    draw_line(part, maker.surface, part.named("Line"), places, widths, DRAWN[0])


def build_drool(maker):
    """A short mouth, and a thread of drool hanging from one of its ends, with its drop."""
    numbers = maker.numbers(DROOL)
    part, surface = maker.part(), maker.surface
    places, widths = [], []
    for index in range(numbers.STEPS + 1):
        along = -1.0 + 2.0 * index / numbers.STEPS
        hollow = 1.0 - along * along
        places.append((numbers.AT[0] + along * numbers.HALF_WIDTH, numbers.AT[1] + numbers.ARCH * (hollow - 1.0)))
        widths.append(numbers.THICKNESS * (0.6 + 0.4 * hollow))
    to_side, wide, hangs = numbers.DROOL
    start = (numbers.AT[0] + to_side, numbers.AT[1] - numbers.ARCH * (to_side / numbers.HALF_WIDTH) ** 2)
    thread = [(start[0], start[1] - hangs * index / 3) for index in range(4)]
    draw_line(part, surface, part.named("Drool"), thread, [wide, wide * 0.8, wide * 0.7, wide * 0.9], DRAWN[0])
    drop_wide, drop_tall = numbers.DROP
    full.add_skin(part, surface, full.patch_loops(surface, (start[0], start[1] - hangs - drop_tall * 0.5), drop_wide,
                                                  drop_tall, 2.0, -0.5, DRAWN[0], 1, 8), part.named("Drool"))
    draw_line(part, surface, part.named("Line"), places, widths, DRAWN[1])


def build_drool_open(maker):
    """A wide open mouth with its tongue, and a thick thread of drool running down from it."""
    numbers = maker.numbers(DROOL_OPEN)
    part, surface = maker.part(), maker.surface
    half_width, half_height = numbers.SIZE
    line = numbers.LINE
    full.add_skin(part, surface, full.patch_loops(surface, numbers.AT, half_width, half_height, 2.3, 0.15, DRAWN[0],
                                                  2, 12), part.named("Line"))
    full.add_skin(part, surface, full.patch_loops(surface, numbers.AT, half_width - line, half_height - line, 2.3,
                                                  0.15, DRAWN[1], 2, 12), part.named("Inside"))
    below, wide, tall = numbers.TONGUE
    full.add_skin(part, surface, full.patch_loops(surface, (numbers.AT[0], numbers.AT[1] + below), wide, tall, 2.0,
                                                  0.0, DRAWN[2], 1, 10), part.named("Tongue"))
    to_side, width, runs = numbers.DROOL
    start = (numbers.AT[0] + to_side, numbers.AT[1] - half_height * 0.45)
    thread = [(start[0], start[1] - runs * index / 3) for index in range(4)]
    draw_line(part, surface, part.named("Drool"), thread, [width * 1.5, width, width * 0.9, width * 0.6], DRAWN[3])


def build_wo(maker):
    """A small round mouth, open: a dark oval with a glint."""
    numbers = maker.numbers(WO)
    part, surface = maker.part(), maker.surface
    half_width, half_height = numbers.SIZE
    full.add_skin(part, surface, full.patch_loops(surface, numbers.AT, half_width, half_height, 2.0, 0.0, DRAWN[0],
                                                  1, 10), part.named("Line"))
    to_side, above, radius = numbers.GLINT
    full.add_skin(part, surface, full.patch_loops(surface, (numbers.AT[0] + to_side, numbers.AT[1] + above), radius,
                                                  radius, 2.0, 0.0, DRAWN[1], 1, 6), part.named("Glint"))


def build_rainbow(maker):
    """An open mouth drawn on the face, and a rainbow leaving it: a stream with real volume that
    falls in front of the body, its six stripes side by side."""
    numbers = maker.numbers(RAINBOW)
    part, surface = maker.part(), maker.surface
    ahead, down = level_axes(maker.head)
    to_side, above, half_width, half_height = numbers.MOUTH
    full.add_skin(part, surface, full.patch_loops(surface, (to_side, above), half_width, half_height, 2.4, 0.0,
                                                  DRAWN[0], 2, 12), part.named("Dark"))
    mouth = surface.front(to_side, above, 0.0)
    stripes = numbers.STRIPES
    path = [mouth + ahead * forward + down * below for forward, below, _, _ in numbers.PATH]
    path.append(mouth + ahead * numbers.END[0] + down * numbers.END[1])
    loops, shares = [], []
    for index, (_, _, wide, thick) in enumerate(numbers.PATH):
        along = (path[index + 1] - path[max(index - 1, 0)]).normalized()
        top = along.cross(X).normalized()          # the side of the stream that is seen: ahead and up
        if top.dot(ahead - down) < 0.0:
            top = -top
        loop = []
        for place in range(len(stripes) + 1):
            across = -1.0 + 2.0 * place / len(stripes)
            # The stripes run from the right of the character (-X) to its left.
            loop.append(path[index] + X * across * wide + top * thick * (0.5 - numbers.ARCH * across * across))
        loop.append(path[index] - top * thick * 0.5)
        loops.append(loop)
        shares.append((0.0 if index == 0 else 1.0,) * len(loop))
    loops.append([path[-1]])
    shares.append((1.0,))
    bands, ends = add_loops(part, loops, part.named(stripes[0]), numbers.OUTLINE_THICKNESS, shares=shares)
    for band in bands:
        for place, face in enumerate(band):
            # The last two faces of a section are its underside: they take the colours beside them.
            stripe = stripes[min(place, len(stripes) - 1)] if place <= len(stripes) else stripes[0]
            part.paint([face], part.named(stripe))


def build_bubblegum(maker):
    """A bubble of gum in front of the mouth: a ball with real volume, the knot that holds it, and a
    glint painted on it."""
    numbers = maker.numbers(BUBBLE)
    part, surface = maker.part(), maker.surface
    ahead, down = level_axes(maker.head)
    mouth = surface.front(numbers.AT[0], numbers.AT[1], 0.0)
    out = (outward(surface, numbers.AT[0], numbers.AT[1]) + ahead).normalized()
    centre = mouth + out * numbers.OFF
    radius = numbers.RADIUS
    part.shell(base.ball_rings(centre, out, X, radius, radius, radius, numbers.STACKS), numbers.SIDES,
               part.named("Gum"), numbers.OUTLINE_THICKNESS)
    knot = numbers.KNOT
    part.shell(base.tube_rings(mouth - out * 0.02, out, X, ((0.0, knot * 0.6), (numbers.OFF - radius * 0.8, knot))),
               6, part.named("Gum"), numbers.OUTLINE_THICKNESS * 0.5)
    # The glint: a small patch just off the ball, on its upper side.
    to_right, up_deg, wide, tall = numbers.GLINT
    turn, lift = math.radians(to_right), math.radians(up_deg)
    towards = (out * math.cos(turn) * math.cos(lift) - X * math.sin(turn) * math.cos(lift)
               - down * math.sin(lift)).normalized()
    across = (-down).cross(towards).normalized()
    along = towards.cross(across)
    here = centre + towards * (radius + 0.004)
    edge = [here + across * wide * math.cos(2.0 * math.pi * index / 8) + along * tall * math.sin(2.0 * math.pi * index / 8)
            for index in range(8)]
    edge = [centre + (point - centre).normalized() * (radius + 0.004) for point in edge]
    full.add_skin(part, SimpleNamespace(middle=centre), [[here], edge], part.named("Glint"))


# ---------------------------------------------------------------------------------------------
# Clothing.
# ---------------------------------------------------------------------------------------------

def torso_section(body, height):
    """Half the width and half the depth of the torso at a height above the ground."""
    rings = body.TORSO_RINGS
    height = min(max(height, rings[0][0]), rings[-1][0])
    for lower, upper in zip(rings, rings[1:]):
        if height <= upper[0]:
            amount = (height - lower[0]) / (upper[0] - lower[0])
            return ((lower[1] + (upper[1] - lower[1]) * amount) * 0.5,
                    (lower[2] + (upper[2] - lower[2]) * amount) * 0.5)
    raise SystemExit("CHOG_TRAITS the rings of the torso are not in order")


def build_tee(maker):
    """A shirt: a thin shell over the torso from the hips to the neck, and one over the top of each
    arm. It has no outline of its own: it lies inside the outline of the body. Bands of the same
    height take the stripe colour in turn when the entry asks for stripes."""
    numbers = maker.numbers(TEE)
    body = maker.body
    striped = bool(maker.entry.get("stripes"))
    lift = numbers.LIFT

    torso = maker.part(on="Body")
    collar_from, collar_to = numbers.COLLAR
    heights = [numbers.HEM]
    while heights[-1] + numbers.STEP < collar_from - 0.012:
        heights.append(heights[-1] + numbers.STEP)
    heights += [collar_from, collar_to]
    loops = []
    for index, height in enumerate(heights):
        half_width, half_depth = torso_section(body, height)
        flare = numbers.HEM_FLARE if index == 0 else 0.0
        loops.append(ring_points(Z * height, X * (half_width + lift + flare), Y * (half_depth + lift + flare),
                                 numbers.SIDES))
    bands, _ = add_loops(torso, loops, torso.named("Main"), 0.0)
    if striped:
        for band in bands[1:-1:2]:
            torso.paint(band, torso.named("Stripe"))
    torso.paint(bands[-1], torso.named("Collar"))
    # A word, a logo, a pattern, patches on the front (chog_trait_hats.py, 15 October 2026).
    hats_and_clothes.decorate_shirt(maker, torso)

    if numbers.SLEEVE <= 0.0:
        return
    spread, ahead = math.radians(body.ARM_SPREAD_DEG), math.radians(body.ARM_FORWARD_DEG)
    ball, radius = body.SHOULDER_RADIUS, body.ARM_RADIUS
    for name, side in (("armLeft", 1.0), ("armRight", -1.0)):
        sleeve = maker.part(on=name)
        down = Vector((side * math.sin(spread), -math.sin(ahead), -math.cos(spread) * math.cos(ahead))).normalized()
        first, second = base.frame_across(down, -Y)
        stations = [(-ball * 0.70, ball * 0.72), (0.0, ball), (ball * 0.6, ball + (radius - ball) * 0.6),
                    (ball * 1.2, radius)]
        while stations[-1][0] + numbers.STEP < numbers.SLEEVE - 0.010:
            stations.append((stations[-1][0] + numbers.STEP, radius))
        stations.append((numbers.SLEEVE, radius + numbers.SLEEVE_FLARE))
        loops = [[down * (-ball - lift)]]
        loops += [ring_points(down * distance, first * (size + lift), second * (size + lift), numbers.ARM_SIDES)
                  for distance, size in stations]
        bands, _ = add_loops(sleeve, loops, sleeve.named("Main"), 0.0)
        if striped:
            for band in bands[1::2]:
                sleeve.paint(band, sleeve.named("Stripe"))


hats_and_clothes.bind(sys.modules[__name__])
HAT_SHAPES = {"Cap": build_cap, "BucketCap": build_bucket, "Crown": build_crown, "Beanie": build_beanie,
              **hats_and_clothes.HAT_SHAPES}
# The numbers of each hat shape: where its "LEAVES" are.
HAT_NUMBERS = {"Cap": CAP, "BucketCap": BUCKET, "Crown": CROWN, "Beanie": BEANIE, **hats_and_clothes.HAT_NUMBERS}


def hat_numbers_of(entry, replaced=None):
    """The numbers of the hat of an entry of HATS (those of its shape, those it replaces), or {}."""
    if not isinstance(entry, dict) or entry.get("shape") not in HAT_NUMBERS:
        return {}
    return {**HAT_NUMBERS[entry["shape"]], **entry.get("numbers", {}), **(replaced or {})}


def face_numbers(head, hat_numbers, left=False):
    """The numbers of the head for its face: without the teeth of hair on the brow under a hat whose
    numbers say "FRINGE": False, and on Side=Left, whose head is all cream (cream_head)."""
    if hat_numbers.get("FRINGE", True) and not left:
        return head
    return SimpleNamespace(**{**vars(head), "FRINGE_TEETH": 0})


def crown_of(entry, replaced=None):
    """The leaves a Genesis keeps (see leaf() in chog_trait_hats.py): those its hat leaves, the whole
    crown when it has no hat or a hat that is not modelled."""
    return hat_numbers_of(entry, replaced).get("LEAVES", FULL_CROWN)


def head_look(traits, replaced=None):
    """What decides the leaves, the points of hair and the teeth of hair on the brow of a Genesis
    ("LEAVES", "HAIR_POINTS", "FRINGE"): the numbers of its hat; with no hat, what a hooded Body entry
    (a hood up, a costume) gives in its "head"; else nothing (the whole crown)."""
    if not traits:
        return {}
    numbers = hat_numbers_of(entry_for("Head", traits), replaced)
    if not numbers:
        body = entry_for("Body", traits)
        if isinstance(body, dict):
            numbers = dict(body.get("head", {}))
    return numbers
# The eyes and mouths of chog_trait_faces.py use the helpers of this file. Its "Laser" draws the
# lasers of Side=Left and hands those of Side=Right to build_laser above.
faces.attach(sys.modules[__name__])
EYE_KINDS = {"AnimeGlass": build_anime_glass, "NormalEye": build_normal_eye, "Laser": build_laser,
             "Happy": build_happy, "Round": build_round, "Visor": build_visor, **faces.EYE_KINDS}
MOUTH_KINDS = {"GoldTooth": build_gold_tooth, "Smile": build_smile, "Drool": build_drool,
               "DroolOpen": build_drool_open, "Wo": build_wo,
               "RainbowPuke": build_rainbow, "Bubblegum": build_bubblegum, **faces.MOUTH_KINDS}
CLOTH_KINDS = {"Tee": build_tee, **hats_and_clothes.CLOTH_KINDS}
# trait of the collection: (start of the name of its part, its table, the word that names the
# builder in an entry, the builders). Clothing comes first: it is under everything else.
OF_SIDE = {"Mouth": faces.by_side(MOUTHS_OF_SIDE, faces.MOUTHS_OF_SIDE), "Eyes": faces.EYES_OF_SIDE}
TRAIT_PARTS = {
    "Body": ("Cloth", CLOTHES, "kind", CLOTH_KINDS),
    "Head": ("Hat", HATS, "shape", HAT_SHAPES),
    "Eyes": ("Eyes", {**EYES, **faces.EYES}, "kind", EYE_KINDS),
    "Mouth": ("Mouth", {**MOUTHS, **faces.MOUTHS}, "kind", MOUTH_KINDS),
}
# Hats and clothing that a Side draws otherwise (Mon Cap, Russian Hat, Tuxedo): chog_trait_hats.py.
OF_SIDE.update({"Head": hats_and_clothes.HATS_OF_SIDE, "Body": hats_and_clothes.CLOTHES_OF_SIDE})
# Batch 4: every value of "Head" (on each Side) gets the leaves and the numbers its pictures ask for
# (HEAD_VALUES of chog_trait_hats.py), which replace those of its shape.
OF_SIDE["Head"] = hats_and_clothes.from_the_art(HATS, OF_SIDE["Head"])


def head_from_the_art(traits):
    """What HEAD_VALUES says of the value of "Head" of a Genesis on its Side: the CHOG numbers looked
    at and what their pictures show; {} when it has no line there (or no hat)."""
    if not traits or "Head" not in traits:
        return {}
    entry = OF_SIDE["Head"].get(traits.get("Side"), {}).get(traits["Head"])
    return dict(entry.get("from_the_art", {})) if entry else {}


# ---------------------------------------------------------------------------------------------
# A Genesis: its traits, its objects, the report.
# ---------------------------------------------------------------------------------------------

def traits_of(number):
    """The traits of a Genesis, by the number shown in its name, from the recorded metadata."""
    if not os.path.isfile(METADATA):
        raise SystemExit(f"CHOG_TRAITS no metadata at {METADATA}")
    with open(METADATA, encoding="utf-8") as lines:
        for line in lines:
            if not line.strip():
                continue
            record = json.loads(line)
            if int(record["metadata_id"]) == int(number):
                return {str(item["trait_type"]): str(item["value"])
                        for item in json.loads(record["raw_json"]).get("attributes", [])}
    raise SystemExit(f"CHOG_TRAITS no Genesis named CHOG #{number} in the recorded metadata")


def entry_for(trait, traits):
    """The entry a Genesis gets for a trait, on its Side: "none" when it has no value for that trait,
    None when its value (on that Side) has no part whose shape or kind is built."""
    _, table, word, builders = TRAIT_PARTS[trait]
    value = traits.get(trait)
    if value is None:
        return "none"
    entry = OF_SIDE.get(trait, {}).get(traits.get("Side"), {}).get(value) or table.get(value)
    return entry if entry and entry.get(word) in builders else None


def coverage():
    """Prints, from the recorded metadata, how many Genesis each part serves (on the Side each value
    is drawn), the values with no part, and how many Genesis have their hat, their eyes, their mouth,
    and all three right: a Genesis without the trait counts as right for it (there is nothing to show)."""
    tokens = []
    with open(METADATA, encoding="utf-8") as lines:
        for line in lines:
            if line.strip():
                record = json.loads(line)
                tokens.append({str(item["trait_type"]): str(item["value"])
                               for item in json.loads(record["raw_json"]).get("attributes", [])})
    origin = [traits for traits in tokens if traits.get("Base") == "Origin"]
    print(f"CHOG_COVERAGE {len(tokens)} Genesis in {os.path.relpath(METADATA, REPOSITORY)}, {len(origin)} on the "
          f"base Origin (the only base built)")
    for trait, (start, _, word, _) in TRAIT_PARTS.items():
        by_part, missing = {}, {}
        for traits in tokens:
            entry = entry_for(trait, traits)
            if entry == "none":
                continue
            label = f"{traits[trait]} [{traits.get('Side')}]"
            if entry is None:
                missing[label] = missing.get(label, 0) + 1
            else:
                values = by_part.setdefault(f"{start}_{entry[word]}", {})
                values[label] = values.get(label, 0) + 1
        for name, values in sorted(by_part.items(), key=lambda item: -sum(item[1].values())):
            shown = ", ".join(f"{label} {count}" for label, count in sorted(values.items(), key=lambda item: -item[1]))
            print(f"CHOG_COVERAGE {name:22s} serves {sum(values.values()):4d} Genesis: {shown}")
        have = sum(1 for traits in tokens if trait in traits)
        print(f"CHOG_COVERAGE   trait {trait}: {have - sum(missing.values())} of the {have} Genesis that have one wear "
              f"a built part, {len(by_part)} parts")
        if missing:
            shown = ", ".join(f"{label} {count}" for label, count in sorted(missing.items(), key=lambda item: -item[1]))
            print(f"CHOG_COVERAGE   {trait} with no part: {shown}")

    # Batch 4: which value of "Head", on its Side, has its leaves from its pictures (HEAD_VALUES), and
    # which is still decided by its shape alone.
    per_value, by_shape = {}, {}
    for traits in tokens:
        entry = entry_for("Head", traits)
        if isinstance(entry, dict):
            label = f"{traits['Head']} [{traits.get('Side')}]"
            chosen = per_value if entry.get("from_the_art") else by_shape
            chosen[label] = chosen.get(label, 0) + 1
    print(f"CHOG_COVERAGE   leaves from the pictures of the value (HEAD_VALUES): {len(per_value)} values, "
          f"{sum(per_value.values())} Genesis")
    print(f"CHOG_COVERAGE   leaves from the hat shape only: {len(by_shape)} values, {sum(by_shape.values())} Genesis"
          + (": " + ", ".join(f"{label} {count}" for label, count in sorted(by_shape.items(), key=lambda item: -item[1]))
             if by_shape else ""))

    def right(traits, trait):
        return entry_for(trait, traits) is not None

    for label, chosen in (("all Genesis", tokens), ("on the base Origin", origin)):
        counts = {trait: sum(1 for traits in chosen if right(traits, trait)) for trait in ("Head", "Eyes", "Mouth")}
        three = sum(1 for traits in chosen if all(right(traits, trait) for trait in ("Head", "Eyes", "Mouth")))
        four = sum(1 for traits in chosen if all(right(traits, trait) for trait in TRAIT_PARTS))
        print(f"CHOG_COVERAGE right, {label} ({len(chosen)}): hat {counts['Head']}, eyes {counts['Eyes']}, mouth "
              f"{counts['Mouth']}, hat and eyes and mouth {three}, and the clothing too {four}")
    print("CHOG_COVERAGE 'right' means: the value, on the Side of the Genesis, has a part whose shape or kind is built "
          "(or the Genesis has no value for that trait). A print not drawn yet (the word of a cap) still counts. "
          "Not counted and not modelled: the traits Accessory and Aura, the bases other than Origin.")


def budget():
    """Builds every entry once, alone, counts its triangles (outline shells included), then adds them
    up for every Genesis on the base Origin: the triangles of each dressed hero, without building the
    1,869 of them. Prints the largest, and every hero over TRIANGLE_LIMIT."""
    tokens = []
    with open(METADATA, encoding="utf-8") as lines:
        for line in lines:
            if line.strip():
                record = json.loads(line)
                traits = {str(item["trait_type"]): str(item["value"])
                          for item in json.loads(record["raw_json"]).get("attributes", [])}
                traits["number"] = str(record["metadata_id"])
                if traits.get("Base") == "Origin":
                    tokens.append(traits)
    root, parts, _, body_n, head_n, _ = build(None)
    base_total = sum(len(part.data.polygons) for part in parts)
    shared = {OUTLINE: next(material for material in bpy.data.materials if material.name == OUTLINE)}
    surface = full.HeadSurface(head_n)
    counted = {}
    # The head is not the same under every hat: it keeps the leaves its hat leaves (crown_of).
    without_head = base_total - sum(len(part.data.polygons) for part in parts if part.name == "Head")
    base_materials = {material.name: material for material in bpy.data.materials}
    heads = {}

    def base_of(traits):
        hat_numbers = head_look(traits)
        entry = head_entry(traits)
        rim = id(entry) if entry is not None and entry.get("under_rim") is not None else None
        key = (hat_numbers.get("LEAVES", FULL_CROWN), hat_numbers.get("HAIR_POINTS"), hat_numbers.get("FRINGE", True),
               rim)
        if key not in heads:
            head = variants.PartShapes(full.MATERIAL_ORDER)
            full.add_head(head, head_n)
            add_leaf_crown(head, head_n, surface, key[0])
            rim_leaves_for(head, traits, None, head_n, body_n, surface)
            if key[1]:
                hats_and_clothes.add_hair_points(head, surface, key[1], SKIN)
            full.add_face(head, face_numbers(head_n, hat_numbers), surface, eyes=False, mouth=False)
            thing = head.finish("ChogBudgetHead", Vector((0.0, 0.0, 0.0)), None, base_materials)
            heads[key] = without_head + len(thing.data.polygons)
            data = thing.data
            bpy.data.objects.remove(thing)
            bpy.data.meshes.remove(data)
        return heads[key]

    def triangles_of(trait, entry, traits):
        key = (trait, id(entry))
        if key not in counted:
            start, _, word, builders = TRAIT_PARTS[trait]
            maker = Maker(f"{start}_{entry[word]}", entry, head_n, body_n, surface)
            builders[entry[word]](maker)
            made = trait_materials(maker.prefix, entry["colours"], entry.get("unlit", ()), shared)
            total = 0
            for shapes, _ in maker.parts:
                thing = shapes.finish(shapes.name, Vector((0.0, 0.0, 0.0)), None, made)
                total += len(thing.data.polygons)
                data = thing.data
                bpy.data.objects.remove(thing)
                bpy.data.meshes.remove(data)
            counted[key] = (total, f"{start}_{entry[word]} ({trait}={traits[trait]}, Side={traits.get('Side')})")
        return counted[key][0]

    totals, worn = [], {}
    for traits in tokens:
        total = base_of(traits)
        worn.setdefault(traits["number"], []).append(f"base {total}")
        hidden = set(head_look(traits).get("HIDES", ()))
        for trait in TRAIT_PARTS:
            entry = entry_for(trait, traits)
            if isinstance(entry, dict) and trait not in hidden:
                count = triangles_of(trait, entry, traits)
                total += count
                worn.setdefault(traits["number"], []).append(f"{traits[trait]} {count}")
        totals.append((total, traits["number"]))
    totals.sort(reverse=True)
    for total, number in totals[:3]:
        print(f"CHOG_BUDGET #{number} {total}: " + ", ".join(worn[number]))
    over = [(total, number) for total, number in totals if total > TRIANGLE_LIMIT]
    print(f"CHOG_BUDGET base {base_total} triangles with the whole crown, "
          f"{min(heads.values())} to {max(heads.values())} under the hats ({len(heads)} sets of leaves); "
          f"{len(counted)} entries built once; {len(totals)} heroes on the base Origin")
    print(f"CHOG_BUDGET largest dressed heroes: " + ", ".join(f"#{number} {total}" for total, number in totals[:8]))
    print(f"CHOG_BUDGET median {totals[len(totals) // 2][0]}, smallest {totals[-1][0]}; over {TRIANGLE_LIMIT}: "
          f"{len(over)} " + ", ".join(f"#{number} {total}" for total, number in over[:20]))
    for (trait, _), (total, label) in sorted(counted.items(), key=lambda item: -item[1][0])[:10]:
        print(f"CHOG_BUDGET heaviest part: {label} {total}")


def build(traits=None, replaced=None, head_replaced=None):
    """Empties the scene and builds the polished base, then the parts of each trait that has an
    entry. "replaced" gives numbers put in place of those of the hat, "head_replaced" of the head, to
    try some. Returns the root, the four parts, the trait parts, the numbers of the body and of the
    head, and the trait values left out."""
    body_n, head_n = full.body_numbers(), head_numbers(head_replaced)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    materials = full.make_materials()

    root = bpy.data.objects.new(base.ROOT_NAME, None)
    root.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(root)

    body = variants.PartShapes(full.MATERIAL_ORDER)
    variants.add_torso(body, body_n)
    variants.add_legs_and_feet(body, body_n)

    head = variants.PartShapes(full.MATERIAL_ORDER)
    surface = full.HeadSurface(head_n)
    full.add_head(head, head_n)
    hat_numbers = head_look(traits, replaced)
    leaves = add_leaf_crown(head, head_n, surface, hat_numbers.get("LEAVES", FULL_CROWN))
    leaves += rim_leaves_for(head, traits, replaced, head_n, body_n, surface)
    left = (traits or {}).get("Side") == "Left"
    if left:
        cream_head(head)
    # Points of hair that a hat lets out on the brow (the durag), in the skin colour: part of the head.
    if hat_numbers.get("HAIR_POINTS"):
        hats_and_clothes.add_hair_points(head, surface, hat_numbers["HAIR_POINTS"], SKIN)
    full.add_face(head, face_numbers(head_n, hat_numbers, left), surface, eyes=False, mouth=False)
    full.turn_head_up(head, head_n)

    arms = {}
    for name, side in (("armLeft", 1.0), ("armRight", -1.0)):
        arms[name] = variants.PartShapes(full.MATERIAL_ORDER)
        variants.add_arm(arms[name], body_n, side)

    shoulder = body_n.SHOULDER_HALF_WIDTH
    parts = [
        body.finish("Body", Vector((0.0, 0.0, 0.0)), root, materials, on_ground=True),
        head.finish("Head", at(0.0, 0.0, full.NECK_HEIGHT), root, materials),
        arms["armLeft"].finish("armLeft", at(shoulder, 0.0, body_n.SHOULDER_HEIGHT), root, materials),
        arms["armRight"].finish("armRight", at(-shoulder, 0.0, body_n.SHOULDER_HEIGHT), root, materials),
    ]
    by_name = {part.name: part for part in parts}

    trait_parts, left_out = [], []
    # A hat that covers a trait (the balaclava over the mouth) says it in "HIDES": that part is not
    # built, as the pictures do not show it. One whose leaves come through it (a knitted mask, as the
    # pictures draw it) says "THROUGH": its crossings are kept in parts.json, not refused.
    hidden = set(hat_numbers.get("HIDES", ()))
    through = bool(hat_numbers.get("THROUGH"))
    for trait, (start, table, word, builders) in TRAIT_PARTS.items():
        value = (traits or {}).get(trait)
        if value is None:
            continue
        if trait in hidden:
            print(f"CHOG_TRAITS {trait}={value} is under the hat: not built")
            continue
        entry = OF_SIDE.get(trait, {}).get((traits or {}).get("Side"), {}).get(value) or table.get(value)
        if entry is None or entry[word] not in builders:
            left_out.append(f"{trait}={value}")
            continue
        if replaced and trait == "Head":
            entry = {**entry, "numbers": {**entry.get("numbers", {}), **replaced}}
        maker = Maker(f"{start}_{entry[word]}", entry, head_n, body_n, surface)
        builders[entry[word]](maker)
        crossing = []
        worn_on_head = [shapes for shapes, on in maker.parts if on == "Head"]
        if trait in ("Head", "Body") and worn_on_head:
            # A hat, or the hood of a costume. Before the head and the part are turned up: both are
            # still in the axes of add_leaf_crown.
            crossing = leaf_crossings(leaves, worn_on_head, head_n, surface)
        trait_made = trait_materials(maker.prefix, entry["colours"], entry.get("unlit", ()), materials)
        for shapes, on in maker.parts:
            if on == "Head":
                full.turn_head_up(shapes, head_n)
            made = shapes.finish(shapes.name, Vector((0.0, 0.0, 0.0)), by_name[on], trait_made)
            made["trait"] = f"{trait}={value}"
            made["prefix"] = maker.prefix
            # What the pictures show on this value that its part does not draw yet (a word, a logo).
            made["missing"] = entry.get("print_missing", "")
            # The leaves of the crown that go through this part (a hat): none is allowed, but where the
            # hat says the pictures draw them through it.
            on_the_head = crossing if on == "Head" else []
            made["leaves_crossing"] = "" if through else ", ".join(on_the_head)
            made["leaves_through"] = ", ".join(on_the_head) if through else ""
            trait_parts.append(made)

    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)
    bpy.context.view_layer.update()
    return root, parts, trait_parts, body_n, head_n, left_out


def report(label, parts, trait_parts, body_n, head_n):
    """The report of make_chog_full.py for the four parts, then the trait parts and the total.
    Returns the counts: triangles of the base, of each trait part, and in all."""
    full.report(label, parts, body_n, head_n)
    total = sum(len(part.data.polygons) for part in parts)
    counts = {"base": total, "parts": {}}
    print(f"CHOG_TRAITS {label} base: {total} triangles")
    failures = []
    for part in trait_parts:
        names = [slot.material.name for slot in part.material_slots]
        triangles = len(part.data.polygons)
        outline = sum(1 for face in part.data.polygons if names[face.material_index] == OUTLINE)
        total += triangles
        counts["parts"][part.name] = triangles
        print(f"CHOG_TRAITS {label} part {part.name:22s} on {part.parent.name:8s} triangles {triangles:5d} "
              f"(outline {outline:4d})  for {part['trait']}  materials {names}")
        if part.parent is None or part.parent.name not in ("Head", "Body", "armLeft", "armRight"):
            failures.append(f"{part.name} is not a child of one of the four parts")
        if part.matrix_parent_inverse != Matrix.Identity(4) or part.location.length > 1e-9:
            failures.append(f"{part.name} does not sit exactly on {part.parent.name}")
        if any(len(face.vertices) != 3 for face in part.data.polygons):
            failures.append(f"{part.name} has faces that are not triangles")
        if any(not name.startswith(part["prefix"] + "_") and name != OUTLINE for name in names):
            failures.append(f"{part.name} has a material that is not named after its trait: {names}")
        if part.get("leaves_crossing"):
            failures.append(f"leaves of the crown go through {part.name}: {part['leaves_crossing']}")
    print(f"CHOG_TRAITS {label} dressed: {total} triangles (limit {TRIANGLE_LIMIT})")
    if total > TRIANGLE_LIMIT:
        failures.append(f"{total} triangles, the limit is {TRIANGLE_LIMIT}")
    if failures:
        raise SystemExit(f"CHOG_TRAITS {label} FAILED: " + "; ".join(failures))
    counts["total"] = total
    return counts


def numbers_given(items):
    """The numbers of NAME=VALUE words (--set, --head-set)."""
    given = {}
    for item in items:
        name, value = item.split("=", 1)
        given[name] = ast.literal_eval(value)
    return given


def main():
    parser = argparse.ArgumentParser(description="Builds a Chog dressed with the traits of one Genesis.")
    parser.add_argument("--genesis", default=None, help="the number shown in the name of the Genesis (CHOG #n)")
    parser.add_argument("--base", action="store_true", help="the polished base alone, no trait part")
    parser.add_argument("--coverage", action="store_true", help="build nothing: print how many Genesis each "
                        "entry serves and how many are dressed whole today")
    parser.add_argument("--budget", action="store_true", help="build each entry once and print the triangles of "
                        "every dressed hero on the base Origin, the largest first")
    parser.add_argument("--hat", default=None, help=f"wear this entry of HATS in place of the hat of the Genesis: "
                                                    f"{sorted(HATS)}")
    parser.add_argument("--set", action="append", default=[], metavar="NAME=VALUE", help="a number of the hat put "
                        "in place of its own, for instance BRIM_LENGTH=0.06 (may be given several times)")
    parser.add_argument("--head-set", action="append", default=[], metavar="NAME=VALUE", help="a number of the head "
                        "put in place of its own, for instance LEAF_LENGTH=0.85 (may be given several times)")
    parser.add_argument("--out", default=None, help="folder for ChogBody.fbx and ChogBody.blend (default: "
                                                    ".artifacts/chog-body/traits/<number>, or traits/base)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    if arguments.coverage:
        coverage()
        return
    if arguments.budget:
        budget()
        return
    if bool(arguments.genesis) == arguments.base:
        raise SystemExit("CHOG_TRAITS give either --genesis NUMBER or --base")
    make_one(None if arguments.base else arguments.genesis, arguments.out, arguments.hat,
             numbers_given(arguments.set), numbers_given(arguments.head_set))


def make_one(number, out=None, hat=None, replaced=None, head_replaced=None):
    """Builds one dressed Genesis (or the base when "number" is None) and writes ChogBody.blend,
    ChogBody.fbx and parts.json into "out" (default: .artifacts/chog-body/traits/<number>). Returns
    the folder. make_chog_traits_many.py calls it for many Genesis in one run of Blender."""
    replaced, head_replaced = replaced or {}, head_replaced or {}
    if number is None:
        label, traits = "base", None
    else:
        label, traits = str(number), traits_of(number)
        print(f"CHOG_TRAITS CHOG #{label}: " + ", ".join(f"{key}={value}" for key, value in traits.items()))
        if hat:
            if hat not in HATS:
                raise SystemExit(f"CHOG_TRAITS no hat named {hat!r} in HATS")
            traits["Head"] = hat
            label = f"{label}_{hat.replace(' ', '')}"
    if (replaced or head_replaced) and not out:
        raise SystemExit("CHOG_TRAITS with --set or --head-set, give --out: a try does not replace the model of "
                         "the Genesis")
    folder = os.path.abspath(out or os.path.join(TRAITS_FOLDER, label))

    root, parts, trait_parts, body_n, head_n, left_out = build(traits, replaced, head_replaced)
    counts = report(label, parts, trait_parts, body_n, head_n)
    distinct = sorted({slot.material.name for part in parts + trait_parts for slot in part.material_slots})
    print(f"CHOG_TRAITS {label} {len(parts) + len(trait_parts)} mesh objects (and the root, an empty), "
          f"{len(distinct)} distinct materials: {distinct}")
    for value in left_out:
        print(f"CHOG_TRAITS {label} NOT MODELLED, left out: {value}")

    # What the renders need to know of this Genesis, kept in the file.
    if traits:
        skin = SKINS.get(traits.get("Skin"))
        if skin is None:
            raise SystemExit(f"CHOG_TRAITS no colour recorded for Skin={traits.get('Skin')}")
        root["chog_genesis"] = str(number)
        root["chog_skin"] = list(skin["skin"])
        root["chog_face"] = list(skin.get("face", GENESIS_FACE))
        root["chog_blush"] = list(GENESIS_BLUSH)
        root["chog_side"] = traits.get("Side", "Right")
        background = background_colour(traits.get("Background"))
        if background:
            root["chog_background"] = list(background)
        else:
            print(f"CHOG_TRAITS {label} no colour recorded for Background={traits.get('Background')}: grey is used")

    blend_path, fbx_path = base.export(root, parts + trait_parts, folder)
    print(f"CHOG_TRAITS saved {blend_path}")
    print(f"CHOG_TRAITS saved {fbx_path}")

    # What was built, for the sheets: the traits read, the parts and their triangles.
    built = {
        "label": label,
        "traits": traits or {},
        "replaced": replaced,
        "head_replaced": head_replaced,
        # What the pictures of its value of "Head" show, on its Side, and which were looked at
        # (HEAD_VALUES of chog_trait_hats.py, batch 4); empty when the value has no line there.
        "head_from_the_art": head_from_the_art(traits),
        "parts": {part.name: {"trait": part["trait"], "on": part.parent.name,
                              "triangles": counts["parts"][part.name],
                              "missing": part.get("missing", ""),
                              "leaves_through": part.get("leaves_through", ""),
                              "materials": [slot.material.name for slot in part.material_slots]}
                  for part in trait_parts},
        "triangles_base": counts["base"],
        "triangles_total": counts["total"],
        "left_out": left_out,
        # Traits that a hat covers (the mouth under a balaclava): not built, not missing.
        "hidden": [f"{trait}={traits[trait]}" for trait in head_look(traits, replaced).get("HIDES", ())
                   if traits and trait in traits],
        # What became of each line of SPIKES under the hat: share kept, turn, lean back (None: its own).
        "leaves": [list(item) for item in head_look(traits, replaced).get("LEAVES", FULL_CROWN)],
        # For a later choice (one mesh and one material per hero at import, or not): the mesh objects
        # (the four parts and the trait parts; the root is an empty besides) and the distinct materials.
        "mesh_objects": len(parts) + len(trait_parts),
        "materials": len({slot.material.name for part in parts + trait_parts for slot in part.material_slots}),
    }
    with open(os.path.join(folder, "parts.json"), "w", encoding="utf-8") as target:
        json.dump(built, target, indent=1)
    print(f"CHOG_TRAITS saved {os.path.join(folder, 'parts.json')}")
    return folder


if __name__ == "__main__":
    main()
