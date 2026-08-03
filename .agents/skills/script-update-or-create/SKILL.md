---
name: script-update-or-create
description: Create or update Unity C# source and recompile through Unity CLI/Pipeline. Use when adding a MonoBehaviour or changing an existing .cs file.
---

# Scripts / Create or Update

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command create_script --name PlayerController --path Scripts --namespace Game --base_class MonoBehaviour
unity command recompile
unity command recompile_status
unity command attach_script --target '{"hierarchyPath":"/Player"}' --type Game.PlayerController
```

## Notes

For custom source content, edit the ordinary `.cs` file using source tooling (allowed), then run `recompile` and poll status. Do not edit serialized assets or .meta files; attach only after compilation succeeds.

