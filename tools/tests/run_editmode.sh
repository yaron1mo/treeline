#!/usr/bin/env bash
# Runs the Unity EditMode tests with Mono, built the way Unity builds them: Treeline.Sim as its own assembly
# (so internal members are not visible to the tests), the tests referencing it. Run from the repo root.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${1:-$ROOT/build/editmode}"
rm -rf "$OUT" && mkdir -p "$OUT/sim"
for f in "$ROOT"/unity/Assets/Scripts/Sim/*.cs; do sed 's/readonly struct/struct/' "$f" > "$OUT/sim/$(basename "$f")"; done
mcs -langversion:latest -target:library -warn:1 -out:"$OUT/Treeline.Sim.dll" "$OUT"/sim/*.cs
mcs -langversion:latest -warn:1 -r:"$OUT/Treeline.Sim.dll" -out:"$OUT/editmode-tests.exe" \
    "$ROOT/tools/tests/NUnitStub.cs" "$ROOT"/unity/Assets/Tests/EditMode/*.cs
cd "$ROOT"
TREELINE_ASSETS="$ROOT/unity/Assets" MONO_PATH="$OUT" mono "$OUT/editmode-tests.exe"
