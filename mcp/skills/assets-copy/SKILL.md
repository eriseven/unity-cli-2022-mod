---
name: assets-copy
description: Copy Unity assets through Unity CLI/Pipeline. Use when duplicating an asset to a new authoring-root path while receiving a new GUID.
---

# Assets / Copy

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command copy_asset --asset Assets/Materials/Source.mat --destination Materials/Copy.mat --dry_run true
unity command copy_asset --asset Assets/Materials/Source.mat --destination Materials/Copy.mat --confirm true
```

## Notes

Copy one source/destination pair per call and verify the returned identity. Asset copies are not undoable; `confirm` is required only when the destination already exists.

