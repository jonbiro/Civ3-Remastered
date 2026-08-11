# Known Civilization III compatibility gaps

This file records concrete differences or remaining uncertainties discovered while comparing the donor engine with the original Civilization III: Conquests data and documentation. It is intentionally evidence-based. Items should be removed when covered by a regression test and corrected or verified behavior.

Statuses use the definitions in `docs/COMPATIBILITY_PLAN.md`.

## UNKNOWN: exact resistance random-call ordering needs oracle fixtures

**Area:** captured cities / resistance / deterministic compatibility

The engine imports the six stock culture-ratio bands, ordered government-versus-government resistance modifiers, and each difficulty level's `MilitaryLaw` value. Per-turn resistance can end through peace or military garrisoning; only qualifying ground combat units count toward the garrison cap; resisters consume no food and are removed first by starvation.

The remaining uncertainty is exact original-game random-call ordering when a city contains multiple resisters, especially if several nationalities are represented. The implemented order is deterministic and source-backed, but it has not yet been compared against a sequence of original-game save fixtures.

**Required verification:** capture Civ III saves immediately before resistance processing, record resulting resister counts across repeated deterministic scenarios, and compare the engine's per-citizen roll order and nationality handling.

## UNKNOWN: exact war-weariness event asymmetries and declaration causes need oracle fixtures

**Area:** diplomacy / war state / citizen happiness / deterministic compatibility

The runtime now stores a signed point balance for each opponent, imports the original SAV array, applies the reverse-engineered 31/61/91/121 levels, decays history during peace, and integrates war happiness/unhappiness into city moods. It also covers hostile-territory exposure, combat attacks/losses, bombardment to one hit point, bombardment destruction, city loss, Police Stations, Universal Suffrage, and high-war-weariness government collapse.

The best-known community research reports intentional or accidental AI/human asymmetries and says the -30 defensive-war offset is not awarded for every apparent declaration cause, such as some alliance/MPP or provoked wars. The donor engine currently exposes one generic declaration path, so Classic mode cannot yet distinguish every original cause. Pillage and field capture of non-defending units also lack complete donor-engine actions; non-defending losses are presently counted when a city is taken.

**Required verification:** create original-game saves around direct declarations, alliance/MPP declarations, failed-spy or nuclear provocations, mixed human/AI combats, pillage, non-defending-unit capture, peace signing, and Democracy collapse. Compare the exact ordered point deltas and first/last affected mood cycles before encoding disputed asymmetries as Classic-mode behavior.

## UNKNOWN: exact Golden Age within-turn sequencing needs oracle fixtures

**Area:** traits / combat / wonders / turn sequencing

Golden Age state, once-per-civilization enforcement, the imported duration, unique-unit victory triggers, cumulative Great Wonder trait triggers, save import/round trips, and production/commerce tile bonuses are implemented and covered by deterministic tests.

The original Civilopedia establishes triggers, duration, and yields, but does not fully specify within-turn sequencing. The implementation applies a combat-triggered Golden Age to the current yield cycle and a wonder-triggered Golden Age beginning with the next full yield cycle, avoiding partial application across a city's iteration order.

**Required verification:** use original-game saves immediately before combat and wonder-completion triggers to confirm the first and final affected production/commerce cycles and `GoldenAgeEndTurn` conversion boundary.

## Policy

Do not work around known differences by changing Classic-mode expected results. Classic mode should retain the original Civilization III behavior as the compatibility target. Enhanced/Remastered profiles may intentionally diverge only after the Classic expectation is represented by tests.
