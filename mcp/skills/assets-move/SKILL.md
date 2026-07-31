---
name: assets-move
description: Move or rename Unity assets through Unity CLI/Pipeline while preserving GUIDs. Use when relocating an existing asset.
---

# Assets / Move or Rename

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command move_asset --asset Assets/Materials/Old.mat --destination Materials/New.mat --dry_run true
unity command move_asset --asset Assets/Materials/Old.mat --destination Materials/New.mat
```

## Notes

Do not move files under `Assets/` with filesystem commands. Pipeline preserves Unity references and GUIDs.

