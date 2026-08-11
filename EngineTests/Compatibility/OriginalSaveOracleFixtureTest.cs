using System;
using System.IO;
using EngineTests.Utils;
using QueryCiv3;
using Xunit;
using Xunit.Abstractions;

namespace EngineTests.Compatibility;

/// <summary>
/// Opt-in contracts against paired private Civilization III saves. Public CI
/// validates the harness itself but never receives or reads original SAV files.
/// </summary>
public class OriginalSaveOracleFixtureTest {
	private readonly ITestOutputHelper output;

	public OriginalSaveOracleFixtureTest(ITestOutputHelper output) {
		this.output = output;
	}

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
	public void OracleManifestRejectsUnknownProperties() {
		const string json = """
		{
		  "name": "typo oracle",
		  "beforeSave": "before.sav",
		  "afterSave": "after.sav",
		  "playerCivilization": "Rome",
		  "before": { "turn": 40, "warWearinessPointz": 30 },
		  "after": {},
		  "delta": {}
		}
		""";

		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			Civ3OracleFixtures.ParseManifest(json)
		);
		Assert.Contains("supported schema", exception.Message);
	}

	[Fact]
	public void RelationshipAssertionsRequireAnOpponentSelector() {
		const string json = """
		{
		  "name": "missing opponent",
		  "beforeSave": "before.sav",
		  "afterSave": "after.sav",
		  "playerCivilization": "Rome",
		  "before": { "warWearinessPoints": 30 },
		  "after": {},
		  "delta": {}
		}
		""";

		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			Civ3OracleFixtures.ParseManifest(json)
		);
		Assert.Contains("opponentCivilization", exception.Message);
	}

	[Fact]
	public void ResisterAssertionsRequireACitySelector() {
		const string json = """
		{
		  "name": "missing city",
		  "beforeSave": "before.sav",
		  "afterSave": "after.sav",
		  "playerCivilization": "Rome",
		  "before": { "resisterCount": 2 },
		  "after": {},
		  "delta": {}
		}
		""";

		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			Civ3OracleFixtures.ParseManifest(json)
		);
		Assert.Contains("cityName", exception.Message);
	}

	[Fact]
	public void OracleManifestMustAssertAtLeastOneSignal() {
		const string json = """
		{
		  "name": "empty oracle",
		  "beforeSave": "before.sav",
		  "afterSave": "after.sav",
		  "playerCivilization": "Rome",
		  "before": {},
		  "after": {},
		  "delta": {}
		}
		""";

		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			Civ3OracleFixtures.ParseManifest(json)
		);
		Assert.Contains("at least one observed signal", exception.Message);
	}

	[Fact]
	public void OracleManifestRequiresDistinctRelativeSavPaths() {
		const string sameSave = """
		{
		  "name": "same save",
		  "beforeSave": "same.sav",
		  "afterSave": "same.sav",
		  "playerCivilization": "Rome",
		  "before": { "turn": 40 },
		  "after": {},
		  "delta": {}
		}
		""";
		const string rootedSave = """
		{
		  "name": "rooted save",
		  "beforeSave": "C:\\private\\before.sav",
		  "afterSave": "after.sav",
		  "playerCivilization": "Rome",
		  "before": { "turn": 40 },
		  "after": {},
		  "delta": {}
		}
		""";

		Assert.Throws<InvalidDataException>(() => Civ3OracleFixtures.ParseManifest(sameSave));
		Assert.Throws<InvalidDataException>(() => Civ3OracleFixtures.ParseManifest(rootedSave));
	}

	[Fact]
	public void OracleLoaderIgnoresUnrelatedJsonFiles() {
		string root = Path.Combine(Path.GetTempPath(), $"civ3-oracle-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try {
			File.WriteAllText(Path.Combine(root, "notes.json"), "not an oracle manifest");
			File.WriteAllText(Path.Combine(root, "fixture.oracle.json"), """
			{
			  "name": "turn boundary",
			  "beforeSave": "before.sav",
			  "afterSave": "after.sav",
			  "playerCivilization": "Rome",
			  "before": { "turn": 40 },
			  "after": { "turn": 41 },
			  "delta": { "turn": 1 }
			}
			""");

			var manifests = Civ3OracleFixtures.LoadManifests(root);

			Assert.Single(manifests);
			Assert.Equal("turn boundary", manifests[0].Manifest.Name);
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void OracleFixturePathsCannotEscapeThePrivateFixtureRoot() {
		string root = Path.Combine(Path.GetTempPath(), $"civ3-oracle-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try {
			string manifestPath = Path.Combine(root, "fixture.oracle.json");
			File.WriteAllText(manifestPath, "{}");

			Assert.Throws<InvalidDataException>(() =>
				Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, "../outside.sav")
			);
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void OracleFixturePathsMustBeRelativeSavFilesInsideTheRoot() {
		string root = Path.Combine(Path.GetTempPath(), $"civ3-oracle-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try {
			string manifestPath = Path.Combine(root, "fixture.oracle.json");
			string savePath = Path.Combine(root, "inside.sav");
			string textPath = Path.Combine(root, "inside.txt");
			File.WriteAllText(manifestPath, "{}");
			File.WriteAllText(savePath, "private placeholder");
			File.WriteAllText(textPath, "not a save");

			Assert.Equal(
				savePath,
				Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, "inside.sav")
			);
			Assert.Throws<InvalidDataException>(() =>
				Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, savePath)
			);
			Assert.Throws<InvalidDataException>(() =>
				Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, "C:\\private\\inside.sav")
			);
			Assert.Throws<InvalidDataException>(() =>
				Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, "inside.txt")
			);
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void OracleManifestItselfMustBeInsideThePrivateRoot() {
		string root = Path.Combine(Path.GetTempPath(), $"civ3-oracle-root-{Guid.NewGuid():N}");
		string outside = Path.Combine(Path.GetTempPath(), $"civ3-oracle-outside-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		Directory.CreateDirectory(outside);
		try {
			string manifestPath = Path.Combine(outside, "fixture.oracle.json");
			File.WriteAllText(manifestPath, "{}");
			File.WriteAllText(Path.Combine(outside, "inside.sav"), "private placeholder");

			Assert.Throws<InvalidDataException>(() =>
				Civ3OracleFixtures.ResolvePrivateFixturePath(root, manifestPath, "inside.sav")
			);
		} finally {
			Directory.Delete(root, recursive: true);
			Directory.Delete(outside, recursive: true);
		}
	}

	[Fact]
	public void ObservedContractOutputContainsCopyableBeforeAfterAndDeltaValues() {
		Civ3OracleManifest manifest = new() {
			Name = "report oracle",
			BeforeSave = "before.sav",
			AfterSave = "after.sav",
			PlayerCivilization = "Rome",
			OpponentCivilization = "Greece",
			CityName = "Athens",
		};
		Civ3OracleSnapshot before = new() {
			Turn = 40,
			WarWearinessPoints = 30,
			AtWar = true,
			HasTriggeredGoldenAge = false,
			GoldenAgeTurnsRemaining = 0,
			ResisterCount = 2,
		};
		Civ3OracleSnapshot after = new() {
			Turn = 41,
			WarWearinessPoints = 33,
			AtWar = true,
			HasTriggeredGoldenAge = true,
			GoldenAgeTurnsRemaining = 20,
			ResisterCount = 1,
		};

		string report = Civ3OracleFixtures.FormatObservedContract(manifest, before, after);

		Assert.Contains("\"before\"", report);
		Assert.Contains("\"after\"", report);
		Assert.Contains("\"delta\"", report);
		Assert.Contains("\"warWearinessPoints\": 3", report);
		Assert.Contains("\"resisterCount\": -1", report);
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

			output.WriteLine(Civ3OracleFixtures.FormatObservedContract(manifest, before, after));
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
