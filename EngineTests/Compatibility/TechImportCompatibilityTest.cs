using System.Collections.Generic;
using C7GameData;
using C7GameData.Save;
using QueryCiv3.Biq;
using Xunit;

namespace EngineTests.Compatibility;

public class TechImportCompatibilityTest {
	[Fact]
	public void FourthTechnologyPrerequisiteUsesFourthSlot() {
		ID.Factory ids = new();
		List<SaveTech> techs = new();
		for (int i = 0; i < 5; ++i) {
			techs.Add(new SaveTech { id = ids.CreateID($"tech-{i}") });
		}

		TECH source = new() {
			Prerequisite1 = 0,
			Prerequisite2 = 1,
			Prerequisite3 = 2,
			Prerequisite4 = 3,
		};
		SaveTech destination = techs[4];

		ImportCiv3.AddTechPrerequisites(source, destination, techs);

		Assert.Equal(
			new[] { techs[0].id, techs[1].id, techs[2].id, techs[3].id },
			destination.Prerequisites
		);
	}
}
