using System;
using Unity.Entities;

namespace ht.replication {
	[InternalBufferCapacity(256)]
	public struct ActorUpdate : IBufferElementData, IComparable<ActorUpdate> {
		public int id;
		public int type;
		public int gen;
		public int datamask;
		public int updatemask;

		/** actor sort based purely on type where type 0 = deletion */
		public int CompareTo(ActorUpdate other) {
			return type.CompareTo(other.type);
		}
	}
}