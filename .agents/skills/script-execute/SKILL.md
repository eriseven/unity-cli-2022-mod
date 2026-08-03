---
name: script-execute
description: Evaluate a small, reviewed C# snippet or file through Unity CLI/Pipeline. Use when a one-off editor or runtime action lacks a typed command.
---

# Scripts / Execute C#

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command eval --code 'return 2 + 2;'
unity command eval_file --file Assets/Automation/OneOff.cs --timeout 5000
```

## Notes

Prefer purpose-built Pipeline commands. Keep eval code minimal, bounded, and auditable; do not use it to bypass authoring safety or to directly edit serialized Unity files.

