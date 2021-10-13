using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace ht.replication {
	[DisableAutoCreation]
	public class ActorSystem : ComponentSystem {

		/** free actor ids*/
		private LinkedList<int> _freeActorIndices = new LinkedList<int>();
		/** actor generations */
		public NativeArray<int> generations;
		/** actor spawn ticks */
		public NativeArray<int> spawns;
		/** actor types */
		public NativeArray<int> types;
		/** actor->entity map */
		public NativeHashMap<int, Entity> entities;

		protected override void OnCreate() {
			for (var i = 1; i < 8192; i++) {
				_freeActorIndices.AddLast(i);
			}
			generations = new NativeArray<int>(8192, Allocator.Persistent);
			spawns = new NativeArray<int>(8192, Allocator.Persistent);
			types = new NativeArray<int>(8192, Allocator.Persistent);
			entities = new NativeHashMap<int, Entity>(8192, Allocator.Persistent);
		}

		protected override void OnDestroy() {
			generations.Dispose();
			spawns.Dispose();
			types.Dispose();
			entities.Dispose();
		}

		public Actor alloc(Entity actor, int tick, int type) {
			var id = _freeActorIndices.First.Value;
			generations[id]++;
			var gen = generations[id];
			_freeActorIndices.RemoveFirst();
			spawns[id] = tick;
			types[id] = type;
			EntityManager.AddComponent<Actor>(actor);
			var data = new Actor {id = id, gen = gen, type = type, spawn = tick };
			EntityManager.SetComponentData(actor, data);
			entities.TryAdd(id, actor);
			return data;
		}

		public void free(Actor actor, int tick) {
			generations[actor.id]++;
			spawns[actor.id] = tick;
			types[actor.id] = 0;
			_freeActorIndices.AddFirst(actor.id);
			entities.Remove(actor.id);
		}

		protected override void OnUpdate() {

		}
	}
}