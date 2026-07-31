---
name: assets-shader-get-data
description: Inspect shader property metadata through Unity CLI/Pipeline. Use when discovering valid names and types before setting material properties.
---

# Shaders / Inspect Properties

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_shader_properties --shader "Universal Render Pipeline/Lit"
unity command get_shader_properties --material Assets/Materials/Wall.mat
```

## Notes

Provide either a shader name or a material reference. Use the returned property list rather than guessing property identifiers.

