---
name: tool-set-enabled-state
description: Inspect or control Unity Pipeline command availability. Use when an old Unity-MCP workflow would enable or disable registered tools.
---

# Pipeline / Command Availability

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command
```

## Notes

Pipeline discovers every compiled `[CliCommand]` after a domain reload and has no runtime enable/disable registry. Inspect availability with `unity command`; control custom-command availability through code/package configuration, not an invented toggle command.

