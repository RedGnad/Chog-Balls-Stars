"""Renders a candidate body made by make_chog_body_variants.py, or the present body in the same
setting, the way a phone shows a hero in a match. It wraps render_chog_body.py, which is not changed.

Run it without the interface of Blender (5.2):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
        --python tools/blender/render_chog_body_variants.py -- --variant A
        [--colours purple,red] [--size 636] [--cloth RRGGBB] [--glove RRGGBB] [--outline RRGGBB]
        [--only game|studio] [--ring red] [--head NUMBER] [--tag WORD]

"--variant" is A, B, C or "now" (the body of the game, .artifacts/chog-body/ChogBody.blend). The
pictures go to .artifacts/chog-body/variants/<variant>/, for each colour:

    chog_<variant>_<colour>_game_head.png              the angle of the game (73 degrees above the horizon,
                                                       slightly behind), on a floor of dark bricks, with the
                                                       picture of a Genesis where the game puts it
    chog_<variant>_<colour>_game_front_head.png        the same when the hero comes towards the camera
    chog_<variant>_<colour>_game_head_phone.png        the two above at the size of a phone screen: the
    chog_<variant>_<colour>_game_front_head_phone.png  whole hero is then about 70 points tall
    chog_<variant>_<colour>_front.png                  the body alone, on plain grey, from the front
    chog_<variant>_<colour>_behind.png                 from behind, as site_home_chog.png draws Chog
    chog_<variant>_<colour>_three_quarter.png          from the front and a little above
    chog_<variant>_<colour>_back.png                   from behind and a little above

What is not the game: these are mock-ups. The floor is a drawing of bricks in the colours measured
on a phone screenshot, with the green disc of the player's own hero under the red one; the light is
the one of render_chog_body.py; the head is the portrait of the Genesis as GenesisArt.RoundCopy cuts
it (inside its round picture, without its background, with the mask of tools/genesis-art/cut_out.py).
Nothing here was imported in Unity.

Colours: the skin is the one asked for, the hands and feet ("ChogGlove") are the pink of the official
drawings of Chog whatever the skin, the outline is near black. The cloth of candidate C has no colour
in the game yet. Here it takes the colour most present at the bottom of the portrait shown as the
head, which is where a portrait shows clothing, unless "--cloth" gives one: a guess made for these
pictures, not something the game does.
"""

import argparse
import math
import os
import sys

import bmesh
import bpy
import numpy

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import render_chog_body as render  # noqa: E402  (the folder of this file must be on the path first)

VARIANT_FOLDER = os.path.join(render.DEFAULT_FOLDER, "variants")
ART_FOLDER = os.path.join(render.REPOSITORY, ".artifacts", "genesis-art")

# The Genesis shown as the head for each skin colour: the two heroes of the phone screenshots of
# 12 October (CHOG #1287 in red, CHOG #1568 in purple).
HEADS = {"red": "1287", "purple": "1568"}
DEFAULT_HEAD = "1795"

# Measured on those screenshots (1280 points wide): a hero, hands included, is 66 points wide for
# 1.449 units of the model, so one unit of the model is about 46 points.
PHONE_POINTS_PER_UNIT = 46.0

# The floor of the arena on the same screenshots: brick, joint, and the size of one brick in units
# of the model (the game draws a body 1.3175 times larger than the model).
BRICK_COLOUR = (0.225, 0.19, 0.20)
BRICK_SHADE = (0.20, 0.168, 0.178)
JOINT_COLOUR = (0.12, 0.10, 0.105)
BRICK_SIZE = (4.2, 1.1)
JOINT_WIDTH = 0.035

# The green disc the game draws on the ground under the player's own hero, on the same screenshots:
# its colour, the lighter line around it and its radius in units of the model.
RING_COLOUR = (0.21, 0.47, 0.18)
RING_EDGE_COLOUR = (0.40, 0.66, 0.34)
RING_RADIUS = 0.70
RING_EDGE_WIDTH = 0.025

# The pink of the hands and feet of Chog, read on the official drawings (site_tools_jump.png,
# site_chog_read.png, chogathon_chog1.jpg in .artifacts/chog-reference/).
GLOVE_COLOUR = (0.88, 0.27, 0.50)
OUTLINE_COLOUR = (0.035, 0.03, 0.045)

# The views: those of render_chog_body.py, and the body seen straight from behind.
VIEWS = dict(render.VIEWS)
VIEWS["behind"] = (180.0, 4.0, True, 0.58, 1.9, False)
VIEWS["front_head"] = (0.0, 4.0, True, 0.92, 2.5, True)
VIEWS["back_head"] = (180.0, 4.0, True, 0.92, 2.5, True)
GAME_VIEWS = ("game_head", "game_front_head")
STUDIO_VIEWS = ("front", "behind", "three_quarter", "back")
# The whole hero as the game shows it today, the flat picture always turned to the camera, seen
# from where the game never looks: to set it beside a character that has a head of its own.
FLAT_HEAD_VIEWS = ("front_head", "three_quarter_head", "back_head")


