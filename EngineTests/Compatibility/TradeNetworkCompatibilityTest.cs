using C7Engine;
using C7Engine.Pathing;
using C7GameData;
using Xunit;
using static C7GameData.Tile.TileOverlays;

namespace EngineTests.Compatibility;

public class TradeNetworkCompatibilityTest {
	private static (C7GameData.GameData gameData, Player player, Player rival, City firstCity, City capital, Tile middle) MakeThreeTileRoad() {
		C7GameData.GameData gameData = new() {
			gameDifficulty = new Difficulty(),
			rules = new Rules { DefaultDealDuration = 20 },
		};
		EngineStorage.InitializeGameDataForTests(gameData);
		TerrainImprovement road = new(ROAD, TerrainImprovement.Layer.Roads, movementCost: 1.0f / 3.0f);
		gameData.terrainImprovements.Add(road);

		ID.Factory ids = new();
		Tile left = new(ids.CreateID("tile"));
		Tile middle = new(ids.CreateID("tile"));
		Tile right = new(ids.CreateID("tile"));
		left.neighbors[TileDirection.EAST] = middle;
		middle.neighbors[TileDirection.WEST] = left;
		middle.neighbors[TileDirection.EAST] = right;
		right.neighbors[TileDirection.WEST] = middle;

		Player player = new() { id = ids.CreateID("player"), civilization = new Civilization("Player") };
		Player rival = new() { id = ids.CreateID("player"), civilization = new Civilization("Rival") };
		City firstCity = new(left, player, "First Founded", ids.CreateID("city"));
		City capital = new(right, player, "Actual Capital", ids.CreateID("city")) { capital = true };
		left.cityAtTile = firstCity;
		right.cityAtTile = capital;
		left.overlays.Add(road);
		middle.overlays.Add(road);
		right.overlays.Add(road);
		player.cities.Add(firstCity);
		player.cities.Add(capital);
		gameData.players.Add(player);
		gameData.players.Add(rival);
		return (gameData, player, rival, firstCity, capital, middle);
	}

	[Fact]
	public void ConnectedToCapitalUsesCapitalFlagInsteadOfFirstCityPosition() {
		var (gameData, player, _, firstCity, capital, middle) = MakeThreeTileRoad();
		TerrainImprovement road = middle.overlays.ImprovementAtLayer(TerrainImprovement.Layer.Roads);
		middle.overlays.Remove(road);

		TradeNetwork network = gameData.GetTradeNetwork();

		Assert.False(network.ConnectedToCapital(player, firstCity));
		Assert.True(network.ConnectedToCapital(player, capital));
	}

	[Fact]
	public void DeclaringWarInvalidatesRouteThroughEnemyTerritory() {
		var (gameData, player, rival, firstCity, _, middle) = MakeThreeTileRoad();
		Tile rivalSeat = new(ID.None("rival-seat"));
		City foreignClaim = new(rivalSeat, rival, "Foreign Claim", ID.None("foreign-city"));
		middle.owningCity = foreignClaim;
		player.EnsureRelationshipExists(rival);

		Assert.True(gameData.GetTradeNetwork().ConnectedToCapital(player, firstCity));

		player.DeclareWarOn(rival, currentTurn: 0);

		Assert.False(gameData.GetTradeNetwork().ConnectedToCapital(player, firstCity));
	}
}
