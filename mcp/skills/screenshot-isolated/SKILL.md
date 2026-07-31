---
name: screenshot-isolated
description: Render one Unity GameObject in an isolated temporary preview scene through Pipeline.
---

# Capture / Isolated Object

```powershell
unity command capture_isolated_object --target '{"hierarchyPath":"/Enemy"}' --width 1024 --height 1024 --save_path Screenshots/enemy.png
```

The source object and its scene are not changed: Pipeline clones it into a preview scene, renders it, then cleans up. A GPU is required.
