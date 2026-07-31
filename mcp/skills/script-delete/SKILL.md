---
name: script-delete
description: Delete a C# script asset and trigger Unity recompilation through Unity CLI/Pipeline. Use when removing a script intentionally.
---

# Scripts / Delete

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command delete_asset --asset Assets/Scripts/OldBehaviour.cs --dry_run true
unity command delete_asset --asset Assets/Scripts/OldBehaviour.cs --confirm true
unity command recompile
unity command recompile_status
```

## Notes

Remove component references and dependent types first. Deletion is not Undo-able; poll compilation until `completed` or `up_to_date`.

