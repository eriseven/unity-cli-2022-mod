---
name: assets-prefab-open
description: Open a prefab asset or instance in Unity's Prefab Stage through Pipeline.
---

# Prefabs / Open Stage

```powershell
unity command open_prefab_stage --prefab Assets/Prefabs/Enemy.prefab
```

The referenced prefab must lie under the configured Pipeline authoring root. The response includes `ContentsRoot`, the editable Prefab Stage root.

While the stage is open, use the extension command below to add a child. It defaults to `ContentsRoot`, so it cannot accidentally modify the scene instance that opened the stage:

```powershell
unity command create_prefab_stage_gameobject --name WeaponSocket
```

For unattended declarative changes, `save_prefab_contents` remains the preferred command.
