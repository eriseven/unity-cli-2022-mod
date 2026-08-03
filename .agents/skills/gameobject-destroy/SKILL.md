---
name: gameobject-destroy
description: Delete Unity GameObjects through Unity CLI/Pipeline. Use when removing a scene object while retaining Unity Undo support.
---

# GameObject / Delete

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command delete_gameobject --target '{"hierarchyPath":"/Environment/OldProp"}'
```

## Notes

Inspect the exact target first. This is Undo-able and intentionally has no old MCP confirmation flag.