def clothing_colour(picture, mask):
    """A guess at the colour of the clothing a portrait shows: the colour most present at its
    bottom, inside the round picture and outside its background. For these pictures only."""
    pixels = render.read_pixels(picture)
    side = pixels.shape[0]
    kept = render.read_pixels(mask)[..., 0] > 0.5 if mask else numpy.ones(pixels.shape[:2], dtype=bool)
    if kept.shape != pixels.shape[:2]:
        kept = numpy.ones(pixels.shape[:2], dtype=bool)
    rows, columns = numpy.mgrid[0:side, 0:pixels.shape[1]]
    inside = (rows - side * 0.5) ** 2 + (columns - pixels.shape[1] * 0.5) ** 2 < (side * 0.5) ** 2
    # The rows of an image of Blender start at the bottom.
    band = (rows > side * 0.03) & (rows < side * 0.20)
    found = pixels[inside & band & kept][:, :3]
    if len(found) < 50:
        return None
    # Colours are sorted into boxes of a sixth of each channel; the fullest box wins.
    boxes = numpy.minimum((found * 6.0).astype(numpy.int32), 5)
    keys = boxes[:, 0] * 36 + boxes[:, 1] * 6 + boxes[:, 2]
    fullest = numpy.bincount(keys, minlength=216).argmax()
    return tuple(float(value) for value in found[keys == fullest].mean(axis=0))


def tones(skin, glove, cloth, outline):
    """The colour of each material of a candidate."""
    return {
        "ChogSkin": skin,
        "ChogGlove": glove,
        # Without a colour of clothing the zone takes the skin: it is then part of the suit.
        "ChogCloth": cloth if cloth is not None else skin,
        "ChogOutline": outline,
    }


def outline_material(colour):
    """One flat colour whatever the light, seen from its inside only and casting no shadow on the
    shape it surrounds."""
    material = bpy.data.materials.new("ChogRenderOutline")
    nodes, links = render.node_tree(material).nodes, render.node_tree(material).links
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = render.linear(colour)
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    material.use_backface_culling = True
    if hasattr(material, "use_backface_culling_shadow"):
        material.use_backface_culling_shadow = True
    return material


def remember_materials(names):
    """Which materials each part has and which one each of its faces takes, before
    render.prepare_scene replaces them all by one skin (emptying the list of materials of a mesh
    also makes its faces forget which one they took)."""
    remembered = {}
    for name in names:
        part = bpy.data.objects.get(name)
        if part is None:
            raise SystemExit(f"CHOG_RENDER the file has no part named {name}")
        taken = numpy.zeros(len(part.data.polygons), dtype=numpy.int32)
        part.data.polygons.foreach_get("material_index", taken)
        remembered[name] = ([slot.material.name for slot in part.material_slots], taken)
    return remembered


def dress(parts, remembered, colours):
    """Puts back the materials of a candidate, each in its tone. The present body has one material
    and keeps the skin render.prepare_scene gave it."""
    made = {"ChogSkin": bpy.data.materials["ChogRenderSkin"]}
    for name, part in parts.items():
        names, taken = remembered[name]
        if len(names) <= 1:
            continue
        part.data.materials.clear()
        for material_name in names:
            if material_name not in colours:
                raise SystemExit(f"CHOG_RENDER {name} has a material this script has no colour for: {material_name}")
            if material_name not in made:
                if material_name == "ChogOutline":
                    made[material_name] = outline_material(colours[material_name])
                else:
                    made[material_name] = render.plain_material("ChogRender" + material_name[4:],
                                                                colours[material_name], 0.75)
            part.data.materials.append(made[material_name])
        part.data.polygons.foreach_set("material_index", taken)
        part.data.update()
    return made


def brick_material(axes):
    """A floor of dark bricks, laid along the axes of "axes" so that the rows can be kept level on
    the picture wherever the camera stands."""
    material = bpy.data.materials.new("ChogRenderBricks")
    tree = render.node_tree(material)
    nodes, links = tree.nodes, tree.links
    shader = nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = 1.0
    shader.inputs["Metallic"].default_value = 0.0
    where = nodes.new("ShaderNodeTexCoord")
    where.object = axes
    bricks = nodes.new("ShaderNodeTexBrick")
    bricks.offset = 0.5
    bricks.inputs["Color1"].default_value = render.linear(BRICK_COLOUR)
    bricks.inputs["Color2"].default_value = render.linear(BRICK_SHADE)
    bricks.inputs["Mortar"].default_value = render.linear(JOINT_COLOUR)
    bricks.inputs["Scale"].default_value = 1.0
    bricks.inputs["Mortar Size"].default_value = JOINT_WIDTH
    bricks.inputs["Mortar Smooth"].default_value = 0.0
    bricks.inputs["Bias"].default_value = 0.0
    bricks.inputs["Brick Width"].default_value = BRICK_SIZE[0]
    bricks.inputs["Row Height"].default_value = BRICK_SIZE[1]
    links.new(where.outputs["Object"], bricks.inputs["Vector"])
    links.new(bricks.outputs["Color"], shader.inputs["Base Color"])
    return material


