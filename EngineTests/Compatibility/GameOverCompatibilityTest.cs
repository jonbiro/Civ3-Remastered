using C7Engine;
using C7GameData;
using Xunit;

namespace EngineTests.Compatibility;

public class GameOverCompatibilityTest {
	[Fact]
	public void GameplayMessagesAreRejectedAfterOutcomeIsRecorded() {
		C7GameData.GameData gameData = new(customSeed: 1234) {
			outcome = new GameOutcome {
				Turn = 42,
				Claims = [new VictoryClaim {
					Type = VictoryType.Conquest,
					PlayerId = ID.None("winner"),
				}],
			},
		};
		EngineStorage.InitializeGameDataForTests(gameData);

		bool mutated = false;
		ActionToEngineMsg message = new(() => mutated = true);

		Assert.False(message.send());
		EngineStorage.ProcessNextMessageToEngine();
		Assert.False(mutated);
	}

	[Fact]
	public void GameplayMessagesStillQueueBeforeGameOver() {
		C7GameData.GameData gameData = new(customSeed: 5678);
		EngineStorage.InitializeGameDataForTests(gameData);

		bool mutated = false;
		ActionToEngineMsg message = new(() => mutated = true);

		Assert.True(message.send());
		EngineStorage.ProcessNextMessageToEngine();
		Assert.True(mutated);
	}

	[Fact]
	public void OutcomeNotificationCarriesPersistedOutcome() {
		GameOutcome outcome = new() {
			Turn = 77,
			Claims = [new VictoryClaim {
					Type = VictoryType.Cultural,
					PlayerId = ID.None("winner"),
				}],
		};

		MsgGameOutcome message = new(outcome);

		Assert.Same(outcome, message.outcome);
	}
}
