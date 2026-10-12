"""Renders the Chog body made by make_chog_body.py, to judge it without opening Blender or Unity.

Run it without the interface of Blender (5.2), after make_chog_body.py:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/render_chog_body.py -- [--colour green] [--size 800] [--poses]
        [--name chog_body] [--out FOLDER] [--picture IMAGE]

It opens .artifacts/chog-body/ChogBody.blend (never saves it) and writes PNG files beside it:

    chog_body_front.png               straight from the front, no perspective
    chog_body_side.png                from its left side, no perspective
    chog_body_three_quarter.png       from the front and a little above
    chog_body_back.png                from behind and a little above: the spikes
    chog_body_game.png                the angle of the game: 73 degrees above the horizon, slightly behind
    chog_body_game_head.png           the same, with a round picture where the game puts the image of
                                      the Genesis, at the sizes of GenesisLook.Default
    chog_body_game_front_head.png     the angle of the game when the hero comes towards the camera
    chog_body_three_quarter_head.png  the whole character from the front (the game never looks from there)
    chog_body_sheet.png               the eight pictures above on one sheet, in that order
    chog_body_poses.png               with --poses: stands, runs, attacks, wins, loses, each seen from
                                      the front three-quarter (top row) and from the game angle with
                                      a head (bottom row); the single pictures go to poses/

The body is one flat colour on a plain mid-grey ground. The neck stub ("Head") is shown, except in the
pictures with a head, where it is hidden as the game hides it. Those are mock-ups made here from the
numbers of the game, not captures of the game.
"""

import argparse
import math
import os
import sys

import bmesh
import bpy
import numpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
DEFAULT_FOLDER = os.path.join(REPOSITORY, ".artifacts", "chog-body")
DEFAULT_PICTURE = os.path.join(REPOSITORY, ".artifacts", "genesis-art", "256", "1795.jpg")

# Colours are given as they look on the screen (sRGB), then turned into what Blender computes with.
COLOURS = {
    "green": (0.36, 0.78, 0.33),
    "purple": (0.51, 0.35, 0.86),
    "cream": (0.93, 0.88, 0.78),
    "red": (0.86, 0.22, 0.20),
}
GROUND_COLOUR = (0.5, 0.5, 0.5)
SKY_COLOUR = (0.62, 0.62, 0.62)
SKY_LIGHT = 0.30             # light from all around
KEY_LIGHT = 2.7              # the sun, above the left shoulder of whoever looks
FILL_LIGHT = 0.25            # a weaker sun from the other side, without shadows

# The angle of the camera of the game, above the horizon (game/README.md).
GAME_ELEVATION_DEG = 73.0
# GenesisLook.Default in game/Assets/Chog/Match/GenesisAppearance.cs, in world units, and the scale
# the game draws a body at (0.85 for every model, times BodyScale). The renders show the model at
# scale 1, so the picture is divided by that scale.
FACE_SIZE = 1.2
FACE_LIFT = 0.55
FACE_TOWARDS_CAMERA = 0.9
BODY_DRAWN_SCALE = 0.85 * 1.55

LENS_MM = 70.0

# name: (turn around the model in degrees, 0 is in front of it and 180 behind it; degrees above the
# horizon; without perspective; height the camera looks at; width of what it frames; with a head).
# The camera of the game is some 30 units away from a hero about 1.6 units tall (Main Camera of
# GameEssentials.prefab: 30 up, 7 back, a field of 35 degrees), so the game angle is drawn without
# perspective: at that distance there is next to none.
VIEWS = {
    "front": (0.0, 4.0, True, 0.58, 1.9, False),
    "side": (90.0, 4.0, True, 0.58, 1.9, False),
    "three_quarter": (35.0, 18.0, False, 0.58, 1.9, False),
    "back": (150.0, 24.0, False, 0.58, 1.9, False),
    "game": (160.0, GAME_ELEVATION_DEG, True, 0.58, 1.9, False),
    "game_head": (160.0, GAME_ELEVATION_DEG, True, 1.25, 2.3, True),
    "game_front_head": (20.0, GAME_ELEVATION_DEG, True, 1.25, 2.3, True),
    "three_quarter_head": (35.0, 18.0, False, 0.92, 2.5, True),
}
SHEET_COLUMNS = 4
POSES = ("stands", "runs", "attacks", "wins", "loses")
POSE_VIEWS = {
    "three_quarter": (35.0, 18.0, False, 0.62, 2.3, False),
    "game_head": (25.0, GAME_ELEVATION_DEG, True, 1.25, 2.7, True),
}

