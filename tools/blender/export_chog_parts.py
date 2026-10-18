"""Exports the 3D heroes for the game: one library of parts from which the game assembles the hero of
any of the 1,969 Genesis (15 October 2026, after the owner said yes to batch 6).

One file per Genesis would weigh about 200 KB each, 400 MB in all: too much for a phone. But the parts a
hero is made of depend on one trait each (the head and its leaves on the hat, or on a hood; the hat on
"Head"; the eyes on "Eyes"; the mouth on "Mouth"; the clothing on "Body"; each on the Side the picture is
drawn), so the library keeps each part once and says, for every Genesis, which parts it wears and the
colour of its skin and face. The game puts them together (Assets/Chog/Match/ChogHeroModel.cs).

Run it without the interface of Blender (5.2), one Blender at a time (it builds about a hundred heroes):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/export_chog_parts.py -- [--out FILE] [--check 40] [--genesis 1287,14]

How the library is made:

- the parts each Genesis wears are named by keys (keys_of) read from its traits with the tables of
  make_chog_traits.py, the same ones its build reads;
- the fewest Genesis that wear every key are built (make_chog_traits.build and its report, so a build
  whose leaves go through its hat fails here too), and each object they are made of is kept once;
- "--check N" builds N more Genesis drawn at random and compares each of their objects with the one
  kept under the same key: a difference means a part depends on more than its key, and the export
  fails rather than give a hero a wrong part.

The file (written to game/Assets/Chog/Resources/ChogParts.bytes by default), little endian:

    "CHOGPARTS5"
    materials: count, then name, colour (3 floats, as on screen), role (0 its colour, 1 the skin of the
               Genesis, 2 its face), light (0 lit, 1 unlit, 2 outline: unlit and seen from inside)
    skins:     count, then name, skin (3 floats), face (3 floats), whether the skin runs to another
               colour at the top (1 byte), that colour (3 floats; the skin's own when it does not)
    rest:      count, then name, parent ("" for the root), position (3 floats): the four parts the
               game moves (ChogBodyAnimator)
    pieces:    count, then name, the rest part it sits on, vertex count, positions (3 floats each),
               normals (3 signed bytes each), material of each vertex (2 bytes), index count, indices
               (2 bytes, or 4 when the piece has more than 65,535 vertices)
    genesis:   count, then the number shown in the name (2 bytes; 0 is the grey Chog of a recruit, bare but
               for the eyes of "Side Eye"), skin
               (1 byte), piece count (1 byte), pieces (2 bytes each), the piece its basic attack throws
               (2 bytes, 65535 for none) and how that piece moves (1 byte, chog_attack_objects.SPIN...)
    lines:     count, then each line a player reads about a basic attack (string), and for every Genesis
               in the order above, the line of its own (2 bytes)

The objects of the basic attacks (chog_attack_objects.py) are pieces too, on the rest part "Attack": what a
hero throws comes from its value of "Head" (docs/ATTACKS-FROM-THE-ART.md); a hero that throws its hat
throws the very piece it wears.

Strings are a 2-byte length then UTF-8. Coordinates are the game's: +Y up, the hero looking along +Z, its
left arm on -X (Blender has +Z up and the hero looking along -Y), and each triangle is turned for
Unity's winding.
"""

import argparse
import hashlib
import json
import os
import random
import struct
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import bpy  # noqa: E402

import make_chog_traits as traits  # noqa: E402  (the folder of this file must be on the path first)
import render_chog_full as shots  # noqa: E402
import chog_attack_objects as attacks  # noqa: E402
import chog_trait_bases as bases  # noqa: E402
from mathutils import Vector  # noqa: E402

# What the 1:1 Genesis throw that no other does (chog_one_of_one.py): the devil's trident.
attacks.ATTACK_OF_HEAD.update(traits.one.ATTACK_OF_HEAD)
attacks.OBJECTS.update(traits.one.ATTACK_OBJECTS)
attacks.WORDS.update(traits.one.ATTACK_WORDS)

