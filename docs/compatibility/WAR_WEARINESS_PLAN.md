# Civilization III war-weariness implementation

## Evidence boundary

The original Civilopedia establishes the player-facing behavior: representative governments suffer war weariness, aggressive and overseas wars are less tolerable, and peace removes the active citizen effect. The BIQ supplies each government's qualitative mode (`None`, `Low`, or `High`) and the Police Station / Universal Suffrage mitigation flags.

The signed point totals, event values, and 31/61/91/121 thresholds are reverse-engineered compatibility evidence. They remain explicitly testable and may be corrected when original-save oracle fixtures disagree.

## Implemented state model

Each ordered player relationship stores one signed point balance.

- negative points: war happiness while actively at war
- 0 through 30: no citizen effect
- 31 through 60: level 1
- 61 through 90: level 2
- 91 through 120: level 3
- 121 or more: level 4

The balance survives native saves and persists through peace while decaying toward zero. Original Civ III saves import the per-opponent array directly.

## Implemented events

- direct declaration against a civilization: defender receives -30 points
- beginning a turn with a unit in that opponent's territory: +1
- losing an attacking unit: +2
- having a defending combat unit attacked: +2 whether it survives or not
- being bombarded down to one hit point: +1
- losing a size-1 city: +16
- losing a larger city: +17
- losing a building, walls, or tile improvement to bombardment: +1
- losing a zero-defense unit during city capture: +1 per unit

Pillage and field capture of non-defending units remain follow-up work because those actions are not yet represented consistently by the donor engine.

## Per-turn behavior

While at war, hostile-territory exposure adds one point. If a level is active and neither side occupies the other's territory, one point is recovered. During peace, the signed balance moves toward zero by `ceil(abs(points) / 20)` each turn.

## Citizen effects

Each opponent's city result is rounded down independently before totals are added. Negative points provide 25% war happiness. Low war weariness uses 25%, 50%, 50%, and 100% unhappiness for levels 1-4. High uses 50% and 100% for levels 1-2; level 3 or higher collapses the government into the normal anarchy transition.

After independently rounded opponent contributions are added, a Police Station removes 25% of the city's laborers from the aggregate unhappy result. Universal Suffrage removes one additional resulting unhappy citizen per city.

## Verification policy

Public CI verifies the hidden point model with deterministic synthetic states on both macOS ARM64 and Linux. Original-file checks validate BIQ flags without committing Firaxis data. Exact event ordering remains provisional until compared with purpose-built original-game save fixtures.

## Remaining oracle work

- exact original AI/human asymmetries
- precise ordering when several events occur in one combat
- pillage and non-defending-unit loss events
- first/last turn timing around peace treaties
- original-save fixtures for Democracy collapse timing
