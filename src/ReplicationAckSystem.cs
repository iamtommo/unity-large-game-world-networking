using Unity.Collections;
using Unity.Entities;

namespace ht.replication {
	/**
	 * Maintains which actors we know for certain that a client has acknowledged (seen).
	 */
	[DisableAutoCreation]
	public class ReplicationAckSystem : ComponentSystem {
		/**
		 * ack data
		 * len = actors * clients
		 * cell = tick
		 */
		public NativeArray<int> ack_data;

		/**
		 * ringbuffer
		 * head (tick) ->
		 *     client -> [128] actors sent
		 */
		public NativeArray<int> sent_data;

		public int head;

		private ReplicationConfig _config;

		protected override void OnCreate() {
			_config = GetSingleton<ReplicationWorld>().config;
			ack_data = new NativeArray<int>(_config.maxActors * _config.maxClients, Allocator.Persistent);
			sent_data = new NativeArray<int>(_config.cacheTicks * _config.maxClients * 128, Allocator.Persistent);
		}

		protected override void OnDestroy() {
			sent_data.Dispose();
			ack_data.Dispose();
		}

		public void SetActorsSent(int client, NativeArray<int> actors) {
			NativeArray<int>.Copy(actors, 0, sent_data, index_into(client), actors.Length);
		}

		public int GetClientActorLastSeenTick(int client, int actor) {
			return ack_data[actor * _config.maxClients + client];
		}

		public void Ack(int client, int system_tick, int observed_tick) {
			if (system_tick - observed_tick > _config.cacheTicks - 1) {
				return;
			}

			var begin_index = index_into(client, system_tick - observed_tick);
			for (var i = 0; i < 128; i++) {
				var actor_sent = sent_data[begin_index + i];
				if (actor_sent == 0) continue;
				var prev_seen_tick = GetClientActorLastSeenTick(client, actor_sent);
				if (prev_seen_tick > observed_tick) {
					// skip if we've seen a more recent tick already
					continue;
				}

				ack_data[actor_sent * _config.maxClients + client] = observed_tick;
			}
		}

		/** should only be used for tests */
		public void ForceAck(int client, int actor, int tick) {
			ack_data[actor * _config.maxClients + client] = tick;
		}

		protected override void OnUpdate() {
			head++;
		}

		public int index_into(int client, int lookback_ticks = 0) {
			return wrap(head - lookback_ticks) + (_config.cacheTicks * 128 * client);
		}

		/** wrap head index around ring buffer length */
		public int wrap(int idx) {
			return idx & (_config.cacheTicks - 1);
		}
	}
}