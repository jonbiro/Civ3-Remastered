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

## Imported and available, but not yet behavior-complete

- Government war-weariness level
- Government assimilation chance

## Next behavior slices

1. Per-opponent war-weariness event accounting and citizen mood effects
2. End-to-end strategic/luxury resource distribution through the trade network
3. Original-game save fixtures for exact Golden Age trigger-cycle timing
4. Original-game save fixtures for exact resistance random-call ordering

## Rules for updating this file

- Do not mark a feature covered solely because a field parses.
- Link behavior to deterministic tests wherever possible.
- Keep original Firaxis files outside the repository.
- Do not turn the inventory into a completion percentage until the full category coverage inventory is documented.
