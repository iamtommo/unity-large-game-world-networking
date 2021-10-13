using System.Collections.Generic;

namespace ht.replication {
	/**
	 * Manage actor id/gen distribution
	 */
	public static class ActorManager {

		private static int _gen = 1;

		private static LinkedList<ActorGenPair> _freeActorIndices = new LinkedList<ActorGenPair>();

		static ActorManager() {
			for (var i = 0; i < 8192; i++) {
				_freeActorIndices.AddLast(new ActorGenPair {actor = i + 1, gen = 1});
			}
		}

		public static int alloc() {
			var id = _freeActorIndices.First;
			Generation[id.Value.actor] = id.Value.gen;
			_freeActorIndices.RemoveFirst();
			return id.Value.actor;
		}

		public static void free(int actor) {
			CycleGeneration(actor);
			_freeActorIndices.AddFirst(new ActorGenPair {actor = actor, gen = Generation[actor]});
		}

		public static Dictionary<int, int> Generation = new Dictionary<int,int>();

		private static int CycleGeneration(int actor) {
			if (Generation.ContainsKey(actor)) {
				var gen = Generation[actor]++;
				return gen;
			}

			Generation.Add(actor, 1);
			return 1;
		}

		struct ActorGenPair {
			public int actor;
			public int gen;
		}

	}
}