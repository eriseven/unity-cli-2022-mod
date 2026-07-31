# Unity Pipeline Extensions

This optional Editor-only package extends com.unity.pipeline with compatibility commands that
are intentionally kept out of the core package: Profiler session helpers, guarded reflection,
Prefab Stage control, isolated object capture, extra Timeline operations, and selected authoring
utilities.

The assembly depends on the public Unity.Pipeline and Unity.Pipeline.Editor APIs. Commands are
discovered automatically by Pipeline's [CliCommand] discovery, so no registry modification or
server fork is required.

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
