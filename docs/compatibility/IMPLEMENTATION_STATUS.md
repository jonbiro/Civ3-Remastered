# Classic compatibility implementation status

This is a concise engineering inventory, not a percentage-complete claim. A subsystem is listed as covered only when a public deterministic behavior test or an opt-in original-file parity test exists.

## Covered in the current compatibility stack

- Civilization III Complete installation validation and original-file discovery
- Core Conquests BIQ parameter parity for difficulty, governments, terrain defense, experience, resources, trade connectors, culture/resistance bands, war-weariness modes, Golden Age duration, and Golden-Age-triggering units
- Four-slot technology prerequisite import
- Road and railroad movement costs
- Terrain, fortification, river, settlement-size, Fortress, and Barricade defense
- Settlement defense suppression while resisters remain
- Runtime/native-save/Civ III SAV resistance state
- Land, harbor, and airport trade connectivity
- Sea/ocean technology gates and naval blockades
- Trade-network cache invalidation for diplomacy, connector buildings, trade-enabling technologies, and naval movement/removal
- Capital connectivity feeding distance corruption
- Per-turn resistance continuation and quelling, including peace, culture bands, government-pair modifiers, garrison eligibility, difficulty limits, food consumption, and starvation priority
- Golden Age once-per-civilization state and native save round trips
- Import of active/previous Golden Age state from Civ III saves
- Golden Age triggers from flagged unit victories against other civilizations, excluding barbarians
- Golden Age triggers from cumulative player-built Great Wonder traits matching civilization traits
- Golden Age +1 production/+1 commerce tile yields, zero-yield handling, imported duration, and end boundary
- Golden-Age-triggering units remain available until the civilization has actually experienced its Golden Age
- Per-opponent signed war-weariness state, native save persistence, and original Civ III SAV import
- Reverse-engineered 31/61/91/121 point levels and peace-time decay toward zero
- Defensive-war happiness, hostile-territory exposure, combat loss/attack, bombardment, improvement-loss, and city-loss event hooks
- Low/high government mood effects, per-opponent rounding, Police Station mitigation, and Universal Suffrage mitigation
- High-war-weariness government collapse into the normal anarchy transition

## Imported and available, but not yet behavior-complete

- Government assimilation chance

## Next behavior slices

1. End-to-end strategic/luxury resource distribution through the trade network
2. Original-game oracle fixtures for war-weariness event ordering, AI asymmetries, and declaration causes
3. Original-game save fixtures for exact Golden Age trigger-cycle timing
4. Original-game save fixtures for exact resistance random-call ordering

## Rules for updating this file

- Do not mark a feature covered solely because a field parses.
- Link behavior to deterministic tests wherever possible.
- Keep original Firaxis files outside the repository.
- Do not turn the inventory into a completion percentage until the full category coverage inventory is documented.
