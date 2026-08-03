---
name: screenshot-scene-view
description: Capture the active Unity Scene view through Unity CLI/Pipeline. Use when visualizing editor scene composition.
---

# Capture / Scene View

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command capture_scene_view --width 1280 --height 720 --save_path Screenshots/scene.png
unity command screenshot --view scene --output Temp/pipeline-screenshots/scene.png
```

## Notes

Use the capture result's path or inline image to perform visual QA after scene changes.

