using Unity.Entities;

namespace ht.replication {
	public struct ReplicationClient : IComponentData {
		public int clientId;
		public int latestAckTick;
	}
}