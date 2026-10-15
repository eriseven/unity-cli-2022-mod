# Unity 2022.3.62f3 compatibility notes

This document records the local compatibility changes made to run `com.unity.pipeline`
`0.6.0-exp.1` in a Unity `2022.3.62f3` project.

## 0.6.0 upstream synchronization

The baseline is the official Unity registry package, not a version-only change:

| Package | Registry tarball SHA-1 | Upstream revision |
| --- | --- | --- |
| `0.5.0-exp.1` | `24975d51528a67809794e52185267762a9561e15` | `17df0ac8d6533a8c22830c89dc85a24c01f30ade` |
| `0.6.0-exp.1` | `9bb4172c603cda2626bacac4d4c64bdb8270a3e0` | `27af91a943315a5702ac7679241399b649d6a20d` |

Source: `https://packages.unity.com/com.unity.pipeline`, with tarballs from
`https://download.packages.unity.com/com.unity.pipeline/-/com.unity.pipeline-<version>.tgz`.
Existing local changes were merged against the official 0.5 baseline before applying 0.6.
See [CHANGELOG](../CHANGELOG.md) for the complete upstream release notes.

The update includes server-side `argv`/`commandLine` binding, compact responses, batch operations,
`run_script`, eval usage reporting, editor analytics, runtime settings commands, automatic runtime
bootstrap, the IL interpreter, signed player hot-reload pushes, the source watcher, and the upstream
regression tests and documentation.

Port-specific decisions:

- Declare `unity: 2022.3`. Keep the already-tested Test Framework `1.4.6` dependency rather than
  downgrading to upstream 0.6's `1.1.33`.
- Preserve the existing Unity 2022 Roslyn DLL bundle, importer settings, checksums, and explicit
  test-assembly references. Add the new interpreter assembly references from upstream.
- Keep upstream 0.6 type visibility. The extensions package uses `.asmref` files to compile its
  Editor code into `Unity.Pipeline.Editor` and tests into `Unity.Pipeline.Tests.Editor`; Runtime
  already grants internal access to both hosts. Extension implementation types are internal too.
  The Editor host references `Unity.AI.Navigation`, with its `1.1.7` package dependency declared
  by this backport. Extension commands remain discoverable without widening Pipeline's public API.
- Use `EditorAnalytics.RegisterEventWithLimit` / `SendEventWithLimit` on Unity 2022, with the same
  event payloads and the Editor analytics enablement check. Unity 2023.2+ uses upstream's
  `IAnalytic` transport. The compatibility interface is private to the Pipeline Editor assembly's
  namespace, not a replacement type in Unity's namespace.
- Keep the physics-material alias, render-queue fallback, public test-facing runtime members,
  and descriptor watchdog recovery from the previous port.
- Use the two-argument `LogAssert.Expect(LogType.Error, ...)` overload in new regression tests.
- Use explicit IPv4 loopback in the test-server convenience client and live-server guard. On this
  Windows/Mono installation, a `localhost` status probe took 2052 ms versus 3 ms on `127.0.0.1`.
  Explicit-host connectivity tests and production server binding are unchanged.
- Import the official IL interpreter analyzer through Unity, with the `RoslynAnalyzer` label and
  all platform compatibility disabled. SHA-256:
  `d2db688b33e8504c04fcd31800e3efbd26bb177ee3b67f8ef2a59a3d5dc19700`.
- Retain `RuntimePipelineManager` and its script GUID as an obsolete compatibility shell. It does
  not start another server or own global discovery state. Its explicit control methods forward to
  the new bootstrap driver; old serialized fields remain available for migration but no longer
  configure a server. Configure **Project Settings > Pipeline > Runtime** before using the runtime
  server. The existing sample scenes retain their bindings through this shell.
- Supply the new Pong example as `PongScript.cs` (matching its MonoBehaviour class) plus an Editor
  menu that authors and saves `PongScene.unity` after sample import. This replaces the upstream
  Unity 6 serialized scene, which still references the removed legacy manager. No sample scene or
  script GUID is edited by hand.
- Native modal-dialog observation remains guarded by upstream's `UNITY_6000_7_OR_NEWER` condition.
  The protocol and dialog tracker are present, but Unity 2022 cannot provide native dialog events.
- The existing Input System assembly references are retained, including the project's subsequent
  restoration of those references. The historical removal described below is not the current state.

### Assembly-reference verification on 2026-10-15

Unity `2022.3.62f3` compiled the restored upstream visibility and both extension `.asmref` files.
All 8 `CustomCommandRegistrationTests` passed, including command discovery, reflection invocation,
GameObject duplication, profiler snapshots, and the assembly/visibility regression check.
Unity generated the new reference metadata and removed the obsolete definition metadata.
The prior full-suite results below predate this assembly migration.

### Assembly-reference verification on 2026-10-15

Unity `2022.3.62f3` compiled the restored upstream visibility and both extension `.asmref` files.
All 8 `CustomCommandRegistrationTests` passed, including command discovery, reflection invocation,
GameObject duplication, profiler snapshots, and the assembly/visibility regression check.
Unity generated the new reference metadata and removed the obsolete definition metadata.
The prior full-suite results below predate this assembly migration.

### Verification on 2026-10-14

Validated in the running Unity `2022.3.62f3` Editor through the Unity CLI/Pipeline:

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Core EditMode (default, non-explicit selection) | 1161 | 0 | 2 |
| Extensions EditMode | 7 | 0 | 0 |
| Runtime PlayMode (Editor/Mono) | 15 | 0 | 17 |

The two core skips are upstream's disabled authoring E2E and Origin-header tests. The 17 PlayMode
skips are upstream's disabled legacy in-place hot-reload suites; they were not newly disabled by
this port. The passing PlayMode tests include interpreter host bindings, the AOT probe corpus
executed on Mono, and runtime-server startup/request handling. This is **not** an IL2CPP Player
build validation; no Player build or target switch was performed.

The broad self-test run crosses a domain reload and can leave the HTTP result collector reporting
`running` after the native run has finished. `Tests/Editor/Unity2022TestReport.cs` provides an opt-in
native Test Framework callback for authoritative NUnit XML results across that reload. Set
`SessionState["Unity.Pipeline.Tests.XmlReportPath"]` to an absolute output filename before a run
and erase the key afterwards. The counts above were read from these native XML reports; extension
and PlayMode counts also agree with `test_status`.

Also checked: clean script compilation with the imported analyzer, CLI argument binding,
non-transactional batch execution, sample import, Pong scene creation/save and its PlayMode
camera capture. Temporary sample/test assets and test-induced project settings were cleaned up;
the Editor was returned to stopped mode with its original clean unnamed default scene.

The sections below explain the original 2022 port and its retained compatibility decisions.

The upstream package declares `"unity": "6000.0"` and uses several Unity 6-era API and plugin import
assumptions. Unity 2022 can compile most of the package, but it needs explicit adjustments around
Roslyn dependencies, renamed Unity APIs, test assembly references, and optional packages.

## Scope

Core changes are in `Packages/com.unity.pipeline`; the extensions package dependency is updated
to `0.6.0-exp.1` as well.

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

Historical verification of the previous **0.5.0** port (not the 0.6 results):

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
