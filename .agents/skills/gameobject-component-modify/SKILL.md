---
name: gameobject-component-modify
description: Set serialized Unity component properties through Unity CLI/Pipeline. Use when changing supported component fields on a known target.
---

# GameObject / Modify Component

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_component_properties --target '{"hierarchyPath":"/Player"}' --type UnityEngine.Rigidbody --properties '{"m_Mass":2}'
unity command set_serialized_field --target '{"hierarchyPath":"/Player"}' --component UnityEngine.Rigidbody --field m_Mass --value 2
```

## Notes

Read fields first, apply one coherent change, and re-read the result. Unknown property names fail the `set_component_properties` batch.

