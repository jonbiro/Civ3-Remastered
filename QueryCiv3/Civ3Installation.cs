using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QueryCiv3 {
	/// <summary>
	/// A validated Civilization III Complete installation.
	///
	/// Civ3 Remastered deliberately stores only paths to the user's original files.
	/// The original assets themselves remain outside this repository.
	/// </summary>
	public sealed class Civ3Installation {
		public string RootPath { get; }
		public string ConquestsBiqPath { get; }
		public string PediaIconsPath { get; }
		public string ButtonsPath { get; }

		internal Civ3Installation(string rootPath, string conquestsBiqPath, string pediaIconsPath, string buttonsPath) {
			RootPath = rootPath;
			ConquestsBiqPath = conquestsBiqPath;
			PediaIconsPath = pediaIconsPath;
			ButtonsPath = buttonsPath;
		}
	}

	/// <summary>
	/// Detects and validates Civilization III Complete installations without
	/// depending on Godot or any platform-specific UI.
	/// </summary>
	public static class Civ3InstallationProbe {
		public const int DefaultSearchDepth = 3;

		/// <summary>
		/// Validates a directory as the root of a Civilization III Complete install.
		/// Paths are resolved case-insensitively so Windows-authored game data works
		/// correctly on case-sensitive macOS and Linux filesystems.
		/// </summary>
		public static Civ3Installation TryOpen(string rootPath) {
			if (string.IsNullOrWhiteSpace(rootPath)) return null;

			string fullRoot;
			try {
				fullRoot = Path.GetFullPath(rootPath);
			} catch (Exception) {
				return null;
			}

			if (!Directory.Exists(fullRoot)) return null;

			string conquestsBiq = ResolveRelativePath(fullRoot, "Conquests/conquests.biq");
			if (conquestsBiq == null) return null;

			string pediaIcons = ResolveFirst(fullRoot,
				"Conquests/Text/PediaIcons.txt",
				"Text/PediaIcons.txt");
			if (pediaIcons == null) return null;

			string buttons = ResolveFirst(fullRoot,
				"Conquests/Art/buttonsFINAL.pcx",
				"Art/buttonsFINAL.pcx");
			if (buttons == null) return null;

			return new Civ3Installation(fullRoot, conquestsBiq, pediaIcons, buttons);
		}

		/// <summary>
		/// Finds a valid install at the selected directory or in a nearby nested
		/// directory. This is useful for extracted archives that contain an extra
		/// wrapper folder around the actual Civilization III Complete root.
		/// </summary>
		public static Civ3Installation Find(string selectedPath, int maxDepth = DefaultSearchDepth) {
			if (string.IsNullOrWhiteSpace(selectedPath) || maxDepth < 0) return null;

			Civ3Installation direct = TryOpen(selectedPath);
			if (direct != null) return direct;

			string root;
			try {
				root = Path.GetFullPath(selectedPath);
			} catch (Exception) {
				return null;
			}
			if (!Directory.Exists(root)) return null;

			Queue<(string path, int depth)> pending = new();
			pending.Enqueue((root, 0));

			while (pending.Count > 0) {
				(string current, int depth) = pending.Dequeue();
				if (depth >= maxDepth) continue;

				IEnumerable<string> children;
				try {
					children = Directory.EnumerateDirectories(current)
						.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
						.ToArray();
				} catch (Exception) {
					continue;
				}

				foreach (string child in children) {
					Civ3Installation install = TryOpen(child);
					if (install != null) return install;
					pending.Enqueue((child, depth + 1));
				}
			}

			return null;
		}

		private static string ResolveFirst(string root, params string[] relativePaths) {
			foreach (string relativePath in relativePaths) {
				string result = ResolveRelativePath(root, relativePath);
				if (result != null) return result;
			}
			return null;
		}

		/// <summary>
		/// Resolves a relative file path component-by-component ignoring case.
		/// This avoids relying on the host filesystem's case-sensitivity rules.
		/// </summary>
		internal static string ResolveRelativePath(string root, string relativePath) {
			if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relativePath)) return null;
			if (!Directory.Exists(root)) return null;

			string current = Path.GetFullPath(root);
			string normalized = relativePath.Replace('\\', '/').Trim('/');
			string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < parts.Length; i++) {
				bool isLast = i == parts.Length - 1;
				IEnumerable<string> entries;
				try {
					entries = isLast
						? Directory.EnumerateFiles(current)
						: Directory.EnumerateDirectories(current);
				} catch (Exception) {
					return null;
				}

				string wanted = parts[i];
				string match = entries.FirstOrDefault(entry =>
					string.Equals(Path.GetFileName(entry), wanted, StringComparison.OrdinalIgnoreCase));
				if (match == null) return null;
				current = match;
			}

			return File.Exists(current) ? current : null;
		}
	}
}
