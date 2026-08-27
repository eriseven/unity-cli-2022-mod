# Unity 2022.3.62f3 compatibility notes

This document records the local compatibility changes made to run `com.unity.pipeline`
`0.5.0-exp.1` in an empty Unity `2022.3.62f3` project.

The upstream package declares `"unity": "6000.0"` and uses several Unity 6-era API and plugin import
assumptions. Unity 2022 can compile most of the package, but it needs explicit adjustments around
Roslyn dependencies, renamed Unity APIs, test assembly references, and optional packages.

## Scope

The changes are limited to `Packages/com.unity.pipeline`.

The goal is compile compatibility with Unity `2022.3.62f3`, not a behavioral rewrite of the package.
Unity Editor has confirmed the project reloads with no compile errors after these changes.

## Changes

### Roslyn dependency bundle

Files changed:

- `Runtime/Unity.Pipeline.asmdef`
- `Runtime/Plugins/CodeAnalysis/*.dll`
- `Runtime/Plugins/CodeAnalysis/*.dll.meta`
- `Runtime/Plugins/CodeAnalysis/CHECKSUMS`

What changed:

- Replaced Unity-2022-incompatible Roslyn transitive DLLs with versions available from the Unity
  `2022.3.62f3` editor installation.
- Added missing Roslyn transitive dependencies:
  - `System.Memory.dll`
  - `System.Threading.Tasks.Extensions.dll`
  - `System.Text.Encoding.CodePages.dll`
- Added those DLLs to `Unity.Pipeline.asmdef` `precompiledReferences`.
- Converted Roslyn plugin `.meta` files to Unity 2022-compatible plugin importer metadata.
- Enabled the Roslyn plugin DLLs for the Editor platform so `Unity.Pipeline.dll` can load in the
  editor domain.
- Updated `CHECKSUMS` so `PipelineRuntimeBuildProcessor.VerifyBundledChecksums()` matches the local
  DLL bundle.

Why:

- Unity 2022 rejected the original Roslyn DLL import metadata and reported the assemblies as
  incompatible with the editor.
- `Unity.Pipeline` references `Microsoft.CodeAnalysis` APIs at compile time and runtime, so those
  assemblies must be both referenced during script compilation and loadable by the editor.
- Roslyn `3.11.0.0` expects older dependency versions than the bundled Unity 6 package dependencies.
  Using Unity 2022's own compatible dependency set avoids assembly load/version conflicts.

Potential impact:

- The package now carries Roslyn dependencies tailored for Unity 2022. If the package is later moved
  back to Unity 6, these DLLs and `.meta` import settings should be reviewed against the upstream
  package bundle.
- `CHECKSUMS` intentionally changed because the physical DLL payload changed. Future DLL updates must
  regenerate these hashes.
- Enabling these DLLs for Editor is required for Unity 2022. If a future Unity version changes plugin
  compatibility rules, re-check plugin importer settings instead of only changing asmdefs.

### Runtime asmdef references

File changed:

- `Runtime/Unity.Pipeline.asmdef`

What changed:

- Removed the hard assembly reference to `Unity.InputSystem`.
- Kept Input System-dependent source behind `#if ENABLE_INPUT_SYSTEM`.

Why:

- This project is an empty Unity 2022 project and does not install the Input System package.
- A hard asmdef reference to `Unity.InputSystem` makes the package fail to compile before the guarded
  code can fall back to the unavailable response path.

Potential impact:

- In projects that later install and enable the Input System package, `RuntimeInputCommand.cs` should
  be revisited. With the hard reference removed, the current empty-project configuration is correct,
  but a project that defines `ENABLE_INPUT_SYSTEM` may need either a restored assembly reference or a
  separate optional input-system assembly.
- Without Input System, runtime input simulation commands compile but return an unavailable response.

### Unity API compatibility

Files changed:

- `Editor/Commands/Assets/AssetCommands.cs`
- `Editor/Commands/Materials/MaterialCommands.cs`
- `Tests/Editor/Assets/PhysicsMaterialAssetCommandsTests.cs`
- `Tests/Editor/Materials/MaterialCommandsTests.cs`

What changed:

- Added a Unity 2022 alias:

```csharp
#if !UNITY_6000_0_OR_NEWER
using PhysicsMaterial = UnityEngine.PhysicMaterial;
#endif
```

- Added a `Material` render queue compatibility helper:

```csharp
private static int GetRawRenderQueue(Material mat)
{
#if UNITY_6000_0_OR_NEWER
    return mat.rawRenderQueue;
#else
    return mat.renderQueue;
#endif
}
```

Why:

- Unity 6 uses `UnityEngine.PhysicsMaterial`; Unity 2022 uses `UnityEngine.PhysicMaterial`.
- Unity 6 exposes `Material.rawRenderQueue`; Unity 2022 does not.

Potential impact:

- `PhysicsMaterial` command inputs still accept both `"PhysicsMaterial"` and `"PhysicMaterial"`.
  On Unity 2022 they resolve to `UnityEngine.PhysicMaterial`.
