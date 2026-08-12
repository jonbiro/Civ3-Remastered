using System.Collections.Generic;
using System.Linq;
using C7GameData;
using C7GameData.Save;
using EngineTests.Utils;
using QueryCiv3;
using Xunit;

namespace EngineTests.Compatibility;

public class VictoryOutcomeCompatibilityTest {
	private static Player MakePlayer(string name, int population = 0) {
		Civilization civilization = new(name + " Civilization");
		Player player = new() {
			id = ID.None(name + "-player"),
			civilization = civilization,
			rules = new Rules(),
		};

		if (population > 0) {
			Tile cityTile = new(ID.None(name + "-city-tile")) {
				baseTerrainType = new TerrainType { Key = "plains" },
				overlayTerrainType = new TerrainType { Key = "plains" },
			};
			City city = new(cityTile, player, name + " City", ID.None(name + "-city"));
			for (int i = 0; i < population; ++i) {
				city.residents.Add(new CityResident {
					city = city,
					nationality = civilization,
					tileWorked = Tile.NONE,
					citizenType = new CitizenType { IsDefaultCitizen = true },
				});
			}
			player.cities.Add(city);
		}

		return player;
	}

	private static C7GameData.GameData MakeDominationState(int firstLand, int firstPopulation, int secondPopulation) {
		C7GameData.GameData gameData = new(customSeed: 12345) {
			rules = new Rules {
				AllowDominationVictory = true,
				DominationTerrainPercent = 70,
				DominationPopulationPercent = 70,
			},
		};
		Player first = MakePlayer("First", firstPopulation);
		Player second = MakePlayer("Second", secondPopulation);
		gameData.players.Add(first);
		gameData.players.Add(second);

		TerrainType plains = new() { Key = "plains" };
		TerrainType ocean = new() { Key = "ocean" };
		for (int i = 0; i < 10; ++i) {
			Tile tile = new(ID.None("land-" + i)) {
				baseTerrainType = plains,
				overlayTerrainType = plains,
			};
			tile.owningCity = i < firstLand ? first.cities[0] : second.cities[0];
			gameData.map.tiles.Add(tile);
		}

		// Water ownership must never enter the Domination land denominator.
		Tile water = new(ID.None("water")) {
			baseTerrainType = ocean,
			overlayTerrainType = ocean,
			owningCity = second.cities[0],
		};
		gameData.map.tiles.Add(water);
		return gameData;
	}

	[Fact]
	public void ConquestRequiresExactlyOneEligibleCivilization() {
		C7GameData.GameData gameData = new(customSeed: 1) {
			rules = new Rules { AllowConquestVictory = true },
		};
		Player first = MakePlayer("First", 1);
		Player second = MakePlayer("Second", 1);
		gameData.players.Add(first);
		gameData.players.Add(second);

		Assert.DoesNotContain(VictoryResolver.Evaluate(gameData).Claims, claim => claim.Type == VictoryType.Conquest);

		second.defeated = true;
		VictoryClaim claim = Assert.Single(VictoryResolver.Evaluate(gameData).Claims.Where(claim => claim.Type == VictoryType.Conquest));
		Assert.Equal(first.id, claim.PlayerId);
	}

	[Fact]
	public void DisabledConquestDoesNotQualifySingleSurvivor() {
		C7GameData.GameData gameData = new(customSeed: 2) {
			rules = new Rules { AllowConquestVictory = false },
		};
		gameData.players.Add(MakePlayer("Only", 1));

		Assert.DoesNotContain(VictoryResolver.Evaluate(gameData).Claims, claim => claim.Type == VictoryType.Conquest);
	}

	[Fact]
	public void DominationQualifiesAtExactTerrainAndPopulationBoundaries() {
		C7GameData.GameData gameData = MakeDominationState(firstLand: 7, firstPopulation: 7, secondPopulation: 3);
		Player first = gameData.players[0];

		VictoryClaim claim = Assert.Single(VictoryResolver.Evaluate(gameData).Claims.Where(claim => claim.Type == VictoryType.Domination));

		Assert.Equal(first.id, claim.PlayerId);
		Assert.Equal(7, claim.CurrentValue);
		Assert.Equal(10, claim.TotalValue);
		Assert.Equal(70, claim.RequiredValue);
		Assert.Equal(7, claim.SecondaryCurrentValue);
		Assert.Equal(10, claim.SecondaryTotalValue);
		Assert.Equal(70, claim.SecondaryRequiredValue);
	}

