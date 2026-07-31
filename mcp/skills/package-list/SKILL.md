---
name: package-list
description: List Unity Package Manager packages through Unity CLI/Pipeline. Use when checking installed, available, or all packages.
---

# Packages / List

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command package_list --scope installed --include_indirect true
unity command package_list --scope available --offline true
```

## Notes

Use `offline true` for a cache-only query. Registry-backed scopes can block while Unity resolves packages.

