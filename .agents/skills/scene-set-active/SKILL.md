---
name: scene-set-active
description: Set the active open Unity scene through Unity CLI/Pipeline. Use when choosing where newly created GameObjects will be placed.
---

# Scenes / Set Active

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_active_scene --path Scenes/Main
```

## Notes

The scene must already be open. Verify the active flag using `list_open_scenes`.

