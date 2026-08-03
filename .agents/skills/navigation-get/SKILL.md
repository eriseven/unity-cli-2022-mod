---
name: navigation-get
description: Inspect NavMesh bake settings or navigation component serialized data through Unity CLI/Pipeline. Use when reading navigation configuration.
---

# Navigation / Inspect

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_navmesh_settings
unity command get_component_properties --target '{"hierarchyPath":"/Enemy"}' --type UnityEngine.AI.NavMeshAgent
```

## Notes

Use `get_navmesh_settings` for legacy bake settings and component inspection for agent/surface/link configuration.

