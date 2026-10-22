"""Eyes and mouths of the Genesis as parts of the polished H3 head: the kinds and the values added on
14 October 2026 to those of make_chog_traits.py. Pictures only: nothing here goes into the game.

make_chog_traits.py imports this module and registers what it holds:

    EYES, MOUTHS                  a value of the trait "Eyes" or "Mouth" -> its kind, its colours, and
                                  the numbers of its kind it replaces ("numbers")
    EYES_OF_SIDE, MOUTHS_OF_SIDE  per Side, the values whose pictures of that Side are another drawing.
                                  NOT_BUILT marks a value whose drawing on that Side has no part: the
                                  hero then shows nothing there rather than the drawing of the other Side
    EYE_KINDS, MOUTH_KINDS        the builders

A builder takes the Maker of make_chog_traits.py and uses the helpers of that file (draw_shape,
draw_line, add_loops, ...), which hands itself over with attach() before any builder is called.

What is drawn (an eye, a mouth, a tear) is a flat coloured shape laid on the curved face, made of
geometry, never of an image. What is an object (goggles, a headset, glasses, a coin) has volume and the
dark outline shell of the model. Every shape was read on the 1000-point pictures of the Genesis
(.artifacts/genesis-art/1000/), on both Sides when a value is worn on both; the colours are given as
they look on the screen.

Places on the face are counted as in make_chog_traits.py: (to the side, above) from the middle of the
face, "to the side" towards the left of the character, which is the right of every picture.

Run alone, without building anything, it prints how many Genesis have their eyes, their mouth, and both
right with the entries of both files:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/chog_trait_faces.py -- --coverage
"""

import json
import math
import os
import sys
from types import SimpleNamespace

from mathutils import Vector

HOST = None        # make_chog_traits, once it called attach()


def attach(host):
    """Hands the helpers of make_chog_traits.py to the builders of this module."""
    global HOST
    HOST = host


X, Y, Z = Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0)), Vector((0.0, 0.0, 1.0))

# The lifts of what is drawn here, off the head: above the cheeks and the nose of the base, eight layers so
# that a tear can lie under an eye and a glint over it. (0.0232 to 0.0358, 0.0018 apart, until 18 October
# 2026: the eyes and mouths stood out of the face like sheets. Now on it: settled() of make_chog_traits.py
# says the rule, the lowest layer 0.0012 over the cheeks, each next one 0.0011 higher.)
LIFT = tuple(0.0080 + 0.0011 * layer for layer in range(8))    # 0.0066 + ... until 22 October 2026 (FACE_OVER in make_chog_traits.py)

DARK = (0.05, 0.05, 0.05)
WHITE = (0.95, 0.95, 0.95)
NOT_BUILT = {"kind": None, "colours": {}}

# ---------------------------------------------------------------------------------------------
# Eyes drawn on the face.
# ---------------------------------------------------------------------------------------------
# An eye whose top is cut by a straight lid that runs past it: "Side Eye", "Tired", the eyes of the
# clown and of the lasers on the pictures of Side=Left. Its middle, its half width and half height
# (of the whole oval), where the lid cuts it (above the middle, against the half height), how much the
# lid rises per unit towards the outer side, the lid line (thickness, how far it runs past the eye
# outwards and inwards), and the glints (to the side of the middle, the same side for both eyes: where
# the light is; above it; half width; half height).
SIDE_EYE = {
    "AT": (0.112, -0.004),
    "SIZE": (0.052, 0.052),
    "CUT": 0.16,
    "SLOPE": 0.10,
    "LID": (0.013, 0.022, 0.006),
    "GLINTS": ((-0.024, 0.002, 0.011, 0.013), (-0.030, -0.024, 0.004, 0.005)),
    "SEGMENTS": 16,
}
# "Tired": the same eye, lower under its lid, with a grey-blue bag under it: how far below the eye the
# bag is, how much larger, and the width of its dark line.
TIRED = {
    **SIDE_EYE,
    "CUT": 0.04,
    "SLOPE": -0.06,
    "GLINTS": ((0.020, -0.002, 0.010, 0.012),),
    "BAG": (0.012, 0.010, 0.010),
    "BAG_LINE": 0.006,
}
# The clown: the lidded eye on a tall four-pointed mark, in one or two colours (top, bottom): how far
# the mark reaches above and below the middle of the eye, and half its width.
CLOWN = {
    **SIDE_EYE,
    "SIZE": (0.046, 0.050),
    "CUT": 0.22,
    "SLOPE": 0.05,
    "GLINTS": ((-0.020, 0.006, 0.010, 0.012),),
    "MARK": (0.088, 0.078, 0.026),
}
# A round shiny eye: a dark ring, the white, a large pupil and two glints ("Teary", and under the
# tears of "Round Tear" and "Round Crying"). Half sizes; glints as in SIDE_EYE.
GLOSSY = {
    "AT": (0.112, 0.006),
    "RING": (0.064, 0.068),
    "WHITE": (0.054, 0.058),
    "PUPIL": (0.042, 0.046),
    "GLINTS": ((-0.014, 0.016, 0.013, 0.015), (0.016, -0.018, 0.006, 0.006)),
    "SEGMENTS": 16,
}
# A tear: where it leaves the eye (against the middle of the eye), where it ends (height on the face),
# its width at the top and at the bottom, the width of its dark line (0: none), the share of its
# width taken by the light streak along it, and how far it drifts to the side as it runs.
# (22 October 2026: the sideways numbers FROM[0] and DRIFT are magnitudes now, laid on the outer side of each eye, mirrored;
# they were signed towards -X for both eyes. The wide tears started 0.008 off the middle of the eye and drifted 0.004: now 0.024
# and 0.008, so that the tear reads as running down the outer half of the eye.)
WIDE_TEARS = {"FROM": (0.024, -0.044), "TO": -0.172, "WIDTH": (0.036, 0.044), "LINE": 0.007,
              "SHINE": 0.22, "DRIFT": 0.008}
THIN_TEAR = {"FROM": (0.040, -0.040), "TO": -0.128, "WIDTH": (0.010, 0.012), "LINE": 0.005,
             "SHINE": 0.0, "DRIFT": 0.008, "DROP": (0.010, 0.014),
             # the tear welling under the pupil: an arc in the white, half width, height of its sag,
             # thickness
             "WELL": (0.034, 0.012, 0.009)}
# "Closed Eye Crying": an arch over each closed eye, thick in its middle, slanted, and a wide tear
# under it with no dark line.
# (22 October 2026, evening, the owner on #806: "the tears are buggy, and the eyes are still the wrong way round, reason". Traced on the 1000
# point pictures of #806 and #24: each closed eye is a thick line that SAGS in its middle (◡: its middle 10 to 16 points below the line between
# its ends), its outer end a little lower; ours were arches (∩). ARC's height is now negative. The tears ran down to 0.172 under the middle of
# the face, past the jaw since the face was lowered: they hung under the chin as square sticks; on the pictures they end on the jaw, round.)
CLOSED_ARCH = {"AT": (0.112, 0.012), "ARC": (0.052, -0.016, 0.020), "TILT": -0.12, "STEPS": 10}
CLOSED_TEARS = {"FROM": (0.020, -0.012), "TO": -0.128, "WIDTH": (0.032, 0.040), "LINE": 0.0,
                "SHINE": 0.0, "DRIFT": 0.012}
# "Dead": a thick cross on each eye: half the length of a stroke, its thickness, its turn.
# (THICKNESS 0.022 and HALF 0.044 until 22 October 2026: the strokes of the pictures are a fifth of the width of the
# cross, ours a seventh.)
DEAD = {"AT": (0.112, 0.008), "HALF": 0.050, "THICKNESS": 0.036, "TURN_DEG": 4.0}   # (0.046 and 0.030 until 22 October 2026: bolder strokes on #117, #257, #289, #9)
# "Heart": a heart on each eye, its point down: half its width, the width of its dark line, how much
# its top leans outwards, and its glint (in the axes of the heart: to the side, above, half width,
# half height).
HEART = {"AT": (0.112, 0.012), "HALF_WIDTH": 0.070,   # 0.060 until 22 October 2026: the hearts of #8, #13, #12, #145 are about 1.2 times larger
          "LINE": 0.010, "LEAN_DEG": 12.0,
         "GLINT": (-0.026, 0.020, 0.011, 0.008), "SEGMENTS": 28}
# "Flat" (Side=Left): a short thick dash for each eye.
FLAT_EYE = {"AT": (0.112, 0.004), "HALF": 0.044, "THICKNESS": 0.017, "SLOPE": 0.04}
# The eyes of a skull as the pictures of Side=Right draw "Flat", "Normal" and "Up" on six skeletons
# (#846, #1214; #1079, #1311; #1313, #1889; 16 October 2026): a large dark socket for each eye, with a short
# white dash across its middle ("dash"), a round white glint near its top ("glint"), or nothing.
SOCKET = {"AT": (0.112, 0.008), "SOCKET": (0.062, 0.066), "DASH": (0.030, 0.009), "GLINT": (0.004, 0.026, 0.018, 0.018),
          "MARK": "", "SEGMENTS": 16,
          # MARK "angry": where the brow cuts the socket above its middle (against its half height), its slope.
          "ANGRY": (0.30, 0.45)}

# White eyes, as the pictures of Side=Left draw "Angry", "Vertical", "Smirk", "Frog", "Retard" and
# "Worried" (added 15 October 2026, read on the 1000-point pictures of CHOG #29, #91, #175, #63, #111, #2,
# #7, #15, #32, #182, #192, #247): a white shape in a dark line, a dark pupil in it, glints on the pupil,
# and, for some, a thick straight lid across the top or a thick arched upper lid.
#   AT          the middle of the eye: to the side of the middle of the face, above it
#   SIZE        half width and half height of the white
#   LINE        width of the dark line around the white
#   LID         None, or (where it cuts the eye above its middle against the half height, how much it
#               rises per unit towards the outer side (negative: towards the nose), its thickness, how
#               far it runs past the eye on the outer side)
#   ARCH        None, or the thickness of a thick upper lid along the top of the white: (at its outer
#               end, at its inner end)
#   TILT        degrees the eye is turned, its outer end lower when positive
#   PUPIL       half width and half height of the pupil; it is cut by the white and by the lid
#   LOOK        where the pupil is against the middle of the eye, for the left eye of the character
#               (+X, the right of the picture) then for its right eye: (to the right of the picture, up)
#   GLINTS      on the pupil, against its middle: to the right of the picture, up, half width, half
#               height
WHITE_EYE = {
    "AT": (0.112, 0.004),
    "SIZE": (0.054, 0.050),
    "LINE": 0.009,
    "LID": None,
    "ARCH": None,
    "TILT": 0.0,
    "PUPIL": (0.036, 0.042),
    "LOOK": ((0.0, 0.0), (0.0, 0.0)),
    "GLINTS": (),
    "SEGMENTS": 20,
    "EYES": None,
    # Both eyes tilted the same way (the left end lower), not mirrored: a glance to one side.
    "SAME_TILT": False,
}
# "Angry": a half disc under a straight lid that slopes down towards the nose; a large pupil looks to
# the right of the picture, cut by the lid.
# (22 October 2026, evening, the owner on #410: "these eyes do not match". At 1000 points (#410, #29, #276) the eye is a half disc under a
# flat lid, nearly all black: the pupil fills it but for a thin white crescent low on the right, a large white glint high on the left.
# Ours were white eyes with a small pupil under a thin lid.)
ANGRY = {**WHITE_EYE, "SIZE": (0.062, 0.050), "LID": (0.04, 0.10, 0.016, 0.012), "PUPIL": (0.056, 0.054),
         "LOOK": ((-0.012, 0.008), (-0.012, 0.008)), "GLINTS": ((-0.016, -0.012, 0.016, 0.014),)}
# "Vertical": a wide almond, a narrow upright pupil with a light stripe down it.
# (22 October 2026, evening, the owner on #1117: "some eyes must be improved, and this time really well". At 1000 points (#1117, #45, #296,
# #1133) the eye is a long flat almond, about 2.3 times as wide as tall, in a bold outline, its pupil a black upright capsule a little left of
# the middle, about 0.2 of the width and 0.7 of the height, a short white bar high on its left. Ours were ovals with a hairline pupil.)
VERTICAL = {**WHITE_EYE, "SIZE": (0.072, 0.027), "LINE": 0.013, "PUPIL": (0.015, 0.022), "LOOK": ((-0.008, 0.0), (-0.008, 0.0)),
            "GLINTS": ((-0.005, 0.008, 0.0045, 0.009),)}
