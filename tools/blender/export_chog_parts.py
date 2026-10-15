"""Exports the 3D heroes for the game: one library of parts from which the game assembles the hero of
any of the 1,969 Genesis (16 October 2026, after the owner said yes to batch 6).

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

    "CHOGPARTS2"
    materials: count, then name, colour (3 floats, as on screen), role (0 its colour, 1 the skin of the
               Genesis, 2 its face), light (0 lit, 1 unlit, 2 outline: unlit and seen from inside)
    skins:     count, then name, skin (3 floats), face (3 floats)
    rest:      count, then name, parent ("" for the root), position (3 floats): the four parts the
               game moves (ChogBodyAnimator)
    pieces:    count, then name, the rest part it sits on, vertex count, positions (3 floats each),
               normals (3 signed bytes each), material of each vertex (2 bytes), index count, indices
               (2 bytes, or 4 when the piece has more than 65,535 vertices)
    genesis:   count, then the number shown in the name (2 bytes; 0 is the grey Chog of a recruit), skin
               (1 byte), piece count (1 byte), pieces (2 bytes each)

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

REST_PARTS = ("Body", "Head", "armLeft", "armRight")
DEFAULT_OUT = os.path.join(traits.REPOSITORY, "game", "Assets", "Chog", "Resources", "ChogParts.bytes")
SUMMARY = os.path.join(traits.REPOSITORY, ".artifacts", "chog-body", "parts-export.json")
MAGIC = b"CHOGPARTS2"

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
SKINS = {**traits.SKINS, "Recruit": RECRUIT_SKIN}
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
            if genesis_traits.get("Base") == "Origin":
                found[int(record["metadata_id"])] = genesis_traits
    return found


def keys_of(genesis_traits):
    """The keys of the parts a Genesis wears: what each part is built from, and nothing else."""
    side = genesis_traits.get("Side", "Right")
    keys = {("rest", name) for name in ("Body", "armLeft", "armRight")}
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
    if thing.name in REST_PARTS:
        if thing.name == "Head":
            return next(key for key in keys_of(genesis_traits) if key[0] == "head")
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


def piece_of(thing, materials):
    """The mesh of one object in the axes of the game: positions, normals, material of each vertex and
    the triangles. "materials" maps a material record to its index and grows."""
    mesh = thing.data
    mesh.calc_loop_triangles()
    normals = mesh.corner_normals
    slots = []
    for slot in mesh.materials:
        # Several values share the names of their materials (every cap is "Hat_Cap_..."), each in its
        # own colours: a material of the library is a name, a colour, a role and a light together.
        record = material_record(slot)
        if record not in materials:
            materials[record] = len(materials)
        slots.append(materials[record])
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


def write_string(out, text):
    data = text.encode("utf-8")
    out.write(struct.pack("<H", len(data)))
    out.write(data)


def write_library(path, materials, rest, pieces, heroes):
    """Writes the file described at the top."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    temporary = path + ".writing"
    with open(temporary, "wb") as out:
        out.write(MAGIC)
        ordered = sorted(materials.items(), key=lambda item: item[1])
        out.write(struct.pack("<I", len(ordered)))
        for (name, colour, role, light), _ in ordered:
            write_string(out, name)
            out.write(struct.pack("<3f2B", *colour, role, light))
        out.write(struct.pack("<I", len(SKINS)))
        for name, skin in SKINS.items():
            write_string(out, name)
            out.write(struct.pack("<6f", *skin["skin"], *skin.get("face", traits.GENESIS_FACE)))
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
        skins = list(SKINS)
        for number, (skin, worn) in sorted(heroes.items()):
            out.write(struct.pack("<HBB", number, skins.index(skin), len(worn)))
            out.write(struct.pack(f"<{len(worn)}H", *worn))
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
    unknown_skins = sorted({found.get("Skin") for found in everyone.values()} - set(traits.SKINS))
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
        heroes[number] = (found["Skin"], worn)

    # The recruit: the body, the arms and the head of a Genesis without hat or hood.
    bare = ("head", None, None, None)
    if bare not in by_key:
        raise SystemExit("CHOG_PARTS no Genesis without hat or hood was built: the recruit has no head")
    heroes[RECRUIT] = ("Recruit", [index for key in (("rest", "Body"), bare, ("rest", "armLeft"), ("rest", "armRight"))
                                   for index in by_key[key]])

    write_library(arguments.out, materials, rest, pieces, heroes)
    size = os.path.getsize(arguments.out)
    triangles = sum(len(piece["indices"]) // 3 for piece in pieces)
    summary = {
        "written": arguments.out,
        "bytes": size,
        "genesis": len(heroes),
        "pieces": len(pieces),
        "triangles_in_pieces": triangles,
        "materials": len(materials),
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
