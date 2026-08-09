using System;
using C7Engine.Simulation;
using Xunit;

namespace EngineTests.Simulation;

public class GameSessionTest {
	[Fact]
	public void SessionRetainsProvidedGameState() {
		C7GameData.GameData gameData = new(customSeed: 12345);

		GameSession session = new(gameData);

		Assert.Same(gameData, session.State);
	}

	[Fact]
	public void SessionRejectsNullGameState() {
		Assert.Throws<ArgumentNullException>(() => new GameSession(null));
	}
}
