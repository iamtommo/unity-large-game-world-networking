using Unity.Entities;

namespace ht.replication {
	public struct ReplicationConfig : IComponentData {
		public int maxActors;
		public int maxClients;
		public int history_bufsz;
		public int cacheTicks;
	}
}