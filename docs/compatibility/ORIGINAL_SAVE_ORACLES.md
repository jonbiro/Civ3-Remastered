# Private original-save oracle fixtures

This harness compares paired, contributor-owned Civilization III: Conquests save files without committing Firaxis files to the repository. Public CI tests the manifest parser and path boundary only. Exact original-game comparisons run locally when both a validated Civilization III Complete installation and a private fixture directory are configured.

## Environment

Set both variables before running the opt-in tests:

```bash
export CIV3_HOME="/absolute/path/to/Civilization III Complete"
export CIV3_ORACLE_HOME="/absolute/path/to/private/civ3-oracles"
dotnet test EngineTests/EngineTests.csproj \
  --configuration Release \
  --filter FullyQualifiedName~OriginalSaveOracleFixtureTest
```

`CIV3_HOME` must pass the normal installation probe. `CIV3_ORACLE_HOME` may contain nested fixture directories. Every `*.json` file under that root is treated as an oracle manifest.

The fixture directory must stay outside the repository. Do not commit, upload, or attach original `.sav`, `.biq`, text, sound, or art files to issues, pull requests, CI artifacts, or release packages.

## Fixture layout

A fixture should keep its paired saves and manifest together:

```text
civ3-oracles/
  war-weariness/
    direct-declaration/
      before.sav
      after.sav
      fixture.json
  golden-age/
    unique-unit-trigger/
      before.sav
      after.sav
      fixture.json
```

Save paths are resolved relative to the manifest. The resolver rejects paths that leave `CIV3_ORACLE_HOME`.

## Manifest contract

```json
{
  "name": "direct declaration crosses level one",
  "beforeSave": "before.sav",
  "afterSave": "after.sav",
  "playerCivilization": "Rome",
  "opponentCivilization": "Greece",
  "cityName": "Rome",
  "before": {
    "turn": 40,
    "warWearinessPoints": 30,
    "atWar": false,
    "hasTriggeredGoldenAge": false,
    "goldenAgeTurnsRemaining": 0,
    "resisterCount": 0
  },
  "after": {
    "turn": 41,
    "warWearinessPoints": 31,
    "atWar": true,
    "hasTriggeredGoldenAge": false,
    "goldenAgeTurnsRemaining": 0,
    "resisterCount": 0
  },
  "delta": {
    "turn": 1,
    "warWearinessPoints": 1,
    "goldenAgeTurnsRemaining": 0,
    "resisterCount": 0
  }
}
```

Fields in `before`, `after`, and `delta` are optional. Omitted fields are not asserted. `opponentCivilization` is needed for relationship and war-weariness assertions. `cityName` is needed for resister assertions. Civilization and city selectors are case-insensitive but must identify exactly one imported object.

The current snapshot vocabulary is intentionally narrow:

- save turn number
- signed war-weariness points against one opponent
- active war state against that opponent
- whether the civilization has ever triggered a Golden Age
- remaining Golden Age turns
- resister count in one owned city

Expand the schema only when a concrete compatibility question requires another observable value.

## Capture procedure

1. Start from a controlled Conquests scenario or save with the relevant rules visible and all unrelated randomness minimized.
2. Save immediately before the single action or turn boundary under study.
3. Perform exactly one controlled action, end exactly one turn, or advance to the precise boundary being tested.
4. Save immediately afterward without performing unrelated moves.
5. Copy both saves into the private fixture directory and record the expected snapshots and deltas in the manifest.
6. Run the focused oracle test locally.
7. When an oracle disagrees with the remaster, preserve the fixture and document the mismatch before changing Classic-mode behavior.

A paired save establishes only the observed transition for that scenario. It does not, by itself, prove a general rule. Use multiple fixtures when player type, declaration cause, government, culture ratio, nationality, combat role, or turn ordering could change the result.

## Required capture matrix

### War weariness

- direct human declaration and direct AI declaration
- defensive-war happiness for human and AI defenders
- military alliance and mutual-protection-pact declarations
- failed-spy and nuclear-provocation declarations
- attacker lost, defender lost, and defender attacked but survives
- bombardment to one hit point
- building, wall, and tile-improvement destruction
- size-one city loss and larger-city loss
- pillage and field capture of zero-defense units
- hostile-territory exposure and recovery with units on both sides
- peace signing at 30, 31, 60, 61, 90, 91, 120, and 121 points
- positive and negative peace-decay rounding boundaries
- first and last mood cycles after war and peace
- Democracy collapse timing and the resulting anarchy boundary

### Golden Age

- unique-unit victory against a civilization
- unique-unit victory against barbarians as a negative control
- Great Wonder trait completion
- combat trigger before and after the civilization's city-yield pass
- wonder completion before and after the civilization's city-yield pass
- first affected production and commerce cycle
- final affected production and commerce cycle
- imported `GoldenAgeEndTurn` conversion at the exact boundary
- once-per-civilization behavior after an earlier Golden Age

### Resistance

- each of the six culture-ratio bands
- representative ordered government-pair modifiers
- zero, one, and several qualifying garrison units
- difficulty `MilitaryLaw` caps
- multiple resisters of one nationality
- mixed-nationality residents and resisters
- peace ending resistance
- starvation when both resisting and non-resisting residents exist
- repeated saves that expose exact per-citizen random-call ordering

## Interpreting failures

An oracle mismatch can come from the importer, the runtime rule, save timing, scenario setup, or an incorrect expected value. Report all observed before/after snapshots first. Do not change the expected manifest merely to make Classic mode pass. Enhanced or Remastered rules may intentionally diverge only after the original behavior is represented and retained as a Classic-mode contract.
