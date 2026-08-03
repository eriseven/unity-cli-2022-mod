---
name: animator-modify
description: Edit AnimatorController parameters, layers, states, and transitions through Unity CLI/Pipeline. Use when constructing or changing controller graphs.
---

# Animator / Modify

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command add_animator_parameter --controller Assets/Animation/Player.controller --name Speed --type Float --defaultValue 0
unity command add_animator_layer --controller Assets/Animation/Player.controller --name UpperBody --weight 1
unity command add_animator_state --controller Assets/Animation/Player.controller --layer "Base Layer" --name Idle --isDefault true
unity command add_animator_transition --controller Assets/Animation/Player.controller --layer "Base Layer" --fromState Idle --toState Run --conditions '[{"parameter":"Speed","mode":"Greater","threshold":0.1}]'
```

## Notes

Use `get_animator_controller` first and preserve stable state/layer names. Use `--dry_run true` on supported commands before a complex graph mutation.

