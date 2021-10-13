using Unity.Mathematics;

namespace ht.replication {
	public struct Region {
		public static Region zero = default(Region);
		public const int RegionSize = 256;
		public const int RegionsWide = 2048 / RegionSize;

		public int2 xy;

		public Region(int x, int y) {
			this.xy = new int2 {x = x, y = y};
		}

		public Region(int2 xy) {
			this.xy = xy;
		}

		public static Region worldToRegion(float3 world) {
			return new Region(new int2((int) math.floor(world.x / RegionSize), (int) math.floor(world.z / RegionSize)));
		}

		public static int dist(Region a, Region b) {
			return math.abs(a.xy.x - b.xy.x) + math.abs(a.xy.y - b.xy.y);
		}

		public static int hash(int2 region) {
			return region.x & 0xFF | ((region.y & 0xFF) << 8);
		}

		public static int2 unhash(int hash) {
			return new int2 {x = hash & 0xFF, y = (hash >> 8) & 0xFF};
		}
	}
}