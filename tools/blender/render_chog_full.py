"""Renders a complete Chog character made by make_chog_full.py: the studio views that show the head,
and the angle of the game at the size a phone shows a hero.

Run it without the interface of Blender (5.2), after make_chog_full.py:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/render_chog_full.py -- --style H1
        [--colours purple,red] [--size 504] [--only game|studio] [--ring red] [--face NUMBER]

The pictures go to .artifacts/chog-body/full/<style>/, for each colour:

    chog_<style>_<colour>_front.png             straight from the front, on plain grey
    chog_<style>_<colour>_three_quarter.png     from the front and a little above
    chog_<style>_<colour>_back.png              straight from behind
    chog_<style>_<colour>_game_front.png        the angle of the game (73 degrees above the horizon) when
                                                the hero comes towards the camera, on the dark bricks
    chog_<style>_<colour>_game.png              the same seen slightly from behind
    chog_<style>_<colour>_game_front_phone.png  the two above at the size of a phone screen: the
    chog_<style>_<colour>_game_phone.png        character is then about 70 points tall

With "--face NUMBER" the cream face carries the face of that Genesis in place of the drawn eyes,
cheeks, nose, mouth and teeth of hair on the brow, and the names are chog_<style>_face<NUMBER>_front.png, _three_quarter.png,
_game_front.png and _game_front_phone.png. That is a mock-up to judge whether a Genesis stays
recognisable on a head in relief. How the face is cut: FACE_CROPS below gives, for each Genesis, one
rectangle of its 256-point portrait, chosen by eye to hold the brim of the hat, both eyes, the
cheeks and the mouth. That rectangle is laid flat on the face of the head, seen along the line the
face looks, and what falls beyond it repeats its edge. The portraits show a Chog turned three
quarters, so the face laid on the head is turned too: nothing redraws it from the front.

What is not the game: the floor is a drawing of bricks in the colours measured on a phone
screenshot, the green disc under the red hero is the one the game draws under the player's own hero,
and the light is the one of render_chog_body.py. The size is a choice: the phone pictures are framed
at PHONE_POINTS_PER_UNIT points for one unit of the model, at which the character is about 70 points
tall from the angle of the game (the exact height is printed for each picture). The game draws the
present body at 46 points for one unit (flat.PHONE_POINTS_PER_UNIT, measured on a phone screenshot),
so it would have to draw this model larger than it draws the present one, by the ratio of the two
numbers, for the pictures to be true. Nothing here was imported in Unity.
"""

import argparse
import math
import os
import sys

import bpy
import numpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import make_chog_full as full  # noqa: E402  (the folder of this file must be on the path first)
import render_chog_body_variants as flat  # noqa: E402

render = flat.render

FULL_FOLDER = os.path.join(render.DEFAULT_FOLDER, "full")

# The colours of the official drawings of Chog (.artifacts/chog-reference/): the cream of face.png,
# the pink of the cheeks and of the gloves, the near black of the eyes.
FACE_COLOUR = (0.99, 0.96, 0.82)
EYE_COLOUR = (0.07, 0.05, 0.10)
BLUSH_COLOUR = (0.88, 0.27, 0.50)
SKINS = dict(render.COLOURS, pink=(0.94, 0.47, 0.70))

# The frame of the pictures, in units of the model. The studio views hold the whole character, crown
# included. A game view is PHONE_POINTS points across on a phone.
STUDIO_FRAME = 1.85
STUDIO_HEIGHT = 0.72
GAME_FRAME = 1.80
GAME_HEIGHT = 0.66
PHONE_POINTS = 126
PHONE_POINTS_PER_UNIT = PHONE_POINTS / GAME_FRAME

# name: (turn around the model, degrees above the horizon, without perspective, height looked at,
# width framed, unused), as in render_chog_body.py.
VIEWS = {
    "front": (0.0, 4.0, True, STUDIO_HEIGHT, STUDIO_FRAME, False),
    "three_quarter": (35.0, 18.0, False, STUDIO_HEIGHT, STUDIO_FRAME, False),
    "back": (180.0, 4.0, True, STUDIO_HEIGHT, STUDIO_FRAME, False),
    "game_front": (20.0, render.GAME_ELEVATION_DEG, True, GAME_HEIGHT, GAME_FRAME, False),
    "game": (160.0, render.GAME_ELEVATION_DEG, True, GAME_HEIGHT, GAME_FRAME, False),
}
STUDIO_VIEWS = ("front", "three_quarter", "back")
GAME_VIEWS = ("game_front", "game")
FACE_VIEWS = ("front", "three_quarter", "game_front")

# For the mock-up of a Genesis face on the head: the rectangle of its 256-point portrait laid on the
# face (left, top, right, bottom, counted from the top left corner), and the skin colour shown with
# it, read on the spikes of the portrait.
FACE_CROPS = {
    "1795": ((56, 44, 256, 204), "pink"),
    "408": ((0, 10, 256, 215), "purple"),
}
PORTRAIT_SIDE = 256.0


