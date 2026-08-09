# Known Civilization III compatibility gaps

This file records concrete differences discovered while comparing the donor engine with the original Civilization III: Conquests data and documentation. It is intentionally evidence-based. Items should be removed when covered by a regression test and corrected behavior.

Statuses use the definitions in `docs/COMPATIBILITY_PLAN.md`.

## UNSUPPORTED: resistance quelling lifecycle is incomplete

**Area:** captured cities / resistance / happiness / culture

Runtime and save-state residents now preserve whether a citizen is resisting, Civ III save import recognizes `CTZN.Type == 3` as a resister, mood calculations exclude resisters, and the normal settlement-size defensive bonus is suppressed while resistance remains.

The remaining gap is the lifecycle that reduces and ends resistance. Civ III can quell resisters through military garrisoning and ending the war, and that behavior still needs deterministic implementation and fixtures.

**Required implementation:** model per-turn resistance quelling, including the relevant government/difficulty/culture modifiers, and add fixtures that begin with known resister counts and verify the original turn-by-turn outcomes.

## UNSUPPORTED: harbor and airport trade routes

**Area:** trade network / resources / corruption

Land trade now uses the actual capital, follows continuous road/rail segments, stops at wartime enemy territory, invalidates the cached network when war or peace changes, and feeds capital connectivity into distance corruption.

The original rules also connect cities through compatible harbor water routes and through airports. Water routes can be blocked by enemy naval forces and depend on the civilization's ability to traverse the relevant water tiles. Those paths are not yet represented by the trade-network flood fill.

**Required implementation:** import the existing BIQ building flags for water and air trade, connect qualifying airport networks, implement technology-aware water-route traversal between harbors, account for naval blockades, and cover each route type with deterministic tests.

## Policy

Do not work around these differences by changing Classic-mode expected results. Classic mode should retain the original Civilization III behavior as the compatibility target. Enhanced/Remastered profiles may intentionally diverge only after the Classic expectation is represented by tests.
