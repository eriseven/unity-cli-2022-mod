---
name: unity-tool-list
description: List all commands registered by a connected Unity Pipeline instance. Use when discovering command names and argument descriptions for the current Editor or Player.
---

# Unity Pipeline / List Commands

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command
unity command --runtime MyGame.exe
```

## Notes

Run without a command name to retrieve the target's actual registry. Use `--runtime` or `--runtime-path` before the command name for a development Player.

