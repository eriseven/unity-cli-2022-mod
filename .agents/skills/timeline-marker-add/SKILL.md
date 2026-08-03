---
name: timeline-marker-add
description: Add a typed Unity Timeline marker through Pipeline.
---

# Timeline / Add Marker

```powershell
unity command add_timeline_marker --timeline Assets/Timelines/Intro.playable --track Signals --time 1.5 --marker_type UnityEngine.Timeline.Marker
```

The marker type must implement `UnityEngine.Timeline.IMarker`; the optional Timeline package is required.
