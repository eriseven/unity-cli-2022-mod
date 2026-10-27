---
name: tests-run
description: List and run Unity EditMode or PlayMode tests through Unity CLI/Pipeline. Use when validating a change with focused automated tests.
---

# Tests / Run

Use the Unity Pipeline `unity` CLI; this replaces the Unity-MCP tool. Do not call `unity-mcp-cli`.

## Workflow

1. Run `unity pipeline list`, then `unity command`, to verify that the target Editor and command are reachable.
2. Use Pipeline `ObjectRef` handles (path, guid, globalId, instanceId, or hierarchyPath) and retain returned identities for follow-up calls.
3. Preview destructive or overwrite operations with `--dry_run true`; apply only with the documented `--confirm true` gate. Re-read the affected object afterward.
4. Never modify Unity serialized assets or `.meta` files directly. Use a custom Pipeline command when no suitable built-in command exists.

## Commands

```powershell
unity command editor set_auto_tick --enable true
unity command test list --mode editor
unity command test run --mode editor --filter MyFixture.MyTest
unity command test run --mode playmode --filter CategoryName --filter_type category --async_tests true
unity command test status
```

## Notes

Use the narrowest filter. For asynchronous runs poll `test status`; inspect Console/Test Runner when a failing run returns opaque details.

