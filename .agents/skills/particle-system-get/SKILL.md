---
name: particle-system-get
description: Inspect ParticleSystem serialized component data through Unity CLI/Pipeline. Use when reading a particle emitter's supported configuration.
---

# Particles / Inspect

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_particle_system --target '{"hierarchyPath":"/Effects/Smoke"}' --include_noise true
```

## Notes

Returns runtime state plus Main, Emission, Shape, and optionally Noise module data. Use `particle-system-modify` for supported module edits; use generic serialized-property commands only for fields outside this typed surface.
