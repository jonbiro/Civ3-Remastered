using System.Collections.Generic;
using C7Engine;
using C7GameData;
using C7GameData.Save;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Public-CI behavior contracts for Civilization III Golden Ages. The tests
/// use synthetic rules and do not require original game assets.
/// </summary>
public class GoldenAgeCompatibilityTest {
	private static (C7GameData.GameData gameData, Player player) MakePlayer(int duration = 20) {
		C7GameData.GameData gameData = new(customSeed: 1234) {
			gameDifficulty = new Difficulty(),
			rules = new Rules { GoldenAgeDuration = duration },
		};
		EngineStorage.InitializeGameDataForTests(gameData);

		Government government = new() {
			id = ID.None("government"),
			name = "Reference Government",
		};
		Civilization civilization = new("Reference Civilization");
		Player player = new() {
			id = ID.None("player"),
			civilization = civilization,
			government = government,
			rules = gameData.rules,
		};
		gameData.players.Add(player);
		return (gameData, player);
	}

	[Fact]
	public void GoldenAgeAddsOneProductionAndCommerceButNoFood() {
		var (gameData, player) = MakePlayer();
		Assert.True(player.StartGoldenAge(gameData));

		TerrainType producingTerrain = new() {
			Key = "plains",
			baseFoodProduction = 1,
			baseShieldProduction = 1,
			baseCommerceProduction = 1,
			movementCost = 1,
		};
		Tile tile = new(ID.None("tile")) {
			baseTerrainType = producingTerrain,
			overlayTerrainType = producingTerrain,
		};

		Assert.Equal(1, tile.FoodYield(player).yield);
		Assert.Equal(2, tile.ProductionYield(player).yield);
		Assert.Equal(2, tile.CommerceYield(player).yield);
	}

	[Fact]
	public void GoldenAgeDoesNotCreateYieldOnZeroYieldTiles() {
		var (gameData, player) = MakePlayer();
		player.StartGoldenAge(gameData);

		TerrainType barrenTerrain = new() {
			Key = "barren",
			baseFoodProduction = 0,
			baseShieldProduction = 0,
			baseCommerceProduction = 0,
			movementCost = 1,
		};
		Tile tile = new(ID.None("tile")) {
			baseTerrainType = barrenTerrain,
			overlayTerrainType = barrenTerrain,
		};

		Assert.Equal(0, tile.ProductionYield(player).yield);
		Assert.Equal(0, tile.CommerceYield(player).yield);
	}

	[Fact]
	public void GoldenAgeLastsExactlyTheConfiguredNumberOfYieldCyclesAndCannotRepeat() {
		var (gameData, player) = MakePlayer(duration: 20);
		Assert.True(player.StartGoldenAge(gameData));

		for (int completedCycles = 0; completedCycles < 20; ++completedCycles) {
			Assert.True(player.IsGoldenAgeActive);
			Assert.Equal(20 - completedCycles, player.goldenAgeTurnsRemaining);
			player.AdvanceGoldenAgeTurn();
		}

		Assert.False(player.IsGoldenAgeActive);
		Assert.Equal(0, player.goldenAgeTurnsRemaining);
		Assert.False(player.StartGoldenAge(gameData));
	}

	[Fact]
	public void GoldenAgeStateSurvivesNativeSaveConversion() {
		var (gameData, player) = MakePlayer();
		player.StartGoldenAge(gameData);
		player.AdvanceGoldenAgeTurn();
		player.AdvanceGoldenAgeTurn();

		SavePlayer saved = new(player);
		Player restored = saved.ToPlayer(
			new GameMap(),
			new List<Civilization> { player.civilization },
			new List<Government> { player.government },
			new List<Tech>(),
			gameData.rules,
			new HashSet<Alliance>()
		);

		Assert.True(restored.hasTriggeredGoldenAge);
		Assert.True(restored.IsGoldenAgeActive);
		Assert.Equal(18, restored.goldenAgeTurnsRemaining);
	}

	[Fact]
	public void FlaggedUnitVictoryAgainstAnotherCivilizationStartsGoldenAge() {
		var (gameData, player) = MakePlayer();
		Player rival = new() {
			id = ID.None("rival"),
			civilization = new Civilization("Rival"),
			government = new Government(),
		};
		UnitPrototype uniqueUnit = new() {
			name = "Reference Unique Unit",
			startsGoldenAge = true,
		};
		UnitPrototype rivalUnit = new() { name = "Rival Unit" };
		MapUnit victor = uniqueUnit.GetInstance(ID.None("victor"), uniqueUnit, player, location: Tile.NONE);
		MapUnit defeated = rivalUnit.GetInstance(ID.None("defeated"), rivalUnit, rival, location: Tile.NONE);

		Assert.True(player.MaybeStartGoldenAgeFromUnitVictory(victor, defeated, gameData));
		Assert.True(player.IsGoldenAgeActive);
	}

	[Fact]
	public void BarbarianVictoryCannotTriggerGoldenAge() {
		var (gameData, player) = MakePlayer();
		Player barbarians = new() {
			id = ID.None("barbarians"),
			civilization = new Civilization("Barbarians") { isBarbarian = true },
			government = new Government(),
		};
		UnitPrototype uniqueUnit = new() {
			name = "Reference Unique Unit",
			startsGoldenAge = true,
		};
		UnitPrototype barbarianUnit = new() { name = "Barbarian Unit" };
		MapUnit victor = uniqueUnit.GetInstance(ID.None("victor"), uniqueUnit, player, location: Tile.NONE);
		MapUnit defeated = barbarianUnit.GetInstance(ID.None("defeated"), barbarianUnit, barbarians, location: Tile.NONE);

		Assert.False(player.MaybeStartGoldenAgeFromUnitVictory(victor, defeated, gameData));
		Assert.False(player.hasTriggeredGoldenAge);
	}

	[Fact]
	public void GreatWondersCanCollectivelyCoverCivilizationTraits() {
		var (gameData, player) = MakePlayer();
		player.civilization.traits.Add(Civilization.Trait.Industrious);
		player.civilization.traits.Add(Civilization.Trait.Religious);

		City city = new(Tile.NONE, player, "Wonder City", ID.None("city"));
		player.cities.Add(city);

		Building industriousWonder = MakeWonder(
			gameData,
			"Industrious Wonder",
			Civilization.Trait.Industrious
		);
		Building religiousWonder = MakeWonder(
			gameData,
			"Religious Wonder",
			Civilization.Trait.Religious
		);

		city.AddBuilding(industriousWonder);
		Assert.False(player.MaybeStartGoldenAgeFromWonders(gameData));

		city.AddBuilding(religiousWonder);
		Assert.True(player.MaybeStartGoldenAgeFromWonders(gameData));
		Assert.True(player.IsGoldenAgeActive);
	}

	private static Building MakeWonder(
		C7GameData.GameData gameData,
		string name,
		Civilization.Trait trait
	) {
		SaveBuilding save = new() {
			name = name,
			greatWonderProperties = new SaveBuilding.GreatWonderProperties(),
			traits = new HashSet<Civilization.Trait> { trait },
		};
		return new Building(save, gameData);
	}
}
