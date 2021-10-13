using Unity.Collections;
using Unity.Entities;

namespace ht.replication {
	/**
	 * Maintain per actor change masks for a short historical period
	 */
	[DisableAutoCreation]
	public class ActorHistorySystem : ComponentSystem {

		/**
		 * change bit masks
		 * structure: lookback * actors
		 */
		public NativeArray<int> diffmasks;

		private ReplicationConfig _config;

		protected override void OnCreate() {
			_config = GetSingleton<ReplicationConfig>();
			diffmasks = new NativeArray<int>(_config.history_bufsz * _config.maxActors, Allocator.Persistent);
		}

		protected override void OnDestroy() {
			diffmasks.Dispose();
		}

		protected override void OnUpdate() {

		}

		public void EndFrameRollover() {
			for (var i = 1; i < _config.history_bufsz; i++) {
				var idx = i * _config.maxActors;
				NativeArray<int>.Copy(diffmasks, idx - _config.maxActors, diffmasks, idx, _config.maxActors);
			}

			/*for (var i = 1; i < _lookback; i++) {
				var idx = i * Hworldsnap.DefaultActorCap;
				for (var j = 0; j < Hworldsnap.DefaultActorCap; j++) {
					changemasks[idx + j] =
						changemasks[idx - Hworldsnap.DefaultActorCap + j];
				}
			}*/
		}
	}
}