# "Smirk": an almond whose outer end is lower, a thick arched upper lid, the pupil looking to the left
# of the picture.
# (22 October 2026, evening: the two different eyes of the strict audit were taken back with the rest of it, the owner finding
# them deformed in the game; the equal almonds of the morning stand until a smirk is shown better than them.)
# (22 October 2026, evening, the owner: "the smirk is not a smirk at all". At 1000 points (#2, #242, #111) each eye is a flat almond
# tilted with its outer end low, a THICK black lid along its whole top, a large black pupil filling its left part from top to bottom
# with a white glint high on it and a small one low: a smug squint. Ours were two round open eyes. Both eyes alike: the near and far
# eyes drawn differently earlier the same day were found deformed in the game.)
SMIRK = {**WHITE_EYE, "SIZE": (0.062, 0.033), "ARCH": (0.024, 0.014), "TILT": 16.0, "PUPIL": (0.036, 0.040),
         "LOOK": ((-0.024, -0.002), (-0.024, -0.002)), "GLINTS": ((-0.004, 0.012, 0.012, 0.008), (-0.026, -0.012, 0.004, 0.004)),
         # (Evening, the owner: "the smirk eyes are still not aligned". The seven Smirk pictures looked at (#2, #111, #242, #441, #455, #499,
         # #517) slant both eyes the same way, the left end lower: a sideways glance. Ours were mirrored, a frown.)
         "SAME_TILT": True}
# "Frog": a large round white, a wide flat pupil across its middle.
FROG = {**WHITE_EYE, "SIZE": (0.058, 0.054), "LINE": 0.012, "PUPIL": (0.042, 0.020),
        "GLINTS": ((-0.016, 0.006, 0.009, 0.006),)}
# "Retard": a large round white, a small pupil off its middle, each eye looking its own way.
RETARD = {**WHITE_EYE, "SIZE": (0.060, 0.060), "LINE": 0.012, "PUPIL": (0.021, 0.023),
          "LOOK": ((0.026, 0.018), (-0.028, -0.004))}
# "Worried": a white under a lid that rises towards the nose, a large dark pupil with three glints.
WORRIED_EYE = {**WHITE_EYE, "SIZE": (0.056, 0.054), "LINE": 0.011, "LID": (0.30, -0.26, 0.012, 0.004),
               "PUPIL": (0.044, 0.046), "LOOK": ((0.004, -0.006), (0.004, -0.006)),
               "GLINTS": ((-0.014, 0.012, 0.012, 0.010), (0.016, -0.014, 0.008, 0.007), (-0.018, -0.020, 0.005, 0.005))}
# "Base", the plain eyes of Side=Left: the lidded eye of "Side Eye", rounder, more of it under the lid,
# which runs further past it; two glints.
BASE_EYE = {**SIDE_EYE, "SIZE": (0.052, 0.050), "CUT": 0.36, "SLOPE": 0.10, "LID": (0.015, 0.020, 0.004),
            "GLINTS": ((-0.018, 0.012, 0.011, 0.014), (-0.024, -0.014, 0.004, 0.005))}
# "XD": a thick chevron on each eye, its point towards the nose: half width, half height, thickness.
XD = {"AT": (0.112, 0.004), "HALF": (0.040, 0.036), "THICKNESS": 0.016}

# ---------------------------------------------------------------------------------------------
# Eyes that are objects.
# ---------------------------------------------------------------------------------------------
# Goggles and headsets: one wide lens in a frame, wrapped around the front of the head, as the visor
# of make_chog_traits.py is. Its middle above the middle of the face; its half height in the middle of
# the front and at its ends; the oval it follows seen from above (about the middle of the head); how
# far around it goes; its thickness; the frame above and below the lens; how far the lens lies behind
# the front of the frame; the notch for the nose (how far the lower edge rises, over which share of
# the width); the colour of the lens from one end to the other ("PIECES", one per column); glare
# stripes (where across the lens at its top and at its bottom, half their width); a curved glint
# (where across at its start and end, height on the lens, thickness).
GOGGLES = {
    "UP": 0.022,
    "HALF_HEIGHT": (0.060, 0.050),
    "HALF_WIDTH": 0.330,
    "HALF_DEPTH": 0.298,
    "AROUND_DEG": 74.0,
    "THICKNESS": 0.028,
    "RIM": (0.014, 0.012),
    "LENS_SINK": 0.004,
    "NOTCH": (0.030, 0.26),
    "PIECES": ("A", "B", "C", "D", "C", "B", "A"),
    "STRIPES": (),
    "GLINT": (),
    "OUTLINE_THICKNESS": 0.014,
}
HEADSET = {
    **GOGGLES,
    "UP": 0.020,
    "HALF_HEIGHT": (0.064, 0.056),
    "THICKNESS": 0.040,
    "RIM": (0.016, 0.016),
    "NOTCH": (0.0, 0.2),
    "PIECES": ("Lens",) * 8,
}
# Glasses with a thin frame through which drawn eyes show ("Retro Glass"): the lens of "Anime Glass"
# with these numbers in place of its own (see ANIME_GLASS of make_chog_traits.py), then on each lens,
# in its plane: the iris (to the side of the middle of the lens, above it, half width, half height),
# a lid line over it (thickness, how far above the middle of the iris, slope) and two glints.
# (LENS_SEGMENTS: 16, those of "Anime Glass", until 15 October 2026; 12 for the triangle budget.)
RETRO_GLASS_NUMBERS = {"RIM": 0.11, "THICKNESS": 0.022, "LENS_BULGE": 0.003, "OUTLINE_THICKNESS": 0.009,
                       "LENS_SEGMENTS": 12,
                       "LENS": (0.128, 0.020, 0.104, 0.074)}
# (22 October 2026, the owner on a Retro Glass Genesis with a bubblegum: "the art draws half-closed, bored eyes under the glasses, we draw
# round open irises". On #35, #282, #632, #1184, #1621, #819, #165 the iris is large, about two thirds of the lens across and nearly its
# whole height, pale blue with a dark rim, a large dark pupil and big glints, and its top is cut by a heavy lid that slopes down towards
# the nose: a frown. Until then an iris 0.096 by 0.104 under a thin lid line that rose towards the nose. IRIS: to the side of the middle
# of the lens, above it, half width, half height; PUPIL: half width, half height; LID: thickness of the lid line, how far above the middle
# of the iris it cuts the iris and the pupil, how much it rises per unit towards the outer side.)
# (22 October 2026, evening, the owner: "there is still a circle that is not like the Genesis". At 1000 points (#35, #1184, #165) the iris
# has NO dark pupil: it is one steel blue disc under the lid, with a large pale blue highlight high on its left and a small white dot
# under that. The dark pupil disc and its rim made the circle. HIGHLIGHT: to the side of the middle of the iris, above it, half
# width, half height (the same side for both eyes: where the light is), drawn in the colour "Pupil"; GLINTS the white dot.)
# (Evening, the owner: "much better, but make the eyes a little smaller, they go past the glasses": the iris, 0.068 by 0.066, reached past
# the lens (0.104 by 0.074, its frame inside that). Now 0.056 by 0.054, and its highlight and glint with it.)
RETRO_EYES = {"IRIS": (0.004, -0.008, 0.056, 0.054), "HIGHLIGHT": (-0.020, 0.005, 0.020, 0.023), "LID": (0.012, 0.024, 0.30),
              "GLINTS": ((-0.025, -0.021, 0.007, 0.008),), "LIFT": 0.0025}
# "3D Glass" (CHOG #361, #379): the lens of "Anime Glass" more square, in a thick light grey frame; the
# lens over the right eye of the character blue, the one over its left eye red.
GLASSES_3D_NUMBERS = {"LENS": (0.126, 0.016, 0.100, 0.066), "LENS_SQUARENESS": 4.0, "LENS_TAPER": 0.04,
                      "RIM": 0.22, "THICKNESS": 0.028, "LENS_BULGE": 0.002, "OUTLINE_THICKNESS": 0.010}
# "BTC Glass" (CHOG #59, #189): round lenses that are two orange coins, a white B on each, a thin frame.
# (First: coins 0.072 across from their middle; on the bust they were half the size of those of the
# pictures, which cover the eyes and the cheeks.)
COIN_GLASS_NUMBERS = {"LENS": (0.126, 0.016, 0.084, 0.084), "LENS_SQUARENESS": 2.0, "LENS_TAPER": 0.0,
                      "RIM": 0.12, "THICKNESS": 0.024, "LENS_BULGE": 0.003, "OUTLINE_THICKNESS": 0.010,
                      "BRIDGE_UP": 0.010, "LENS_SEGMENTS": 12}
COIN_GLASS_PRINT = 0.62          # height of the B against the radius of the lens
# "Thug" (CHOG #132, #203): pixel sunglasses. Rows of blocks from the top down, each (above the middle of
# the face at its top, its height, its half width, how far its middle is moved outwards), for one lens;
# the bar across both lenses; the white pixels on each lens (outwards from the middle of the lens, above
# the middle of the face, half size); how far the front and the back of the blocks stand off the face.
PIXEL_SHADES = {
    "LENS_AT": 0.112,
    "ROWS": ((0.044, 0.024, 0.094, 0.000), (0.020, 0.022, 0.090, 0.004), (-0.002, 0.022, 0.074, 0.012),
             (-0.024, 0.020, 0.050, 0.020)),
    "BAR": (0.052, 0.018, 0.212),
    "WHITE": ((-0.040, 0.030, 0.0085), (-0.023, 0.012, 0.0085), (-0.006, -0.006, 0.0085)),
    "LIFTS": (0.008, 0.036),     # (0.018 and 0.046 until 18 October 2026, the face then 0.009 higher)
    "OUTLINE_THICKNESS": 0.008,
}

# ---------------------------------------------------------------------------------------------
# Mouths.
# ---------------------------------------------------------------------------------------------
# A line that curves ("Sad", "Smile", and the smiles of Side=Left): its ends, how far its middle
# rises above the straight line between them (negative: it sags), its thickness in the middle and at
# its ends, and a hook at either end (length against the line, how far it turns, in degrees).
SAD = {"FROM": (-0.040, -0.090), "TO": (0.074, -0.106), "BEND": 0.014, "THICKNESS": (0.012, 0.007),
       "HOOK_START": (0.0, 0.0), "HOOK_END": (0.0, 0.0), "STEPS": 12}
SMILE_CURL = {"FROM": (0.016, -0.082), "TO": (0.066, -0.082), "BEND": -0.016, "THICKNESS": (0.011, 0.008),
              "HOOK_START": (0.18, 70.0), "HOOK_END": (0.10, -50.0), "STEPS": 12}
# "Worried": a wavy line: its middle, half its width, how high a wave is, how many waves, thickness.
WORRIED = {"AT": (-0.022, -0.088), "HALF_WIDTH": 0.048, "WAVE": 0.010, "WAVES": 3.5, "THICKNESS": 0.010,
           "STEPS": 28, "TILT": -0.08}
# "Lip Bite": a long line slanting down towards the left of the character, two small teeth hanging from
# it: where along the line (0 at its start), half their width, how far they hang, their dark line.
LIP_BITE = {"FROM": (-0.076, -0.072), "TO": (0.104, -0.112), "BEND": 0.004, "THICKNESS": (0.010, 0.007),
            "TEETH": (0.30, 0.38), "TOOTH": (0.008, 0.013), "TOOTH_LINE": 0.0045, "STEPS": 10}
# Teeth, gritted or holding a coin: a band between two places, half its height, how square its ends
# are, the width of its dark line, how many teeth across, and whether the upper row is in shade at its
# top.
GRIT = {"FROM": (-0.024, -0.084), "TO": (0.104, -0.104), "HALF_TALL": 0.024, "SQUARENESS": 3.2,
        "LINE": 0.008, "TEETH": 6, "BITE": 0.005, "GAP": 0.0035}
