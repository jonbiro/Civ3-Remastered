# Known Civilization III compatibility gaps

This file records concrete differences or remaining uncertainties discovered while comparing the donor engine with the original Civilization III: Conquests data and documentation. It is intentionally evidence-based. Items should be removed when covered by a regression test and corrected or verified behavior.

Statuses use the definitions in `docs/COMPATIBILITY_PLAN.md`.

## UNKNOWN: exact resistance random-call ordering needs oracle fixtures

**Area:** captured cities / resistance / deterministic compatibility

The engine imports the six stock culture-ratio bands, ordered government-versus-government resistance modifiers, and each difficulty level's `MilitaryLaw` value. Per-turn resistance can end through peace or military garrisoning; only qualifying ground combat units count toward the garrison cap; resisters consume no food and are removed first by starvation.

The remaining uncertainty is exact original-game random-call ordering when a city contains multiple resisters, especially if several nationalities are represented. The implemented order is deterministic and source-backed, but it has not yet been compared against a sequence of original-game save fixtures.

**Required verification:** capture Civ III saves immediately before resistance processing, record resulting resister counts across repeated deterministic scenarios, and compare the engine's per-citizen roll order and nationality handling.

## UNSUPPORTED: war-weariness event accounting

**Area:** diplomacy / war state / citizen happiness

The stock BIQ government values are imported: Republic and Feudalism use low war weariness, Democracy uses high war weariness, and the remaining stock governments use none. The runtime does not yet track the original game's event-driven war-weariness points, thresholds, aggressor/defender distinctions, or per-opponent history.

**Required implementation:** add persistent per-opponent war-weariness state, source-backed point events and thresholds, peace-time reset/decay behavior, and deterministic mood fixtures before applying unhappy citizens.

## UNKNOWN: exact Golden Age within-turn sequencing needs oracle fixtures

**Area:** traits / combat / wonders / turn sequencing

Golden Age state, once-per-civilization enforcement, the imported duration, unique-unit victory triggers, cumulative Great Wonder trait triggers, save import/round trips, and production/commerce tile bonuses are implemented and covered by deterministic tests.

The original Civilopedia establishes triggers, duration, and yields, but does not fully specify within-turn sequencing. The implementation applies a combat-triggered Golden Age to the current yield cycle and a wonder-triggered Golden Age beginning with the next full yield cycle, avoiding partial application across a city's iteration order.

**Required verification:** use original-game saves immediately before combat and wonder-completion triggers to confirm the first and final affected production/commerce cycles and `GoldenAgeEndTurn` conversion boundary.

## Policy

Do not work around known differences by changing Classic-mode expected results. Classic mode should retain the original Civilization III behavior as the compatibility target. Enhanced/Remastered profiles may intentionally diverge only after the Classic expectation is represented by tests.