# From the axes of Unity (x right, y up, z ahead) to the axes of the model here (the Chog looks
# along -Y, +Z is up, its right hand is at -X).
UNITY_TO_BLENDER = Matrix(((-1.0, 0.0, 0.0), (0.0, 0.0, -1.0), (0.0, 1.0, 0.0)))


def linear(colour):
    """A colour of the screen (sRGB) as the numbers Blender computes with."""
    def channel(value):
        return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4
    return tuple(channel(value) for value in colour[:3]) + (1.0,)


def parse_colour(text):
    if text in COLOURS:
        return COLOURS[text]
    text = text.lstrip("#")
    if len(text) != 6:
        raise SystemExit(f"CHOG_RENDER unknown colour {text!r}: use one of {sorted(COLOURS)} or RRGGBB")
    return tuple(int(text[index:index + 2], 16) / 255.0 for index in (0, 2, 4))


# ---------------------------------------------------------------------------------------------
# Scene: materials, ground, light, camera.
# ---------------------------------------------------------------------------------------------

def node_tree(block):
    """The nodes of a material or of a sky; older versions of Blender make them only when asked."""
    if block.node_tree is None:
        block.use_nodes = True
    return block.node_tree


def plain_material(name, colour, roughness):
    material = bpy.data.materials.new(name)
    material.diffuse_color = linear(colour)
    shader = node_tree(material).nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = linear(colour)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = 0.0
    return material


def picture_material(name, path):
    """A picture drawn as it is, whatever the light: the image of the Genesis in the game is unlit."""
    material = bpy.data.materials.new(name)
    nodes, links = node_tree(material).nodes, node_tree(material).links
    nodes.clear()
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = bpy.data.images.load(path)
    emission = nodes.new("ShaderNodeEmission")
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(texture.outputs["Color"], emission.inputs["Color"])
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def set_engine(scene):
    """Eevee, which renders without a window on this Mac. Blender 4.2 to 4.5 gave it another name."""
    names = ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT")
    for name in names:
        try:
            scene.render.engine = name
            return name
        except TypeError:
            continue
    raise SystemExit(f"CHOG_RENDER this Blender has no engine among {names}")


