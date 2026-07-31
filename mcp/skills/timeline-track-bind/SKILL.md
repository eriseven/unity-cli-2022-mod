---
name: timeline-track-bind
description: Bind a Unity Timeline track to a scene object through a PlayableDirector.
---

# Timeline / Bind Track

```powershell
unity command bind_timeline_track --timeline Assets/Timelines/Intro.playable --track Camera --director '{"hierarchyPath":"/Director"}' --binding '{"hierarchyPath":"/Main Camera"}'
```

Use `--binding null` to clear a binding. The optional `com.unity.timeline` package must be installed.
