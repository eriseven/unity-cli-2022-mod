---
name: editor-application-set-state
description: Control Unity Editor play, stop, pause, focus, and ticking state through Unity CLI/Pipeline. Use when changing editor lifecycle state.
---

# Editor / Set State

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command editor_play
unity command editor_pause
unity command editor_stop
unity command set_autotick --enable true
```

## Notes

Use `set_autotick` before headless compilation or tests and re-enable it after every domain reload if more headless work remains.

