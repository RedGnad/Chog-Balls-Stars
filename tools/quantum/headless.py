#!/usr/bin/env python3
"""Build the real Quantum simulation outside Unity, test Genesis binding, and replay a match.

  headless.py build | test [--dotnet <path to dotnet>]
  headless.py verify <replay.json> <asset database.json> <verdict.json> [--dotnet ...]

"verify" plays a recorded match again and writes who won (see tools/quantum/Verifier.cs).
"""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "game"
OUT = ROOT / ".artifacts/quantum-dotnet"


def project_file(path, properties):
    element = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    group = ET.SubElement(element, "PropertyGroup")
    for key, value in properties.items():
        ET.SubElement(group, key).text = value
    return element, ET.SubElement(element, "ItemGroup")


def save_project(path, element):
    path.parent.mkdir(parents=True, exist_ok=True)
    ET.indent(element)
    ET.ElementTree(element).write(path, encoding="unicode")


def reference(group, name, path):
    entry = ET.SubElement(group, "Reference", Include=name)
    ET.SubElement(entry, "HintPath").text = str(path)


def generate():
    OUT.mkdir(parents=True, exist_ok=True)
    # No NuGet dependencies are required for these projects. Reference packs ship in the SDK.
    (OUT / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>\n")
    libs = OUT / "Lib/Release"
    libs.mkdir(parents=True, exist_ok=True)
    archive = PROJECT / "Assets/Photon/Quantum/Editor/Dotnet/Quantum.Dotnet.Release.zip"
    with zipfile.ZipFile(archive) as z:
        for entry in z.infolist():
            if Path(entry.filename).name != entry.filename:
                raise ValueError("Unexpected SDK archive path")
        z.extractall(libs)

    asmdef = PROJECT / "Assets/Photon/Quantum/Simulation/Quantum.Simulation.asmdef"
    guid = re.search(r"^guid: (\w+)$", Path(str(asmdef) + ".meta").read_text(), re.M)[1]
    folders = [asmdef.parent]
    for path in (PROJECT / "Assets").rglob("*.asmref"):
        value = json.loads(path.read_text(encoding="utf-8-sig"))["reference"]
        if value in ("Quantum.Simulation", "GUID:" + guid):
            folders.append(path.parent)
    properties = {"TargetFramework": "netstandard2.1", "AssemblyName": "Quantum.Simulation",
                  "AllowUnsafeBlocks": "true", "LangVersion": "latest", "EnableDefaultCompileItems": "false",
                  "GenerateAssemblyInfo": "false", "GenerateTargetFrameworkAttribute": "false"}
    simulation, items = project_file(OUT / "Simulation/Quantum.Simulation.csproj", properties)
    for folder in sorted(folders):
        ET.SubElement(items, "Compile", Include=str(folder / "**/*.cs"))
    # The sample keeps a few rules of its game manager (decisions of its state machine) beside
    # the simulation folder, where Unity compiles them into the game's own assembly. They are
    # part of what a match does, so a replay needs them: here they join the simulation.
    ET.SubElement(items, "Compile", Include=str(PROJECT / "Assets/QuantumUser/*.cs"))
    for name in ("Quantum.Engine", "Quantum.Deterministic", "Quantum.Log"):
        reference(items, name, libs / (name + ".dll"))
    # Use the same addon DLL labels as Photon's Unity .NET exporter.
    addons = sorted(Path(str(meta)[:-5]) for meta in (PROJECT / "Assets").rglob("*.dll.meta")
                    if "- QuantumDotnetInclude" in meta.read_text())
    for dll in addons:
        reference(items, dll.stem, dll)
    simulation_path = OUT / "Simulation/Quantum.Simulation.csproj"
    save_project(simulation_path, simulation)

    checks_path = OUT / "CatalogChecks/Chog.HeroCatalog.Checks.csproj"
    checks, items = project_file(checks_path, {
        "TargetFramework": "net8.0", "OutputType": "Exe", "EnableDefaultCompileItems": "false",
        "TreatWarningsAsErrors": "true", "NoWarn": "CS0649"})  # CLI DTOs are populated by its serializer.
    ET.SubElement(items, "Compile", Include=str(ROOT / "tools/hero-catalog/Program.cs"))
    ET.SubElement(items, "ProjectReference", Include=str(ROOT / "shared/Chog.Heroes/Chog.Heroes.csproj"))
    save_project(checks_path, checks)

    test, items = project_file(OUT / "Tests/Chog.Quantum.Tests.csproj", {
        "TargetFramework": "net8.0", "OutputType": "Exe", "AllowUnsafeBlocks": "true",
        "EnableDefaultCompileItems": "false", "TreatWarningsAsErrors": "true"})
    ET.SubElement(items, "Compile", Include=str(ROOT / "tools/quantum/Program.cs"))
    ET.SubElement(items, "Compile", Include=str(PROJECT / "Assets/Chog/Runtime/GenesisLoadoutBinder.cs"))
    ET.SubElement(items, "ProjectReference", Include=str(simulation_path))
    ET.SubElement(items, "ProjectReference", Include=str(ROOT / "shared/Chog.Heroes/Chog.Heroes.csproj"))
    ET.SubElement(items, "ProjectReference", Include=str(checks_path), ReferenceOutputAssembly="false")
    for name in ("Quantum.Engine", "Quantum.Deterministic", "Quantum.Log"):
        reference(items, name, libs / (name + ".dll"))
    # The components of a bot hold types of the addons (the Bot SDK): a test that makes a bot
    # needs them too.
    for dll in addons:
        reference(items, dll.stem, dll)
    test_path = OUT / "Tests/Chog.Quantum.Tests.csproj"
    save_project(test_path, test)

    # The program that replays a recorded match (tools/quantum/Verifier.cs). It reads the
    # engine's replay and asset files, which needs Photon's JSON library and Newtonsoft's:
    # the first ships in the SDK archive, the second in a Unity package of the project.
    newtonsoft = sorted((PROJECT / "Library/PackageCache").glob("com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll"))
    if not newtonsoft:
        raise FileNotFoundError("Newtonsoft.Json.dll not found under game/Library/PackageCache: open the project in Unity once")
    verifier, items = project_file(OUT / "Verifier/Chog.Quantum.Verifier.csproj", {
        "TargetFramework": "net8.0", "OutputType": "Exe", "AllowUnsafeBlocks": "true", "ImplicitUsings": "enable",
        "EnableDefaultCompileItems": "false", "TreatWarningsAsErrors": "true"})
    ET.SubElement(items, "Compile", Include=str(ROOT / "tools/quantum/Verifier.cs"))
    ET.SubElement(items, "ProjectReference", Include=str(simulation_path))
    for name in ("Quantum.Engine", "Quantum.Deterministic", "Quantum.Log", "Quantum.Json"):
        reference(items, name, libs / (name + ".dll"))
    reference(items, "Newtonsoft.Json", newtonsoft[-1])
    # Its test option that hands every hero to a bot touches the bot's component.
    for dll in addons:
        reference(items, dll.stem, dll)
    verifier_path = OUT / "Verifier/Chog.Quantum.Verifier.csproj"
    save_project(verifier_path, verifier)
    print("Simulation source folders:", *[str(p.relative_to(PROJECT)) for p in folders], sep="\n  ", flush=True)
    return simulation_path, test_path, verifier_path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("build", "test", "verify"))
    parser.add_argument("files", nargs="*", type=Path, help="for verify: replay.json, asset database.json, verdict.json")
    parser.add_argument("--dotnet", type=Path)
    args = parser.parse_args()
    if args.action == "verify" and len(args.files) not in (0, 3):
        parser.error("verify takes three files (replay.json or folder of a match, asset database.json, report.json), "
                     "or none to only build the program")
    local = ROOT / ".artifacts/dotnet-sdk/dotnet"
    dotnet = args.dotnet or (local if local.is_file() else shutil.which("dotnet"))
    if not dotnet:
        parser.error("A .NET 8-compatible SDK is required; use --dotnet /path/to/dotnet.")
    simulation, tests, verifier = generate()
    env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
               DOTNET_GENERATE_ASPNET_CERTIFICATE="false", DOTNET_NOLOGO="1",
               DOTNET_CLI_USE_MSBUILD_SERVER="0", MSBUILDDISABLENODEREUSE="1",
               DOTNET_CLI_HOME=str(ROOT / ".artifacts/dotnet-home"),
               NUGET_PACKAGES=str(ROOT / ".artifacts/nuget"))
    target = tests if args.action == "test" else verifier if args.action == "verify" else simulation
    command = [str(dotnet), "build", str(target), "--configuration", "Release", "--verbosity", "minimal",
               "--disable-build-servers", "-m:1", "-p:UseSharedCompilation=false", "-p:BuildInParallel=false",
               "--configfile", str(OUT / "NuGet.Config")]
    result = subprocess.run(command, cwd=ROOT, env=env)
    if result.returncode or args.action == "build" or (args.action == "verify" and not args.files):
        return result.returncode
    if args.action == "verify":
        replay, database, verdict = (path.resolve() for path in args.files)
        return subprocess.run([str(dotnet), str(ROOT / ".artifacts/dotnet-build/bin/Chog.Quantum.Verifier/release/Chog.Quantum.Verifier.dll"),
                               str(replay), str(database), str(PROJECT / "Assets/Photon/Quantum/Resources/LUT"), str(verdict)],
                              cwd=ROOT, env=env).returncode
    check_result = subprocess.run([str(dotnet), str(ROOT / ".artifacts/dotnet-build/bin/Chog.HeroCatalog.Checks/release/Chog.HeroCatalog.Checks.dll"),
                                   "test", str(ROOT / "content/hero-catalog.prototype.json"),
                                   str(ROOT / "tests/fixtures/genesis/manifest.json")], cwd=ROOT, env=env)
    if check_result.returncode:
        return check_result.returncode
    return subprocess.run([str(dotnet), str(ROOT / ".artifacts/dotnet-build/bin/Chog.Quantum.Tests/release/Chog.Quantum.Tests.dll"),
                           str(ROOT)], cwd=ROOT, env=env).returncode


if __name__ == "__main__":
    sys.exit(main())
