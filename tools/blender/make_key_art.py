"""The layers of the submission banner (23 October 2026, the owner: "a dynamic action picture with the 3D heroes throwing their
attacks at each other, a well made marketing banner, our own assets, Blender if needed, in the codes of the gaming
references"). Each hero of the layout is built from the game's library of parts (render_recruit.build_hero), dressed in the
game's look (render_chog_traits.game_look) and posed with the numbers of the game's own animator (render_chog_body.pose_numbers)
pushed further; each attack object (chog_attack_objects) and each tower (make_chog_structures) is made the same way. Every
layer is rendered alone, on a transparent background, through the one camera of the picture, so the layers fall into place in
perspective when tools/release/compose_banner.py lays them with the trails, the light and the title.

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/make_key_art.py -- --layout content/key-art/banner.json [--only heroes,objects,towers,floor] \
        [--scale 1.0]

The layers go to .artifacts/key-art/<layout name>/ (hero_<i>.png, object_<i>.png, tower_<i>.png, floor.png), with layers.json:
where each one is and, for the compositor, the point of each object and each tower's eyes on the picture.
"""

import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

import chog_attack_objects as attacks  # noqa: E402
import make_chog_traits as traits  # noqa: E402
import render_chog_traits as pictures  # noqa: E402
import render_recruit as recruit  # noqa: E402

render, flat, shots = pictures.render, pictures.flat, pictures.shots
OUT_ROOT = os.path.join(traits.REPOSITORY, ".artifacts", "key-art")


def setup_render(scene, size, samples=64):
    render.set_engine(scene)
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    if hasattr(scene.eevee, "taa_render_samples"):
        scene.eevee.taa_render_samples = samples


def place_camera(scene, spec):
    """The one camera of the picture: where it stands, what it looks at, its lens and its roll (degrees)."""
    camera = scene.camera
    if camera is None:
        camera = bpy.data.objects.new("KeyArtCamera", bpy.data.cameras.new("KeyArtCamera"))
        scene.collection.objects.link(camera)
        scene.camera = camera
    camera.data.type = "PERSP"
    camera.data.lens = spec["lens"]
    camera.data.sensor_width = 36.0
    camera.data.clip_end = 200.0
    render.aim(camera, Vector(spec["at"]), Vector(spec["look"]))
    camera.rotation_euler.rotate_axis("Z", math.radians(spec.get("roll", 0.0)))
    bpy.context.view_layer.update()
    return camera


def on_picture(scene, camera, point):
    """Where a point of the scene falls on the picture, in pixels from the top left."""
    from bpy_extras.object_utils import world_to_camera_view
    seen = world_to_camera_view(scene, camera, Vector(point))
    width, height = scene.render.resolution_x, scene.render.resolution_y
    return [round(seen.x * width, 1), round((1.0 - seen.y) * height, 1), round(seen.z, 3)]


def key_art_pose(parts, pose):
    """The game's pose numbers (render_chog_body.pose_numbers), then pushed: the layout gives the swing of the run (0 to 1), how far
    the throw has gone (0 to 1), and extra turns in degrees for the body, the head and each arm, in the axes of Unity."""
    run = pose.get("run", 0.0)
    strike = pose.get("strike", 0.0)
    cycle = math.pi * 0.5 * pose.get("swing", 1.0)
    swing = math.sin(cycle)
    thrown = 0.0 if strike <= 0.0 or strike >= 1.0 else math.sin(math.pi * math.sqrt(strike))
    forward = -100.0 * thrown
    numbers = {
        "body_offset": (0.0, abs(swing) * 0.07 * run, thrown * 0.12),
        "body_turn": (12.0 * run + 16.0 * thrown, 0.0, swing * 5.0 * run),
        "head_turn": (-6.0 * run - 8.0 * thrown, 0.0, -swing * 3.0 * run),
        "arm_left_turn": (swing * 55.0 * run, 0.0, 0.0),
        "arm_right_turn": (-swing * 55.0 * run + forward, 0.0, 0.0),
    }
    for key, extra in (("body_turn", "body"), ("head_turn", "head"), ("arm_left_turn", "left"), ("arm_right_turn", "right")):
        if extra in pose:
            numbers[key] = tuple(a + b for a, b in zip(numbers[key], pose[extra]))
    body = parts["Body"]
    body.location = render.UNITY_TO_BLENDER @ Vector(numbers["body_offset"]) + body.location
    body.rotation_euler = render.unity_turn(numbers["body_turn"])
    parts["Head"].rotation_euler = render.unity_turn(numbers["head_turn"])
    parts["armLeft"].rotation_euler = render.unity_turn(numbers["arm_left_turn"])
    parts["armRight"].rotation_euler = render.unity_turn(numbers["arm_right_turn"])
    bpy.context.view_layer.update()


