---
name: timeline-modify
description: Create and modify Unity Timeline tracks, clips, markers, and bindings through Pipeline.
---

# Timeline / Modify

```powershell
unity command add_timeline_track --timeline Assets/Timelines/Intro.playable --trackType Animation --name Animation
unity command add_timeline_clip --timeline Assets/Timelines/Intro.playable --track Animation --start 0 --duration 2 --asset Assets/Animations/Walk.anim
unity command set_timeline_clip_timing --timeline Assets/Timelines/Intro.playable --track Animation --clip_index 0 --start 1 --duration 2
```

For track removal use `remove_timeline_track --confirm true`; markers and PlayableDirector bindings use `add_timeline_marker`, `set_playable_director_timeline`, and `bind_timeline_track`. The Timeline package is optional and must be installed.