REST_PARTS = ("Body", "Head", "armLeft", "armRight")
DEFAULT_OUT = os.path.join(traits.REPOSITORY, "game", "Assets", "Chog", "Resources", "ChogParts.bytes")
SUMMARY = os.path.join(traits.REPOSITORY, ".artifacts", "chog-body", "parts-export.json")
MAGIC = b"CHOGPARTS5"
NO_ATTACK = 0xFFFF

# The colours of the base, as render_chog_traits.py draws them (the skin and face are per Genesis).
BASE_COLOURS = {
    "ChogGlove": shots.flat.GLOVE_COLOUR,
    "ChogEye": shots.EYE_COLOUR,
    "ChogBlush": traits.GENESIS_BLUSH,
    "ChogOutline": shots.flat.OUTLINE_COLOUR,
}
ROLE_FIXED, ROLE_SKIN, ROLE_FACE = 0, 1, 2
# A recruit (a player without a Genesis) wears the bare Chog, kept under the number 0, in a grey that no
# Skin of the collection has: what a Genesis adds shows by what the recruit lacks.
RECRUIT = 0
RECRUIT_SKIN = {"skin": (0.62, 0.62, 0.66), "face": traits.GENESIS_FACE}
# A face without eyes read as blank (17 October 2026), so the recruit wears the eyes of "Side Eye", the very
# part the Genesis of that value wear: a dark eye half closed under a straight lid, glancing aside, the
# Genesis drawing of the official mascot's half-lidded look (chogathon.chog.xyz/chog1.jpg, the first eyes
# of the site's picture maker), and the most worn eye part that is neither closed nor an object. CHOG #112
# wears it on the bare head too.
RECRUIT_EYES = ("trait", "Eyes", "Side Eye", "Right")
SKINS = {**traits.SKINS, "Recruit": RECRUIT_SKIN}
# The Skull base (batch 7): its own bone colours, by "Skin", under names that do not meet those of Origin.
SKINS.update({"Skull " + name: {"skin": colour, "face": colour} for name, colour in bases.SKULL_SKINS.items()})
# The Radioactive base: a skin that runs from its colour at the feet to a lighter one at the top (format 5).
# Its "face" is the deeper tone the skull shows in through the skin (bases.RADIOACTIVE_LINES).
SKINS.update({"Radioactive " + name: {"skin": bottom, "face": bases.RADIOACTIVE_LINES[name], "top": top}
              for name, (bottom, top) in bases.RADIOACTIVE_SKINS.items()})
BASES = ("Origin", "Skull", "Radioactive")


def skin_key(genesis_traits):
    """The name of a Genesis's colours in SKINS."""
    base_name = genesis_traits.get("Base", "Origin")
    return ("" if base_name == "Origin" else base_name + " ") + genesis_traits.get("Skin", "")
LIGHT_LIT, LIGHT_UNLIT, LIGHT_OUTLINE = 0, 1, 2


def all_genesis():
    """Every Genesis of the base Origin in the recorded metadata: number shown in its name -> its traits.
    The 100 Genesis on another base (Skull, Radioactive, 1:1) need their own head, not built yet: the
    game keeps their earlier look."""
    found = {}
    with open(traits.METADATA, encoding="utf-8") as lines:
        for line in lines:
            if not line.strip():
                continue
            record = json.loads(line)
            genesis_traits = {str(item["trait_type"]): str(item["value"])
                              for item in json.loads(record["raw_json"]).get("attributes", [])}
            number = int(record["metadata_id"])
            if genesis_traits.get("Base") in BASES:
                found[number] = genesis_traits
            elif genesis_traits.get("Base") == "1:1":
                # A 1:1 Genesis names no trait: it is built with the look chog_one_of_one.py gives it.
                found[number] = traits.one.look_of(number, genesis_traits)
    return found


