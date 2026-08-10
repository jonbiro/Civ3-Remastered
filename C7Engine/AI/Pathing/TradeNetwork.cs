using System.Collections.Generic;
using System.Linq;
using System;
using C7GameData;

namespace C7Engine.Pathing {
	public class TradeNetworkSegment {
		public Dictionary<Resource, int> resourceCounts = new();
		public HashSet<Tile> tiles = new();

		public void AddTile(Tile t, Player p) {
			tiles.Add(t);
			if (t.Resource != Resource.NONE && t.OwningPlayer() == p) {
				if (resourceCounts.TryGetValue(t.Resource, out int currentCount)) {
					resourceCounts[t.Resource] = currentCount + 1;
				} else {
					resourceCounts[t.Resource] = 1;
				}
			}
		}

		public void MergeFrom(TradeNetworkSegment other) {
			tiles.UnionWith(other.tiles);
			foreach ((Resource resource, int count) in other.resourceCounts) {
				resourceCounts.TryGetValue(resource, out int existing);
				resourceCounts[resource] = existing + count;
			}
		}
	}

	// A class for calculating the trade networks.
	//
	// The main idea is that calculating the entire empire's trade network once
	// is faster than doing it repeatedly for each city. Land segments are found
	// by road/rail flood fill, then qualifying harbor and airport connections
	// merge those segments into the final network.
	public class TradeNetwork {
		private readonly GameData gameData;
		private Dictionary<Player, Dictionary<City, TradeNetworkSegment>> segments = new();

		public TradeNetwork(GameData gameData) {
			this.gameData = gameData;
			foreach (Player p in gameData.players) {
				ComputeTradeNetwork(p);
			}
		}

		private static bool CanUseLandTradeTile(Player player, Tile tile) {
			Player tileOwner = tile.OwningPlayer();
			return tileOwner == null || tileOwner == player || player.IsAtPeaceWith(tileOwner);
		}

		private static bool CityAllowsWaterTrade(City city) {
			return city.GetBuildings().Any(cb => cb.building.allowsWaterTrade);
		}

		private static bool CityAllowsAirTrade(City city) {
			return city.GetBuildings().Any(cb => cb.building.allowsAirTrade);
		}

		private bool PlayerHasTradeTech(Player player, Func<Tech, bool> predicate) {
			return gameData.techs.Any(tech => predicate(tech) && player.knownTechs.Contains(tech.id));
		}

		private bool HasEnemyNavalBlockader(Player player, Tile tile) {
			return tile.unitsOnTile.Any(unit =>
				unit.owner != null
				&& unit.owner != player
				&& unit.IsWaterUnit()
				&& !player.IsAtPeaceWith(unit.owner)
			);
		}

		private bool CanUseWaterTradeTile(Player player, Tile tile) {
			if (tile == Tile.NONE || !tile.IsWater() || !player.HasExploredTile(tile)) {
				return false;
			}
			if (HasEnemyNavalBlockader(player, tile)) {
				return false;
			}

			return tile.baseTerrainType.Key switch {
				"coast" => true,
				"sea" => PlayerHasTradeTech(player, tech => tech.EnablesTradeOverSea),
				"ocean" => PlayerHasTradeTech(player, tech => tech.EnablesTradeOverOcean),
				_ => false,
			};
		}

		private void MergeSegments(Player player, TradeNetworkSegment target, TradeNetworkSegment source) {
			if (target == source) {
				return;
			}

			target.MergeFrom(source);
			Dictionary<City, TradeNetworkSegment> playerSegments = segments[player];
			foreach (City city in playerSegments.Where(kv => kv.Value == source).Select(kv => kv.Key).ToList()) {
				playerSegments[city] = target;
			}
		}

		private void ConnectAirTrade(Player player) {
			List<City> airports = player.cities.Where(CityAllowsAirTrade).ToList();
			if (airports.Count < 2) {
				return;
			}

			TradeNetworkSegment target = segments[player][airports[0]];
			foreach (City airport in airports.Skip(1)) {
				MergeSegments(player, target, segments[player][airport]);
			}
		}

