# Notes for AI agents

## Generated code: .ecs → .out.cs (LeMP)

Files ending in `.out.cs` are GENERATED from Enhanced C# (`.ecs`) source files in the
same directory. Never edit a `.out.cs` file directly — edit the `.ecs` file and regenerate:

```
Lib\LeMP\LeMP.exe path/to/file.ecs        (Windows)
mono Lib/LeMP/LeMP.exe path/to/file.ecs   (Linux/macOS; LeMP.exe is a .NET Framework binary)
```

Notes on Enhanced C# / LeMP:
- Most basic C# syntax works, but most C# 10+ syntax is NOT available.
- There are occasional unpatched parser bugs, e.g. you may need to write `X<Y<Z> >`
  instead of `X<Y<Z>>`.
- Re-use the macro patterns you see in existing `.ecs` files.

## Building and testing

- Test runner: `Core/Tests` (LoycCore.Tests.csproj, targets netcoreapp2.1; runs on
  newer runtimes with `DOTNET_ROLL_FORWARD=Major`). It is a menu-driven console app
  using Loyc.MiniTest, not NUnit/xUnit — test fixtures must be registered in
  `Core/Tests/Program.cs`. Pass the menu choice as argv, e.g. `2` runs the SyncLib suite.
- A test marked `[Test(Fails = "...")]` is a known failure and does not fail the run.

## Commit scoping: Core/ is mirrored to another repository

The `Core/` folder is mirrored to https://github.com/qwertie/LoycCore using `git subrepo`,
with messages copied unchanged, so when making commits:

- Never mix changes under `Core/` with changes outside `Core/` in the same commit.
- Write each Core commit's message as if LoycCore were the whole repository: scope it to
  the Core change alone, and don't mention paths or files that exist only in ecsharp
  (`Main/`, `appveyor.yml`, the VS extension, etc.).
