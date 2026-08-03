---
name: profiler-clear-data
description: Clear transient Unity Profiler frames through the Pipeline Profiler extension.
---

# Profiler / Clear Data

Discard the current in-memory Profiler frames only with explicit confirmation:

```powershell
unity command profiler_clear_data --confirm true
```

This does not delete project assets or saved JSON snapshots.
