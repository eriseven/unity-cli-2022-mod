---
name: reflection-method-find
description: Discover callable C# method signatures on a loaded Unity type through Pipeline.
---

# Reflection / Find Methods

```powershell
unity command find_methods --type MyGame.Spawner --name Rebuild
```

Use fully-qualified type names to avoid ambiguity. Add `--include_non_public true` only when inspecting project-owned implementation details.
