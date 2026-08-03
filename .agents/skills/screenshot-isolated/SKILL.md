---
name: screenshot-isolated
description: Render one Unity GameObject in an isolated temporary preview scene through Pipeline.
---

# Capture / Isolated Object

Capture a GameObject from an isolated, disposable preview scene. The command never saves or edits the source scene.

```powershell
unity command capture_isolated_object --target '{"hierarchyPath":"/Enemy"}' --resolution 1024 --camera_view Front --background_mode SolidColor --background_color '#20242C' --padding 1.15 --save_path Screenshots/enemy.png
```

## Composition controls

Use `--camera_view` to choose `Front`, `Back`, `Left`, `Right`, `Top`, `Bottom`, or `Composite`. `Composite` renders Front, Right, Back, and Top views into a 2×2 image; `--resolution` is the size of each quadrant.

```powershell
unity command capture_isolated_object --target '{"hierarchyPath":"/Enemy"}' --resolution 512 --camera_view Composite --background_mode Transparent --padding 1.2 --field_of_view 45 --near_clip_plane 0.01 --far_clip_plane 500 --save_path Screenshots/enemy-composite.png
```

Use `--background_mode SolidColor`, `Skybox`, or `Transparent`; `--background_color` accepts `#RRGGBB` or `#RRGGBBAA`. The legacy `--transparent_background` option remains available when `--background_mode` is omitted.

`--isolated false` clones the complete source-scene context into the preview scene, while `--include_children false` captures only the target root. Both still leave the source scene unchanged.

## Lighting

The default is one directional light. Pass `--lights '[]'` for no additional lights, or provide a JSON array of light specifications:

```powershell
unity command capture_isolated_object --target '{"hierarchyPath":"/Enemy"}' --camera_view Right --background_mode SolidColor --background_color '#101820' --lights '[{"type":"Directional","color":"#FFF4E5","intensity":1.2,"rotation":[45,-45,0]},{"type":"Point","color":"#7FB3FF","intensity":0.5,"position":[2,2,-2],"range":10}]' --save_path Screenshots/enemy-lit.png
```

Supported light fields are `type` (`Directional`, `Point`, `Spot`, `Area`, `Disc`), `color`, `intensity`, `rotation`, `position`, `range`, `spotAngle`, `innerSpotAngle`, `shadows`, `shadowStrength`, `bounceIntensity`, `colorTemperature`, `cookieSize`, `cullingMask`, and `renderMode`.

The PNG is returned inline by default and can also be written below `Assets/` with `--save_path`. A GPU is required.
