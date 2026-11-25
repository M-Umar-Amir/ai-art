using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Downtown Toronto's street (client, 25 Nov 2026: "buildings and transport and people and environment should be
    /// based on downtown toronto"). Everything here is static and merges into the block meshes (StreetMesh):
    ///   - TTC streetcar track: two tracks of girder rails at the TTC's own 1,495 mm gauge, under the outer lanes
    ///     (westbound left, eastbound right), as on King, Queen and Spadina;
    ///   - the overhead: a trolley contact wire 5.7 m over each track, carried by span wires every 20 m between steel
    ///     poles at the kerb, with droppers and pull-off insulators;
    ///   - a streetcar stop before every junction: the red-and-white stop flag on its pole and a glass transit shelter
    ///     with a backlit KFC poster in its ad panel (KFC is the only brand on the street);
    ///   - the city's post-and-ring bike stands, grey multi-stream litter bins and bike-share docks (unbranded bikes).
    /// The blue street-name signs with real downtown names are on the junction poles (TrackSpawner.Intersections).
    public static class TorontoStreet
    {
        public const float Gauge = 1.495f, WireHeight = 5.7f, SpanHeight = 6.35f, PoleX = 4.45f;
        /// The streetcar tracks run under the outer lanes.
        public static readonly float[] TrackX = { -2.3f, 2.3f };
        /// North-south cross streets met along King Street West, west to east (the blue blades at each junction).
        public static readonly string[] CrossStreets =
            { "Spadina Ave", "Peter St", "John St", "Simcoe St", "University Ave", "York St", "Bay St", "Yonge St", "Church St", "Jarvis St" };
        public const string MainStreet = "King St W";
        /// City of Toronto street-sign blue (Pantone 647) with white lettering.
        public static readonly Color SignBlue = new(.14f, .38f, .57f);

        public static bool Active => DashRun.District == 0;
        /// The street lane is a streetcar track (a stopped streetcar may stand there).
        public static bool TrackLane(int lane) => lane == 0 || lane == Tuning.Lanes - 1;

        static void Box(Vector3 at, Vector3 size, Material m) => StreetMesh.Add(PrimitiveType.Cube, at, Quaternion.identity, size, m);
        static void Box(Vector3 at, Quaternion rotation, Vector3 size, Material m) => StreetMesh.Add(PrimitiveType.Cube, at, rotation, size, m);
        static void Cyl(Vector3 at, Quaternion rotation, Vector3 size, Material m) => StreetMesh.Add(PrimitiveType.Cylinder, at, rotation, size, m);

        static Material RailSteel(TrackSpawner track)
        {
            var m = DetailCull.MarkNoShadow(DashCity.Mat(track, "ttcRailHead", new Color(.62f, .62f, .66f)));
            m.SetFloat("_Metallic", .9f); m.SetFloat("_Smoothness", .82f);
            return m;
        }

        // ------------------------------------------------------------------ track and overhead (per 20 m segment)

        /// Rails, contact wires and (outside junctions) a span with its two poles, for the segment starting at z.
        public static void Segment(TrackSpawner track, Transform parent, float z, float length, bool junction)
        {
            if (!Active) return;
            var rail = RailSteel(track);
            var groove = DetailCull.MarkNoShadow(DashCity.Mat(track, "ttcRailGroove", new Color(.035f, .035f, .04f)));
            var wire = DetailCull.MarkNoShadow(DashCity.Mat(track, "ttcContactWire", new Color(.09f, .08f, .07f)));
            var pole = DashCity.Mat(track, "ttcPoleSteel", new Color(.3f, .32f, .33f));
            var insulator = DetailCull.MarkNoShadow(DashCity.Mat(track, "ttcInsulator", new Color(.16f, .12f, .1f)));
            float mid = z + length * 0.5f;
            StreetMesh.Begin(parent);
            try
            {
                foreach (float tx in TrackX)
                {
                    foreach (float s in new[] { -1f, 1f })
                    {
                        float rx = tx + s * Gauge * 0.5f;
                        Box(new Vector3(rx, 0.008f, mid), new Vector3(0.075f, 0.016f, length), rail);
                        // The girder rail's flangeway groove, on the inside of the gauge.
                        Box(new Vector3(rx - s * 0.065f, 0.006f, mid), new Vector3(0.045f, 0.012f, length), groove);
                    }
                    Box(new Vector3(tx, WireHeight, mid), new Vector3(0.028f, 0.028f, length), wire);
                }
                if (junction)
                {
                    // The cross street's centre track and its wire (a Spadina-style right-of-way), out past the corners.
                    float cm = z + length * 0.5f;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        float cx = side * (4.2f + 35f);
                        foreach (float s in new[] { -1f, 1f })
                        {
                            Box(new Vector3(cx, 0.008f, cm + s * Gauge * 0.5f), new Vector3(70f, 0.016f, 0.075f), rail);
                            Box(new Vector3(cx, 0.006f, cm + s * (Gauge * 0.5f - 0.065f)), new Vector3(70f, 0.012f, 0.045f), groove);
                        }
                        Box(new Vector3(cx, WireHeight, cm), new Vector3(70f, 0.028f, 0.028f), wire);
                    }
                    return;
                }
                // Span wire between two kerb poles, just past the segment start (clear of lamps, trees and seats).
                float zs = z + 0.5f;
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * PoleX;
                    Box(new Vector3(x, 0.35f, zs), new Vector3(0.42f, 0.3f, 0.42f), pole);
                    Box(new Vector3(x, 3.75f, zs), new Vector3(0.24f, 7f, 0.24f), pole);
                    Box(new Vector3(x, 7.3f, zs), new Vector3(0.3f, 0.12f, 0.3f), pole);
                    Box(new Vector3(x - side * 0.22f, SpanHeight, zs), new Vector3(0.44f, 0.08f, 0.08f), pole); // span bracket
                }
                Box(new Vector3(0f, SpanHeight, zs), new Vector3(PoleX * 2f - 0.8f, 0.02f, 0.02f), wire);
                foreach (float tx in TrackX)
                {
                    Box(new Vector3(tx, (SpanHeight + WireHeight) * 0.5f, zs), new Vector3(0.02f, SpanHeight - WireHeight, 0.02f), wire);
                    Box(new Vector3(tx, SpanHeight - 0.2f, zs), new Vector3(0.07f, 0.16f, 0.07f), insulator);
                    Box(new Vector3(tx, WireHeight + 0.04f, zs), new Vector3(0.05f, 0.06f, 0.3f), insulator); // the clamp on the wire
                }
            }
            finally { StreetMesh.End(); }
        }

        // ------------------------------------------------------------------ pavement furniture (per block side)

        /// Toronto pavement furniture for one block side. `stop` marks the near side of the next junction, where the
        /// streetcar stop and its shelter stand.
        public static void Furniture(TrackSpawner track, Transform parent, float side, float z, int h, bool stop, bool outlet)
        {
            if (!Active) return;
            StreetMesh.Begin(parent);
            try
            {
                if (stop) StreetcarStop(track, parent, side, z);
                else LitterBin(track, new Vector3(side * 4.62f, 0.2f, z + 12.8f), side);
                foreach (float bz in new[] { z + 17.4f, z + 18.6f }) PostAndRing(track, new Vector3(side * 4.55f, 0.2f, bz));
                if (h % 3 == 0 && !outlet) BikeShareDock(track, side, z + 12.6f, 5);
            }
            finally { StreetMesh.End(); }
        }

        /// Stop flag on its pole and a glass shelter whose end panel carries a backlit KFC poster.
        static void StreetcarStop(TrackSpawner track, Transform parent, float side, float z)
        {
            var grey = DashCity.Mat(track, "ttcPoleSteel", new Color(.3f, .32f, .33f));
            var red = DashCity.Mat(track, "ttcStopRed", new Color(.78f, .06f, .08f), .15f);
            var white = DashCity.Mat(track, "ttcStopWhite", new Color(.93f, .93f, .9f), .12f);
            var ink = DashCity.Mat(track, "ttcStopInk", new Color(.08f, .08f, .1f));
            // The stop pole: a white flag with a red band, a streetcar pictogram (no lettering, no logo).
            float px = side * 4.5f, pz = z + 12.5f;
            Box(new Vector3(px, 1.6f, pz), new Vector3(0.08f, 2.8f, 0.08f), grey);
            Box(new Vector3(px, 2.75f, pz - 0.05f), new Vector3(0.46f, 0.62f, 0.03f), white);
            Box(new Vector3(px, 2.98f, pz - 0.068f), new Vector3(0.46f, 0.16f, 0.01f), red);
            Box(new Vector3(px, 2.68f, pz - 0.068f), new Vector3(0.32f, 0.12f, 0.01f), red);          // car body
            Box(new Vector3(px, 2.71f, pz - 0.072f), new Vector3(0.26f, 0.04f, 0.01f), ink);          // its windows
            Box(new Vector3(px, 2.79f, pz - 0.068f), new Vector3(0.012f, 0.08f, 0.01f), ink);         // trolley pole
            Box(new Vector3(px, 2.54f, pz - 0.068f), new Vector3(0.36f, 0.02f, 0.01f), ink);          // rail

            // Transit shelter: dark frame, roof, glass back and ends, a bench and the poster panel at the far end.
            var frame = DashCity.Mat(track, "shelterFrame", new Color(.16f, .17f, .18f));
            frame.SetFloat("_Metallic", .5f); frame.SetFloat("_Smoothness", .5f);
            var glass = DashCity.Mat(track, "shelterGlass", new Color(.34f, .4f, .44f), .05f);
            glass.SetFloat("_Smoothness", .92f); glass.SetFloat("_Metallic", .3f);
            float x0 = side * 4.6f, x1 = side * 5.85f, xm = (x0 + x1) * 0.5f;
            float z0 = z + 5.3f, z1 = z + 9.1f, zm = (z0 + z1) * 0.5f;
            const float top = 2.55f, foot = 0.2f;
            foreach (float cx in new[] { x0, x1 })
                foreach (float cz in new[] { z0, z1 })
                    Box(new Vector3(cx, (foot + top) * 0.5f, cz), new Vector3(0.07f, top - foot, 0.07f), frame);
            Box(new Vector3(xm, top + 0.06f, zm), new Vector3(Mathf.Abs(x1 - x0) + 0.25f, 0.1f, z1 - z0 + 0.3f), frame);
            Box(new Vector3(x1, 1.35f, zm), new Vector3(0.025f, 1.9f, z1 - z0 - 0.1f), glass);            // back, to the building side
            Box(new Vector3(xm, 1.35f, z1), new Vector3(Mathf.Abs(x1 - x0) - 0.1f, 1.9f, 0.025f), glass); // near end
            Box(new Vector3(xm + side * 0.25f, 0.62f, zm), new Vector3(0.38f, 0.05f, 1.8f), frame);       // bench
            // The ad panel: a lit box at the end facing the oncoming rider, poster both sides.
            Box(new Vector3(xm, 1.3f, z0), new Vector3(1.24f, 1.95f, 0.14f), frame);
            var poster = KfcStreet.Ad(track, track.kfcPoster, 0.9f);
            KfcStreet.Quad(parent, new Vector3(xm, 1.3f, z0 - 0.075f), Quaternion.identity, 1.08f, 1.62f, poster, true);
            KfcStreet.Quad(parent, new Vector3(xm, 1.3f, z0 + 0.075f), Quaternion.Euler(0f, 180f, 0f), 1.08f, 1.62f, poster, false);
        }

        /// The city's grey multi-stream bin: three openings under a sloped silver top.
        static void LitterBin(TrackSpawner track, Vector3 at, float side)
        {
            var body = DashCity.Mat(track, "torontoBinGrey", new Color(.28f, .3f, .31f));
            var cap = DashCity.Mat(track, "torontoBinCap", new Color(.62f, .63f, .64f));
            cap.SetFloat("_Metallic", .6f);
            var slot = DashCity.Mat(track, "torontoBinSlot", new Color(.03f, .03f, .035f));
            Box(at + new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 1.1f, 1.05f), body);
            Box(at + new Vector3(0f, 1.13f, 0f), new Vector3(0.56f, 0.07f, 1.1f), cap);
            for (int i = -1; i <= 1; i++)
                Box(at + new Vector3(-side * 0.255f, 0.9f, i * 0.32f), new Vector3(0.02f, 0.12f, 0.24f), slot);
        }

        /// Toronto's post-and-ring bike stand: a short post with a ring in line with the kerb.
        static void PostAndRing(TrackSpawner track, Vector3 at)
        {
            var steel = DashCity.Mat(track, "postRingSteel", new Color(.66f, .67f, .7f));
            steel.SetFloat("_Metallic", .85f); steel.SetFloat("_Smoothness", .7f);
            Cyl(at + new Vector3(0f, 0.45f, 0f), Quaternion.identity, new Vector3(0.06f, 0.45f, 0.06f), steel);
            const int n = 10;
            const float r = 0.24f, cy = 0.62f;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / n;
                var p = at + new Vector3(0f, cy + Mathf.Sin(a) * r, Mathf.Cos(a) * r);
                Box(p, Quaternion.Euler(90f - a * Mathf.Rad2Deg, 0f, 0f), new Vector3(0.035f, 0.035f, 2f * Mathf.PI * r / n + 0.01f), steel); // along the ring
            }
        }

        /// A bike-share dock along the building side of the pavement: a grey rail, a pay terminal and docked bikes
        /// (charcoal frames, green fenders, front baskets; no branding).
        static void BikeShareDock(TrackSpawner track, float side, float z, int bikes)
        {
            var rail = DashCity.Mat(track, "dockRail", new Color(.42f, .44f, .45f));
            var frame = DashCity.Mat(track, "shareBikeFrame", new Color(.12f, .13f, .14f));
            var fender = DashCity.Mat(track, "shareBikeGreen", new Color(.25f, .55f, .3f));
            var tyre = DetailCull.MarkDetail(DashCity.Mat(track, "shareBikeTyre", new Color(.04f, .04f, .04f)));
            var screen = DetailCull.MarkNoShadow(DashCity.Mat(track, "dockScreen", new Color(.55f, .8f, .9f), 1.2f));
            float x = side * 7.55f;
            float len = bikes * 0.85f + 0.6f;
            Box(new Vector3(x, 0.26f, z + len * 0.5f), new Vector3(0.22f, 0.12f, len), rail);
            // Pay terminal at the near end.
            Box(new Vector3(x, 0.95f, z), new Vector3(0.36f, 1.5f, 0.3f), rail);
            Box(new Vector3(x - side * 0.185f, 1.35f, z), new Vector3(0.01f, 0.32f, 0.22f), screen);
            for (int i = 0; i < bikes; i++)
            {
                float bz = z + 0.9f + i * 0.85f;
                // Docked bike, side-on to the street, front wheel in the dock.
                var fw = new Vector3(x - side * 0.25f, 0.53f, bz);
                var rw = new Vector3(x - side * 1.3f, 0.53f, bz);
                foreach (var w in new[] { fw, rw })
                {
                    Cyl(w, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.66f, 0.025f, 0.66f), tyre); // axle across the bike
                    Box(w + Vector3.up * 0.36f, Quaternion.identity, new Vector3(0.6f, 0.03f, 0.08f), fender);
                }
                var mid = (fw + rw) * 0.5f;
                Box(mid + Vector3.up * 0.3f, Quaternion.Euler(0f, 0f, side * 12f), new Vector3(1.0f, 0.06f, 0.06f), frame); // down tube
                Box(rw + new Vector3(side * 0.28f, 0.42f, 0f), new Vector3(0.06f, 0.6f, 0.06f), frame);                      // seat post
                Box(rw + new Vector3(side * 0.28f, 0.76f, 0f), new Vector3(0.26f, 0.06f, 0.12f), frame);                     // saddle
                Box(fw + new Vector3(-side * 0.06f, 0.55f, 0f), new Vector3(0.06f, 0.8f, 0.06f), frame);                     // head tube
                Box(fw + new Vector3(-side * 0.06f, 0.96f, 0f), new Vector3(0.06f, 0.04f, 0.56f), frame);                    // bars
                Box(fw + new Vector3(side * 0.1f, 0.78f, 0f), new Vector3(0.3f, 0.2f, 0.36f), frame);                        // basket
            }
        }
    }
}
