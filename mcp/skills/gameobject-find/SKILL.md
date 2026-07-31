---
name: gameobject-find
description: Find loaded Unity GameObjects through Unity CLI/Pipeline. Use when resolving objects by name, tag, component type, or hierarchy path.
---

# GameObject / Find

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command find_gameobjects --name Player --include_inactive true
unity command find_gameobjects --type UnityEngine.Camera --hierarchy_path /Environment/MainCamera
```

## Notes

Filters combine. Persist the returned `instanceId` or `hierarchyPath` rather than resolving by name again after a mutation.

