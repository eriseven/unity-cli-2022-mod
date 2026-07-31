---
name: gameobject-duplicate
description: Duplicate a Unity GameObject with hierarchy and components through Pipeline.
---

# GameObject / Duplicate

```powershell
unity command duplicate_gameobject --source '{"hierarchyPath":"/Enemy"}' --name EnemyCopy
```

Optionally supply `--parent` and `--world_position_stays true`. The command is Undo-aware and intentionally refuses to mutate scenes during Play mode.
