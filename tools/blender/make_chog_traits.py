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
import chog_trait_bases as bases  # noqa: E402  (the Skull base, batch 7, 15 October 2026)
import chog_one_of_one as one  # noqa: E402  (the ten 1:1 Genesis, 16 October 2026)
import chog_trait_accessories as accessories  # noqa: E402  (what a Genesis holds, 17 October 2026)

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
    # The face fills the front of the head, as the pictures draw it (22 October 2026): up to the edge of the
    # hood, the hat or the costume, and at the sides up to the skin of the crown. Its edge follows the egg of
    # the head: at each height it takes FACE_FIT of the width the head has there (face_place), so the cream
    # runs out to the side of the head and the dark skin ring around it is gone. FACE_TOP is how far the brow
    # stands above the middle of the face (FACE_HALF_HEIGHT, of H3, is how far the chin stands below it).
    # (Until then: a rounded square 0.250 from its middle, 0.305 at the brow, 0.178 high, which left 0.04 of
    # bare skin each side of the cheeks and 0.12 above the brow: the owner saw a delimited oval, smaller than
    # the face of the pictures. FACE_FIT 0.85 at the cheeks, 0.47 at the brow corner.)
    # (22 October 2026, the owner on the recruit on the pedestal: "the forehead is too big now", "the face is too high", "the
    # bottom does not reach the neck". Measured on the pictures of the bare Genesis (#76, #75, #112, #237, #432 at 1000 points):
    # from the eyes up to the middle of the hair line is about 0.9 of the distance from the eyes to the mouth, and from the
    # eyes down to the chin about twice that, so the cream face is tall below the eyes and short above them. FACE_TOP 0.235
    # to 0.125 above the middle of the face (the eyes stand 0.012 above it: the hair line 0.09 above them, the teeth of hair
    # 0.040 long, FRINGE_DROP 0.050 before), and the chin FACE_HALF_HEIGHT 0.178 to 0.196, which takes it to 0.246 under the
    # middle of the head, 0.004 from the lowest point of the egg: the cream now wraps under the chin, to the neck. FACE_FIT
    # 0.985 to 0.96: at the very edge the face turns away from the camera and the rim of the game's shader (a warm white
    # added at the edges that face the key) turns its cream white, a sliver along the outline that was a skin ring
    # before the face filled the head; 0.96 keeps most of the fill and leaves the grazing part to the skin.)
    # (Lot 6, 22 October 2026, the owner on #76 from the front on the stage: "on the sides and at the bottom the face still
    # does not take all the room". The pictures (#76 at 1000 points, #75, #112, #237, #432) fill almost the whole front of the
    # head with the cream: the skin shows only as the zigzag on top and a thin band at the sides, the cheeks sit near the
    # outline and the chin is the bottom of the head. The edge was a rounded square whose width was cut by the same squareness
    # as its height, which left a triangle of skin in each lower corner (from the front, a band of skin about a fifth of the
    # head wide beside the cheeks and under the face): the width has its own squareness now, 20, which makes the edge follow the
    # outline of the egg down its sides; FACE_FIT 0.96 to 0.99, the chin at 0.248 under the middle of the head, 0.002 from its
    # lowest point (0.246 before), and seven teeth of hair 0.080 wide (five of 0.088) so the fringe still covers the brow, which
    # is wider.)
    "FACE_FIT": 0.99,
    "FACE_SQUARENESS_X": 20.0,
    "FACE_SQUARENESS_X_TOP": 8.0,
    "FRINGE_TEETH": 7,
    "FACE_HALF_HEIGHT": 0.198,
    "FRINGE_DROP": 0.040,
    "FACE_TOP": 0.125,
    "FACE_SQUARENESS": 4.0,
    "CHEEK": (0.180, -0.062, 0.046, 0.032),
    "FRINGE_WIDTH": 0.080,
    # The face lies on the head (18 October 2026, see add_settled_face): it stands FACE_LIFT off the egg of
    # the head (0.012 until then), in five rings evenly spaced over the head and 24 points round its edge
    # evenly spaced along it, so that each triangle of the face keeps above the head.
    # FACE_EDGE: how far inside the face the dark line starts, its lift there (under the face),
    # the lift of its outer edge, then how far beyond that edge and how deep under the head the skirt that
    # closes it goes. FACE_OVER: how much higher than the face the cheeks and the nose lie, and the teeth of
    # hair. (A face 0.0030 off the egg let a point of the head show through it at the cheek: the quads by its
    # edge sag up to 0.0032 between their corners.)
    "FACE_LIFT": 0.0046,
    "FACE_TOP_LIFT": 0.002,
    # (22 October 2026, the specks inside the cheeks seen on the hero stage: FACE_OVER was (0.0008, 0.0038). The cheek is
    # a fan of 12 triangles, its flat triangles sag under the egg between their corners, and so do those of the face, a
    # little differently: measured on the 10,400 points of the two cheeks of #75 (check_face_overlaps.py --decals) the
    # cheek stood 0.00014 over the face at its lowest, so a pixel here and there showed the cream through the pink. Now
    # 0.0020 and 0.0052, and every layer drawn above them 0.0014 higher (settled(), LIFT of chog_trait_faces.py), which
    # keeps their order: the lowest point of the cheeks is 0.0014 over the face, of an eye 0.0027.)
    "FACE_OVER": (0.0020, 0.0052),
    "FACE_RING_SHARES": (0.2, 0.4, 0.6, 0.8, 1.0),
    "FACE_SEGMENTS": 24,
    "FACE_EDGE": (0.012, 0.0012, 0.0020, 0.005, 0.012),
}

# What is drawn on the face, in layers, off the egg of the head (eyes, mouths, tears, glints: DRAWN here,
# LIFT of chog_trait_faces.py, which come between them). Until 18 October 2026 they stood 0.0245 to
# 0.0397 off it: measured with a ray from each drawn point back to the head (#75, #1287, #23), an eye stood
# 0.028 to 0.035 off the head and 0.016 to 0.019 off the face, and from the side a grin, a brow or the
# teeth of hair stood out of the profile as sheets ("like a sheet hanging in the air", the owner). The
# rule now: the lowest layer just above the cheeks (FACE_LIFT + FACE_OVER = 0.0054), each next one 0.0011 to
# 0.0020 higher, in the order the old values had (settled(): each old value v becomes 0.0066 + (v - 0.0232)
# x 0.611), so nothing that lay over something else changes sides. 0.0011 is about ten times the step of
# the depth buffer of the game's camera at its distance (26 units, near plane 0.3, 24 bits).
DRAWN_BEFORE = (0.0245, 0.0277, 0.0307, 0.0337, 0.0367, 0.0397)


def settled(lift):
    """Where a drawn layer that stood "lift" off the head until 18 October 2026 lies now."""
    return 0.0080 + (lift - 0.0232) * (0.0011 / 0.0018)     # 0.0066 until 22 October 2026, see FACE_OVER


