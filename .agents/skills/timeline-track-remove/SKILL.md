---
name: timeline-track-remove
description: Remove a Unity Timeline track and its content through Pipeline.
---

# Timeline / Remove Track

```powershell
unity command remove_timeline_track --timeline Assets/Timelines/Intro.playable --track Obsolete --dry_run true --confirm true
```

Removing a track deletes its clips and markers, so `--confirm true` is required even for the dry run.
