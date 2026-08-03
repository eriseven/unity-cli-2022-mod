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
unity command modify_particle_system --target '{"hierarchyPath":"/Effects/Smoke"}' --main '{"duration":3,"loop":false,"startSpeed":4,"maxParticles":128}' --emission '{"enabled":true,"rateOverTime":25}' --shape '{"shapeType":"Sphere","radius":2}' --noise '{"enabled":true,"strength":0.5}'
```

## Notes

Supported module fields: Main (`duration`, `loop`, `startLifetime`, `startSpeed`, `startSize`, `gravityModifier`, `simulationSpace`, `maxParticles`, `playOnAwake`), Emission (`enabled`, `rateOverTime`, `rateOverDistance`), Shape (`enabled`, `shapeType`, `radius`, `angle`, `arc`, `radiusThickness`), and Noise (`enabled`, `strength`, `frequency`, `scrollSpeed`). Read first and use generic serialized-property commands for unsupported modules.
