using System;
using C7Engine.Simulation;
using C7GameData;
using Xunit;

namespace EngineTests.Simulation;

public class GameSessionTest {
	[Fact]
	public void SessionRetainsProvidedGameState() {
		GameData gameData = new(customSeed: 12345);

		GameSession session = new(gameData);

		Assert.Same(gameData, session.State);
	}

	[Fact]
	public void SessionRejectsNullGameState() {
		Assert.Throws<ArgumentNullException>(() => new GameSession(null));
	}
}
