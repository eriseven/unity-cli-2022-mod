---
name: script-read
description: Read UTF-8 C# or text files under the authoring root through Unity CLI/Pipeline. Use when inspecting source that is part of the Unity project.
---

# Scripts / Read

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command read_text_file --path Scripts/PlayerController.cs --max_bytes 1048576
```

## Notes

Use normal source-code tooling for broad code search; this command is useful when the active Unity-side authoring root matters.

