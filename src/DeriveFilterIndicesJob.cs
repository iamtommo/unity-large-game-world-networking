using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace ht.replication {
	/** collect all actors which have a datamask and/or have a changemask within cacheticks */
	[BurstCompile]
	public struct DeriveFilterIndicesJob : IJobParallelFor {
		[ReadOnly]
		public int system_tick;
		[ReadOnly]
		public ReplicationConfig config;
		[ReadOnly]
		public NativeArray<int> datamasks;
		[ReadOnly]
		public NativeArray<int> lastchangeticks;

		[WriteOnly]
		public NativeArray<int> filter;

		public void Execute(int actor) {
			if (datamasks[actor] != 0 || lastchangeticks[actor] > system_tick - config.cacheTicks) {
				filter[actor] = 1;
			}
		}
	}
}