def prepare_scene(size, body_colour):
    scene = bpy.context.scene
    used = set_engine(scene)
    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.film_transparent = False
    # Colours as given, without the film look that greys them.
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"

    if hasattr(scene.eevee, "taa_render_samples"):
        scene.eevee.taa_render_samples = 48

    # The sky is one grey to the eye and a weaker white light on the model, so shapes keep a shade.
    world = bpy.data.worlds.new("ChogRenderSky")
    nodes, links = node_tree(world).nodes, node_tree(world).links
    nodes.clear()
    seen = nodes.new("ShaderNodeBackground")
    seen.inputs["Color"].default_value = linear(SKY_COLOUR)
    seen.inputs["Strength"].default_value = 1.0
    lighting = nodes.new("ShaderNodeBackground")
    lighting.inputs["Color"].default_value = (1.0, 1.0, 1.0, 1.0)
    lighting.inputs["Strength"].default_value = SKY_LIGHT
    which = nodes.new("ShaderNodeLightPath")
    mix = nodes.new("ShaderNodeMixShader")
    output = nodes.new("ShaderNodeOutputWorld")
    links.new(which.outputs["Is Camera Ray"], mix.inputs["Fac"])
    links.new(lighting.outputs["Background"], mix.inputs[1])
    links.new(seen.outputs["Background"], mix.inputs[2])
    links.new(mix.outputs["Shader"], output.inputs["Surface"])
    scene.world = world

    ground_mesh = bpy.data.meshes.new("ChogRenderGround")
    shapes = bmesh.new()
    bmesh.ops.create_grid(shapes, x_segments=1, y_segments=1, size=40.0)
    shapes.to_mesh(ground_mesh)
    shapes.free()
    ground_mesh.materials.append(plain_material("ChogRenderGround", GROUND_COLOUR, 1.0))
    ground = bpy.data.objects.new("ChogRenderGround", ground_mesh)
    scene.collection.objects.link(ground)

    skin = plain_material("ChogRenderSkin", body_colour, 0.75)
    parts = {}
    for name in ("Body", "Head", "armLeft", "armRight"):
        part = bpy.data.objects.get(name)
        if part is None:
            raise SystemExit(f"CHOG_RENDER the file has no part named {name}")
        part.data.materials.clear()
        part.data.materials.append(skin)
        parts[name] = part

    camera_data = bpy.data.cameras.new("ChogRenderCamera")
    camera = bpy.data.objects.new("ChogRenderCamera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera

    key_data = bpy.data.lights.new("ChogRenderKey", "SUN")
    key_data.energy = KEY_LIGHT
    key_data.angle = math.radians(6.0)
    key = bpy.data.objects.new("ChogRenderKey", key_data)
    scene.collection.objects.link(key)

    fill_data = bpy.data.lights.new("ChogRenderFill", "SUN")
    fill_data.energy = FILL_LIGHT
    fill_data.use_shadow = False
    fill = bpy.data.objects.new("ChogRenderFill", fill_data)
    scene.collection.objects.link(fill)
    return used, parts, camera, (key, fill)


def aim(thing, position, target):
    """Places a camera or a light at "position", looking at "target"."""
    thing.location = position
    thing.rotation_euler = (target - position).to_track_quat("-Z", "Y").to_euler()


def place_camera(camera, lights, view):
    turn_deg, elevation_deg, flat, height, frame, _ = view
    turn, elevation = math.radians(turn_deg), math.radians(elevation_deg)
    away = Vector((math.sin(turn) * math.cos(elevation), -math.cos(turn) * math.cos(elevation),
                   math.sin(elevation)))
    target = Vector((0.0, 0.0, height))
    data = camera.data
    if flat:
        data.type = "ORTHO"
        data.ortho_scale = frame
        distance = 12.0
    else:
        data.type = "PERSP"
        data.lens = LENS_MM
        data.sensor_width = 36.0
        distance = frame * 0.5 / (18.0 / LENS_MM)
    data.clip_start, data.clip_end = 0.05, 200.0
    aim(camera, target + away * distance, target)
    bpy.context.view_layer.update()

    # The light comes from above the left shoulder of whoever looks, so every view is lit alike.
    key, fill = lights
    right = camera.matrix_world.to_3x3() @ Vector((1.0, 0.0, 0.0))
    flat_away = Vector((away.x, away.y, 0.0))
    flat_away = flat_away.normalized() if flat_away.length > 1e-6 else Vector((0.0, -1.0, 0.0))
    aim(key, target + (flat_away * 0.9 - right * 0.9 + Vector((0.0, 0.0, 1.5))) * 5.0, target)
    aim(fill, target + (flat_away * 0.6 + right * 1.2 + Vector((0.0, 0.0, 0.5))) * 5.0, target)
    bpy.context.view_layer.update()


def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"CHOG_RENDER wrote {path}")


# ---------------------------------------------------------------------------------------------
# The picture the game shows as the head.
# ---------------------------------------------------------------------------------------------

def add_face(picture):
    mesh = bpy.data.meshes.new("ChogRenderFace")
    shapes = bmesh.new()
    shapes.loops.layers.uv.verify()
    bmesh.ops.create_circle(shapes, cap_ends=True, segments=48, radius=0.5, calc_uvs=True)
    shapes.to_mesh(mesh)
    shapes.free()
    mesh.materials.append(picture_material("ChogRenderFace", picture))
    face = bpy.data.objects.new("ChogRenderFace", mesh)
    bpy.context.scene.collection.objects.link(face)
    if hasattr(face, "visible_shadow"):
        face.visible_shadow = False
    return face


