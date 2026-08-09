# Known Civilization III compatibility gaps

This file records concrete differences discovered while comparing the donor engine with the original Civilization III: Conquests data and documentation. It is intentionally evidence-based. Items should be removed only when covered by a regression test and corrected behavior.

Statuses use the definitions in `docs/COMPATIBILITY_PLAN.md`.

## BUG: fourth technology prerequisite imports the third prerequisite

**Area:** BIQ import / technology tree

`ImportCiv3.ImportTechs()` correctly checks whether `Prerequisite4` exists, but then adds `Prerequisite3` to the imported prerequisite set a second time. A ruleset or scenario using all four prerequisite slots can therefore lose its fourth prerequisite in the C7 model.

**Required fix:** change the fourth-prerequisite import to use `Prerequisite4`, then add a focused regression test using a synthetic four-prerequisite technology or a redistributable fixture.

## UNSUPPORTED: city resistance is not represented in runtime residents

**Area:** captured cities / combat defense / happiness / culture

The original Conquests rules distinguish resisting citizens and explicitly suppress the normal city-size defensive bonus while a city has resisters. The current runtime `CityResident` model records citizen type, worked tile, nationality, city and mood, but no resistance state. `City.GetDefenseBonuses()` therefore always supplies the town/city/metropolis defense bonus based on population size.

**Required implementation:** represent resistance in runtime and save-state residents, import resistance from Civ III saves, and make city defense, labor, resistance-quelling and related systems consume the same state. Add a regression test proving that a city with at least one resister does not receive its normal city-size defensive bonus.

## UNSUPPORTED: capital trade connectivity is ignored by corruption

**Area:** corruption / trade network

The original rules state that connection to the capital by road, harbor or airport reduces corruption and waste. The current distance-corruption calculation contains a placeholder `connectedTocapital = false`, so that factor is never applied.

**Required implementation:** use the engine trade-network model to determine capital connectivity, invalidate/recompute it on relevant network changes, and cover connected/disconnected city cases with deterministic corruption fixtures.

## Policy

Do not work around these differences by changing Classic-mode expected results. Classic mode should retain the original Civilization III behavior as the compatibility target. Enhanced/Remastered profiles may intentionally diverge only after the Classic expectation is represented by tests.
