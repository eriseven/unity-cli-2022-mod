---
name: package-remove
description: Remove a Unity Package Manager package through Unity CLI/Pipeline. Use when uninstalling a package with an explicit confirmation gate.
---

# Packages / Remove

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command package_remove --name com.unity.timeline --dry_run true
unity command package_remove --name com.unity.timeline --confirm true
unity command package_status
unity command recompile_status
```

## Notes

Poll both status commands after applying a removal. Do not edit `Packages/manifest.json` manually for this workflow.

