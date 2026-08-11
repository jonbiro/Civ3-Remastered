using System;

namespace C7GameData;

/// <summary>
/// Civilization III war-weariness point thresholds and player-facing effects.
///
/// The government mode is read from the BIQ. The hidden point values are
/// reverse-engineered compatibility contracts and are kept here so event hooks,
/// mood calculations, and tests use one explicit source of truth.
/// </summary>
public static class WarWearinessRules {
	public const int DefensiveWarHappiness = -30;
	public const int UnitInEnemyTerritoryPerTurn = 1;
	public const int WartimeRecoveryPerTurn = 1;
	public const int LostNonDefendingUnit = 1;
	public const int LostAttackingUnit = 2;
	public const int DefendingUnitAttacked = 2;
	public const int BombardedToOneHitPoint = 1;
	public const int LostSizeOneCity = 16;
	public const int LostLargerCity = 17;
	public const int LostImprovement = 1;

	public static int LevelForPoints(int points) {
		if (points <= 30) return 0;
		if (points <= 60) return 1;
		if (points <= 90) return 2;
		if (points <= 120) return 3;
		return 4;
	}

	public static int DecayTowardZeroAtPeace(int points) {
		if (points == 0) return 0;
		int amount = (int)Math.Ceiling(Math.Abs(points) / 20.0);

		// Civ III also applies its normal one-point level recovery while at
		// peace whenever a positive war-weariness level is still active.
		if (LevelForPoints(points) > 0) {
			amount += WartimeRecoveryPerTurn;
		}

		return points > 0 ? Math.Max(0, points - amount) : Math.Min(0, points + amount);
	}

	public static int UnhappyPercentage(Government.WarWearinessLevel governmentMode, int level) {
		if (level <= 0 || governmentMode == Government.WarWearinessLevel.None) {
			return 0;
		}

		return governmentMode switch {
			Government.WarWearinessLevel.Low => level switch {
				1 => 25,
				2 => 50,
				3 => 50,
				_ => 100,
			},
			Government.WarWearinessLevel.High => level switch {
				1 => 50,
				_ => 100,
			},
			_ => 0,
		};
	}

	public static int PercentageOfLaborers(int laborers, int percentage) {
		return Math.Max(0, laborers) * Math.Max(0, percentage) / 100;
	}
}
