#!/usr/bin/env bash
# Compile-checks unity/Assets/Scripts/Game and Render against a stand-in for UnityEngine (tools/tests/UnityEngineStub.cs) and the
# real Sim, so typos show up before Unity opens. It does not run anything. Run from the repo root.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="${1:-$ROOT/build/game-check}"
rm -rf "$OUT" && mkdir -p "$OUT"
mcs -langversion:latest -target:library -warn:1 -nowarn:162,429 -out:"$OUT/Treeline.Game.dll" \
    "$ROOT/tools/tests/UnityEngineStub.cs" "$ROOT"/unity/Assets/Scripts/Sim/*.cs "$ROOT"/unity/Assets/Scripts/Game/*.cs "$ROOT"/unity/Assets/Scripts/Render/*.cs
echo "Game scripts compile."
