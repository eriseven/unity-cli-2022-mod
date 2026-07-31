---
name: assets-prefab-create
description: Create a Prefab asset from a scene GameObject through Unity CLI/Pipeline. Use when saving a configured GameObject as a .prefab.
---

# Prefabs / Create

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command create_prefab --source '{"hierarchyPath":"/Enemy"}' --path Prefabs/Enemy.prefab
```

## Notes

The scene-side connection is Undo-able, but the prefab asset write is not. Inspect the source object before saving.

