---
name: type-get-json-schema
description: Inspect a loaded C# type's fields, properties, and methods through Pipeline reflection metadata.
---

# Reflection / Type Schema

```powershell
unity command get_type_schema --type MyGame.Spawner
```

The result is JSON-friendly reflection metadata rather than Unity's serialized-asset schema. Use fully-qualified names; non-public members are opt-in.
