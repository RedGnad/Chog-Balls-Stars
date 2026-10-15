"""Renders the objects of the basic attacks (chog_attack_objects.py) on one sheet, to be looked at before
they go into the game: each object twice, seen from the side and from the angle of the game camera (73
degrees above the horizon), in the colours and with the outline the game draws.

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/render_chog_attack_objects.py -- --out .artifacts/chog-body/attacks/objects.png

The leaf is drawn in the red skin; in the game it takes the skin of the hero.
"""

import argparse
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import make_chog_traits as traits  # noqa: E402
import render_chog_body as render  # noqa: E402
import render_chog_body_variants as flat  # noqa: E402
import chog_attack_objects as attacks  # noqa: E402

COLUMNS = 6
CELL = 0.55
VIEWS = (("side", 15.0), ("game", 73.0))


def main():
    parser = argparse.ArgumentParser(description="Renders the attack objects on one sheet.")
    parser.add_argument("--out", required=True)
    parser.add_argument("--size", type=int, default=1800)
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    bpy.ops.wm.read_factory_settings(use_empty=True)
    shared = traits.full.make_materials()
    root = bpy.data.objects.new("ChogAttacks", None)
    bpy.context.scene.collection.objects.link(root)

    made = []
    leaf = traits.variants.PartShapes(traits.full.MATERIAL_ORDER)
    attacks.build_leaf(leaf, traits.word_pieces)
    made.append(("Leaf", leaf.finish("Attack_Leaf", Vector((0.0, 0.0, 0.0)), root, shared)))
    for key, (colours, unlit, builder, _) in attacks.OBJECTS.items():
        prefix = "Attack_" + key
        shapes = traits.TraitShapes(prefix, prefix, colours)
        builder(shapes, traits.word_pieces)
        own = traits.trait_materials(prefix, colours, unlit, shared)
        made.append((key, shapes.finish(prefix, Vector((0.0, 0.0, 0.0)), root, own)))

    scene = bpy.context.scene
    render.set_engine(scene)
    scene.render.resolution_x = arguments.size
    rows = math.ceil(len(made) / COLUMNS) * len(VIEWS)
    scene.render.resolution_y = int(arguments.size * rows / COLUMNS)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("Sky")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = render.linear((0.16, 0.14, 0.20))
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
    scene.world = world

    # Materials as the game draws them: flat colours lit by one sun, unlit ones in their colour, the
    # outline dark and seen from inside.
    outline = flat.outline_material(flat.OUTLINE_COLOUR)
    cache = {}
    for _, thing in made:
        for index, material in enumerate(thing.data.materials):
            name = material.name
            if name not in cache:
                if name == traits.OUTLINE:
                    cache[name] = outline
                elif name == traits.SKIN:
                    cache[name] = render.plain_material("Skin", (0.95, 0.05, 0.05), 0.75)
                elif material.get("unlit"):
                    cache[name] = flat_unlit(name, tuple(material["srgb"]))
                elif "srgb" in material:
                    cache[name] = render.plain_material("R" + name, tuple(material["srgb"]), 0.75)
                else:
                    cache[name] = material
            thing.data.materials[index] = cache[name]

    # One copy of every object for each view, laid in rows; the camera looks down at the angle of the view.
    copies = []
    for view_index, (view, elevation) in enumerate(VIEWS):
        for index, (key, thing) in enumerate(made):
            copy = thing.copy()
            scene.collection.objects.link(copy)
            copy.parent = None
            row = (index // COLUMNS) + view_index * math.ceil(len(made) / COLUMNS)
            column = index % COLUMNS
            copy.location = Vector(((column - (COLUMNS - 1) * 0.5) * CELL, 0.0, -row * CELL * 10.0))
            copy["view"] = elevation
            copies.append(copy)
        for _, thing in made:
            thing.hide_render = True

    camera = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    scene.collection.objects.link(camera)
    scene.camera = camera
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    sun.data.energy = 3.0
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(40.0), 0.0, math.radians(-30.0))

    # Each row is rendered alone by moving the camera over it: rows are far apart in depth.
    tiles = []
    per_view_rows = math.ceil(len(made) / COLUMNS)
    folder = os.path.dirname(os.path.abspath(arguments.out))
    os.makedirs(folder, exist_ok=True)
    scene.render.resolution_y = int(arguments.size / COLUMNS)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = CELL * COLUMNS
    for view_index, (view, elevation) in enumerate(VIEWS):
        for row in range(per_view_rows):
            depth = -(row + view_index * per_view_rows) * CELL * 10.0
            angle = math.radians(elevation)
            target = Vector((0.0, 0.0, depth))
            away = Vector((0.0, -math.cos(angle), math.sin(angle)))
            camera.location = target + away * 4.0
            camera.rotation_euler = (math.radians(90.0 - elevation), 0.0, 0.0)
            path = os.path.join(folder, f"_row_{view}_{row}.png")
            scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            tiles.append(path)
    print("CHOG_ATTACK_SHEET rows " + " ".join(tiles))


def flat_unlit(name, colour):
    material = bpy.data.materials.new("U" + name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = render.linear(colour)
    output = nodes.new("ShaderNodeOutputMaterial")
    material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


if __name__ == "__main__":
    main()
