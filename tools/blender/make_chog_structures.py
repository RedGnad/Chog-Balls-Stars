"""The tower and the core each team defends, in 3D, in the style of the heroes: the head of the game's
Chog (H3: crown of leaves, cream face, pink cheeks), dark outline shells, flat colours.

Run it without the interface of Blender (5.2):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/make_chog_structures.py -- [--render] [--export]

--render  writes pictures under .artifacts/chog-body/structures/: each structure turned around (eight
          views a little above the ground) and from the angle of the game (73 degrees above the ground),
          for one side's colours.
--export  writes game/Assets/Chog/Resources/ChogStructures.bytes in the format of the library of hero
          parts (export_chog_parts.py, CHOGPARTS5), which ChogPartsLibrary reads: four records, the
          tower and the core of each side.

What each is, after the flat pictures the game drew until now (StructureViews.Paint):

- The tower: a totem. A column of pale stone with two bands in the colour of its side, on a dark
  plinth, under the head of a Chog with fire laser eyes and gritted teeth: the beam the game draws
  when the tower fires leaves from those eyes. About 3.4 units high, where a hero is 2.5.
- The core: a big pink heart over a round plinth, held in a crown of nine Chog leaves that open
  outwards. The game makes the heart beat.

One unit here is one unit of the game (the heroes are built smaller and scaled by the game; these are
not). The parts the game moves are their own pieces: "Head" (the tower turns it towards whoever it
shoots), "Eyes" (an empty part where the beam starts) and "Heart". The side's colour is the "skin" of
the library: the game gives "Ours" or "Theirs".
"""

import argparse
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import bmesh  # noqa: E402
import bpy  # noqa: E402
from mathutils import Matrix, Vector  # noqa: E402

import make_chog_traits as traits  # noqa: E402  (the folder of this file must be on the path first)
import export_chog_parts as exporter  # noqa: E402
import render_chog_body as render  # noqa: E402

variants, base, full = traits.variants, traits.base, traits.full
OUTLINE = traits.OUTLINE
OUT_DIR = os.path.join(traits.REPOSITORY, ".artifacts", "chog-body", "structures")
LIBRARY = os.path.join(traits.REPOSITORY, "game", "Assets", "Chog", "Resources", "ChogStructures.bytes")

# Materials of the structures. "StructureTeam" is the colour of the side (the skin of the library).
TEAM = "StructureTeam"
COLOURS = {
    "StructureStone": (0.86, 0.82, 0.95),     # the column: a pale lavender stone
    "StructurePlinth": (0.20, 0.15, 0.38),    # the dark plinths
    "StructureChog": traits.SKINS["Origin"]["skin"],   # the purple of the default Chog (head, leaves)
    "StructureHeart": (0.95, 0.32, 0.58),     # the pink of the heart (the cheeks of the Chog)
    "StructureGlint": (1.00, 0.86, 0.92),     # the light on the heart
}
UNLIT = ("StructureGlint",)
SIDES = {
    "Ours": {"skin": (0.25, 0.56, 1.00)},     # the blue and the red of the bars and alarms of the game
    "Theirs": {"skin": (1.00, 0.30, 0.30)},
}
# The records of the library: number -> (structure, side).
RECORDS = {1: ("Tower", "Ours"), 2: ("Tower", "Theirs"), 3: ("Core", "Ours"), 4: ("Core", "Theirs")}

OUTLINE_THICKNESS = 0.05
SIDES_ROUND = 20

# The tower.
PLINTH = ((0.0, 0.86), (0.24, 0.86), (0.30, 0.78))          # (height, radius) up the plinth
TEAM_RING = ((0.28, 0.74), (0.40, 0.74))
COLUMN = ((0.38, 0.47), (1.50, 0.40))
BANDS = (((0.70, 0.455), (0.82, 0.445)), ((1.12, 0.43), (1.24, 0.42)))
COLLAR = ((1.44, 0.47), (1.56, 0.47))
NECK = 1.52               # where the head sits
HEAD_WIDTH = 2.05         # across the crown, in units of the game
TOWER_LOOK = {"Eyes": "Tower laser", "Mouth": "Grit", "Side": "Right"}
# The eyes of the tower: the fire lasers of the collection with beams cut short, two glowing red and
# yellow eyes (whole beams stuck out of the face like a beak, seen from the side). The game draws the
# beam itself when the tower fires. Numbers of the hero's head, before it is scaled.
TOWER_EYES = {"numbers": {"BEAM_LENGTH": 0.07, "BEAM_RADIUS": 0.034, "BEAM_SUNK": 0.03, "BEAM_CORE": (0, 1, 2, 3)}}
traits.TRAIT_PARTS["Eyes"][1]["Tower laser"] = {**traits.TRAIT_PARTS["Eyes"][1]["fire laser"], **TOWER_EYES}
# The heart leans back, its face up towards the camera of the game, which looks down from 73 degrees.
HEART_LEAN_DEG = 35.0

