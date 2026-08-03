---
name: timeline-clip-move
description: Move a Timeline clip to a new start time through Pipeline.
---

# Timeline / Move Clip

```powershell
unity command move_timeline_clip --timeline Assets/Timelines/Intro.playable --track Camera --clip_index 0 --new_start 2.5 --dry_run true
```

Remove `--dry_run` to save. The optional `com.unity.timeline` package must be installed.
