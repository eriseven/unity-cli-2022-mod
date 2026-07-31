---
name: unity-initial-setup
description: Install and verify Unity Pipeline CLI control for a Unity project. Use when replacing Unity-MCP setup with the supported Unity CLI workflow.
---

# Unity Pipeline / Initial Setup

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity pipeline install --project-path .
unity pipeline list
unity command --project-path .
unity command --project-path . editor_status
```

## Notes

Open the project in Unity after installation; the Editor server starts with the package. Do not install `unity-mcp-cli` or configure MCP tools for this Pipeline workflow.

