using Unity.Entities;

namespace ht.replication {
	public struct Perception : IComponentData {
		public int client;
		public int tick;
	}
}