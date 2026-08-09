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
	private static (Player player, Tile source, Tile target, TerrainImprovement road, TerrainImprovement railroad) MakeRoute() {
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

		return (player, source, target, road, railroad);
	}

	[Fact]
	public void RoadMovementCostsOneThirdOfAMovementPoint() {
		var (player, source, target, road, _) = MakeRoute();
		source.overlays.Add(road);
		target.overlays.Add(road);

		float cost = TilePath.GetMovementCost(player, source, TileDirection.EAST, target);

		Assert.Equal(1.0f / 3.0f, cost, precision: 6);
	}

	[Fact]
	public void RailroadMovementCostsZeroMovementPoints() {
		var (player, source, target, _, railroad) = MakeRoute();
		source.overlays.Add(railroad);
		target.overlays.Add(railroad);

		float cost = TilePath.GetMovementCost(player, source, TileDirection.EAST, target);

		Assert.Equal(0.0f, cost, precision: 6);
	}

	[Fact]
	public void RoadDoesNotOverrideTerrainUnlessRouteIsContinuous() {
		var (player, source, target, road, _) = MakeRoute();
		source.overlays.Add(road);

		float cost = TilePath.GetMovementCost(player, source, TileDirection.EAST, target);

		Assert.Equal(3.0f, cost, precision: 6);
	}
}
