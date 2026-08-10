# Classic compatibility implementation status

This is a concise engineering inventory, not a percentage-complete claim. A subsystem is listed as covered only when a public deterministic behavior test or an opt-in original-file parity test exists.

## Covered in the current compatibility branch

- Civilization III Complete installation validation and original-file discovery
- Core Conquests BIQ parameter parity for difficulty, governments, terrain defense, experience, resources, trade connectors, culture/resistance bands, war-weariness modes, and Golden Age duration
- Four-slot technology prerequisite import
- Road and railroad movement costs
- Terrain, fortification, river, and settlement-size defense
- Settlement defense suppression while resisters remain
- Runtime/native-save/Civ III SAV resistance state
- Land, harbor, and airport trade connectivity
- Sea/ocean technology gates and naval blockades
- Trade-network cache invalidation for diplomacy, connector buildings, trade-enabling technologies, and naval movement/removal
- Capital connectivity feeding distance corruption
- Per-turn resistance continuation and quelling, including peace, culture bands, government-pair modifiers, garrison eligibility, difficulty limits, food consumption, and starvation priority

## Imported and available, but not yet behavior-complete

- Government war-weariness level
- Government assimilation chance
- Golden Age duration

## Next behavior slices

1. Per-opponent war-weariness event accounting and citizen mood effects
2. Golden Age triggers, once-per-civilization state, 20-turn lifecycle, and tile yields
3. Fortress and barricade direct behavior tests
4. End-to-end strategic/luxury resource distribution through the trade network
5. Original-game save fixtures for exact resistance random-call ordering

## Rules for updating this file

- Do not mark a feature covered solely because a field parses.
- Link behavior to deterministic tests wherever possible.
- Keep original Firaxis files outside the repository.
- Do not turn the inventory into a completion percentage until the full category coverage inventory is documented.
