using C7Engine;
using C7Engine.Pathing;
using C7GameData;
using C7GameData.Save;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Synthetic tests for the harbor and airport trade links documented by the
/// original Civilization III: Conquests rules. Original assets are not needed.
/// </summary>
public class TradeNetworkConnectorCompatibilityTest {
	private static C7GameData.GameData MakeGameData() {
		C7GameData.GameData gameData = new() {
			gameDifficulty = new Difficulty(),
			rules = new Rules { DefaultDealDuration = 20 },
		};
		EngineStorage.InitializeGameDataForTests(gameData);
		return gameData;
	}

	private static TerrainType Terrain(string key) {
		return new TerrainType {
			Key = key,
			DisplayName = key,
			movementCost = 1,
			height = key is "coast" or "sea" or "ocean" ? -2 : 0,
		};
	}

	private static Tile TileAt(ID.Factory ids, string key) {
		TerrainType terrain = Terrain(key);
		return new Tile(ids.CreateID("tile")) {
			baseTerrainType = terrain,
			overlayTerrainType = terrain,
		};
	}

	private static void Link(Tile left, TileDirection leftToRight, Tile right) {
		left.neighbors[leftToRight] = right;
		right.neighbors[leftToRight.Reversed()] = left;
	}

	private static Building TradeBuilding(C7GameData.GameData gameData, string name, SaveBuilding.Flag flag) {
		SaveBuilding saveBuilding = new() { name = name };
		saveBuilding.flags.Add(flag);
		return new Building(saveBuilding, gameData);
	}

	private static (C7GameData.GameData gameData, Player player, City capital, City city, Tile firstWater, Tile lastWater) MakeTwoHarbors(string middleWater = null) {
		C7GameData.GameData gameData = MakeGameData();
		ID.Factory ids = new();

		Tile capitalTile = TileAt(ids, "plains");
		Tile firstWater = TileAt(ids, "coast");
		Tile lastWater = TileAt(ids, "coast");
		Tile cityTile = TileAt(ids, "plains");
		Link(capitalTile, TileDirection.EAST, firstWater);
		if (middleWater == null) {
			Link(firstWater, TileDirection.EAST, lastWater);
		} else {
			Tile middle = TileAt(ids, middleWater);
			Link(firstWater, TileDirection.EAST, middle);
			Link(middle, TileDirection.EAST, lastWater);
		}
		Link(lastWater, TileDirection.EAST, cityTile);

		Player player = new() {
			id = ids.CreateID("player"),
			isHuman = true,
			civilization = new Civilization("Reference Civilization"),
		};
		City capital = new(capitalTile, player, "Capital", ids.CreateID("city")) { capital = true };
		City city = new(cityTile, player, "Port", ids.CreateID("city"));
		capitalTile.cityAtTile = capital;
		cityTile.cityAtTile = city;
		player.cities.Add(capital);
		player.cities.Add(city);
		gameData.players.Add(player);

		Building harbor = TradeBuilding(gameData, "Harbor", SaveBuilding.Flag.AllowsWaterTrade);
		capital.AddBuilding(harbor);
		city.AddBuilding(harbor);

		player.tileKnowledge.AddTilesToKnown(firstWater, recomputeActiveTiles: false);
		player.tileKnowledge.AddTilesToKnown(lastWater, recomputeActiveTiles: false);
		if (middleWater != null) {
			Tile middle = firstWater.neighbors[TileDirection.EAST];
			player.tileKnowledge.AddTilesToKnown(middle, recomputeActiveTiles: false);
		}

		return (gameData, player, capital, city, firstWater, lastWater);
	}

	[Fact]
	public void HarborsConnectCitiesAcrossExploredCoast() {
		var (gameData, player, _, city, _, _) = MakeTwoHarbors();

		Assert.True(gameData.GetTradeNetwork().ConnectedToCapital(player, city));
	}

	[Fact]
	public void SeaTradeRequiresATechThatEnablesSeaTrade() {
		var (gameData, player, _, city, _, _) = MakeTwoHarbors(middleWater: "sea");
		Assert.False(gameData.GetTradeNetwork().ConnectedToCapital(player, city));

		Tech navigation = new() {
			id = ID.None("sea-trade-tech"),
			Name = "Reference Sea Trade",
			EnablesTradeOverSea = true,
		};
		gameData.techs.Add(navigation);
		player.knownTechs.Add(navigation.id);
		gameData.InvalidateCachedTradeNetwork();

		Assert.True(gameData.GetTradeNetwork().ConnectedToCapital(player, city));
	}

	[Fact]
	public void OceanTradeRequiresATechThatEnablesOceanTrade() {
		var (gameData, player, _, city, _, _) = MakeTwoHarbors(middleWater: "ocean");
		Assert.False(gameData.GetTradeNetwork().ConnectedToCapital(player, city));

		Tech magnetism = new() {
			id = ID.None("ocean-trade-tech"),
			Name = "Reference Ocean Trade",
			EnablesTradeOverOcean = true,
		};
		gameData.techs.Add(magnetism);
		player.knownTechs.Add(magnetism.id);
		gameData.InvalidateCachedTradeNetwork();

		Assert.True(gameData.GetTradeNetwork().ConnectedToCapital(player, city));
	}

	[Fact]
	public void EnemyNavalUnitBlockadesHarborRouteDuringWar() {
		var (gameData, player, _, city, firstWater, _) = MakeTwoHarbors();
		ID.Factory ids = new();
		Player rival = new() {
			id = ids.CreateID("rival"),
			civilization = new Civilization("Rival"),
		};
		gameData.players.Add(rival);
		player.EnsureRelationshipExists(rival);

		UnitPrototype ship = new() { name = "Blockader" };
		ship.categories.Add("Sea");
		MapUnit blockader = ship.GetInstance(ids.CreateID("unit"), ship, rival, location: firstWater);
		firstWater.unitsOnTile.Add(blockader);
		gameData.mapUnits.Add(blockader);
		rival.units.Add(blockader);

		Assert.True(gameData.GetTradeNetwork().ConnectedToCapital(player, city));

		player.DeclareWarOn(rival, currentTurn: 0);

		Assert.False(gameData.GetTradeNetwork().ConnectedToCapital(player, city));

		gameData.RemoveUnit(blockader);

		Assert.True(gameData.GetTradeNetwork().ConnectedToCapital(player, city));
	}

	[Fact]
	public void AirportsConnectOtherwiseDisconnectedCities() {
		C7GameData.GameData gameData = MakeGameData();
		ID.Factory ids = new();
		Player player = new() {
			id = ids.CreateID("player"),
			civilization = new Civilization("Reference Civilization"),
		};
		Tile capitalTile = TileAt(ids, "plains");
		Tile cityTile = TileAt(ids, "plains");
		City capital = new(capitalTile, player, "Capital", ids.CreateID("city")) { capital = true };
		City city = new(cityTile, player, "Air Hub", ids.CreateID("city"));
		capitalTile.cityAtTile = capital;
		cityTile.cityAtTile = city;
		player.cities.Add(capital);
		player.cities.Add(city);
		gameData.players.Add(player);

		Assert.False(gameData.GetTradeNetwork().ConnectedToCapital(player, city));

		Building airport = TradeBuilding(gameData, "Airport", SaveBuilding.Flag.AllowsAirTrade);
		capital.AddBuilding(airport);
		city.AddBuilding(airport);

		Assert.True(gameData.GetTradeNetwork().ConnectedToCapital(player, city));
	}
}
