---
name: gameobject-modify
description: Modify Unity GameObject transform, active state, tag, layer, or name through Unity CLI/Pipeline. Use when changing a scene object without arbitrary reflection.
---

# GameObject / Modify

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_transform --target '{"hierarchyPath":"/Player"}' --position '[0,1,0]'
unity command set_active --target '{"hierarchyPath":"/Player"}' --active true
unity command set_tag --target '{"hierarchyPath":"/Player"}' --tag Player
unity command set_layer --target '{"hierarchyPath":"/Player"}' --layer Gameplay
unity command rename_gameobject --target '{"hierarchyPath":"/Player"}' --name Hero
```

## Notes

Use the smallest specialized command and re-read the object afterward. All listed scene mutations are Undo-able and blocked in Play mode.

