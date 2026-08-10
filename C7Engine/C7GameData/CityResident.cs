namespace C7GameData {
	public class CityResident {
		public CitizenType citizenType;

		// Only relevant if citizenType.IsDefaultCitizen == true
		public Tile tileWorked = Tile.NONE;
		public Civilization nationality;
		public City city;

		// Resisters in captured cities do not work and suppress the normal
		// city-size defensive bonus until resistance is quelled.
		public bool isResisting;

		// Only relevant if citizenType.IsDefaultCitizen == true
		public enum Mood {
			Happy,
			Content,
			Unhappy
		};
		public Mood mood;
	}
}
