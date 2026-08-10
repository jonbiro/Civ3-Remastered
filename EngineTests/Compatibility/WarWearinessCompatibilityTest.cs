using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using C7Engine;
using C7GameData;
using C7GameData.Save;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Public-CI behavioral contracts for Civilization III war weariness. The
/// hidden point values are reverse-engineered compatibility contracts; the
/// tests use synthetic game state and require no original assets.
/// </summary>
public class WarWearinessCompatibilityTest {
	private static (
		C7GameData.GameData gameData,
		Player player,
		Player opponent,
		City city,
		CitizenType laborer
	) MakeState(
		int population = 8,
		Government.WarWearinessLevel mode = Government.WarWearinessLevel.Low
	) {
		C7GameData.GameData gameData = new(customSeed: 12345) {
			turn = 0,
			gameDifficulty = new Difficulty {
				NumberOfCitizensBornContent = population,
				PercentageOfOptimalCities = 100,
			},
			rules = new Rules {
				MaximumLevel1CitySize = 6,
				MaximumLevel2CitySize = 12,
				MaxRankOfWorkableTiles = 2,
			},
		};
		EngineStorage.InitializeGameDataForTests(gameData);

		ID.Factory ids = new();
		Government government = new() {
			id = ids.CreateID("government"),
			name = "Representative Government",
			warWeariness = mode,
		};
		Player player = new() {
			id = ids.CreateID("player"),
			civilization = new Civilization("Player Civilization"),
			government = government,
			rules = gameData.rules,
		};
		Player opponent = new() {
			id = ids.CreateID("player"),
			civilization = new Civilization("Opponent Civilization"),
			government = new Government {
				id = ids.CreateID("government"),
				name = "Opponent Government",
			},
			rules = gameData.rules,
		};

		// Empty deal lists represent an active war.
		player.playerRelationships[opponent.id] = new PlayerRelationship();
		opponent.playerRelationships[player.id] = new PlayerRelationship();

		TerrainType terrain = new() {
			Key = "plains",
			baseFoodProduction = 0,
			baseShieldProduction = 0,
			baseCommerceProduction = 0,
			movementCost = 1,
		};
		Tile tile = new(ids.CreateID("tile")) {
			baseTerrainType = terrain,
			overlayTerrainType = terrain,
			Resource = Resource.NONE,
		};
		City city = new(tile, player, "Reference City", ids.CreateID("city"));
		tile.cityAtTile = city;
		tile.owningCity = city;
		player.cities.Add(city);
		gameData.cities.Add(city);
		gameData.players.Add(player);
		gameData.players.Add(opponent);
		gameData.governments.Add(government);
		gameData.governments.Add(opponent.government);

		CitizenType laborer = new() { IsDefaultCitizen = true };
		gameData.citizenTypes.Add(laborer);
		for (int i = 0; i < population; ++i) {
			city.residents.Add(new CityResident {
				city = city,
				nationality = player.civilization,
				citizenType = laborer,
				tileWorked = Tile.NONE,
			});
		}

		return (gameData, player, opponent, city, laborer);
	}

	[Theory]
	[InlineData(-30, 0)]
	[InlineData(0, 0)]
	[InlineData(30, 0)]
	[InlineData(31, 1)]
	[InlineData(60, 1)]
	[InlineData(61, 2)]
	[InlineData(90, 2)]
	[InlineData(91, 3)]
	[InlineData(120, 3)]
	[InlineData(121, 4)]
	public void PointThresholdsMatchReverseEngineeredConquestsBands(int points, int level) {
		Assert.Equal(level, WarWearinessRules.LevelForPoints(points));
	}

	[Theory]
	[InlineData(121, 113)]
	[InlineData(31, 29)]
	[InlineData(20, 19)]
	[InlineData(1, 0)]
	[InlineData(-30, -28)]
	[InlineData(-1, 0)]
	public void PeaceDecayMovesSignedHistoryTowardZero(int points, int expected) {
		Assert.Equal(expected, WarWearinessRules.DecayTowardZeroAtPeace(points));
	}

