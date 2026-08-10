using C7Engine;
using C7GameData;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Synthetic engine-level checks for defensive modifiers documented by the
/// original Civilization III: Conquests rules. These do not require original
/// game assets and therefore run in public CI.
/// </summary>
public class CombatDefenseCompatibilityTest {
	private static MapUnit MakeDefender(double terrainBonus, bool fortified) {
		EngineStorage.InitializeGameDataForTests(new C7GameData.GameData {
			fortificationBonus = new StrengthBonus("Fortified", 0.25),
			riverCrossingBonus = new StrengthBonus("Behind river", 0.25),
			cityLevel1DefenseBonus = new StrengthBonus("Town", 0.50),
			cityLevel2DefenseBonus = new StrengthBonus("City", 0.50),
			cityLevel3DefenseBonus = new StrengthBonus("Metropolis", 1.00),
			rules = new Rules { MaximumLevel1CitySize = 6, MaximumLevel2CitySize = 12 },
			gameDifficulty = new Difficulty(),
		});

		Tile tile = new(ID.None("tile")) {
			overlayTerrainType = new TerrainType {
				defenseBonus = new StrengthBonus("Reference terrain", terrainBonus),
			},
		};

		Civilization civilization = new("Reference Civilization");
		Player player = new() { civilization = civilization };
		UnitPrototype prototype = new() {
			name = "Reference Defender",
			defense = 2,
			movement = 1,
		};

		MapUnit defender = prototype.GetInstance(
			ID.None("unit"),
			prototype,
			player,
			location: tile
		);
		defender.isFortified = fortified;
		return defender;
	}

	[Fact]
	public void HillsApplyFiftyPercentDefenseBonus() {
		MapUnit defender = MakeDefender(terrainBonus: 0.50, fortified: false);

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, attackDirection: null);

		Assert.Equal(3.0, strength, precision: 6);
	}

	[Fact]
	public void FortificationAppliesTwentyFivePercentDefenseBonus() {
		MapUnit defender = MakeDefender(terrainBonus: 0.0, fortified: true);

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, attackDirection: null);

		Assert.Equal(2.5, strength, precision: 6);
	}

	[Fact]
	public void MountainsApplyOneHundredPercentDefenseBonus() {
		MapUnit defender = MakeDefender(terrainBonus: 1.00, fortified: false);

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, attackDirection: null);

		Assert.Equal(4.0, strength, precision: 6);
	}

	[Fact]
	public void RiverCrossingAppliesTwentyFivePercentDefenseBonus() {
		MapUnit defender = MakeDefender(terrainBonus: 0.0, fortified: false);
		defender.location.riverWest = true;

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, TileDirection.EAST);

		Assert.Equal(2.5, strength, precision: 6);
	}

	[Theory]
	[InlineData(6, 3.0)]
	[InlineData(7, 3.0)]
	[InlineData(13, 4.0)]
	public void SettlementSizeAppliesConquestsDefenseBonus(int population, double expectedStrength) {
		MapUnit defender = MakeDefender(terrainBonus: 0.0, fortified: false);
		City city = new(defender.location, defender.owner, "Reference City", ID.None("city"));
		for (int i = 0; i < population; ++i) {
			city.residents.Add(new CityResident());
		}
		defender.location.cityAtTile = city;

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, attackDirection: null);

		Assert.Equal(expectedStrength, strength, precision: 6);
	}

	[Fact]
	public void ResistingCitizenSuppressesSettlementSizeDefenseBonus() {
		MapUnit defender = MakeDefender(terrainBonus: 0.0, fortified: false);
		City city = new(defender.location, defender.owner, "Captured City", ID.None("city"));
		for (int i = 0; i < 7; ++i) {
			city.residents.Add(new CityResident { isResisting = i == 0 });
		}
		defender.location.cityAtTile = city;

		double strength = defender.StrengthVersus(MapUnit.NONE, CombatRole.Defense, attackDirection: null);

		Assert.Equal(2.0, strength, precision: 6);
	}
}
