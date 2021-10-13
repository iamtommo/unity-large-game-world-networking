using Unity.Collections;

namespace ht.replication {
	public interface IDenseWorld {
		NativeArray<int> GetActorGenerations();
		NativeArray<int> GetActorDatamasks();
	}
}