---
name: assets-delete
description: Delete Unity assets through Unity CLI/Pipeline with an explicit destructive-operation gate. Use when removing project assets.
---

# Assets / Delete

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command delete_asset --asset Assets/Materials/Old.mat --dry_run true
unity command delete_asset --asset Assets/Materials/Old.mat --confirm true
```

## Notes

Deletion is not Unity-Undo reversible. Confirm the resolved target from the dry run before applying it.

