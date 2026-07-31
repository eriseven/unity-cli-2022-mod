---
name: scene-unload
description: Close a loaded additive Unity scene safely through Pipeline.
---

# Scenes / Unload

```powershell
unity command unload_scene --path Assets/Scenes/Overlay.unity --dry_run true
unity command unload_scene --path Assets/Scenes/Overlay.unity --save_before_unload true
```

The active scene cannot be closed. A dirty scene must either be saved with `save_before_unload` or explicitly discarded with `--confirm true`.