# A coin held in the teeth: the teeth (as GRIT), the place of the coin on the face, how far its middle
# stands ahead of the face and below it, its radius, its thickness, its turn (degrees towards the
# left of the character, degrees up), the size of its print against the radius.
COIN = {
    **GRIT,
    "FROM": (-0.044, -0.078), "TO": (0.090, -0.098), "HALF_TALL": 0.020,
    # (RADIUS 0.046 until 15 October 2026: on the bust the coin was half the size the pictures draw it,
    # about the size of an eye.)
    # (22 October 2026, the owner on a Russian Hat Genesis: the coin sits too low. On #34, #790, #1185, #982, #1864, #25 the coin is
    # held at the left end of the grin, about a third of its width from the left, its middle on the lower line of the teeth. It
    # was 0.040 under that line (COIN_AT -0.100 and 0.020 down), at the very corner.)
    "COIN_AT": (-0.006, -0.092), "COIN_OFF": (0.040, 0.008), "RADIUS": 0.058, "COIN_THICKNESS": 0.013,
    # (18 sides and 6 teeth until 15 October 2026; 12 and 4 for the triangle budget of the dressed
    # heroes: the coin covers the middle of the teeth.)
    # (Evening, the owner on #25: "the coin is almost horizontal although it is held in the teeth". Tilted 28 degrees face up, on a head that
    # looks up a little it lay flat from the stage camera. The art (#25, #34, #395) holds it upright, its face to the viewer: 6 degrees face down.)
    # Turned towards the side the stage camera sees, as the art shows its face in three quarters.
    "COIN_TURN": (34.0, -6.0), "SIDES": 12, "PRINT": 0.62, "OUTLINE_THICKNESS": 0.008, "TEETH": 4,
}
# "Clown Mouth": a wide red smile with a dark line along its middle.
# (First: RED (0.030, 0.012) from -0.070 to 0.104: on the bust a thin red line where the pictures draw
# wide red lips.)
CLOWN_MOUTH = {"FROM": (-0.084, -0.076), "TO": (0.116, -0.082), "BEND": -0.036, "RED": (0.040, 0.016),
               "LINE": (0.009, 0.004), "STEPS": 14}
# "normal smile" as the pictures of Side=Left draw it (CHOG #2, #151, #214, #361, #485, #531, #1193; 17 October
# 2026): one long smile from under the nose to the far cheek, sagging in its middle and turning up at its far
# end. Until then those Genesis wore the short smile of Side=Right.
SMILE_LEFT = {"FROM": (-0.030, -0.078), "TO": (0.122, -0.074), "BEND": -0.030, "THICKNESS": (0.012, 0.007),
              "HOOK_START": (0.0, 0.0), "HOOK_END": (0.10, 55.0), "STEPS": 12}
# "Sad" as the pictures of Side=Left draw it (CHOG #46, #71, #131, #152, #182, #247, #1538): a short arch, a
# frown, beside the nose towards the far cheek, its ends a little turned out. Until then they wore the long
# slanted line of Side=Right.
SAD_LEFT = {"FROM": (0.012, -0.112), "TO": (0.090, -0.110), "BEND": 0.034, "THICKNESS": (0.012, 0.008),
            "HOOK_START": (0.10, -30.0), "HOOK_END": (0.10, 30.0), "STEPS": 10}
# "Flat": a short straight line.
FLAT_MOUTH = {"FROM": (0.022, -0.098), "TO": (0.076, -0.090), "BEND": 0.0, "THICKNESS": (0.012, 0.010),
              "HOOK_START": (0.0, 0.0), "HOOK_END": (0.0, 0.0), "STEPS": 4}


# ---------------------------------------------------------------------------------------------
# What every builder here is made with.
# ---------------------------------------------------------------------------------------------

def disc(part, surface, material, middle, half_width, half_height, lift, segments=12, squareness=2.0, taper=0.0):
    """A filled oval on the face."""
    HOST.full.add_skin(part, surface, HOST.full.patch_loops(surface, middle, half_width, half_height, squareness,
                                                            taper, lift, 1, segments), material)


def turned(middle, place, degrees):
    """A place given in the axes of a shape turned by "degrees" (counter-clockwise on the picture)."""
    angle = math.radians(degrees)
    return (middle[0] + place[0] * math.cos(angle) - place[1] * math.sin(angle),
            middle[1] + place[0] * math.sin(angle) + place[1] * math.cos(angle))


def oval_edge(middle, half_width, half_height, segments, degrees=0.0):
    return [turned(middle, (half_width * math.cos(2.0 * math.pi * index / segments),
                            half_height * math.sin(2.0 * math.pi * index / segments)), degrees)
            for index in range(segments)]


def curve(start, stop, bend, steps):
    """Places along a line from "start" to "stop" whose middle stands "bend" above the straight line
    (to its left as it runs: up for a line drawn from left to right)."""
    along = (stop[0] - start[0], stop[1] - start[1])
    length = math.hypot(*along) or 1.0
    across = (-along[1] / length, along[0] / length)
    places = []
    for index in range(steps + 1):
        share = index / steps
        lift = bend * 4.0 * share * (1.0 - share)
        places.append((start[0] + along[0] * share + across[0] * lift, start[1] + along[1] * share + across[1] * lift))
    return places


def hooked(places, hook_start, hook_end):
    """Adds a short hook at either end of a line: (length against the line, turn in degrees)."""
    length = sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(places, places[1:]))
    result = list(places)
    for at_end, (share, degrees) in ((False, hook_start), (True, hook_end)):
        if share <= 0.0:
            continue
        end, before = (result[-1], result[-2]) if at_end else (result[0], result[1])
        direction = math.atan2(end[1] - before[1], end[0] - before[0]) + math.radians(degrees)
        steps = 3
        added = []
        here = end
        for _ in range(steps):
            here = (here[0] + math.cos(direction) * length * share / steps,
                    here[1] + math.sin(direction) * length * share / steps)
            added.append(here)
            direction += math.radians(degrees) * 0.35
        result = result + added if at_end else list(reversed(added)) + result
    return result


def tapered(places, middle, ends):
    """Widths along a line: "middle" at its middle, "ends" at both ends."""
    count = len(places) - 1
    return [ends + (middle - ends) * math.sin(math.pi * index / count) ** 0.6 for index in range(count + 1)]


def stroke(part, surface, material, numbers, lift):
    """A curved line from a table such as SAD: its places and widths, then drawn. Returns its places."""
    places = curve(numbers.FROM, numbers.TO, numbers.BEND, numbers.STEPS)
    places = hooked(places, numbers.HOOK_START, numbers.HOOK_END)
    middle, ends = numbers.THICKNESS
    HOST.draw_line(part, surface, material, places, tapered(places, middle, ends), lift)
    return places


def lidded_eye(part, surface, material, numbers, side, lift):
    """A dark eye whose top is cut by a straight lid. Returns the lid as a function of the place across."""
    middle = (side * numbers.AT[0], numbers.AT[1])
    half_width, half_height = numbers.SIZE

    def lid(place):
        return middle[1] + half_height * numbers.CUT + numbers.SLOPE * (place - middle[0]) * side

    edge = [(place, min(up, lid(place))) for place, up in HOST.oval(middle, half_width, half_height, numbers.SEGMENTS)]
    inside = (middle[0], (middle[1] - half_height + lid(middle[0])) * 0.5)
    HOST.draw_shape(part, surface, material, edge, lift, middle=inside)
    return middle, lid


def lid_line(part, surface, material, numbers, middle, lid, side, lift):
    half_width = numbers.SIZE[0]
    thick, outwards, inwards = numbers.LID
    start, stop = middle[0] - side * (half_width + inwards), middle[0] + side * (half_width + outwards)
    steps = 4
    places = [(start + (stop - start) * index / steps, 0.0) for index in range(steps + 1)]
    places = [(place, lid(place) + thick * 0.25) for place, _ in places]
    HOST.draw_line(part, surface, material, places, [thick * 0.8] + [thick] * (steps - 1) + [thick * 0.6], lift)


def glints(part, surface, material, middle, table, lift):
    for to_side, above, wide, tall in table:
        disc(part, surface, material, (middle[0] + to_side, middle[1] + above), wide, tall, lift, 8)


def lidded_eyes(part, surface, numbers, dark, glint, lifts, seen=None):
    """Both lidded eyes with their lid lines and glints. Returns the middles of the eyes; "seen", a list, receives the middle of the
    area of each (what the lid leaves of the oval), where a beam leaves it."""
    middles = []
    for side in (1.0, -1.0):
        middle, lid = lidded_eye(part, surface, dark, numbers, side, lifts[0])
        if seen is not None:
            half_width, half_height = numbers.SIZE
            edge = [(place, min(up, lid(place))) for place, up in HOST.oval(middle, half_width, half_height, numbers.SEGMENTS)]
            seen.append(HOST.centroid(edge))
        lid_line(part, surface, dark, numbers, middle, lid, side, lifts[1])
        if glint:
            glints(part, surface, glint, middle, numbers.GLINTS, lifts[2])
        middles.append(middle)
    return middles


def glossy_eyes(part, surface, numbers, dark, white, lifts):
    """Both round shiny eyes. Returns their middles."""
    middles = []
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        segments = numbers.SEGMENTS
        disc(part, surface, dark, middle, *numbers.RING, lifts[0], segments)
        disc(part, surface, white, middle, *numbers.WHITE, lifts[1], segments)
        disc(part, surface, dark, middle, *numbers.PUPIL, lifts[2], segments)
        glints(part, surface, white, middle, numbers.GLINTS, lifts[3])
        middles.append(middle)
    return middles


def tear(part, surface, materials, start, numbers, lifts, side=1.0):
    """A tear running down from "start": its dark line (when it has one), its colour, a light streak,
    and a round end. "side" is the side of the face its eye is on (+1 for the eye at +X, -1 for the other): the tear
    drifts outwards and its streak lies on the inner side, so the two tears of a pair are mirror images
    (22 October 2026, the owner on the banana #321: "why are the eyes and tears in the same direction?"; until then
    both drifted towards -X, as in the three quarter pictures, which puts one on the outer side of its eye and the other on
    the inner side)."""
    line_material, tear_material, shine_material = materials
    top, bottom = numbers.WIDTH
    steps = 6
    places, widths = [], []
    for index in range(steps + 1):
        share = index / steps
        places.append((start[0] + side * abs(numbers.DRIFT) * share * share, start[1] + (numbers.TO - start[1]) * share))
        widths.append(top + (bottom - top) * share)
    end = places[-1]
    if numbers.LINE > 0.0:
        HOST.draw_line(part, surface, line_material, places, [width + 2.0 * numbers.LINE for width in widths], lifts[0])
        disc(part, surface, line_material, end, bottom * 0.5 + numbers.LINE, bottom * 0.5 + numbers.LINE, lifts[0], 10)
    HOST.draw_line(part, surface, tear_material, places, widths, lifts[1])
    disc(part, surface, tear_material, end, bottom * 0.5, bottom * 0.5, lifts[1], 10)
    if numbers.SHINE > 0.0:
        streak = [(place[0] - side * width * 0.22, place[1]) for place, width in zip(places[1:-1], widths[1:-1])]
        HOST.draw_line(part, surface, shine_material, streak, [width * numbers.SHINE for width in widths[1:-1]], lifts[2])


def heart_edge(middle, half_width, degrees, grow=0.0, segments=28):
    """The edge of a heart, point down: the classic curve, its middle at "middle"."""
    scale = (half_width + grow) / 16.0
    edge = []
    for index in range(segments):
        angle = 2.0 * math.pi * index / segments
        x = 16.0 * math.sin(angle) ** 3
        y = 13.0 * math.cos(angle) - 5.0 * math.cos(2 * angle) - 2.0 * math.cos(3 * angle) - math.cos(4 * angle) + 2.5
        edge.append(turned(middle, (x * scale, y * scale), degrees))
    return edge


def rounded_band(start, stop, half_tall, squareness, grow, segments=20):
    """The edge of a rounded rectangle laid along the line from "start" to "stop", made larger by
    "grow" all around. Returns the edge and its middle."""
    middle = ((start[0] + stop[0]) * 0.5, (start[1] + stop[1]) * 0.5)
    half_long = math.hypot(stop[0] - start[0], stop[1] - start[1]) * 0.5 + grow
    degrees = math.degrees(math.atan2(stop[1] - start[1], stop[0] - start[0]))
    edge = [turned(middle, (half_long * HOST.power(math.cos(2.0 * math.pi * index / segments), squareness),
                            (half_tall + grow) * HOST.power(math.sin(2.0 * math.pi * index / segments), squareness)),
                   degrees) for index in range(segments)]
    return edge, middle, degrees, half_long - grow


