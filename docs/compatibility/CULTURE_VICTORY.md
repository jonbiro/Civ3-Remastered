# Civilization III culture and cultural victory compatibility

## Evidence boundary

The original Civilization III Complete `Civilopedia.txt` supplied privately by the contributor is the behavioral specification for this slice. It states that city culture accumulates from culture-producing improvements and Wonders, that border expansion occurs at the documented city-culture thresholds, that one city can satisfy a Cultural Victory, and that civilization-wide Cultural Victory uses a scenario/map-dependent culture target.

The BIQ `GAME` section is the machine-readable authority for whether Cultural Victory is enabled and for the scenario's `OneCityCultureWin` and `AllCitiesCultureWin` targets. Those values are imported directly rather than replaced with stock-game assumptions.

Original Firaxis files remain outside the repository and public CI.

## Implemented behavior

- culture remains stored per city and per controlling player
- city border rank uses explicit thresholds: 10, 100, 1,000, 10,000, and 20,000 culture
- threshold comparisons are inclusive, so reaching a threshold expands immediately
- BIQ Cultural Victory enable/disable state is imported into runtime rules
- BIQ one-city and civilization-wide culture targets are imported into runtime rules
- one-city qualification occurs at the exact configured threshold
- civilization qualification sums culture from the player's active cities
- scenario rules can disable Cultural Victory entirely
- simultaneous qualifying civilizations are preserved as multiple qualifications instead of being resolved according to player iteration order

The last point is deliberate. The donor engine has no common victory/outcome state yet. Cultural qualification is therefore represented separately so it can feed the same future resolution layer as Conquest, Domination, Space Race, Diplomatic, Wonder, Victory Point, and time-limit outcomes.

## Deterministic tests

Public synthetic tests cover every side of the culture-border boundaries and both cultural-victory paths. An opt-in private-file test imports the contributor's actual `Conquests/conquests.biq` and verifies that the parsed GAME values reach runtime `Rules` unchanged.

## Remaining culture work

The original Civilopedia says each culture-producing Wonder or improvement doubles its per-turn culture contribution after 1,000 years. The donor currently stores `year = 1` for newly built buildings with a TODO for in-game construction-year tracking. Until that state is reliable, the doubling rule is not guessed.

Required follow-up:

1. store the actual construction time for each building/wonder
2. define the 1,000-year age calculation for BC/AD and non-year scenario timescales
3. import original-save construction timing where the SAV exposes it
4. compare the first doubled culture cycle against private original-game oracle fixtures
5. integrate cultural qualifications into the common victory-resolution/game-ending layer
