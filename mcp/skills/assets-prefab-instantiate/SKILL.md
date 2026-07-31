---
name: assets-prefab-instantiate
description: Instantiate Prefab assets into a loaded scene through Unity CLI/Pipeline. Use when placing a prefab instance in the active or named scene.
---

# Prefabs / Instantiate

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command instantiate_prefab --prefab Assets/Prefabs/Enemy.prefab --scene_path Assets/Scenes/Main.unity --name Enemy01
```

## Notes

Read the returned scene-object identity and use `set_transform` or component commands for follow-up configuration.

