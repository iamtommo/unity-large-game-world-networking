using Unity.Collections;
using Unity.Entities;

namespace ht.replication {
	[DisableAutoCreation]
	[AlwaysUpdateSystem]
	public class ActorChangeSystem : ComponentSystem {
		/**
		 * change bit masks ring buffer
		 * structure: cacheticks * actors
		 */
		public NativeArray<int> changemasks;

		/**
		 * last change tick per actor
		 */
		public NativeArray<int> lastchangeticks;

		/** ring buffer head */
		public int _head;

		private ReplicationConfig _config;

		protected override void OnCreate() {
			_config = GetSingleton<ReplicationWorld>().config;
			changemasks = new NativeArray<int>(_config.cacheTicks * _config.maxActors, Allocator.Persistent);
			lastchangeticks = new NativeArray<int>(_config.maxActors, Allocator.Persistent);
		}

		protected override void OnUpdate() {
			var replication_world = GetSingleton<ReplicationWorld>();
			_head++;

			// clear
			for (var i = 0; i < _config.maxActors; i++) {
				changemasks[index_into(i)] = 0;
			}

			// gather
			Entities.ForEach((ref Actor actor) => {
				changemasks[index_into(actor.id)] = actor.changemask;
				if (actor.changemask != 0) {
					lastchangeticks[actor.id] = replication_world.tick;
				}
				actor.changemask = 0;
			});
		}

		public int GetChangemask(int actor, int ticks_back) {
			return changemasks[wrap(_head - ticks_back) * _config.maxActors + actor];
		}

		public void SetChangemask(int actor, int mask) {
			changemasks[index_into(actor)] = mask;
		}

		public int index_into(int actor) {
			return wrap(_head) * _config.maxActors + actor;
		}

		/** wrap head index around ring buffer length */
		public int wrap(int idx) {
			return idx & (_config.cacheTicks - 1);
		}

		protected override void OnDestroy() {
			changemasks.Dispose();
			lastchangeticks.Dispose();
		}
	}
}