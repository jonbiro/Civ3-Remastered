using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

	internal static JsonSerializerOptions JsonOptions { get; } = new() {
		PropertyNameCaseInsensitive = true,
		WriteIndented = true,
	};

	public static string GetRootPath() {
		string configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
		return string.IsNullOrWhiteSpace(configured) ? null : Path.GetFullPath(configured);
	}

	public static bool ShouldSkipOracleTests() {
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

		return !Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories).Any();
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
		return manifest;
	}

	public static IReadOnlyList<(string ManifestPath, Civ3OracleManifest Manifest)> LoadManifests(string root) {
		if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) {
			throw new DirectoryNotFoundException($"Oracle fixture directory was not found: {root}");
		}

		return Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.Select(path => (path, ParseManifest(File.ReadAllText(path))))
			.ToList();
	}

	public static string ResolvePrivateFixturePath(string root, string manifestPath, string relativePath) {
		if (string.IsNullOrWhiteSpace(relativePath)) {
			throw new InvalidDataException("Oracle fixture path cannot be empty.");
		}

		string fullRoot = Path.GetFullPath(root);
		string manifestDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? fullRoot;
		string candidate = Path.GetFullPath(Path.Combine(manifestDirectory, relativePath));
		string relativeToRoot = Path.GetRelativePath(fullRoot, candidate);
		if (Path.IsPathRooted(relativeToRoot)
			|| relativeToRoot == ".."
			|| relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
			|| relativeToRoot.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)) {
			throw new InvalidDataException($"Oracle fixture path escapes {EnvironmentVariable}: {relativePath}");
		}
		if (!File.Exists(candidate)) {
			throw new FileNotFoundException("Oracle SAV file was not found.", candidate);
		}
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
			SaveCity city = save.Cities.SingleOrDefault(candidate =>
				candidate.owner == player.id
				&& string.Equals(candidate.name, manifest.CityName, StringComparison.OrdinalIgnoreCase)
			);
			if (city == null) {
				throw new InvalidDataException(
					$"Oracle '{manifest.Name}' cannot find city '{manifest.CityName}' "
					+ $"owned by {manifest.PlayerCivilization}."
				);
			}
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

	private static SavePlayer FindPlayer(SaveGame save, string civilization, string oracleName) {
		SavePlayer player = save.Players.SingleOrDefault(candidate =>
			string.Equals(candidate.civilization, civilization, StringComparison.OrdinalIgnoreCase)
		);
		if (player == null) {
			throw new InvalidDataException(
				$"Oracle '{oracleName}' cannot find civilization '{civilization}'."
			);
		}
		return player;
	}
}
