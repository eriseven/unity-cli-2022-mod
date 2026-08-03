---
name: profiler-capture-frame
description: Capture a structured Unity performance frame through the Pipeline Profiler extension.
---

# Profiler / Capture Frame

Run the custom Pipeline command after the Editor is reachable:

```powershell
unity command profiler_capture_frame
```

The result combines the current render counters, memory counters, script-related counters, and recording state. It is an immediate JSON snapshot, not a Unity binary `.data` recording.
