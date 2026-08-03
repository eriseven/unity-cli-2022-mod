---
name: timeline-director-bind
description: Assign a Unity Timeline asset to a scene PlayableDirector through Pipeline.
---

# Timeline / Director Bind

```powershell
unity command set_playable_director_timeline --director '{"hierarchyPath":"/CutsceneDirector"}' --timeline Assets/Timelines/Intro.playable --play_on_awake false
```

Use `--dry_run true` to validate both references first. The extension adds a missing PlayableDirector on the referenced GameObject. To bind individual output tracks, use `bind_timeline_track`.
