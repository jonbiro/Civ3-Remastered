using System.Collections.Generic;
using System.Linq;
using C7Engine;
using C7GameData;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Public-CI behavioral contracts for Civilization III captured-city
/// resistance. The rule values are synthetic so no original assets are needed.
/// </summary>
public class ResistanceCompatibilityTest {
	private static (C7GameData.GameData gameData, Player conqueror, Player conquered, City city, CitizenType citizenType) MakeCapturedCity(
		int continuedResistanceChance = 0,
		int militaryLaw = 1
	) {
		C7GameData.GameData gameData = new(customSeed: 12345) {
			gameDifficulty = new Difficulty { MilitaryLaw = militaryLaw },
			rules = new Rules { MaxRankOfWorkableTiles = 2 },
			cultureRelationshipLevels = new List<CultureRelationshipLevel> {
				new() {
					name = "equal",
					cultureRatioPercentage = 100,
					cultureRatioNumerator = 1,
					cultureRatioDenominator = 1,
					initialResistanceChance = 0,
					continuedResistanceChance = continuedResistanceChance,
				},
			},
		};
		EngineStorage.InitializeGameDataForTests(gameData);

		ID.Factory ids = new();
		Player conqueror = new() {
			id = ids.CreateID("player"),
			civilization = new Civilization("Conqueror"),
			government = new Government { name = "Conqueror Government" },
			rules = gameData.rules,
		};
		Player conquered = new() {
			id = ids.CreateID("player"),
			civilization = new Civilization("Conquered"),
			government = new Government { name = "Conquered Government" },
			rules = gameData.rules,
		};

		// An empty relationship deal list represents war.
		conqueror.playerRelationships[conquered.id] = new PlayerRelationship();
		conquered.playerRelationships[conqueror.id] = new PlayerRelationship();

		TerrainType terrain = new() {
			Key = "plains",
			baseFoodProduction = 0,
			movementCost = 1,
		};
		Tile tile = new(ids.CreateID("tile")) {
			baseTerrainType = terrain,
			overlayTerrainType = terrain,
		};
		City city = new(tile, conqueror, "Captured City", ids.CreateID("city"));
		tile.cityAtTile = city;
		conqueror.cities.Add(city);
		gameData.cities.Add(city);
		gameData.players.Add(conqueror);
		gameData.players.Add(conquered);

		// A non-default citizen avoids invoking tile assignment in these
		// focused tests. Resistance behavior itself is independent of the
		// citizen's specialist/laborer type.
		CitizenType citizenType = new() { IsDefaultCitizen = false };
		return (gameData, conqueror, conquered, city, citizenType);
	}

	private static CityResident AddResister(City city, Civilization nationality, CitizenType citizenType) {
		CityResident resident = new() {
			city = city,
			nationality = nationality,
			citizenType = citizenType,
			tileWorked = Tile.NONE,
			isResisting = true,
		};
		city.residents.Add(resident);
		return resident;
	}

	private static void AddGarrisonUnit(City city, Player owner, string category, int attack, int defense, int bombard = 0) {
		UnitPrototype prototype = new() {
			name = "Garrison",
			attack = attack,
			defense = defense,
			bombard = bombard,
		};
		prototype.categories.Add(category);
		MapUnit unit = prototype.GetInstance(ID.None("unit"), prototype, owner, location: city.location);
		city.location.unitsOnTile.Add(unit);
	}

	[Fact]
	public void GarrisonCapUsesOnlyGroundCombatUnits() {
		var (gameData, conqueror, conquered, city, citizenType) = MakeCapturedCity();
		for (int i = 0; i < 4; ++i) {
			AddResister(city, conquered.civilization, citizenType);
		}

		AddGarrisonUnit(city, conqueror, category: "Land", attack: 1, defense: 1);
		AddGarrisonUnit(city, conqueror, category: "Sea", attack: 2, defense: 2);
		AddGarrisonUnit(city, conqueror, category: "Land", attack: 0, defense: 1, bombard: 4);
		AddGarrisonUnit(city, conqueror, category: "Land", attack: 0, defense: 0);

		int quelled = city.QuellResistance(gameData);

		Assert.Equal(1, quelled);
		Assert.Equal(3, city.residents.Count(resident => resident.isResisting));
	}