	[Fact]
	public void DominationRequiresBothThresholds() {
		C7GameData.GameData belowLand = MakeDominationState(firstLand: 6, firstPopulation: 7, secondPopulation: 3);
		Assert.DoesNotContain(VictoryResolver.Evaluate(belowLand).Claims, claim => claim.Type == VictoryType.Domination);

		C7GameData.GameData belowPopulation = MakeDominationState(firstLand: 7, firstPopulation: 6, secondPopulation: 4);
		Assert.DoesNotContain(VictoryResolver.Evaluate(belowPopulation).Claims, claim => claim.Type == VictoryType.Domination);
	}

	[Fact]
	public void CulturalQualificationFeedsTheCommonOutcome() {
		C7GameData.GameData gameData = new(customSeed: 3) {
			rules = new Rules {
				AllowCulturalVictory = true,
				OneCityCultureWin = 20_000,
			},
		};
		Player player = MakePlayer("Culture", 1);
		player.cities[0].perPlayerCulture[player] = 20_000;
		gameData.players.Add(player);

		VictoryClaim claim = Assert.Single(VictoryResolver.Evaluate(gameData).Claims);
		Assert.Equal(VictoryType.Cultural, claim.Type);
		Assert.Equal(player.id, claim.PlayerId);
		Assert.Equal(player.cities[0].id, claim.CityId);
	}

	[Fact]
	public void MultipleVictoryTypesForSameCivilizationResolveToOneWinner() {
		ID winner = ID.None("winner");
		GameOutcome outcome = new() {
			Claims = [
				new VictoryClaim { Type = VictoryType.Cultural, PlayerId = winner },
				new VictoryClaim { Type = VictoryType.Domination, PlayerId = winner },
			],
		};

		Assert.True(outcome.IsResolved);
		Assert.Equal(winner, outcome.WinnerId);
	}

	[Fact]
	public void ClaimsForDifferentCivilizationsRemainUnresolved() {
		GameOutcome outcome = new() {
			Claims = [
				new VictoryClaim { Type = VictoryType.Cultural, PlayerId = ID.None("first") },
				new VictoryClaim { Type = VictoryType.Conquest, PlayerId = ID.None("second") },
			],
		};

		Assert.True(outcome.HasClaims);
		Assert.False(outcome.IsResolved);
		Assert.Null(outcome.WinnerId);
	}

	[Fact]
	public void FirstTurnBoundaryOutcomeIsNeverOverwritten() {
		C7GameData.GameData gameData = new(customSeed: 4) {
			turn = 42,
			rules = new Rules { AllowConquestVictory = true },
		};
		Player winner = MakePlayer("Winner", 1);
		gameData.players.Add(winner);

		GameOutcome first = VictoryResolver.RecordAtTurnBoundary(gameData);
		Assert.NotNull(first);
		Assert.Equal(42, first.Turn);

		gameData.turn = 43;
		winner.defeated = true;
		GameOutcome second = VictoryResolver.RecordAtTurnBoundary(gameData);

		Assert.Same(first, second);
		Assert.Equal(42, second.Turn);
	}

	[Fact]
	public void OutcomeSurvivesNativeSaveJsonClone() {
		ID winner = ID.None("winner");
		SaveGame saved = new() {
			Outcome = new GameOutcome {
				Turn = 77,
				Claims = [new VictoryClaim {
					Type = VictoryType.Cultural,
					PlayerId = winner,
					CurrentValue = 20_000,
					RequiredValue = 20_000,
				}],
			},
		};

		SaveGame cloned = saved.Clone();

		Assert.NotNull(cloned.Outcome);
		Assert.Equal(77, cloned.Outcome.Turn);
		VictoryClaim claim = Assert.Single(cloned.Outcome.Claims);
		Assert.Equal(VictoryType.Cultural, claim.Type);
		Assert.Equal(winner, claim.PlayerId);
	}

	[SkippableFact]
	public void OriginalConquestsBiqConquestAndDominationSettingsReachRuntimeRules() {
		Skip.If(Civ3TestData.ShouldSkipCiv3DependentTests(), "No private Civilization III Complete installation configured.");
		Civ3Installation installation = Civ3InstallationProbe.TryOpen(Civ3Location.GetCiv3Path());
		Assert.NotNull(installation);

		BiqData raw = BiqData.LoadFile(installation.ConquestsBiqPath);
		SaveGame imported = ImportCiv3.ImportBiq(
			installation.ConquestsBiqPath,
			installation.ConquestsBiqPath,
			_ => installation.PediaIconsPath
		);

		Assert.Equal(raw.Game[0].ConquestVictory, imported.Rules.AllowConquestVictory);
		Assert.Equal(raw.Game[0].DominationVictory, imported.Rules.AllowDominationVictory);
		Assert.Equal(raw.Game[0].DominationTerrain, imported.Rules.DominationTerrainPercent);
		Assert.Equal(raw.Game[0].DominationPopulation, imported.Rules.DominationPopulationPercent);
	}
}