def keys_of(genesis_traits):
    """The keys of the parts a Genesis wears: what each part is built from, and nothing else."""
    return {own_key(key, genesis_traits) for key in shared_keys_of(genesis_traits)}


def own_key(key, genesis_traits):
    """A 1:1 Genesis keeps its body, its arms and its head under its own number: their colours are its own
    (hands, cheeks), and nothing is shared with another Genesis but the parts of the traits it wears."""
    number = traits.one.number_of(genesis_traits)
    return key + ("1:1", number) if number and key[0] in ("rest", "head") else key


def shared_keys_of(genesis_traits):
    """keys_of before a 1:1 Genesis takes its own."""
    side = genesis_traits.get("Side", "Right")
    keys = {("rest", name) for name in ("Body", "armLeft", "armRight")}
    if bases.is_skull(genesis_traits) or bases.is_radioactive(genesis_traits):
        # A skull: its own head (no leaf, whatever the hat), and the ribs on its body. A radioactive Chog:
        # its head with the crown its head object leaves and the skull's teeth, the ribs on its body.
        base_name = genesis_traits["Base"]
        # (A skull's head depends on its Side since 18 October 2026: its jaw and its teeth are drawn otherwise.)
        head = (("head", "Skull", side) if base_name == "Skull"
                else ("head", "Radioactive", genesis_traits.get("Head")))
        # A skeleton's hands are bone (bases.bone_hands): its arms are its own.
        arms = ("Skull",) if base_name == "Skull" else ()
        keys = {("rest", "Body", base_name), ("rest", "armLeft") + arms, ("rest", "armRight") + arms, head}
        for trait in traits.TRAIT_PARTS:
            value = genesis_traits.get(trait)
            if value is not None and isinstance(traits.entry_for(trait, genesis_traits), dict):
                keys.add(("trait", trait, value, side))
        return keys
    # The head: its leaves follow the hat when the hat is modelled, else a hood (head_look).
    hat = traits.entry_for("Head", genesis_traits)
    hat_value = genesis_traits.get("Head") if isinstance(hat, dict) else None
    hood = None
    if hat_value is None:
        body = traits.entry_for("Body", genesis_traits)
        if isinstance(body, dict) and body.get("head"):
            hood = genesis_traits.get("Body")
    keys.add(("head", hat_value, hood, side if (hat_value or hood) else None))
    hidden = set(traits.head_look(genesis_traits).get("HIDES", ()))
    for trait in traits.TRAIT_PARTS:
        value = genesis_traits.get(trait)
        if value is None or trait in hidden or not isinstance(traits.entry_for(trait, genesis_traits), dict):
            continue
        keys.add(("trait", trait, value, side))
    return keys


def key_of_object(thing, genesis_traits):
    """The key an object of a built hero belongs to (see keys_of)."""
    return own_key(shared_key_of_object(thing, genesis_traits), genesis_traits)


def shared_key_of_object(thing, genesis_traits):
    """key_of_object before a 1:1 Genesis takes its own."""
    if thing.name in REST_PARTS:
        if thing.name == "Head":
            return next(key for key in shared_keys_of(genesis_traits) if key[0] == "head")
        if thing.name == "Body" and (bases.is_skull(genesis_traits) or bases.is_radioactive(genesis_traits)):
            return ("rest", "Body", genesis_traits["Base"])
        if thing.name in ("armLeft", "armRight") and bases.is_skull(genesis_traits):
            return ("rest", thing.name, "Skull")
        return ("rest", thing.name)
    trait, value = thing["trait"].split("=", 1)
    return ("trait", trait, value, genesis_traits.get("Side", "Right"))


def game_point(vector):
    """A point of Blender (+Z up, looking along -Y) in the axes of the game (+Y up, looking along +Z,
    left arm on -X)."""
    return (-vector[0], vector[2], -vector[1])


