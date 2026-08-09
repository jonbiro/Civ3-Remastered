using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace QueryCiv3 {
	public class Civ3Location {
		public static readonly string RegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Infogrames Interactive\Civilization III";

		private static string SteamCommonDir() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
				return Path.Join(programFilesX86, "Steam\\steamapps\\common");
			}
			string home = GetHome();
			return home == null ? null : Path.Combine(home, "Library/Application Support/Steam/steamapps/common");
		}

		private static string GetHome() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		private static bool FolderIsCiv3(DirectoryInfo di) {
			return Civ3InstallationProbe.TryOpen(di.FullName) != null;
		}

		private static string ConvertUnixVarsToWindowsVars(string input) {
			if (string.IsNullOrEmpty(input)) return input;

			// Replace all instances of $VARIABLE with %VARIABLE%
			return Regex.Replace(input, @"\$(\w+)", match => $"%{match.Groups[1].Value}%");
		}

		private static string GetExpandedPath(string path) {
			bool isUnixLike = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			if (isUnixLike && path[0] == '~') path = GetHome() + path.Substring(1);
			if (isUnixLike) path = ConvertUnixVarsToWindowsVars(path);
			path = Environment.ExpandEnvironmentVariables(path);
			path = Path.GetFullPath(path);
			return path;
		}

		/// <summary>
		/// Finds and validates a Civilization III Complete installation from a
		/// user-selected directory. The directory may be the install root itself
		/// or a nearby wrapper directory created by an archive/extractor.
		/// </summary>
		public static Civ3Installation FindCiv3Installation(string selectedPath, int maxDepth = Civ3InstallationProbe.DefaultSearchDepth) {
			if (string.IsNullOrWhiteSpace(selectedPath)) return null;

			string expanded;
			try {
				expanded = GetExpandedPath(selectedPath);
			} catch (Exception) {
				return null;
			}
			return Civ3InstallationProbe.Find(expanded, maxDepth);
		}

		private static string FindRoot(string candidatePath, int maxDepth = 1) {
			Civ3Installation installation = FindCiv3Installation(candidatePath, maxDepth);
			return installation?.RootPath;
		}

		public static string GetCiv3Path() {
			// Use CIV3_HOME env var if present, but only accept it if it actually
			// resolves to a Civ III Complete install. Allow a small nested search so
			// an extracted archive wrapper directory can be supplied directly.
			string path = Environment.GetEnvironmentVariable("CIV3_HOME");
			if (!string.IsNullOrEmpty(path)) {
				string root = FindRoot(path, Civ3InstallationProbe.DefaultSearchDepth);
				if (root != null) return root;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				// Look up in Windows registry if present.
				path = (string)Microsoft.Win32.Registry.GetValue(RegistryKey, "install_path", "");
				if (!string.IsNullOrEmpty(path)) {
					string root = FindRoot(path);
					if (root != null) return root;
				}
			}

			// Check for a Civ III Complete folder in steamapps/common.
			string steam = SteamCommonDir();
			if (!string.IsNullOrEmpty(steam)) {
				DirectoryInfo root = new(steam);
				if (root.Exists) {
					foreach (DirectoryInfo di in root.GetDirectories()) {
						if (FolderIsCiv3(di)) {
							return di.FullName;
						}
					}
				}
			}

			return "/civ3/path/not/found";
		}
	}
}
