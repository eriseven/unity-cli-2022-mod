---
name: screenshot-game-view
description: Capture Unity Game view output through Unity CLI/Pipeline. Use when recording visual evidence from a camera render.
---

# Capture / Game View

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command capture game_view --width 1280 --height 720 --save_path Screenshots/game.png
unity command capture screenshot --view game --output Temp/pipeline-screenshots/game.png
```

## Notes

In Pipeline 0.8, `capture game_view` defaults to `source=screen` and works in Edit Mode.
Pass `--source camera` for an off-screen camera render. `capture screenshot` also writes Editor Game-view files.

