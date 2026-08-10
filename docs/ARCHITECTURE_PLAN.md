# Architecture plan

## Current donor architecture

The OpenCiv3 donor code already has useful separation:

- `QueryCiv3`: BIQ/SAV parsing and Civ III binary-format knowledge.
- `Blast`: PKWare DCL decompression used by Civ III files.
- `ConvertCiv3Media`: PCX/FLC/media decoding.
- `C7Engine`: simulation, rules, turn handling, pathing, AI, and game data.
- `C7`: Godot presentation, input, rendering, animation, and UI.
- `EngineTests`: headless/unit/integration tests.

This project should preserve those useful boundaries while removing prototype-era global coupling over time.

## Target architecture

```text
Original Civ III data/assets
          |
          v
  Civ3 format/media adapters
          |
          v
     Compatibility import
          |
          v
+---------------------------+
|      Simulation Core      |
|                           |
| GameSession               |
| GameData / world state    |
| commands                  |
| deterministic RNG         |
| domain events             |
| turn engine               |
+---------------------------+
          |
          +---------------------> Headless compatibility tests
          |
          +---------------------> AI
          |
          v
   Godot presentation
```

## Phase 1: preserve behavior while creating seams

The first architectural changes should be additive and low risk.

1. Introduce an explicit `GameSession` object that owns a `GameData` instance.
2. Add injectable randomness behind an interface while preserving the current random sequence until compatibility tests exist.
3. Add a domain-event boundary for simulation events.
4. Move UI/animation waiting out of domain objects incrementally.
5. Replace direct `EngineStorage.gameData` access subsystem by subsystem.
6. Keep `EngineStorage` as a compatibility facade during migration rather than deleting it abruptly.

## GameSession responsibilities

Eventually `GameSession` should own:

- current `GameData` state
- deterministic random source
- command processing
- domain event publication
- compatibility profile/mode
- lifecycle for one loaded or newly created game

It should not own:

- Godot nodes
- textures
- animations
- camera state
- platform-specific paths
- UI controller widgets

## Determinism

Simulation tests need reproducibility. The long-term invariant should be:

> same initial state + same seed + same command stream = same resulting state and domain events

Any use of `new Random()` inside gameplay code should eventually be replaced by the session random source. This migration must be test-backed because changing random-call order can change game outcomes.

## Simulation versus presentation

Domain logic may emit events such as:

- unit moved
- combat started
- combat round resolved
- unit damaged
- unit destroyed
- city founded
- city captured
- technology completed
- border expanded

The presentation layer decides whether those events are animated, skipped, sped up, or ignored. A headless run should never need a renderer to make progress.

## Compatibility boundary

Strict Civ III compatibility belongs in the simulation and import layers, not in the Godot UI. A graphical redesign should not alter economy, combat, diplomacy, AI inputs, or turn outcomes unless a non-Classic compatibility profile explicitly requests it.

## Migration rule

Do not perform a broad rewrite simply to make the architecture cleaner. For each subsystem:

1. characterize current behavior with tests
2. add the new seam
3. migrate callers
4. compare behavior
5. remove obsolete global access only when no callers remain

This keeps the donor project playable throughout the modernization.