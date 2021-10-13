using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ht.replication {
	[BurstCompile]
	public struct BufferUpdatesJob : IJobForEach_BC<ActorUpdate, ReplicationClient> {
		[ReadOnly]
		public int system_tick;
		[ReadOnly]
		public ReplicationConfig config;
		/** potential update filter */
		[ReadOnly]
		public NativeArray<int> filter;
		[ReadOnly]
		public NativeArray<int> actorspawns;
		[ReadOnly]
		public NativeArray<int> actorgenerations;
		[ReadOnly]
		public NativeArray<int> actortypes;
		[ReadOnly]
		public NativeArray<int> datamasks;
		[ReadOnly]
		public NativeArray<int> changemasks;
		[ReadOnly]
		public NativeArray<int> ackdata;

		public void Execute(DynamicBuffer<ActorUpdate> buf, ref ReplicationClient client) {
			for (var i = 0; i < filter.Length; i++) {
				if (filter[i] == 0) continue;

				var created_at = actorspawns[i];
				var acked_tick = ackdata[i * config.maxClients + client.clientId];
				if (acked_tick == 0 && datamasks[i] == 0) {
					// zero data actor.
					continue;
				}

				if (acked_tick == 0) {
					// full update.
					buf.Add(new ActorUpdate {
						id = i, datamask = datamasks[i], type = actortypes[i],
						gen = actorgenerations[i], updatemask = datamasks[i] | 1//important note bit 1
					});
					continue;
				}

				if (created_at > acked_tick) {
					// full update. generation changed.
					buf.Add(new ActorUpdate {
						id = i, datamask = datamasks[i], type = actortypes[i],
						gen = actorgenerations[i], updatemask = datamasks[i] | 1//important note bit 1
					});
					continue;
				}

				var seen_ticks_ago = system_tick - acked_tick;
				if (seen_ticks_ago > config.cacheTicks - 1) {
					// full update.
					buf.Add(new ActorUpdate {
						id = i, datamask = datamasks[i], type = actortypes[i],
						gen = actorgenerations[i], updatemask = datamasks[i] | 1//important note bit 1
					});
					continue;
				}

				// partial update.
				var partial_mask = 0;
				for (var j = 0; j < math.min(config.cacheTicks, seen_ticks_ago); j++) {
					partial_mask |= changemasks[((system_tick - j) & (config.cacheTicks - 1)) * config.maxActors + i];
				}

				buf.Add(new ActorUpdate {
					id = i, datamask = datamasks[i], type = actortypes[i],
					gen = actorgenerations[i], updatemask = partial_mask
				});
			}
		}

	}
}