		private int FloodWaterComponent(Player player, Tile start, int componentId, Dictionary<Tile, int> componentByTile) {
			Queue<Tile> toCheck = new();
			toCheck.Enqueue(start);
			componentByTile[start] = componentId;

			while (toCheck.Count > 0) {
				Tile tile = toCheck.Dequeue();
				foreach (Tile neighbor in tile.neighbors.Values) {
					if (componentByTile.ContainsKey(neighbor) || !CanUseWaterTradeTile(player, neighbor)) {
						continue;
					}
					componentByTile[neighbor] = componentId;
					toCheck.Enqueue(neighbor);
				}
			}
			return componentId;
		}

		private void ConnectWaterTrade(Player player) {
			List<City> harbors = player.cities.Where(CityAllowsWaterTrade).ToList();
			if (harbors.Count < 2) {
				return;
			}

			Dictionary<Tile, int> componentByTile = new();
			Dictionary<int, HashSet<City>> harborsByComponent = new();
			int nextComponentId = 0;

			foreach (City harbor in harbors) {
				HashSet<int> adjacentComponents = new();
				foreach (Tile neighbor in harbor.location.neighbors.Values) {
					if (!CanUseWaterTradeTile(player, neighbor)) {
						continue;
					}

					if (!componentByTile.TryGetValue(neighbor, out int componentId)) {
						componentId = FloodWaterComponent(player, neighbor, nextComponentId++, componentByTile);
					}
					adjacentComponents.Add(componentId);
				}

				foreach (int componentId in adjacentComponents) {
					if (!harborsByComponent.TryGetValue(componentId, out HashSet<City> connectedHarbors)) {
						connectedHarbors = new();
						harborsByComponent[componentId] = connectedHarbors;
					}
					connectedHarbors.Add(harbor);
				}
			}

			foreach (HashSet<City> connectedHarbors in harborsByComponent.Values) {
				if (connectedHarbors.Count < 2) {
					continue;
				}
				City first = connectedHarbors.First();
				TradeNetworkSegment target = segments[player][first];
				foreach (City harbor in connectedHarbors.Skip(1)) {
					MergeSegments(player, target, segments[player][harbor]);
				}
			}
		}

		private void ComputeTradeNetwork(Player player) {
			HashSet<Tile> seen = new();

			segments[player] = new();
			foreach (City c in player.cities) {
				if (segments[player].ContainsKey(c)) {
					continue;
				}

				TradeNetworkSegment segment = new();
				segments[player][c] = segment;

				Queue<Tile> toCheck = new();
				toCheck.Enqueue(c.location);
				seen.Add(c.location);

				while (toCheck.Count > 0) {
					Tile x = toCheck.Dequeue();
					segment.AddTile(x, player);

					if (x.cityAtTile != null && x.cityAtTile.owner == player) {
						segments[player][x.cityAtTile] = segment;
					}

					foreach (Tile n in x.neighbors.Values) {
						if (n.IsRoaded() && CanUseLandTradeTile(player, n) && seen.Add(n)) {
							toCheck.Enqueue(n);
						}
					}
				}
			}

			ConnectWaterTrade(player);
			ConnectAirTrade(player);
		}

		public Dictionary<Resource, int> GetResourcesAvailableToCity(Player player, City city) {
			TradeNetworkSegment segment = segments[player][city];
			Dictionary<Resource, int> result = new();
			foreach ((Resource r, int count) in segment.resourceCounts) {
				if (player.KnowsAboutResource(r)) {
					result[r] = count;
				}
			}
			return result;
		}

		public bool HasTradeAccess(Tile t, Player p, Resource r) {
			if (!p.KnowsAboutResource(r)) {
				return false;
			}

			foreach (TradeNetworkSegment segment in segments[p].Values.Distinct()) {
				if (segment.tiles.Contains(t) && segment.resourceCounts.TryGetValue(r, out int count) && count > 0) {
					return true;
				}
			}
			return false;
		}

		public bool ConnectedToCapital(Player p, City c) {
			if (!segments.TryGetValue(p, out Dictionary<City, TradeNetworkSegment> playerSegments)
				|| !playerSegments.TryGetValue(c, out TradeNetworkSegment citySegment)) {
				return false;
			}

			City capital = p.cities.FirstOrDefault(city => city.IsCapital());
			if (capital == null || !playerSegments.TryGetValue(capital, out TradeNetworkSegment capitalSegment)) {
				return false;
			}

			return citySegment == capitalSegment;
		}
	}
}
