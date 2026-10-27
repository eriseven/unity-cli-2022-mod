---
name: console-get-logs
description: Read recent Unity Editor console logs through Unity CLI/Pipeline. Use when diagnosing compilation, test, or runtime issues.
---

# Console / Read

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command console get --level error --tail 100
unity command console status
unity command console get --tail 100 --level warn
```

## Notes

Use `console get` for Editor or development Player diagnostics. Pipeline 0.8 removed `get_console_logs`.
When following, pass both `since` and `since_session` from the preceding response; check `reset` and
`dropped`. Use `groundTruth.compilationFailed` and native counts to distinguish historical buffered
errors from current compilation state.

