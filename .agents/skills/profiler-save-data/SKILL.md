---
name: profiler-save-data
description: Save a structured Pipeline Profiler snapshot as JSON under the configured authoring root.
---

# Profiler / Save Data

```powershell
unity command profiler_save_data --path Profiling/run-001.json
```

Use `--dry_run true` before an overwrite, and `--confirm true` only when replacing an existing snapshot. The file is a Pipeline JSON snapshot, not Unity's binary `.data` capture format.
