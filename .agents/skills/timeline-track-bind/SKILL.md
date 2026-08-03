---
name: timeline-track-bind
description: Bind a Unity Timeline track to a scene object through a PlayableDirector.
---

# Timeline / Bind Track

```powershell
unity command bind_timeline_track --timeline Assets/Timelines/Intro.playable --track Camera --director '{"hierarchyPath":"/Director"}' --binding '{"hierarchyPath":"/Main Camera"}'
```

When a track expects a Component (for example an AnimationTrack expects Animator), passing its GameObject resolves that component automatically. Use `--binding null` to clear a binding. The optional `com.unity.timeline` package must be installed.