def teeth(part, surface, names, numbers, lifts):
    """A band of teeth: dark line, white, the line where the rows meet, the lines between teeth."""
    line_name, teeth_name = names
    edge, middle, degrees, half_long = rounded_band(numbers.FROM, numbers.TO, numbers.HALF_TALL, numbers.SQUARENESS,
                                                    numbers.LINE)
    HOST.draw_shape(part, surface, line_name, edge, lifts[0], middle=middle)
    edge, _, _, _ = rounded_band(numbers.FROM, numbers.TO, numbers.HALF_TALL, numbers.SQUARENESS, 0.0)
    HOST.draw_shape(part, surface, teeth_name, edge, lifts[1], middle=middle)
    bite = [turned(middle, (half_long * (-0.96 + 1.92 * index / 8), 0.0), degrees) for index in range(9)]
    HOST.draw_line(part, surface, line_name, bite, [numbers.BITE] * 9, lifts[2])
    for index in range(1, numbers.TEETH):
        across = half_long * (-1.0 + 2.0 * index / numbers.TEETH)
        for row in (1.0, -1.0):
            gap = [turned(middle, (across, row * numbers.HALF_TALL * share), degrees) for share in (0.05, 0.5, 0.92)]
            HOST.draw_line(part, surface, line_name, gap, [numbers.GAP] * 3, lifts[2])


# ---------------------------------------------------------------------------------------------
# Eye builders.
# ---------------------------------------------------------------------------------------------

def build_side_eye(maker):
    """Two dark eyes half closed under a straight lid that runs past them, with glints."""
    numbers = maker.numbers(SIDE_EYE)
    part = maker.part()
    lidded_eyes(part, maker.surface, numbers, part.named("Dark"), part.named("Glint"), LIFT)


def build_tired(maker):
    """The eyes of "Side Eye", lower under their lid, each over a grey-blue bag with a dark line."""
    numbers = maker.numbers(TIRED)
    part, surface = maker.part(), maker.surface
    drop, wider, taller = numbers.BAG
    # The bag is a larger eye a little lower, cut by a lid of its own; its dark line larger still.
    bag = SimpleNamespace(**{**vars(numbers), "AT": (numbers.AT[0], numbers.AT[1] - drop),
                             "SIZE": (numbers.SIZE[0] + wider, numbers.SIZE[1] + taller), "CUT": 0.0})
    line = SimpleNamespace(**{**vars(bag), "SIZE": (bag.SIZE[0] + numbers.BAG_LINE, bag.SIZE[1] + numbers.BAG_LINE)})
    for side in (1.0, -1.0):
        lidded_eye(part, surface, part.named("Dark"), line, side, LIFT[0])
        lidded_eye(part, surface, part.named("Bag"), bag, side, LIFT[1])
    lidded_eyes(part, surface, numbers, part.named("Dark"), part.named("Glint"), LIFT[2:])


def build_clown(maker):
    """The lidded eyes, each on a tall four-pointed mark of one or two colours."""
    numbers = maker.numbers(CLOWN)
    part, surface = maker.part(), maker.surface
    above, below, half_width = numbers.MARK
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        # The upper and the lower half of the mark, each a narrow pointed shape.
        for material, reach in (("Top", above), ("Bottom", -below)):
            steps = 6
            edge = []
            for index in range(steps + 1):
                share = index / steps
                width = half_width * (1.0 - share) ** 1.4
                edge.append((middle[0] - width, middle[1] + reach * share))
            for index in range(steps, -1, -1):
                share = index / steps
                width = half_width * (1.0 - share) ** 1.4
                edge.append((middle[0] + width, middle[1] + reach * share))
            HOST.draw_shape(part, surface, part.named(material), edge, LIFT[0],
                            middle=(middle[0], middle[1] + reach * 0.25))
    lidded_eyes(part, surface, numbers, part.named("Dark"), part.named("Glint"), LIFT[1:])


def build_teary(maker):
    """Two round shiny eyes: a dark ring, the white, a large pupil, two glints."""
    numbers = maker.numbers(GLOSSY)
    part = maker.part()
    glossy_eyes(part, maker.surface, numbers, part.named("Dark"), part.named("White"), LIFT[3:])


def build_round_tear(maker):
    """The shiny eyes of "Teary", a tear welling under each pupil and a thin one running down."""
    numbers = maker.numbers(GLOSSY)
    part, surface = maker.part(), maker.surface
    tears = SimpleNamespace(**THIN_TEAR)
    middles = glossy_eyes(part, surface, numbers, part.named("Dark"), part.named("White"), LIFT[3:])
    materials = (part.named("Dark"), part.named("Tear"), part.named("Tear"))
    for middle in middles:
        side = 1.0 if middle[0] >= 0.0 else -1.0
        start = (middle[0] + side * abs(tears.FROM[0]), middle[1] + tears.FROM[1])
        tear(part, surface, materials, start, tears, LIFT[0:3], side)
        drop_wide, drop_tall = tears.DROP
        drift = side * abs(tears.DRIFT)
        disc(part, surface, part.named("Dark"), (start[0] + drift, tears.TO - drop_tall * 0.4),
             drop_wide + tears.LINE, drop_tall + tears.LINE, LIFT[0], 10, taper=-0.5)
        disc(part, surface, part.named("Tear"), (start[0] + drift, tears.TO - drop_tall * 0.4),
             drop_wide, drop_tall, LIFT[1], 10, taper=-0.5)
        half_width, sag, thick = tears.WELL
        places = [(middle[0] + half_width * (-1.0 + 2.0 * index / 6),
                   middle[1] - numbers.PUPIL[1] - 0.002 - sag * (1.0 - (-1.0 + 2.0 * index / 6) ** 2) * 0.4)
                  for index in range(7)]
        HOST.draw_line(part, surface, part.named("Tear"), places, [thick * 0.4] + [thick] * 5 + [thick * 0.4],
                       LIFT[5] + 0.0009)


def build_round_crying(maker):
    """The shiny eyes of "Teary" over two wide tears, each with its dark line and a light streak."""
    numbers = maker.numbers(GLOSSY)
    part, surface = maker.part(), maker.surface
    tears = SimpleNamespace(**WIDE_TEARS)
    middles = glossy_eyes(part, surface, numbers, part.named("Dark"), part.named("White"), LIFT[3:])
    for middle in middles:
        side = 1.0 if middle[0] >= 0.0 else -1.0
        start = (middle[0] + side * abs(tears.FROM[0]), middle[1] + tears.FROM[1])
        tear(part, surface, (part.named("Dark"), part.named("Tear"), part.named("Shine")), start, tears, LIFT[0:3], side)


def build_closed_crying(maker):
    """Two closed eyes, each an arch thick in its middle, over a wide tear with no dark line."""
    numbers = maker.numbers(CLOSED_ARCH)
    part, surface = maker.part(), maker.surface
    tears = SimpleNamespace(**CLOSED_TEARS)
    half_width, arch, thick = numbers.ARC
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        start = (middle[0] + side * abs(tears.FROM[0]), middle[1] + tears.FROM[1])
        tear(part, surface, (part.named("Tear"), part.named("Tear"), part.named("Tear")), start, tears, LIFT[0:3], side)
        places, widths = [], []
        for index in range(numbers.STEPS + 1):
            along = -1.0 + 2.0 * index / numbers.STEPS
            hollow = 1.0 - along * along
            places.append((middle[0] + along * half_width,
                           middle[1] + arch * (hollow - 0.5) + numbers.TILT * along * half_width * side))
            widths.append(thick * (0.35 + 0.65 * hollow ** 0.6))
        HOST.draw_line(part, surface, part.named("Dark"), places, widths, LIFT[3])


def build_dead(maker):
    """A thick cross on each eye."""
    numbers = maker.numbers(DEAD)
    part, surface = maker.part(), maker.surface
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        for degrees in (45.0, -45.0):
            ends = [turned(middle, (numbers.HALF * share, 0.0), degrees + numbers.TURN_DEG * side)
                    for share in (-1.0, -0.5, 0.0, 0.5, 1.0)]
            HOST.draw_line(part, surface, part.named("Dark"), ends, [numbers.THICKNESS] * 5, LIFT[0])
            for end in (ends[0], ends[-1]):
                disc(part, surface, part.named("Dark"), end, numbers.THICKNESS * 0.5, numbers.THICKNESS * 0.5, LIFT[0], 8)


def build_heart(maker):
    """A red heart on each eye, its point down, a dark line around it and a glint, leaning outwards."""
    numbers = maker.numbers(HEART)
    part, surface = maker.part(), maker.surface
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        degrees = -numbers.LEAN_DEG * side
        fan = turned(middle, (0.0, -numbers.HALF_WIDTH * 0.06), degrees)
        HOST.draw_shape(part, surface, part.named("Dark"),
                        heart_edge(middle, numbers.HALF_WIDTH, degrees, numbers.LINE, numbers.SEGMENTS), LIFT[0],
                        middle=fan)
        HOST.draw_shape(part, surface, part.named("Heart"),
                        heart_edge(middle, numbers.HALF_WIDTH, degrees, 0.0, numbers.SEGMENTS), LIFT[1], middle=fan)
        to_side, above, wide, tall = numbers.GLINT
        glint_middle = turned(middle, (to_side, above), degrees)
        HOST.draw_shape(part, surface, part.named("Glint"), oval_edge(glint_middle, wide, tall, 8, degrees + 35.0),
                        LIFT[2], middle=glint_middle)


def build_flat_eye(maker):
    """A short thick dash for each eye."""
    numbers = maker.numbers(FLAT_EYE)
    part, surface = maker.part(), maker.surface
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        places = [(middle[0] + numbers.HALF * share, middle[1] + numbers.SLOPE * numbers.HALF * share * side)
                  for share in (-1.0, -0.5, 0.0, 0.5, 1.0)]
        HOST.draw_line(part, surface, part.named("Dark"), places, [numbers.THICKNESS] * 5, LIFT[0])
        for end in (places[0], places[-1]):
            disc(part, surface, part.named("Dark"), end, numbers.THICKNESS * 0.5, numbers.THICKNESS * 0.5, LIFT[0], 8)


def build_socket_eye(maker):
    """The dark sockets of a skull's eyes and the white mark in them (SOCKET)."""
    numbers = maker.numbers(SOCKET)
    part, surface = maker.part(), maker.surface
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        if numbers.MARK == "angry":
            # "Angry" on the skulls of Side=Right (#459, #552, #1642, #1667, #1861; 17 October 2026): the socket
            # cut along its top by a straight brow, lower towards the nose, as "Normal Eye" cuts its eye.
            cut, slope = numbers.ANGRY
            half_width, half_height = numbers.SOCKET

            def brow(x):
                return middle[1] + half_height * cut + slope * (x - middle[0]) * side

            edge = [(x, min(y, brow(x))) for x, y in oval_edge(middle, half_width, half_height, numbers.SEGMENTS)]
            HOST.draw_shape(part, surface, part.named("Dark"), edge, LIFT[0],
                            middle=(middle[0], middle[1] - half_height * 0.2))
        else:
            disc(part, surface, part.named("Dark"), middle, *numbers.SOCKET, LIFT[0], numbers.SEGMENTS)
        if numbers.MARK == "dash":
            disc(part, surface, part.named("White"), middle, *numbers.DASH, LIFT[1], 10, squareness=4.0)
        elif numbers.MARK == "glint":
            across, up, wide, tall = numbers.GLINT
            disc(part, surface, part.named("White"), (middle[0] + across * side, middle[1] + up), wide, tall, LIFT[1], 10)


