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

## Directives

### Prime directives

- Concise, elegant, reuseable and reused code are crucial to maximize the team's productivity. Concise does NOT mean minimizing newlines. Instead, follow the generalized DRY principle: factor code to avoid repeating patterns of any kind. When adding features, actively seek out similar functionality to find opportunities for code re-use. Make the code concise via in-function refactoring during the Second Pass. Do not without approval add large ancillary code blocks to handle special cases.
- Care about Separation of Concerns and Information Hiding
- To give human reviewers a simpler diff, avoid unnecessary changes during tasks (but you can ignore added/removed BOMs)
- Stop searching for the C# Severity type. All you need to know: it's a numeric enum that includes Severity.Error, .Warning and .Note (Error > Warning > Note)
- Communicate clearly: avoid pronouns and English summaries of code symbols in favor of saying the code symbols themselves ("Les2Parser" not "LES parser"); notice when you're writing something ambiguous and add a clarification in parentheses or † with a footnote below. When writing plans, state where in the code each change will be made, and include member/function/type names with signatures.

### Style directives

- Put high-level code first and callers before callees (helper functions at bottom).
  - Put nested functions at the bottom of the outer function or of the block it is called from.
- Renaming something? If the file has that name, rename it too.
- Wrap code before column 120 and comments before column 100
- Naming:
  - Use verb phrases for names of new functions: findFoo(), not fooLookup()
  - Indicate whether transforms are in-place or not (`reverse()` vs `getReversed()`)
  - Use the codebase's dominant vocabulary for concepts & avoid coining terms: don't say "Q" if existing code says "flow rate"
- Avoid one-liner loops/flow: spread `if (x) continue` and `for (let x of list) write(x)` over two lines
- Prefer nesting over early exit, e.g. instead of

    let c = list.find(...);
    if (!c) break;
    Change(c);
    ...

  write
  
    let c = list.find(...);
    if (c) {
        Change(c);
        ...
    }

### Other

- Use subagents more often when your task is large and context exceeds 100K tokens
- If you're not on a dedicated temporary worktree, other agents may be working in the same tree so prefer not to stash and never switch branches when the user didn't explicitly ask for it

## Communication

Always write clearly, plainly, and unambiguously:

1. Prefer active voice to make the subject of each sentence explicit
2. Prefer unabbreviated code symbols over English paraphrase: the parser => `LesParser`
3. Be specific
4. Avoid saying "gate" and similar metaphors, and prioritize clarity over concision
5. Talk like Peter Miller, Scott Alexander, Matt Yglesias, Spencer Greenberg or Julia Galef
6. Notice when your speech is vague and follow up immediately with an example or parenthetical
7. Label lists with numbered codes that are unique within a conversation, so the user can refer to each item unambiguously:
  - P1, P2, ..., Pn: Numbered proposals or propositions
  - F1...Fn: findings
  - D1...Dn: decisions
  - T1...Tn: tests
  - B1...Bn: bugs
  - S1...Sn: sections
  - You can invent new series too
