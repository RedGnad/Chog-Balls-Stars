#!/usr/bin/env python3
"""Inspect the local Quantum sample for a Fusion port; never imports or edits assets.

The GUID graph covers serialized Unity references, including importer metadata.
It does not resolve Quantum's numeric AssetRefs, Resources.Load, or package GUIDs.
The resulting inventory is an audit, not a ready-to-copy bundle.
"""
import argparse
import hashlib
import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "game/Assets"
GUID = re.compile(r"\bguid: ([0-9a-f]{32})\b")
TEXT_ASSETS = {".prefab", ".unity", ".asset", ".mat", ".controller", ".overrideController",
               ".anim", ".shader", ".meta", ".qprototype"}
COUPLING = re.compile(r"\bQuantum\b|Photon\.Deterministic|\bPredictedFrame\b|\bVerifiedFrame\b")


def read(path):
    return path.read_text(encoding="utf-8-sig")


def fingerprint(path):
    return {"path": str(path.relative_to(ROOT)), "bytes": path.stat().st_size,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def guid_index():
    index = {}
    for meta in sorted(ASSETS.rglob("*.meta")):
        match = re.search(r"^guid: ([0-9a-f]{32})$", read(meta), re.MULTILINE)
        if match:
            path = Path(str(meta)[:-5])
            if match[1] in index:
                raise ValueError(f"Duplicate GUID: {match[1]}")
            index[match[1]] = path
    return index


def references(path):
    refs = set()
    for candidate in (path, Path(str(path) + ".meta")):
        if candidate.is_file() and candidate.suffix in TEXT_ASSETS:
            refs.update(GUID.findall(read(candidate)))
    return {g for g in refs if not g.startswith("0000000000000000")}


def graph(roots, index):
    pending = list(roots)
    visited, unresolved = set(), set()
    while pending:
        path = pending.pop()
        if path in visited or not path.is_file():
            continue
        visited.add(path)
        for guid in references(path):
            target = index.get(guid)
            if target is None or not target.exists():
                unresolved.add(guid)
            elif target.is_file():
                pending.append(target)
    return sorted(visited), sorted(unresolved)


def script_references(path, index):
    scripts = []
    for guid in sorted(set(re.findall(r"m_Script: \{[^}]*guid: ([0-9a-f]{32})", read(path)))):
        target = index.get(guid)
        scripts.append({"guid": guid, "path": str(target.relative_to(ROOT)) if target else None,
                        "mentions_quantum": bool(COUPLING.search(read(target)))
                        if target and target.suffix == ".cs" else None})
    return scripts


def skill_parameters(path):
    content = read(path)
    result = {"source": fingerprint(path), "fixed_point_scale": 65536}
    fields = ("ActionInterval", "RotationLockDuration", "MovementLockDuration", "Cost",
              "AutoAimRadius", "MaxDistance", "Radius")
    for name in fields:
        match = re.search(rf"^  {name}:\n    RawValue: (-?\d+)$", content, re.MULTILINE)
        if match:
            raw = int(match[1])
            result[name] = {"raw": raw, "decoded": raw / 65536}
    for name in ("HasTTL", "ActionAmount", "AutoAimCheckSight", "CostType"):
        match = re.search(rf"^  {name}: (\d+)$", content, re.MULTILINE)
        if match:
            result[name] = int(match[1])
    angles = re.search(r"^  Angles:\n((?:  - RawValue: -?\d+\n?)+)", content, re.MULTILINE)
    if angles:
        result["Angles"] = [int(v) / 65536 for v in re.findall(r"RawValue: (-?\d+)", angles[1])]
    return result


def camera_parameters():
    prefab = ASSETS / "TwinStickShooter/Prefabs/GameEssentials.prefab"
    source = ASSETS / "TwinStickShooter/Scripts/GameView/LocalPlayerCameraFollow.cs"
    guid = re.search(r"^guid: (\w+)$", read(Path(str(source) + ".meta")), re.MULTILINE)[1]
    blocks = re.split(r"^--- ", read(prefab), flags=re.MULTILINE)
    component = next(block for block in blocks if f"guid: {guid}" in block)
    values = {}
    for name in ("InterpolationSpeed", "BaseOffset", "MaxZ", "MinZ", "InvertedMaxZ",
                 "InvertedMinZ", "InitialPosition", "CanMove", "LockXCoordinate",
                 "InitialDelay", "GameStartDelay"):
        values[name] = re.search(rf"^  {name}: (.+)$", component, re.MULTILINE)[1]
    camera = next(block for block in blocks if block.startswith("!u!20 "))
    values["field_of_view"] = re.search(r"^  field of view: (.+)$", camera, re.MULTILINE)[1]
    values["orthographic"] = re.search(r"^  orthographic: (.+)$", camera, re.MULTILINE)[1]
    scene = ASSETS / "QuantumUser/Scenes/Gameplay.unity"
    # Report overrides rather than silently presenting prefab defaults as runtime state.
    overrides = re.findall(r"propertyPath: ([^\n]+)\n\s+value: ([^\n]*)", read(scene))
    relevant = [(k, v) for k, v in overrides if any(k == n or k.startswith(n + ".") for n in values)]
    return {"source": fingerprint(source), "prefab": fingerprint(prefab), "scene": fingerprint(scene),
            "prefab_values": values, "matching_scene_override_fields": relevant,
            "limit": "Static values; intro events and team bounds affect runtime camera behaviour."}


def inspect():
    index = guid_index()
    heroes = [ASSETS / f"QuantumUser/Resources/Character_Playable_{name}.prefab"
              for name in ("Knight", "Archer", "Spellcaster")]
    dependencies, missing = graph(heroes, index)
    inventory = []
    for path in dependencies:
        entry = fingerprint(path)
        entry["guid"] = re.search(r"^guid: (\w+)$", read(Path(str(path) + ".meta")), re.MULTILINE)[1]
        if path.suffix == ".cs":
            entry["mentions_quantum"] = bool(COUPLING.search(read(path)))
        inventory.append(entry)
    skills = {path.stem: skill_parameters(path)
              for path in sorted((ASSETS / "QuantumUser/Resources").glob("*SkillData.asset"))
              if path.stem.startswith(("Knight", "Archer", "Spellcaster"))}
    paths = ["game/License.txt", "game/Assets/Photon/Quantum/Assemblies/Quantum.Deterministic.xml",
             "game/Assets/QuantumUser/Simulation/System/CharacterSkillCreationSystem.cs",
             "game/Assets/QuantumUser/Simulation/System/SkillSystem.cs",
             "game/Assets/QuantumUser/Simulation/AssetDefinition/SkillData.cs",
             "game/Assets/TwinStickShooter/Scripts/UI/AttackPreview.cs",
             "game/Assets/TwinStickShooter/Scripts/GameView/CharacterAnimations.cs",
             "game/Assets/TwinStickShooter/Scripts/GameView/CharacterView.cs",
             "game/Assets/TwinStickShooter/Scripts/GameView/BallisticAttackView.cs",
             "game/Assets/TwinStickShooter/Scripts/Audio/AudioManager.cs",
             "game/Assets/TwinStickShooter/Scripts/Audio/GameplaySFX.cs",
             "game/Assets/TwinStickShooter/Third Party Assets/Audio/Audio Credits.txt",
             "game/ProjectSettings/GraphicsSettings.asset",
             "game-fusion/ProjectSettings/GraphicsSettings.asset",
             "game-fusion/Assets/Chog/Arena/ArenaHero.cs"]
    paths.extend(str(p.relative_to(ROOT)) for p in sorted(
        (ASSETS / "QuantumUser/Simulation/AssetDefinition").glob("*.cs"))
        if p.stem.startswith(("Archer", "Knight", "Spellcaster", "Ballistic", "Ranged", "AreaOfEffect")))
    return {"inspected_at_utc": datetime.now(timezone.utc).isoformat(),
            "method": "Read-only local source, serialized parameters and transitive Unity GUID references",
            "runtime_port_implemented": False, "runtime_parity_verified": False,
            "limitations": ["GUID closure is not a portable asset bundle or a license clearance.",
                            "Quantum numeric AssetRefs, runtime loads and package GUIDs are not resolved.",
                            "C# type dependencies are not traced; mentions_quantum is only a textual coupling signal.",
                            "Source inspection does not measure game feel or mobile performance.",
                            "Fixed-point values use Q48.16 per Photon documentation; floats are inspection output, not a simulation port."],
            "fixed_point_reference": "https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/fixed-point",
            "hero_prefabs": [{"source": fingerprint(p), "scripts": script_references(p, index)} for p in heroes],
            "hero_guid_closure": {"asset_count": len(inventory),
                                  "asset_bytes_excluding_meta": sum(p.stat().st_size for p in dependencies),
                                  "extensions": dict(sorted(Counter(p.suffix for p in dependencies).items())),
                                  "unresolved_guids": missing, "files": inventory},
            "skills": skills, "camera": camera_parameters(),
            "inspected_sources": [fingerprint(ROOT / path) for path in paths]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, help="Write the inspection JSON to this explicit path")
    args = parser.parse_args()
    result = inspect()
    content = json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        args.output.write_text(content)
        closure = result["hero_guid_closure"]
        print(json.dumps({"output": str(args.output), "hero_prefabs": len(result["hero_prefabs"]),
                          "skills": len(result["skills"]), "closure_assets": closure["asset_count"],
                          "closure_bytes": closure["asset_bytes_excluding_meta"],
                          "unresolved_guids": closure["unresolved_guids"]}, indent=2))
    else:
        print(content, end="")


if __name__ == "__main__":
    main()