# The core.
CORE_PLINTH = ((0.0, 1.02), (0.20, 1.02), (0.26, 0.94))
CORE_RING = ((0.24, 0.88), (0.34, 0.88))
LEAVES = 9
LEAF_FOOT = 0.50          # distance of the foot of a leaf from the middle
LEAF_LENGTH = 1.25
LEAF_WIDTH = 0.30         # half
LEAF_THICK = 0.07         # half
LEAF_OPEN_DEG = 30.0      # from upright, outwards
HEART_CENTRE = 1.22
HEART_WIDTH = 1.25
HEART_DEPTH = 0.36        # half


def flat_material(name, colour, unlit=False):
    """A material of one colour, kept as "srgb" for the export and the renders."""
    material = bpy.data.materials.new(name)
    material.diffuse_color = traits.linear(colour)
    material["srgb"] = list(colour)
    material["unlit"] = unlit
    return material


def round_rings(stations):
    """Rings of a round, upright solid: (height, radius) from the bottom."""
    return [(Vector((0.0, 0.0, height)), Vector((radius, 0.0, 0.0)), Vector((0.0, radius, 0.0)))
            for height, radius in stations]


def custom_shell(part, loops, material, outline):
    """A closed shell through loops of points of any shape (a single point closes an end), as
    make_chog_body.add_shell makes one through ellipses; outlined like the shells of PartShapes."""
    mesh = part.mesh
    made = [[mesh.verts.new(point) for point in loop] for loop in loops]
    faces = []
    for lower, upper in zip(made, made[1:]):
        sides = max(len(lower), len(upper))
        for index in range(sides):
            after = (index + 1) % sides
            if len(lower) == 1:
                corners = (lower[0], upper[after], upper[index])
            elif len(upper) == 1:
                corners = (lower[index], lower[after], upper[0])
            else:
                corners = (lower[index], lower[after], upper[after], upper[index])
            face = mesh.faces.new(corners)
            face.smooth = True
            faces.append(face)
    part.paint(faces, material)
    if outline > 0.0:
        part.outlined.append((faces, outline, None))
    return faces


def build_tower_body(part):
    part.shell(round_rings(PLINTH), SIDES_ROUND, "StructurePlinth", OUTLINE_THICKNESS)
    part.shell(round_rings(TEAM_RING), SIDES_ROUND, TEAM, OUTLINE_THICKNESS)
    part.shell(round_rings(COLUMN), SIDES_ROUND, "StructureStone", OUTLINE_THICKNESS)
    for band in BANDS:
        part.shell(round_rings(band), SIDES_ROUND, TEAM, OUTLINE_THICKNESS)
    part.shell(round_rings(COLLAR), SIDES_ROUND, "StructurePlinth", OUTLINE_THICKNESS)


def leaf_loops(foot, outwards, around):
    """A Chog leaf: a blade that swells from its foot and ends in a point, open outwards."""
    tilt = math.radians(LEAF_OPEN_DEG)
    along = (Vector((0.0, 0.0, 1.0)) * math.cos(tilt) + outwards * math.sin(tilt)).normalized()
    flat = along.cross(around).normalized()       # the thin way of the blade
    loops = [[foot - along * 0.04]]
    for share in (0.08, 0.22, 0.40, 0.58, 0.74, 0.88):
        width = LEAF_WIDTH * math.sin(math.pi * min(1.0, share * 1.15)) ** 0.7 * (1.0 - 0.25 * share)
        thick = LEAF_THICK * (1.0 - 0.6 * share)
        centre = foot + along * (share * LEAF_LENGTH)
        loops.append([centre + around * width * math.cos(angle) + flat * thick * math.sin(angle)
                      for angle in (2.0 * math.pi * index / 10 for index in range(10))])
    loops.append([foot + along * LEAF_LENGTH])
    return loops


