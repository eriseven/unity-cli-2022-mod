---
name: navigation-set-bake-settings
description: Read or change legacy NavMesh bake settings through Unity CLI/Pipeline. Use when configuring project-level NavMesh bake parameters.
---

# Navigation / Set Bake Settings

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_navmesh_settings
unity command set_navmesh_settings --settings '{"agentRadius":0.5,"agentHeight":2}' --dry_run true
unity command set_navmesh_settings --settings '{"agentRadius":0.5,"agentHeight":2}'
```

## Notes

Use the legacy settings pair for bake parameters. Use `bake_navmesh_surfaces` for AI Navigation `NavMeshSurface` components.