def add_ring():
    """The disc under the player's own hero: two flat discs just above the floor, the larger and
    lighter one showing as a line around the other."""
    discs = []
    for name, radius, height, colour in (("ChogRenderRingEdge", RING_RADIUS + RING_EDGE_WIDTH, 0.002, RING_EDGE_COLOUR),
                                         ("ChogRenderRing", RING_RADIUS, 0.004, RING_COLOUR)):
        mesh = bpy.data.meshes.new(name)
        shapes = bmesh.new()
        bmesh.ops.create_circle(shapes, cap_ends=True, segments=64, radius=radius)
        shapes.to_mesh(mesh)
        shapes.free()
        mesh.materials.append(render.plain_material(name, colour, 1.0))
        disc = bpy.data.objects.new(name, mesh)
        disc.location = (0.0, 0.0, height)
        bpy.context.scene.collection.objects.link(disc)
        if hasattr(disc, "visible_shadow"):
            disc.visible_shadow = False
        discs.append(disc)
    return discs


def add_cut_face(picture, mask):
    """The portrait of a Genesis as the game shows it (GenesisArt.RoundCopy): what is inside the
    round picture and is not its background. "mask" says what to keep: white is kept."""
    face = render.add_face(picture)
    tree = face.data.materials[0].node_tree
    nodes, links = tree.nodes, tree.links
    emission = next(node for node in nodes if node.bl_idname == "ShaderNodeEmission")
    output = next(node for node in nodes if node.bl_idname == "ShaderNodeOutputMaterial")
    kept = nodes.new("ShaderNodeTexImage")
    kept.image = bpy.data.images.load(mask)
    kept.image.colorspace_settings.name = "Non-Color"
    nothing = nodes.new("ShaderNodeBsdfTransparent")
    choose = nodes.new("ShaderNodeMixShader")
    links.new(kept.outputs["Color"], choose.inputs["Fac"])
    links.new(nothing.outputs["BSDF"], choose.inputs[1])
    links.new(emission.outputs["Emission"], choose.inputs[2])
    links.new(choose.outputs["Shader"], output.inputs["Surface"])
    return face


def write_small(source, target, step):
    """The picture "step" times smaller: each point is the mean of the points it stands for."""
    pixels = render.read_pixels(source)
    height, width = pixels.shape[0] // step, pixels.shape[1] // step
    small = pixels[:height * step, :width * step].reshape(height, step, width, step, 4).mean(axis=(1, 3))
    small[..., 3] = 1.0
    image = bpy.data.images.new("ChogRenderSmall", width=width, height=height, alpha=False)
    image.pixels.foreach_set(small.astype(numpy.float32).ravel())
    image.filepath_raw = target
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)
    print(f"CHOG_RENDER wrote {target} ({width} x {height})")


