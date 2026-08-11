using System;
using System.IO;
using EngineTests.Utils;
using QueryCiv3;
using Xunit;

namespace EngineTests.Compatibility;

/// <summary>
/// Opt-in contracts against paired private Civilization III saves. Public CI
/// validates the harness itself but never receives or reads original SAV files.
/// </summary>
public class OriginalSaveOracleFixtureTest {
	[Fact]
	public void OracleManifestSchemaSupportsAllQueuedCompatibilitySignals() {
		const string json = """
		{
		  "name": "representative oracle",
		  "beforeSave": "before.sav",
		  "afterSave": "after.sav",
		  "playerCivilization": "Rome",
		  "opponentCivilization": "Greece",
		  "cityName": "Athens",
		  "before": {
		    "turn": 40,
		    "warWearinessPoints": 30,
		    "atWar": true,
		    "hasTriggeredGoldenAge": false,
		    "goldenAgeTurnsRemaining": 0,
		    "resisterCount": 2
		  },
		  "after": {
		    "turn": 41,
		    "warWearinessPoints": 31,
		    "atWar": true,
		    "hasTriggeredGoldenAge": true,
		    "goldenAgeTurnsRemaining": 20,
		    "resisterCount": 1
		  },
		  "delta": {
		    "turn": 1,
		    "warWearinessPoints": 1,
		    "goldenAgeTurnsRemaining": 20,
		    "resisterCount": -1
		  }
		}
		""";

		Civ3OracleManifest manifest = Civ3OracleFixtures.ParseManifest(json);

		Assert.Equal("representative oracle", manifest.Name);
		Assert.Equal(30, manifest.Before.WarWearinessPoints);
		Assert.True(manifest.Before.AtWar);
		Assert.False(manifest.Before.HasTriggeredGoldenAge);
		Assert.Equal(20, manifest.After.GoldenAgeTurnsRemaining);
		Assert.Equal(-1, manifest.Delta.ResisterCount);
	}

	[Fact]
	public void OracleFixturePathsCannotEscapeThePrivateFixtureRoot() {
		string root = Path.Combine(Path.GetTempPath(), $"civ3-oracle-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try {
			string manifestPath = Path.Combine(root, "fixture.json");
			File.WriteAllText(manifestPath, "{}");

			Assert.Throws<InvalidDataException>(() =>
				Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, "../outside.sav")
			);
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[SkippableFact]
	public void PrivatePairedSavesMatchRecordedOracleContracts() {
		Skip.If(
			Civ3OracleFixtures.ShouldSkipOracleTests(),
			$"Set CIV3_HOME and {Civ3OracleFixtures.EnvironmentVariable} to run private original-save oracles."
		);

		string root = Civ3OracleFixtures.GetRootPath();
		Civ3Installation installation = Civ3InstallationProbe.TryOpen(Civ3Location.GetCiv3Path());
		Assert.NotNull(installation);

		var manifests = Civ3OracleFixtures.LoadManifests(root);
		Assert.NotEmpty(manifests);
		foreach ((string manifestPath, Civ3OracleManifest manifest) in manifests) {
			string beforePath = Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, manifest.BeforeSave);
			string afterPath = Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, manifest.AfterSave);
			Civ3OracleSnapshot before = Civ3OracleFixtures.Capture(beforePath, manifest, installation);
			Civ3OracleSnapshot after = Civ3OracleFixtures.Capture(afterPath, manifest, installation);

			AssertSnapshot(manifest.Before, before);
			AssertSnapshot(manifest.After, after);
			AssertDelta(manifest.Delta, before, after);
		}
	}

	private static void AssertSnapshot(Civ3OracleExpectedSnapshot expected, Civ3OracleSnapshot actual) {
		if (expected.Turn.HasValue) Assert.Equal(expected.Turn.Value, actual.Turn);
		if (expected.WarWearinessPoints.HasValue) Assert.Equal(expected.WarWearinessPoints.Value, actual.WarWearinessPoints);
		if (expected.AtWar.HasValue) Assert.Equal(expected.AtWar.Value, actual.AtWar);
		if (expected.HasTriggeredGoldenAge.HasValue) Assert.Equal(expected.HasTriggeredGoldenAge.Value, actual.HasTriggeredGoldenAge);
		if (expected.GoldenAgeTurnsRemaining.HasValue) Assert.Equal(expected.GoldenAgeTurnsRemaining.Value, actual.GoldenAgeTurnsRemaining);
		if (expected.ResisterCount.HasValue) Assert.Equal(expected.ResisterCount.Value, actual.ResisterCount);
	}

	private static void AssertDelta(
		Civ3OracleExpectedDelta expected,
		Civ3OracleSnapshot before,
		Civ3OracleSnapshot after
	) {
		if (expected.Turn.HasValue) {
			Assert.Equal(expected.Turn.Value, after.Turn - before.Turn);
		}
		if (expected.WarWearinessPoints.HasValue) {
			Assert.True(before.WarWearinessPoints.HasValue && after.WarWearinessPoints.HasValue);
			Assert.Equal(
				expected.WarWearinessPoints.Value,
				after.WarWearinessPoints.Value - before.WarWearinessPoints.Value
			);
		}
		if (expected.GoldenAgeTurnsRemaining.HasValue) {
			Assert.Equal(
				expected.GoldenAgeTurnsRemaining.Value,
				after.GoldenAgeTurnsRemaining - before.GoldenAgeTurnsRemaining
			);
		}
		if (expected.ResisterCount.HasValue) {
			Assert.True(before.ResisterCount.HasValue && after.ResisterCount.HasValue);
			Assert.Equal(
				expected.ResisterCount.Value,
				after.ResisterCount.Value - before.ResisterCount.Value
			);
		}
	}
}
