using System;
using System.Linq;
using EngineTests.Utils;
using QueryCiv3;
using QueryCiv3.Biq;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Private-install parser contracts for the Conquests trade connector flags.
/// The original BIQ remains outside the repository.
/// </summary>
public class ConquestsTradeReferenceTest {
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
	public void StockHarborAndAirportExposeTradeConnectorFlags() {
		BiqData biq = LoadReferenceBiq();
		BLDG harbor = biq.Bldg.Single(b => string.Equals(b.Name, "Harbor", StringComparison.OrdinalIgnoreCase));
		BLDG airport = biq.Bldg.Single(b => string.Equals(b.Name, "Airport", StringComparison.OrdinalIgnoreCase));

		Assert.True(harbor.AllowsWaterTrade);
		Assert.True(airport.AllowsAirTrade);
	}

	[SkippableFact]
	public void StockTechnologyTreeContainsSeaAndOceanTradeEnablers() {
		BiqData biq = LoadReferenceBiq();

		Assert.Contains(biq.Tech, tech => tech.EnablesTradeOverSea);
		Assert.Contains(biq.Tech, tech => tech.EnablesTradeOverOcean);
	}
}
