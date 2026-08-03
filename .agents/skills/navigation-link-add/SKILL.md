---
name: navigation-link-add
description: Add and configure an AI Navigation NavMeshLink through Unity CLI/Pipeline. Use when authoring a link component on a scene GameObject.
---

# Navigation / Add Link

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command add_component --target '{"hierarchyPath":"/JumpLink"}' --type Unity.AI.Navigation.NavMeshLink
unity command set_component_properties --target '{"hierarchyPath":"/JumpLink"}' --type Unity.AI.Navigation.NavMeshLink --properties '{"m_Bidirectional":true}'
```

## Notes

Install `com.unity.ai.navigation` first when needed. Query the component's actual serialized field names before setting additional values.

