using System;
using Unity.Entities;

namespace ht.replication {
	public struct Actor : IComponentData, IComparable<Actor> {
		/** actor id*/
		public int id;
		/** actor generation */
		public int gen;
		/** actor type */
		public int type;
		/** actor spawn tick */
		public int spawn;
		public int datamask;
		public int changemask;

		/** actor sort based purely on type where type 0 = deletion */
		public int CompareTo(Actor other) {
			return type.CompareTo(other.type);
		}
	}
}