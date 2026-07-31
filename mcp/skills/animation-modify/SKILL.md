---
name: animation-modify
description: Add, replace, or remove AnimationClip float curves through Unity CLI/Pipeline. Use when editing animation curve bindings or keyframes.
---

# Animation / Modify Curves

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_animation_curve --clip Assets/Animations/Walk.anim --path "" --type Transform --property m_LocalPosition.x --keys '[{"time":0,"value":0},{"time":1,"value":2}]'
unity command remove_animation_curve --clip Assets/Animations/Walk.anim --path "" --type Transform --property m_LocalPosition.x --dry_run true
unity command remove_animation_curve --clip Assets/Animations/Walk.anim --path "" --type Transform --property m_LocalPosition.x --confirm true
```

## Notes

Use `dry_run` before deleting a binding. The command supports float curves; add an explicit custom CLI command for unsupported animation features such as events or object-reference curves.