def hero_layer(hero, index, layout, folder):
    """One hero of the layout, alone on a transparent picture."""
    blend = os.path.join(folder, "blend", f"hero_{index}.blend")
    recruit.build_hero(recruit.DEFAULT_LIBRARY, hero["number"], blend)
    bpy.ops.wm.open_mainfile(filepath=blend)
    root = bpy.data.objects[traits.base.ROOT_NAME]
    remembered = flat.remember_materials(pictures.MAIN_PARTS)
    skin = tuple(root["chog_skin"])
    tones = shots.tones(skin)
    tones["ChogFace"] = tuple(root["chog_face"])
    tones["ChogBlush"] = tuple(root["chog_blush"])
    for material in bpy.data.materials:
        if material.name not in tones and "srgb" in material:
            tones[material.name] = tuple(material["srgb"])
    used, parts, camera, lights = render.prepare_scene(64, skin)
    if "chog_skin_top" in root:
        pictures.gradient_skin(bpy.data.materials["ChogRenderSkin"], skin, tuple(root["chog_skin_top"]))
    made = flat.dress(parts, remembered, tones)
    pictures.dress_traits(made["ChogOutline"])
    stops = pictures.skin_stops_of(root) if "chog_skin_top" in root else None
    meshes = [thing for thing in bpy.data.objects if thing.type == "MESH" and thing.name != "ChogRenderGround"]
    pictures.game_look(meshes, stops)
    bpy.data.objects["ChogRenderGround"].hide_render = True
    for thing in bpy.data.objects:
        if hasattr(thing, "visible_shadow"):
            thing.visible_shadow = False

    key_art_pose(parts, hero.get("pose", {}))
    root.location = Vector((hero["at"][0], hero["at"][1], hero.get("lift", 0.0)))
    root.rotation_euler = (0.0, 0.0, math.radians(hero.get("facing", 0.0)))
    root.scale = Vector([hero.get("scale", 1.0)] * 3)
    scene = bpy.context.scene
    setup_render(scene, layout["size"])
    scene.camera = camera
    camera = place_camera(scene, layout["camera"])
    path = os.path.join(folder, f"hero_{index}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    # Where its feet and its hand are on the picture: the shadow and the start of its attack.
    bpy.context.view_layer.update()
    hand = parts["armRight"].matrix_world.translation
    return {"file": os.path.basename(path), "number": hero["number"], "team": hero["team"], "depth": on_picture(scene, camera, root.location)[2],
            "feet": on_picture(scene, camera, root.location), "hand": on_picture(scene, camera, hand), "used": used}


def object_layer(thing, index, layout, folder):
    """One attack object in flight, alone on a transparent picture, in the colours and the outline the game gives it."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    shared = traits.full.make_materials()
    key = thing["kind"]
    root = bpy.data.objects.new("KeyArtObject", None)
    bpy.context.scene.collection.objects.link(root)
    if key == "Leaf":
        shapes = traits.variants.PartShapes(traits.full.MATERIAL_ORDER)
        attacks.build_leaf(shapes, traits.word_pieces)
        made = shapes.finish("Attack_Leaf", Vector((0.0, 0.0, 0.0)), root, shared)
    else:
        colours, unlit, builder, _ = attacks.OBJECTS[key]
        prefix = "Attack_" + key
        shapes = traits.TraitShapes(prefix, prefix, colours)
        builder(shapes, traits.word_pieces)
        own = traits.trait_materials(prefix, colours, unlit, shared)
        made = shapes.finish(prefix, Vector((0.0, 0.0, 0.0)), root, own)
    outline = flat.outline_material(flat.OUTLINE_COLOUR)
    cache = {}
    for slot_index, material in enumerate(made.data.materials):
        name = material.name
        if name not in cache:
            if name == traits.OUTLINE:
                cache[name] = outline
            elif name == traits.SKIN:
                cache[name] = render.plain_material("Skin", tuple(thing.get("skin", (0.95, 0.05, 0.05))), 0.75)
            elif material.get("unlit") or thing.get("glow"):
                cache[name] = pictures.unlit_material("U" + name, tuple(material.get("srgb", (1.0, 1.0, 1.0))))
            elif "srgb" in material:
                cache[name] = render.plain_material("R" + name, tuple(material["srgb"]), 0.75)
            else:
                cache[name] = material
        made.data.materials[slot_index] = cache[name]
    root.location = Vector(thing["at"])
    root.rotation_euler = tuple(math.radians(value) for value in thing.get("turn", (0.0, 0.0, 0.0)))
    root.scale = Vector([thing.get("scale", 1.0)] * 3)
    scene = bpy.context.scene
    setup_render(scene, layout["size"], 32)
    world = bpy.data.worlds.new("KeyArtSky")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.35, 0.33, 0.42, 1.0)
    scene.world = world
    sun = bpy.data.objects.new("KeyArtSun", bpy.data.lights.new("KeyArtSun", "SUN"))
    sun.data.energy = 3.0
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(40.0), 0.0, math.radians(-30.0))
    camera = place_camera(scene, layout["camera"])
    path = os.path.join(folder, f"object_{index}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    entry = {"file": os.path.basename(path), "kind": key, "team": thing.get("team"), "at": on_picture(scene, camera, thing["at"])}
    if "from" in thing:
        entry["from"] = on_picture(scene, camera, thing["from"])
    return entry


def tower_layer(tower, index, layout, folder):
    """One team's tower (make_chog_structures): its column, its plinth in the colour of its side, the head with the laser eyes."""
    import make_chog_structures as structures
    made = structures.build_all()
    bpy.data.objects.remove(made["heart"], do_unlink=True)
    bpy.data.objects.remove(made["core"], do_unlink=True)
    side = structures.SIDES[tower.get("side", "Ours")]
    looks = {}
    for thing in bpy.data.objects:
        if thing.type != "MESH":
            continue
        for slot_index, material in enumerate(thing.data.materials):
            if material is None or material.name == structures.OUTLINE:
                continue
            name = material.name
            if name not in looks:
                colour = (side["skin"] if name == structures.TEAM else traits.GENESIS_FACE if name == "ChogFace"
                          else structures.exporter.BASE_COLOURS.get(name) or tuple(material.get("srgb", (1.0, 0.0, 1.0))))
                looks[name] = (structures.traits_render_unlit(name, colour) if material.get("unlit")
                               else render.plain_material("Look" + name, colour, 0.8))
            thing.data.materials[slot_index] = looks[name]
    body = made["tower"]
    body.location = Vector((tower["at"][0], tower["at"][1], 0.0))
    body.rotation_euler = (0.0, 0.0, math.radians(tower.get("facing", 0.0)))
    body.scale = Vector([tower.get("scale", 1.0)] * 3)
    scene = bpy.context.scene
    setup_render(scene, layout["size"], 32)
    world = bpy.data.worlds.new("KeyArtSky")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.30, 0.28, 0.40, 1.0)
    scene.world = world
    sun = bpy.data.objects.new("KeyArtSun", bpy.data.lights.new("KeyArtSun", "SUN"))
    sun.data.energy = 3.0
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(45.0), 0.0, math.radians(-20.0))
    camera = place_camera(scene, layout["camera"])
    path = os.path.join(folder, f"tower_{index}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.context.view_layer.update()
    eyes = made["head"].matrix_world @ structures.eyes_point(made["eyes"])
    return {"file": os.path.basename(path), "side": tower.get("side", "Ours"), "eyes": on_picture(scene, camera, eyes),
            "depth": on_picture(scene, camera, body.location)[2]}


def floor_layer(layout, folder):
    """The ground of the arena, the game's dark bricks, seen through the camera; the sky stays transparent."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    import bmesh
    scene = bpy.context.scene
    setup_render(scene, layout["size"], 32)
    mesh = bpy.data.meshes.new("KeyArtFloor")
    shapes = bmesh.new()
    bmesh.ops.create_grid(shapes, x_segments=1, y_segments=1, size=60.0)
    shapes.to_mesh(mesh)
    shapes.free()
    floor = bpy.data.objects.new("KeyArtFloor", mesh)
    scene.collection.objects.link(floor)
    axes = bpy.data.objects.new("KeyArtFloorAxes", None)
    scene.collection.objects.link(axes)
    axes.scale = Vector([layout.get("floor_scale", 1.0)] * 3)
    mesh.materials.append(flat.brick_material(axes))
    world = bpy.data.worlds.new("KeyArtSky")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.5, 0.5, 0.55, 1.0)
    scene.world = world
    sun = bpy.data.objects.new("KeyArtSun", bpy.data.lights.new("KeyArtSun", "SUN"))
    sun.data.energy = 2.4
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50.0), 0.0, math.radians(-25.0))
    place_camera(scene, layout["camera"])
    path = os.path.join(folder, "floor.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return {"file": "floor.png"}


def main():
    parser = argparse.ArgumentParser(description="Renders the layers of the banner.")
    parser.add_argument("--layout", required=True)
    parser.add_argument("--only", default="floor,towers,heroes,objects")
    parser.add_argument("--scale", type=float, default=1.0, help="a smaller picture for a quick look")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    with open(arguments.layout, encoding="utf-8") as file:
        layout = json.load(file)
    layout["size"] = [round(value * arguments.scale) for value in layout["size"]]
    name = os.path.splitext(os.path.basename(arguments.layout))[0]
    folder = os.path.join(OUT_ROOT, name)
    os.makedirs(os.path.join(folder, "blend"), exist_ok=True)
    wanted = set(arguments.only.split(","))
    kept_path = os.path.join(folder, "layers.json")
    kept = json.load(open(kept_path, encoding="utf-8")) if os.path.isfile(kept_path) else {}
    kept["size"] = layout["size"]
    if "floor" in wanted:
        kept["floor"] = floor_layer(layout, folder)
    if "towers" in wanted:
        kept["towers"] = [tower_layer(tower, index, layout, folder) for index, tower in enumerate(layout.get("towers", []))]
    if "heroes" in wanted:
        kept["heroes"] = [hero_layer(hero, index, layout, folder) for index, hero in enumerate(layout["heroes"])]
    if "objects" in wanted:
        kept["objects"] = [object_layer(thing, index, layout, folder) for index, thing in enumerate(layout.get("objects", []))]
    with open(kept_path, "w", encoding="utf-8") as file:
        json.dump(kept, file, indent=1)
    print("CHOG_KEY_ART layers in " + folder, flush=True)


if __name__ == "__main__":
    main()
