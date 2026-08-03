---
name: gameobject-component-list-all
description: List concrete Unity Component types available to the current project through Pipeline.
---

# GameObject / List Component Types

```powershell
unity command list_component_types --search NavMesh --page 0 --page_size 50
```

This discovers component types across loaded assemblies. To inspect a particular object's known component data, use the existing component property commands with that object's `ObjectRef`.
