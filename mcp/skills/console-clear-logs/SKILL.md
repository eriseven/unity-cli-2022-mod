---
name: console-clear-logs
description: Clear captured Unity Editor console logs through Unity CLI/Pipeline. Use when beginning a clean diagnostic pass.
---

# Console / Clear

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command clear_console
```

## Notes

Clear only when a fresh diagnostic baseline is intended; otherwise use the log cursor or severity filter to distinguish new errors.

