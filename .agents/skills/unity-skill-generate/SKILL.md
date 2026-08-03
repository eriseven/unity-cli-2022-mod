---
name: unity-skill-generate
description: Maintain Unity CLI/Pipeline skill files from the live command registry. Use when replacing Unity-MCP automatic skill generation with an explicit Pipeline documentation workflow.
---

# Unity Pipeline / Maintain Skills

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

Pipeline has no `unity-skill-generate` command. Use the live `unity command` listing as the source of truth, author concise SKILL.md guidance for custom commands, and validate the skill folders; do not claim automatic generation.

