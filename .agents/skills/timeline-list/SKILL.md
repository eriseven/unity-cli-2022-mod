---
name: timeline-list
description: Find Unity Timeline assets through Unity CLI/Pipeline. Use when locating .playable TimelineAsset files in the authoring root.
---

# Timeline / List

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command find_assets --type TimelineAsset --search_in Timelines --limit 200
```

## Notes

Require `com.unity.timeline`. Inspect an individual result with `get_timeline`.

