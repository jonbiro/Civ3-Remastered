# Civilization III Remastered

## Project charter

Civilization III Remastered is a personal-use modernization and compatibility project built from the OpenCiv3 codebase.

The first objective is not to invent a new 4X game. It is to reproduce Civilization III Complete as faithfully as practical on modern macOS while preserving the ability to improve presentation, quality of life, and AI behind explicit compatibility modes.

## Product goals

1. Run natively and reliably on modern Apple Silicon macOS.
2. Load original Civilization III Complete game data, scenarios, and assets from a user-provided installation/data directory.
3. Reproduce Civilization III rules and turn outcomes with a testable compatibility target.
4. Keep simulation logic independent from rendering and animation so the game can run headlessly.
5. Preserve a Classic mode whose behavior is intentionally conservative.
6. Add Enhanced/Remastered modes only after the corresponding Classic mechanics are understood and covered by compatibility tests.
7. Keep original-game assets external to this repository. The repository should contain code, tests, documentation, and original project-owned assets only.

## Non-goals for the first phase

- Rewriting the engine from scratch.
- Maintaining source-level compatibility with future OpenCiv3 development at the expense of this project's architecture.
- Reproducing every historical bug by default.
- Improving AI before the underlying simulation rules are trustworthy.
- Shipping original Civilization III copyrighted game assets in the repository.

## Compatibility modes

### Classic

The reference mode. Rules, balance, timing, and game outcomes should match Civilization III Complete as closely as practical. Any intentional deviation should be documented.

### Enhanced

Classic simulation with opt-in quality-of-life improvements and eventually stronger AI.

### Remastered

A later mode for larger presentation, balance, UI, AI, and gameplay changes that would no longer qualify as strict Civ III compatibility.

## Engineering principles

- The original Civilization III behavior is the compatibility oracle.
- Prefer deterministic, headless tests for simulation mechanics.
- Separate simulation state transitions from animation and UI.
- Replace global/static state incrementally rather than through a single risky rewrite.
- Preserve working OpenCiv3 code until a replacement is covered by tests.
- Treat QueryCiv3, ConvertCiv3Media, and ImportCiv3 as valuable executable documentation of Civ III formats even where they are incomplete.
- Every compatibility fix should add a regression test whenever practical.

## Donor baseline

This repository was forked from `C7-Game/OpenCiv3`. The `Development` branch is the donor baseline. Remaster work should happen on project-owned branches and be merged deliberately so upstream history remains understandable.

OpenCiv3's MIT-licensed code remains subject to its license and attribution requirements.