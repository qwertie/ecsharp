README
======

The ecsharp repository holds several tools for enhancing .NET and C# development:

- The Loyc .NET Core libraries, a set of libraries whose theme is "stuff that 
  should be built into the .NET framework, but isn't." These libraries have their
  [own repository](http://github.com/qwertie/LoycCore) and [home page](http://core.loyc.net),
  and the Loyc .NET Core repository is the `Core/` folder in this repository.
  One of these libraries (Loyc.Syntax) supports [universal syntax trees](http://loyc.net/loyc-trees),
  [LES2 and LES3](http://loyc.net/les).

  - [SyncLib](http://core.loyc.net/synclib) is included: a fast & flexible framework for 
    doing messy real-world (de)serialization with less code and no DTOs. Supports JSON, Protocol Buffers, 
    and a compact binary format.

- [Enhanced C#](http://ecsharp.net) (or EC#) is a liberalization and regularization of the C# language.
  You can think of EC# as a C# _preprocessor_, since only the "front end" part of the project is done.
  The preprocessor consists of three mostly-independent parts,
    1. The Enhanced C# parser
    2. [LeMP](http://ecsharp.net/lemp), the Lexical Macro Processor
    3. LeMP Standard Macros

- [LLLPG](http://ecsharp.net/lllpg), the Loyc LL(k) Parser Generator, which is used 
  to generate code from the grammars of Enhanced C#, [LES](http://loyc.net/les), and 
  LLLPG itself.

These projects are the first products of the [Loyc](http://loyc.net) (Language of Your Choice) initiative.

Installation
------------

If you just want the [core libraries](http://core.loyc.net/), you can find them in NuGet. Otherwise, see

- How to set up [LeMP or LLLPG in Visual Studio](http://ecsharp.net/lemp/install.html)
- How to set up [LeMP or LLLPG on other platforms](http://ecsharp.net/lemp/install.html#on-other-platforms)
- [Download page](https://github.com/qwertie/ecsharp/releases)

How to build
------------

Open Loyc.netfx.sln in Visual Studio (or Loyc.netstd.sln for the .NET Standard edition), set the build configuration to Debug, and build it!

If you need to change any .ecs or .les source files (Enhanced C# or [LES](http://loyc.net/les/)), you'll need to install the latest LeMP extension for Visual Studio, which can be found on the [Releases page](https://github.com/qwertie/ecsharp/releases). There is no _build step_ for these files, so the extension is not required for building. Unfortunately VS Code is not supported at this time - let me know if you need support.

Visual Studio may complain, while building a .NET Framework 4.7.2 project, that 'Your project does not reference ".NETFramework,Version=v4.7.2" framework...." if you built the .NET Standard version of the same project earlier. To fix this, locate the folder named `obj` inside the project from which the error message originated, delete the entire `obj` folder, and rebuild (the `project.assets.json` file inside that folder seems to be causing the error).

How to publish new versions
---------------------------

This is a note-to-self / note-to-AI-agent; pull-requestors can ignore it. Steps that need Windows, Visual Studio, or push/publish credentials are marked; everything else can be done by an agent in a Linux container.

### 1. Update version numbers

- `Core/AssemblyVersion.cs`: both `AssemblyVersion` and `AssemblyFileVersion` (e.g. `30.3.0`).
- `appveyor.yml`: the `version:` line at the top (e.g. `30.3.{build}`) **and** the `- set SEMVER=` line (e.g. `30.3.0`). SEMVER becomes the NuGet package version, so it must match AssemblyVersion.cs.
- If the VS extension will be released too: the version in `Visual Studio Integration\LoycForVS2022\source.extension.vsixmanifest`.

### 2. Build & test (Release configuration)

- On Windows: build `Loyc.all.sln` (Release) and run `Bin\Release\Tests.exe 12345` then `Tests.exe 67` (each digit selects a test-menu item; set 8, LLLPG, is excluded because it is sometimes nondeterministic).
- On Linux: `dotnet build -c Release Loyc.netstd.sln`, then run test suites 1–6 one at a time: `cd Core/Bin/Release/net6.0 && DOTNET_ROLL_FORWARD=Major dotnet LoycCore.Tests.dll N`. A small number of environment-dependent failures is normal on Linux (BinaryFormatter-based serialization tests, a `%TEMP%`-related test, `TokenTests.StructSizeCheck`); any *new* failure blocks the release. Optionally also build `Loyc.netfx.sln` using the `Microsoft.NETFramework.ReferenceAssemblies.net472` package with `-p:FrameworkPathOverride`, and run the test EXE under mono to exercise the .NET Framework code paths.

### 3. Update the version history & docs

- Add a `### vX.Y.Z: Month Day, Year ###` section to `version-history.md` at the root of the core.loyc.net site (the **gh-pages branch of qwertie/LoycCore**), summarizing commit messages since the previous release tag. Omit trivial changes; put breaking changes first.
- If LeMP, LLLPG or Enhanced C# changed: also `lemp/version-history.md` / `lllpg/version-history.md` on ecsharp.net (the **gh-pages branch of this repo**).
- Update any affected manuals (e.g. `synclib/manual.md` on core.loyc.net).

### 4. Commit, push, watch AppVeyor

- Commit with a message like `⬤ Version 30.3.0` (the ⬤ marks release commits in the history). The ⬤ commit contains both the `Core/AssemblyVersion.cs` and appveyor.yml bumps — the sanctioned exception to the Core/ commit-scoping rule in AGENTS.md, since its message reads fine in LoycCore too.
- Push master (credentials) and confirm the AppVeyor build is green. Pushes to master build `-ciNNN` NuGet packages as artifacts but publish nothing.

### 5. Tag to publish the NuGet packages

- Create an unannotated tag and push it: `git tag v30.3.0 && git push origin v30.3.0` (credentials).
- The tag push makes AppVeyor build again and **publish all packages to NuGet.org** (Loyc.* plus LeMP, LLLPG, LeMP-Tool) at the SEMVER version. Deployment only runs for tags on master, and needs the encrypted NuGet API key in appveyor.yml to be current — if publishing fails with 401/403, encrypt a fresh nuget.org key at https://ci.appveyor.com/tools/encrypt and update the `secure:` line.

### 6. Sync Core/ to the LoycCore repository

The `Core/` folder is a [git-subrepo](https://github.com/ingydotnet/git-subrepo) of https://github.com/qwertie/LoycCore (master branch; that repo also hosts the core.loyc.net site on gh-pages). `Core/.gitrepo` records the sync state. The `.gitrepo` file is *not* pushed to LoycCore and commits that only touch it are skipped, so LoycCore looks like an ordinary standalone repo.

- **Push Core changes made here:** `git subrepo push Core` — every commit touching `Core/` since the last sync is copied to LoycCore with its message unchanged (hence the commit-scoping rule in AGENTS.md); other commits are skipped.
- **Pull changes made directly in LoycCore:** `git subrepo pull Core` — creates one merge commit here and updates `.gitrepo`. Editing either repo independently is fine; pull before push if both moved.
- Both commands need a **clean working tree** (`git stash` around them if needed) and git-subrepo on PATH.
- Windows/bind-mount caveat: a checkout with `core.symlinks=false` turns `lib/git-subrepo.d/bash+.bash` into a text file containing a path; replace it with a copy of (or real symlink to) `ext/bashplus/lib/bash+.bash`. If a command aborts and later complains "There is already a worktree with branch subrepo/Core", run `git subrepo clean Core`.

(History: Core was synced with git-subtree long ago — see LoycCore's local `master-old-ecsharp-subtree` branch — then briefly by a replay script; since v30.3.1 it is a true git-subrepo, adopted by hand-writing the `commit`/`parent` baselines in `.gitrepo` because `git subrepo clone` refuses non-empty directories.)

### 7. GitHub release (requires Windows + Visual Studio)

- Uninstall the previously installed LeMP VS extension (from within VS), then run `UpdateLibLeMPAndReinstall.bat`. It builds `Loyc.netfx.sln` (Release) plus the VS extension, and copies all outputs — dll/exe/xml/exe.config/pdb files and `LeMP_VisualStudio.vsix` — into `Lib\LeMP`. Manually check that the extension still works.
- Copy `Lib\LeMP` to a sibling folder named for the release, e.g. `Lib\LeMP-30.3`.
- In the copy, delete **all `*.pdb` files** and **`LeMP_VisualStudio.vsix`**. Everything else ships, including the demo and test EXEs and the XML doc files. (To double-check the expected contents, list a previous release's zip.)
- Zip the folder at maximum compression, with the folder itself as the zip root: `7z a -tzip -mx=9 LeMP-30.3.zip LeMP-30.3\` (entries should look like `LeMP-30.3/LeMP.exe`).
- On github.com, create a release from the `vX.Y.Z` tag with **two assets**: `LeMP-X.Y.zip` and `Lib\LeMP\LeMP_VisualStudio.vsix` (attached separately, which is why it was deleted from the zip). Link the version history in the description.

### 8. Documentation & Marketplace

- Regenerate API docs by running `doc/Doxygen.bat` in the gh-pages branch (Windows), and push the gh-pages branches of both sites, including the step-3 edits (credentials).
- If applicable, publish the new VSIX on the Visual Studio Marketplace (credentials).