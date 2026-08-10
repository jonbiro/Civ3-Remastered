using System;
using C7GameData;

namespace C7Engine.Simulation {
	/// <summary>
	/// Represents the lifetime and state of one running game.
	///
	/// This is intentionally an additive scaffold. EngineStorage remains the active
	/// compatibility facade while systems are migrated incrementally. Keeping the
	/// initial type small lets us introduce an explicit session boundary without
	/// changing current gameplay behavior or random-call ordering.
	/// </summary>
	public sealed class GameSession {
		public GameData State { get; }

		public GameSession(GameData state) {
			State = state ?? throw new ArgumentNullException(nameof(state));
		}
	}
}
