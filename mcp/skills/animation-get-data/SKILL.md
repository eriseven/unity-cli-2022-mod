---
name: animation-get-data
description: Inspect AnimationClip metadata and curve bindings through Unity CLI/Pipeline. Use when reading an existing .anim asset.
---

# Animation / Inspect

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_animation_clip --clip Assets/Animations/Walk.anim --includeKeys true
```

## Notes

Pass the returned asset identity or a path/guid/globalId handle. Do not parse or edit .anim YAML directly.

