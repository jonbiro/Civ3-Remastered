using System.Linq;
using C7Engine;
using C7GameData;
using C7GameData.Save;
using Xunit;
using static C7GameData.Tile.TileOverlays;

namespace EngineTests.Compatibility;

/// <summary>
/// Direct behavior checks for the stock Civ III fortress and barricade
/// defensive modifiers documented by the original Civilopedia.
/// </summary>
public class FortificationCompatibilityTest {
	private static MapUnit MakeDefender(string improvementKey) {
		C7GameData.GameData gameData = new() {
			gameDifficulty = new Difficulty(),
		};
		EngineStorage.InitializeGameDataForTests(gameData);

		TerrainType terrain = new() {
			Key = "plains",
			movementCost = 1,
			defenseBonus = new StrengthBonus("Terrain", 0),
		};
		SaveTerrainImprovement saveImprovement = SaveTerrainImprovement.Civ3Improvements()
			.Single(improvement => improvement.key == improvementKey);
		TerrainImprovement improvement = new(
			saveImprovement,
			rulesEngine: null,
			resolveTerrainType: _ => terrain
		);
		Tile tile = new(ID.None("tile")) {
			baseTerrainType = terrain,
			overlayTerrainType = terrain,
		};
		tile.overlays.Add(improvement);

		Player player = new() {
			civilization = new Civilization("Reference Civilization"),
			government = new Government(),
		};
		UnitPrototype prototype = new() {
			name = "Reference Defender",
			defense = 2,
			movement = 1,
		};
		return prototype.GetInstance(ID.None("unit"), prototype, player, location: tile);
	}

	[Fact]
	public void FortressAddsFiftyPercentDefense() {
		MapUnit defender = MakeDefender(FORTRESS);

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, attackDirection: null);

		Assert.Equal(3.0, strength, precision: 6);
	}

	[Fact]
	public void BarricadeAddsOneHundredPercentDefense() {
		MapUnit defender = MakeDefender(BARRICADE);

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, attackDirection: null);

		Assert.Equal(4.0, strength, precision: 6);
	}
}