	[Fact]
	public void MilitaryLawMultipliesThePerUnitQuellingCap() {
		var (gameData, conqueror, conquered, city, citizenType) = MakeCapturedCity(militaryLaw: 2);
		for (int i = 0; i < 5; ++i) {
			AddResister(city, conquered.civilization, citizenType);
		}
		AddGarrisonUnit(city, conqueror, category: "Land", attack: 1, defense: 1);
		AddGarrisonUnit(city, conqueror, category: "Land", attack: 1, defense: 1);

		int quelled = city.QuellResistance(gameData);

		Assert.Equal(4, quelled);
		Assert.Single(city.residents.Where(resident => resident.isResisting));
	}

	[Fact]
	public void PeaceEndsResistanceWithoutAGarrison() {
		var (gameData, conqueror, conquered, city, citizenType) = MakeCapturedCity(continuedResistanceChance: 100);
		for (int i = 0; i < 3; ++i) {
			AddResister(city, conquered.civilization, citizenType);
		}
		MultiTurnDeal peace = new(
			DealType.DiplomaticAgreement,
			DealSubType.Peace,
			DealDetails.Exchange
		);
		conqueror.playerRelationships[conquered.id].multiTurnDeals.Add(peace);
		conquered.playerRelationships[conqueror.id].multiTurnDeals.Add(peace);

		int quelled = city.QuellResistance(gameData);

		Assert.Equal(3, quelled);
		Assert.DoesNotContain(city.residents, resident => resident.isResisting);
	}

	[Fact]
	public void ContinuedResistanceChanceUsesCultureBandAndGovernmentPair() {
		var (gameData, conqueror, conquered, city, _) = MakeCapturedCity();
		gameData.cultureRelationshipLevels = new List<CultureRelationshipLevel> {
			new() {
				name = "in awe of",
				cultureRatioPercentage = 300,
				cultureRatioNumerator = 3,
				cultureRatioDenominator = 1,
				continuedResistanceChance = 30,
			},
			new() {
				name = "impressed with",
				cultureRatioPercentage = 100,
				cultureRatioNumerator = 1,
				cultureRatioDenominator = 1,
				continuedResistanceChance = 50,
			},
			new() {
				name = "disdainful of",
				cultureRatioPercentage = 33,
				cultureRatioNumerator = 1,
				cultureRatioDenominator = 3,
				continuedResistanceChance = 80,
			},
		};
		city.perPlayerCulture[conqueror] = 300;
		city.perPlayerCulture[conquered] = 100;
		conqueror.government.resistanceModifierByForeignGovernment[conquered.government.name] = 5;

		CultureRelationshipLevel relationship = ResistanceRules.GetCultureRelationship(gameData, conqueror, conquered);
		int chance = ResistanceRules.ResistanceChance(gameData, conqueror, conquered, continuedResistance: true);

		Assert.Equal("in awe of", relationship.name);
		Assert.Equal(35, chance);
	}

	[Fact]
	public void ResistersConsumeNoFoodAndAreRemovedFirstByStarvation() {
		var (gameData, conqueror, conquered, city, citizenType) = MakeCapturedCity();
		CityResident productiveCitizen = new() {
			city = city,
			nationality = conqueror.civilization,
			citizenType = citizenType,
			tileWorked = Tile.NONE,
		};
		city.residents.Add(productiveCitizen);
		city.residents.Add(new CityResident {
			city = city,
			nationality = conqueror.civilization,
			citizenType = citizenType,
			tileWorked = Tile.NONE,
		});
		CityResident resister = AddResister(city, conquered.civilization, citizenType);

		Assert.Equal(4, city.FoodConsumedPerTurn());

		city.foodStored = 0;
		city.HandleCityGrowth(gameData);

		Assert.DoesNotContain(resister, city.residents);
		Assert.Contains(productiveCitizen, city.residents);
	}
}
