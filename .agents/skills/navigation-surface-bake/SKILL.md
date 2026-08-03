---
name: navigation-surface-bake
description: Bake AI Navigation NavMeshSurface components through Unity CLI/Pipeline. Use when regenerating component-based navigation data.
---

# Navigation / Bake Surfaces

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command bake_navmesh_surfaces_compat
```

## Notes

This extension command invokes `NavMeshSurface.BuildNavMesh` for every active-scene surface, then returns the per-surface data-presence result. Pass `--target '{"hierarchyPath":"/NavigationSurface"}'` to bake or clear one surface, and `--clear true` to remove its baked data. Do not substitute the legacy `bake_navmesh` command: it does not build `NavMeshSurface` data.