DRAWN = tuple(settled(lift) for lift in DRAWN_BEFORE)

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
    # The leaves of the head come through the band where they grow, as a crown sits on a spiky head: their
    # crossings are kept in parts.json, not refused (16 October 2026).
    "THROUGH": True,
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
HATS.update(bases.HATS)

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
    "SIZE": (0.050, 0.063),      # half width, half height (0.043 by 0.060 until 22 October 2026, strict audit of the eyes: on #75, #80, #177, #197 the eye is rounder and larger)
    "BROW_AT": 0.50,             # where the brow cuts the eye, against its half height
    "BROW_SLOPE": 0.42,
    "BROW": (0.011, 0.016, 0.004),   # thickness of the brow, how far it runs past the eye outwards and inwards
    "SEGMENTS": 14,
    # The glints: each to the side of the middle of the eye (the same side for both eyes: where the
    # light is), above it, half width, half height.
    "GLINTS": ((-0.014, 0.012, 0.017, 0.021), (-0.018, -0.026, 0.008, 0.009)),
}
LASER = {
    # A beam leaves each eye level with the ground, straight ahead: how long, how thick (half), how
    # far behind the face it starts.
    "BEAM_LENGTH": 0.34,         # (0.28 until 17 October 2026: the pictures draw the beams long, out of the frame)
    "BEAM_RADIUS": 0.019,
    "BEAM_SUNK": 0.03,
    # (First: six sides, two of them the light core. Seen from the front and from above, where those
    # two are what shows, the beam was a pale pink stick; on the pictures it is purple with a thin
    # light line along it.)
    "BEAM_SIDES": 8,
    # How far the beam is turned up from level, degrees (22 October 2026, only the lasers of Side=Left use it, see
    # build_laser of chog_trait_faces.py).
    "BEAM_PITCH_DEG": 0.0,
    "BEAM_CORE": (1,),           # which of its sides is the light core: up, towards the left
    "OUTLINE_THICKNESS": 0.010,
    # The eye a beam leaves, as the pictures of Side=Right draw it (CHOG #38, #41, #65, #212 fire; #107, #126,
    # #205, #1241 purple; #72, #329, #875, #1467 green; 17 October 2026): a whole tall dark oval, no brow
    # across it and no glint, the beam leaving its middle from a hot spot of the beam's colours. Until then
    # it was the eye of "Normal Eye", cut by a slanted brow: an angry look the pictures of lasers never draw.
    # Its middle (to the side, above), half width and half height, segments; then the hot spot: the radius of
    # the glow (the beam's colour) and of the core in it.
    # (22 October 2026, the owner on the dragon #16: "the eyes do not really match, frowned against ovals". The pictures of
    # Side=Right (#52, #102, #107, #41, #72, #16, #42) draw each eye as a dark almond, a little wider than high (0.14 by 0.13 on the pictures, the beam about a quarter of it), whose top is
    # cut by a flat lid that slopes down towards the nose: a frown; the beam leaves its middle. Until then a whole tall oval,
    # 0.090 by 0.128. EYE_LID: where the lid cuts the eye above its middle (against its half height) and how much it rises
    # per unit towards the outer side.)
    "EYE_AT": (0.112, 0.006),
    "EYE_SIZE": (0.070, 0.064),
    "EYE_LID": (0.28, 0.40),
    "EYE_SEGMENTS": 20,
    "GLOW": (0.024, 0.012),
}
HAPPY = {
    "AT": (0.110, 0.012),
    # (22 October 2026, strict audit of the eyes, on #163, #226, #19, #89, #67: the pictures draw the closed eyes as thick black arches, about
    # 1.7 times the thickness of ours, the eye at +X (the near one in the pictures of Side=Right) wider and thicker than the far one.)
    "ARC": (0.056, 0.036, 0.027),    # half width, height of the arch, thickness in its middle
    "STEPS": 8,
    # An arch of its own for each eye (22 October 2026, Side=Left, read on CHOG #925, #1430, #1207, #1405): {side: (half
    # width, height of the arch, thickness in its middle, how much higher its right end stands than its left)}, side -1 the
    # eye on the left of the picture (the near eye), +1 the other. None: both eyes the same, ARC.
    "EYE_ARCS": {1.0: (0.062, 0.036, 0.031, 0.0), -1.0: (0.050, 0.032, 0.022, 0.0)},
}
ROUND = {
    "AT": (0.112, 0.006),
    # (22 October 2026, strict audit: the eyes of the pictures (#202, #139, #55, #84, #134) are taller ovals, about 0.75 as wide as high against 0.89 for ours, the
    # white ring thin and the black disc large: 0.058 by 0.070, 0.047 by 0.060, 0.037 by 0.049 where they were 0.060 by 0.066, 0.049 by 0.055, 0.036 by 0.042.)
    "LINE": (0.058, 0.070),      # half width, half height of the dark disc around the eye
    "WHITE": (0.047, 0.060),
    "PUPIL": (0.037, 0.049),
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

def egg_normal(surface, point):
    """The way out of the egg of the head at a point on it (or near it)."""
    offset = point - surface.middle
    return Vector((offset.x / surface.half.x ** 2, offset.y / surface.half.y ** 2,
                   offset.z / surface.half.z ** 2)).normalized()


def along_egg(surface, start, away, distance, lift):
    """The point "distance" further over the egg of the head from "start" (a point on it), going the way
    "away" points (laid along the egg), then "lift" off it. Unlike HeadSurface.front, it stays on the head
    where the face reaches its sides and the bottom of the chin."""
    normal = egg_normal(surface, start)
    way = away - normal * away.dot(normal)
    if way.length < 1e-9:
        return start + normal * lift
    point = surface.towards((start + way.normalized() * distance - surface.middle).normalized())
    return point + egg_normal(surface, point) * lift


def centre_on_face(maker, surface):
    """Moves what a mouth drew across the face so that its middle lies on the middle line of the face, where the nose is (22 October
    2026, the owner: "some mouths are offset, I do not know why"). The pictures of Side=Right and Side=Left show the face turned, and
    the mouths were placed where the pictures draw them against the nose: a smile 0.040 to 0.052 off the middle, a flat mouth
    0.050, which from the front reads as a mouth pushed to one side. Each vertex of each part is moved across by minus the
    middle of the part (the middle of its extent) and put back on the surface of the head, at the same height off it."""
    for shapes, on in maker.parts:
        if on != "Head" or not shapes.mesh.verts:
            continue
        xs = [vertex.co.x for vertex in shapes.mesh.verts]
        shift = -0.5 * (min(xs) + max(xs))
        if abs(shift) < 0.002:
            continue
        for vertex in shapes.mesh.verts:
            offset = vertex.co - surface.middle
            lift = offset.length - (surface.towards(offset.normalized()) - surface.middle).length
            moved = vertex.co + X * shift - surface.middle
            ground = surface.towards(moved.normalized())
            vertex.co = ground + (ground - surface.middle).normalized() * lift


def face_place(numbers, angle, amount):
    """A place of the face (to the side, above its middle): on its edge at "angle" when "amount" is 1. The
    edge is a rounded square whose width at each height is FACE_FIT of the width of the egg of the head at
    that height (22 October 2026: it was a fixed oval, so a head narrower at the brow and at the chin left
    a ring of skin round the face), FACE_TOP above the middle of the face and FACE_HALF_HEIGHT below."""
    cosine, sine = math.cos(angle), math.sin(angle)
    z = (numbers.FACE_TOP if sine >= 0.0 else numbers.FACE_HALF_HEIGHT) * power(sine, numbers.FACE_SQUARENESS)
    height = numbers.FACE_MIDDLE[1] + z * amount
    room = numbers.HEAD_HALF_WIDTH * math.sqrt(max(1.0 - (height / numbers.HEAD_HALF_HEIGHT) ** 2, 0.0))
    # (22 October 2026: the width has its own squareness, FACE_SQUARENESS_X, so that the edge of the face follows the outline of
    # the egg all down its sides, instead of cutting the lower corners off with the rounded square.)
    squareness = getattr(numbers, "FACE_SQUARENESS_X", numbers.FACE_SQUARENESS)
    if sine > 0.0:
        # Over the brow the width is rounded off more (FACE_SQUARENESS_X_TOP): with 20 the edge went straight up the side and
        # crossed the whole brow in the last degree of its angle, so that no point of the edge lay along the brow (lot 7).
        squareness = getattr(numbers, "FACE_SQUARENESS_X_TOP", squareness)
    return numbers.FACE_FIT * room * power(cosine, squareness) * amount, z * amount


def opening_rim(numbers, size, margin, tail, step=3.0):
    """The edge of a hood or a helmet round the face, as a table (degrees from the front, polar degrees: the
    "RIM" of chog_trait_hats.py), made to follow the edge of the face (22 October 2026). Until then the tables
    were typed in for a smaller face, and a bigger one went under the hood or left a band of skin round it.
    "size(degrees from the front)" is how much larger than the head the shell is there. For each degree from
    the front, the rim is the first polar angle, from the top down, at which the shell leaves the sky and
    enters the face seen from the front, its edge pushed out by "margin" (the dark line and the roll of the
    hood). Past the last degree at which the shell meets the face (its side), "tail" (the same kind of table)
    takes over: the hood closes down to the jaw and the back."""
    key = (tuple(numbers.FACE_MIDDLE), numbers.FACE_FIT, numbers.FACE_TOP, numbers.FACE_HALF_HEIGHT,
           numbers.FACE_SQUARENESS, getattr(numbers, "FACE_SQUARENESS_X", 0.0),
           getattr(numbers, "FACE_SQUARENESS_X_TOP", 0.0), numbers.HEAD_HALF_WIDTH, numbers.HEAD_HALF_HEIGHT,
           margin, step, tuple(tail),
           tuple(size(degrees) for degrees in range(0, 181, 10)))
    if key in OPENING_RIMS:
        return OPENING_RIMS[key]
    middle = (0.0, numbers.FACE_MIDDLE[1])
    outline = []
    for index in range(120):
        x, up = face_place(numbers, 2.0 * math.pi * index / 120, 1.0)
        point = (x, numbers.FACE_MIDDLE[1] + up)
        away = math.hypot(point[0] - middle[0], point[1] - middle[1])
        grown = 1.0 + margin / max(away, 1e-6)
        outline.append((middle[0] + (point[0] - middle[0]) * grown, middle[1] + (point[1] - middle[1]) * grown))

    def inside(x, z):
        crossings = False
        for (x1, z1), (x2, z2) in zip(outline, outline[1:] + outline[:1]):
            if (z1 > z) != (z2 > z) and x < x1 + (z - z1) * (x2 - x1) / (z2 - z1):
                crossings = not crossings
        return crossings

    table, degrees = [], 0.0
    while degrees <= 180.0:
        shell = size(degrees)
        sideways = numbers.HEAD_HALF_WIDTH * shell * math.sin(math.radians(degrees))
        found = None
        for half_degrees in range(0, 360):
            polar = math.radians(half_degrees * 0.5)
            if inside(sideways * math.sin(polar), numbers.HEAD_HALF_HEIGHT * shell * math.cos(polar)):
                found = half_degrees * 0.5
                break
        if found is None:
            break
        table.append((degrees, found))
        degrees += step
    if not table:
        raise SystemExit("CHOG_TRAITS opening_rim: the shell never meets the face")
    # Where the shell no longer meets the face its side edge drops, almost upright, from there down to the
    # first line of the tail (about 7 degrees round the head).
    last, last_polar = table[-1]
    drop = tail[0][1]
    table += [(last + 3.0, last_polar + (drop - last_polar) * 0.5), (last + 7.0, drop)]
    OPENING_RIMS[key] = tuple(table) + tuple(item for item in tail if item[0] > last + 8.0)
    return OPENING_RIMS[key]


OPENING_RIMS = {}


def face_edge_angles(numbers, surface, segments):
    """The angles (of face_place) of "segments" points round the edge of the face, evenly spaced along the
    edge as it lies on the head: the upper half and the lower half each get an even number, so the middle of
    the brow and of the chin are points and both sides mirror each other. Spaced by angle, the points of the
    rounded square bunch at its corners and leave chords 0.15 long over the brow, which sank 0.006 into the
    head between their ends."""
    def half_lengths(first, last, steps=200):
        angles = [first + (last - first) * index / steps for index in range(steps + 1)]
        points = [surface.front(*face_place(numbers, angle, 1.0), 0.0) for angle in angles]
        lengths = [0.0]
        for before, after in zip(points, points[1:]):
            lengths.append(lengths[-1] + (after - before).length)
        return angles, lengths

    halves = [half_lengths(0.0, math.pi), half_lengths(math.pi, 2.0 * math.pi)]
    total = halves[0][1][-1] + halves[1][1][-1]
    upper = max(2, 2 * round(segments * halves[0][1][-1] / total / 2.0))
    counts = (upper, segments - upper)
    chosen = []
    for (angles, lengths), count in zip(halves, counts):
        for index in range(count):
            wanted = lengths[-1] * index / count
            step = next(step for step in range(len(lengths) - 1) if lengths[step + 1] >= wanted)
            share = (wanted - lengths[step]) / max(lengths[step + 1] - lengths[step], 1e-12)
            chosen.append(angles[step] + (angles[step + 1] - angles[step]) * share)
    return chosen


def add_settled_face(part, numbers, surface):
    """The cream face of the base, its dark line, the cheeks, the nose and the teeth of hair on the brow, laid
    ON the head (18 October 2026). add_face of make_chog_full.py laid them 0.006 to 0.022 off the egg of the
    head, which is itself 0 to 0.007 outside the faceted head between its points: seen turned (the hero
    pages, the pick), the face stood off the head like a sheet with a dark line under its edge, and the teeth
    of hair on the brow stood in the air. Now: the face FACE_LIFT off the egg, in triangles small enough
    that none sags into the head (its points evenly spaced along its edge, face_edge_angles, and its rings
    evenly spaced over the head from its middle to that edge, FACE_RING_SHARES); the dark line from under
    the face out to its width, then a skirt down into the head, so no gap shows at the edge from any side;
    each tooth of hair the same, its skirt in the skin colour. Where the face reaches the bottom of the chin,
    the line and the skirt are laid along the egg (along_egg), never in the air."""
    lift, line = numbers.FACE_LIFT, numbers.FACE_LINE
    inset, under, outer, beyond, depth = numbers.FACE_EDGE
    segments, shares = numbers.FACE_SEGMENTS, numbers.FACE_RING_SHARES
    middle_of_face = surface.front(0.0, 0.0, 0.0)
    towards_middle = (middle_of_face - surface.middle).normalized()
    edges = [surface.front(*face_place(numbers, angle, 1.0), 0.0)
             for angle in face_edge_angles(numbers, surface, segments)]

    def over_the_head(edge, share, height):
        """The point "share" of the way over the head from the middle of the face to a point of its edge
        (by the angle seen from the middle of the head), "height" off the egg."""
        towards_edge = (edge - surface.middle).normalized()
        point = surface.towards((towards_middle * (1.0 - share) + towards_edge * share).normalized())
        return point + egg_normal(surface, point) * height

    # The face: its middle, then rings out to its edge. Along the brow the edge points of the face are far apart (the top of the
    # edge is flat and wide) and a flat triangle between two of them sags under the dome of the head by more than FACE_LIFT,
    # so the head came through the face there, in long jagged bands across the forehead (22 October 2026, the owner on the recruit,
    # and measure_face_gaps: 41 points on #76 since the face was widened). The face is therefore lifted more, up to FACE_TOP_LIFT,
    # along the brow, growing with the square of the ring (the edge ring most): the fringe that hides the brow is lifted with it.
    top_lift = numbers.FACE_TOP_LIFT
    heights = []
    for angle in face_edge_angles(numbers, surface, segments):
        up = face_place(numbers, angle, 1.0)[1]
        reach = min(1.0, max(0.0, (up - (numbers.FACE_TOP - 0.10)) / 0.08))
        heights.append(reach * reach * (3.0 - 2.0 * reach))

    def brow_lift(index, share):
        return lift + top_lift * heights[index] * share * share

    loops = [[surface.front(0.0, 0.0, lift)]]
    for share in shares:
        loops.append([over_the_head(edge, share, brow_lift(index, share)) for index, edge in enumerate(edges)])
    full.add_skin(part, surface, loops, full.FACE)

    # The dark line: from under the edge of the face out over the head, then into it.
    rows = [[], [], []]
    for edge in edges:
        away = edge - middle_of_face
        reach = (edge - middle_of_face).length
        rows[0].append(over_the_head(edge, 1.0 - inset / reach, under))
        rows[1].append(along_egg(surface, edge, away, line, outer))
        rows[2].append(along_egg(surface, edge, away, line + beyond, -depth))
    full.add_skin(part, surface, rows, OUTLINE)

    drawn = lift + numbers.FACE_OVER[0]
    cheek_side, cheek_up, cheek_width, cheek_height = numbers.CHEEK
    for side in (1.0, -1.0):
        full.add_skin(part, surface, full.patch_loops(surface, (side * cheek_side, cheek_up), cheek_width, cheek_height,
                                                      2.0, 0.0, drawn, 1, 12), full.BLUSH)
    nose_side, nose_up, nose_width, nose_height = numbers.NOSE
    full.add_skin(part, surface, full.patch_loops(surface, (nose_side, nose_up), nose_width, nose_height, 2.0, 0.6,
                                                  drawn, 1, 8), full.BLUSH)

    # The teeth of hair on the brow, in the skin colour: from a point on the face up over the dark line, then
    # into the head. A tooth 0.085 long sags 0.004 to 0.006 between its tip and its corners over the corners
    # of the brow, so it has a row halfway and lies higher than the eyes (FACE_OVER): the face never shows
    # through it (0.0015 over the face, in one triangle, it did).
    teeth = numbers.FRINGE_TEETH
    if teeth:
        brow = numbers.FACE_TOP * 0.995
        hair = lift + numbers.FACE_OVER[1] + top_lift
        tip = brow - numbers.FRINGE_DROP
        for index in range(teeth):
            middle = (index - (teeth - 1) * 0.5) * numbers.FRINGE_WIDTH
            half = numbers.FRINGE_WIDTH * 0.5
            corners = [surface.front(middle + offset, brow, 0.0) for offset in (-half, half)]
            ups = [corner - middle_of_face for corner in corners]
            full.add_skin(part, surface, [
                [surface.front(middle, tip, hair)],
                [surface.front(middle + offset * 0.55, tip + (brow - tip) * 0.55, hair) for offset in (-half, half)],
                [along_egg(surface, corner, up, line, hair) for corner, up in zip(corners, ups)],
                [along_egg(surface, corner, up, line + beyond, -depth) for corner, up in zip(corners, ups)],
            ], SKIN, around=False)


def add_leaf_crown(part, numbers, surface, leaves=FULL_CROWN, sides=None):
    """The crown of add_crown of make_chog_full.py, blade for blade, each one a leaf, as "leaves" (one
    item per line of SPIKES, see leaf()) leaves it. Returns each leaf built, for leaf_crossings(): its
    name and its rings (middle, half width, half thickness), from inside the head to its point. "sides": points
    around a leaf in place of LEAF_SIDES (the short leaves under the glass of the astronaut, 22 October 2026: four
    keep the hero under the triangle limit)."""
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
            part.shell(rings, sides or numbers.LEAF_SIDES, SKIN, numbers.OUTLINE_THICKNESS)
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
    # Inside an open hat (the crown): (degrees from the front, polar degrees) of leaves rooted on the head
    # within it, on both sides but 0 and 180; their length against the leaf of the row.
    "INSIDE": (),
    "INSIDE_LENGTH": 1.0,
    # The sectors where the check of spike_sectors_seen wants a tip seen (SPIKE_SECTORS below when not
    # given): fewer only where the hat covers the sides of the head down to the jaw.
    "SECTORS": None,
    # Rows of smaller leaves planted under the first (16 October 2026: the owner saw one single row of
    # spikes under a cap from the front and from the side, "one turned down is missing"). Each: the angles
    # of its leaves (between those of the first row), how much lower than the first it is rooted and how
    # much further down it leans (degrees), its length and width against a leaf of the first row, how far
    # its point bends up, and the points around a leaf (fewer than the first row: the triangles of a
    # dressed hero). A leaf of these rows that finds no place under the hat is left out and said.
    "EXTRA_ROWS": (
        {"ANGLES": (91.0, 117.0, 143.0, 168.0), "BELOW_DEG": 20.0, "DROOP_DEG": 30.0, "LENGTH": 0.80,
         "WIDTH": 0.85, "LIFT": 0.12, "SIDES": 4},
    ),
}
# The leaves of the row are smaller than those of the crown: a lighter section from root to point (four
# rings in place of six of LEAF_PROFILE), so that 9 to 12 of them keep a dressed hero under TRIANGLE_LIMIT
# (#44, crown and duck float, went over with the profile of the crown, 15 October 2026).
RIM_PROFILE = (
    (0.00, 0.90, 0.92),
    (0.20, 1.00, 0.96),
    (0.52, 0.72, 0.64),
    (0.84, 0.26, 0.24),
)
RIM_REACH = 0.30                 # a ray out of the head that meets the hat within this is covered (0.10
                                 # until batch 6: the roomy crown of a bucket hat stands further off the head)
