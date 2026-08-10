# Known Civilization III compatibility gaps

This file records concrete differences discovered while comparing the donor engine with the original Civilization III: Conquests data and documentation. It is intentionally evidence-based. Items should be removed when covered by a regression test and corrected behavior.

Statuses use the definitions in `docs/COMPATIBILITY_PLAN.md`.

## UNKNOWN: exact resistance random-call ordering needs oracle fixtures

**Area:** captured cities / resistance / deterministic compatibility

The engine now imports the six stock culture-ratio bands, the ordered government-versus-government resistance modifiers, and each difficulty level's `MilitaryLaw` value. Per-turn resistance can end through peace or military garrisoning; only qualifying ground combat units count toward the garrison cap; resisters consume no food and are removed first by starvation.

The remaining uncertainty is exact original-game random-call ordering when a city contains multiple resisters, especially if several nationalities are represented. The implemented order is deterministic and source-backed, but it has not yet been compared against a sequence of original-game save fixtures.

**Required verification:** capture Civ III saves immediately before resistance processing, record resulting resister counts across repeated deterministic scenarios, and compare the engine's per-citizen roll order and nationality handling.

## UNSUPPORTED: war-weariness event accounting

**Area:** diplomacy / war state / citizen happiness

The stock BIQ government values are imported: Republic and Feudalism use low war weariness, Democracy uses high war weariness, and the remaining stock governments use none. The runtime does not yet track the original game's event-driven war-weariness points, thresholds, aggressor/defender distinctions, or per-opponent history.

**Required implementation:** add persistent per-opponent war-weariness state, source-backed point events and thresholds, peace-time reset/decay behavior, and deterministic mood fixtures before applying unhappy citizens.

## UNSUPPORTED: Golden Age lifecycle and yields

**Area:** traits / combat / wonders / city yields

The stock `GoldenAgeDuration` rule is imported and verified as 20 turns. Trigger state, once-per-civilization enforcement, unique-unit victory triggers, wonder-trait triggers, and the production/commerce tile bonuses remain unimplemented.

**Required implementation:** add persistent Golden Age state and trigger bookkeeping, apply the original worked-tile yield bonuses, and cover start/end boundaries and save round trips.

## Policy

Do not work around known differences by changing Classic-mode expected results. Classic mode should retain the original Civilization III behavior as the compatibility target. Enhanced/Remastered profiles may intentionally diverge only after the Classic expectation is represented by tests.
