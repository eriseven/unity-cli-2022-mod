---
name: gameobject-create
description: Create empty Unity GameObjects or primitives through Unity CLI/Pipeline. Use when adding scene objects with an optional parent and transform.
---

# GameObject / Create

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command create_gameobject --name Cube --primitive cube --parent '{"hierarchyPath":"/Environment"}'
unity command set_transform --target '{"hierarchyPath":"/Environment/Cube"}' --position '[0,1,0]' --rotation '[0,45,0]' --scale '[1,1,1]'
```

## Notes

Create first, then set transform explicitly. Pipeline uses local transform values and scene mutations are Undo-able outside Play mode.