def build_white_eye(defaults):
    """The builder of a kind of white eye (see WHITE_EYE): dark line, white, pupil, lid, glints."""
    def build(maker):
        every = maker.numbers(defaults)
        part, surface = maker.part(), maker.surface
        for side, look in zip((1.0, -1.0), every.LOOK):
            # "EYES": numbers of one eye only, by its side (+1 the eye at +X, -1 the other), for the values whose pictures draw
            # the two eyes differently (a smirk: a lidded eye and an open one), put over the numbers of both.
            numbers = SimpleNamespace(**{**vars(every), **(every.EYES or {}).get(side, {})})
            look = numbers.LOOK[0] if (every.EYES or {}).get(side, {}).get("LOOK") else look
            half_width, half_height = numbers.SIZE
            line, segments = numbers.LINE, numbers.SEGMENTS
            middle = (side * numbers.AT[0], numbers.AT[1])
            turn = numbers.TILT if numbers.SAME_TILT else -numbers.TILT * side

            def place(local):
                return turned(middle, local, turn)

            def lid(x, grow=0.0):
                """The height of the straight lid at a place across the eye (in the axes of the eye)."""
                if numbers.LID is None:
                    return 1.0
                cut, slope = numbers.LID[0], numbers.LID[1]
                return half_height * cut + slope * x * side + grow

            def outline(wide, tall, grow=0.0):
                return [(x, min(y, lid(x, grow))) for x, y in oval_edge((0.0, 0.0), wide, tall, segments)]

            inside = (0.0, (lid(0.0) - half_height) * 0.5 if numbers.LID else 0.0)
            HOST.draw_shape(part, surface, part.named("Dark"),
                            [place(point) for point in outline(half_width + line, half_height + line, line * 0.5)],
                            LIFT[0], middle=place(inside))
            HOST.draw_shape(part, surface, part.named("White"), [place(point) for point in outline(half_width, half_height)],
                            LIFT[1], middle=place(inside))

            # The pupil, cut by the white (each point outside it is brought back onto its edge, along
            # the line from the middle of the eye) and by the lid.
            pupil_wide, pupil_tall = numbers.PUPIL
            centre = (look[0], look[1])
            edge = []
            for x, y in oval_edge(centre, pupil_wide, pupil_tall, segments):
                reach = math.hypot(x / half_width, y / half_height)
                if reach > 0.97:
                    x, y = x * 0.97 / reach, y * 0.97 / reach
                edge.append((x, min(y, lid(x, -0.002))))
            reach = math.hypot(centre[0] / half_width, centre[1] / half_height)
            fan = centre if reach < 0.6 else (centre[0] * 0.6 / reach, centre[1] * 0.6 / reach)
            fan = (fan[0], min(fan[1], lid(fan[0]) - pupil_tall * 0.3))
            HOST.draw_shape(part, surface, part.named("Dark"), [place(point) for point in edge], LIFT[2],
                            middle=place(fan))

            if numbers.LID is not None:
                thick, past = numbers.LID[2], numbers.LID[3]
                start, stop = -side * (half_width + line), side * (half_width + line + past)
                places = [(start + (stop - start) * index / 4, 0.0) for index in range(5)]
                places = [place((x, lid(x) + thick * 0.3)) for x, _ in places]
                HOST.draw_line(part, surface, part.named("Dark"), places,
                               [thick * 0.8, thick, thick, thick, thick * 0.7], LIFT[3])
            if numbers.ARCH is not None:
                outer, inner = numbers.ARCH
                steps = 10
                places, widths = [], []
                for index in range(steps + 1):
                    # From the outer end of the eye over its top to its inner end.
                    angle = math.pi * index / steps
                    x = side * math.cos(angle) * (half_width + line * 0.3)
                    y = math.sin(angle) * (half_height + line * 0.3)
                    places.append(place((x, y)))
                    widths.append(outer + (inner - outer) * index / steps)
                HOST.draw_line(part, surface, part.named("Dark"), places, widths, LIFT[3])
            for to_right, above, wide, tall in numbers.GLINTS:
                spot = (centre[0] + to_right, centre[1] + above)
                HOST.draw_shape(part, surface, part.named("Glint"), [place(point) for point in oval_edge(spot, wide, tall, 8)],
                                LIFT[4], middle=place(spot))
    build.__doc__ = "A white eye of WHITE_EYE: dark line, white, pupil, lid, glints."
    return build


def build_base_eye(maker):
    """The plain eyes of Side=Left: lidded eyes as "Side Eye" draws them, rounder, with two glints."""
    numbers = maker.numbers(BASE_EYE)
    part = maker.part()
    lidded_eyes(part, maker.surface, numbers, part.named("Dark"), part.named("Glint"), LIFT)


def build_xd(maker):
    """A thick chevron on each eye, its point towards the nose."""
    numbers = maker.numbers(XD)
    part, surface = maker.part(), maker.surface
    half_width, half_height = numbers.HALF
    thick = numbers.THICKNESS
    for side in (1.0, -1.0):
        middle = (side * numbers.AT[0], numbers.AT[1])
        tip = (middle[0] - side * half_width, middle[1])
        for up in (1.0, -1.0):
            end = (middle[0] + side * half_width, middle[1] + up * half_height)
            places = [(tip[0] + (end[0] - tip[0]) * share, tip[1] + (end[1] - tip[1]) * share) for share in (0.0, 0.5, 1.0)]
            HOST.draw_line(part, surface, part.named("Dark"), places, [thick] * 3, LIFT[0])
            disc(part, surface, part.named("Dark"), end, thick * 0.5, thick * 0.5, LIFT[0], 8)
        disc(part, surface, part.named("Dark"), tip, thick * 0.55, thick * 0.55, LIFT[1], 8)


def lens_planes(maker, numbers):
    """The front of each lens of the glasses of build_anime_glass (make_chog_traits.py) built with these
    numbers: (side, the middle of its front, the way out of it, the way to the right, the way up)."""
    surface, head = maker.surface, maker.head
    lens_side, lens_up = numbers.LENS[0], numbers.LENS[1]
    planes = []
    for side in (1.0, -1.0):
        normal = ((-Y) * (1.0 - numbers.WRAP) + HOST.outward(surface, side * lens_side, lens_up) * numbers.WRAP).normalized()
        right = (X - normal * X.dot(normal)).normalized()
        up = normal.cross(right)
        centre = (surface.front(side * lens_side, lens_up, head.FACE_LIFT) + normal * numbers.THICKNESS * 0.5
                  + normal * (numbers.THICKNESS * 0.5 + numbers.LENS_BULGE))
        planes.append((side, centre, normal, right, up))
    return planes


def lay_bitcoin(part, material, origin, across, upwards, axis, size):
    """The Bitcoin B (the letter of the font built into Blender and two short bars through it), flat in
    the plane of "across" and "upwards" at "origin", facing "axis"; "size" is half its height. One point
    per curve of the letter: it is small."""
    pieces = HOST.word_pieces("B", 0.03, 1.0, 1)
    low = min(point.co.y for point in pieces.verts)
    high = max(point.co.y for point in pieces.verts)
    middle_x = (min(point.co.x for point in pieces.verts) + max(point.co.x for point in pieces.verts)) * 0.5
    scale = size * 1.25 / max(high - low, 1e-6)

    def on_plane(x, y):
        return origin + across * x + upwards * y

    mesh = part.mesh
    placed, made = {}, []
    for piece in pieces.faces:
        corners = []
        for point in piece.verts:
            if point.index not in placed:
                placed[point.index] = mesh.verts.new(on_plane((point.co.x - middle_x) * scale,
                                                              (point.co.y - (low + high) * 0.5) * scale))
            corners.append(placed[point.index])
        if len(set(corners)) < 3:
            continue
        face = mesh.faces.new(corners)
        face.normal_update()
        if face.normal.dot(axis) < 0.0:
            face.normal_flip()
        made.append(face)
    pieces.free()
    part.paint(made, material)
    part.open_faces.extend(made)
    flat = SimpleNamespace(middle=origin - axis)
    bar_half_width, bar_half_tall = size * 0.06, size * 0.16
    for x in (-size * 0.12, size * 0.10):
        for y in (size * 0.72, -size * 0.72):
            corners = [on_plane(x - bar_half_width, y - bar_half_tall), on_plane(x + bar_half_width, y - bar_half_tall),
                       on_plane(x + bar_half_width, y + bar_half_tall), on_plane(x - bar_half_width, y + bar_half_tall)]
            HOST.full.add_skin(part, flat, [corners[:2], [corners[3], corners[2]]], material, around=False)


def build_glasses_3d(maker):
    """The glasses of "Anime Glass" with a thick frame and square lenses, the lens over the left eye of
    the character in the colour "Red", the other in "Lens"."""
    HOST.build_anime_glass(maker)
    part = maker.parts[-1][0]
    lens = part.order.index(part.named("Lens"))
    part.paint([face for face in part.mesh.faces if face.material_index == lens and face.calc_center_median().x > 0.0],
               part.named("Red"))


def build_coin_glass(maker):
    """Round glasses whose lenses are two coins, with the Bitcoin B on each."""
    HOST.build_anime_glass(maker)
    part = maker.parts[-1][0]
    numbers = maker.numbers(HOST.ANIME_GLASS)
    for side, centre, normal, right, up in lens_planes(maker, numbers):
        lay_bitcoin(part, part.named("Print"), centre + normal * 0.0015, right, up, normal,
                    numbers.LENS[3] * COIN_GLASS_PRINT)


def build_pixel_shades(maker):
    """Pixel sunglasses: blocks with volume, stacked into two lenses with stepped lower edges and a bar
    across, white square pixels on the front of each lens."""
    numbers = maker.numbers(PIXEL_SHADES)
    part, surface = maker.part(), maker.surface
    back, front = numbers.LIFTS

    def block(left, right, top, bottom):
        rows = []
        for lift in (back, front):
            rows.append([surface.front(left, bottom, lift), surface.front(right, bottom, lift),
                         surface.front(right, top, lift), surface.front(left, top, lift)])
        HOST.add_loops(part, rows, part.named("Dark"), numbers.OUTLINE_THICKNESS, smooth=False)

    top, tall, half = numbers.BAR
    block(-half, half, top, top - tall)
    for side in (1.0, -1.0):
        middle = side * numbers.LENS_AT
        for above, height, half_width, outwards in numbers.ROWS:
            centre = middle + side * outwards
            block(centre - half_width, centre + half_width, above, above - height)
        for outwards, above, size in numbers.WHITE:
            x = middle + side * outwards
            corners = [surface.front(x - size, above - size, front + 0.0015), surface.front(x + size, above - size, front + 0.0015),
                       surface.front(x + size, above + size, front + 0.0015), surface.front(x - size, above + size, front + 0.0015)]
            HOST.full.add_skin(part, surface, [corners[:2], [corners[3], corners[2]]], part.named("White"), around=False)


def wrap_lens(maker, defaults):
    """Goggles or a headset: one wide lens in a frame, wrapped around the front of the head. The lens
    takes the colours of PIECES from one end to the other; stripes and a glint are laid on it."""
    numbers = maker.numbers(defaults)
    part, surface, head = maker.part(), maker.surface, maker.head
    pieces = numbers.PIECES
    columns = len(pieces) + 2
    height = head.FACE_MIDDLE[1] + numbers.UP
    middle_tall, end_tall = numbers.HALF_HEIGHT
    rim_top, rim_bottom = numbers.RIM
    notch, notch_width = numbers.NOTCH

    def frame_at(across):
        angle = math.radians(across * numbers.AROUND_DEG)
        here = surface.middle + Vector((numbers.HALF_WIDTH * math.sin(angle), -numbers.HALF_DEPTH * math.cos(angle), 0.0))
        out = Vector((math.sin(angle) / numbers.HALF_WIDTH, -math.cos(angle) / numbers.HALF_DEPTH, 0.0)).normalized()
        tall = middle_tall + (end_tall - middle_tall) * across * across
        rise = notch * max(0.0, 1.0 - (across / notch_width) ** 2) if notch_width > 0.0 else 0.0
        return here, out, height + tall, height - tall + rise

    def on_lens(across, share, forward):
        """A point of the lens: "share" 0 at its lower edge, 1 at its upper edge."""
        here, out, top, bottom = frame_at(across)
        low, high = bottom + rim_bottom, top - rim_top
        return here + Z * (low + (high - low) * share) + out * (forward - numbers.LENS_SINK)

    loops, shares = [], []
    for index in range(columns + 1):
        across = -1.0 + 2.0 * index / columns
        here, out, top, bottom = frame_at(across)
        back = here - out * numbers.THICKNESS
        loops.append([here + Z * top, on_lens(across, 1.0, 0.0), on_lens(across, 0.0, 0.0), here + Z * bottom,
                      back + Z * (bottom + 0.004), back + Z * (top - 0.004)])
        at_end = index in (0, columns)
        shares.append((0.0,) * 6 if at_end else (1.0, 1.0, 1.0, 1.0, 0.2, 0.2))
    bands, _ = HOST.add_loops(part, loops, part.named("Frame"), numbers.OUTLINE_THICKNESS, shares=shares)
    for index, band in enumerate(bands):
        if 1 <= index <= len(pieces):
            part.paint([band[1]], part.named(pieces[index - 1]))

    flat = SimpleNamespace(middle=surface.middle)
    lens_columns = len(pieces)

    def lens_across(share_across):
        """From 0 (the first lens column) to 1 (the last) to "across" of the band."""
        return -1.0 + 2.0 * (1.0 + share_across * lens_columns) / columns

    for top_at, bottom_at, half in numbers.STRIPES:
        steps = 4
        rows = []
        for edge in (-1.0, 1.0):
            row = []
            for index in range(steps + 1):
                share = index / steps
                across = bottom_at + (top_at - bottom_at) * share + edge * half
                row.append(on_lens(lens_across(across), 0.06 + 0.88 * share, 0.0025))
            rows.append(row)
        HOST.full.add_skin(part, flat, rows, part.named("Glare"), around=False)
    if numbers.GLINT:
        start, stop, up, thick = numbers.GLINT
        steps = 6
        rows = []
        for edge in (-1.0, 1.0):
            row = []
            for index in range(steps + 1):
                share = index / steps
                across = start + (stop - start) * share
                bend = up + 0.10 * math.sin(math.pi * share) + edge * thick * (0.4 + 0.6 * math.sin(math.pi * share))
                row.append(on_lens(lens_across(across), bend, 0.0025))
            rows.append(row)
        HOST.full.add_skin(part, flat, rows, part.named("Glare"), around=False)