def material_record(material):
    """name, colour, role and light of a material of a built hero."""
    name = material.name
    if name == traits.SKIN:
        return name, (1.0, 1.0, 1.0), ROLE_SKIN, LIGHT_LIT
    if name == "ChogFace":
        return name, traits.GENESIS_FACE, ROLE_FACE, LIGHT_LIT
    if name in BASE_COLOURS:
        light = LIGHT_OUTLINE if name == traits.OUTLINE else LIGHT_LIT
        return name, tuple(BASE_COLOURS[name]), ROLE_FIXED, light
    if "srgb" not in material:
        raise SystemExit(f"CHOG_PARTS no colour known for the material {name}")
    light = LIGHT_UNLIT if material.get("unlit") else LIGHT_LIT
    return name, tuple(material["srgb"]), ROLE_FIXED, light


def piece_of(thing, materials, record=None):
    """The mesh of one object in the axes of the game: positions, normals, material of each vertex and
    the triangles. "materials" maps a material record to its index and grows. "record" gives the record
    of a material (material_record by default; make_chog_structures.py gives its own)."""
    record_of = record or material_record
    mesh = thing.data
    mesh.calc_loop_triangles()
    normals = mesh.corner_normals
    slots = []
    for slot in mesh.materials:
        # Several values share the names of their materials (every cap is "Hat_Cap_..."), each in its
        # own colours: a material of the library is a name, a colour, a role and a light together.
        made = record_of(slot)
        if made not in materials:
            materials[made] = len(materials)
        slots.append(materials[made])
    positions, vectors, kinds, indices, seen = [], [], [], [], {}
    for triangle in mesh.loop_triangles:
        kind = slots[triangle.material_index]
        corners = []
        for loop in triangle.loops:
            vertex = mesh.loops[loop].vertex_index
            normal = normals[loop].vector
            packed = tuple(max(-127, min(127, round(value * 127.0))) for value in game_point(normal))
            identity = (vertex, packed, kind)
            if identity not in seen:
                seen[identity] = len(positions)
                positions.append(game_point(mesh.vertices[vertex].co))
                vectors.append(packed)
                kinds.append(kind)
            corners.append(seen[identity])
        # The reflection between the two sets of axes turns the winding: Unity's front is clockwise.
        indices.extend((corners[0], corners[2], corners[1]))
    return {"positions": positions, "normals": vectors, "materials": kinds, "indices": indices}


def fingerprint(piece, materials):
    """What makes two pieces the same: their points (to a tenth of a millimetre), their triangles and
    their materials (name, colour, role, light)."""
    names = {index: repr(record) for record, index in materials.items()}
    digest = hashlib.sha256()
    for point in piece["positions"]:
        digest.update(struct.pack("<3i", *(round(value * 10000.0) for value in point)))
    digest.update(struct.pack(f"<{len(piece['indices'])}I", *piece["indices"]))
    digest.update("|".join(names[kind] for kind in piece["materials"]).encode("utf-8"))
    return digest.hexdigest()


def build_hero(number, genesis_traits):
    """Builds one Genesis as make_chog_traits.make_one does (its report included, so a failed check
    fails here) and returns its objects."""
    root, parts, trait_parts, body_n, head_n, left_out = traits.build(dict(genesis_traits))
    traits.report(str(number), parts, trait_parts, body_n, head_n)
    for part in parts:
        if part.rotation_euler.to_quaternion().angle > 1e-6 or abs(part.scale.length - 3 ** 0.5) > 1e-6:
            raise SystemExit(f"CHOG_PARTS {part.name} is turned or scaled: the game expects neither")
    for part in trait_parts:
        if part.location.length > 1e-9 or part.rotation_euler.to_quaternion().angle > 1e-6:
            raise SystemExit(f"CHOG_PARTS {part.name} does not sit exactly on {part.parent.name}")
    return parts, trait_parts, left_out


def cover(everyone):
    """The fewest Genesis (greedily) that wear every key, the numbers in order."""
    missing = set().union(*everyone.values())
    chosen = []
    while missing:
        number = max(sorted(everyone), key=lambda n: len(everyone[n] & missing))
        chosen.append(number)
        missing -= everyone[number]
    return chosen


