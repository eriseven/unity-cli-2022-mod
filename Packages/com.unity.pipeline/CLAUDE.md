# com.unity.pipeline — Unity 2022 port

This embedded package tracks upstream 0.8.0-exp.1. Read
`Documentation~/unity-2022-compatibility.md` for retained compatibility changes and validation.

## Layout

- `Runtime/Attributes`: unconstrained `Unity.Pipeline.Attributes` assembly for command and CodeReload attributes.
- `Runtime/`: `Unity.Pipeline`, gated to Editor, instrumented players, or `ENABLE_RUNTIME_PIPELINE`.
- `Runtime/IlInterpreter`: interpreter VM with the same gating.
- `Runtime/CodeReload`: code-reload registry, transforms, and player push transport.
- `Editor/`: HTTP server, commands, test runner, JSON settings, and source watcher.
- `CodeGen/`: Mono.Cecil IL postprocessor; keep this assembly at package root.
- `Tests/`: retained and migrated regression suite; upstream 0.8 omits tests from its core tarball.

## Compatibility

Keep the Unity 2022 Roslyn bundle/checksum manifest and import plugins through Unity.
Do not hand-edit binary assets or metadata. Existing script GUIDs are preserved with AssetDatabase.
Editor settings live in `ProjectSettings/Packages/com.unity.pipeline/EditorPipelineConfig.json`.
Runtime settings remain in Project Settings > Pipeline > Runtime. The legacy Editor asset is inert.
Use `[CodeReload]`, `codereload_status` and `cleanup_codereload`; the override-file helper workflow
and generated link.xml no longer exist. Samples are distributed separately upstream.

Extensions compile into the host Editor/test assemblies through `.asmref`; retain AI Navigation
references and avoid widening internal APIs merely for extension access.

## Working and verifying

Follow the project `UNITY-GUIDE.md` and use the `unity-pipeline` skill via Unity CLI. Discover the
live command catalog and explicitly target the project. Keep auto-tick enabled, batch source
changes, recompile, then poll `recompile_status`. Use asynchronous execution for broad/self-hosted
test runs and the optional native XML callback described in the compatibility notes.

`console` replaces `get_console_logs`. Check native ground truth as well as buffered historical
logs. Both listeners accept IPv4 loopback (`127.0.0.1`).

Do not commit or push without an explicit request.
