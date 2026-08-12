using System.Collections.Generic;
using System.Linq;

namespace C7GameData;

public enum CulturalVictoryPath {
	OneCity,
	Civilization,
}

/// <summary>
/// A source-backed cultural-victory qualification. This deliberately records
/// qualification rather than ending the game; all victory types will feed a
/// common outcome layer so simultaneous conditions can be resolved in one
/// deterministic place.
/// </summary>
public sealed class CulturalVictoryQualification {
	public Player Player { get; init; }
	public CulturalVictoryPath Path { get; init; }
	public City City { get; init; }
	public int Culture { get; init; }
	public int Threshold { get; init; }
}

public static class CulturalVictory {
	public static int TotalCulture(Player player) {
		if (player == null) return 0;
		return player.cities
			.Where(city => city != null && city.residents.Count > 0)
			.Sum(city => city.GetCulture());
	}

	public static CulturalVictoryQualification Evaluate(Player player, Rules rules) {
		if (player == null || rules == null || player.isBarbarians || player.defeated || !rules.AllowCulturalVictory) {
			return null;
		}

		if (rules.OneCityCultureWin > 0) {
			City qualifyingCity = player.cities.FirstOrDefault(city =>
				city != null
				&& city.residents.Count > 0
				&& city.GetCulture() >= rules.OneCityCultureWin
			);
			if (qualifyingCity != null) {
				return new CulturalVictoryQualification {
					Player = player,
					Path = CulturalVictoryPath.OneCity,
					City = qualifyingCity,
					Culture = qualifyingCity.GetCulture(),
					Threshold = rules.OneCityCultureWin,
				};
			}
		}

		int totalCulture = TotalCulture(player);
		if (rules.AllCitiesCultureWin > 0 && totalCulture >= rules.AllCitiesCultureWin) {
			return new CulturalVictoryQualification {
				Player = player,
				Path = CulturalVictoryPath.Civilization,
				Culture = totalCulture,
				Threshold = rules.AllCitiesCultureWin,
			};
		}

		return null;
	}

	public static IReadOnlyList<CulturalVictoryQualification> EvaluateAll(GameData gameData) {
		if (gameData?.rules == null) return [];

		List<CulturalVictoryQualification> result = [];
		foreach (Player player in gameData.players) {
			CulturalVictoryQualification qualification = Evaluate(player, gameData.rules);
			if (qualification != null) result.Add(qualification);
		}
		return result;
	}
}
