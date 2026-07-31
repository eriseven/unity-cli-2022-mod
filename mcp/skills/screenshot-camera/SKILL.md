---
name: screenshot-camera
description: Render a Unity camera to a PNG through Unity CLI/Pipeline. Use when capturing a named camera's output for inspection.
---

# Capture / Camera

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command capture_game_view --camera MainCamera --width 1280 --height 720 --save_path Screenshots/main.png
```

## Notes

Use `include_inline_image true` only when the caller needs base64 in addition to the saved file. Verify the resulting image visually.

