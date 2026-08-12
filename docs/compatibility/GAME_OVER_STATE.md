# Postgame state after a recorded victory

## Purpose

The common victory resolver persists the first full-round outcome. This layer makes that outcome operational: turn advancement stops, gameplay commands stop mutating the engine, and the frontend enters a postgame inspection state.

## Engine behavior

- victory is still evaluated only at a complete round boundary
- once an outcome containing claims is recorded, `TurnHandling.AdvanceTurn` sends `MsgGameOutcome` and returns instead of starting another round
- `MessageToEngine.send()` rejects normal gameplay commands after a recorded outcome
- `EngineStorage` checks the outcome again when a queued command is dequeued, so a command queued before the victory boundary cannot mutate the finished game afterward
- engine shutdown remains permitted after game over
- loading a native save that already contains an outcome restores the frontend directly into postgame state

## Frontend behavior

`GameState.GameOver` is separate from both `PlayerTurn` and `ComputerTurn`.

The game-over state blocks End Turn, observer-mode changes, unit actions, production changes, slider changes, and other engine mutations. The player can still inspect the finished world through map dragging, zoom, tile information, city screens, advisors, and game views.

The current popup is intentionally neutral product UI. It distinguishes:

- a resolved victory by the human civilization
- a resolved victory by another civilization
- an unresolved simultaneous result involving different civilizations

It does not claim to reproduce the original Civilization III end-screen presentation.

## Simultaneous results

If multiple victory claims belong to one civilization, that civilization is the resolved winner. If claims belong to different civilizations on the same round boundary, the finished game remains non-mutating but the winner is not invented. The persisted claims remain available for future original-game precedence verification.

## Verification

Public deterministic tests verify that gameplay messages are rejected once an outcome exists, gameplay messages still function before game over, and the frontend outcome message carries the persisted outcome object. Full Godot/.NET compilation verifies the frontend integration on macOS and Linux, while export CI covers macOS, Windows, and Linux.
