"""Renders a Chog made by make_chog_traits.py: a bust framed like the picture of its Genesis, the
studio views of the whole character, and the angle of the game at the size a phone shows a hero.

Run it without the interface of Blender (5.2), after make_chog_traits.py:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/render_chog_traits.py -- --name 1287
        [--colour red|RRGGBB] [--size 504] [--bust-size 1008] [--only bust,studio,game,towards,turnaround]
        [--blend FILE --out FOLDER]

"--name" is the folder under .artifacts/chog-body/traits/ that holds ChogBody.blend: the number of a
Genesis, or "base". The pictures go to the same folder:

    chog_<name>_bust.png               head and shoulders, from the side the picture of the Genesis
                                       shows, the camera tilted as that picture is, on one flat colour
                                       (the background colour of the Genesis; grey for the base), lit
                                       from the camera too, as the pictures are flat colours
    chog_<name>_front.png              the five views of render_chog_full.py, same frames, same light:
    chog_<name>_three_quarter.png      from the front, three quarter, from behind,
    chog_<name>_back.png
    chog_<name>_game_front.png         the angle of the game (73 degrees above the horizon) coming
    chog_<name>_game.png               towards the camera, and seen slightly from behind
    chog_<name>_game_front_phone.png   the two above at the size of a phone screen
    chog_<name>_game_phone.png

The skin, the cream of the face and the pink of the cheeks are those kept in the file by
make_chog_traits.py for a Genesis; the base takes the red and the face colours of
render_chog_full.py, so it can be set beside the pictures of H3. A trait part is drawn in the colours
of its materials; a material marked "unlit" (the white lenses, and everything that is drawn on the
face: eyes, mouths) is drawn in its colour whatever the light, a material marked "glass" (the helmet of the
astronaut) is drawn semi-transparent (glass_material), and a trait part casts no shadow.
Those two are choices of these pictures: a game shader would have to do the same for the game to
look like them. What is not the game is also what render_chog_full.py says: the floor, the green
disc, the light and the size of the phone pictures (the model drawn 1.5 times larger than the game
draws a body today) are choices of these mock-ups. Nothing here was imported in Unity.
"""

import argparse
import math
import os
import sys

import bpy
from mathutils import Quaternion, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import make_chog_traits as traits  # noqa: E402  (the folder of this file must be on the path first)
import render_chog_full as shots  # noqa: E402

render, flat = shots.render, shots.flat

MAIN_PARTS = ("Body", "Head", "armLeft", "armRight")

# The bust. The pictures of the collection show a Chog from close, three quarter, the whole picture
# tilted: on CHOG #1287 the brim, the grin and the line the letters stand on all run down to the
# right by 10 to 17 degrees. For "Side=Right" the camera stands at the front LEFT of the character:
# what is on its left is then nearer and larger, at the right of the picture (the M of TISM, the
# right lens, the gold tooth), the spikes of its far side stand out at the left of the picture, and
# beams leaving the eyes run to the left, as on the pictures. (First: the camera at the front right,
# read from "the spikes are at the left". The near leaves then filled the left of the picture as one
# red mass, and the letters shrank the wrong way.) "Side=Left" is the mirror of it. The camera is
# turned by BUST_TURN_DEG from straight ahead, above the horizon by BUST_ELEVATION_DEG (the head of
# H3 looks up by 20 degrees, so this is nearly face on), rolled by BUST_ROLL_DEG. It looks at
# BUST_TARGET (ahead of the character, above the ground) and frames BUST_FRAME units at that distance.
BUST_TURN_DEG = 22.0
BUST_ELEVATION_DEG = 20.0
BUST_ROLL_DEG = 17.0
BUST_TARGET = (0.17, 0.865)          # ahead of the character, above the ground
BUST_FRAME = 0.73
BUST_SHIFT = (-0.105, 0.06)          # then the frame is moved by this much to the right and up the picture
BUST_PLAIN = (0.62, 0.62, 0.62)      # behind the base, which has no Genesis
# The light of the bust: the pictures of the collection are flat colours, so the sun of the other
# views is weaker here and a second one shines from the camera, without shadows. The other views
# keep the light of render_chog_body.py, to be set beside the earlier pictures.
BUST_KEY_LIGHT = 1.7
BUST_FRONT_LIGHT = 1.1
# The turnaround (added 15 October 2026, after batch 4 was judged on one angle only): the head alone,
# without perspective, from eight turns around it at TURN_ELEVATION_DEG above the horizon (0 is the
# front, 90 the character's left, 180 the back), then from straight above, then from the game's angle
# behind and in front of it. What the pictures of the collection never show (the back, the other side,
# the top) is judged here: chog_<name>_turn_<turn>.png, chog_<name>_turn_top.png,
# chog_<name>_turn_game.png, chog_<name>_turn_game_front.png.
TURN_TURNS = (0, 45, 90, 135, 180, 225, 270, 315)
TURN_ELEVATION_DEG = 12.0
TURN_HEIGHT = 0.9
TURN_FRAME = 0.95
TURN_SIZE = 360


