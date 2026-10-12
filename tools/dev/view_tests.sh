#!/bin/sh
# The tests of the view code (game/Assets/Chog/Match/Tests), compiled and run outside Unity.
#
#   sh tools/dev/view_tests.sh
#
# The Editor can stay open and nothing in the project is touched. Each assembly is compiled
# with the arguments the Editor last used (tools/dev/typecheck.py), in dependency order, then
# the NUnit tests are run by a small reflection runner (TestRunner.cs): the Unity Test Runner
# is not involved, so a test that needs a running engine (a scene, a frame) cannot run here.
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$HERE/../.." && pwd)
OUT="$ROOT/.artifacts/typecheck"
SCRIPTING=/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting
DOTNET="$SCRIPTING/DotNetSdk/dotnet"

python3 "$HERE/typecheck.py" Quantum.Simulation
python3 "$HERE/typecheck.py" Chog.Diagnostics --use Quantum.Simulation --sources Assets/Chog/Diagnostics
python3 "$HERE/typecheck.py" Chog.Match --unsafe --use Quantum.Simulation Chog.Diagnostics --sources Assets/Chog/Match
python3 "$HERE/typecheck.py" Chog.Match.Tests --use Chog.Match Quantum.Simulation --sources Assets/Chog/Match/Tests

if [ ! -f "$OUT/TestRunner.dll" ] || [ "$HERE/TestRunner.cs" -nt "$OUT/TestRunner.dll" ]; then
  "$DOTNET" "$SCRIPTING/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll" -nologo -target:exe \
    -r:"$SCRIPTING/NetStandard/ref/2.1.0/netstandard.dll" -out:"$OUT/TestRunner.dll" "$HERE/TestRunner.cs"
  cp "$HERE/TestRunner.runtimeconfig.json" "$OUT/TestRunner.runtimeconfig.json"
fi
# What the tests load at run time besides the assemblies compiled above.
cp "$ROOT"/game/Library/PackageCache/com.unity.ext.nunit@*/net472/unity-custom/nunit.framework.dll "$OUT/"
cp "$SCRIPTING/Managed/UnityEngine/UnityEngine.CoreModule.dll" "$OUT/"
"$DOTNET" "$OUT/TestRunner.dll" "$OUT/Chog.Match.Tests.dll"
