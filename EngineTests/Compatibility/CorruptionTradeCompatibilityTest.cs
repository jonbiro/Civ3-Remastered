using C7Engine;
using C7GameData;
using Xunit;
using static C7GameData.Tile.TileOverlays;

namespace EngineTests.Compatibility;

public class CorruptionTradeCompatibilityTest {
	[Fact]
	public void CapitalRoadConnectionReducesDistanceCorruption() {
		GameData gameData = new() {
			gameDifficulty = new Difficulty { PercentageOfOptimalCities = 100 },
			rules = new Rules { MaximumLevel1CitySize = 6, MaximumLevel2CitySize = 12 },
		};
		gameData.map.numTilesWide = 20;
		gameData.map.numTilesTall = 20;
		gameData.map.optimalNumberOfCities = 20;
		EngineStorage.InitializeGameDataForTests(gameData);
		TerrainImprovement road = new(ROAD, TerrainImprovement.Layer.Roads, movementCost: 1.0f / 3.0f);
		gameData.terrainImprovements.Add(road);

		ID.Factory ids = new();
		Tile capitalTile = MakeTile(ids, gameData, 0, 0);
		Tile roadTile = MakeTile(ids, gameData, 1, 1);
		Tile cityTile = MakeTile(ids, gameData, 2, 2);
		capitalTile.neighbors[TileDirection.SOUTHEAST] = roadTile;
		roadTile.neighbors[TileDirection.NORTHWEST] = capitalTile;
		roadTile.neighbors[TileDirection.SOUTHEAST] = cityTile;
		cityTile.neighbors[TileDirection.NORTHWEST] = roadTile;

		Player player = new() {
			id = ids.CreateID("player"),
			isHuman = true,
			civilization = new Civilization("Reference Civilization"),
			government = new Government { corruptionType = Government.CorruptionType.Problematic },
			rules = gameData.rules,
		};
		City capital = new(capitalTile, player, "Capital", ids.CreateID("city")) { capital = true };
		City city = new(cityTile, player, "Connected City", ids.CreateID("city"));
		capitalTile.cityAtTile = capital;
		cityTile.cityAtTile = city;
		capitalTile.overlays.Add(road);
		roadTile.overlays.Add(road);
		cityTile.overlays.Add(road);
		player.cities.Add(capital);
		player.cities.Add(city);
		gameData.players.Add(player);

		player.DoCorruptionCalculations(gameData);
		float connectedCorruption = city.corruption;

		roadTile.overlays.Remove(road);
		player.DoCorruptionCalculations(gameData);
		float disconnectedCorruption = city.corruption;

		Assert.True(connectedCorruption < disconnectedCorruption);
	}

	private static Tile MakeTile(ID.Factory ids, GameData gameData, int x, int y) {
		TerrainType terrain = new() { Key = "plains", movementCost = 1 };
		return new Tile(ids.CreateID("tile")) {
			XCoordinate = x,
			YCoordinate = y,
			map = gameData.map,
			baseTerrainType = terrain,
			overlayTerrainType = terrain,
		};
	}
}
