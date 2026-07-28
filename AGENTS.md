# Notes for AI agents

## Generated code: .ecs files (LeMP)

To regenerate `.out.cs` files from `.ecs` (Enhanced C#) files:

```
Lib\LeMP\LeMP.exe --outext=.out.cs path/to/file.ecs        (Windows)
mono Lib/LeMP/LeMP.exe --outext=.out.cs path/to/file.ecs   (Linux/macOS)
```

To avoid noise, don't commit generated files if ONLY the first line (LeMP version banner) changed.

Notes on Enhanced C#:
- Most basic C# syntax works, but some C# 10+ syntax is not available.
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
  (Exception: the `⬤ Version X.Y.Z` release commits pair `Core/AssemblyVersion.cs` with
  appveyor.yml; their message is universal, so it syncs fine.)
- Write each Core commit's message as if LoycCore were the whole repository: scope it to
  the Core change alone, and don't mention paths or files that exist only in ecsharp
  (`Main/`, `appveyor.yml`, the VS extension, etc.).
