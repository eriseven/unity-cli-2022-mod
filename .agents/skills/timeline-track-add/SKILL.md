---
name: timeline-track-add
description: Add a Unity Timeline track through Unity CLI/Pipeline. Use when composing Animation, Audio, Activation, Control, Playable, Signal, or Marker tracks.
---

# Timeline / Add Track

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command add_timeline_track --timeline Assets/Timelines/Intro.playable --trackType Animation --name Character
unity command add_timeline_track --timeline Assets/Timelines/Intro.playable --trackType Audio --name Music --parentTrack Group
```

## Notes

Require `com.unity.timeline`. Read existing tracks first, particularly before nesting under a parent track.

