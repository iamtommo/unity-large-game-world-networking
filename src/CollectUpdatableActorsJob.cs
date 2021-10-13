using Unity.Collections;
using Unity.Jobs;

namespace ht.replication {
	/** collect all actors which have a datamask and/or have a changemask within cacheticks */
	public struct CollectUpdatableActorsJob : IJobParallelFor {
		[ReadOnly]
		public int system_tick;
		[ReadOnly]
		public ReplicationConfig config;
		[ReadOnly]
		public NativeArray<int> datamasks;
		[ReadOnly]
		public NativeArray<int> changemasks;

		[WriteOnly]
		public NativeList<int> updatables;

		public void Execute(int actor) {
			if (datamasks[actor] != 0) {
				updatables.Add(actor);
				return;
			}
			
			for (var i = 0; i < config.cacheTicks; i++) {
				if (changemasks[((system_tick - i) & (config.cacheTicks - 1)) * config.maxActors + actor] != 0) {
					updatables.Add(actor);
					return;
				}
			}
		}
	}
}