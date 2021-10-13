using Unity.Collections;
using Unity.Entities;

namespace ht.replication {
	[DisableAutoCreation]
	public class PerceptionSystem : ComponentSystem {

		/**
		 * perception data
		 * len = actors * clients
		 * cell = gen | tick
		 */
		public NativeArray<long> _pdata;

		/**
		 * actor generations sent to client where gen=0 assumes not sent
		 * len = 32 * actors * clients
		 * cell = gen || 0
		 */
		public NativeArray<int> _psent;
		public int _psenthead;//ringbuf head index (tick)

		private ReplicationConfig _config;

		protected override void OnCreate() {
			_config = GetSingleton<ReplicationWorld>().config;
			_pdata = new NativeArray<long>(_config.maxActors * _config.maxClients, Allocator.Persistent);
			_psent = new NativeArray<int>(32 * _config.maxActors * _config.maxClients, Allocator.Persistent);
		}

		protected override void OnDestroy() {
			_psent.Dispose();
			_pdata.Dispose();
		}

		public void SetActorGenSent(int client, int actor, int gen) {
			_psent[(_psenthead & (32 - 1)) + actor * _config.maxClients + client] = gen;
		}

		// warning will overflow if lookback > 32-1
		public int GetActorGenSent(int client, int actor, int lookback_ticks) {
			return _psent[((_psenthead - lookback_ticks) & (32 - 1)) + actor * _config.maxClients + client];
		}

		public int GetClientActorLastSeenTick(int client, int actor) {
			return (int) (_pdata[actor * _config.maxClients + client] & 0xFFFFFFFF);
		}

		public int GetClientActorLastSeenGen(int client, int actor) {
			return (int) ((_pdata[actor * _config.maxClients + client] >> 32) & 0xFFFFFFFF);
		}

		private void See(int system_tick, int client, int observed_tick) {
			if (system_tick - observed_tick >= 30) {
				return;
			}

			for (var actor = 0; actor < _config.maxActors; actor++) {
				var prev_seen_tick = GetClientActorLastSeenTick(client, actor);
				if (prev_seen_tick > observed_tick) {
					// skip if we've seen a more recent tick already
					continue;
				}
				var gen_sent = GetActorGenSent(client, actor, (system_tick - observed_tick));
				if (gen_sent == 0) {
					// skip if we didn't send any data at this tick
					continue;
				}

				_pdata[actor * _config.maxClients + client] = (((long) gen_sent) << 32) | observed_tick;
			}
		}

		protected override void OnUpdate() {
			var tick = GetSingleton<ReplicationWorld>().tick;

			// this is kinda dangerous since it assumes that the head index should be the current tick.
			// it works out OK since systems which add data to here i.e. SetActorGenSent when writing packets
			// happens after this system is updated.
			// also something to keep in mind for tests but eh fuck it works for now
			_psenthead = tick;

			Entities.ForEach((Entity e, ref Perception perception) => {
				See(tick, perception.client, perception.tick);
				PostUpdateCommands.DestroyEntity(e);
			});
		}

		/**
		 * Given perception data of structure: array[clients * actors] where cell = tick last seen
		 * Returns a map of ticks on which the perception system is dependent (for history deltas)
		 * (could use a hash set but there's no native set implemented yet and i cba)
		 */
		/*public NativeHashMap<int, bool> DeriveObserverDependentTicks(NativeArray<int> data) {
			var dependent_tickmap = new NativeHashMap<int, bool>(500, Allocator.Persistent);
			for (var actor = 0; actor < GameConst.ActorCap; actor++) {
				for (var client = 0; client < GameConst.MaxClients; client++) {
					var dependent_tick = data[actor * GameConst.MaxClients + client];
					if (dependent_tick == 0 || dependent_tickmap.TryGetValue(dependent_tick, out var ignore)) {
						continue;
					}

					dependent_tickmap.TryAdd(dependent_tick, true);
				}
			}

			return dependent_tickmap;
		}

		public NativeHashMap<int, bool> GetObserverDependentTicks() {
			return DeriveObserverDependentTicks(_pdata);
		}*/
	}
}