using Unity.Entities;

namespace ht.replication {
	[DisableAutoCreation]
	public class ReplicationClientSystem : ComponentSystem {

		protected override void OnCreate() {

		}

		protected override void OnUpdate() {
			Entities.WithNone<ReplicationClientSystemState>().ForEach((Entity e, ref ReplicationClient client) => {
				EntityManager.AddBuffer<ActorUpdate>(e);
				EntityManager.AddComponent<ReplicationClientSystemState>(e);
			});

			Entities.WithNone<ReplicationClient>().ForEach((Entity e, ref ReplicationClientSystemState systemState) => {
				PostUpdateCommands.RemoveComponent<ReplicationClientSystemState>(e);
				PostUpdateCommands.DestroyEntity(e);
			});
		}
	}
}