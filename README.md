# Ragna

A Ragnarok-style 3D action RPG built with **Godot 4.7 (.NET)** and C#.

## Requirements

- Godot 4.7.x, .NET edition
- .NET 8 SDK

## Build

```sh
godot --headless --build-solutions --quit
```

## Layout

| Path | Purpose |
| --- | --- |
| `scenes/` | Godot scenes (`Main.tscn` is the entry scene) |
| `scripts/core/` | Engine-agnostic core logic and shared types |
| `scripts/gameplay/` | Player, monsters, combat, items |
| `scripts/ui/` | HUD, inventory and menu scripts |
| `scripts/dialogue/` | Dialogue runner and scene UI |
| `data/` | Data files (JSON dialogue, item and monster tables) |
| `assets/` | Art, audio, fonts |
| `tests/` | Test projects (excluded from the game assembly, ignored by Godot) |

## C# conventions

File-scoped namespaces, records for immutable data, and nullable reference types enabled. Warnings are treated as errors.
