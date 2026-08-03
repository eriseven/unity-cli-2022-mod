---
name: scene-get-data
description: Read an open Unity scene hierarchy through Unity CLI/Pipeline. Use when resolving GameObjects and their reusable identities.
---

# Scenes / Inspect Hierarchy

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_scene_hierarchy
unity command get_scene_hierarchy --path Assets/Scenes/Main.unity
```

## Notes

Pipeline returns the hierarchy rather than arbitrary scene YAML. Use its `instanceId`/`hierarchyPath` values in follow-up GameObject commands.

