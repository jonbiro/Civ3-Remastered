# Civilization III compatibility plan

## Objective

The project needs an objective definition of "finished." The primary measure should be behavioral compatibility with Civilization III Complete, not alpha/beta labels.

The original game is the oracle for Classic mode. We should build small, reproducible test fixtures that compare known original-game outcomes with this engine.

## Compatibility categories

Track at least the following independently:

- BIQ parsing and scenario import
- SAV parsing and save import
- map topology and wrapping
- terrain yields and movement
- roads, railroads, irrigation, mines, and worker timing
- unit movement and zone-of-control behavior
- combat
- bombardment and air missions
- city growth and starvation
- citizen assignment and specialists
- production and shield overflow
- corruption and waste
- commerce, sliders, maintenance, and unit support
- happiness, disorder, war weariness, drafting, and population rushing
- culture and border expansion
- resources and trade networks
- technologies and era advancement
- governments and anarchy
- wonders and building effects
- diplomacy, reputation, treaties, and trade valuation
- barbarians
- espionage
- armies and leaders
- victory conditions
- scenario-specific rules
- AI behavior, tracked separately from rules compatibility

## Test levels

### Level 1: formula tests

Small deterministic tests for isolated formulas and rules.

Examples:

- road movement cost
- granary food retention
- courthouse corruption modifier
- veteran-versus-regular promotion chances
- government unit-support allowance

### Level 2: fixture-state tests

Construct a small known game state and perform one action or one end-turn transition.

Examples:

- attack a fortified spearman across a river
- grow a size-6 city with and without fresh water
- complete a wonder when another civilization is building it
- declare war while units are inside another civilization's territory

### Level 3: imported Civ III save tests

Load a Civ III `.sav` captured immediately before a known action/end turn, import it, execute the same transition, and compare selected resulting state.

These fixtures should reference user-owned original-game data outside the repository unless redistribution is clearly permitted.

### Level 4: deterministic scenario traces

Starting from a known scenario/seed, apply a recorded command stream and compare checkpoints over many turns.

## Fixture format

Compatibility fixtures should eventually have a small metadata format similar to:

```json
{
  "id": "corruption-republic-courthouse-001",
  "category": "corruption",
  "source": "Civilization III Complete",
  "setup": "fixtures/corruption/republic-courthouse-001.json",
  "action": "end-turn",
  "expected": {
    "city": {
      "name": "Test City",
      "grossCommerce": 18,
      "corruptedCommerce": 5,
      "usableCommerce": 13
    }
  }
}
```

The exact schema can evolve. The important point is that expected values are explicit and reviewable.

## Compatibility dashboard

A future test-reporting tool should summarize pass/fail counts by subsystem, for example:

```text
Movement          124 / 128
Combat            211 / 224
Cities            155 / 170
Corruption         48 /  61
Diplomacy          12 /  39
```

Do not claim percentage completeness for a category until the category has a documented coverage inventory. A high pass rate over a tiny test set is not evidence that the subsystem is complete.

## Handling intentional differences

Every known difference in Classic mode should be one of:

- `BUG`: engine behavior is believed wrong
- `UNKNOWN`: original behavior has not been established
- `INTENTIONAL`: documented deviation from original Civ III
- `UNSUPPORTED`: feature not implemented yet

Enhanced and Remastered profiles may intentionally differ, but the Classic expectation should remain in the fixture set.

## Rules for compatibility fixes

1. Reproduce the original behavior before changing the implementation when practical.
2. Add or update a regression fixture/test.
3. Prefer narrow fixes over broad rewrites.
4. Preserve deterministic random-call order unless the change explicitly targets randomness behavior.
5. Record the source of obscure behavior in test comments or documentation.

## First compatibility targets

Start with mechanics that are both central and already substantially implemented in the donor engine:

1. map wrapping and tile distance
2. land/sea movement and roads/railroads
3. basic combat odds and modifiers
4. city food/growth
5. production
6. commerce sliders
7. unit support
8. corruption/waste
9. culture borders
10. technology research timing

These give us a trustworthy foundation before spending major effort on AI or presentation.