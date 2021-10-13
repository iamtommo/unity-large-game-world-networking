using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace ht.replication {
	/**
	 * Depends on perception&diff systems to produce actor update masks for each actor per client.
	 */
	[DisableAutoCreation]
	public class ActorUpdateMaskSystem : ComponentSystem {

		private PerceptionSystem _perceptionSystem;
		private ActorHistorySystem _diffmaskSystem;
		private ReplicationConfig _config;

		public NativeArray<int> updatemasks;
		public IDenseWorld denseWorld;

		protected override void OnCreate() {
			_config = GetSingleton<ReplicationConfig>();
			_perceptionSystem = World.GetExistingSystem<PerceptionSystem>();
			_diffmaskSystem = World.GetExistingSystem<ActorHistorySystem>();
			updatemasks = new NativeArray<int>(_config.maxClients * _config.maxActors, Allocator.Persistent);
		}

		protected override void OnDestroy() {
			updatemasks.Dispose();
		}

		public int GetActorUpdateMaskForClient(int client, int actor) {
			return updatemasks[client * _config.maxActors + actor];
		}

		protected override void OnUpdate() {
			var tick = GetSingleton<ReplicationWorld>().tick;

			// Clear update masks
			new MemsetNativeArray<int>() {
				Source = updatemasks
			}.Schedule(updatemasks.Length, 128).Complete();

			var updatemasks_job = new DeriveUpdateMasksJob {
				system_tick = tick,
				config = _config,
				perception_data = _perceptionSystem._pdata,
				diffmasks = _diffmaskSystem.diffmasks,
				actor_datamasks = denseWorld.GetActorDatamasks(),
				actor_generations = denseWorld.GetActorGenerations(),
				updatemasks = updatemasks
			}.Schedule(_config.maxClients, 8);
			updatemasks_job.Complete();
		}

		private struct DeriveUpdateMasksJob : IJobParallelFor {
			[ReadOnly] public int system_tick;
			[ReadOnly] public ReplicationConfig config;
			[ReadOnly] public NativeArray<long> perception_data;
			[ReadOnly] public NativeArray<int> actor_datamasks;
			[ReadOnly] public NativeArray<int> actor_generations;
			[ReadOnly] public NativeArray<int> diffmasks;

			[WriteOnly]
			[NativeDisableParallelForRestriction]
			public NativeArray<int> updatemasks;

			public void Execute(int client_index) {
				for (var actor = 0; actor < config.maxActors; actor++) {
					var perception = perception_data[actor * config.maxClients + client_index];
					var seen_actor_tick = perception & 0xFFFFFFFF;
					var seen_actor_gen = (perception >> 32) & 0xFFFFFFFF;
					if (actor_generations[actor] > seen_actor_gen) {
						// full update. unseen or actor generation changed.
						updatemasks[client_index * config.maxActors + actor] = actor_datamasks[actor] | 1;
						// in the case of actor deletion the datamask will be 0 so we force the gen change flag (ALWAYS ONE)
						// otherwise the replication system will never propagate deletions to clients
						// since it skips actors with an updatemask of 0
						return;
					}

					var tickd = system_tick - seen_actor_tick;
					if (seen_actor_tick > 0 & tickd < config.history_bufsz - 1) {
						// partial update.
						var mask = 0;
						for (var i = 0; i < math.min(config.history_bufsz - 1, tickd); i++) {
							mask |= diffmasks[i * config.maxActors + actor];
						}

						updatemasks[client_index * config.maxActors + actor] = mask;
						return;
					}

					updatemasks[client_index * config.maxActors + actor] = actor_datamasks[actor];
				}
			}
		}

	}
}