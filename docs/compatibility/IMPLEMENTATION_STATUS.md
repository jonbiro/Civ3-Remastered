# Classic compatibility implementation status

This is a concise engineering inventory, not a percentage-complete claim. A subsystem is listed as covered only when a public deterministic behavior test or an opt-in original-file parity test exists.

## Covered in the current compatibility stack

- Civilization III Complete installation validation and original-file discovery
- Core Conquests BIQ parameter parity for difficulty, governments, terrain defense, experience, resources, trade connectors, culture/resistance bands, war-weariness modes, Golden Age duration, Golden-Age-triggering units, and Cultural Victory settings
- Four-slot technology prerequisite import
- Road and railroad movement costs
- Terrain, fortification, river, settlement-size, Fortress, and Barricade defense
- Settlement defense suppression while resisters remain
- Runtime/native-save/Civ III SAV resistance state
- Land, harbor, and airport trade connectivity
- Sea/ocean technology gates and naval blockades
- Trade-network cache invalidation for diplomacy, connector buildings, trade-enabling technologies, and naval movement/removal
- Capital connectivity feeding distance corruption
- Strategic and luxury resource counts distributed through each city's connected land, harbor, and airport trade segment
- Resource prerequisite technologies controlling strategic/luxury visibility without changing physical route connectivity
- Connected strategic resources feeding unit and building production requirements
- Connected luxury resources feeding citizen moods, including removal when the route is broken
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
- Private paired-SAV oracle harness with stable civilization/city selectors, exact before/after snapshots, exact deltas, path-containment checks, and public-CI-safe opt-in behavior
- Explicit city-culture border thresholds at 10 / 100 / 1,000 / 10,000 / 20,000 culture
- BIQ Cultural Victory enable, one-city target, and civilization target imported without hardcoded scenario assumptions
- One-city and civilization-wide Cultural Victory qualification with deterministic exact-boundary tests
- Common persisted victory-outcome model shared by victory types
- BIQ Conquest and Domination enable flags plus Domination land/population thresholds imported unchanged
- Cultural, Conquest, and Domination victory claims evaluated at full-round boundaries
- Domination land and population thresholds checked with exact integer ratios, excluding water from world-land totals
- Native saves persist the first recorded outcome and its claims
- Simultaneous claims for one civilization resolve to that winner; cross-civilization simultaneous claims remain preserved and unresolved until original precedence is established
- Turn advancement stops after the first recorded full-round outcome instead of beginning another round
- Frontend restores completed saves into a dedicated non-mutating `GameOver` state while retaining postgame inspection
- Gameplay engine messages are rejected both when sent after game over and when dequeued after an outcome was recorded
- Victory, defeat, and unresolved simultaneous outcomes receive neutral postgame presentation without claiming unverified original-screen fidelity

## Imported and available, but not yet behavior-complete

- Government assimilation chance
- Culture-producing building/wonder construction age needed for the original 1,000-year culture-doubling rule
- BIQ Space Race, Diplomatic, Victory Point, Wonder, and turn-limit victory configuration is parseable but does not yet have a complete common-outcome evaluator

## Next behavior slices

1. Populate the private oracle matrix for war-weariness event ordering, AI asymmetries, declaration causes, and peace boundaries
2. Populate private save fixtures for exact Golden Age trigger-cycle timing and end boundaries
3. Populate private save fixtures for exact resistance random-call ordering and mixed-nationality handling
4. Complete Space Race and Diplomatic victory runtime state/evaluation, then Wonder/Victory Point/Histograph where scenario state supports them
5. Complete citizen mood, disorder, celebration, and government-transition compatibility beyond the already covered luxury and war-weariness effects
6. Add reliable building construction-time state and verify the 1,000-year culture-doubling boundary against original-game fixtures

See `docs/compatibility/ORIGINAL_SAVE_ORACLES.md` for the private fixture contract and required capture matrix. See `docs/compatibility/CULTURE_VICTORY.md`, `docs/compatibility/VICTORY_OUTCOMES.md`, and `docs/compatibility/GAME_OVER_STATE.md` for the culture, outcome, and postgame evidence boundaries.

## Rules for updating this file

- Do not mark a feature covered solely because a field parses.
- Link behavior to deterministic tests wherever possible.
- Keep original Firaxis files outside the repository.
- Do not turn the inventory into a completion percentage until the full category coverage inventory is documented.
