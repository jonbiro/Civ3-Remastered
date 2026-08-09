# Civ3 Remastered roadmap

## Milestone 1: Apple Silicon donor baseline and compatibility harness

Goal: establish a trustworthy development baseline on the M4 MacBook Pro before changing major gameplay systems.

### Baseline

- [ ] Build the fork successfully from a clean checkout on Apple Silicon macOS.
- [ ] Pin and document the Godot and .NET versions used by the fork.
- [ ] Run the full `EngineTests` suite on macOS.
- [ ] Configure a user-provided Civilization III Complete data directory without copying original game assets into the repository.
- [ ] Load at least one stock BIQ through the current importer.
- [ ] Load at least one original Civilization III SAV through the current importer.
- [ ] Run the donor headless simulation test for at least 50 turns.
- [ ] Reproduce or clear known macOS ARM64 launch/export issues against the current fork rather than relying on older release reports.

### Compatibility harness

- [ ] Establish a dedicated compatibility-test namespace/directory.
- [ ] Define a fixture schema for original-game expected behavior.
- [ ] Add first oracle-backed fixtures for map wrapping and tile distance.
- [ ] Add first oracle-backed fixtures for land/sea movement and roads.
- [ ] Add first oracle-backed fixtures for basic combat modifiers.
- [ ] Add first oracle-backed fixtures for city food/growth.
- [ ] Add first oracle-backed fixtures for production.
- [ ] Add a simple subsystem pass/fail summary that does not overstate completeness.

### Architectural seams

- [x] Introduce an additive `GameSession` scaffold.
- [ ] Add a deterministic random-source abstraction without changing current random-call order.
- [ ] Add a simulation-domain event abstraction.
- [ ] Introduce an `EngineStorage` compatibility facade backed by an explicit session.
- [ ] Migrate one low-risk subsystem away from direct static `EngineStorage.gameData` access.

## Milestone 2: Classic rules foundation

Verify and correct high-frequency mechanics before major UI or AI work.

- [ ] Movement and terrain entry rules.
- [ ] Roads, railroads, zone of control, and worker timing.
- [ ] Normal combat, defensive bombard, retreat, and promotion.
- [ ] City growth, starvation, aqueducts, hospitals, and granaries.
- [ ] Production, overflow, population-cost units, and hurry production.
- [ ] Commerce sliders, specialists, maintenance, and unit support.
- [ ] Corruption and waste including trade-network connection effects.
- [ ] Happiness, disorder, drafting, war weariness, and population-rush anger.
- [ ] Culture and border expansion.
- [ ] Research timing and era advancement.

## Milestone 3: Complete-game systems

- [ ] Governments and anarchy edge cases.
- [ ] Resources and trade-network behavior.
- [ ] Diplomacy, treaties, reputation, and trade valuation.
- [ ] Wonders and global/continental effects.
- [ ] Barbarians and goody huts.
- [ ] Artillery/bombardment canonical behavior.
- [ ] Air units and air missions.
- [ ] Naval transport, carriers, submarines, and amphibious behavior.
- [ ] Armies and leaders.
- [ ] Espionage.
- [ ] Victory conditions.
- [ ] Scenario-specific rules.

## Milestone 4: Modern macOS presentation

Only after core Classic behavior is trustworthy:

- [ ] Retina/HiDPI rendering.
- [ ] Resolution-independent/scalable UI.
- [ ] Modern camera pan and zoom.
- [ ] Improved unit-stack controls.
- [ ] Improved city management screens.
- [ ] Improved diplomacy and trade screens.
- [ ] Modern save/autosave UX.
- [ ] Search/filter/navigation conveniences.
- [ ] Optional higher-resolution asset pipeline.

## Milestone 5: Enhanced AI

Build stronger AI on top of the verified simulation rather than mixing AI redesign into rules reconstruction.

- [ ] Capability-based unit roles instead of name-based special cases.
- [ ] Strategic planning and war objectives.
- [ ] Improved settlement/site selection.
- [ ] Improved worker planning.
- [ ] Better production/research choices.
- [ ] Naval transport planning.
- [ ] Diplomacy and trade strategy.
- [ ] Performance budgets for large maps/late game.

## Working rules

- Original Civ III behavior is the Classic-mode oracle.
- Every compatibility correction should add a regression test when practical.
- Do not commit original Civilization III game assets.
- Prefer incremental migration to broad rewrites.
- Keep simulation independent from Godot presentation wherever practical.
- Do not treat a subsystem's passing test percentage as completeness without a coverage inventory.