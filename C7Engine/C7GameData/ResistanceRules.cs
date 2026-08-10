using System;
using System.Collections.Generic;
using System.Linq;

namespace C7GameData;

/// <summary>
/// One Civilization III culture-comparison band used by resistance,
/// propaganda, and related captured-city calculations.
/// </summary>
public class CultureRelationshipLevel {
	public string name;
	public int cultureRatioPercentage;
	public int cultureRatioDenominator;
	public int cultureRatioNumerator;
	public int initialResistanceChance;
	public int continuedResistanceChance;
}

/// <summary>
/// Civilization III resistance calculations that are shared by city capture
/// and the per-turn resistance-quelling lifecycle.
/// </summary>
public static class ResistanceRules {
	public static int TotalCulture(GameData gameData, Player player) {
		if (player == null) {
			return 0;
		}

		int result = 0;
		foreach (City city in gameData.cities) {
			if (city.perPlayerCulture.TryGetValue(player, out int culture)) {
				result += culture;
			}
		}
		return result;
	}

	public static CultureRelationshipLevel GetCultureRelationship(
		GameData gameData,
		Player conqueror,
		Player conquered
	) {
		if (gameData.cultureRelationshipLevels.Count == 0) {
			return null;
		}

		long conquerorCulture = TotalCulture(gameData, conqueror);
		long conqueredCulture = TotalCulture(gameData, conquered);

		// Equal zero-culture civilizations are treated as culturally equal.
		if (conquerorCulture == 0 && conqueredCulture == 0) {
			return gameData.cultureRelationshipLevels
				.OrderBy(level => Math.Abs(level.cultureRatioPercentage - 100))
				.First();
		}

		// The BIQ levels are expressed as the conqueror's culture divided by
		// the conquered civilization's culture. Evaluate the ratio with integer
		// cross multiplication so large culture totals do not lose precision.
		foreach (CultureRelationshipLevel level in gameData.cultureRelationshipLevels
				.OrderByDescending(level => (double)level.cultureRatioNumerator / Math.Max(1, level.cultureRatioDenominator))) {
			if (conquerorCulture * level.cultureRatioDenominator
					>= conqueredCulture * level.cultureRatioNumerator) {
				return level;
			}
		}

		return gameData.cultureRelationshipLevels
			.OrderBy(level => (double)level.cultureRatioNumerator / Math.Max(1, level.cultureRatioDenominator))
			.First();
	}

	public static int ResistanceChance(
		GameData gameData,
		Player conqueror,
		Player conquered,
		bool continuedResistance
	) {
		CultureRelationshipLevel relationship = GetCultureRelationship(gameData, conqueror, conquered);
		if (relationship == null) {
			// Preserve resistance rather than silently erasing it when a legacy
			// save or non-Civ3 ruleset has no resistance data.
			return 100;
		}

		int baseChance = continuedResistance
			? relationship.continuedResistanceChance
			: relationship.initialResistanceChance;
		int governmentModifier = conqueror?.government?.ResistanceModifierAgainst(conquered?.government) ?? 0;
		return Math.Clamp(baseChance + governmentModifier, 0, 100);
	}

	public static int MaximumQuelledByGarrison(GameData gameData, City city) {
		int qualifyingUnits = city.location.unitsOnTile.Count(unit =>
			unit.owner == city.owner
			&& unit.IsLandUnit()
			&& unit.unitType.attack > 0
			&& unit.unitType.defense > 0);
		return qualifyingUnits * Math.Max(0, gameData.gameDifficulty.MilitaryLaw);
	}
}