def gradient_skin(material, bottom, top, low=0.62, high=1.12):
    """Makes "material" run from "bottom" (at the height "low" above the ground) to "top" (at "high")."""
    tree = render.node_tree(material)
    nodes, links = tree.nodes, tree.links
    shader = nodes.get("Principled BSDF")
    position = nodes.new("ShaderNodeNewGeometry")
    split = nodes.new("ShaderNodeSeparateXYZ")
    ramp = nodes.new("ShaderNodeMapRange")
    ramp.inputs["From Min"].default_value = low
    ramp.inputs["From Max"].default_value = high
    ramp.clamp = True
    mix = nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.inputs[6].default_value = render.linear(bottom)
    mix.inputs[7].default_value = render.linear(top)
    links.new(position.outputs["Position"], split.inputs["Vector"])
    links.new(split.outputs["Z"], ramp.inputs["Value"])
    links.new(ramp.outputs["Result"], mix.inputs["Factor"])
    links.new(mix.outputs[2], shader.inputs["Base Color"])


def unlit_material(name, colour):
    """One flat colour whatever the light."""
    material = bpy.data.materials.new(name)
    nodes, links = render.node_tree(material).nodes, render.node_tree(material).links
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = render.linear(colour)
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def glass_material(name, colour):
    """The glass of a helmet as the game draws it (22 October 2026): the game keeps the pixels of a "glass" material
    that a fixed screen pattern lets through, from 0.3 of them where it faces the camera to 0.9 at its edges
    (coverage = lerp(0.9, 0.3, toward^2), toward being how much it faces the camera), and lightens its colour toward its
    edges (a share 0.6 * (1 - toward)^2 of white). Here the same two curves are a mix between a transparent shader and
    the colour, so the sheet shows how much of the head shows through the glass; hashed (dithered) transparency, so
    the two sides of the shell need no sorting. Only the renders do this: the game has no transparency."""
    material = bpy.data.materials.new(name)
    material.surface_render_method = "DITHERED"
    tree = render.node_tree(material)
    nodes, links = tree.nodes, tree.links
    nodes.clear()
    # Layer Weight "Facing" is 1 - |n . v| at a blend of 0.5: 0 face on, 1 at the edge. toward = 1 - facing.
    layer = nodes.new("ShaderNodeLayerWeight")
    layer.inputs["Blend"].default_value = 0.5
    edge = layer.outputs["Facing"]
    toward = nodes.new("ShaderNodeMath")
    toward.operation = "SUBTRACT"
    toward.inputs[0].default_value = 1.0
    links.new(edge, toward.inputs[1])
    squared = nodes.new("ShaderNodeMath")
    squared.operation = "MULTIPLY"
    links.new(toward.outputs["Value"], squared.inputs[0])
    links.new(toward.outputs["Value"], squared.inputs[1])
    # coverage = 0.9 - 0.6 * toward^2
    scaled = nodes.new("ShaderNodeMath")
    scaled.operation = "MULTIPLY_ADD"
    scaled.inputs[1].default_value = -0.6
    scaled.inputs[2].default_value = 0.9
    links.new(squared.outputs["Value"], scaled.inputs[0])
    # lightening = 0.6 * edge^2, towards white
    edge_squared = nodes.new("ShaderNodeMath")
    edge_squared.operation = "MULTIPLY"
    links.new(edge, edge_squared.inputs[0])
    links.new(edge, edge_squared.inputs[1])
    lighten = nodes.new("ShaderNodeMath")
    lighten.operation = "MULTIPLY"
    lighten.inputs[1].default_value = 0.6
    links.new(edge_squared.outputs["Value"], lighten.inputs[0])
    tint = nodes.new("ShaderNodeMix")
    tint.data_type = "RGBA"
    tint.inputs[6].default_value = render.linear(colour)
    tint.inputs[7].default_value = (1.0, 1.0, 1.0, 1.0)
    links.new(lighten.outputs["Value"], tint.inputs["Factor"])
    emission = nodes.new("ShaderNodeEmission")
    links.new(tint.outputs[2], emission.inputs["Color"])
    clear = nodes.new("ShaderNodeBsdfTransparent")
    mix = nodes.new("ShaderNodeMixShader")
    links.new(scaled.outputs["Value"], mix.inputs["Fac"])
    links.new(clear.outputs["BSDF"], mix.inputs[1])
    links.new(emission.outputs["Emission"], mix.inputs[2])
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(mix.outputs["Shader"], output.inputs["Surface"])
    return material