def tones(skin):
    return {
        "ChogSkin": skin,
        "ChogFace": FACE_COLOUR,
        "ChogGlove": flat.GLOVE_COLOUR,
        "ChogEye": EYE_COLOUR,
        "ChogBlush": BLUSH_COLOUR,
        "ChogOutline": flat.OUTLINE_COLOUR,
    }


def nothing_material():
    """What is drawn in it is not seen: for the eyes and cheeks of the model under a Genesis face."""
    material = bpy.data.materials.new("ChogRenderNothing")
    nodes, links = render.node_tree(material).nodes, render.node_tree(material).links
    nodes.clear()
    clear = nodes.new("ShaderNodeBsdfTransparent")
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(clear.outputs["BSDF"], output.inputs["Surface"])
    return material


def genesis_face_material(token, head_numbers, head_part):
    """The face of a Genesis laid flat on the face of the head, seen along the line the face looks."""
    box, _ = FACE_CROPS[token]
    picture = os.path.join(flat.ART_FOLDER, "256", f"{token}.jpg")
    if not os.path.isfile(picture):
        raise SystemExit(f"CHOG_RENDER no portrait at {picture}")

    # Axes that sit on the middle of the face and are turned up with the head: across the face is
    # their X, up the face their Z.
    turn = Matrix.Rotation(-math.radians(head_numbers.HEAD_PITCH_DEG), 3, "X")
    middle = Vector((head_numbers.FACE_MIDDLE[0], 0.0, head_numbers.FACE_MIDDLE[1]))
    axes = bpy.data.objects.new("ChogRenderFaceAxes", None)
    bpy.context.scene.collection.objects.link(axes)
    axes.location = head_part.matrix_world @ (full.Z * head_numbers.HEAD_MIDDLE + turn @ middle)
    axes.rotation_euler = turn.to_euler()

    # The face of the head, brow included, is this wide and this tall.
    width = 2.0 * head_numbers.FACE_HALF_WIDTH * (1.0 + head_numbers.FACE_TAPER)
    height = 2.0 * head_numbers.FACE_HALF_HEIGHT
    left, top, right, bottom = (value / PORTRAIT_SIDE for value in box)
    low, high = 1.0 - bottom, 1.0 - top          # the rows of an image are counted from the bottom

    material = bpy.data.materials.new("ChogRenderGenesisFace")
    tree = render.node_tree(material)
    nodes, links = tree.nodes, tree.links
    shader = nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = 0.75
    shader.inputs["Metallic"].default_value = 0.0
    where = nodes.new("ShaderNodeTexCoord")
    where.object = axes
    apart = nodes.new("ShaderNodeSeparateXYZ")
    together = nodes.new("ShaderNodeCombineXYZ")
    place = nodes.new("ShaderNodeMapping")
    place.vector_type = "POINT"
    place.inputs["Scale"].default_value = ((right - left) / width, (high - low) / height, 1.0)
    place.inputs["Location"].default_value = ((left + right) * 0.5, (low + high) * 0.5, 0.0)
    image = nodes.new("ShaderNodeTexImage")
    image.image = bpy.data.images.load(picture)
    image.extension = "EXTEND"
    links.new(where.outputs["Object"], apart.inputs["Vector"])
    links.new(apart.outputs["X"], together.inputs["X"])
    links.new(apart.outputs["Z"], together.inputs["Y"])
    links.new(together.outputs["Vector"], place.inputs["Vector"])
    links.new(place.outputs["Vector"], image.inputs["Vector"])
    links.new(image.outputs["Color"], shader.inputs["Base Color"])
    return material


def hide_fringe(head, nothing):
    """The teeth of hair on the brow are the only lone triangles of the head (no point of theirs
    belongs to another face). Under the face of a Genesis, which brings its own hat or hair, they
    are not drawn. Returns how many were found."""
    mesh = head.data
    uses = numpy.zeros(len(mesh.vertices), dtype=numpy.int32)
    for face in mesh.polygons:
        for index in face.vertices:
            uses[index] += 1
    slot = next(index for index, material in enumerate(mesh.materials) if material.name == nothing.name)
    found = 0
    for face in mesh.polygons:
        if all(uses[index] == 1 for index in face.vertices):
            face.material_index = slot
            found += 1
    mesh.update()
    return found


def screen_height(parts, camera):
    """How tall the character is on the picture, in units of the model, from where the camera is."""
    up = camera.matrix_world.to_3x3() @ Vector((0.0, 1.0, 0.0))
    heights = [(part.matrix_world @ point.co).dot(up) for part in parts.values() for point in part.data.vertices]
    return max(heights) - min(heights)


