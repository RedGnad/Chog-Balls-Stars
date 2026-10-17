"""Renders a hero of the game's library of parts as the game assembles it: its pieces are read from
ChogParts.bytes (written by export_chog_parts.py) and laid on the four parts the game moves, so the
pictures show what the library holds, not a new build. Made on 17 October 2026 to judge the recruit's eyes
turned around (the grey Chog of a player without a Genesis, number 0); any number of the library works.

Run it without the interface of Blender (5.2), one Blender at a time:

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/render_recruit.py -- [--hero 0] [--library FILE] [--name recruit] \
        [--out .artifacts/chog-body/recruit-eyes] [--only studio,game,turnaround]

It writes <out>/<name>/ChogBody.blend, then the pictures of render_chog_traits.render_one into the same
folder (chog_<name>_front.png, _turn_000.png, _turn_game_front.png...). make_recruit_sheet.py lays them on
one sheet.

How a piece comes back: the export turned Blender's axes into the game's, (x, y, z) -> (-x, z, -y), a
reflection, and turned each triangle for Unity's winding; both are undone here, and each vertex keeps the
normal the library gives it. A piece that is not one of the four parts sits on its part with no move of its
own, as the game draws it. Colours: the skin and the face of the hero's entry of the skins, every other
material in the colour the library gives it; a material the game draws unlit is drawn unlit here.
"""

import argparse
import os
import struct
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import make_chog_traits as traits  # noqa: E402  (the folder of this file must be on the path first)
import render_chog_traits as pictures  # noqa: E402

DEFAULT_LIBRARY = os.path.join(traits.REPOSITORY, "game", "Assets", "Chog", "Resources", "ChogParts.bytes")
DEFAULT_OUT = os.path.join(traits.REPOSITORY, ".artifacts", "chog-body", "recruit-eyes")
MAGIC = b"CHOGPARTS5"
REST_PARTS = ("Body", "Head", "armLeft", "armRight")
LIGHT_UNLIT = 1


class Reader:
    """Reads the little endian layout described at the top of export_chog_parts.py."""

    def __init__(self, data):
        self.data, self.at = data, 0

    def take(self, layout):
        size = struct.calcsize("<" + layout)
        values = struct.unpack_from("<" + layout, self.data, self.at)
        self.at += size
        return values

    def text(self):
        (length,) = self.take("H")
        value = self.data[self.at:self.at + length].decode("utf-8")
        self.at += length
        return value


def read_library(path):
    """The materials, skins, rest parts, pieces and heroes of a library file."""
    with open(path, "rb") as source:
        reader = Reader(source.read())
    if reader.data[:len(MAGIC)] != MAGIC:
        raise SystemExit(f"CHOG_RECRUIT {path} is not a library of format {MAGIC.decode()}")
    reader.at = len(MAGIC)
    materials = []
    for _ in range(reader.take("I")[0]):
        name = reader.text()
        colour = reader.take("3f")
        role, light = reader.take("2B")
        materials.append({"name": name, "colour": colour, "role": role, "light": light})
    skins = []
    for _ in range(reader.take("I")[0]):
        name = reader.text()
        values = reader.take("6f")
        reader.take("B3f")
        skins.append({"name": name, "skin": values[:3], "face": values[3:]})
    rest = {}
    for _ in range(reader.take("I")[0]):
        name, parent = reader.text(), reader.text()
        rest[name] = (parent, reader.take("3f"))
    pieces = []
    for _ in range(reader.take("I")[0]):
        name, on = reader.text(), reader.text()
        (count,) = reader.take("I")
        positions = reader.take(f"{count * 3}f")
        normals = reader.take(f"{count * 3}b")
        kinds = reader.take(f"{count}H")
        (index_count,) = reader.take("I")
        indices = reader.take(f"{index_count}{'H' if count <= 65535 else 'I'}")
        pieces.append({"name": name, "on": on, "positions": positions, "normals": normals, "kinds": kinds,
                       "indices": indices})
    heroes = {}
    for _ in range(reader.take("I")[0]):
        number, skin, worn_count = reader.take("HBB")
        worn = reader.take(f"{worn_count}H")
        reader.take("HB")
        heroes[number] = {"skin": skin, "worn": worn}
    return materials, skins, rest, pieces, heroes


def blender_point(x, y, z):
    """A point of the game (+Y up, looking along +Z) back in Blender's axes (+Z up, looking along -Y)."""
    return (-x, -z, y)


