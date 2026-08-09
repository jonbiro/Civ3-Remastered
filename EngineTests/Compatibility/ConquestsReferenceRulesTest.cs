using System;
using System.Collections.Generic;
using System.Linq;
using EngineTests.Utils;
using QueryCiv3;
using QueryCiv3.Biq;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Compatibility contracts for the stock Civilization III: Conquests ruleset.
///
/// These tests intentionally do not embed or redistribute the original BIQ.
/// They run only when a contributor has a validated Civilization III Complete
/// installation available locally through CIV3_HOME or normal install discovery.
/// Expected values are independently documented by the original Conquests text
/// data and recorded in docs/compatibility/CONQUESTS_REFERENCE.md.
/// </summary>
public class ConquestsReferenceRulesTest {
	private static BiqData LoadReferenceBiq() {
		Skip.If(
			Civ3TestData.ShouldSkipCiv3DependentTests(),
			"No validated Civilization III Complete install found."
		);

		Civ3Installation installation = Civ3InstallationProbe.TryOpen(Civ3Location.GetCiv3Path());
		Assert.NotNull(installation);
		return BiqData.LoadFile(installation.ConquestsBiqPath);
	}

	[SkippableFact]
	public void DefaultRulesExposeExpectedDifficultyContentCitizens() {
		BiqData biq = LoadReferenceBiq();
		Dictionary<string, int> expected = new(StringComparer.OrdinalIgnoreCase) {
			["Chieftain"] = 4,
			["Warlord"] = 3,
			["Regent"] = 2,
			["Monarch"] = 2,
			["Emperor"] = 1,
			["Demigod"] = 1,
			["Deity"] = 1,
			["Sid"] = 1,
		};

		Assert.NotNull(biq.Diff);
		Assert.Equal(expected.Count, biq.Diff.Length);
		foreach ((string name, int contentCitizens) in expected) {
			DIFF difficulty = biq.Diff.Single(d =>
				string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
			Assert.Equal(contentCitizens, difficulty.NumberOfCitizensBornContent);
		}
	}

	[SkippableFact]
	public void DefaultRulesExposeExpectedGovernmentUnitSupport() {
		BiqData biq = LoadReferenceBiq();
		Dictionary<string, (int town, int city, int metropolis)> expected = new(StringComparer.OrdinalIgnoreCase) {
			["Anarchy"] = (0, 0, 0),
			["Despotism"] = (4, 4, 4),
			["Monarchy"] = (2, 4, 8),
			["Republic"] = (1, 3, 4),
			["Feudalism"] = (5, 2, 1),
			["Communism"] = (6, 6, 6),
			["Fascism"] = (4, 7, 10),
			["Democracy"] = (0, 0, 0),
		};

		Assert.NotNull(biq.Govt);
		Assert.Equal(expected.Count, biq.Govt.Length);
		foreach ((string name, (int town, int city, int metropolis) support) in expected) {
			GOVT government = biq.Govt.Single(g =>
				string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
			Assert.Equal(support.town, government.FreeUnitsPerTown);
			Assert.Equal(support.city, government.FreeUnitsPerCity);
			Assert.Equal(support.metropolis, government.FreeUnitsPerMetropolis);
		}
	}

	[SkippableFact]
	public void DefaultRulesExposeExpectedTerrainDefenseBonuses() {
		BiqData biq = LoadReferenceBiq();
		Dictionary<string, int> expected = new(StringComparer.OrdinalIgnoreCase) {
			["Desert"] = 10,
			["Plains"] = 10,
			["Grassland"] = 10,
			["Tundra"] = 10,
			["Flood Plain"] = 10,
			["Hills"] = 50,
			["Mountains"] = 100,
			["Forest"] = 25,
			["Jungle"] = 25,
			["Marsh"] = 20,
			["Volcano"] = 80,
			["Coast"] = 10,
			["Sea"] = 10,
			["Ocean"] = 10,
		};

		Assert.NotNull(biq.Terr);
		foreach ((string name, int defenseBonus) in expected) {
			TERR terrain = biq.Terr.Single(t =>
				string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
			Assert.Equal(defenseBonus, terrain.DefenseBonus);
		}
	}

	[SkippableFact]
	public void DefaultRulesExposeExpectedGlobalDefenseBonuses() {
		BiqData biq = LoadReferenceBiq();
		Assert.NotNull(biq.Rule);
		RULE rule = Assert.Single(biq.Rule);

		Assert.Equal(25, rule.FortificationsDefensiveBonus);
		Assert.Equal(25, rule.RiverDefensiveBonus);
		Assert.Equal(50, rule.TownDefenseBonus);
		Assert.Equal(50, rule.CityDefenseBonus);
		Assert.Equal(100, rule.MetropolisDefenseBonus);
	}

	[SkippableFact]
	public void DefaultRulesExposeExpectedExperienceLevels() {
		BiqData biq = LoadReferenceBiq();
		Assert.NotNull(biq.Expr);
		Assert.Equal(4, biq.Expr.Length);
		Assert.Equal(
			new[] { "Conscript", "Regular", "Veteran", "Elite" },
			biq.Expr.Select(level => level.Name).ToArray()
		);
	}

	[SkippableFact]
	public void DefaultRulesExposeTwentySevenNaturalResources() {
		BiqData biq = LoadReferenceBiq();
		Assert.NotNull(biq.Good);
		Assert.Equal(27, biq.Good.Length);
	}
}
