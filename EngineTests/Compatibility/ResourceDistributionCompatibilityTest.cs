using System.Collections.Generic;
using System.Linq;
using C7Engine;
using C7GameData;
using C7GameData.Save;
using Xunit;
using static C7GameData.Tile.TileOverlays;

namespace EngineTests.Compatibility;

/// <summary>
/// End-to-end compatibility contracts for strategic and luxury resources.
/// These tests begin with map ownership and trade connectivity, then verify
/// the player-facing production and citizen-mood effects.
/// </summary>
public class ResourceDistributionCompatibilityTest {
	private static C7GameData.GameData MakeGameData(int contentCitizens = 8) {
		C7GameData.GameData gameData = new(customSeed: 12345) {
			gameDifficulty = new Difficulty {
				NumberOfCitizensBornContent = contentCitizens,
				PercentageOfOptimalCities = 100,
			},
			rules = new Rules {
				DefaultDealDuration = 20,
				MaximumLevel1CitySize = 6,
				MaximumLevel2CitySize = 12,
				MaxRankOfWorkableTiles = 2,
			},
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
			baseFoodProduction = 0,
			baseShieldProduction = 0,
			baseCommerceProduction = 0,
		};
	}

	private static Tile TileAt(ID.Factory ids, string key = "plains") {
		TerrainType terrain = Terrain(key);
		return new Tile(ids.CreateID("tile")) {
			baseTerrainType = terrain,
			overlayTerrainType = terrain,
			Resource = Resource.NONE,
		};
	}

	private static void Link(Tile left, TileDirection leftToRight, Tile right) {
		left.neighbors[leftToRight] = right;
		right.neighbors[leftToRight.Reversed()] = left;
	}

	private static Player MakePlayer(C7GameData.GameData gameData, ID.Factory ids) {
		Government government = new() {
			id = ids.CreateID("government"),
			name = "Reference Government",
			militaryPoliceLimit = 0,
		};
		Player player = new() {
			id = ids.CreateID("player"),
			civilization = new Civilization("Reference Civilization"),
			government = government,
			rules = gameData.rules,
		};
		gameData.governments.Add(government);
		gameData.players.Add(player);
		return player;
	}

	private static Building TradeBuilding(C7GameData.GameData gameData, string name, SaveBuilding.Flag flag) {
		SaveBuilding saveBuilding = new() { name = name };
		saveBuilding.flags.Add(flag);
		return new Building(saveBuilding, gameData);
	}

	private static TerrainImprovement AddRoad(C7GameData.GameData gameData, params Tile[] tiles) {
		TerrainImprovement road = gameData.terrainImprovements.FirstOrDefault(improvement => improvement.layer == TerrainImprovement.Layer.Roads);
		if (road == null) {
			road = new TerrainImprovement(ROAD, TerrainImprovement.Layer.Roads, movementCost: 1.0f / 3.0f);
			gameData.terrainImprovements.Add(road);
		}
		foreach (Tile tile in tiles) {
			tile.overlays.Add(road);
		}
		return road;
	}

	private static City AddCity(
		C7GameData.GameData gameData,
		Player player,
		Tile tile,
		ID.Factory ids,
		string name,
		bool capital = false,
		int population = 0
	) {
		City city = new(tile, player, name, ids.CreateID("city")) { capital = capital };
		tile.cityAtTile = city;
		tile.owningCity = city;
		player.cities.Add(city);
		gameData.cities.Add(city);

		if (population > 0) {
			CitizenType laborer = gameData.citizenTypes.FirstOrDefault(type => type.IsDefaultCitizen);
			if (laborer == null) {
				laborer = new CitizenType { IsDefaultCitizen = true };
				gameData.citizenTypes.Add(laborer);
			}
			for (int i = 0; i < population; ++i) {
				city.residents.Add(new CityResident {
					city = city,
					nationality = player.civilization,
					citizenType = laborer,
					tileWorked = Tile.NONE,
				});
			}
		}

		return city;
	}

	[Fact]
	public void RoadConnectedStrategicResourceUnlocksResourceUnitProduction() {
		C7GameData.GameData gameData = MakeGameData();
		ID.Factory ids = new();
		Player player = MakePlayer(gameData, ids);
		Tile cityTile = TileAt(ids);
		Tile ironTile = TileAt(ids);
		Link(cityTile, TileDirection.EAST, ironTile);
		City city = AddCity(gameData, player, cityTile, ids, "Capital", capital: true);
		ironTile.owningCity = city;

		Resource iron = new() {
			Key = "IRON",
			Name = "Iron",
			Category = ResourceCategory.STRATEGIC,
		};
		ironTile.Resource = iron;
		gameData.Resources.Add(iron);

		UnitPrototype swordsman = new() {
			name = "Swordsman",
			shieldCost = 30,
			attack = 3,
			defense = 2,
			movement = 1,
		};
		swordsman.categories.Add("Land");
		swordsman.producibleBy.Add(player.civilization);
		swordsman.requiredResources.Add(iron);
		gameData.unitPrototypes.Add(swordsman);

		Assert.DoesNotContain(swordsman, city.ListProductionOptions(gameData));

		AddRoad(gameData, ironTile);
		gameData.InvalidateCachedTradeNetwork();

		Assert.Contains(swordsman, city.ListProductionOptions(gameData));
		Assert.Equal(1, city.GetStrategicResources(gameData)[iron]);
	}

