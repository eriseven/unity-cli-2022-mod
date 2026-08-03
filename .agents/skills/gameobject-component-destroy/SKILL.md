---
name: gameobject-component-destroy
description: Remove a Unity component through Unity CLI/Pipeline. Use when deleting a component by its handle or by GameObject plus type.
---

# GameObject / Remove Component

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command remove_component --target '{"hierarchyPath":"/Player"}' --type UnityEngine.Rigidbody
```

## Notes

Read the target's relevant component data before removal. The operation is Undo-able and blocked in Play mode.

