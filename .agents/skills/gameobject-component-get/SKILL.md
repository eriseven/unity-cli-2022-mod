---
name: gameobject-component-get
description: Read serialized Unity component properties through Unity CLI/Pipeline. Use when inspecting a known component on a GameObject.
---

# GameObject / Get Component Properties

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command get_component_properties --target '{"hierarchyPath":"/Player"}' --type UnityEngine.Rigidbody
unity command get_serialized_fields --target '{"hierarchyPath":"/Player"}' --component UnityEngine.Rigidbody
```

## Notes

Use `get_component_properties` for the component property map and `get_serialized_fields` for field paths and values.

