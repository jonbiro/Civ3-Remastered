# Civilization III war-weariness implementation plan

## Evidence boundary

The original Civilopedia establishes the user-facing behavior: war weariness applies under representative governments, worsens through aggressive or costly wars, may produce civil disorder, and stops affecting citizens when peace is signed.

The BIQ supplies the government's qualitative mode (`None`, `Low`, or `High`). It does not contain the hidden per-opponent point thresholds or individual event values.

The detailed point model below comes from long-standing CivFanatics save-file experiments and is therefore treated as reverse-engineered compatibility evidence, not official documentation. Every disputed edge case remains explicit in `KNOWN_GAPS.md` until tested against original saves.

## State model

Each ordered player relationship stores one signed war-weariness point total.

- negative points: war happiness while actively at war
- 0 through 30: no citizen effect
- 31 through 60: level 1
- 61 through 90: level 2
- 91 through 120: level 3
- 121 or more: level 4

The point balance persists through peace and decays toward zero. Citizen effects are active only while the two civilizations are at war.

## Initial supported point events

- direct unprovoked declaration against a civilization: defender receives -30 points
- beginning a turn with at least one unit in that opponent's territory: +1
- losing a non-defending unit: +1
- losing an attacking unit: +2
- having a defending combat unit attacked: +2, regardless of whether it survives
- having a unit bombarded to one hit point: +1
- losing a size-1 city: +16
- losing a larger city: +17
- losing an improvement to pillage or bombard: +1

Because the original human/AI bookkeeping contains known asymmetric bugs, the first implementation will apply the logical player-facing event to the civilization that suffered it. Historical AI mirroring bugs will be tracked separately rather than silently reproduced.

## Per-turn changes

While at war:

- add one point if the player has at least one unit in the opponent's territory
- if the relationship is at level 1 or higher and neither side occupies the other's territory, subtract one point

While at peace:

- move the signed point balance toward zero by `ceil(abs(points) / 20)` each turn

## Citizen effects

Calculate each active opponent separately and round each city's effect down before adding the results.

War happiness:

- any government, negative points: 25% of laborers move toward happy
- happiness effects are not reduced by Police Stations or Universal Suffrage

Low war weariness, used by Republic and Feudalism:

- level 1: 25% unhappy
- level 2: 50% unhappy
- level 3: 50% unhappy
- level 4: 100% unhappy

High war weariness, used by Democracy:

- level 1: 50% unhappy
- level 2: 100% unhappy
- level 3 or higher: government collapse/revolt

Mitigation:

- Police Station reduces the percentage penalty by 25 percentage points in that city
- Universal Suffrage reduces the empire-wide result by one unhappy citizen per city
- total war-weariness unhappiness cannot exceed the number of eligible laborers

## Implementation sequence

1. Persistent per-opponent signed point state and level calculation.
2. War declaration, peace decay, and territory-turn accounting.
3. Combat and city-loss event hooks.
4. Police Station and Universal Suffrage BIQ flag import.
5. City mood integration and government-collapse signal.
6. Native save round-trip tests.
7. Original-save oracle fixtures for disputed AI asymmetries and exact event ordering.
