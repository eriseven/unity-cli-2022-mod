---
name: gameobject-duplicate
description: Duplicate one or more Unity GameObjects atomically through Pipeline.
---

# GameObject / Duplicate

```powershell
unity command duplicate_gameobject --sources '[{"hierarchyPath":"/Enemy"},{"hierarchyPath":"/Pickup"}]'
```

All input references are resolved before Unity mutates the scene. With no custom name or
parent, the command uses Unity Editor's standard duplicate operation: it preserves canonical
`(1)` naming and leaves the duplicated objects selected. The `result` array returns the source
references, matching the original MCP contract.

`--source '{"hierarchyPath":"/Enemy"}'` remains a single-object alias. `--name`, `--parent`,
and `--world_position_stays` are supported only with one source because they are Pipeline-only
placement options.
