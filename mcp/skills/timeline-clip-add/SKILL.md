---
name: timeline-clip-add
description: Add a clip to a named Unity Timeline track through Unity CLI/Pipeline. Use when composing an Animation or Audio clip at a known time and duration.
---

# Timeline / Add Clip

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command add_timeline_clip --timeline Assets/Timelines/Intro.playable --track Animation --start 0 --duration 2 --asset Assets/Animations/Walk.anim
```

## Notes

Require `com.unity.timeline`. Read the Timeline first and use its exact track names.

