using System.Collections.Generic;
using System.Linq;

namespace C7GameData;

public enum VictoryType {
	Cultural,
	Conquest,
	Domination,
	SpaceRace,
	Diplomatic,
	Wonder,
	VictoryPoints,
	Histograph,
}

public sealed class VictoryClaim {
	public VictoryType Type;
	public ID PlayerId;
	public ID CityId;
	public int CurrentValue;
	public int RequiredValue;
	public int SecondaryCurrentValue;
	public int SecondaryRequiredValue;
}

/// <summary>
/// Persistable outcome state. More than one claim may exist because original
/// simultaneous-condition precedence has not yet been established. A result
/// is resolved only when all claims point to the same civilization.
/// </summary>
public sealed class GameOutcome {
	public int Turn;
	public List<VictoryClaim> Claims = [];

	public bool HasClaims => Claims.Count > 0;
	public bool IsResolved => Claims.Count > 0 && Claims.Select(claim => claim.PlayerId).Distinct().Count() == 1;
	public ID WinnerId => IsResolved ? Claims[0].PlayerId : null;
}

public static class VictoryResolver {
	public static GameOutcome Evaluate(GameData gameData) {
		GameOutcome outcome = new() { Turn = gameData?.turn ?? 0 };
		if (gameData?.rules == null) return outcome;

		AddCulturalClaims(gameData, outcome);
		AddConquestClaims(gameData, outcome);
		AddDominationClaims(gameData, outcome);
		return outcome;
	}

	private static IEnumerable<Player> EligiblePlayers(GameData gameData) {
		return gameData.players.Where(player =>
			player != null
			&& player.isIncludedInGame
			&& !player.isBarbarians
			&& !player.defeated
		);
	}

	private static void AddCulturalClaims(GameData gameData, GameOutcome outcome) {
		foreach (CulturalVictoryQualification qualification in CulturalVictory.EvaluateAll(gameData)) {
			outcome.Claims.Add(new VictoryClaim {
				Type = VictoryType.Cultural,
				PlayerId = qualification.Player.id,
				CityId = qualification.City?.id,
				CurrentValue = qualification.Culture,
				RequiredValue = qualification.Threshold,
			});
		}
	}

	private static void AddConquestClaims(GameData gameData, GameOutcome outcome) {
		if (!gameData.rules.AllowConquestVictory) return;

		List<Player> survivors = EligiblePlayers(gameData).ToList();
		if (survivors.Count != 1) return;

		outcome.Claims.Add(new VictoryClaim {
			Type = VictoryType.Conquest,
			PlayerId = survivors[0].id,
			CurrentValue = 1,
			RequiredValue = 1,
		});
	}

	private static void AddDominationClaims(GameData gameData, GameOutcome outcome) {
		if (!gameData.rules.AllowDominationVictory) return;
		if (gameData.rules.DominationTerrainPercent <= 0 || gameData.rules.DominationPopulationPercent <= 0) return;

		List<Tile> landTiles = gameData.map.tiles
			.Where(tile => tile?.baseTerrainType != null && !tile.baseTerrainType.isWater())
			.ToList();
		if (landTiles.Count == 0) return;

		List<Player> players = EligiblePlayers(gameData).ToList();
		int worldPopulation = players.Sum(player => player.cities.Sum(city => city.residents.Count));
		if (worldPopulation == 0) return;

		foreach (Player player in players) {
			int ownedLand = landTiles.Count(tile => tile.OwningPlayer() == player);
			int population = player.cities.Sum(city => city.residents.Count);

			bool terrainQualified = (long)ownedLand * 100 >= (long)landTiles.Count * gameData.rules.DominationTerrainPercent;
			bool populationQualified = (long)population * 100 >= (long)worldPopulation * gameData.rules.DominationPopulationPercent;
			if (!terrainQualified || !populationQualified) continue;

			outcome.Claims.Add(new VictoryClaim {
				Type = VictoryType.Domination,
				PlayerId = player.id,
				CurrentValue = ownedLand,
				RequiredValue = gameData.rules.DominationTerrainPercent,
				SecondaryCurrentValue = population,
				SecondaryRequiredValue = gameData.rules.DominationPopulationPercent,
			});
		}
	}
}