def heart_outline(points=40):
    """The heart of a playing card, about one unit wide, its middle at the origin: (across, up)."""
    shape = []
    for index in range(points):
        angle = 2.0 * math.pi * index / points
        across = 16.0 * math.sin(angle) ** 3
        up = 13.0 * math.cos(angle) - 5.0 * math.cos(2 * angle) - 2.0 * math.cos(3 * angle) - math.cos(4 * angle)
        shape.append((across / 32.0, (up + 2.5) / 32.0))
    return shape


def build_core_body(part):
    part.shell(round_rings(CORE_PLINTH), SIDES_ROUND, "StructurePlinth", OUTLINE_THICKNESS)
    part.shell(round_rings(CORE_RING), SIDES_ROUND, TEAM, OUTLINE_THICKNESS)
    for index in range(LEAVES):
        angle = 2.0 * math.pi * (index + 0.5) / LEAVES
        outwards = Vector((math.cos(angle), math.sin(angle), 0.0))
        around = Vector((-math.sin(angle), math.cos(angle), 0.0))
        foot = outwards * LEAF_FOOT + Vector((0.0, 0.0, CORE_RING[-1][0] - 0.04))
        custom_shell(part, leaf_loops(foot, outwards, around), "StructureChog", OUTLINE_THICKNESS)


def build_heart(part):
    """A puffed heart, its flat sides to the front and the back, its middle at the origin of its part."""
    shape = heart_outline()
    stations = (-1.0, -0.85, -0.6, -0.3, 0.0, 0.3, 0.6, 0.85, 1.0)
    loops = []
    for station in stations:
        swell = max(0.0, 1.0 - station * station) ** 0.5
        if swell < 1e-6:
            loops.append([Vector((0.0, station * HEART_DEPTH, 0.0))])
            continue
        loops.append([Vector((across * HEART_WIDTH * swell, station * HEART_DEPTH, up * HEART_WIDTH * swell))
                      for across, up in shape])
    custom_shell(part, loops, "StructureHeart", OUTLINE_THICKNESS)
    # A light on the upper left lobe, towards the front and the back.
    for side in (-1.0, 1.0):
        glint = [Vector((-0.22 * HEART_WIDTH + 0.07 * math.cos(a), side * (HEART_DEPTH * 0.93),
                         0.22 * HEART_WIDTH + 0.05 * math.sin(a)))
                 for a in (2.0 * math.pi * index / 8 for index in range(8))]
        middle = Vector((-0.22 * HEART_WIDTH, side * (HEART_DEPTH * 0.99), 0.22 * HEART_WIDTH))
        face_points = [part.mesh.verts.new(point) for point in glint]
        top = part.mesh.verts.new(middle)
        made = []
        for index in range(8):
            corners = (face_points[index], face_points[(index + 1) % 8], top)
            made.append(part.mesh.faces.new(corners if side > 0 else tuple(reversed(corners))))
        part.paint(made, "StructureGlint")
        part.open_faces.extend(made)
    # Leaning back: its top towards the back (+Y), its face towards the front and up.
    bmesh.ops.rotate(part.mesh, verts=part.mesh.verts[:], cent=Vector((0.0, 0.0, 0.0)),
                     matrix=Matrix.Rotation(math.radians(-HEART_LEAN_DEG), 3, "X"))


def make_materials():
    """The materials of the structures, the side's colour as the one of "Ours"."""
    made = {name: flat_material(name, colour, name in UNLIT) for name, colour in COLOURS.items()}
    made[TEAM] = flat_material(TEAM, SIDES["Ours"]["skin"])
    made[OUTLINE] = bpy.data.materials[OUTLINE]
    return made


def tower_head():
    """The head of a Chog with the tower's eyes and mouth, as make_chog_traits builds a hero, alone and
    scaled to HEAD_WIDTH. Empties the scene first (traits.build does), then makes the materials of the
    structures. Returns the root, the head, the parts on it and the materials."""
    root, parts, trait_parts, _, _, left_out = traits.build(dict(TOWER_LOOK))
    materials = make_materials()
    if left_out:
        raise SystemExit(f"CHOG_STRUCTURES the tower's look has parts that do not exist: {left_out}")
    head = next(part for part in parts if part.name == "Head")
    for part in parts:
        if part is not head:
            bpy.data.objects.remove(part, do_unlink=True)
    on_head = [part for part in trait_parts if part.parent is head]
    for part in trait_parts:
        if part not in on_head:
            bpy.data.objects.remove(part, do_unlink=True)

    corners = [point.co for point in head.data.vertices]
    width = max(point.x for point in corners) - min(point.x for point in corners)
    scale = HEAD_WIDTH / width
    for thing in [head] + on_head:
        thing.data.transform(Matrix.Scale(scale, 4))
        thing.data.update()
    head.location = Vector((0.0, 0.0, NECK))

    # The head is in the purple of the default Chog, not in the colour of a side.
    purple = materials["StructureChog"]
    for thing in [head] + on_head:
        for index, slot in enumerate(thing.data.materials):
            if slot is not None and slot.name == traits.SKIN:
                thing.data.materials[index] = purple
    print(f"CHOG_STRUCTURES head scaled {scale:.2f} to {HEAD_WIDTH} across", flush=True)
    return root, head, on_head, materials


