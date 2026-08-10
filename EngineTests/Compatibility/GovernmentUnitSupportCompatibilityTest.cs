using C7Engine;
using C7GameData;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Engine-level behavior checks for the Conquests town/city/metropolis unit
/// support model. The stock Republic values (1/3/4) come from the original
/// Conquests rules documentation; the tests exercise the engine algorithm with
/// synthetic data so they run in public CI without original game files.
/// </summary>
public class GovernmentUnitSupportCompatibilityTest {
	private static Player MakeRepublicPlayer() {
		EngineStorage.InitializeGameDataForTests(new C7GameData.GameData {
			gameDifficulty = new Difficulty(),
		});

		Civilization civilization = new() { name = "Test Civilization" };
		return new Player {
			isHuman = true,
			civilization = civilization,
			government = new Government {
				name = "Republic",
				freeUnitsPerTown = 1,
				freeUnitsPerCity = 3,
				freeUnitsPerMetropolis = 4,
				unitCost = 1,
			},
			rules = new Rules {
				MaximumLevel1CitySize = 6,
				MaximumLevel2CitySize = 12,
			},
		};
	}

	private static City AddCity(Player player, string name, int population) {
		City city = new(Tile.NONE, player, name, ID.None("city"));
		for (int i = 0; i < population; ++i) {
			city.residents.Add(new CityResident());
		}
		player.cities.Add(city);
		return city;
	}

	private static void AddUnits(Player player, int count) {
		for (int i = 0; i < count; ++i) {
			player.units.Add(new MapUnit(ID.None("unit")) {
				owner = player,
				nationality = player.civilization,
			});
		}
	}

	[Theory]
	[InlineData(6, 1)]
	[InlineData(7, 3)]
	[InlineData(12, 3)]
	[InlineData(13, 4)]
	public void RepublicSupportUsesCiv3SettlementSizeBoundaries(int population, int expectedAllowedUnits) {
		Player player = MakeRepublicPlayer();
		AddCity(player, "Reference City", population);
		AddUnits(player, 10);

		(int totalUnits, int allowedUnits, int supportCost) = player.TotalUnitsAllowedUnitsAndSupportCostRaw();

		Assert.Equal(10, totalUnits);
		Assert.Equal(expectedAllowedUnits, allowedUnits);
		Assert.Equal(10 - expectedAllowedUnits, supportCost);
	}

	[Fact]
	public void RepublicSupportAddsAllowancesAcrossTownCityAndMetropolis() {
		Player player = MakeRepublicPlayer();
		AddCity(player, "Town", 6);
		AddCity(player, "City", 7);
		AddCity(player, "Metropolis", 13);
		AddUnits(player, 10);

		(int totalUnits, int allowedUnits, int supportCost) = player.TotalUnitsAllowedUnitsAndSupportCostRaw();

		Assert.Equal(10, totalUnits);
		Assert.Equal(8, allowedUnits);
		Assert.Equal(2, supportCost);
	}
}
