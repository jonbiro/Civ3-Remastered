# Common victory outcome compatibility

## Purpose

Civilization III exposes several independent victory paths. The donor engine previously had no common persisted game-outcome state, which made it unsafe to implement one victory type by immediately ending the turn from inside a civilization update loop.

This layer introduces one outcome model that can be shared by every victory type.

## Implemented paths

### Cultural

The common resolver consumes the source-backed one-city and civilization-wide culture qualifications implemented in `CulturalVictory.cs`.

### Conquest

When the BIQ enables Conquest Victory, a civilization qualifies when it is the only included, non-barbarian, non-defeated civilization remaining.

### Domination

When the BIQ enables Domination Victory, a civilization must satisfy both configured GAME-section thresholds:

- percentage of world land territory controlled
- percentage of world population controlled

Water tiles are excluded from the territory denominator. Threshold comparisons use integer cross-multiplication rather than rounded floating-point percentages, so exact boundaries are deterministic.

## Scenario configuration

The following BIQ GAME values are imported directly into runtime rules:

- `ConquestVictory`
- `DominationVictory`
- `DominationTerrain`
- `DominationPopulation`

This preserves custom-scenario settings rather than replacing them with stock-game constants.

## Resolution boundary

Victory is evaluated after every included civilization has received the same complete round update. This prevents player-list iteration order from deciding an end-of-round condition.

The first outcome containing claims is persisted and never overwritten by later turns.

## Simultaneous claims

Multiple victory types claimed by the same civilization produce a resolved outcome for that civilization.

If different civilizations qualify on the same resolution boundary, all claims are preserved and the outcome remains unresolved. Original-game precedence between such simultaneous claims has not yet been established by the supplied sources, so Classic mode does not invent one.

## Save compatibility

Native saves persist the outcome turn and all claims. Old saves have a null outcome and continue normally.

## Follow-up victory paths

The shared model intentionally includes enum values for Space Race, Diplomatic, Wonder, Victory Point, and Histograph outcomes, but those are not evaluated until their required runtime state and original behavior are implemented and tested.