def eyes_point(eyes_part):
    """Where the beam starts: between the two lasers, in the axes of the head."""
    points = [point.co for point in eyes_part.data.vertices]
    middle = sum(points, Vector()) / len(points)
    front = min(point.y for point in points)
    return Vector((0.0, front * 0.5 + middle.y * 0.5, middle.z))


def build_all():
    """Both structures side by side (the tower at x -2, the core at x +2), in one scene."""
    # traits.build empties the scene: the head comes first.
    root, head, on_head, materials = tower_head()
    order = tuple(COLOURS) + (TEAM, OUTLINE)

    tower_body = variants.PartShapes(order)
    build_tower_body(tower_body)
    tower = tower_body.finish("Tower_Body", Vector((0.0, 0.0, 0.0)), root, materials)
    head.parent = tower
    head.location = Vector((0.0, 0.0, NECK))
    head.name = "Tower_Head"
    eyes = next((part for part in on_head if part.get("trait", "").startswith("Eyes=")), None)
    if eyes is None:
        raise SystemExit("CHOG_STRUCTURES the tower's head has no eyes")

    core_body = variants.PartShapes(order)
    build_core_body(core_body)
    core = core_body.finish("Core_Body", Vector((0.0, 0.0, 0.0)), root, materials)
    heart_shapes = variants.PartShapes(order)
    build_heart(heart_shapes)
    heart = heart_shapes.finish("Core_Heart", Vector((0.0, 0.0, HEART_CENTRE)), core, materials)

    tower.location = Vector((-2.0, 0.0, 0.0))
    core.location = Vector((2.0, 0.0, 0.0))
    bpy.context.view_layer.update()
    for thing in (tower, head, *on_head, core, heart):
        print(f"CHOG_STRUCTURES {thing.name}: {len(thing.data.polygons)} triangles", flush=True)
    return {"tower": tower, "head": head, "on_head": on_head, "eyes": eyes, "core": core, "heart": heart}


def structure_record(material):
    """As export_chog_parts.material_record, with the colour of the side as the skin of the library."""
    if material.name == TEAM:
        return TEAM, (1.0, 1.0, 1.0), exporter.ROLE_SKIN, exporter.LIGHT_LIT
    return exporter.material_record(material)


