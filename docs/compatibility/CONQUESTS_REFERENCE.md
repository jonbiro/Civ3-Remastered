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

Global/structural values currently covered by the BIQ parity test:

- fortified unit: +25%
- across a river: +25%
- town: +50%
- city: +50%
- metropolis: +100%

Terrain values currently covered:

- desert, plains, grassland, tundra, flood plain, coast, sea, ocean: +10%
- marsh: +20%
- forest and jungle: +25%
- hills: +50%
- volcano: +80%
- mountains: +100%

### Other baseline facts

- Four experience levels: Conscript, Regular, Veteran, Elite.
- Twenty-seven natural resources.

## Source-derived contracts queued for behavioral tests

The original data also documents the following rules. These should become engine-level behavior tests rather than parser-only assertions:

- Golden Age duration is 20 turns.
- Road movement costs one third of a movement point per tile.
- Railroad movement costs zero movement points.
- A fortress gives +50% defense and a barricade doubles that fortress bonus.
- Cities connect for trade through road/rail, compatible harbor water routes, or airports.
- Enemy territory can break a road/rail trade route and enemy naval units can blockade a harbor route.
- Republic, Feudalism, and Democracy are subject to war weariness.
- A single connected strategic or luxury resource supplies all connected cities in a civilization.

## Behavioral coverage implemented

- Republic free-unit support is covered across the town/city/metropolis population boundaries and across mixed settlement sizes.
- Hills, mountains, and fortification defense modifiers are covered by synthetic engine tests that run in public CI.
- River crossing and city-size defense modifiers are the next combat contracts to add.

## Test policy

1. Original assets and text remain outside the repository.
2. Prefer small expected numeric/state contracts over copied original text.
3. Every obscure expected value should record its source category here.
4. Parser checks establish that we can read the original data correctly. They do **not** prove the engine behaves correctly.
5. Each parser contract should eventually be paired with an engine behavior test where applicable.
6. Differences discovered during implementation are classified as `BUG`, `UNKNOWN`, `INTENTIONAL`, or `UNSUPPORTED` per `docs/COMPATIBILITY_PLAN.md`.

## Next implementation slice

Continue the source-derived behavior work in this order:

1. river crossing and city-size combat modifiers
2. road and railroad movement
3. trade-network connectivity and blockades
4. difficulty/content-citizen behavior
5. war weariness
6. Golden Age duration and production/commerce bonuses