	[Fact]
	public void DirectDeclarationGivesDefenderWarHappiness() {
		var (gameData, aggressor, defender, _, _) = MakeState();
		MultiTurnDeal peace = new(DealType.DiplomaticAgreement, DealSubType.Peace, DealDetails.Exchange);
		aggressor.playerRelationships[defender.id].multiTurnDeals.Add(peace);
		defender.playerRelationships[aggressor.id].multiTurnDeals.Add(peace);

		PlayerRelationship.DeclareWar(aggressor, defender, sneakAttack: false, refuseContactUntilTurn: 5);

		Assert.True(PlayerRelationship.AtWar(aggressor, defender));
		Assert.Equal(0, aggressor.playerRelationships[defender.id].warWearinessPoints);
		Assert.Equal(-30, defender.playerRelationships[aggressor.id].warWearinessPoints);
	}

	[Fact]
	public void LowWarWearinessLevelOneMakesTwentyFivePercentUnhappy() {
		var (gameData, player, opponent, city, _) = MakeState(population: 8);
		player.playerRelationships[opponent.id].warWearinessPoints = 31;

		Assert.Equal((0, 2), player.GetWarWearinessMoodEffects(city));

		city.RecalculateCitizenMoods(gameData);
		Assert.Equal(2, city.residents.Count(resident => resident.mood == CityResident.Mood.Unhappy));
	}

	[Fact]
	public void EachOpponentContributionRoundsDownBeforeTotalsAreAdded() {
		var (_, player, opponent, city, _) = MakeState(population: 3);
		Player secondOpponent = new() {
			id = ID.None("second-opponent"),
			civilization = new Civilization("Second Opponent"),
			government = new Government { id = ID.None("second-government"), name = "Second Government" },
			rules = player.rules,
		};
		player.playerRelationships[opponent.id].warWearinessPoints = 31;
		player.playerRelationships[secondOpponent.id] = new PlayerRelationship { warWearinessPoints = 31 };

		Assert.Equal((0, 0), player.GetWarWearinessMoodEffects(city));
	}

	[Fact]
	public void WarHappinessAlsoRoundsPerOpponent() {
		var (_, player, opponent, city, _) = MakeState(population: 7);
		Player secondOpponent = new() {
			id = ID.None("second-opponent"),
			civilization = new Civilization("Second Opponent"),
			government = new Government { id = ID.None("second-government"), name = "Second Government" },
			rules = player.rules,
		};
		player.playerRelationships[opponent.id].warWearinessPoints = -30;
		player.playerRelationships[secondOpponent.id] = new PlayerRelationship { warWearinessPoints = -30 };

		Assert.Equal((2, 0), player.GetWarWearinessMoodEffects(city));
	}

	[Fact]
	public void PoliceStationReductionIsAppliedOnceAfterOpponentTotals() {
		var (gameData, player, opponent, city, _) = MakeState(population: 8);
		Player secondOpponent = new() {
			id = ID.None("second-opponent"),
			civilization = new Civilization("Second Opponent"),
			government = new Government { id = ID.None("second-government"), name = "Second Government" },
			rules = player.rules,
		};
		player.playerRelationships[opponent.id].warWearinessPoints = 31;
		player.playerRelationships[secondOpponent.id] = new PlayerRelationship { warWearinessPoints = 31 };
		Building policeStation = new(new SaveBuilding {
			name = "Police Station",
			flags = new HashSet<SaveBuilding.Flag> { SaveBuilding.Flag.ReducesWarWeariness },
		}, gameData);
		city.AddBuilding(policeStation);

		// Two independently rounded 25% penalties produce four unhappy
		// citizens. The Police Station then removes 25% of the city's eight
		// laborers once, leaving two.
		Assert.Equal((0, 2), player.GetWarWearinessMoodEffects(city));
	}

