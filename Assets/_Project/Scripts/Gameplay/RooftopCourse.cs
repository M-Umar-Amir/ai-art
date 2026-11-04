using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The rooftop shortcut (client correction, 2 Nov): not another lane, but the roofs of the real buildings on
    /// the right-hand side of the street. A kicker ramp in the right lane sends the rider up the facade; the run
    /// carries on across the roofs, with alley gaps to jump (some spanned by a glass skybridge in one lane), and
    /// ends with a drop back into the street. Layout is a pure function of the zone index, so the builder, the
    /// ground test, the spawn rules and the bots all agree.
    public static class RooftopCourse
    {
        /// RETIRED (client, 4 Nov 2026): the shortcut is now the Skypark portal. Start is pushed out of reach so
        /// every predicate here is false and no rooftop block row is built; the layout code is kept for reference.
        public const float Start = 1e7f, Every = 480f, Length = 150f;
        /// The kicker ramp in the right-hand lane ends at the zone start.
        public const float RampLength = 7f, RampHeight = 0.9f;
        public const int EntryLane = 2;
        /// The rider rolls off the last roof here, onto the skip on the kerb, then down into the street.
        public const float DropAt = Length - 6f;

        /// The real things the parkour line steps on, metres from the zone start: x across the street, the top
        /// the rider lands on, and the z span the builder fills. Up: a parked delivery truck on the kerb, then a
        /// steel fire-escape landing on the facade, then the parapet. Down: a construction skip on the kerb.
        public struct Step { public float z0, z1, x, top; public float Mid => (z0 + z1) * 0.5f; }
        public static readonly Step Truck = new() { z0 = 1.5f, z1 = 8.5f, x = 5.5f, top = 2.5f };
        public static readonly Step Landing = new() { z0 = 12f, z1 = 20f, x = 6.25f, top = 5.2f };
        public const float RoofLanding = 27f;
        public static readonly Step Skip = new() { z0 = DropAt + 8f, z1 = DropAt + 13f, x = 5.6f, top = 2.4f };
        public const float StreetLanding = DropAt + 22f;

        public struct Block
        {
            public float z0, z1;
            /// Glass skybridge across the gap before this block, in this rooftop lane (-1: open gap).
            public int bridgeLane;
        }

        static readonly Dictionary<int, List<Block>> cache = new();

        public static int Index(float d) => Mathf.FloorToInt((d - Start + 60f) / Every);
        public static float ZoneStart(int index) => Start + index * Every;
        public static float ZoneStartAt(float d) => ZoneStart(Mathf.Max(0, Index(d)));

        /// d lies in the zone's footprint, approach and landing included.
        public static bool Near(float d, float before = 60f, float after = 30f)
        {
            if (d < Start - before) return false;
            float s = ZoneStartAt(d);
            return d >= s - before && d <= s + Length + after;
        }
        /// The right-hand side of the street belongs to the rooftop block here.
        public static bool OwnsRightSide(float z) => Near(z, 12f, 4f);
        public static bool OnRamp(float d) { float s = ZoneStartAt(d); return d >= s - RampLength && d <= s + 0.6f && d >= Start - RampLength; }
        /// Riding over the ramp lip in the entry lane starts the climb.
        public static bool EntryWindow(float d) { float s = ZoneStartAt(d); return d >= s - 0.8f && d <= s + 1.2f && d >= Start - 1f; }
        public static bool PastDrop(float d) { float s = ZoneStartAt(d); return d >= s + DropAt; }
        /// The street's entry lane is kept clear around the ramp and the landing.
        public static bool KeepLaneClear(float z)
        {
            if (z < Start - 40f) return false;
            float s = ZoneStartAt(z);
            return (z > s - 34f && z < s + 10f) || (z > s + Length - 8f && z < s + StreetLanding + 12f);
        }

        public static List<Block> Blocks(int index)
        {
            if (cache.TryGetValue(index, out var list)) return list;
            list = new List<Block>();
            var rng = new System.Random(9173 + index * 31);
            float s = ZoneStart(index), end = s + DropAt + 4f;
            // The first block starts behind the ramp so the climb has a wall to run up, and is long enough
            // to take the whole climb (about 26 m at speed) before the first gap.
            float z = s - 6f, first = 40f;
            int bridge = -1;
            while (z < end - 8f)
            {
                float len = list.Count == 0 ? first : 18f + (float)rng.NextDouble() * 12f;
                float z1 = Mathf.Min(z + len, end);
                if (end - z1 < 12f) z1 = end;
                list.Add(new Block { z0 = z, z1 = z1, bridgeLane = bridge });
                float gap = 4.5f + (float)rng.NextDouble() * 2f;
                bridge = rng.NextDouble() < 0.45 ? rng.Next(0, 3) : -1;
                z = z1 + gap;
            }
            cache[index] = list;
            return list;
        }

        /// Roof under the rider at this distance and lateral position (roof-local lane coordinate).
        public static bool Supported(float d, float laneX)
        {
            var blocks = Blocks(Mathf.Max(0, Index(d)));
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (d >= b.z0 && d <= b.z1) return true;
                if (i > 0 && d > blocks[i - 1].z1 && d < b.z0)
                    return b.bridgeLane >= 0 && Mathf.Abs(laneX - Core.Tuning.LaneX[b.bridgeLane]) < 1.05f;
            }
            return false;
        }

        /// Distance to the next roof edge ahead (for the HUD prompt and the bots); infinity if none.
        public static float NextEdge(float d, float laneX)
        {
            var blocks = Blocks(Mathf.Max(0, Index(d)));
            for (int i = 0; i < blocks.Count - 1; i++)
            {
                var next = blocks[i + 1];
                if (blocks[i].z1 < d) continue;
                if (next.bridgeLane >= 0 && Mathf.Abs(laneX - Core.Tuning.LaneX[next.bridgeLane]) < 1.05f) continue;
                return blocks[i].z1 - d;
            }
            return float.PositiveInfinity;
        }
    }
}
