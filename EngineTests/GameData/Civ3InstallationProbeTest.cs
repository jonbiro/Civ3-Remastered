using System;
using System.IO;
using QueryCiv3;
using Xunit;

namespace EngineTests.GameData;

public class Civ3InstallationProbeTest {
	private static string MakeTempDirectory() {
		string path = Path.Combine(Path.GetTempPath(), $"civ3-install-probe-{Guid.NewGuid():N}");
		Directory.CreateDirectory(path);
		return path;
	}

	private static void CreateFile(string root, string relativePath) {
		string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, "fixture");
	}

	private static void MakeCompleteInstall(string root) {
		CreateFile(root, "Conquests/conquests.biq");
		CreateFile(root, "Conquests/Text/PediaIcons.txt");
		CreateFile(root, "Conquests/Art/buttonsFINAL.pcx");
	}

	[Fact]
	public void TryOpenAcceptsCompleteInstallRoot() {
		string root = MakeTempDirectory();
		try {
			MakeCompleteInstall(root);

			Civ3Installation installation = Civ3InstallationProbe.TryOpen(root);

			Assert.NotNull(installation);
			Assert.Equal(Path.GetFullPath(root), installation.RootPath);
			Assert.True(installation.ConquestsBiqPath.EndsWith("conquests.biq", StringComparison.OrdinalIgnoreCase));
			Assert.True(installation.PediaIconsPath.EndsWith("PediaIcons.txt", StringComparison.OrdinalIgnoreCase));
			Assert.True(installation.ButtonsPath.EndsWith("buttonsFINAL.pcx", StringComparison.OrdinalIgnoreCase));
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void FindLocatesInstallInsideArchiveWrapper() {
		string root = MakeTempDirectory();
		try {
			string installRoot = Path.Combine(root, "Extracted Media", "Sid Meier's Civilization III Complete");
			Directory.CreateDirectory(installRoot);
			MakeCompleteInstall(installRoot);

			Civ3Installation installation = Civ3InstallationProbe.Find(root);

			Assert.NotNull(installation);
			Assert.Equal(Path.GetFullPath(installRoot), installation.RootPath);
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void TryOpenRejectsPartialInstall() {
		string root = MakeTempDirectory();
		try {
			CreateFile(root, "Conquests/conquests.biq");
			CreateFile(root, "Conquests/Text/PediaIcons.txt");

			Assert.Null(Civ3InstallationProbe.TryOpen(root));
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void TryOpenResolvesWindowsAuthoredCaseInsensitively() {
		string root = MakeTempDirectory();
		try {
			CreateFile(root, "conQUESTS/CONQUESTS.BIQ");
			CreateFile(root, "conQUESTS/text/pediaicons.TXT");
			CreateFile(root, "conQUESTS/art/BUTTONSfinal.PCX");

			Civ3Installation installation = Civ3InstallationProbe.TryOpen(root);

			Assert.NotNull(installation);
			Assert.True(File.Exists(installation.ConquestsBiqPath));
			Assert.True(File.Exists(installation.PediaIconsPath));
			Assert.True(File.Exists(installation.ButtonsPath));
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void FindRespectsMaximumSearchDepth() {
		string root = MakeTempDirectory();
		try {
			string installRoot = Path.Combine(root, "one", "two", "three", "four");
			Directory.CreateDirectory(installRoot);
			MakeCompleteInstall(installRoot);

			Assert.Null(Civ3InstallationProbe.Find(root, maxDepth: 3));
			Assert.NotNull(Civ3InstallationProbe.Find(root, maxDepth: 4));
		} finally {
			Directory.Delete(root, recursive: true);
		}
	}
}