def make_mesh(piece, materials, made):
    """The mesh of one piece, its faces turned back, each vertex with its normal, one material slot per
    material it uses ("made" maps a material index of the library to its Blender material)."""
    count = len(piece["kinds"])
    points = [blender_point(*piece["positions"][3 * v:3 * v + 3]) for v in range(count)]
    indices = piece["indices"]
    faces = [(indices[i], indices[i + 2], indices[i + 1]) for i in range(0, len(indices), 3)]
    mesh = bpy.data.meshes.new(piece["name"])
    mesh.from_pydata(points, [], faces)
    slots = []
    for kind in piece["kinds"]:
        if kind not in slots:
            slots.append(kind)
    for kind in slots:
        if kind not in made:
            record = materials[kind]
            material = bpy.data.materials.new(record["name"])
            material["srgb"] = list(record["colour"])
            if record["light"] == LIGHT_UNLIT:
                material["unlit"] = True
            made[kind] = material
        mesh.materials.append(made[kind])
    mesh.polygons.foreach_set("material_index", [slots.index(piece["kinds"][face[0]]) for face in faces])
    mesh.polygons.foreach_set("use_smooth", [True] * len(faces))
    normals = [blender_point(*(value / 127.0 for value in piece["normals"][3 * v:3 * v + 3])) for v in range(count)]
    if hasattr(mesh, "normals_split_custom_set_from_vertices"):
        mesh.normals_split_custom_set_from_vertices(normals)
    else:
        print(f"CHOG_RECRUIT this Blender cannot set the normals of {piece['name']}: smooth shading only", flush=True)
    mesh.update()
    return mesh


def build_hero(library_path, number, blend_path):
    """Lays the pieces of one hero of the library in an empty scene and saves it for render_chog_traits."""
    materials, skins, rest, pieces, heroes = read_library(library_path)
    if number not in heroes:
        raise SystemExit(f"CHOG_RECRUIT no hero {number} in {library_path}")
    hero = heroes[number]
    skin = skins[hero["skin"]]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    root = bpy.data.objects.new(traits.base.ROOT_NAME, None)
    scene.collection.objects.link(root)

    made, parts, worn_names = {}, {}, []
    worn = [pieces[index] for index in hero["worn"]]
    for name in REST_PARTS:
        own = [piece for piece in worn if piece["name"] == name and piece["on"] == name]
        if len(own) != 1:
            raise SystemExit(f"CHOG_RECRUIT hero {number} wears {len(own)} pieces for its part {name}")
        thing = bpy.data.objects.new(name, make_mesh(own[0], materials, made))
        scene.collection.objects.link(thing)
        parent, position = rest[name]
        thing.parent = root if not parent else None
        thing.location = blender_point(*position)
        parts[name] = thing
    for name in REST_PARTS:
        parent = rest[name][0]
        if parent:
            parts[name].parent = parts[parent]
    for piece in worn:
        if piece["name"] in REST_PARTS and piece["on"] == piece["name"]:
            continue
        thing = bpy.data.objects.new(piece["name"], make_mesh(piece, materials, made))
        scene.collection.objects.link(thing)
        thing.parent = parts[piece["on"]]
        thing["trait"] = piece["name"] + "=" + piece["name"]
        worn_names.append(piece["name"])

    # What render_chog_traits reads on the root of a Genesis: its colours and the side of its picture.
    blush = next((record["colour"] for record in materials if record["name"] == "ChogBlush"), traits.GENESIS_BLUSH)
    root["chog_genesis"] = skin["name"] if number == 0 else str(number)
    root["chog_skin"] = list(skin["skin"])
    root["chog_face"] = list(skin["face"])
    root["chog_blush"] = list(blush)
    root["chog_side"] = "Right"
    os.makedirs(os.path.dirname(blend_path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"CHOG_RECRUIT hero {number} ({skin['name']}) from {library_path}: {len(worn)} pieces, "
          f"{sum(len(piece['indices']) // 3 for piece in worn)} triangles, wearing {worn_names}", flush=True)


def main():
    parser = argparse.ArgumentParser(description="Renders a hero of the library of parts as the game assembles it.")
    parser.add_argument("--hero", type=int, default=0, help="its number in the library (0: the recruit)")
    parser.add_argument("--library", default=DEFAULT_LIBRARY, help="the library file to read")
    parser.add_argument("--name", default="recruit", help="the folder under --out and the name of the pictures")
    parser.add_argument("--out", default=DEFAULT_OUT, help="the folder that receives the folder of the pictures")
    parser.add_argument("--only", default="studio,game,turnaround", help="the families of render_chog_traits.py")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    folder = os.path.join(os.path.abspath(arguments.out), arguments.name)
    blend = os.path.join(folder, "ChogBody.blend")
    build_hero(os.path.abspath(arguments.library), arguments.hero, blend)
    pictures.render_one(arguments.name, blend, folder, arguments.only)


if __name__ == "__main__":
    main()
