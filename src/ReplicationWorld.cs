using Unity.Entities;

namespace ht.replication {
	public struct ReplicationWorld : IComponentData {
		public int tick;
		public ReplicationConfig config;
	}
}