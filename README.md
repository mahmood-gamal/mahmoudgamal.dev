# Ragna

A Ragnarok-style 3D action RPG built with **Godot 4.7 (.NET)** and C#.

## Requirements

- Godot 4.7.x, .NET edition
- .NET 8 SDK

## Build

```sh
godot --headless --build-solutions --quit
```

Or, without the editor:

```sh
dotnet build
dotnet test
```

## Projects

| Project | Path | Notes |
| --- | --- | --- |
| `Ragna` | `Ragna.csproj` | Godot game assembly (scenes, nodes, UI) |
| `Ragna.Core` | `scripts/core/Ragna.Core.csproj` | Engine-agnostic game logic. Plain .NET SDK with no GodotSharp reference, so `using Godot;` does not compile here |
| `Ragna.Core.Tests` | `tests/Ragna.Core.Tests/` | xUnit tests for `Ragna.Core` |

## Layout

| Path | Purpose |
| --- | --- |
| `scenes/` | Godot scenes (`Main.tscn` is the entry scene) |
| `scripts/core/` | Engine-agnostic core logic and shared types (no Godot dependency) |
| `scripts/gameplay/` | Player, monsters, combat, items |
| `scripts/ui/` | HUD, inventory and menu scripts |
| `scripts/dialogue/` | Dialogue runner and scene UI |
| `data/` | Data files (JSON dialogue, item and monster tables) |
| `assets/` | Art, audio, fonts |
| `scenes/maps/` | Playable maps (`TestMap.tscn` is generated, see [docs/terrain.md](docs/terrain.md)) |
| `assets/terrain/` | Terrain MeshLibrary, materials and textures |
| `tools/` | Asset build scripts (`tools/build_terrain.sh`) |
| `docs/` | Guides, for example [terrain and GridMap painting](docs/terrain.md) |
| `tests/` | xUnit test projects (excluded from the game assembly, ignored by Godot) |

## C# conventions

File-scoped namespaces, records for immutable data, and nullable reference types enabled. Warnings are treated as errors.
