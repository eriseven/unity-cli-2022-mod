---
name: type-get-json-schema
description: Generate a compatibility JSON Schema for a loaded C# type through Pipeline.
---

# Type / Get JSON Schema

```powershell
unity command get_json_schema --type MyGame.Spawner
```

The response has a `result` string containing a JSON Schema compatible with the original
`type-get-json-schema` skill. For example, `UnityEngine.Vector3` produces object properties
`x`, `y`, and `z`, marks them required, and disables additional properties.

Use `--include_nested_types true` to emit `$defs` for nested complex types and
`--write_indented true` for formatted output. For method/property reflection metadata, use
`get_type_schema` instead.
