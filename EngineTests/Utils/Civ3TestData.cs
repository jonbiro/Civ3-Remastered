using System;
using QueryCiv3;

namespace EngineTests.Utils;

public static class Civ3TestData {
	public static bool ShouldSkipCiv3DependentTests() {
		// GitHub Actions sets CI, and Civ3 is not installed there. Local
		// contributors can also run without Civ3 assets or CIV3_HOME configured.
		if (Environment.GetEnvironmentVariable("CI") != null) {
			return true;
		}

		return Civ3InstallationProbe.TryOpen(Civ3Location.GetCiv3Path()) == null;
	}
}
