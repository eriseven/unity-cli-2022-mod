---
name: animation-create
description: Create Unity AnimationClip assets through Unity CLI/Pipeline. Use when creating empty .anim clips with frame-rate or looping options.
---

# Animation / Create

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command create_animation_clip --path Animations/Walk.anim --frameRate 60 --loop true
unity command create_animation_clip --path Animations/Walk.anim --confirm true  # overwrite only
```

## Notes

Use a path under the authoring root. Create curves separately with `set_animation_curve`, then read the clip back before continuing.

