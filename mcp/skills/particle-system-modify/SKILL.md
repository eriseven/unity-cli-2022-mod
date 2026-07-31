---
name: particle-system-modify
description: Modify supported ParticleSystem serialized fields through Unity CLI/Pipeline. Use when changing a known particle-system setting.
---

# Particles / Modify

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_component_properties --target '{"hierarchyPath":"/Effects/Smoke"}' --type UnityEngine.ParticleSystem --properties '{"m_PlayOnAwake":true}'
unity command set_serialized_field --target '{"hierarchyPath":"/Effects/Smoke"}' --component UnityEngine.ParticleSystem --field m_PlayOnAwake --value true
```

## Notes

Read first and use actual serialized field paths from the target. Add a typed command for complex module operations that are not exposed by generic serialized-property support.

