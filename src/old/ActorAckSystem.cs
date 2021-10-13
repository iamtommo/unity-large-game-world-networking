using Unity.Collections;
using Unity.Entities;

namespace ht.replication {
	/**
	 * Maintains which actors we know for certain that a client has acknowledged (seen).
	 */
	[DisableAutoCreation]
	public class ActorAckSystem : ComponentSystem {
		/**
		 * ack data
		 * len = actors * clients
		 * cell = tick
		 */
		public NativeArray<int> ack_data;

		/**
		 * len = actors * clients
		 * cell = tick where data was sent
		 */
		public NativeArray<int> sent_data;

		private ReplicationConfig _config;

		protected override void OnCreate() {
			_config = GetSingleton<ReplicationWorld>().config;
			ack_data = new NativeArray<int>(_config.maxActors * _config.maxClients, Allocator.Persistent);
			sent_data = new NativeArray<int>(_config.maxActors * _config.maxClients, Allocator.Persistent);
		}

		protected override void OnDestroy() {
			sent_data.Dispose();
			ack_data.Dispose();
		}

		public void SetActorSent(int client, int actor, int tick) {
			sent_data[actor * _config.maxClients + client] = tick;
		}

		public int GetClientActorLastSeenTick(int client, int actor) {
			return ack_data[actor * _config.maxClients + client];
		}

		public void ClearAckData(int client) {
			for (var i = 0; i < _config.maxActors; i++) {

			}
		}

		private void Ack(int system_tick, int client, int observed_tick) {
			if (system_tick - observed_tick >= _config.cacheTicks - 1) {
				return;
			}
			for (var actor = 0; actor < _config.maxActors; actor++) {
				var prev_seen_tick = GetClientActorLastSeenTick(client, actor);
				if (prev_seen_tick > observed_tick) {
					// skip if we've seen a more recent tick already
					continue;
				}

				var tick_sent = sent_data[actor * _config.maxClients + client];
				if (tick_sent == 0) {
					// skip if we didn't send any data at this tick
					continue;
				}
				if (observed_tick >= tick_sent) {
					ack_data[actor * _config.maxClients + client] = observed_tick;
				}
			}
		}

		/** should only be used for tests */
		public void ForceAck(int client, int actor, int tick) {
			ack_data[actor * _config.maxClients + client] = tick;
		}

		protected override void OnUpdate() {
			var tick = GetSingleton<ReplicationWorld>().tick;
			Entities.ForEach((Entity e, ref Perception perception) => {
				Ack(tick, perception.client, perception.tick);
				PostUpdateCommands.DestroyEntity(e);
			});
		}
	}
}