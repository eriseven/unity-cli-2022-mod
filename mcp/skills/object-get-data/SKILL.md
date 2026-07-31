---
name: object-get-data
description: Read serialized fields from a Unity asset or component through Unity CLI/Pipeline. Use when inspecting a known ObjectRef.
---

# Object / Inspect Serialized Fields

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_serialized_fields --target Assets/Data/GameConfig.asset
unity command get_serialized_fields --target '{"hierarchyPath":"/Player"}' --component PlayerController
```

## Notes

Use type-specific commands where available. The returned object-reference handles are reusable in later CLI calls.

