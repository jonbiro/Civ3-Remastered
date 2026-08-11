using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using C7GameData;
using C7GameData.Save;
using QueryCiv3;

namespace EngineTests.Utils;

public sealed class Civ3OracleManifest {
	public string Name { get; set; }
	public string BeforeSave { get; set; }
	public string AfterSave { get; set; }
	public string PlayerCivilization { get; set; }
	public string OpponentCivilization { get; set; }
	public string CityName { get; set; }
	public Civ3OracleExpectedSnapshot Before { get; set; } = new();
	public Civ3OracleExpectedSnapshot After { get; set; } = new();
	public Civ3OracleExpectedDelta Delta { get; set; } = new();
}

public sealed class Civ3OracleExpectedSnapshot {
	public int? Turn { get; set; }
	public int? WarWearinessPoints { get; set; }
	public bool? AtWar { get; set; }
	public bool? HasTriggeredGoldenAge { get; set; }
	public int? GoldenAgeTurnsRemaining { get; set; }
	public int? ResisterCount { get; set; }
}

public sealed class Civ3OracleExpectedDelta {
	public int? Turn { get; set; }
	public int? WarWearinessPoints { get; set; }
	public int? GoldenAgeTurnsRemaining { get; set; }
	public int? ResisterCount { get; set; }
}

public sealed class Civ3OracleSnapshot {
	public int Turn { get; init; }
	public int? WarWearinessPoints { get; init; }
	public bool? AtWar { get; init; }
	public bool HasTriggeredGoldenAge { get; init; }
	public int GoldenAgeTurnsRemaining { get; init; }
	public int? ResisterCount { get; init; }

	public override string ToString() {
		return JsonSerializer.Serialize(this, Civ3OracleFixtures.JsonOptions);
	}
}

public static class Civ3OracleFixtures {
	public const string EnvironmentVariable = "CIV3_ORACLE_HOME";
	public const string ManifestSearchPattern = "*.oracle.json";

	internal static JsonSerializerOptions JsonOptions { get; } = new() {
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		ReadCommentHandling = JsonCommentHandling.Skip,
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
		WriteIndented = true,
	};

	public static string GetRootPath() {
		string configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
		return string.IsNullOrWhiteSpace(configured) ? null : Path.GetFullPath(configured);
	}

	public static bool ShouldSkipOracleTests() {
		string ci = Environment.GetEnvironmentVariable("CI");
		if (!string.IsNullOrWhiteSpace(ci)
			&& !string.Equals(ci, "false", StringComparison.OrdinalIgnoreCase)
			&& ci != "0") {
			return true;
		}

		string root = GetRootPath();
		if (root == null || !Directory.Exists(root)) {
			return true;
		}

		if (Civ3InstallationProbe.TryOpen(Civ3Location.GetCiv3Path()) == null) {
			return true;
		}

		return !Directory.EnumerateFiles(root, ManifestSearchPattern, SearchOption.AllDirectories).Any();
	}

