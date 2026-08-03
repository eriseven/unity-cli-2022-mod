---
name: navigation-modify
description: Modify supported navigation component fields through Unity CLI/Pipeline. Use when configuring a known NavMesh agent, link, modifier, or surface.
---

# Navigation / Modify Component

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_component_properties --target '{"hierarchyPath":"/Enemy"}' --type UnityEngine.AI.NavMeshAgent --properties '{"m_Speed":4}'
unity command set_serialized_field --target '{"hierarchyPath":"/Enemy"}' --component UnityEngine.AI.NavMeshAgent --field m_Speed --value 4
```

## Notes

Use the component-specific commands first and inspect before/after. Use a custom command for dynamic runtime behavior that is not serialized.

