---
name: assets-find-built-in
description: Search Unity Editor built-in resources through Pipeline.
---

# Assets / Find Built-in

```powershell
unity command find_builtin_assets --name "Default" --type UnityEngine.Material --max_results 10
```

Built-in resources have no project GUID. Results return their type, Unity resource path, and a best-effort object reference.
