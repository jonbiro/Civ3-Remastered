using ConvertCiv3Media;
using EngineTests.Utils;
using QueryCiv3;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Opt-in smoke tests against a contributor's private Civilization III Complete
/// installation. Public CI skips these because the original game files are not
/// part of this repository.
/// </summary>
public class OriginalCiv3FilesSmokeTest {
	private static Civ3Installation GetInstallation() {
		return Civ3InstallationProbe.TryOpen(Civ3Location.GetCiv3Path());
	}

	[SkippableFact]
	public void DefaultConquestsBiqParsesCoreRuleSections() {
		Skip.If(Civ3TestData.ShouldSkipCiv3DependentTests(), "No validated Civilization III Complete install found.");

		Civ3Installation installation = GetInstallation();
		Assert.NotNull(installation);

		BiqData biq = BiqData.LoadFile(installation.ConquestsBiqPath);

		Assert.NotNull(biq.Bldg);
		Assert.NotEmpty(biq.Bldg);
		Assert.NotNull(biq.Prto);
		Assert.NotEmpty(biq.Prto);
		Assert.NotNull(biq.Tech);
		Assert.NotEmpty(biq.Tech);
		Assert.NotNull(biq.Terr);
		Assert.NotEmpty(biq.Terr);
		Assert.NotNull(biq.Govt);
		Assert.NotEmpty(biq.Govt);
		Assert.NotNull(biq.Race);
		Assert.NotEmpty(biq.Race);
		Assert.NotNull(biq.Rule);
		Assert.NotEmpty(biq.Rule);
	}

	[SkippableFact]
	public void OriginalButtonsPcxDecodesSuccessfully() {
		Skip.If(Civ3TestData.ShouldSkipCiv3DependentTests(), "No validated Civilization III Complete install found.");

		Civ3Installation installation = GetInstallation();
		Assert.NotNull(installation);

		Pcx pcx = new(installation.ButtonsPath);

		Assert.True(pcx.Width > 0);
		Assert.True(pcx.Height > 0);
		Assert.NotNull(pcx.ColorIndices);
		Assert.Equal(pcx.Width * pcx.Height, pcx.ColorIndices.Length);
		Assert.Equal(256, pcx.Palette.GetLength(0));
		Assert.Equal(3, pcx.Palette.GetLength(1));
	}
}