def build_attack_objects(materials):
    """Every object of chog_attack_objects.py, made in an empty scene: key -> its piece."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    shared = traits.full.make_materials()
    root = bpy.data.objects.new("ChogAttacks", None)
    bpy.context.scene.collection.objects.link(root)
    made = {}
    leaf = traits.variants.PartShapes(traits.full.MATERIAL_ORDER)
    attacks.build_leaf(leaf, traits.word_pieces)
    thing = leaf.finish("Attack_Leaf", Vector((0.0, 0.0, 0.0)), root, shared)
    made["Leaf"] = piece_of(thing, materials)
    for key, (colours, unlit, builder, _) in attacks.OBJECTS.items():
        prefix = "Attack_" + key
        shapes = traits.TraitShapes(prefix, prefix, colours)
        builder(shapes, traits.word_pieces)
        own = traits.trait_materials(prefix, colours, unlit, shared)
        thing = shapes.finish(prefix, Vector((0.0, 0.0, 0.0)), root, own)
        made[key] = piece_of(thing, materials)
        print(f"CHOG_PARTS attack object {key}: {len(made[key]['indices']) // 3} triangles", flush=True)
    return made


def write_string(out, text):
    data = text.encode("utf-8")
    out.write(struct.pack("<H", len(data)))
    out.write(data)


def write_library(path, materials, rest, pieces, heroes, labels, skins=None):
    """Writes the file described at the top. "skins" replaces the table of SKINS (the structures give
    the colours of the two sides)."""
    table = skins or SKINS
    os.makedirs(os.path.dirname(path), exist_ok=True)
    temporary = path + ".writing"
    with open(temporary, "wb") as out:
        out.write(MAGIC)
        ordered = sorted(materials.items(), key=lambda item: item[1])
        out.write(struct.pack("<I", len(ordered)))
        for (name, colour, role, light), _ in ordered:
            write_string(out, name)
            out.write(struct.pack("<3f2B", *colour, role, light))
        out.write(struct.pack("<I", len(table)))
        for name, skin in table.items():
            write_string(out, name)
            out.write(struct.pack("<6f", *skin["skin"], *skin.get("face", traits.GENESIS_FACE)))
            out.write(struct.pack("<B3f", 1 if "top" in skin else 0, *skin.get("top", skin["skin"])))
        out.write(struct.pack("<I", len(rest)))
        for name, (parent, position) in rest.items():
            write_string(out, name)
            write_string(out, parent)
            out.write(struct.pack("<3f", *position))
        out.write(struct.pack("<I", len(pieces)))
        for piece in pieces:
            write_string(out, piece["name"])
            write_string(out, piece["on"])
            count = len(piece["positions"])
            out.write(struct.pack("<I", count))
            out.write(struct.pack(f"<{count * 3}f", *(value for point in piece["positions"] for value in point)))
            out.write(struct.pack(f"<{count * 3}b", *(value for normal in piece["normals"] for value in normal)))
            out.write(struct.pack(f"<{count}H", *piece["materials"]))
            indices = piece["indices"]
            out.write(struct.pack("<I", len(indices)))
            out.write(struct.pack(f"<{len(indices)}{'H' if count <= 65535 else 'I'}", *indices))
        out.write(struct.pack("<I", len(heroes)))
        skins = list(table)
        for number, (skin, worn, attack, style) in sorted(heroes.items()):
            out.write(struct.pack("<HBB", number, skins.index(skin), len(worn)))
            out.write(struct.pack(f"<{len(worn)}H", *worn))
            out.write(struct.pack("<HB", attack, style))
        lines = sorted(set(labels.values()))
        out.write(struct.pack("<I", len(lines)))
        for line in lines:
            write_string(out, line)
        for number in sorted(heroes):
            out.write(struct.pack("<H", lines.index(labels[number])))
    os.replace(temporary, path)


def main():
    parser = argparse.ArgumentParser(description="Exports the library of 3D hero parts for the game.")
    parser.add_argument("--out", default=DEFAULT_OUT, help="the library file to write")
    parser.add_argument("--check", type=int, default=40, help="Genesis drawn at random, built again and compared")
    parser.add_argument("--genesis", default=None, help="only these Genesis (numbers shown in the names), for a try")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    started = time.monotonic()

    everyone = all_genesis()
    if arguments.genesis:
        wanted = {int(number) for number in arguments.genesis.split(",") if number.strip()}
        everyone = {number: found for number, found in everyone.items() if number in wanted}
    worn_keys = {number: keys_of(found) for number, found in everyone.items()}
    unknown_skins = sorted({skin_key(found) for found in everyone.values()} - set(SKINS))
    if unknown_skins:
        raise SystemExit(f"CHOG_PARTS no colour recorded for Skin {unknown_skins}")
    chosen = cover(worn_keys)
    print(f"CHOG_PARTS {len(everyone)} Genesis wear {len(set().union(*worn_keys.values()))} keys; "
          f"{len(chosen)} Genesis are built to make them all", flush=True)

    materials, rest, pieces, by_print, by_key = {}, {}, [], {}, {}
    failed = {}

    def take(number, check):
        genesis_traits = everyone[number]
        parts, trait_parts, _ = build_hero(number, genesis_traits)
        made = {}
        for thing in parts + trait_parts:
            key = key_of_object(thing, genesis_traits)
            piece = piece_of(thing, materials)
            made.setdefault(key, []).append((thing.name, thing.parent.name if thing.name not in REST_PARTS
                                             else thing.name, piece))
            if thing.name in REST_PARTS and thing.name not in rest:
                rest[thing.name] = ("" if thing.name == "Body" else "Body",
                                    game_point(thing.location))
        for key, objects in made.items():
            prints = [fingerprint(piece, materials) for _, _, piece in objects]
            if key in by_key:
                if [pieces[index]["print"] for index in by_key[key]] != prints:
                    raise SystemExit(f"CHOG_PARTS #{number}: the part {key} differs from the one kept for that key "
                                     f"(it depends on more than its key)")
                continue
            if check:
                continue
            indices = []
            for (name, on, piece), digest in zip(objects, prints):
                if digest not in by_print:
                    by_print[digest] = len(pieces)
                    pieces.append({**piece, "name": name, "on": on, "print": digest})
                indices.append(by_print[digest])
            by_key[key] = indices

    for index, number in enumerate(chosen):
        try:
            take(number, check=False)
        except SystemExit as stop:
            if "differs from the one kept" in str(stop):
                raise
            failed[number] = str(stop)
            print(f"CHOG_PARTS #{number} FAILED: {stop}", flush=True)
        if (index + 1) % 10 == 0 or index + 1 == len(chosen):
            print(f"CHOG_PARTS built {index + 1}/{len(chosen)} ({time.monotonic() - started:.0f} s)", flush=True)

    # Keys whose first Genesis failed: up to three more Genesis that wear them are tried.
    tries = {}
    for number in sorted(everyone):
        lacking = worn_keys[number] - set(by_key)
        if not lacking or number in failed or all(tries.get(key, 0) >= 3 for key in lacking):
            continue
        for key in lacking:
            tries[key] = tries.get(key, 0) + 1
        try:
            take(number, check=False)
        except SystemExit as stop:
            if "differs from the one kept" in str(stop):
                raise
            failed[number] = str(stop)
            print(f"CHOG_PARTS #{number} FAILED: {stop}", flush=True)

    rng = random.Random(16102026)
    drawn = rng.sample(sorted(set(everyone) - set(chosen)), min(arguments.check, len(everyone) - len(chosen)))
    for number in drawn:
        try:
            take(number, check=True)
        except SystemExit as stop:
            if "differs from the one kept" in str(stop):
                raise
            failed[number] = str(stop)
    print(f"CHOG_PARTS checked {len(drawn)} more Genesis against the kept parts: all the same", flush=True)

    # The objects of the basic attacks, after the heroes (each build empties the scene).
    attack_pieces = {}
    for key, piece in build_attack_objects(materials).items():
        attack_pieces[key] = len(pieces)
        pieces.append({**piece, "name": "Attack_" + key, "on": "Attack", "print": fingerprint(piece, materials)})

    def attack_for(found, worn):
        """The piece a hero's basic attack throws and how it moves (see chog_attack_objects)."""
        key, style = attacks.attack_of(found.get("Head"))
        if bases.is_skull(found) and not found.get("Head"):
            key, style = "Bone", attacks.TUMBLE
        if key == "HAT":
            hat = by_key.get(("trait", "Head", found.get("Head"), found.get("Side", "Right")))
            if hat:
                return hat[0], style
            key, style = attacks.attack_of("")
        return attack_pieces[key], style

    heroes, incomplete = {}, {}
    for number, found in everyone.items():
        worn, lacking = [], []
        for key in sorted(worn_keys[number], key=repr):
            if key in by_key:
                worn.extend(by_key[key])
            else:
                lacking.append(key)
        if not any(pieces[index]["on"] == "Head" and pieces[index]["name"] == "Head" for index in worn):
            incomplete[number] = lacking
            continue
        if lacking:
            incomplete[number] = lacking
        heroes[number] = (skin_key(found), worn, *attack_for(found, worn))

    # The recruit: the body, the arms and the head of a Genesis without hat or hood, and plain eyes.
    bare = ("head", None, None, None)
    if bare not in by_key:
        raise SystemExit("CHOG_PARTS no Genesis without hat or hood was built: the recruit has no head")
    if RECRUIT_EYES not in by_key:
        raise SystemExit(f"CHOG_PARTS no Genesis wearing {RECRUIT_EYES} was built: the recruit has no eyes")
    recruit_keys = (("rest", "Body"), bare, ("rest", "armLeft"), ("rest", "armRight"), RECRUIT_EYES)
    heroes[RECRUIT] = ("Recruit", [index for key in recruit_keys for index in by_key[key]],
                       attack_pieces["Leaf"], attacks.TUMBLE)

    labels = {number: attacks.label_of(everyone[number].get("Head"), bases.is_skull(everyone[number])) if number in everyone else
              "THROWS THE SPIKES OF ITS CROWN" for number in heroes}
    # A 1:1 Genesis says what it throws in its own words (chog_one_of_one.LINES).
    labels.update({number: line for number, line in traits.one.LINES.items() if number in heroes})
    write_library(arguments.out, materials, rest, pieces, heroes, labels)
    size = os.path.getsize(arguments.out)
    triangles = sum(len(piece["indices"]) // 3 for piece in pieces)
    summary = {
        "written": arguments.out,
        "bytes": size,
        "genesis": len(heroes),
        "pieces": len(pieces),
        "triangles_in_pieces": triangles,
        "materials": len(materials),
        "attack_objects": {key: len(pieces[index]["indices"]) // 3 for key, index in attack_pieces.items()},
        "built": len(chosen),
        "checked": len(drawn),
        "failed": failed,
        "lacking_parts": {str(number): [list(key) for key in keys] for number, keys in incomplete.items()},
        "seconds": round(time.monotonic() - started),
    }
    os.makedirs(os.path.dirname(SUMMARY), exist_ok=True)
    with open(SUMMARY, "w", encoding="utf-8") as target:
        json.dump(summary, target, indent=1)
    print(f"CHOG_PARTS wrote {arguments.out}: {size} bytes, {len(pieces)} pieces, {triangles} triangles, "
          f"{len(materials)} materials, {len(heroes)} Genesis ({len(incomplete)} lacking a part, "
          f"{len(failed)} builds failed) in {summary['seconds']} s; summary {SUMMARY}", flush=True)


if __name__ == "__main__":
    main()