	public static Civ3OracleManifest ParseManifest(string json) {
		Civ3OracleManifest manifest;
		try {
			manifest = JsonSerializer.Deserialize<Civ3OracleManifest>(json, JsonOptions);
		} catch (JsonException exception) {
			throw new InvalidDataException("Oracle manifest is not valid JSON for the supported schema.", exception);
		}

		if (manifest == null || string.IsNullOrWhiteSpace(manifest.Name)) {
			throw new InvalidDataException("Oracle manifest must contain a non-empty name.");
		}
		if (string.IsNullOrWhiteSpace(manifest.BeforeSave) || string.IsNullOrWhiteSpace(manifest.AfterSave)) {
			throw new InvalidDataException($"Oracle manifest '{manifest.Name}' must name beforeSave and afterSave files.");
		}
		ValidatePortableSavePath(manifest.Name, "beforeSave", manifest.BeforeSave);
		ValidatePortableSavePath(manifest.Name, "afterSave", manifest.AfterSave);
		if (string.Equals(manifest.BeforeSave, manifest.AfterSave, StringComparison.OrdinalIgnoreCase)) {
			throw new InvalidDataException($"Oracle manifest '{manifest.Name}' must use different beforeSave and afterSave files.");
		}
		if (string.IsNullOrWhiteSpace(manifest.PlayerCivilization)) {
			throw new InvalidDataException($"Oracle manifest '{manifest.Name}' must name playerCivilization.");
		}
		if (manifest.Before == null || manifest.After == null || manifest.Delta == null) {
			throw new InvalidDataException($"Oracle manifest '{manifest.Name}' must contain before, after, and delta objects.");
		}

		bool expectsRelationship = manifest.Before.WarWearinessPoints.HasValue
			|| manifest.Before.AtWar.HasValue
			|| manifest.After.WarWearinessPoints.HasValue
			|| manifest.After.AtWar.HasValue
			|| manifest.Delta.WarWearinessPoints.HasValue;
		if (expectsRelationship && string.IsNullOrWhiteSpace(manifest.OpponentCivilization)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifest.Name}' must name opponentCivilization for relationship assertions."
			);
		}

		bool expectsResisters = manifest.Before.ResisterCount.HasValue
			|| manifest.After.ResisterCount.HasValue
			|| manifest.Delta.ResisterCount.HasValue;
		if (expectsResisters && string.IsNullOrWhiteSpace(manifest.CityName)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifest.Name}' must name cityName for resister assertions."
			);
		}

		if (!HasAnyExpectation(manifest)) {
			throw new InvalidDataException($"Oracle manifest '{manifest.Name}' must assert at least one observed signal.");
		}
		if ((manifest.Before.GoldenAgeTurnsRemaining.HasValue && manifest.Before.GoldenAgeTurnsRemaining.Value < 0)
			|| (manifest.After.GoldenAgeTurnsRemaining.HasValue && manifest.After.GoldenAgeTurnsRemaining.Value < 0)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifest.Name}' cannot expect a negative Golden Age turns-remaining snapshot."
			);
		}
		if ((manifest.Before.ResisterCount.HasValue && manifest.Before.ResisterCount.Value < 0)
			|| (manifest.After.ResisterCount.HasValue && manifest.After.ResisterCount.Value < 0)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifest.Name}' cannot expect a negative resister-count snapshot."
			);
		}

		return manifest;
	}

	public static IReadOnlyList<(string ManifestPath, Civ3OracleManifest Manifest)> LoadManifests(string root) {
		if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) {
			throw new DirectoryNotFoundException($"Oracle fixture directory was not found: {root}");
		}

		string fullRoot = Path.GetFullPath(root);
		return Directory.EnumerateFiles(fullRoot, ManifestSearchPattern, SearchOption.AllDirectories)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.Select(path => {
				EnsurePathIsWithinRoot(fullRoot, path, "Oracle manifest path escapes the private fixture root.");
				try {
					return (path, ParseManifest(File.ReadAllText(path)));
				} catch (InvalidDataException exception) {
					throw new InvalidDataException($"Invalid oracle manifest '{path}': {exception.Message}", exception);
				}
			})
			.ToList();
	}

	public static string ResolvePrivateFixturePath(string root, string manifestPath, string relativePath) {
		if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) {
			throw new DirectoryNotFoundException($"Oracle fixture directory was not found: {root}");
		}
		if (string.IsNullOrWhiteSpace(manifestPath)) {
			throw new InvalidDataException("Oracle manifest path cannot be empty.");
		}
		if (string.IsNullOrWhiteSpace(relativePath)) {
			throw new InvalidDataException("Oracle fixture path cannot be empty.");
		}
		if (IsPortableRootedPath(relativePath)) {
			throw new InvalidDataException($"Oracle fixture path must be relative to its manifest: {relativePath}");
		}
		if (!string.Equals(Path.GetExtension(relativePath), ".sav", StringComparison.OrdinalIgnoreCase)) {
			throw new InvalidDataException($"Oracle fixture must be a .sav file: {relativePath}");
		}

		string fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
		string fullManifestPath = Path.GetFullPath(manifestPath);
		EnsurePathIsWithinRoot(fullRoot, fullManifestPath, "Oracle manifest path escapes the private fixture root.");
		if (!File.Exists(fullManifestPath)) {
			throw new FileNotFoundException("Oracle manifest file was not found.", fullManifestPath);
		}
		string manifestDirectory = Path.GetDirectoryName(fullManifestPath) ?? fullRoot;
		string candidate = Path.GetFullPath(Path.Combine(manifestDirectory, relativePath));
		EnsurePathIsWithinRoot(
			fullRoot,
			candidate,
			$"Oracle fixture path escapes {EnvironmentVariable}: {relativePath}"
		);
		if (!File.Exists(candidate)) {
			throw new FileNotFoundException("Oracle SAV file was not found.", candidate);
		}
		RejectSymbolicLinkTraversal(fullRoot, candidate);
		return candidate;
	}

	public static Civ3OracleSnapshot Capture(
		string savePath,
		Civ3OracleManifest manifest,
		Civ3Installation installation
	) {
		ArgumentNullException.ThrowIfNull(manifest);
		ArgumentNullException.ThrowIfNull(installation);

		SaveGame save = ImportCiv3.ImportSav(
			savePath,
			installation.ConquestsBiqPath,
			_ => installation.PediaIconsPath
		);

		SavePlayer player = FindPlayer(save, manifest.PlayerCivilization, manifest.Name);
		int? warWearinessPoints = null;
		bool? atWar = null;
		if (!string.IsNullOrWhiteSpace(manifest.OpponentCivilization)) {
			SavePlayer opponent = FindPlayer(save, manifest.OpponentCivilization, manifest.Name);
			if (!player.playerRelationships.TryGetValue(opponent.id.ToString(), out PlayerRelationship relationship)) {
				throw new InvalidDataException(
					$"Oracle '{manifest.Name}' cannot find the relationship from "
					+ $"{manifest.PlayerCivilization} to {manifest.OpponentCivilization}."
				);
			}
			warWearinessPoints = relationship.warWearinessPoints;
			atWar = relationship.AtWar();
		}

		int? resisterCount = null;
		if (!string.IsNullOrWhiteSpace(manifest.CityName)) {
			List<SaveCity> matchingCities = save.Cities.Where(candidate =>
				candidate.owner == player.id
				&& string.Equals(candidate.name, manifest.CityName, StringComparison.OrdinalIgnoreCase)
			).ToList();
			if (matchingCities.Count != 1) {
				throw new InvalidDataException(
					$"Oracle '{manifest.Name}' expected exactly one city '{manifest.CityName}' "
					+ $"owned by {manifest.PlayerCivilization}, but found {matchingCities.Count}."
				);
			}
			resisterCount = matchingCities[0].residents.Count(resident => resident.isResisting);
		}

		return new Civ3OracleSnapshot {
			Turn = save.TurnNumber,
			WarWearinessPoints = warWearinessPoints,
			AtWar = atWar,
			HasTriggeredGoldenAge = player.hasTriggeredGoldenAge,
			GoldenAgeTurnsRemaining = player.goldenAgeTurnsRemaining,
			ResisterCount = resisterCount,
		};
	}

	public static string FormatObservedContract(
		Civ3OracleManifest manifest,
		Civ3OracleSnapshot before,
		Civ3OracleSnapshot after
	) {
		return JsonSerializer.Serialize(new {
			manifest.Name,
			manifest.BeforeSave,
			manifest.AfterSave,
			manifest.PlayerCivilization,
			manifest.OpponentCivilization,
			manifest.CityName,
			Before = before,
			After = after,
			Delta = new {
				Turn = after.Turn - before.Turn,
				WarWearinessPoints = Difference(before.WarWearinessPoints, after.WarWearinessPoints),
				GoldenAgeTurnsRemaining = after.GoldenAgeTurnsRemaining - before.GoldenAgeTurnsRemaining,
				ResisterCount = Difference(before.ResisterCount, after.ResisterCount),
			},
		}, JsonOptions);
	}

	private static int? Difference(int? before, int? after) {
		return before.HasValue && after.HasValue ? after.Value - before.Value : null;
	}

	private static void ValidatePortableSavePath(string manifestName, string propertyName, string path) {
		if (IsPortableRootedPath(path)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifestName}' property {propertyName} must be a relative path."
			);
		}
		if (!string.Equals(Path.GetExtension(path), ".sav", StringComparison.OrdinalIgnoreCase)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifestName}' property {propertyName} must point to a .sav file."
			);
		}
	}

	private static bool IsPortableRootedPath(string path) {
		return Path.IsPathRooted(path)
			|| path.StartsWith('\\')
			|| path.StartsWith('/')
			|| (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':');
	}

	private static void EnsurePathIsWithinRoot(string fullRoot, string candidate, string message) {
		string relativeToRoot = Path.GetRelativePath(fullRoot, Path.GetFullPath(candidate));
		if (Path.IsPathRooted(relativeToRoot)
			|| relativeToRoot == ".."
			|| relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
			|| relativeToRoot.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)) {
			throw new InvalidDataException(message);
		}
	}

	private static void RejectSymbolicLinkTraversal(string root, string path) {
		string relativeToRoot = Path.GetRelativePath(root, path);
		string current = root;
		foreach (string segment in relativeToRoot.Split(
			new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
			StringSplitOptions.RemoveEmptyEntries
		)) {
			current = Path.Combine(current, segment);
			FileSystemInfo info = Directory.Exists(current)
				? new DirectoryInfo(current)
				: new FileInfo(current);
			if (info.LinkTarget != null) {
				throw new InvalidDataException(
					$"Oracle fixture paths cannot traverse symbolic links: {current}"
				);
			}
		}
	}

	private static bool HasAnyExpectation(Civ3OracleManifest manifest) {
		return SnapshotHasExpectation(manifest.Before)
			|| SnapshotHasExpectation(manifest.After)
			|| manifest.Delta.Turn.HasValue
			|| manifest.Delta.WarWearinessPoints.HasValue
			|| manifest.Delta.GoldenAgeTurnsRemaining.HasValue
			|| manifest.Delta.ResisterCount.HasValue;
	}

	private static bool SnapshotHasExpectation(Civ3OracleExpectedSnapshot snapshot) {
		return snapshot.Turn.HasValue
			|| snapshot.WarWearinessPoints.HasValue
			|| snapshot.AtWar.HasValue
			|| snapshot.HasTriggeredGoldenAge.HasValue
			|| snapshot.GoldenAgeTurnsRemaining.HasValue
			|| snapshot.ResisterCount.HasValue;
	}

	private static SavePlayer FindPlayer(SaveGame save, string civilization, string oracleName) {
		List<SavePlayer> matchingPlayers = save.Players.Where(candidate =>
			string.Equals(candidate.civilization, civilization, StringComparison.OrdinalIgnoreCase)
		).ToList();
		if (matchingPlayers.Count != 1) {
			throw new InvalidDataException(
				$"Oracle '{oracleName}' expected exactly one civilization '{civilization}', "
				+ $"but found {matchingPlayers.Count}."
			);
		}
		return matchingPlayers[0];
	}
}
