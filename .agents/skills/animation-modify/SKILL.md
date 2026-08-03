---
name: animation-modify
description: Add, replace, or remove AnimationClip float curves through Unity CLI/Pipeline. Use when editing animation curve bindings or keyframes.
---

# Animation / Modify

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
unity command set_animation_clip_metadata --clip Assets/Animations/Walk.anim --frame_rate 30 --loop true --wrap_mode Loop --legacy false
unity command add_animation_event --clip Assets/Animations/Walk.anim --time 0.5 --function_name OnFootstep --int_parameter 1 --string_parameter left
unity command clear_animation_events --clip Assets/Animations/Walk.anim --dry_run true
unity command clear_animation_events --clip Assets/Animations/Walk.anim --confirm true
```

## Notes

Use `dry_run` before destructive removals. Float curves, metadata, and AnimationEvents are supported; object-reference curves remain read-only.
