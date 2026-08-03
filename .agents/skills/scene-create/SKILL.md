---
name: scene-create
description: Create and save Unity scenes through Unity CLI/Pipeline. Use when creating an empty or default-template scene under the authoring root.
---

# Scenes / Create

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command create_scene --path Scenes/Level1 --template default
unity command create_scene --path Scenes/AdditiveLevel --additive true --template empty
```

## Notes

Creation is blocked in Play mode. Use the returned scene identity and save or set it active explicitly as needed.

