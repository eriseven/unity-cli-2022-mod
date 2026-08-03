---
name: profiler-load-data
description: Load a structured Pipeline Profiler snapshot JSON file from the authoring root.
---

# Profiler / Load Data

```powershell
unity command profiler_load_data --path Profiling/run-001.json
```

The command parses and returns the JSON. Snapshot files are limited to 10 MiB and must stay under the configured authoring root.