def build_goggles(maker):
    """Ski goggles: a frame, a lens in a gradient from one end to the other, a notch for the nose."""
    wrap_lens(maker, GOGGLES)


def build_headset(maker):
    """A headset: a thick frame around one dark lens, with glare on it."""
    wrap_lens(maker, HEADSET)


def build_retro_glass(maker):
    """The glasses of "Anime Glass" with a thin frame and tinted lenses, the eyes seen through them: an
    iris, a pupil, a lid line and glints laid on each lens."""
    HOST.build_anime_glass(maker)
    part = maker.parts[-1][0]
    numbers = maker.numbers(HOST.ANIME_GLASS)
    surface, head = maker.surface, maker.head
    eyes = SimpleNamespace(**RETRO_EYES)
    lens_side, lens_up = numbers.LENS[0], numbers.LENS[1]
    for side in (1.0, -1.0):
        # The plane of the lens, as build_anime_glass of make_chog_traits.py computes it.
        normal = ((-Y) * (1.0 - numbers.WRAP) + HOST.outward(surface, side * lens_side, lens_up) * numbers.WRAP).normalized()
        right = (X - normal * X.dot(normal)).normalized()
        up = normal.cross(right)
        centre = (surface.front(side * lens_side, lens_up, head.FACE_LIFT) + normal * numbers.THICKNESS * 0.5
                  + normal * (numbers.THICKNESS * 0.5 + numbers.LENS_BULGE))
        behind = SimpleNamespace(middle=centre - normal)

        def on_lens(place, lift):
            return centre + right * place[0] + up * place[1] + normal * lift

        def lay(material, edge, middle, lift):
            HOST.full.add_skin(part, behind, [[on_lens(middle, lift)], [on_lens(place, lift) for place in edge]], material)

        to_side, above, half_width, half_height = eyes.IRIS
        iris = (to_side, above)
        thick, lid_above, slope = eyes.LID

        def under_lid(edge):
            """The edge of a shape cut by the lid: the lid runs level with the iris at "lid_above" above its middle and rises
            by "slope" per unit towards the outer side (side of the lens)."""
            return [(place, min(up, above + lid_above + slope * (place - to_side) * side)) for place, up in edge]

        lay(part.named("Iris"), under_lid(oval_edge(iris, half_width, half_height, 16)), iris, eyes.LIFT)
        shine_side, shine_up, shine_wide, shine_tall = eyes.HIGHLIGHT
        shine = (to_side + shine_side, above + shine_up)
        lay(part.named("Pupil"), under_lid(oval_edge(shine, shine_wide, shine_tall, 12)), shine, eyes.LIFT * 2.0)
        steps = 4
        lid = [(to_side + half_width * 1.15 * (-1.0 + 2.0 * index / steps),
                above + lid_above + thick * 0.4 + slope * half_width * 1.15 * (-1.0 + 2.0 * index / steps) * side)
               for index in range(steps + 1)]
        for first, second in zip(lid, lid[1:]):
            # A short slab of the lid line between two places, laid on the lens.
            along = Vector((second[0] - first[0], second[1] - first[1], 0.0))
            across = Vector((-along.y, along.x, 0.0)).normalized() * thick * 0.5
            corners = [(first[0] - across.x, first[1] - across.y), (second[0] - across.x, second[1] - across.y),
                       (second[0] + across.x, second[1] + across.y), (first[0] + across.x, first[1] + across.y)]
            mid = ((first[0] + second[0]) * 0.5, (first[1] + second[1]) * 0.5)
            lay(part.named("Lid"), corners, mid, eyes.LIFT * 3.0)
        for glint_side, glint_up, wide, tall in eyes.GLINTS:
            spot = (to_side + glint_side, above + glint_up)
            lay(part.named("Glint"), oval_edge(spot, wide, tall, 8), spot, eyes.LIFT * 3.0)


def build_laser(maker):
    """Lasers: on the pictures of Side=Right the dark eyes of "Normal Eye" (build_laser of
    make_chog_traits.py); on those of Side=Left ("eyes": "lidded") the lidded eyes of "Side Eye", lower,
    without glints, and the same short beams."""
    if maker.entry.get("eyes") != "lidded":
        HOST.build_laser(maker)
        return
    numbers = maker.numbers(HOST.LASER)
    part, surface = maker.part(), maker.surface
    eyes = SimpleNamespace(**{**SIDE_EYE, **maker.entry.get("eye_numbers", {})})
    centres = []
    lidded_eyes(part, surface, eyes, part.named("Dark"), None, LIFT, centres)
    middles = centres
    ahead, down = HOST.level_axes(maker.head)
    radius, sides = numbers.BEAM_RADIUS, numbers.BEAM_SIDES
    # The beam turned up from level by BEAM_PITCH_DEG, in the plane of the ahead and up axes of the head.
    pitch = math.radians(numbers.BEAM_PITCH_DEG)
    way = ahead * math.cos(pitch) - down * math.sin(pitch)
    across = -(down * math.cos(pitch) + ahead * math.sin(pitch))
    for middle in middles:
        start = surface.front(middle[0], middle[1], 0.0)
        stations = ((-numbers.BEAM_SUNK, 0.7), (0.02, 1.0), (numbers.BEAM_LENGTH - 0.03, 1.0), (numbers.BEAM_LENGTH, 0.55))
        loops = [HOST.ring_points(start + way * distance, X * radius * size, across * size * radius, sides)
                 for distance, size in stations]
        shares = [(0.0,) * sides] + [(1.0,) * sides] * (len(stations) - 1)
        bands, ends = HOST.add_loops(part, loops, part.named("Beam"), numbers.OUTLINE_THICKNESS, shares=shares)
        for band in bands:
            part.paint([band[index] for index in numbers.BEAM_CORE], part.named("Core"))
        part.paint(ends[-1:], part.named("Core"))


# ---------------------------------------------------------------------------------------------
# Mouth builders.
# ---------------------------------------------------------------------------------------------

def build_line_mouth(defaults):
    def build(maker):
        numbers = maker.numbers(defaults)
        part = maker.part()
        stroke(part, maker.surface, part.named("Line"), numbers, LIFT[0])
    build.__doc__ = "A drawn line that curves, with a hook at either end when its table asks for one."
    return build


def build_worried(maker):
    """A wavy line."""
    numbers = maker.numbers(WORRIED)
    part = maker.part()
    places = []
    for index in range(numbers.STEPS + 1):
        along = -1.0 + 2.0 * index / numbers.STEPS
        wave = math.sin(along * numbers.WAVES * math.pi) * numbers.WAVE
        places.append((numbers.AT[0] + along * numbers.HALF_WIDTH, numbers.AT[1] + wave + numbers.TILT * along * numbers.HALF_WIDTH))
    widths = [numbers.THICKNESS * (0.7 if index in (0, numbers.STEPS) else 1.0) for index in range(numbers.STEPS + 1)]
    HOST.draw_line(part, maker.surface, part.named("Line"), places, widths, LIFT[0])


def build_lip_bite(maker):
    """A long line slanting down to one side, two small teeth hanging from it."""
    numbers = maker.numbers(LIP_BITE)
    part, surface = maker.part(), maker.surface
    places = curve(numbers.FROM, numbers.TO, numbers.BEND, numbers.STEPS)
    middle, ends = numbers.THICKNESS
    HOST.draw_line(part, surface, part.named("Line"), places, tapered(places, middle, ends), LIFT[2])
    half_width, hangs = numbers.TOOTH
    for where in numbers.TEETH:
        top = (numbers.FROM[0] + (numbers.TO[0] - numbers.FROM[0]) * where,
               numbers.FROM[1] + (numbers.TO[1] - numbers.FROM[1]) * where - middle * 0.3)
        # A small U: straight sides from the line down, a round end.
        edge = [(top[0] - half_width, top[1])]
        steps = 6
        for index in range(steps + 1):
            angle = math.pi * index / steps
            edge.append((top[0] - half_width * math.cos(angle), top[1] - (hangs - half_width) - half_width * math.sin(angle)))
        edge.append((top[0] + half_width, top[1]))
        inside = (top[0], top[1] - hangs * 0.45)
        line = numbers.TOOTH_LINE
        grown = [(inside[0] + (place[0] - inside[0]) * (1.0 + line / half_width),
                  inside[1] + (place[1] - inside[1]) * (1.0 + line / hangs)) for place in edge]
        HOST.draw_shape(part, surface, part.named("Line"), grown, LIFT[0], middle=inside)
        HOST.draw_shape(part, surface, part.named("Teeth"), edge, LIFT[1], middle=inside)


def build_grit(maker):
    """Gritted teeth: a band of teeth, its two rows meeting along a line."""
    numbers = maker.numbers(GRIT)
    part = maker.part()
    teeth(part, maker.surface, (part.named("Line"), part.named("Teeth")), numbers, LIFT)


def build_coin(maker):
    """A coin held in the teeth: the band of teeth drawn on the face, and the coin, a disc with volume,
    its face printed with the mark of the entry ("print": "bitcoin" or "monad")."""
    numbers = maker.numbers(COIN)
    part, surface = maker.part(), maker.surface
    teeth(part, surface, (part.named("Line"), part.named("Teeth")), numbers, LIFT)

    ahead, down = HOST.level_axes(maker.head)
    mouth = surface.front(numbers.COIN_AT[0], numbers.COIN_AT[1], 0.0)
    forward, below = numbers.COIN_OFF
    centre = mouth + ahead * forward + down * below
    turn, tilt = (math.radians(value) for value in numbers.COIN_TURN)
    axis = (ahead * math.cos(turn) + X * math.sin(turn)) * math.cos(tilt) - down * math.sin(tilt)
    axis.normalize()
    across, upwards = HOST.base.frame_across(axis, X)
    radius, half_thick = numbers.RADIUS, numbers.COIN_THICKNESS * 0.5
    rings = [(centre - axis * half_thick, across * radius * 0.96, upwards * radius * 0.96),
             (centre - axis * half_thick * 0.6, across * radius, upwards * radius),
             (centre + axis * half_thick * 0.6, across * radius, upwards * radius),
             (centre + axis * half_thick, across * radius * 0.96, upwards * radius * 0.96)]
    points, faces = part.shell(rings, numbers.SIDES, part.named("Edge"), numbers.OUTLINE_THICKNESS, smooth=False)
    front_face = max(faces, key=lambda face: (face.calc_center_median() - centre).dot(axis) + len(face.verts) * 1e-6)
    part.paint([front_face], part.named("Coin"))

    flat = SimpleNamespace(middle=centre)
    face_at = centre + axis * (half_thick + 0.0015)
    size = radius * numbers.PRINT

    def on_coin(x, y, lift=0.0):
        return face_at + across * x + upwards * y + axis * lift

    mark = maker.entry.get("print")
    if mark == "monad":
        # A rounded square stood on its corner, as a ring.
        rings_of = []
        for scale in (0.62, 1.0):
            row = []
            for index in range(16):
                angle = 2.0 * math.pi * index / 16
                x, y = HOST.power(math.cos(angle), 3.0) * size * 0.72 * scale, HOST.power(math.sin(angle), 3.0) * size * 0.72 * scale
                row.append(on_coin((x - y) * 0.7071, (x + y) * 0.7071))
            rings_of.append(row)
        HOST.full.add_skin(part, flat, rings_of, part.named("Print"))
    elif mark == "bitcoin":
        # A B from the font built into Blender, and the two short bars through it.
        pieces = HOST.word_pieces("B", 0.03, 1.0)
        low = min(point.co.y for point in pieces.verts)
        high = max(point.co.y for point in pieces.verts)
        middle_x = (min(point.co.x for point in pieces.verts) + max(point.co.x for point in pieces.verts)) * 0.5
        scale = size * 1.25 / max(high - low, 1e-6)
        mesh = part.mesh
        placed, made = {}, []
        for piece in pieces.faces:
            corners = []
            for point in piece.verts:
                if point.index not in placed:
                    placed[point.index] = mesh.verts.new(on_coin((point.co.x - middle_x) * scale,
                                                                 (point.co.y - (low + high) * 0.5) * scale))
                corners.append(placed[point.index])
            if len(set(corners)) < 3:
                continue
            face = mesh.faces.new(corners)
            face.normal_update()
            if face.normal.dot(axis) < 0.0:
                face.normal_flip()
            made.append(face)
        pieces.free()
        part.paint(made, part.named("Print"))
        part.open_faces.extend(made)
        bar_half_width, bar_half_tall = size * 0.06, size * 0.16
        for x in (-size * 0.12, size * 0.10):
            for y in (size * 0.72, -size * 0.72):
                corners = [on_coin(x - bar_half_width, y - bar_half_tall), on_coin(x + bar_half_width, y - bar_half_tall),
                           on_coin(x + bar_half_width, y + bar_half_tall), on_coin(x - bar_half_width, y + bar_half_tall)]
                HOST.full.add_skin(part, flat, [corners[:2], [corners[3], corners[2]]], part.named("Print"), around=False)


