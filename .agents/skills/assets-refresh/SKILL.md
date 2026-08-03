---
name: assets-refresh
description: Refresh Unity's AssetDatabase through Unity CLI/Pipeline. Use when an explicit refresh is necessary after an allowed text-file change.
---

# Assets / Refresh

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command eval --code 'UnityEditor.AssetDatabase.Refresh(); return "refreshed";'
```

## Notes

Prefer a dedicated Pipeline command, which imports its own result, over a standalone refresh. Keep eval snippets minimal and do not use them to edit serialized assets or .meta files.

