using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace ht.replication {
	[AlwaysUpdateSystem]
	[DisableAutoCreation]
	public class ActorUpdateSystemV2 : ComponentSystem {

		private ActorSystem _actorSystem;
		private ReplicationAckSystem _ackSystem;
		private ActorChangeSystem _changeSystem;
		private ReplicationConfig _config;
		private EntityQuery _actorQuery;

		protected override void OnCreate() {
			_config = GetSingleton<ReplicationWorld>().config;
			_actorSystem = World.GetOrCreateSystem<ActorSystem>();
			_ackSystem = World.GetOrCreateSystem<ReplicationAckSystem>();
			_changeSystem = World.GetOrCreateSystem<ActorChangeSystem>();
			_actorQuery = GetEntityQuery(typeof(Actor));
		}

		protected override void OnDestroy() {

		}

		public int GetActorUpdateMaskForClient(int client, int actor) {
			return -1; //updatemasks[client * _config.maxActors + actor];
		}

		protected override void OnUpdate() {
			var tick = GetSingleton<ReplicationWorld>().tick;

			Profiler.BeginSample("realloc");
			var datamasks = new NativeArray<int>(_config.maxActors, Allocator.TempJob);
			Profiler.EndSample();

			Profiler.BeginSample("collect-liveactors");
			// collect actor datamasks
			var live_actors = _actorQuery.ToComponentDataArray<Actor>(Allocator.TempJob);
			foreach (var a in live_actors) {
				datamasks[a.id] = a.datamask;
			}
			live_actors.Dispose();
			Profiler.EndSample();

			Profiler.BeginSample("collect-filterindices");
			var filterindices = new NativeArray<int>(_config.maxActors, Allocator.TempJob);
			var derive_filter_indices_job = new DeriveFilterIndicesJob {
				system_tick = tick,
				config = _config,
				datamasks = datamasks,
				lastchangeticks = _changeSystem.lastchangeticks,
				filter = filterindices
			};
			derive_filter_indices_job.Schedule(_config.maxActors, 64).Complete();
			Profiler.EndSample();

			Profiler.BeginSample("buffer-updates");
			new BufferUpdatesJob {
				system_tick = tick,
				config = _config,
				filter = filterindices,
				actorgenerations = _actorSystem.generations,
				actortypes = _actorSystem.types,
				actorspawns = _actorSystem.spawns,
				changemasks = _changeSystem.changemasks,
				ackdata = _ackSystem.ack_data,
				datamasks = datamasks
			}.Run(GetEntityQuery(typeof(ReplicationClient), typeof(ActorUpdate)));//.Schedule(GetEntityQuery(typeof(ReplicationClient), typeof(ActorUpdate))).Complete();
			Profiler.EndSample();

			/*var updatemasks_job = new DeriveUpdateMasksJob {
				system_tick = tick,
				config = _config,
				ack_data = _ackSystem.ack_data,
				changemasks = _changeSystem.changemasks,
				datamasks = datamasks,
				actor_spawns = _actorSystem.spawns,
				updatemasks = updatemasks
			};
			updatemasks_job.Schedule(_config.maxClients, 8).Complete();*/

			filterindices.Dispose();
			datamasks.Dispose();
		}

	}
}