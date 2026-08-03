---
name: timeline-track-list
description: Read Unity Timeline tracks through Unity CLI/Pipeline. Use when selecting an existing track for a follow-up clip or custom binding change.
---

# Timeline / List Tracks

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_timeline_details --timeline Assets/Timelines/Intro.playable --include_clips true --include_markers true
```

## Notes

Pipeline Extensions returns compatible track details, including root index, mute/lock state, clips, source asset paths, and markers.
