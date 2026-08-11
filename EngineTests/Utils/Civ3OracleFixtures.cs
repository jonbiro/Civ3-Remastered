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
		PropertyNameCaseInsensitive = true,
		UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
		WriteIndented = true,
	};

	public static string GetRootPath() {
		string configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
		return string.IsNullOrWhiteSpace(configured) ? null : Path.GetFullPath(configured);
	}

	public static bool ShouldSkipOracleTests() {
		// Public CI must never receive or inspect publisher-owned save files.
		if (Environment.GetEnvironmentVariable("CI") != null) {
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
		Civ3OracleManifest manifest = JsonSerializer.Deserialize<Civ3OracleManifest>(json, JsonOptions);
		if (manifest == null || string.IsNullOrWhiteSpace(manifest.Name)) {
			throw new InvalidDataException("Oracle manifest must contain a non-empty name.");
		}
		if (string.IsNullOrWhiteSpace(manifest.BeforeSave) || string.IsNullOrWhiteSpace(manifest.AfterSave)) {
			throw new InvalidDataException($"Oracle manifest '{manifest.Name}' must name beforeSave and afterSave files.");
		}
		if (string.IsNullOrWhiteSpace(manifest.PlayerCivilization)) {
			throw new InvalidDataException($"Oracle manifest '{manifest.Name}' must name playerCivilization.");
		}

		// Explicit JSON nulls must not disable validation or cause a later null
		// reference while an opt-in private test is running.
		manifest.Before ??= new Civ3OracleExpectedSnapshot();
		manifest.After ??= new Civ3OracleExpectedSnapshot();
		manifest.Delta ??= new Civ3OracleExpectedDelta();

		if (!HasExpectedSignal(manifest.Before)
			&& !HasExpectedSignal(manifest.After)
			&& !HasExpectedSignal(manifest.Delta)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifest.Name}' must assert at least one before, after, or delta signal."
			);
		}

		bool requiresOpponent = manifest.Before.WarWearinessPoints.HasValue
			|| manifest.Before.AtWar.HasValue
			|| manifest.After.WarWearinessPoints.HasValue
			|| manifest.After.AtWar.HasValue
			|| manifest.Delta.WarWearinessPoints.HasValue;
		if (requiresOpponent && string.IsNullOrWhiteSpace(manifest.OpponentCivilization)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifest.Name}' must name opponentCivilization for war-state signals."
			);
		}

		bool requiresCity = manifest.Before.ResisterCount.HasValue
			|| manifest.After.ResisterCount.HasValue
			|| manifest.Delta.ResisterCount.HasValue;
		if (requiresCity && string.IsNullOrWhiteSpace(manifest.CityName)) {
			throw new InvalidDataException(
				$"Oracle manifest '{manifest.Name}' must name cityName for resistance signals."
			);
n		}

		return manifest;
	}

	public static IReadOnlyList<(string ManifestPath, Civ3OracleManifest Manifest)> LoadManifests(string root) {
		if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) {
			throw new DirectoryNotFoundException($"Oracle fixture directory was not found: {root}");
		}

		return Directory.EnumerateFiles(root, ManifestSearchPattern, SearchOption.AllDirectories)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.Select(path => {
				try {
					return (path, ParseManifest(File.ReadAllText(path)));
				}
				catch (Exception exception) when (exception is JsonException or InvalidDataException) {
					throw new InvalidDataException($"Oracle manifest is invalid: {path}", exception);
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
		if (Path.IsPathRooted(relativePath)) {
			throw new InvalidDataException($"Oracle fixture path must be relative: {relativePath}");
		}

		string fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
		string fullManifestPath = Path.GetFullPath(manifestPath);
		EnsurePathWithinRoot(fullRoot, fullManifestPath, "Oracle manifest path escapes the private fixture root");
		if (!File.Exists(fullManifestPath)) {
			throw new FileNotFoundException("Oracle manifest file was not found.", fullManifestPath);
		}

		string manifestDirectory = Path.GetDirectoryName(fullManifestPath) ?? fullRoot;
		string candidate = Path.GetFullPath(Path.Combine(manifestDirectory, relativePath));
		EnsurePathWithinRoot(fullRoot, candidate, $"Oracle fixture path escapes {EnvironmentVariable}");
		if (!string.Equals(Path.GetExtension(candidate), ".sav", StringComparison.OrdinalIgnoreCase)) {
			throw new InvalidDataException($"Oracle fixture must be a Civilization III .sav file: {relativePath}");
		}
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
			SaveCity city = FindCity(save, player, manifest.CityName, manifest.Name);
			resisterCount = city.residents.Count(resident => resident.isResisting);
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

	private static bool HasExpectedSignal(Civ3OracleExpectedSnapshot snapshot) {
		return snapshot.Turn.HasValue
			|| snapshot.WarWearinessPoints.HasValue
			|| snapshot.AtWar.HasValue
			|| snapshot.HasTriggeredGoldenAge.HasValue
			|| snapshot.GoldenAgeTurnsRemaining.HasValue
			|| snapshot.ResisterCount.HasValue;
	}

	private static bool HasExpectedSignal(Civ3OracleExpectedDelta delta) {
		return delta.Turn.HasValue
			|| delta.WarWearinessPoints.HasValue
			|| delta.GoldenAgeTurnsRemaining.HasValue
			|| delta.ResisterCount.HasValue;
	}

	private static void EnsurePathWithinRoot(string root, string path, string message) {
		string relativeToRoot = Path.GetRelativePath(root, path);
		if (Path.IsPathRooted(relativeToRoot)
			|| relativeToRoot == ".."
			|| relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
			|| relativeToRoot.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)) {
			throw new InvalidDataException($"{message}: {path}");
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

	private static SavePlayer FindPlayer(SaveGame save, string civilization, string oracleName) {
		List<SavePlayer> matches = save.Players.Where(candidate =>
			string.Equals(candidate.civilization, civilization, StringComparison.OrdinalIgnoreCase)
		).ToList();
		if (matches.Count != 1) {
			throw new InvalidDataException(
				$"Oracle '{oracleName}' expected exactly one civilization named '{civilization}', "
				+ $"but found {matches.Count}."
			);
		}
		return matches[0];
	}

	private static SaveCity FindCity(SaveGame save, SavePlayer player, string cityName, string oracleName) {
		List<SaveCity> matches = save.Cities.Where(candidate =>
			candidate.owner == player.id
			&& string.Equals(candidate.name, cityName, StringComparison.OrdinalIgnoreCase)
		).ToList();
		if (matches.Count != 1) {
			throw new InvalidDataException(
				$"Oracle '{oracleName}' expected exactly one city named '{cityName}' owned by "
				+ $"{player.civilization}, but found {matches.Count}."
			);
		}
		return matches[0];
	}
}
