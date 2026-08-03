---
name: assets-modify
description: Modify Unity asset serialized fields or importer settings through Unity CLI/Pipeline. Use when changing supported asset data without editing serialized files.
---

# Assets / Modify

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_serialized_field --target Assets/Data/GameConfig.asset --field difficulty --value 2
unity command set_import_settings --asset Assets/Textures/Icon.png --settings '{"isReadable":true}' --dry_run true
```

## Notes

Use the relevant typed command for materials, animation, or import settings. Read the target first and re-read it after mutation.

