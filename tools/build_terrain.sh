#!/usr/bin/env bash
# Regenerates the terrain textures, materials, MeshLibrary and test map.
# Usage: tools/build_terrain.sh            (GODOT env var overrides the binary)
set -euo pipefail
cd "$(dirname "$0")/.."
GODOT="${GODOT:-godot}"

"$GODOT" --headless --build-solutions --quit
"$GODOT" --headless res://tools/terrain/TerrainBuilder.tscn -- textures
"$GODOT" --headless --import
"$GODOT" --headless res://tools/terrain/TerrainBuilder.tscn -- library
"$GODOT" --headless res://tools/terrain/TerrainBuilder.tscn -- map
"$GODOT" --headless --import