def place_face(face, camera, neck):
    """As GenesisAppearance.LateUpdate does: turned to the camera, raised up the screen."""
    turn = camera.matrix_world.to_3x3()
    up, forward = turn @ Vector((0.0, 1.0, 0.0)), turn @ Vector((0.0, 0.0, -1.0))
    face.rotation_euler = camera.matrix_world.to_euler()
    face.scale = (FACE_SIZE / BODY_DRAWN_SCALE,) * 3
    face.location = neck + (up * FACE_LIFT - forward * FACE_TOWARDS_CAMERA) / BODY_DRAWN_SCALE


# ---------------------------------------------------------------------------------------------
# Poses. A copy, for these pictures only, of ChogBodyAnimator.Pose (game/Assets/Chog/Match): the
# game is the truth. Angles in degrees about the axes of Unity, offsets in units of the model.
# ---------------------------------------------------------------------------------------------

def pose_numbers(name):
    cycle, run, strike, mood = {
        "stands": (0.0, 0.0, 0.0, 0.0),
        "runs": (math.pi * 0.5, 1.0, 0.0, 0.0),     # the arms at the end of their swing
        "attacks": (0.0, 0.0, 0.25, 0.0),           # the arms thrown the furthest
        "wins": (0.0, 0.0, 0.0, 1.0),
        "loses": (0.0, 0.0, 0.0, -1.0),
    }[name]
    swing = math.sin(cycle)
    thrown = 0.0 if strike <= 0.0 or strike >= 1.0 else math.sin(math.pi * math.sqrt(strike))
    cheer, slump = max(0.0, min(1.0, mood)), max(0.0, min(1.0, -mood))
    hop = 0.0
    forward = -100.0 * thrown
    return {
        "body_offset": (0.0, abs(swing) * 0.07 * run + hop * 0.14 - slump * 0.1, thrown * 0.12),
        "body_turn": (12.0 * run + 16.0 * thrown + 28.0 * slump, 0.0, swing * 5.0 * run),
        "head_turn": (-6.0 * run - 8.0 * thrown + 30.0 * slump, 0.0, -swing * 3.0 * run),
        "arm_left_turn": (swing * 55.0 * run + forward + 12.0 * slump, 0.0, -150.0 * cheer + hop * 12.0),
        "arm_right_turn": (-swing * 55.0 * run + forward + 12.0 * slump, 0.0, 150.0 * cheer - hop * 12.0),
    }


def unity_turn(degrees):
    """Quaternion.Euler(x, y, z) of Unity (about Z, then X, then Y) as a turn in the axes used here."""
    x, y, z = (math.radians(value) for value in degrees)
    turn = Matrix.Rotation(y, 3, "Y") @ Matrix.Rotation(x, 3, "X") @ Matrix.Rotation(z, 3, "Z")
    return (UNITY_TO_BLENDER @ turn @ UNITY_TO_BLENDER.inverted()).to_euler()


def hang_under_body(parts):
    """The game puts the head and the arms under the body, without moving them."""
    body = parts["Body"]
    bpy.context.view_layer.update()
    for name in ("Head", "armLeft", "armRight"):
        part = parts[name]
        world = part.matrix_world.copy()
        part.parent = body
        part.matrix_parent_inverse = body.matrix_world.inverted()
        part.matrix_world = world
    bpy.context.view_layer.update()


def apply_pose(parts, name):
    numbers = pose_numbers(name)
    body = parts["Body"]
    body.location = UNITY_TO_BLENDER @ Vector(numbers["body_offset"])
    body.rotation_euler = unity_turn(numbers["body_turn"])
    parts["Head"].rotation_euler = unity_turn(numbers["head_turn"])
    parts["armLeft"].rotation_euler = unity_turn(numbers["arm_left_turn"])
    parts["armRight"].rotation_euler = unity_turn(numbers["arm_right_turn"])
    bpy.context.view_layer.update()


# ---------------------------------------------------------------------------------------------
# Sheets: several pictures of the same size side by side.
# ---------------------------------------------------------------------------------------------

def read_pixels(path):
    image = bpy.data.images.load(path)
    width, height = image.size
    pixels = numpy.empty(width * height * 4, dtype=numpy.float32)
    image.pixels.foreach_get(pixels)
    bpy.data.images.remove(image)
    return pixels.reshape(height, width, 4)


