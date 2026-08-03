---
name: navigation-agent-set-destination
description: Set a NavMeshAgent destination in Unity Editor Play mode through Pipeline.
---

# Navigation / Set Agent Destination

```powershell
unity command editor_play
unity command set_navmesh_agent_destination --target '{"hierarchyPath":"/Agent"}' --destination '[4,0,10]'
```

The target may be a NavMeshAgent component or a GameObject containing one. It must already be placed on a baked NavMesh.