	[Fact]
	public void ResourcePrerequisiteTechControlsVisibilityWithoutChangingTheRoute() {
		C7GameData.GameData gameData = MakeGameData();
		ID.Factory ids = new();
		Player player = MakePlayer(gameData, ids);
		Tile cityTile = TileAt(ids);
		Tile saltpeterTile = TileAt(ids);
		Link(cityTile, TileDirection.EAST, saltpeterTile);
		City city = AddCity(gameData, player, cityTile, ids, "Capital", capital: true);
		saltpeterTile.owningCity = city;
		AddRoad(gameData, saltpeterTile);

		Tech chemistry = new() {
			id = ids.CreateID("tech"),
			Name = "Chemistry",
		};
		gameData.techs.Add(chemistry);
		Resource saltpeter = new() {
			Key = "SALTPETER",
			Name = "Saltpeter",
			Category = ResourceCategory.STRATEGIC,
			Prerequisite = chemistry.id,
		};
		saltpeterTile.Resource = saltpeter;
		gameData.Resources.Add(saltpeter);

		Assert.DoesNotContain(saltpeter, city.GetStrategicResources(gameData).Keys);

		player.knownTechs.Add(chemistry.id);

		Assert.Equal(1, city.GetStrategicResources(gameData)[saltpeter]);
	}

	[Fact]
	public void HarborsCarryStrategicResourcesBetweenDisconnectedLandSegments() {
		C7GameData.GameData gameData = MakeGameData();
		ID.Factory ids = new();
		Player player = MakePlayer(gameData, ids);
		Tile capitalTile = TileAt(ids);
		Tile firstCoast = TileAt(ids, "coast");
		Tile lastCoast = TileAt(ids, "coast");
		Tile portTile = TileAt(ids);
		Link(capitalTile, TileDirection.EAST, firstCoast);
		Link(firstCoast, TileDirection.EAST, lastCoast);
		Link(lastCoast, TileDirection.EAST, portTile);
		City capital = AddCity(gameData, player, capitalTile, ids, "Capital", capital: true);
		City port = AddCity(gameData, player, portTile, ids, "Port");

		Resource horses = new() {
			Key = "HORSES",
			Name = "Horses",
			Category = ResourceCategory.STRATEGIC,
		};
		capitalTile.Resource = horses;
		gameData.Resources.Add(horses);

		Building harbor = TradeBuilding(gameData, "Harbor", SaveBuilding.Flag.AllowsWaterTrade);
		capital.AddBuilding(harbor);
		port.AddBuilding(harbor);
		player.tileKnowledge.AddTilesToKnown(firstCoast, recomputeActiveTiles: false);
		player.tileKnowledge.AddTilesToKnown(lastCoast, recomputeActiveTiles: false);

		Assert.Equal(1, port.GetStrategicResources(gameData)[horses]);
	}

	[Fact]
	public void ConnectedLuxuryChangesCitizenMoodAndDisconnectingItRemovesTheEffect() {
		C7GameData.GameData gameData = MakeGameData(contentCitizens: 4);
		ID.Factory ids = new();
		Player player = MakePlayer(gameData, ids);
		Tile cityTile = TileAt(ids);
		Tile silkTile = TileAt(ids);
		Link(cityTile, TileDirection.EAST, silkTile);
		City city = AddCity(gameData, player, cityTile, ids, "Capital", capital: true, population: 4);
		silkTile.owningCity = city;
		TerrainImprovement road = AddRoad(gameData, silkTile);

		Resource silk = new() {
			Key = "SILK",
			Name = "Silk",
			Category = ResourceCategory.LUXURY,
		};
		silkTile.Resource = silk;
		gameData.Resources.Add(silk);

		city.RecalculateCitizenMoods(gameData);
		Assert.Equal(1, city.residents.Count(resident => resident.mood == CityResident.Mood.Happy));
		Assert.Equal(1, city.GetLuxuries(gameData)[silk]);

		silkTile.overlays.Remove(road);
		gameData.InvalidateCachedTradeNetwork();
		city.RecalculateCitizenMoods(gameData);

		Assert.Equal(0, city.residents.Count(resident => resident.mood == CityResident.Mood.Happy));
		Assert.DoesNotContain(silk, city.GetLuxuries(gameData).Keys);
	}
}
