---
name: ping
description: Check that a Unity Editor Pipeline server is reachable through the Unity CLI. Use when verifying a target instance before any operation.
---

# Pipeline / Connectivity Check

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity pipeline list
unity command editor_status
```

## Notes

`editor_status` is the Pipeline replacement for an MCP ping. If no server is reachable, open the project in Unity and verify the Pipeline package/server instead of retrying old MCP commands.

