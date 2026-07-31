---
name: assets-material-create
description: Create Material assets through Unity CLI/Pipeline. Use when creating a .mat asset with a chosen shader.
---

# Assets / Create Material

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command create_asset --path Materials/Wall.mat --type UnityEngine.Material --shader "Universal Render Pipeline/Lit"
```

## Notes

Use `list_shaders` first when the shader name is uncertain. Add `--confirm true` only to overwrite an existing material.