def render_colour(variant, blend, folder, colour_name, size, phone, options):
    """Every picture of one body in one skin colour. "options" are the arguments of the command."""
    bpy.ops.wm.open_mainfile(filepath=blend)
    remembered = remember_materials(("Body", "Head", "armLeft", "armRight"))

    token = options.head or HEADS.get(colour_name, DEFAULT_HEAD)
    picture = os.path.join(ART_FOLDER, "256", f"{token}.jpg")
    mask = os.path.join(ART_FOLDER, "cut", f"{token}.png")
    if not os.path.isfile(picture):
        raise SystemExit(f"CHOG_RENDER no portrait at {picture}")
    if not os.path.isfile(mask):
        print(f"CHOG_RENDER no mask at {mask}: the portrait is drawn whole, in a disc")
        mask = None

    skin = render.parse_colour(colour_name)
    used, parts, camera, lights = render.prepare_scene(size, skin)
    scene = bpy.context.scene
    scene.render.image_settings.compression = 90
    glove = render.parse_colour(options.glove) if options.glove else GLOVE_COLOUR
    outline = render.parse_colour(options.outline) if options.outline else OUTLINE_COLOUR
    cloth = None
    if any("ChogCloth" in names for names, _ in remembered.values()):
        cloth = render.parse_colour(options.cloth) if options.cloth else clothing_colour(picture, mask)
        said = "no colour found, the skin is used" if cloth is None else " ".join(f"{value:.2f}" for value in cloth)
        print(f"CHOG_RENDER cloth of CHOG #{token}: {said} ({'given' if options.cloth else 'guessed from the portrait'})")
    dress(parts, remembered, tones(skin, glove, cloth, outline))

    ground = bpy.data.objects["ChogRenderGround"]
    plain = ground.data.materials[0]
    axes = bpy.data.objects.new("ChogRenderFloorAxes", None)
    scene.collection.objects.link(axes)
    bricks = brick_material(axes)
    ring = add_ring() if colour_name in options.ring.split(",") else []

    face = add_cut_face(picture, mask) if mask else render.add_face(picture)
    label = colour_name + (f"_{options.tag}" if options.tag else "")
    print(f"CHOG_RENDER {variant} in {label}, engine {used}, {size} px, head CHOG #{token}, "
          f"{'on the green disc' if ring else 'on the bare floor'}")

    def shoot(view_name, in_game):
        view = VIEWS[view_name]
        with_head = view[5]
        ground.data.materials[0] = bricks if in_game else plain
        for disc in ring:
            disc.hide_render = not in_game
        # The rows of bricks stay level on the picture, as they are on the screen of the game.
        axes.location = (0.3, 0.37, 0.0)
        axes.rotation_euler = (0.0, 0.0, math.radians(view[0]))
        render.place_camera(camera, lights, view)
        parts["Head"].hide_render = with_head
        face.hide_render = not with_head
        render.place_face(face, camera, parts["Head"].matrix_world.translation.copy())
        path = os.path.join(folder, f"chog_{variant}_{label}_{view_name}.png")
        render.render(path)
        return path

    if options.only in (None, "game"):
        for view_name in GAME_VIEWS:
            path = shoot(view_name, True)
            write_small(path, path[:-4] + "_phone.png", size // phone)
    if options.only in (None, "studio"):
        for view_name in STUDIO_VIEWS:
            shoot(view_name, False)
    if options.only == "flat":
        for view_name in FLAT_HEAD_VIEWS:
            shoot(view_name, False)


def main():
    parser = argparse.ArgumentParser(description="Renders a candidate Chog body as a phone shows a hero.")
    parser.add_argument("--variant", required=True, help="A, B, C or now (the body of the game)")
    parser.add_argument("--colours", default="purple,red", help="skin colours, separated by commas: "
                        f"{sorted(render.COLOURS)} or RRGGBB")
    parser.add_argument("--glove", default=None, help="colour of the hands and feet: a name or RRGGBB "
                        "(default: the pink of Chog)")
    parser.add_argument("--size", type=int, default=636, help="width and height of a large picture; brought down "
                        "to a whole number of times the phone size")
    parser.add_argument("--cloth", default=None, help="colour of the cloth (candidate C): a name or RRGGBB "
                        "(default: guessed from the portrait shown as the head)")
    parser.add_argument("--outline", default=None, help="colour of the outline: a name or RRGGBB")
    parser.add_argument("--only", default=None, choices=("game", "studio", "flat"), help="one family of pictures; "
                        "\"flat\" is the hero with its flat picture from the front, three quarter and back, which "
                        "is not rendered otherwise")
    parser.add_argument("--head", default=None, help="number of the Genesis shown as the head, for every colour")
    parser.add_argument("--ring", default="red", help="the colours shown on the green disc the game draws under "
                        "the player's own hero, separated by commas; the others stand on the bare floor")
    parser.add_argument("--tag", default=None, help="a word added to the names of the pictures, so that a second "
                        "set (another cloth, another outline) does not replace the first")
    parser.add_argument("--blend", default=None, help="the file to render (default: the one of the variant)")
    parser.add_argument("--out", default=None, help="folder for the pictures (default: the one of the variant)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    variant = arguments.variant
    if arguments.blend:
        blend = os.path.abspath(arguments.blend)
    elif variant == "now":
        blend = os.path.join(render.DEFAULT_FOLDER, "ChogBody.blend")
    else:
        blend = os.path.join(VARIANT_FOLDER, variant, "ChogBody.blend")
    if not os.path.isfile(blend):
        raise SystemExit(f"CHOG_RENDER no file at {blend}: run make_chog_body_variants.py first")
    folder = os.path.abspath(arguments.out or os.path.join(VARIANT_FOLDER, variant))
    os.makedirs(folder, exist_ok=True)

    # The game views are 2.3 units wide: that many points on a phone, and a large picture a whole
    # number of times that.
    frame = render.VIEWS["game_head"][4]
    phone = int(round(frame * PHONE_POINTS_PER_UNIT))
    size = max(1, arguments.size // phone) * phone

    for colour_name in arguments.colours.split(","):
        render_colour(variant, blend, folder, colour_name.strip(), size, phone, arguments)


if __name__ == "__main__":
    main()