def dress_traits(outline):
    """Gives every trait part (the objects make_chog_traits.py marked with their trait) materials for
    the renders, in the colours its own materials keep. Returns the parts."""
    found = [thing for thing in bpy.data.objects if thing.type == "MESH" and "trait" in thing]
    made = {traits.OUTLINE: outline}
    for part in found:
        # A part casts no shadow: the brim of a hat would otherwise put the whole face in the dark,
        # where the pictures of the collection keep it in its flat colour.
        if hasattr(part, "visible_shadow"):
            part.visible_shadow = False
        materials = part.data.materials
        for index in range(len(materials)):
            name = materials[index].name
            if name not in made:
                colour = tuple(materials[index]["srgb"])
                if materials[index].get("glass"):
                    made[name] = glass_material("ChogRender" + name, colour)
                elif materials[index].get("unlit"):
                    made[name] = unlit_material("ChogRender" + name, colour)
                else:
                    made[name] = render.plain_material("ChogRender" + name, colour, 0.75)
            materials[index] = made[name]
    return found


def screen_height(things, camera):
    """How tall the character is on the picture, in units of the model, from where the camera is."""
    up = camera.matrix_world.to_3x3() @ Vector((0.0, 1.0, 0.0))
    heights = [(thing.matrix_world @ point.co).dot(up) for thing in things for point in thing.data.vertices]
    return max(heights) - min(heights)


def seen_sky(scene):
    """The node that gives the colour seen behind everything (render.prepare_scene makes it)."""
    nodes = scene.world.node_tree.nodes
    mix = next(node for node in nodes if node.bl_idname == "ShaderNodeMixShader")
    return mix.inputs[2].links[0].from_node


def place_bust(camera, lights, side):
    """The camera of the bust: on the side of the character its picture shows, tilted like it."""
    where = 1.0 if side == "Right" else -1.0
    ahead, height = BUST_TARGET
    render.place_camera(camera, lights, (where * BUST_TURN_DEG, BUST_ELEVATION_DEG, False, height, BUST_FRAME, False))
    roll = Quaternion((0.0, 0.0, 1.0), math.radians(where * BUST_ROLL_DEG))
    turned = camera.rotation_euler.to_quaternion() @ roll
    camera.rotation_euler = turned.to_euler()
    camera.location = (camera.location + Vector((0.0, -ahead, 0.0))
                       + turned @ Vector((where * BUST_SHIFT[0], BUST_SHIFT[1], 0.0)))
    bpy.context.view_layer.update()