RIM_SCAN = (6.0, 160.0, 1.0)     # polar degrees scanned from the top down, and the step
# How a leaf of the row that would cross its hat is moved, tried in this order (add_rim_leaves): its
# length against the leaf of the row, degrees turned from its angle, degrees rooted lower, degrees leaning
# further down. None is dropped: a leaf that finds no place stops the build.
RIM_TRIES = {
    "LENGTH": (1.0, 0.85, 0.70),
    "TURN_DEG": (0.0, 8.0, -8.0, 16.0, -16.0),
    "BELOW_DEG": (0.0, 6.0, 12.0, 18.0, 24.0),
    "DROOP_DEG": (0.0, 12.0, 24.0, 36.0, 48.0, 60.0),
}


def hat_outside(hat_shapes, surface):
    """The faces of a hat that lie outside the head, as a tree, and their edges: what leaf_crossings and
    the row under the rim test against (the rows of a hat tucked inside the head are not seen). (None,
    []) when the hat has none."""
    from mathutils.bvhtree import BVHTree
    points, polygons, edges = [], [], []
    for shapes in hat_shapes:
        for face in shapes.mesh.faces:
            centre = face.calc_center_median() - surface.middle
            if sum((centre[index] / surface.half[index]) ** 2 for index in range(3)) <= 1.02 ** 2:
                continue
            corners = [point.co.copy() for point in face.verts]
            polygons.append([len(points) + index for index in range(len(corners))])
            points.extend(corners)
            edges.extend((corners[index], corners[(index + 1) % len(corners)]) for index in range(len(corners)))
    return (BVHTree.FromPolygons(points, polygons) if polygons else None), edges


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
    for leaf_crossings(). A leaf that would cross the hat is moved until it does not (RIM_TRIES); one that
    finds no place stops the build: no leaf is left out without a word (15 October 2026, after batch 5,
    where the brim of the bucket hats and the tails of the durag silently took 5 to 7 leaves of 9)."""
    under = {**UNDER_RIM, **under}
    outside = hat_outside(hat_shapes, surface)
    tree = outside[0]
    if tree is None:
        return []
    _, _, length, width, thickness, _ = numbers.SPIKES[-2]      # the pair of the back row of H3
    length *= under["LENGTH"] * numbers.LEAF_LENGTH
    width *= under["WIDTH"] * numbers.LEAF_WIDTH
    thickness *= under["THICK"] * numbers.LEAF_WIDTH
    ahead, down = level_axes(numbers)
    back, up = -ahead, -down
    profile = RIM_PROFILE

    def rings_of(angle, rim, below, droop, length, width=width, thickness=thickness, lift=None):
        """The rings of one leaf rooted "below" degrees under the rim at that angle, leaning down by
        "droop" degrees from straight out of the head."""
        lift = under["LIFT"] if lift is None else lift
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
        curl = rise.normalized() * length * lift if rise.length > 1e-6 else Vector()
        rings = [(root - away * numbers.SPIKE_SUNK, wide * width * 0.5 * profile[0][1],
                  thick * thickness * 0.5 * profile[0][2])]
        for along, wider, thicker in profile[1:]:
            rings.append((root + away * length * along + curl * along * along, wide * width * 0.5 * wider,
                          thick * thickness * 0.5 * thicker))
        rings.append((root + away * length + curl, NOWHERE, NOWHERE))
        return rings

    def place(degrees, side, name, row=None):
        """The first way of placing the leaf that does not cross the hat (as leaf_crossings judges it,
        outline included), in the order of RIM_TRIES: its whole length first, at its own angle, then
        rooted lower and leaning down; then a little to either side of its angle (past a tail, a
        flap); then shorter. A leaf of an extra row ("row") is rooted lower and leans further, smaller.
        Returns (rings, what was changed) or None."""
        row = row or {}
        lower, further = row.get("BELOW_DEG", 0.0), row.get("DROOP_DEG", 0.0)
        own = (width * row.get("WIDTH", 1.0), thickness * row.get("WIDTH", 1.0), row.get("LIFT"))
        for share in RIM_TRIES["LENGTH"]:
            for turn in RIM_TRIES["TURN_DEG"]:
                angle = math.radians(270.0 + side * (degrees + turn))
                rim = rim_polar(tree, surface, angle)
                if rim is None:
                    continue
                for below in RIM_TRIES["BELOW_DEG"]:
                    for droop in RIM_TRIES["DROOP_DEG"]:
                        rings = rings_of(angle, rim, under["BELOW_DEG"] + lower + below,
                                         under["DROOP_DEG"] + further + droop,
                                         length * row.get("LENGTH", 1.0) * share, *own)
                        if not leaf_crossings([(name, rings)], hat_shapes, numbers, surface, outside):
                            moved = [f"{label} {value:+.0f}" for label, value in
                                     (("turn", turn), ("lower", below), ("down", droop)) if value]
                            if share < 1.0:
                                moved.append(f"length {share:.2f}")
                            return rings, f"rim {rim:.0f}" + (", " + ", ".join(moved) if moved else "")
        return None

    built, said = [], []
    for degrees in under["ANGLES"]:
        for side in ((1.0,) if degrees in (0.0, 180.0) else (1.0, -1.0)):
            where = "" if degrees in (0.0, 180.0) else (" left" if side > 0.0 else " right")
            name = f"rim leaf {degrees:.0f}{where}"
            found = place(degrees, side, name)
            if found is None:
                rims = [rim_polar(tree, surface, math.radians(270.0 + side * (degrees + turn)))
                        for turn in RIM_TRIES["TURN_DEG"]]
                raise SystemExit(f"CHOG_TRAITS FAILED: {name} finds no place under the rim of the hat that does "
                                 f"not cross it (RIM_TRIES); rim found at polar degrees "
                                 + ", ".join("none" if rim is None else f"{rim:.0f}" for rim in rims))
            rings, how = found
            part.shell(rings, numbers.LEAF_SIDES, SKIN, numbers.OUTLINE_THICKNESS, smooth=True)
            built.append((name, rings))
            said.append(f"{degrees:.0f}{where[:2]} ({how})")

    for number, row in enumerate(under["EXTRA_ROWS"], start=2):
        for degrees in row["ANGLES"]:
            for side in ((1.0,) if degrees in (0.0, 180.0) else (1.0, -1.0)):
                where = "" if degrees in (0.0, 180.0) else (" left" if side > 0.0 else " right")
                name = f"rim leaf {degrees:.0f}{where}, row {number}"
                found = place(degrees, side, name, row)
                if found is None:
                    said.append(f"row {number} {degrees:.0f}{where[:2]} left out (no place)")
                    continue
                rings, how = found
                part.shell(rings, row.get("SIDES", numbers.LEAF_SIDES), SKIN, numbers.OUTLINE_THICKNESS, smooth=True)
                built.append((name, rings))
                said.append(f"row {number} {degrees:.0f}{where[:2]} ({how})")

    # Inside an open hat (the crown): leaves rooted on the head within it, straight out of the head and
    # leaning back, shortened or tilted until they cross nothing.
    for degrees, polar_deg in under["INSIDE"]:
        for side in ((1.0,) if degrees in (0.0, 180.0) else (1.0, -1.0)):
            where = "" if degrees in (0.0, 180.0) else (" left" if side > 0.0 else " right")
            name = f"inside leaf {degrees:.0f}{where}"
            angle = math.radians(270.0 + side * degrees)
            found = None
            for share in RIM_TRIES["LENGTH"] + (0.55,):
                for droop in (0.0, -12.0, 12.0, 24.0):
                    rings = rings_of(angle, polar_deg, 0.0, droop, length * under["INSIDE_LENGTH"] * share)
                    if not leaf_crossings([(name, rings)], hat_shapes, numbers, surface, outside):
                        how = ([f"length {share:.2f}"] if share < 1.0 else []) + ([f"tilt {droop:+.0f}"] if droop else [])
                        found = rings, ", ".join(how)
                        break
                if found:
                    break
            if found is None:
                raise SystemExit(f"CHOG_TRAITS FAILED: {name} finds no place inside the hat that does not "
                                 f"cross it")
            part.shell(found[0], numbers.LEAF_SIDES, SKIN, numbers.OUTLINE_THICKNESS, smooth=True)
            built.append((name, found[0]))
            said.append(f"inside {degrees:.0f}{where[:2]}" + (f" ({found[1]})" if found[1] else ""))
    print(f"CHOG_TRAITS rim row: {len(built)} leaves: " + "; ".join(said))
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


# The check of "same head, same result" (15 October 2026, after batch 5). The owner found the spikes
# all around some hats from the camera of the game and none around others, on the same head. Every
# Genesis whose head keeps leaves must show, from each view of SPIKE_VIEWS (turn around the character
# and degrees above the horizon, as render_chog_body.place_camera takes them), the tip of at least one
# leaf in each sector of SPIKE_SECTORS (degrees from the front, + its left, - its right, 45 degrees
# wide): a tip hidden by the hat or by the head does not count. A Genesis that fails stops its build.
SPIKE_VIEWS = {"from above": (180.0, 89.0), "game, behind": (160.0, 73.0)}
SPIKE_SECTORS = (90, -90, 135, -135, 180)


def spike_sectors_seen(leaves, hat_shapes, numbers, surface):
    """For each view of SPIKE_VIEWS, the sectors where the tip of a leaf is seen (see above). Leaves and
    hat in the axes of "Head" before the head is turned up."""
    tree = hat_outside(hat_shapes, surface)[0] if hat_shapes else None
    pitch = Matrix.Rotation(math.radians(numbers.HEAD_PITCH_DEG), 3, "X")
    middle, half = surface.middle, surface.half
    seen = {}
    for view, (turn_deg, elevation_deg) in SPIKE_VIEWS.items():
        turn, elevation = math.radians(turn_deg), math.radians(elevation_deg)
        world = Vector((math.sin(turn) * math.cos(elevation), -math.cos(turn) * math.cos(elevation),
                        math.sin(elevation)))
        towards = (pitch @ world).normalized()
        sectors = set()
        for _, rings in leaves:
            for point in (rings[-1][0], rings[-2][0]):      # the tip, and just under it
                start = point + towards * 0.003
                if tree is not None and tree.ray_cast(start, towards, 5.0)[0] is not None:
                    continue
                # The head, an egg: does the line towards the camera enter it?
                where = Vector(((start - middle)[index] / half[index] for index in range(3)))
                way = Vector((towards[index] / half[index] for index in range(3)))
                a, b, c = way.dot(way), 2.0 * where.dot(way), where.dot(where) - 1.0
                inside = b * b - 4.0 * a * c
                if inside >= 0.0 and (-b + math.sqrt(inside)) / (2.0 * a) > 0.0 and c > 0.0 and \
                        (-b - math.sqrt(inside)) / (2.0 * a) > 0.0:
                    continue
                away = point - middle
                degrees = math.degrees(math.atan2(away.x, -away.y))
                sector = int(round(degrees / 45.0)) * 45
                sectors.add(180 if sector == -180 else sector)
                break
        seen[view] = sectors
    return seen


def spikes_missing(seen, required=SPIKE_SECTORS):
    """What spike_sectors_seen lacks against the sectors required (SPIKE_SECTORS, or the "SECTORS" of
    the row of a hat whose flaps or cheek guards cover the sides of the head), in words, or ""."""
    lacking = []
    for view, sectors in seen.items():
        missing = [sector for sector in required if sector not in sectors]
        if missing:
            lacking.append(f"{view}: no tip seen at {', '.join(f'{sector:+d}' for sector in missing)}")
    return "; ".join(lacking)


def leaf_crossings(leaves, hat_shapes, numbers, surface, outside=None):
    """The leaves (from add_leaf_crown) that go through a hat, or that a thin part of the hat (a band, a
    brim) goes through: an edge of a leaf, made thicker by its outline, crosses a face of the hat, or an
    edge of the hat crosses a face of that leaf. Only the faces of the hat that lie outside the head
    count (hat_outside(), which "outside" gives when it is already made). Both are in the axes of "Head"
    before the head is turned up. Returns the names of those leaves."""
    from mathutils.bvhtree import BVHTree

    def cut(tree, segments):
        for start, stop in segments:
            way = stop - start
            if way.length > 1e-9 and tree.ray_cast(start, way.normalized(), way.length)[0] is not None:
                return True
        return False

    hat_tree, hat_edges = outside or hat_outside(hat_shapes, surface)
    if hat_tree is None:
        return []
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


def trait_materials(prefix, colours, unlit, shared, glass=()):
    """The materials of a trait, in the colours of its colour set. "srgb" keeps the colour as it was
    given, for the renders; "unlit" says it is drawn in that colour whatever the light; "glass" (22 October 2026,
    the helmet of the astronaut) says it is drawn on a share of the pixels only, so what is inside shows through
    (export_chog_parts.py gives it the light 3; render_chog_traits.py draws it semi-transparent)."""
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
        material["glass"] = key in glass
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


def centroid(edge):
    """The middle of the area of a shape given by the places of its edge (to the side, above), in order."""
    area = cx = cy = 0.0
    for (x1, y1), (x2, y2) in zip(edge, edge[1:] + edge[:1]):
        cross = x1 * y2 - x2 * y1
        area += cross
        cx += (x1 + x2) * cross
        cy += (y1 + y2) * cross
    if abs(area) < 1e-12:
        return (sum(p[0] for p in edge) / len(edge), sum(p[1] for p in edge) / len(edge))
    return (cx / (3.0 * area), cy / (3.0 * area))


def draw_laser_eyes(part, surface, numbers, dark, glow, core):
    """The eyes a beam leaves (LASER): a whole tall dark oval each, a hot spot of the beam's colours in its
    middle. Returns where the middle of each is: (to the side, above)."""
    half_width, half_height = numbers.EYE_SIZE
    glow_radius, core_radius = numbers.GLOW
    middles = []
    for side in (1.0, -1.0):
        middle = (side * numbers.EYE_AT[0], numbers.EYE_AT[1])
        cut, slope = numbers.EYE_LID

        def lid(place, middle=middle, side=side):
            return middle[1] + half_height * cut + slope * (place - middle[0]) * side

        edge = [(place, min(up, lid(place))) for place, up in oval(middle, half_width, half_height, numbers.EYE_SEGMENTS)]
        inside = (middle[0], (middle[1] - half_height + lid(middle[0])) * 0.5)
        draw_shape(part, surface, dark, edge, DRAWN[0], middle=inside)
        # The beam and its hot spot leave the middle of what is seen of the eye, not the middle of the oval the lid cuts: the lid
        # takes the top off, so the middle of the dark almond lies lower than that of the oval (22 October 2026, the owner: the
        # beams "sit at the inner corner of each eye, not at its centre").
        seen = centroid(edge)
        draw_shape(part, surface, glow, oval(seen, glow_radius, glow_radius, 10), DRAWN[1], middle=seen)
        draw_shape(part, surface, core, oval(seen, core_radius, core_radius, 8), DRAWN[2], middle=seen)
        middles.append(seen)
    return middles


def build_laser(maker):
    """The eyes of a laser as the pictures of Side=Right draw them (draw_laser_eyes), and a short beam
    leaving each, level with the ground and straight ahead. The game draws the long beam itself when the
    hero attacks."""
    numbers = maker.numbers(LASER)
    part, surface = maker.part(), maker.surface
    ahead, down = level_axes(maker.head)
    radius, sides = numbers.BEAM_RADIUS, numbers.BEAM_SIDES
    for middle in draw_laser_eyes(part, surface, numbers, part.named("Dark"), part.named("Beam"), part.named("Core")):
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
    steps = numbers.STEPS
    for side in (1.0, -1.0):
        half_width, arch, thick = numbers.ARC
        rise = 0.0
        if numbers.EYE_ARCS:
            half_width, arch, thick, rise = numbers.EYE_ARCS[side]
        places, widths = [], []
        for index in range(steps + 1):
            along = -1.0 + 2.0 * index / steps
            hollow = 1.0 - along * along
            places.append((side * numbers.AT[0] + along * half_width, numbers.AT[1] + arch * (hollow - 0.5) + rise * along * 0.5))
            widths.append(thick * (0.45 + 0.55 * hollow ** 0.7))
        draw_line(part, surface, part.named("Dark"), places, widths, DRAWN[0])


def build_round(maker):
    """Two round eyes: a white disc in a dark line, a large dark pupil in its middle (PUPIL None: no pupil)."""
    numbers = maker.numbers(ROUND)
    part, surface = maker.part(), maker.surface
    layers = ((numbers.LINE, "Dark", DRAWN[0]), (numbers.WHITE, "White", DRAWN[1]), (numbers.PUPIL, "Dark", DRAWN[2]))
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        for size, material, lift in layers:
            if size is None:      # no pupil: the radioactive "Round Eye" is a white disc in a ring
                continue
            half_width, half_height = size
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
              **hats_and_clothes.HAT_SHAPES, **bases.HAT_SHAPES}
# The numbers of each hat shape: where its "LEAVES" are.
HAT_NUMBERS = {"Cap": CAP, "BucketCap": BUCKET, "Crown": CROWN, "Beanie": BEANIE, **hats_and_clothes.HAT_NUMBERS,
               **bases.HAT_NUMBERS}


def hat_numbers_of(entry, replaced=None):
    """The numbers of the hat of an entry of HATS (those of its shape, those it replaces), or {}."""
    if not isinstance(entry, dict) or entry.get("shape") not in HAT_NUMBERS:
        return {}
    return {**HAT_NUMBERS[entry["shape"]], **entry.get("numbers", {}), **(replaced or {})}


def face_numbers(head, hat_numbers):
    """The numbers of the head for its face: without the teeth of hair on the brow under a hat whose
    numbers say "FRINGE": False."""
    if hat_numbers.get("FRINGE", True):
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
    "Eyes": ("Eyes", {**EYES, **faces.EYES, **bases.radioactive_eyes({**EYES, **faces.EYES})}, "kind", EYE_KINDS),
    "Mouth": ("Mouth", {**MOUTHS, **faces.MOUTHS}, "kind", MOUTH_KINDS),
    # What a Genesis holds in its right hand (chog_trait_accessories.py, 17 October 2026).
    "Accessory": ("Accessory", accessories.ACCESSORIES, "kind", accessories.ACCESSORY_KINDS),
}
# Hats and clothing that a Side draws otherwise (Mon Cap, Russian Hat, Tuxedo): chog_trait_hats.py.
OF_SIDE.update({"Head": hats_and_clothes.HATS_OF_SIDE, "Body": hats_and_clothes.CLOTHES_OF_SIDE})
# Batch 4: every value of "Head" (on each Side) gets the leaves and the numbers its pictures ask for
# (HEAD_VALUES of chog_trait_hats.py), which replace those of its shape.
OF_SIDE["Head"] = hats_and_clothes.from_the_art(HATS, OF_SIDE["Head"])
# The ten 1:1 Genesis (chog_one_of_one.py): their eyes, mouths, hats, clothing and skins.
TRAIT_PARTS["Eyes"][1].update(one.EYES)
TRAIT_PARTS["Mouth"][1].update(one.mouths(TRAIT_PARTS["Mouth"][1]))
_ONE_HATS, _ONE_HAT_NUMBERS = one.hats(hats_and_clothes.HEADBAND)
HATS.update(_ONE_HATS)
HAT_SHAPES.update(one.HAT_SHAPES)
HAT_NUMBERS.update(_ONE_HAT_NUMBERS)
CLOTHES.update(one.clothes(hats_and_clothes.tee, hats_and_clothes.costume))
CLOTH_KINDS.update(one.CLOTH_KINDS)
SKINS.update(one.SKINS)
bases.SKULL_SKINS.update(one.SKULL_SKINS)


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
                found = {str(item["trait_type"]): str(item["value"])
                         for item in json.loads(record["raw_json"]).get("attributes", [])}
                # A 1:1 Genesis names no trait: it is given its look (chog_one_of_one.py).
                return one.look_of(number, found) if found.get("Base") == "1:1" else found
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
               rim, hat_numbers.get("LEAF_SIDES"))
        if key not in heads:
            head = variants.PartShapes(full.MATERIAL_ORDER)
            full.add_head(head, head_n)
            add_leaf_crown(head, head_n, surface, key[0], key[4])
            rim_leaves_for(head, traits, None, head_n, body_n, surface)
            if key[1]:
                hats_and_clothes.add_hair_points(head, surface, key[1], SKIN)
            add_settled_face(head, face_numbers(head_n, hat_numbers), surface)
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
            if trait == "Mouth":
                centre_on_face(maker, surface)
            made = trait_materials(maker.prefix, entry["colours"], entry.get("unlit", ()), shared, entry.get("glass", ()))
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
    if bases.is_skull(traits) or bases.is_radioactive(traits):
        # The skeleton under the skull, or seen through a radioactive body: ribs and breastbone on the
        # torso (clothing covers them).
        bases.add_ribs(body, body_n, full.EYE)

    head = variants.PartShapes(full.MATERIAL_ORDER)
    surface = full.HeadSurface(head_n)
    full.add_head(head, head_n)
    hat_numbers = head_look(traits, replaced)
    skull = bases.is_skull(traits)
    if skull:
        # A skull: no leaf, no hair, no cream face; the bone is the face (chog_trait_bases.py).
        leaves = []
        side = traits.get("Side", "Right")
        bases.add_skull_face(head, head_n, surface, full, draw_shape, oval, SKIN, full.EYE, full.BLUSH, full.FACE,
                             side=side)
        bases.narrow_jaw(head, head_n, side)
    elif bases.is_radioactive(traits):
        # A radioactive Chog: the crown its hat leaves (all of it under horns), no cream face, the skull's
        # teeth and nose showing, no cheek.
        leaves = add_leaf_crown(head, head_n, surface, hat_numbers.get("LEAVES", FULL_CROWN),
                                hat_numbers.get("LEAF_SIDES"))
        bases.add_radioactive_hairline(head, head_n, surface, full.FACE)
        bases.add_skull_face(head, head_n, surface, full, draw_shape, oval, SKIN, full.EYE, full.BLUSH, full.FACE,
                             cheeks=False)
    else:
        leaves = add_leaf_crown(head, head_n, surface, hat_numbers.get("LEAVES", FULL_CROWN),
                                hat_numbers.get("LEAF_SIDES"))
        leaves += rim_leaves_for(head, traits, replaced, head_n, body_n, surface)
        # Points of hair that a hat lets out on the brow (the durag), in the skin colour: part of the head.
        if hat_numbers.get("HAIR_POINTS"):
            hats_and_clothes.add_hair_points(head, surface, hat_numbers["HAIR_POINTS"], SKIN)
        add_settled_face(head, face_numbers(head_n, hat_numbers), surface)
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
    one_number = one.number_of(traits)
    if skull or one_number in one.HANDS_OF_SKIN:
        bases.bone_hands(parts, materials, SKIN, full.GLOVE)
    if one_number in one.BLUSH:
        # Cheeks that are not pink (the manga page): their own material, in their colour.
        blush = bpy.data.materials.new(f"OneBlush{one_number}")
        blush.diffuse_color = linear(one.BLUSH[one_number])
        shader = blush.node_tree.nodes.get("Principled BSDF") if blush.node_tree else None
        if shader is not None:
            shader.inputs["Base Color"].default_value = linear(one.BLUSH[one_number])
        blush["srgb"] = list(one.BLUSH[one_number])
        blush["unlit"] = False
        for part in parts:
            for index, slot in enumerate(part.data.materials):
                if slot is not None and slot.name == full.BLUSH:
                    part.data.materials[index] = blush

    trait_parts, left_out = [], []
    # A hat that covers a trait (the balaclava over the mouth) says it in "HIDES": that part is not
    # built, as the pictures do not show it. One whose leaves come through it (a knitted mask, as the
    # pictures draw it) says "THROUGH": its crossings are kept in parts.json, not refused.
    hidden = set(hat_numbers.get("HIDES", ()))
    through = bool(hat_numbers.get("THROUGH"))
    seen = None
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
        if trait == "Mouth":
            centre_on_face(maker, surface)
        crossing = []
        worn_on_head = [shapes for shapes, on in maker.parts if on == "Head"]
        if trait in ("Head", "Body") and worn_on_head:
            # A hat, or the hood of a costume. Before the head and the part are turned up: both are
            # still in the axes of add_leaf_crown.
            crossing = leaf_crossings(leaves, worn_on_head, head_n, surface)
            if trait == "Head":
                seen = spike_sectors_seen(leaves, worn_on_head, head_n, surface)
        trait_made = trait_materials(maker.prefix, entry["colours"], entry.get("unlit", ()), materials,
                                     entry.get("glass", ()))
        for shapes, on in maker.parts:
            if on == "Head":
                full.turn_head_up(shapes, head_n)
            # On the body the outlines keep above the ground, as those of the feet do (the boots of a
            # costume, 16 October 2026).
            made = shapes.finish(shapes.name, Vector((0.0, 0.0, 0.0)), by_name[on], trait_made, on_ground=(on == "Body"))
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

    # Same head, same result: where the head keeps leaves, their tips must show all around it from the
    # camera of the game and from above (SPIKE_VIEWS).
    if seen is None:
        seen = spike_sectors_seen(leaves, [], head_n, surface)
    head_object = by_name["Head"]
    head_object["spikes_seen"] = "; ".join(f"{view}: {', '.join(f'{sector:+d}' for sector in sorted(sectors))}"
                                           for view, sectors in seen.items())
    row = (head_entry(traits, replaced) or {}).get("under_rim") or {}
    # A thing on the head that stands where spikes would (the ram horns at the sides) names the sectors that
    # must still show tips ("SECTORS" of its numbers), as a row under a rim does.
    required = hat_numbers.get("SECTORS") or row.get("SECTORS", SPIKE_SECTORS)
    head_object["spikes_missing"] = spikes_missing(seen, required) if leaves else ""

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
    for part in parts:
        if part.get("spikes_seen"):
            print(f"CHOG_TRAITS {label} spike tips seen: {part['spikes_seen']}")
        if part.get("spikes_missing"):
            failures.append(f"spikes not seen all around the head ({part['spikes_missing']})")
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
        skin = (bases.skin_of(traits) if bases.is_skull(traits) or bases.is_radioactive(traits)
                else SKINS.get(traits.get("Skin")))
        if skin is None:
            raise SystemExit(f"CHOG_TRAITS no colour recorded for Skin={traits.get('Skin')}")
        root["chog_genesis"] = str(number)
        root["chog_skin"] = list(skin["skin"])
        if "top" in skin:
            root["chog_skin_top"] = list(skin["top"])
        root["chog_face"] = list(skin.get("face", GENESIS_FACE))
        root["chog_blush"] = list(one.BLUSH.get(one.number_of(traits), GENESIS_BLUSH))
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
