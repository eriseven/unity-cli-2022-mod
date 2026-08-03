---
name: scene-save
description: Save one or all open Unity scenes through Unity CLI/Pipeline. Use when persisting scene edits after verification.
---

# Scenes / Save

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command save_scene
unity command save_scene --path Scenes/Main
unity command save_all
```

## Notes

Save only the intended dirty scenes and verify their state with `list_open_scenes`.

