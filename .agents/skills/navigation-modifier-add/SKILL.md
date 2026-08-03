---
name: navigation-modifier-add
description: Add and configure an AI Navigation NavMeshModifier through Unity CLI/Pipeline. Use when authoring navigation area overrides.
---

# Navigation / Add Modifier

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command add_component --target '{"hierarchyPath":"/Obstacle"}' --type Unity.AI.Navigation.NavMeshModifier
unity command get_component_properties --target '{"hierarchyPath":"/Obstacle"}' --type Unity.AI.Navigation.NavMeshModifier
```

## Notes

Require the AI Navigation package. Set only fields discovered from the component property read.