def render_one(style, blend, folder, colour_name, size, options):
    """Every picture of one style in one skin colour, or with the face of one Genesis."""
    token = options.face
    bpy.ops.wm.open_mainfile(filepath=blend)
    remembered = flat.remember_materials(("Body", "Head", "armLeft", "armRight"))

    skin = SKINS[colour_name] if colour_name in SKINS else render.parse_colour(colour_name)
    used, parts, camera, lights = render.prepare_scene(size, skin)
    scene = bpy.context.scene
    scene.render.image_settings.compression = 90
    made = flat.dress(parts, remembered, tones(skin))

    if token:
        nothing = nothing_material()
        swap = {
            made["ChogFace"].name: genesis_face_material(token, full.head_numbers(style), parts["Head"]),
            made["ChogEye"].name: nothing,
            made["ChogBlush"].name: nothing,
        }
        materials = parts["Head"].data.materials
        for index in range(len(materials)):
            if materials[index].name in swap:
                materials[index] = swap[materials[index].name]
        print(f"CHOG_RENDER {hide_fringe(parts['Head'], nothing)} teeth of hair on the brow are not drawn")

    ground = bpy.data.objects["ChogRenderGround"]
    plain = ground.data.materials[0]
    axes = bpy.data.objects.new("ChogRenderFloorAxes", None)
    scene.collection.objects.link(axes)
    bricks = flat.brick_material(axes)
    ring = flat.add_ring() if colour_name in options.ring.split(",") and not token else []
    label = f"face{token}" if token else colour_name
    print(f"CHOG_RENDER {style} in {label}, engine {used}, {size} px, skin {colour_name}, "
          f"{'on the green disc' if ring else 'on the bare floor'}")

    def shoot(view_name, in_game):
        view = VIEWS[view_name]
        ground.data.materials[0] = bricks if in_game else plain
        for disc in ring:
            disc.hide_render = not in_game
        axes.location = (0.3, 0.37, 0.0)
        axes.rotation_euler = (0.0, 0.0, math.radians(view[0]))
        render.place_camera(camera, lights, view)
        if in_game:
            tall = screen_height(parts, camera)
            print(f"CHOG_RENDER {style} {view_name}: the character is {tall:.3f} units tall on the picture, "
                  f"{tall * PHONE_POINTS_PER_UNIT:.0f} points at the size of a phone")
        path = os.path.join(folder, f"chog_{style}_{label}_{view_name}.png")
        render.render(path)
        if in_game:
            flat.write_small(path, path[:-4] + "_phone.png", size // PHONE_POINTS)

    if token:
        for view_name in FACE_VIEWS:
            shoot(view_name, view_name in GAME_VIEWS)
        return
    if options.only in (None, "studio"):
        for view_name in STUDIO_VIEWS:
            shoot(view_name, False)
    if options.only in (None, "game"):
        for view_name in GAME_VIEWS:
            shoot(view_name, True)


def main():
    parser = argparse.ArgumentParser(description="Renders a complete Chog character.")
    parser.add_argument("--style", required=True, choices=sorted(full.STYLES))
    parser.add_argument("--colours", default=None, help="skin colours, separated by commas: "
                        f"{sorted(SKINS)} or RRGGBB (default: purple,red, or the skin that goes with --face)")
    parser.add_argument("--size", type=int, default=504, help="width and height of a large picture; brought down "
                        "to a whole number of times the phone size")
    parser.add_argument("--only", default=None, choices=("game", "studio"), help="one family of pictures")
    parser.add_argument("--ring", default="red", help="the colours shown on the green disc the game draws under "
                        "the player's own hero, separated by commas; the others stand on the bare floor")
    parser.add_argument("--face", default=None, choices=sorted(FACE_CROPS), help="mock-up: the cream face carries "
                        "the face of this Genesis")
    parser.add_argument("--blend", default=None, help="the file to render (default: the one of the style)")
    parser.add_argument("--out", default=None, help="folder for the pictures (default: the one of the style)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    style = arguments.style
    blend = os.path.abspath(arguments.blend) if arguments.blend else os.path.join(FULL_FOLDER, style, "ChogBody.blend")
    if not os.path.isfile(blend):
        raise SystemExit(f"CHOG_RENDER no file at {blend}: run make_chog_full.py first")
    folder = os.path.abspath(arguments.out or os.path.join(FULL_FOLDER, style))
    os.makedirs(folder, exist_ok=True)
    size = max(1, arguments.size // PHONE_POINTS) * PHONE_POINTS

    if arguments.colours:
        colours = [name.strip() for name in arguments.colours.split(",")]
    elif arguments.face:
        colours = [FACE_CROPS[arguments.face][1]]
    else:
        colours = ["purple", "red"]
    for colour_name in colours:
        render_one(style, blend, folder, colour_name, size, arguments)


if __name__ == "__main__":
    main()