- On Unity 2022, `get_material_properties` reports `material.renderQueue` as the closest compatible
  value. This loses the Unity 6-only distinction that `rawRenderQueue == -1` explicitly means
  "inherit from shader". The setter still assigns `renderQueue`, including `-1`.

### Test assembly references

Files changed:

- `Tests/Runtime/Unity.Pipeline.Tests.Runtime.asmdef`
- `Tests/Editor/Unity.Pipeline.Tests.Editor.asmdef`
- `Tests/Editor/ApiEndpointsTests.cs`

What changed:

- Enabled `overrideReferences` for the test asmdefs and explicitly listed required precompiled
  references.
- Runtime tests now explicitly reference:
  - `nunit.framework.dll`
  - `Newtonsoft.Json.dll`
- Editor tests now explicitly reference:
  - `nunit.framework.dll`
  - `Newtonsoft.Json.dll`
  - Roslyn DLLs used by test-visible API return types
- Removed `Unity.InputSystem` and `Unity.InputSystem.TestFramework` hard references from the editor
  test asmdef.
- Updated `LogAssert.Expect(...)` calls to the Unity 2022-compatible overload that includes
  `LogType.Error`.

Why:

- Unity 2022 did not pass `nunit.framework.dll` to the Runtime test assembly while
  `overrideReferences` was false, producing errors for `NUnit`, `Test`, `SetUp`, `TearDown`, and
  related attributes.
- Some editor tests inspect APIs whose signatures expose Roslyn types, so the test assembly needs
  direct Roslyn references.
- The empty project does not install Input System packages, so test asmdefs cannot hard-reference
  those assemblies.
- Unity 2022's `LogAssert.Expect` API does not support the Unity 6 single-argument overload used by
  the package.

Potential impact:

- Test assemblies are now stricter about their precompiled reference list. Adding tests that use new
  third-party assemblies requires updating the relevant asmdef.
- Input System-specific editor tests are not compiled in this empty-project configuration because the
  hard package reference was removed.

### Package internal API visibility

Files changed:

- `Runtime/Common/BasePipelineServer.cs`
- `Runtime/Commands/HotReloadCommands.cs`

What changed:

- Made `BasePipelineServer.Token` public.
- Made `HotReloadCommands.ReloadFileOverride(...)` public.

Why:

- Runtime tests compile as a separate assembly and directly use these members.
- The methods/properties are part of the package's test-facing workflow in the current source, but
  Unity 2022 compilation surfaced that their accessibility did not match that usage.

Potential impact:

- This slightly increases the public surface of the runtime assembly.
- `Token` is still derived from the server's existing token source; this change does not alter token
  generation or authentication behavior.
- `ReloadFileOverride` was already exposed as a CLI command via `[CliCommand]`; making the C# method
  public aligns direct API visibility with the command surface.

### Descriptor watchdog recovery

File changed:

- `Runtime/Common/BasePipelineServer.cs`

What changed:

- A healthy watchdog tick now also refreshes the instance descriptor heartbeat.

Why:

- During the `0.5.0-exp.1` upgrade validation, the HTTP listener remained healthy on port 7800
  while `Library/Pipeline/.unity-pipeline-port` was absent, so the Unity CLI could not discover the
  running server.
- Refreshing the heartbeat recreates a missing descriptor from the server's in-memory descriptor
  without requiring a domain reload or a manual Editor restart.
- Test servers are unaffected because they override `WritesDescriptor` to `false`.

## Verification

Verified in the Unity Editor:

- Unity `2022.3.62f3`
- Embedded package reports `com.unity.pipeline` version `0.5.0-exp.1`
- Project refresh completed with `Mono: successfully reloaded assembly`
- Latest editor refresh showed `LogAssemblyErrors (0ms)`
- No current compile errors remained in the Unity Console
- Unity CLI discovered the live server on port 7800 and listed 191 registered commands, including
  the new `audit` and `audit_status` commands
- Core EditMode tests: 651 total, 649 passed, 0 failed, 2 skipped for version/package constraints
- Extension EditMode tests: 7 total, 7 passed, 0 failed
- `audit` returned the expected structured `unavailable` result because Project Auditor is not
  installed in this Unity 2022 project

Also verified with Unity-generated Bee response files through Roslyn `csc`:

- `Unity.Pipeline`
- `Unity.Pipeline.Editor`
- `Unity.Pipeline.CodeGen`
- `Unity.Pipeline.Tests.Runtime`
- `Unity.Pipeline.Tests.Editor`

Generated response files confirmed that:

- Runtime tests include `nunit.framework.dll` and `Newtonsoft.Json.dll`.
- Editor tests include `nunit.framework.dll`, `Newtonsoft.Json.dll`, and Roslyn DLL references.

## Maintenance notes

- If upgrading Unity back to Unity 6, compare this package against the upstream package before keeping
  these compatibility changes.
- If updating any DLL under `Runtime/Plugins/CodeAnalysis`, regenerate `CHECKSUMS`.
- If installing Input System in this project, re-check `RuntimeInputCommand.cs` and the test asmdefs.
- If adding test dependencies, update `precompiledReferences` because test asmdefs now use
  `overrideReferences: true`.
- Keep this document with the local package until the package officially supports Unity 2022.
