---
name: assets-get-data
description: Read serialized fields or importer settings for Unity assets through Unity CLI/Pipeline. Use when inspecting a project asset's editable data.
---

# Assets / Inspect Serialized Data

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_serialized_fields --target Assets/Data/GameConfig.asset
unity command get_import_settings --asset Assets/Textures/Icon.png --platform Default
```

## Notes

Use the type-specific commands (`get_material_properties`, `get_animation_clip`, and similar) when available. Do not read or edit Unity serialized asset files directly.

