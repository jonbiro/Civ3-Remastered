using C7GameData;
using C7GameData.Save;
using EngineTests.Utils;
using QueryCiv3;
using Xunit;

namespace EngineTests.Compatibility;

public class CulturalVictoryCompatibilityTest {
	private static (Player Player, City City) MakeCity(int culture = 0, string name = "Reference City") {
		Civilization civilization = new(name + " Civilization");
		Player player = new() {
			id = ID.None(name + "-player"),
			civilization = civilization,
			rules = new Rules(),
		};
		Tile tile = new(ID.None(name + "-tile")) {
			baseTerrainType = TerrainType.NONE,
			overlayTerrainType = TerrainType.NONE,
			Resource = Resource.NONE,
		};
		City city = new(tile, player, name, ID.None(name + "-city"));
		city.perPlayerCulture[player] = culture;
		city.residents.Add(new CityResident {
			city = city,
			nationality = civilization,
			tileWorked = Tile.NONE,
			citizenType = new CitizenType { IsDefaultCitizen = true },
		});
		player.cities.Add(city);
		return (player, city);
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(9, 1)]
	[InlineData(10, 2)]
	[InlineData(99, 2)]
	[InlineData(100, 3)]
	[InlineData(999, 3)]
	[InlineData(1_000, 4)]
	[InlineData(9_999, 4)]
	[InlineData(10_000, 5)]
	[InlineData(19_999, 5)]
	[InlineData(20_000, 6)]
	[InlineData(100_000, 6)]
	public void BorderExpansionUsesDocumentedCultureThresholds(int culture, int expectedLevel) {
		var (_, city) = MakeCity(culture);
		Assert.Equal(expectedLevel, city.GetBorderExpansionLevel());
	}

	[Fact]
	public void OneCityCultureTargetQualifiesAtTheExactBoundary() {
		var (player, city) = MakeCity(19_999);
		Rules rules = new() {
			AllowCulturalVictory = true,
			OneCityCultureWin = 20_000,
			AllCitiesCultureWin = 100_000,
		};

		Assert.Null(CulturalVictory.Evaluate(player, rules));

		city.perPlayerCulture[player] = 20_000;
		CulturalVictoryQualification result = CulturalVictory.Evaluate(player, rules);

		Assert.NotNull(result);
		Assert.Equal(CulturalVictoryPath.OneCity, result.Path);
		Assert.Same(city, result.City);
		Assert.Equal(20_000, result.Culture);
		Assert.Equal(20_000, result.Threshold);
	}

	[Fact]
	public void CivilizationCultureTargetSumsActiveCities() {
		var (player, first) = MakeCity(60_000, "First City");
		var (_, secondTemplate) = MakeCity(40_000, "Second City");
		City second = new(secondTemplate.location, player, "Second City", ID.None("second-city"));
		second.perPlayerCulture[player] = 40_000;
		second.residents.Add(new CityResident {
			city = second,
			nationality = player.civilization,
			tileWorked = Tile.NONE,
			citizenType = new CitizenType { IsDefaultCitizen = true },
		});
		player.cities.Add(second);
		Rules rules = new() {
			AllowCulturalVictory = true,
			OneCityCultureWin = 20_000,
			AllCitiesCultureWin = 100_000,
		};

		// Disable the one-city route for this aggregate-only contract.
		rules.OneCityCultureWin = 0;
		CulturalVictoryQualification result = CulturalVictory.Evaluate(player, rules);

		Assert.NotNull(result);
		Assert.Equal(CulturalVictoryPath.Civilization, result.Path);
		Assert.Null(result.City);
		Assert.Equal(100_000, result.Culture);
		Assert.Equal(100_000, result.Threshold);
	}

	[Fact]
	public void CulturalVictoryCanBeDisabledByScenarioRules() {
		var (player, _) = MakeCity(25_000);
		Rules rules = new() {
			AllowCulturalVictory = false,
			OneCityCultureWin = 20_000,
			AllCitiesCultureWin = 100_000,
		};

		Assert.Null(CulturalVictory.Evaluate(player, rules));
	}

	[Fact]
	public void EvaluateAllPreservesSimultaneousQualifiersForCommonVictoryResolution() {
		var (first, _) = MakeCity(20_000, "First");
		var (second, _) = MakeCity(20_000, "Second");
		C7GameData.GameData gameData = new(customSeed: 1234) {
			rules = new Rules {
				AllowCulturalVictory = true,
				OneCityCultureWin = 20_000,
				AllCitiesCultureWin = 100_000,
			},
		};
		gameData.players.Add(first);
		gameData.players.Add(second);

		var results = CulturalVictory.EvaluateAll(gameData);

		Assert.Equal(2, results.Count);
		Assert.Same(first, results[0].Player);
		Assert.Same(second, results[1].Player);
	}

	[SkippableFact]
	public void OriginalConquestsBiqCultureVictorySettingsReachRuntimeRules() {
		Skip.If(Civ3TestData.ShouldSkipCiv3DependentTests(), "No private Civilization III Complete installation configured.");
		Civ3Installation installation = Civ3InstallationProbe.TryOpen(Civ3Location.GetCiv3Path());
		Assert.NotNull(installation);

		BiqData raw = BiqData.LoadFile(installation.ConquestsBiqPath);
		SaveGame imported = ImportCiv3.ImportBiq(
			installation.ConquestsBiqPath,
			installation.ConquestsBiqPath,
			_ => installation.PediaIconsPath
		);

		Assert.Equal(raw.Game[0].CulturalVictory, imported.Rules.AllowCulturalVictory);
		Assert.Equal(raw.Game[0].OneCityCultureWin, imported.Rules.OneCityCultureWin);
		Assert.Equal(raw.Game[0].AllCitiesCultureWin, imported.Rules.AllCitiesCultureWin);
		Assert.True(imported.Rules.OneCityCultureWin > 0);
		Assert.True(imported.Rules.AllCitiesCultureWin > 0);
	}
}
