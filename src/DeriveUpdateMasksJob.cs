using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ht.replication {
	[BurstCompile]
	public struct DeriveUpdateMasksJob : IJobParallelFor {
			[ReadOnly] public int system_tick;
			[ReadOnly] public ReplicationConfig config;
			[ReadOnly] public NativeArray<int> ack_data;
			[ReadOnly] public NativeArray<int> datamasks;
			[ReadOnly] public NativeArray<int> actor_spawns;
			[ReadOnly] public NativeArray<int> changemasks;

			[WriteOnly]
			[NativeDisableParallelForRestriction]
			public NativeArray<int> updatemasks;

			public void Execute(int client) {
				for (var actor = 0; actor < config.maxActors; actor++) {
					var created_at = actor_spawns[actor];
					var acked_tick = ack_data[actor * config.maxClients + client];
					if (acked_tick == 0 && datamasks[actor] == 0) {
						// zero data actor.
						continue;
					}
					
					if (acked_tick == 0) {
						// full update.
						updatemasks[client * config.maxActors + actor] = datamasks[actor] | 1;//important note bit 1
						continue;
					}

					if (created_at > acked_tick) {
						// full update. generation changed.
						updatemasks[client * config.maxActors + actor] = datamasks[actor] | 1;//important note bit 1
						continue;
					}

					var seen_ticks_ago = system_tick - acked_tick;
					if (seen_ticks_ago > config.cacheTicks - 1) {
						// full update.
						updatemasks[client * config.maxActors + actor] = datamasks[actor] | 1;//important note bit 1
						continue;
					}

					// partial update.
					var mask = 0;
					for (var i = 0; i < math.min(config.cacheTicks, seen_ticks_ago); i++) {
						mask |= changemasks[((system_tick - i) & (config.cacheTicks - 1)) * config.maxActors + actor];
					}

					updatemasks[client * config.maxActors + actor] = mask;

					/*if (actors[actor].gen > acked_gen) {
						// full update. unseen or actor generation changed.
						updatemasks[client * config.maxActors + actor] = actors[actor].datamask | 1;
						// in the case of actor deletion the datamask will be 0 so we force the gen change flag (ALWAYS ONE)
						// otherwise the replication system will never propagate deletions to clients
						// since it skips actors with an updatemask of 0
						return;
					}

					var seen_ticks_ago = system_tick - acked_tick;
					if (ack == 0L || seen_ticks_ago > config.cacheTicks - 1) {
						// full update. unseen or rollover.
						updatemasks[client * config.maxActors + actor] = actors[actor].datamask;
						return;
					}

					// partial update.
					var mask = 0;
					for (var i = 0; i < math.min(config.cacheTicks, seen_ticks_ago); i++) {
						mask |= changemasks[(system_tick & (config.cacheTicks - 1)) * config.maxActors + actor];
					}

					updatemasks[client * config.maxActors + actor] = mask;*/
				}
			}
		}
}