def main():
    parser = argparse.ArgumentParser(description="Renders a Chog dressed with the traits of a Genesis.")
    parser.add_argument("--name", required=True, help="the folder under .artifacts/chog-body/traits/: the number "
                                                      "of a Genesis, or base")
    parser.add_argument("--colour", default=None, help="skin colour: a name or RRGGBB (default: the skin of the "
                                                       "Genesis, or red for the base)")
    parser.add_argument("--size", type=int, default=504, help="width and height of a large picture; brought down "
                        "to a whole number of times the phone size")
    parser.add_argument("--bust-size", type=int, default=1008, help="width and height of the bust")
    parser.add_argument("--only", default=None, help="some families of pictures, separated by commas: bust, studio, "
                        "game, or towards (the game angle coming towards the camera, alone)")
    parser.add_argument("--blend", default=None, help="the file to render (default: the one of the folder)")
    parser.add_argument("--out", default=None, help="folder for the pictures (default: the one of the file)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    render_one(arguments.name, arguments.blend, arguments.out, arguments.only, arguments.colour, arguments.size,
               arguments.bust_size)


def render_one(name, blend=None, out=None, only=None, colour=None, size=504, bust_size=1008):
    """Renders the pictures of one built Chog (see the top of this file). make_chog_traits_many.py calls
    it for many Genesis in one run of Blender."""
    folder = os.path.join(traits.TRAITS_FOLDER, name)
    blend = os.path.abspath(blend) if blend else os.path.join(folder, "ChogBody.blend")
    if not os.path.isfile(blend):
        raise SystemExit(f"CHOG_RENDER no file at {blend}: run make_chog_traits.py first")
    folder = os.path.abspath(out or folder)
    os.makedirs(folder, exist_ok=True)
    size = max(1, size // shots.PHONE_POINTS) * shots.PHONE_POINTS

    bpy.ops.wm.open_mainfile(filepath=blend)
    root = bpy.data.objects[traits.base.ROOT_NAME]
    genesis = root.get("chog_genesis")
    remembered = flat.remember_materials(MAIN_PARTS)

    if colour:
        skin = shots.SKINS[colour] if colour in shots.SKINS else render.parse_colour(colour)
    elif genesis:
        skin = tuple(root["chog_skin"])
    else:
        skin = shots.SKINS["red"]
    tones = shots.tones(skin)
    if genesis:
        tones["ChogFace"] = tuple(root["chog_face"])
        tones["ChogBlush"] = tuple(root["chog_blush"])
    # A material of a part of the base that keeps its own colour (the grey cheeks of a 1:1).
    for material in bpy.data.materials:
        if material.name not in tones and "srgb" in material:
            tones[material.name] = tuple(material["srgb"])
    background = tuple(root["chog_background"]) if genesis and "chog_background" in root else BUST_PLAIN
    side = root.get("chog_side", "Right")

    used, parts, camera, lights = render.prepare_scene(size, skin)
    if genesis and "chog_skin_top" in root:
        # A radioactive Genesis: its skin runs from its colour at the feet to a lighter one at the top.
        gradient_skin(bpy.data.materials["ChogRenderSkin"], skin, tuple(root["chog_skin_top"]))
    scene = bpy.context.scene
    scene.render.image_settings.compression = 90
    made = flat.dress(parts, remembered, tones)
    worn = dress_traits(made["ChogOutline"])
    everything = list(parts.values()) + worn

    ground = bpy.data.objects["ChogRenderGround"]
    plain = ground.data.materials[0]
    axes = bpy.data.objects.new("ChogRenderFloorAxes", None)
    scene.collection.objects.link(axes)
    bricks = flat.brick_material(axes)
    ring = flat.add_ring()
    sky = seen_sky(scene)
    grey = tuple(sky.inputs["Color"].default_value)
    said = f"CHOG #{genesis}" if genesis else "the base"
    print(f"CHOG_RENDER {name} ({said}), engine {used}, {size} px, skin {' '.join(f'{value:.2f}' for value in skin)}, "
          f"wearing {[part.name for part in worn]}")

    front_data = bpy.data.lights.new("ChogRenderFront", "SUN")
    front_data.energy = BUST_FRONT_LIGHT
    front_data.use_shadow = False
    front = bpy.data.objects.new("ChogRenderFront", front_data)
    scene.collection.objects.link(front)

    def shoot(view_name, in_game):
        view = shots.VIEWS[view_name]
        ground.hide_render = False
        front.hide_render = True
        lights[0].data.energy = render.KEY_LIGHT
        ground.data.materials[0] = bricks if in_game else plain
        sky.inputs["Color"].default_value = grey
        for disc in ring:
            disc.hide_render = not in_game
        axes.location = (0.3, 0.37, 0.0)
        axes.rotation_euler = (0.0, 0.0, math.radians(view[0]))
        scene.render.resolution_x = scene.render.resolution_y = size
        render.place_camera(camera, lights, view)
        if in_game:
            tall = screen_height(everything, camera)
            print(f"CHOG_RENDER {name} {view_name}: the character is {tall:.3f} units tall on the picture, "
                  f"{tall * shots.PHONE_POINTS_PER_UNIT:.0f} points at the size of a phone")
        path = os.path.join(folder, f"chog_{name}_{view_name}.png")
        render.render(path)
        if in_game:
            flat.write_small(path, path[:-4] + "_phone.png", size // shots.PHONE_POINTS)

    families = {"bust", "studio", "game", "towards", "turnaround"}
    wanted = set(only.split(",")) if only else {"bust", "studio", "game"}
    if wanted - families:
        raise SystemExit(f"CHOG_RENDER unknown families {sorted(wanted - families)}: use {sorted(families)}")
    if "bust" in wanted:
        # No floor and no shadow behind the bust: one flat colour, as the picture of a Genesis has.
        ground.hide_render = True
        for disc in ring:
            disc.hide_render = True
        sky.inputs["Color"].default_value = render.linear(background)
        scene.render.resolution_x = scene.render.resolution_y = bust_size
        place_bust(camera, lights, side)
        lights[0].data.energy = BUST_KEY_LIGHT
        front.hide_render = False
        front.rotation_euler = camera.rotation_euler
        render.render(os.path.join(folder, f"chog_{name}_bust.png"))
    if "studio" in wanted:
        for view_name in shots.STUDIO_VIEWS:
            shoot(view_name, False)
    for view_name in shots.GAME_VIEWS:
        if "game" in wanted or ("towards" in wanted and view_name == "game_front"):
            shoot(view_name, True)
    if "turnaround" in wanted:
        ground.hide_render = True
        for disc in ring:
            disc.hide_render = True
        front.hide_render = True
        lights[0].data.energy = render.KEY_LIGHT
        sky.inputs["Color"].default_value = grey
        scene.render.resolution_x = scene.render.resolution_y = TURN_SIZE
        turns = [(f"turn_{turn:03d}", turn, TURN_ELEVATION_DEG) for turn in TURN_TURNS]
        turns += [("turn_top", 180.0, 89.0), ("turn_game", 160.0, render.GAME_ELEVATION_DEG),
                  ("turn_game_front", 20.0, render.GAME_ELEVATION_DEG)]
        for view_name, turn, elevation in turns:
            render.place_camera(camera, lights, (turn, elevation, True, TURN_HEIGHT, TURN_FRAME, False))
            render.render(os.path.join(folder, f"chog_{name}_{view_name}.png"))


if __name__ == "__main__":
    main()
