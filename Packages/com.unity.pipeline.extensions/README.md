# Unity Pipeline Extensions

This optional Editor-only package extends com.unity.pipeline with compatibility commands that
are intentionally kept out of the core package: Profiler session helpers, guarded reflection,
Prefab Stage control, isolated object capture, extra Timeline operations, and selected authoring
utilities.

The Editor source is compiled into `Unity.Pipeline.Editor` through an `.asmref`, giving it access
to that assembly's internal types. The Runtime assembly already grants internal access to this
Editor assembly. Tests similarly join `Unity.Pipeline.Tests.Editor`. This keeps upstream 0.6's
type visibility without adding public APIs or new friend assemblies. Commands are discovered
automatically by Pipeline's `[CliCommand]` discovery.

The extension no longer produces a separate DLL. References, platforms, and compilation options
come from the host asmdef; this backport adds `Unity.AI.Navigation` and its package dependency to
the host for the extension's NavMesh commands. The package's files and namespaces remain separate.
Run its tests by fixture (`CustomCommandRegistrationTests`), not by the former extension assembly
name. Code using this extension must reference `Unity.Pipeline.Editor` and respect its internal API.

Install it as a local package:

~~~
{
  "dependencies": {
    "com.unity.pipeline.extensions": "file:com.unity.pipeline.extensions"
  }
}
~~~

Run unity command after the Editor recompiles to inspect the available commands. Timeline
commands also require the optional com.unity.timeline package.

## Boundaries

- Commands that write authoring data use Pipeline's path sandbox and confirmation conventions.
- invoke_method always requires confirm=true; use dry_run=true to validate overload selection first.
- Profiler snapshot files are JSON summaries, not Unity binary .data captures.