def write_sheet(rows, path, tile):
    """"rows" is a list of rows of picture paths, the first row at the top."""
    columns = max(len(row) for row in rows)
    sheet = numpy.zeros((len(rows) * tile, columns * tile, 4), dtype=numpy.float32)
    sheet[..., 3] = 1.0
    for row_index, row in enumerate(rows):
        for column_index, picture in enumerate(row):
            pixels = read_pixels(picture)
            step = max(1, pixels.shape[0] // tile)
            # Each point of the sheet is the mean of the points it stands for.
            size = tile * step
            small = pixels[:size, :size].reshape(tile, step, tile, step, 4).mean(axis=(1, 3))
            # The rows of an image of Blender start at the bottom.
            bottom = (len(rows) - 1 - row_index) * tile
            left = column_index * tile
            sheet[bottom:bottom + tile, left:left + tile] = small
    image = bpy.data.images.new("ChogRenderSheet", width=columns * tile, height=len(rows) * tile, alpha=False)
    image.pixels.foreach_set(sheet.ravel())
    image.filepath_raw = path
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)
    print(f"CHOG_RENDER wrote {path}")


def main():
    parser = argparse.ArgumentParser(description="Renders the Chog body from several views.")
    parser.add_argument("--blend", default=os.path.join(DEFAULT_FOLDER, "ChogBody.blend"))
    parser.add_argument("--out", default=DEFAULT_FOLDER, help="folder for the pictures")
    parser.add_argument("--name", default="chog_body", help="what the names of the pictures start with")
    parser.add_argument("--colour", default="green", help=f"one of {sorted(COLOURS)} or RRGGBB")
    parser.add_argument("--size", type=int, default=800, help="width and height of a picture")
    parser.add_argument("--picture", default=DEFAULT_PICTURE, help="image shown as the head in the mock-ups")
    parser.add_argument("--poses", action="store_true", help="also render the sheet of the five poses")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(arguments.blend))
    folder = os.path.abspath(arguments.out)
    os.makedirs(folder, exist_ok=True)
    used, parts, camera, lights = prepare_scene(arguments.size, parse_colour(arguments.colour))
    print(f"CHOG_RENDER engine {used}, colour {arguments.colour}, {arguments.size} px")

    face = None
    if os.path.isfile(arguments.picture):
        face = add_face(arguments.picture)
    else:
        print(f"CHOG_RENDER no picture at {arguments.picture}: no head is drawn, the neck stub stays")

    def shoot(view, path):
        with_head = view[5] and face is not None
        place_camera(camera, lights, view)
        # With a head: the neck stub hidden, as the game hides it, and the picture in its place.
        parts["Head"].hide_render = with_head
        if face is not None:
            face.hide_render = not with_head
            place_face(face, camera, parts["Head"].matrix_world.translation.copy())
        render(path)

    written = []
    for name, view in VIEWS.items():
        if view[5] and face is None:
            continue
        written.append(os.path.join(folder, f"{arguments.name}_{name}.png"))
        shoot(view, written[-1])

    rows = [written[start:start + SHEET_COLUMNS] for start in range(0, len(written), SHEET_COLUMNS)]
    write_sheet(rows, os.path.join(folder, f"{arguments.name}_sheet.png"), arguments.size // 2)

    if arguments.poses:
        hang_under_body(parts)
        scratch = os.path.join(folder, "poses")
        os.makedirs(scratch, exist_ok=True)
        # The single pictures of an earlier run are taken away, so none of an older model stays.
        for old in os.listdir(scratch):
            if old.startswith(f"{arguments.name}_") and old.endswith(".png"):
                os.remove(os.path.join(scratch, old))
        rows = [[] for _ in POSE_VIEWS]
        for pose in POSES:
            apply_pose(parts, pose)
            for row, (view_name, view) in enumerate(POSE_VIEWS.items()):
                path = os.path.join(scratch, f"{arguments.name}_{pose}_{view_name}.png")
                shoot(view, path)
                rows[row].append(path)
        write_sheet(rows, os.path.join(folder, f"{arguments.name}_poses.png"), arguments.size // 2)


if __name__ == "__main__":
    main()
