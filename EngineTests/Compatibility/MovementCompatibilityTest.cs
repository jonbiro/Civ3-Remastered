using System.Collections.Generic;
using C7Engine;
using C7GameData;
using Xunit;
using static C7GameData.Tile.TileOverlays;

namespace EngineTests.Compatibility;

/// <summary>
/// Engine-level movement contracts documented by the original Civilization III:
/// Conquests rules. These tests use synthetic terrain and improvements so they
/// run in public CI without original game files.
/// </summary>
public class MovementCompatibilityTest {
	private static (Player player, Tile source, Tile target) MakeRoute() {
		TerrainImprovement road = new(ROAD, TerrainImprovement.Layer.Roads, movementCost: 1.0f / 3.0f);
		TerrainImprovement railroad = new(RAILROAD, TerrainImprovement.Layer.Roads, movementCost: 0.0f);
		EngineStorage.InitializeGameDataForTests(new C7GameData.GameData {
			gameDifficulty = new Difficulty(),
			terrainImprovements = new List<TerrainImprovement> { road, railroad },
		});

		TerrainType slowTerrain = new() {
			Key = "reference-slow-terrain",
			DisplayName = "Reference Slow Terrain",
			movementCost = 3,
		};

		Tile source = new(ID.None("source")) {
			baseTerrainType = slowTerrain,
			overlayTerrainType = slowTerrain,
		};
		Tile target = new(ID.None("target")) {
			baseTerrainType = slowTerrain,
			overlayTerrainType = slowTerrain,
		};

		Player player = new() {
			civilization = new Civilization("Reference Civilization"),
		};

		return (player, source, target);
	}

	[Fact]
	public void RoadMovementCostsOneThirdOfAMovementPoint() {
		(Player player, Tile source, Tile target) = MakeRoute();
		source.overlays.Add(ROAD);
		target.overlays.Add(ROAD);

		float cost = TilePath.GetMovementCost(player, source, TileDirection.EAST, target);

		Assert.Equal(1.0f / 3.0f, cost, precision: 6);
	}

	[Fact]
	public void RailroadMovementCostsZeroMovementPoints() {
		(Player player, Tile source, Tile target) = MakeRoute();
		source.overlays.Add(RAILROAD);
		target.overlays.Add(RAILROAD);

		float cost = TilePath.GetMovementCost(player, source, TileDirection.EAST, target);

		Assert.Equal(0.0f, cost, precision: 6);
	}

	[Fact]
	public void RoadDoesNotOverrideTerrainUnlessRouteIsContinuous() {
		(Player player, Tile source, Tile target) = MakeRoute();
		source.overlays.Add(ROAD);

		float cost = TilePath.GetMovementCost(player, source, TileDirection.EAST, target);

		Assert.Equal(3.0f, cost, precision: 6);
	}
}
