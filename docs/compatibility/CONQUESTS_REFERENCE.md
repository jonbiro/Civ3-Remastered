# Civilization III: Conquests reference contracts

This document records non-copyrightable compatibility facts extracted from a user-provided Civilization III: Complete installation. The original Firaxis files are **not** committed to this repository.

## Reference source identity

These hashes identify the exact local files used to establish the initial compatibility contracts. They are provenance only. Contributors supply their own original files locally.

| File | SHA-256 |
| --- | --- |
| `conquests.biq` | `1bd610ab1420217f7d64a9214b60c0bc7686e4781a0d6e66590171535495609c` |
| `script.txt` | `a4f38e3af3b0af26e9f5d2c9f1f591acd1205eb225f14cdddda1e74de26b48fa` |
| `diplomacy.txt` | `7ea49573286f4f2dc468f0e557b36f564670326ace9bc6737a51b9bd21639f94` |
| `PediaIcons.txt` | `034023423b90c0bd166f49fc3c6ec1e8ee0c8c1be155efe82a26e100bda0416a` |
| `labels.txt` | `ad6f5703249935c8c5e6eb5d4f60dee38d6720405611aca79c1da8b95fdec49a` |
| `Civilopedia.txt` | `f88e8020bb69fc489e5fccc52ec371571510f99961b9d92889278800c68bd2c2` |
| `credits.txt` | `7540e5a9ccd7f2bcedf64bcfb480265eb97ce51027ae5eab61f24e59f15aeecc` |
| `complete_credits.txt` | `15bddebdecb84f00e7020a5158d79e625452ac56a2efcb889eeb8f203f44e26e` |

## Initial machine-checkable contracts

`EngineTests/Compatibility/ConquestsReferenceRulesTest.cs` verifies these directly against the locally installed `Conquests/conquests.biq`.

### Difficulty levels and content citizens

| Difficulty | Citizens born content |
| --- | ---: |
| Chieftain | 4 |
| Warlord | 3 |
| Regent | 2 |
| Monarch | 2 |
| Emperor | 1 |
| Demigod | 1 |
| Deity | 1 |
| Sid | 1 |

### Government free unit support

Values are free units per town / city / metropolis.

| Government | Town | City | Metropolis |
| --- | ---: | ---: | ---: |
| Anarchy | 0 | 0 | 0 |
| Despotism | 4 | 4 | 4 |
| Monarchy | 2 | 4 | 8 |
| Republic | 1 | 3 | 4 |
| Feudalism | 5 | 2 | 1 |
| Communism | 6 | 6 | 6 |
| Fascism | 4 | 7 | 10 |
| Democracy | 0 | 0 | 0 |

### Defensive bonuses

Global/structural values covered by the BIQ parity test:

- fortified unit: +25%
- across a river: +25%
- town: +50%
- city: +50%
- metropolis: +100%

Terrain values covered:

- desert, plains, grassland, tundra, flood plain, coast, sea, ocean: +10%
- marsh: +20%
- forest and jungle: +25%
- hills: +50%
- volcano: +80%
- mountains: +100%

### Resistance contracts

The stock Conquests rules define six culture-ratio bands. Values are the chance that a foreign citizen initially resists / continues resisting on a later turn.

| Culture relationship | Ratio threshold | Initial | Continued |
| --- | ---: | ---: | ---: |
| in awe of | 300% | 40% | 30% |
| admirers of | 200% | 50% | 40% |
| impressed with | 100% | 60% | 50% |
| unimpressed by | 75% | 70% | 60% |
| dismissive of | 50% | 80% | 70% |
| disdainful of | 33% | 90% | 80% |

The BIQ also supplies an ordered government-versus-government resistance modifier matrix. The stock difficulty levels all use `MilitaryLaw = 1`, so each qualifying ground combat unit can quell at most one resister per turn under the default rules.

### Other baseline facts