def export(made, path):
    """The library of the structures: rest parts Body, Head, Eyes and Heart, and four records."""
    materials, pieces = {}, []
    head_at = made["head"].location.copy()
    eyes_at = eyes_point(made["eyes"])
    rest = {
        "Body": ("", (0.0, 0.0, 0.0)),
        "Head": ("Body", exporter.game_point(head_at)),
        "Eyes": ("Head", exporter.game_point(eyes_at)),
        "Heart": ("Body", exporter.game_point(Vector((0.0, 0.0, HEART_CENTRE)))),
    }

    def add(thing, on, name):
        piece = exporter.piece_of(thing, materials, record=structure_record)
        piece.update({"name": name, "on": on})
        pieces.append(piece)
        return len(pieces) - 1

    tower = [add(made["tower"], "Body", "Tower_Body"), add(made["head"], "Head", "Tower_Head")]
    tower += [add(part, "Head", part.name) for part in made["on_head"]]
    core = [add(made["core"], "Body", "Core_Body"), add(made["heart"], "Heart", "Core_Heart")]
    heroes, labels = {}, {}
    for number, (kind, side) in RECORDS.items():
        heroes[number] = (side, tower if kind == "Tower" else core, exporter.NO_ATTACK, 0)
        labels[number] = kind.upper()
    exporter.write_library(path, materials, rest, pieces, heroes, labels, skins=SIDES)
    triangles = {number: sum(len(pieces[index]["indices"]) // 3 for index in worn)
                 for number, (_, worn, _, _) in heroes.items()}
    print(f"CHOG_STRUCTURES wrote {path}: {len(pieces)} pieces, {len(materials)} materials, "
          f"triangles {triangles}, {os.path.getsize(path)} bytes", flush=True)


def render_pictures(made):
    """Turnarounds and the angle of the game, in the colours of the export, for the side "Ours"."""
    os.makedirs(OUT_DIR, exist_ok=True)
    scene = bpy.context.scene
    used = render.set_engine(scene)
    scene.render.resolution_x = scene.render.resolution_y = 420
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    if hasattr(scene.eevee, "taa_render_samples"):
        scene.eevee.taa_render_samples = 32
    world = bpy.data.worlds.new("StructureSky")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = traits.linear((0.15, 0.12, 0.30))
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
    scene.world = world

    ground = bpy.data.meshes.new("StructureGround")
    shapes = bmesh.new()
    bmesh.ops.create_grid(shapes, x_segments=1, y_segments=1, size=30.0)
    shapes.to_mesh(ground)
    shapes.free()
    ground.materials.append(render.plain_material("StructureGroundColour", (0.15, 0.125, 0.32), 1.0))
    scene.collection.objects.link(bpy.data.objects.new("StructureGround", ground))

    # Every material drawn in its colour: lit, or flat for the unlit ones; the outline as the game does.
    looks = {}
    for thing in bpy.data.objects:
        if thing.type != "MESH" or thing.name == "StructureGround":
            continue
        if hasattr(thing, "visible_shadow"):
            thing.visible_shadow = thing.name in ("Tower_Body", "Core_Body")
        for index, material in enumerate(thing.data.materials):
            if material is None or material.name == OUTLINE:
                continue
            name = material.name
            if name not in looks:
                colour = (SIDES["Ours"]["skin"] if name == TEAM else traits.GENESIS_FACE if name == "ChogFace"
                          else exporter.BASE_COLOURS.get(name) or tuple(material.get("srgb", (1.0, 0.0, 1.0))))
                looks[name] = (traits_render_unlit(name, colour) if material.get("unlit")
                               else render.plain_material("Look" + name, colour, 0.8))
            thing.data.materials[index] = looks[name]

    camera = bpy.data.objects.new("StructureCamera", bpy.data.cameras.new("StructureCamera"))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.lens = 50
    sun = bpy.data.objects.new("StructureSun", bpy.data.lights.new("StructureSun", "SUN"))
    sun.data.energy = 3.2
    scene.collection.objects.link(sun)
    fill = bpy.data.objects.new("StructureFill", bpy.data.lights.new("StructureFill", "SUN"))
    fill.data.energy = 1.0
    fill.data.use_shadow = False
    scene.collection.objects.link(fill)

    written = []
    for kind, thing, height in (("tower", made["tower"], 1.8), ("core", made["core"], 1.0)):
        target = thing.location + Vector((0.0, 0.0, height))
        views = [(f"turn{turn:03d}", turn, 12.0, 9.0) for turn in range(0, 360, 45)]
        views.append(("game", 0.0, 73.0, 12.0))
        for label, turn, elevation, distance in views:
            # Turn 0 looks at the front of the Chog (its face looks along -Y).
            angle = math.radians(turn)
            lift = math.radians(elevation)
            where = Vector((math.sin(angle) * math.cos(lift), -math.cos(angle) * math.cos(lift), math.sin(lift)))
            render.aim(camera, target + where * distance, target)
            render.aim(sun, target + Vector((where.x * 0.5 - 0.6, where.y * 0.5 - 0.8, 1.6)) * 10.0, target)
            render.aim(fill, target + where * 10.0, target)
            path = os.path.join(OUT_DIR, f"{kind}_{label}.png")
            scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            written.append(path)
    print(f"CHOG_STRUCTURES {len(written)} pictures in {OUT_DIR} ({used})", flush=True)


def traits_render_unlit(name, colour):
    material = bpy.data.materials.new("Look" + name)
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = traits.linear(colour)
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def main():
    parser = argparse.ArgumentParser(description="Builds the tower and the core in 3D.")
    parser.add_argument("--render", action="store_true", help="pictures under .artifacts/chog-body/structures/")
    parser.add_argument("--export", action="store_true", help="the library of the game")
    parser.add_argument("--out", default=LIBRARY, help="the library file to write")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    made = build_all()
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "ChogStructures.blend"))
    if arguments.export:
        export(made, arguments.out)
    if arguments.render:
        render_pictures(made)


if __name__ == "__main__":
    main()
