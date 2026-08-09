# Technology baseline

Civ3 Remastered uses current stable technology while avoiding preview/development runtimes for the main branch.

## Engine

- Godot 4.7.1 stable, .NET edition
- C# on .NET 8
- Apple Silicon is a first-class target

Godot 4.8 development snapshots are intentionally excluded from the baseline until a stable release exists and passes the full regression suite.

## .NET

- Target framework: `net8.0`
- SDK baseline: .NET SDK 8.0.423
- C# language version: 12

Godot 4.7 uses .NET 8, so the project stays on the current supported Godot runtime rather than forcing a newer .NET target that the engine does not use.

## Dependency policy

- Prefer the newest stable compatible package release.
- Do not adopt prerelease packages on `Development` without a specific reason and regression coverage.
- Do not accept a dependency update that adds a commercial/runtime build key requirement when a supported license-key-free version meets the project's needs.
- Versions are centralized in `Directory.Packages.props`.
- Dependabot checks NuGet and GitHub Actions weekly.
- CI audits transitive dependencies for known vulnerabilities.
- Framework assemblies already supplied by .NET 8 should not be carried as legacy PackageReferences.

### Compatibility exceptions

`SixLabors.ImageSharp` remains on 3.1.12. Version 4.x is newer but requires a Six Labors license during build. 3.1.12 is the newest 3.x release and is sufficient for the existing test tooling without adding that build-time licensing dependency.

## CI baseline

Every pull request to `Development` must build the full solution and run EngineTests on:

- Ubuntu 24.04
- macOS 26 on ARM64 GitHub-hosted runners

The macOS ARM64 lane is the closest automated approximation of the primary Apple Silicon development target.