- Four experience levels: Conscript, Regular, Veteran, Elite.
- Twenty-seven natural resources.
- The stock Harbor carries the BIQ water-trade flag and the stock Airport carries the air-trade flag.
- The stock technology tree contains rules that enable trade over sea and over ocean.
- Republic and Feudalism have low war weariness; Democracy has high war weariness; Monarchy and the other stock non-representative governments have none.
- The stock Golden Age duration is 20 turns.

## Behavioral coverage implemented

The public-CI compatibility suite covers both imported rule parameters and engine behavior for the first Classic-mode slices.

- Republic free-unit support across town, city, and metropolis population boundaries and mixed settlement sizes.
- Hills, mountains, fortification, and river-crossing defense modifiers.
- Town, city, and metropolis defensive bonuses.
- A resisting citizen suppresses the normal settlement-size defensive bonus.
- Road movement costs one third of a movement point on a continuous road route.
- Railroad movement costs zero movement points on a continuous rail route.
- A broken road route falls back to the destination terrain movement cost.
- Four technology prerequisite slots import independently, including the fourth slot.
- Land trade uses the city marked as the actual capital rather than assuming the first city in a list is the capital.
- Land trade crosses neutral or peaceful territory but stops at territory owned by a civilization currently at war with the player.
- Declaring war and signing peace invalidate the cached trade network so route availability is recalculated.
- A road connection to the capital reduces distance corruption compared with the equivalent disconnected city.
- Resistance state survives native save conversion and is imported from Civ III save citizen records identified as resisters.
- Harbors connect cities through explored coast tiles.
- Sea and ocean trade traversal is gated by the corresponding BIQ technology capabilities.
- Enemy naval units at war block water-trade paths; moving or removing naval units invalidates the cached network so blockades update.
- Airports merge otherwise disconnected city trade segments.
- Building and technology changes that create new water/air trade capabilities invalidate the cached trade network.
- Per-turn resistance uses the imported culture band and ordered government-pair modifier.
- Peace with the resister's mother country ends resistance without requiring a garrison.
- Only qualifying ground combat units count toward the per-turn garrison cap; sea, air, artillery-only, worker, and settler units do not.
- The difficulty `MilitaryLaw` value multiplies the per-unit quelling cap.
- Resisters consume no food and are removed before productive citizens when starvation reduces population.
- Culture relationship levels and government resistance data survive native save round trips.
- Government war-weariness levels and Golden Age duration are imported and available to the runtime for the next behavior slices.

## Source-derived contracts still queued

- Exact original resistance random-call ordering and mixed-nationality handling need original-game oracle fixtures.
- War weariness needs persistent per-opponent event accounting, thresholds, aggressor/defender handling, and happiness integration.
- Golden Ages need trigger bookkeeping, once-per-civilization enforcement, the 20-turn lifecycle, and production/commerce bonuses.
- A fortress gives +50% defense and a barricade gives +100%; their direct terrain-improvement interaction still needs dedicated behavior tests.
- A single connected strategic or luxury resource supplies all connected cities in a civilization; broader end-to-end resource-distribution fixtures are still needed.

## Test policy

1. Original assets and text remain outside the repository.
2. Prefer small expected numeric/state contracts over copied original text.
3. Every obscure expected value should record its source category here.
4. Parser checks establish that we can read the original data correctly. They do **not** prove the engine behaves correctly.
5. Each parser contract should eventually be paired with an engine behavior test where applicable.
6. Differences discovered during implementation are classified as `BUG`, `UNKNOWN`, `INTENTIONAL`, or `UNSUPPORTED` per `docs/COMPATIBILITY_PLAN.md`.

## Next implementation slice

Continue the source-derived behavior work in this order:

1. war-weariness event accounting and mood effects
2. Golden Age lifecycle and production/commerce bonuses
3. fortress and barricade behavior tests
4. end-to-end strategic/luxury resource distribution across connected trade networks
5. original-save oracle fixtures for resistance random-call ordering