def build_clown_mouth(maker):
    """A wide red smile with a dark line along its middle."""
    numbers = maker.numbers(CLOWN_MOUTH)
    part, surface = maker.part(), maker.surface
    places = curve(numbers.FROM, numbers.TO, numbers.BEND, numbers.STEPS)
    HOST.draw_line(part, surface, part.named("Red"), places, tapered(places, *numbers.RED), LIFT[0])
    HOST.draw_line(part, surface, part.named("Line"), places, tapered(places, *numbers.LINE), LIFT[1])


# ---------------------------------------------------------------------------------------------
# The tables.
# ---------------------------------------------------------------------------------------------
TEAR_BLUE = (0.62, 0.82, 0.95)

EYES = {
    # Lasers: colours read on the beams of CHOG #41 and #812 (fire), #875 (green). A fire beam is red
    # with a yellow middle a third of its width. (First: three of its eight sides yellow; from the
    # bust and from above, where those are the sides seen, the beam was a yellow stick.)
    "fire laser": {
        "kind": "Laser",
        "colours": {"Dark": DARK, "Beam": (0.93, 0.16, 0.08), "Core": (0.99, 0.88, 0.16)},
        "unlit": ("Dark", "Beam", "Core"),
        "numbers": {"BEAM_CORE": (1,), "BEAM_RADIUS": 0.021},
    },
    "Green laser": {
        "kind": "Laser",
        "colours": {"Dark": DARK, "Beam": (0.13, 0.66, 0.16), "Core": (0.52, 0.93, 0.36)},
        "unlit": ("Dark", "Beam", "Core"),
    },
    # Visors: the colours across the band from one end to the other, read on CHOG #1 and #100
    # (gradient: blue at the ends, pink, peach in the middle) and #82 and #114 (plasma: blue at the
    # ends, cyan, green, yellow in the middle).
    "Gradient visor": {
        "kind": "Visor",
        "colours": {"Blue": (0.27, 0.27, 0.70), "Violet": (0.55, 0.40, 0.72), "Mauve": (0.82, 0.56, 0.70),
                    "Salmon": (0.94, 0.68, 0.68), "Peach": (0.97, 0.79, 0.70), "Light": (0.97, 0.86, 0.76)},
        "numbers": {"PIECES": ("Blue", "Violet", "Mauve", "Salmon", "Peach", "Light", "Light", "Peach", "Salmon",
                               "Mauve", "Violet", "Blue")},
    },
    "Plasma visor": {
        "kind": "Visor",
        "colours": {"Blue": (0.16, 0.22, 0.86), "Cyan": (0.10, 0.55, 0.90), "Teal": (0.16, 0.76, 0.62),
                    "Green": (0.45, 0.88, 0.26), "Lime": (0.74, 0.94, 0.16), "Yellow": (0.86, 0.95, 0.14)},
        "numbers": {"PIECES": ("Blue", "Cyan", "Teal", "Green", "Lime", "Yellow", "Yellow", "Lime", "Green", "Teal",
                               "Cyan", "Blue")},
    },
    # The same drawing as "Round" (CHOG #37, #237, #885, #953 beside #55, #313, #1078): a slightly
    # smaller pupil in a wider white.
    "Round Eye": {
        "kind": "Round",
        "colours": {"Dark": DARK, "White": WHITE},
        "unlit": ("Dark", "White"),
        "numbers": {"PUPIL": (0.034, 0.039)},
    },
    "Side Eye": {
        "kind": "SideEye",
        "colours": {"Dark": DARK, "Glint": WHITE},
        "unlit": ("Dark", "Glint"),
    },
    "Tired": {
        "kind": "Tired",
        "colours": {"Dark": DARK, "Bag": (0.62, 0.66, 0.78), "Glint": WHITE},
        "unlit": ("Dark", "Bag", "Glint"),
    },
    "Clown Eye": {
        "kind": "Clown",
        "colours": {"Dark": DARK, "Glint": WHITE, "Top": (0.10, 0.20, 0.92), "Bottom": (0.10, 0.20, 0.92)},
        "unlit": ("Dark", "Glint", "Top", "Bottom"),
    },
    "Clown Eye Mix": {
        "kind": "Clown",
        "colours": {"Dark": DARK, "Glint": WHITE, "Top": (0.90, 0.10, 0.10), "Bottom": (0.10, 0.20, 0.92)},
        "unlit": ("Dark", "Glint", "Top", "Bottom"),
    },
    "Teary": {
        "kind": "Teary",
        "colours": {"Dark": DARK, "White": WHITE},
        "unlit": ("Dark", "White"),
    },
    "Round Tear": {
        "kind": "RoundTear",
        "colours": {"Dark": DARK, "White": WHITE, "Tear": TEAR_BLUE},
        "unlit": ("Dark", "White", "Tear"),
    },
    "Round Crying": {
        "kind": "RoundCrying",
        "colours": {"Dark": DARK, "White": WHITE, "Tear": TEAR_BLUE, "Shine": (0.86, 0.94, 0.99)},
        "unlit": ("Dark", "White", "Tear", "Shine"),
    },
    "Closed Eye Crying": {
        "kind": "ClosedEyeCrying",
        "colours": {"Dark": DARK, "Tear": (0.58, 0.79, 0.95)},
        "unlit": ("Dark", "Tear"),
    },
    "Dead": {
        "kind": "Dead",
        "colours": {"Dark": DARK},
        "unlit": ("Dark",),
    },
    "Heart": {
        "kind": "Heart",
        "colours": {"Dark": DARK, "Heart": (0.90, 0.10, 0.12), "Glint": WHITE},
        "unlit": ("Dark", "Heart", "Glint"),
    },
    "Flat": {
        "kind": "FlatEye",
        "colours": {"Dark": DARK},
        "unlit": ("Dark",),
    },
    # Goggles: CHOG #162 and #1551 (MOG: a thin dark frame, cyan, yellow, orange, red across), #4
    # (Chog Viper: a light grey frame, blue and cyan; its CHOG lettering is not drawn).
    "MOG": {
        "kind": "Goggles",
        "colours": {"Frame": (0.14, 0.13, 0.15), "Cyan": (0.22, 0.80, 0.90), "Yellow": (0.98, 0.80, 0.20),
                    "Orange": (0.97, 0.52, 0.12), "Red": (0.90, 0.20, 0.10)},
        "unlit": ("Cyan", "Yellow", "Orange", "Red"),
        "numbers": {"PIECES": ("Cyan", "Yellow", "Orange", "Red", "Orange", "Yellow", "Cyan"), "RIM": (0.008, 0.008)},
    },
    "Chog Viper": {
        "kind": "Goggles",
        "colours": {"Frame": (0.84, 0.86, 0.90), "Deep": (0.10, 0.18, 0.78), "Blue": (0.12, 0.42, 0.92),
                    "Cyan": (0.30, 0.78, 0.96), "Light": (0.62, 0.92, 0.98)},
        "unlit": ("Deep", "Blue", "Cyan", "Light"),
        "numbers": {"PIECES": ("Deep", "Blue", "Cyan", "Light", "Cyan", "Blue", "Deep")},
    },
    # Headsets: CHOG #491 (Modern VR: a black lens in a glowing cyan rim, a curved glint) and #46,
    # #191 (Classic VR: a thick grey frame, a black lens, glare stripes).
    "Modern VR": {
        "kind": "Headset",
        "colours": {"Frame": (0.18, 0.74, 0.96), "Lens": (0.04, 0.04, 0.07), "Glare": (0.92, 0.95, 0.98)},
        "unlit": ("Frame", "Lens", "Glare"),
        # Taller than the headset of "Classic VR": on the pictures it covers the face from the brow to
        # the cheeks. (First: the height of the other headset, a thin band on the bust.)
        "numbers": {"RIM": (0.010, 0.010), "GLINT": (0.10, 0.34, 0.62, 0.06), "HALF_HEIGHT": (0.080, 0.068)},
    },
    "Classic VR": {
        "kind": "Headset",
        "colours": {"Frame": (0.50, 0.50, 0.54), "Lens": (0.04, 0.04, 0.05), "Glare": (0.86, 0.86, 0.88)},
        "unlit": ("Lens", "Glare"),
        "numbers": {"STRIPES": ((0.52, 0.42, 0.035), (0.66, 0.56, 0.045), (0.80, 0.70, 0.030))},
    },
    "Retro Glass": {
        "kind": "RetroGlass",
        "colours": {"Frame": (0.82, 0.12, 0.16), "Lens": (0.68, 0.85, 0.93), "Iris": (0.38, 0.60, 0.74),
                    "Pupil": (0.66, 0.87, 0.96), "Lid": (0.16, 0.20, 0.30), "Glint": WHITE},   # "Pupil": the pale highlight
        "unlit": ("Lens", "Iris", "Pupil", "Lid", "Glint"),
        "numbers": RETRO_GLASS_NUMBERS,
    },
    # Added 15 October 2026: the eyes of Side=Left that had no part. Colours read on the 1000-point
    # pictures named at the tables of their kinds.
    "Angry": {"kind": "Angry", "colours": {"Dark": DARK, "White": WHITE, "Glint": WHITE},
              "unlit": ("Dark", "White", "Glint")},
    "Vertical": {"kind": "Vertical", "colours": {"Dark": DARK, "White": WHITE, "Glint": WHITE},
                 "unlit": ("Dark", "White", "Glint")},
    "Smirk": {"kind": "Smirk", "colours": {"Dark": DARK, "White": WHITE, "Glint": WHITE},
              "unlit": ("Dark", "White", "Glint")},
    "Frog": {"kind": "Frog", "colours": {"Dark": DARK, "White": WHITE, "Glint": WHITE},
             "unlit": ("Dark", "White", "Glint")},
    "Retard": {"kind": "Retard", "colours": {"Dark": DARK, "White": WHITE, "Glint": WHITE},
               "unlit": ("Dark", "White", "Glint")},
    "Worried": {"kind": "WorriedEye", "colours": {"Dark": DARK, "White": WHITE, "Glint": WHITE},
                "unlit": ("Dark", "White", "Glint")},
    "Base": {"kind": "BaseEye", "colours": {"Dark": DARK, "Glint": WHITE}, "unlit": ("Dark", "Glint")},
    "XD": {"kind": "XD", "colours": {"Dark": DARK}, "unlit": ("Dark",)},
    "3D Glass": {
        "kind": "Glasses3D",
        "colours": {"Frame": (0.82, 0.82, 0.84), "Lens": (0.16, 0.13, 0.86), "Red": (0.93, 0.20, 0.13)},
        "unlit": ("Lens", "Red"),
        "numbers": GLASSES_3D_NUMBERS,
    },
    "BTC Glass": {
        "kind": "CoinGlass",
        "colours": {"Frame": (0.80, 0.42, 0.08), "Lens": (0.95, 0.60, 0.16), "Print": (0.99, 0.97, 0.93)},
        "unlit": ("Print",),
        "numbers": COIN_GLASS_NUMBERS,
    },
    "Thug": {
        "kind": "PixelShades",
        "colours": {"Dark": (0.04, 0.04, 0.05), "White": (0.97, 0.97, 0.97)},
        "unlit": ("White",),
    },
}

