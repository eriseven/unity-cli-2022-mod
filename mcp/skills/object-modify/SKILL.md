---
name: object-modify
description: Set a serialized Unity field through Unity CLI/Pipeline. Use when changing a known field on an asset or component.
---

# Object / Modify Serialized Fields

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_serialized_field --target Assets/Data/GameConfig.asset --field difficulty --value 2
unity command set_serialized_field --target '{"hierarchyPath":"/Player"}' --component PlayerController --field speed --value 6
```

## Notes

Read the field first, change the minimum necessary value, and read it back. Do not use raw reflection or direct serialized-file edits for standard data.

