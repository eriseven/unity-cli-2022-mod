---
name: unity-skill-create
description: Create a custom Unity Pipeline CLI command in C#. Use when built-in Pipeline commands do not cover a repeatable Unity authoring or runtime operation.
---

# Unity Pipeline / Create Custom Command

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command set_autotick --enable true
unity command recompile
unity command recompile_status
unity command
```

## Notes

Declare a static method with `[CliCommand]` and describe arguments with `[CliArg]`. For authoring commands, use `ProjectPaths`, `ObjectRef`, `AuthoringResult`, and `AuthoringUndoScope`; add `confirm`/`dry_run` to destructive or overwriting work. Place code in a compatible Editor assembly, recompile, poll completion, then confirm registration with `unity command`.