EYES_OF_SIDE = {
    "Left": {
        # (22 October 2026, CHOG #925, #1430, #1207, #1405, the skull among them) the near eye, on the left of the
        # picture, is a long flat stroke whose right end is higher, the far eye a short arch: not the two equal arches of
        # Side=Right.
        "Happy": {
            "kind": "Happy", "colours": {"Dark": DARK}, "unlit": ("Dark",),
            # (22 October 2026, evening, the owner on #26: "there are still deformed eyes". A long flat stroke beside a short arch read as a
            # broken face from the front. Two equal arches, a little thicker, as on Side=Right.)
            "numbers": {"EYE_ARCS": None, "ARC": (0.052, 0.034, 0.020)},
        },
        # The lasers of Side=Left: lidded eyes, and beams of other colours (CHOG #893, #252, #565, #1658),
        # most of their sides the light core with an edge of the darker colour under it.
        "Purple laser": {
            "kind": "Laser", "eyes": "lidded",
            "colours": {"Dark": DARK, "Beam": (0.36, 0.34, 0.90), "Core": (0.95, 0.62, 0.93)},
            "unlit": ("Dark", "Beam", "Core"),
            "numbers": {"BEAM_CORE": (0, 1, 2, 3, 4), "BEAM_RADIUS": 0.019, "BEAM_PITCH_DEG": 8.0},
        },
        "fire laser": {
            "kind": "Laser", "eyes": "lidded",
            "colours": {"Dark": DARK, "Beam": (0.90, 0.16, 0.14), "Core": (0.76, 0.90, 0.28)},
            "unlit": ("Dark", "Beam", "Core"),
            "numbers": {"BEAM_CORE": (0, 1, 2, 3, 4), "BEAM_RADIUS": 0.019, "BEAM_PITCH_DEG": 8.0},
        },
        "cyan laser": {
            "kind": "Laser", "eyes": "lidded",
            "colours": {"Dark": DARK, "Beam": (0.14, 0.24, 0.62), "Core": (0.34, 0.84, 0.95)},
            "unlit": ("Dark", "Beam", "Core"),
            "numbers": {"BEAM_CORE": (0, 1, 2, 3, 4), "BEAM_RADIUS": 0.019, "BEAM_PITCH_DEG": 8.0},
        },
    },
    "Right": {
        # Worn on Side=Right only by skeletons (Base Skull), another drawing: dark sockets (SOCKET).
        "Flat": {"kind": "Socket", "colours": {"Dark": DARK, "White": WHITE}, "unlit": ("Dark", "White"),
                 "numbers": {"MARK": "dash"}},
        "Normal": {"kind": "Socket", "colours": {"Dark": DARK, "White": WHITE}, "unlit": ("Dark", "White")},
        "Up": {"kind": "Socket", "colours": {"Dark": DARK, "White": WHITE}, "unlit": ("Dark", "White"),
               "numbers": {"MARK": "glint"}},
        # Worn on Side=Right only by five skeletons: dark sockets cut by an angry brow. Until 17 October 2026
        # they wore the white eyes of "Angry" of Side=Left, which their pictures do not draw.
        "Angry": {"kind": "Socket", "colours": {"Dark": DARK, "White": WHITE}, "unlit": ("Dark", "White"),
                  "numbers": {"MARK": "angry"}},
    },
}

MOUTHS = {
    "Sad": {
        "kind": "Sad",
        "colours": {"Line": DARK},
        "unlit": ("Line",),
    },
    "Smile": {
        "kind": "SmileCurl",
        "colours": {"Line": DARK},
        "unlit": ("Line",),
    },
    "Worried": {
        "kind": "Worried",
        "colours": {"Line": DARK},
        "unlit": ("Line",),
    },
    "Lip Bite": {
        "kind": "LipBite",
        "colours": {"Line": DARK, "Teeth": WHITE},
        "unlit": ("Line", "Teeth"),
    },
    "Grit": {
        "kind": "Grit",
        "colours": {"Line": DARK, "Teeth": WHITE},
        "unlit": ("Line", "Teeth"),
    },
    "BTC Coin": {
        "kind": "Coin", "print": "bitcoin",
        "colours": {"Line": DARK, "Teeth": WHITE, "Coin": (0.96, 0.58, 0.14), "Edge": (0.82, 0.40, 0.08),
                    "Print": (0.99, 0.97, 0.93)},
        "unlit": ("Line", "Teeth", "Print"),
    },
    "Mon coin": {
        "kind": "Coin", "print": "monad",
        "colours": {"Line": DARK, "Teeth": WHITE, "Coin": (0.47, 0.36, 0.88), "Edge": (0.30, 0.20, 0.62),
                    "Print": (0.98, 0.97, 1.00)},
        "unlit": ("Line", "Teeth", "Print"),
    },
    "Clown Mouth": {
        "kind": "ClownMouth",
        "colours": {"Red": (0.88, 0.10, 0.10), "Line": DARK},
        "unlit": ("Red", "Line"),
    },
    "Flat": {
        "kind": "FlatMouth",
        "colours": {"Line": DARK},
        "unlit": ("Line",),
    },
}

MOUTHS_OF_SIDE = {
    # The bubble of the pictures of Side=Left is on the left of the character (the far cheek seen from
    # that side), the mirror of the pictures of Side=Right: the entry of make_chog_traits.py, moved.
    "Left": {
        "Bubblegum": {
            "kind": "Bubblegum",
            "colours": {"Gum": (0.95, 0.51, 0.92), "Glint": (0.97, 0.90, 0.97)},
            "unlit": ("Glint",),
            "numbers": {"AT": (0.020, -0.100)},
        },
        # Other drawings on Side=Left (17 October 2026, SMILE_LEFT and SAD_LEFT).
        "normal smile": {"kind": "SmileLeft", "colours": {"Line": DARK}, "unlit": ("Line",)},
        "Sad": {"kind": "SadLeft", "colours": {"Line": DARK}, "unlit": ("Line",)},
    },
    "Right": {},
}

EYE_KINDS = {
    "SideEye": build_side_eye, "Tired": build_tired, "Clown": build_clown, "Teary": build_teary,
    "RoundTear": build_round_tear, "RoundCrying": build_round_crying, "ClosedEyeCrying": build_closed_crying,
    "Dead": build_dead, "Heart": build_heart, "FlatEye": build_flat_eye, "Goggles": build_goggles,
    "Headset": build_headset, "RetroGlass": build_retro_glass, "Laser": build_laser,
    "Angry": build_white_eye(ANGRY), "Vertical": build_white_eye(VERTICAL), "Smirk": build_white_eye(SMIRK),
    "Frog": build_white_eye(FROG), "Retard": build_white_eye(RETARD), "WorriedEye": build_white_eye(WORRIED_EYE),
    "BaseEye": build_base_eye, "XD": build_xd, "Glasses3D": build_glasses_3d, "CoinGlass": build_coin_glass,
    "PixelShades": build_pixel_shades, "Socket": build_socket_eye,
}
MOUTH_KINDS = {
    "Sad": build_line_mouth(SAD), "SmileCurl": build_line_mouth(SMILE_CURL), "Worried": build_worried,
    "LipBite": build_lip_bite, "Grit": build_grit, "Coin": build_coin, "ClownMouth": build_clown_mouth,
    "FlatMouth": build_line_mouth(FLAT_MOUTH), "SmileLeft": build_line_mouth(SMILE_LEFT),
    "SadLeft": build_line_mouth(SAD_LEFT),
}


def by_side(*tables):
    """Several tables of values per Side merged into one, the later ones first."""
    merged = {}
    for table in tables:
        for side, values in table.items():
            merged.setdefault(side, {}).update(values)
    return merged


# ---------------------------------------------------------------------------------------------
# How many Genesis are right.
# ---------------------------------------------------------------------------------------------

def coverage(metadata, eyes, mouths, eye_kinds, mouth_kinds, of_side, skins):
    """Prints, for the tables given, how many Genesis have their eyes, their mouth, and both right: a
    Genesis on the base Origin, of a skin that is recorded, whose value of the trait (on its Side) has
    an entry whose kind is built. One without the trait counts as right for it (there is nothing to
    show). Returns the counts."""
    tokens = []
    with open(metadata, encoding="utf-8") as lines:
        for line in lines:
            if line.strip():
                record = json.loads(line)
                tokens.append({str(item["trait_type"]): str(item["value"])
                               for item in json.loads(record["raw_json"]).get("attributes", [])})

    def entry(trait, table, traits):
        value = traits.get(trait)
        if value is None:
            return "none"
        found = of_side.get(trait, {}).get(traits.get("Side"), {}).get(value) or table.get(value)
        return found

    def right(trait, table, kinds, traits):
        found = entry(trait, table, traits)
        return found == "none" or bool(found and found.get("kind") in kinds)

    dressable = [traits for traits in tokens if traits.get("Base") == "Origin" and traits.get("Skin") in skins]
    eyes_right = [traits for traits in dressable if right("Eyes", eyes, eye_kinds, traits)]
    mouth_right = [traits for traits in dressable if right("Mouth", mouths, mouth_kinds, traits)]
    both = [traits for traits in eyes_right if right("Mouth", mouths, mouth_kinds, traits)]
    with_eyes = sum(1 for traits in eyes_right if "Eyes" in traits)
    with_mouth = sum(1 for traits in mouth_right if "Mouth" in traits)
    print(f"CHOG_FACES {len(tokens)} Genesis, {len(dressable)} on the base Origin with a recorded skin")
    print(f"CHOG_FACES eyes right: {len(eyes_right)} of {len(tokens)} ({with_eyes} wear an Eyes value with a built part)")
    print(f"CHOG_FACES mouth right: {len(mouth_right)} of {len(tokens)} ({with_mouth} wear a Mouth value with a built "
          f"part, {len(mouth_right) - with_mouth} have no Mouth)")
    print(f"CHOG_FACES eyes and mouth both right: {len(both)} of {len(tokens)}")
    for trait, table, kinds in (("Eyes", eyes, eye_kinds), ("Mouth", mouths, mouth_kinds)):
        by_kind = {}
        missing = {}
        for traits in dressable:
            if trait not in traits:
                continue
            found = entry(trait, table, traits)
            name = f"{traits[trait]} / {traits.get('Side')}"
            if found and found.get("kind") in kinds:
                by_kind.setdefault(found["kind"], {}).setdefault(name, 0)
                by_kind[found["kind"]][name] += 1
            else:
                missing[name] = missing.get(name, 0) + 1
        for kind, values in sorted(by_kind.items(), key=lambda item: -sum(item[1].values())):
            print(f"CHOG_FACES {trait} kind {kind:16s} {sum(values.values()):4d}: "
                  + ", ".join(f"{name} {count}" for name, count in sorted(values.items(), key=lambda item: -item[1])))
        print(f"CHOG_FACES {trait} not built ({sum(missing.values())} Genesis on Origin): "
              + ", ".join(f"{name} {count}" for name, count in sorted(missing.items(), key=lambda item: -item[1])))
    return {"eyes": len(eyes_right), "mouth": len(mouth_right), "both": len(both), "all": len(tokens)}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    if here not in sys.path:
        sys.path.insert(0, here)
    sys.dont_write_bytecode = True
    import make_chog_traits as traits     # noqa: E402  (registers this module's tables on import)
    eyes = traits.TRAIT_PARTS["Eyes"]
    mouths = traits.TRAIT_PARTS["Mouth"]
    coverage(traits.METADATA, eyes[1], mouths[1], eyes[3], mouths[3], traits.OF_SIDE, traits.SKINS)


if __name__ == "__main__":
    main()
