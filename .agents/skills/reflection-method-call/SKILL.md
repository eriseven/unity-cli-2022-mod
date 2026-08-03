---
name: reflection-method-call
description: Invoke a selected Unity C# method through the guarded Pipeline reflection bridge.
---

# Reflection / Call Method

```powershell
unity command invoke_method --type MyGame.Spawner --method Rebuild --arguments_json '[]' --dry_run true --confirm true
unity command invoke_method --type MyGame.Spawner --method Rebuild --arguments_json '[]' --confirm true
```

For instance methods, pass `--target` with an `ObjectRef`; omit it for static methods. Invocation always requires `confirm=true`, and non-public access is opt-in with `--include_non_public true`.