	[Fact]
	public void PoliceStationAndUniversalSuffrageReduceUnhappiness() {
		var (gameData, player, opponent, city, _) = MakeState(population: 8);
		player.playerRelationships[opponent.id].warWearinessPoints = 61;
		Assert.Equal((0, 4), player.GetWarWearinessMoodEffects(city));

		Building policeStation = new(new SaveBuilding {
			name = "Police Station",
			flags = new HashSet<SaveBuilding.Flag> { SaveBuilding.Flag.ReducesWarWeariness },
		}, gameData);
		city.AddBuilding(policeStation);
		Assert.Equal((0, 2), player.GetWarWearinessMoodEffects(city));

		Building universalSuffrage = new(new SaveBuilding {
			name = "Universal Suffrage",
			greatWonderProperties = new SaveBuilding.GreatWonderProperties(),
			flags = new HashSet<SaveBuilding.Flag> { SaveBuilding.Flag.ReducesWarWearinessGlobally },
		}, gameData);
		city.AddBuilding(universalSuffrage);
		Assert.Equal((0, 1), player.GetWarWearinessMoodEffects(city));
	}

	[Fact]
	public void HostileTerritoryExposureAndWartimeRecoveryUpdateOncePerTurn() {
		var (gameData, player, opponent, _, _) = MakeState();
		Tile hostileTile = new(ID.None("hostile-tile")) {
			baseTerrainType = TerrainType.NONE,
			overlayTerrainType = TerrainType.NONE,
			Resource = Resource.NONE,
		};
		City hostileCity = new(hostileTile, opponent, "Hostile City", ID.None("hostile-city"));
		hostileTile.cityAtTile = hostileCity;
		hostileTile.owningCity = hostileCity;
		UnitPrototype prototype = new() { name = "Infantry", attack = 1, defense = 1, movement = 1 };
		prototype.categories.Add("Land");
		MapUnit intruder = prototype.GetInstance(ID.None("intruder"), prototype, player, location: hostileTile);
		player.units.Add(intruder);

		player.UpdateWarWearinessForTurn(gameData);
		Assert.Equal(1, player.playerRelationships[opponent.id].warWearinessPoints);

		player.units.Clear();
		player.playerRelationships[opponent.id].warWearinessPoints = 31;
		player.UpdateWarWearinessForTurn(gameData);
		Assert.Equal(30, player.playerRelationships[opponent.id].warWearinessPoints);
	}

	[Fact]
	public void HighWarWearinessLevelThreeCollapsesGovernmentIntoAnarchy() {
		var (gameData, player, opponent, _, _) = MakeState(
			population: 8,
			mode: Government.WarWearinessLevel.High
		);
		player.civilization.traits.Add(Civilization.Trait.Religious);
		player.playerRelationships[opponent.id].warWearinessPoints = 91;
		Government anarchy = new() {
			id = ID.None("anarchy"),
			name = "Anarchy",
			transitionType = true,
		};
		gameData.governments.Add(anarchy);

		Assert.True(player.MaybeCollapseGovernmentFromWarWeariness(gameData));
		Assert.Same(anarchy, player.government);
		Assert.Equal(2, player.inAnarchyUntilTurn);
	}

	[Fact]
	public void WarWearinessSurvivesNativeSaveConversion() {
		var (gameData, player, opponent, _, _) = MakeState();
		player.playerRelationships[opponent.id].warWearinessPoints = 77;

		SavePlayer saved = new(player);
		JsonSerializerOptions options = new() {
			IncludeFields = true,
			Converters = { new IDJsonConverter() },
		};
		string json = JsonSerializer.Serialize(saved, options);
		SavePlayer serializedRoundTrip = JsonSerializer.Deserialize<SavePlayer>(json, options);
		Assert.NotNull(serializedRoundTrip);

		Player restored = serializedRoundTrip.ToPlayer(
			new GameMap(),
			new List<Civilization> { player.civilization },
			new List<Government> { player.government },
			new List<Tech>(),
			gameData.rules,
			new HashSet<Alliance>()
		);

		Assert.Equal(77, restored.playerRelationships[opponent.id].warWearinessPoints);
	}
}
