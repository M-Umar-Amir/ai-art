using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Downtown street blocks (client, 25 Nov 2026: "shops to be made like these references": Bishop Arts Station,
    /// Dallas, and the Distillery District, Toronto). Each 20 m block side is a brick mixed-use building:
    ///   - a 6.4 m shop storey: three shopfronts between brick piers, each with a black-framed glazed front onto a lit
    ///     interior, a lit transom, a painted sign band with gooseneck barn lamps, a canvas awning and a hanging blade
    ///     sign. Only KFC is a brand on this street (client: "only kfc outlets, remove others, only kfc branding"), so
    ///     these shops carry no names: the blade signs show generic pictograms (florist, barber, records...);
    ///   - 2 to 5 upper floors in one of three bricks (WindowAtlas brick kinds from make_brick_facades.py): Bishop Arts
    ///     red-orange loft brick with black steel balconies, Distillery warehouse brick with arched windows and a fire
    ///     escape, or Bishop Arts buff brick with wide bronze windows and a louvred band over the shops;
    ///   - a stone belt course, a corbelled cornice and coping, roof plant and sometimes a timber water tower;
    ///   - a KFC rooftop bulletin on alternating sides (KfcStreet), and the curved corner screen on the corner building
    ///     the rider meets across a junction, whose corner is rounded behind the screen;
    ///   - the taller row behind (x 28-36 m), now downtown Toronto (client, 25 Nov 2026: "buildings ... based on
    ///     downtown Toronto"): glass condo towers with white slab edges and set-back crowns, TD Centre-style black
    ///     steel slabs and glass offices, some carrying KFC LED screens; no neon edges or spires;
    ///   - on some blocks Toronto's signature facadism instead: the brick building kept at the street with a glass
    ///     condo tower rising set back behind its facade (King Street West, The Well, Mirvish);
    ///   - the CN Tower and the Financial District on the horizon ahead (TorontoSkyline).
    /// Every third block one side is a KFC restaurant instead (KfcStreet.Outlet). Everything static is merged by
    /// TrackSpawner.Combine.
    public static class DowntownArchitecture
    {
        public const float Front = 8.5f, Depth = 14f, Podium = 6.4f, Floor = 3.2f;

        static int Hash(int a, int b)
        {
            unchecked
            {
                int h = a * 73856093 ^ b * 19349663;
                h ^= h >> 13; h *= 1274126177; h ^= h >> 16;
                return h & 0x7fffffff;
            }
        }

        /// A static part, straight into the block's merged meshes (StreetMesh); `parent` is kept for readability.
        static void P(Transform parent, Vector3 at, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube) =>
            StreetMesh.Add(type, at, Quaternion.identity, size, m);
        static void P(Transform parent, Vector3 at, Quaternion rotation, Vector3 size, Material m) =>
            StreetMesh.Add(PrimitiveType.Cube, at, rotation, size, m);

        /// Small brightness/warmth shifts so neighbouring buildings of the same brick do not read as copies.
        static readonly Color[] BrickTints = { new(1f, 1f, 1f), new(.93f, .91f, .9f), new(1.04f, 1f, .96f), new(.88f, .87f, .88f) };
        static readonly Color[] AwningColours =
        {
            new(.12f, .3f, .2f), new(.07f, .07f, .08f), new(.1f, .16f, .3f), new(.6f, .44f, .12f), new(.28f, .32f, .34f), new(.36f, .2f, .14f),
        };
        static readonly Color[] BoardColours = { new(.06f, .16f, .11f), new(.05f, .05f, .06f), new(.07f, .1f, .2f), new(.2f, .12f, .07f) };
        static readonly Color[] InteriorColours =
        {
            new(.62f, .7f, .6f), new(.3f, .34f, .42f), new(.72f, .55f, .42f), new(.85f, .8f, .7f), new(.6f, .75f, .7f), new(.32f, .3f, .32f),
        };
        static readonly Color[] GoodsColours =
        {
            new(.9f, .3f, .45f), new(.95f, .8f, .2f), new(.3f, .55f, .9f), new(.95f, .95f, .92f), new(.35f, .7f, .4f), new(.75f, .4f, .2f),
        };

        public static void Build(TrackSpawner track, Transform parent, float side, float z)
        {
            StreetMesh.Begin(parent);
            try { BuildBlock(track, parent, side, z); }
            finally { StreetMesh.End(); }
        }

        static void BuildBlock(TrackSpawner track, Transform parent, float side, float z)
        {
            int block = Mathf.FloorToInt(z / 20f);
            int h = Hash(block, side > 0 ? 7 : 3);
            float face = side * Front;
            // Pavement fill up to the building line.
            P(parent, new Vector3(side * (Front - 0.8f), 0.1f, z + 10f), new Vector3(1.6f, 0.2f, 19.6f), track.sidewalkMaterial);
            // The block just past a junction faces the rider across it: its near corner carries the curved screen.
            bool corner = TrackSpawner.Junction(z - 20f);
            if (!corner && KfcStreet.OutletAt(block, side))
            {
                KfcStreet.Outlet(track, parent, side, z, face, h);
                MidTower(track, parent, side, z, h);
                return;
            }
            var kind = (h % 3) switch { 0 => WindowAtlas.Kind.Loft, 1 => WindowAtlas.Kind.Warehouse, _ => WindowAtlas.Kind.Buff };
            int floors = 2 + (h >> 4) % 4;
            if (corner) floors = Mathf.Max(floors, 4);
            float top = Podium + floors * Floor;
            var tint = BrickTints[(h >> 8) % BrickTints.Length];
            bool billboard = !corner && KfcStreet.BillboardAt(block, side);
            // A facadism tower never stands just in front of a bulletin on its side (it would hide the board).
            bool tower = !corner && !billboard && !KfcStreet.BillboardAt(block + 1, side) && (h >> 12) % 4 == 0;
            BrickBody(track, parent, side, z, face, h, kind, tint, top, corner, tower);
            Storefronts(track, parent, side, z, face, h, kind, tint);
            if (corner) KfcStreet.CornerScreen(track, parent, side, z, face, top);
            else if (billboard) KfcStreet.RooftopBillboard(track, parent, side, z, face, top, block);
            if (KfcStreet.ProjectingAt(block, side)) KfcStreet.ProjectingBillboard(track, parent, side, z, face, top);
            if (tower) FacadismTower(track, parent, side, z, face, top, h);
            else MidTower(track, parent, side, z, h);
        }

        // ------------------------------------------------------------------ the brick building

        static void BrickBody(TrackSpawner track, Transform parent, float side, float z, float face, int h, WindowAtlas.Kind kind, Color tint, float top, bool corner, bool tower)
        {
            Vector3 In(float d, float y, float pz) => new(face + side * d, y, pz);
            var facade = WindowAtlas.Facade(track, kind, tint, 1.3f);
            var plain = WindowAtlas.Facade(track, WindowAtlas.Plain(kind), tint, 0f);
            var stone = DashCity.Mat(track, "stoneTrim", new Color(.78f, .75f, .68f));
            var coping = DashCity.Mat(track, "copingStone", new Color(.74f, .71f, .64f));
            var steel = DashCity.Mat(track, "roofSteel", new Color(0.16f, 0.18f, 0.21f));
            float z0 = z + 0.3f, z1 = z + 19.7f, mid = z + 10f, upper = top - Podium, ym = (Podium + top) * 0.5f;
            float R = KfcStreet.CornerRadius;

            // The shop storey's mass behind the shop interiors, and the upper floors over the whole footprint.
            P(parent, In(4.6f + (Depth - 4.6f) * 0.5f, Podium * 0.5f, mid), new Vector3(Depth - 4.6f, Podium, z1 - z0), plain);
            if (corner)
            {
                // Rounded corner for the curved screen: the body stops short of the corner, a back strip fills behind
                // it, and a cylinder of the corner radius (inscribed in the corner square) makes the curve.
                P(parent, In(Depth * 0.5f, ym, (z0 + R + z1) * 0.5f), new Vector3(Depth, upper, z1 - z0 - R), facade);
                P(parent, In((R + Depth) * 0.5f, ym, z0 + R * 0.5f), new Vector3(Depth - R, upper, R), facade);
                P(parent, In(R, ym, z0 + R), new Vector3(2f * R, upper * 0.5f, 2f * R), facade, PrimitiveType.Cylinder);
            }
            else P(parent, In(Depth * 0.5f, ym, mid), new Vector3(Depth, upper, z1 - z0), facade);
            // The gap to the next building is a recessed party wall, not a slot of sky.
            P(parent, In(0.8f + (Depth - 0.8f) * 0.5f, top * 0.5f, z1 + 0.3f), new Vector3(Depth - 0.8f, top, 0.62f), plain);

            // Stone belt course over the shop storey; a corbelled brick cornice and stone coping at the roof line.
            P(parent, In(-0.08f, Podium - 0.08f, mid), new Vector3(0.36f, 0.22f, z1 - z0), stone);
            float from = corner ? z0 + R : z0;
            P(parent, In(-0.12f, top - 0.45f, (from + z1) * 0.5f), new Vector3(0.28f, 0.5f, z1 - from), plain);
            P(parent, In(-0.06f, top - 0.74f, (from + z1) * 0.5f), new Vector3(0.16f, 0.08f, z1 - from), stone);
            P(parent, In(-0.18f, top + 0.06f, (from + z1) * 0.5f), new Vector3(0.44f, 0.14f, z1 - from + 0.1f), coping);
            P(parent, In(Depth * 0.5f, top + 0.06f, mid), new Vector3(Depth, 0.12f, z1 - z0), coping);

            // Roof: a stair hut, condensers, and on some a timber water tank on legs (Toronto/Dallas loft roofs). A
            // facadism tower stands on this roof instead.
            if (!tower)
            {
                P(parent, In(10.5f, top + 1.2f, z + 4.5f), new Vector3(2.4f, 2.4f, 2.6f), plain);
                P(parent, In(8.5f, top + 0.55f, z + 14.5f), new Vector3(2.2f, 1.1f, 1.8f), steel);
                if (h % 4 == 1) WaterTank(track, parent, In(Depth - 3.2f, top, z + 7f + h % 5));
            }

            switch (kind)
            {
                case WindowAtlas.Kind.Loft: Balconies(track, parent, side, face, z0 + (corner ? R : 0f), z1, top, h); break;
                case WindowAtlas.Kind.Warehouse: if ((h & 8) == 0) FireEscape(track, parent, side, face, z0 + (corner ? R : 0f), z1, top); break;
            }
        }

        /// Black steel balconies with railings under some loft windows (windows sit at world z = 3.2k + 1.6).
        static void Balconies(TrackSpawner track, Transform parent, float side, float face, float z0, float z1, float top, int h)
        {
            Vector3 In(float d, float y, float pz) => new(face + side * d, y, pz);
            var steel = DashCity.Mat(track, "blackSteel", new Color(.05f, .05f, .055f));
            steel.SetFloat("_Metallic", .4f); steel.SetFloat("_Smoothness", .45f);
            int firstCell = Mathf.RoundToInt(Podium / Floor), lastCell = Mathf.RoundToInt(top / Floor) - 1;
            for (int k = Mathf.CeilToInt((z0 + 1.3f - 1.6f) / 3.2f); 3.2f * k + 1.6f < z1 - 1.3f; k++)
                for (int c = firstCell; c <= lastCell; c++)
                {
                    if (((c + k + h) % 3 + 3) % 3 != 0) continue;
                    float zc = 3.2f * k + 1.6f, y = 3.2f * c + 0.62f;
                    P(parent, In(-0.55f, y, zc), new Vector3(1.1f, 0.12f, 2.3f), steel);
                    P(parent, In(-1.08f, y + 1.0f, zc), new Vector3(0.05f, 0.05f, 2.3f), steel);
                    P(parent, In(-1.08f, y + 0.15f, zc), new Vector3(0.04f, 0.04f, 2.3f), steel);
                    foreach (float e in new[] { -1.13f, 1.13f })
                    {
                        P(parent, In(-0.55f, y + 1.0f, zc + e), new Vector3(1.1f, 0.05f, 0.05f), steel);
                        for (float bd = -0.45f; bd > -1.05f; bd -= 0.4f)
                            P(parent, In(bd, y + 0.55f, zc + e), new Vector3(0.025f, 0.9f, 0.025f), steel);
                    }
                    for (float bz = -1.05f; bz <= 1.06f; bz += 0.3f)
                        P(parent, In(-1.08f, y + 0.55f, zc + bz), new Vector3(0.025f, 0.9f, 0.025f), steel);
                    if (((c * 7 + k) & 3) == 0) // a potted plant on some
                    {
                        P(parent, In(-0.35f, y + 0.3f, zc + 0.7f), new Vector3(0.4f, 0.45f, 0.4f), DashCity.Mat(track, "planterTerracotta", new Color(.62f, .32f, .2f)));
                        P(parent, In(-0.35f, y + 0.75f, zc + 0.7f), new Vector3(0.55f, 0.6f, 0.55f), track.foliageMaterial);
                    }
                }
        }

        /// A zig-zag steel fire escape over two window bays (Distillery warehouses), from the first upper floor up.
        static void FireEscape(TrackSpawner track, Transform parent, float side, float face, float z0, float z1, float top)
        {
            Vector3 In(float d, float y, float pz) => new(face + side * d, y, pz);
            var steel = DashCity.Mat(track, "blackSteel", new Color(.05f, .05f, .055f));
            int k0 = Mathf.CeilToInt((z0 + 1f) / 3.2f);
            float a = 3.2f * k0 + 0.5f, b = a + 5.4f;
            if (b > z1 - 0.8f) return;
            float zm = (a + b) * 0.5f;
            int firstCell = Mathf.RoundToInt(Podium / Floor), lastCell = Mathf.RoundToInt(top / Floor) - 1;
            for (int c = firstCell; c <= lastCell; c++)
            {
                float y = 3.2f * c + 0.55f;
                P(parent, In(-0.65f, y, zm), new Vector3(1.3f, 0.07f, 5.4f), steel);
                foreach (float ry in new[] { 0.5f, 0.95f })
                    P(parent, In(-1.28f, y + ry, zm), new Vector3(0.04f, 0.04f, 5.4f), steel);
                for (float bz = a + 0.1f; bz < b; bz += 0.9f)
                    P(parent, In(-1.28f, y + 0.48f, bz), new Vector3(0.03f, 0.95f, 0.03f), steel);
                foreach (float e in new[] { a, b })
                    P(parent, In(-0.65f, y + 0.95f, e), new Vector3(1.3f, 0.04f, 0.04f), steel);
                // Stair up to the next landing, alternating direction (the zig-zag).
                if (c == lastCell) break;
                bool up = ((c - firstCell) & 1) == 0;
                var from = In(-0.7f, y, up ? a + 0.6f : b - 0.6f);
                var to = In(-0.7f, y + 3.2f, up ? b - 2.2f : a + 2.2f);
                var run = to - from;
                P(parent, (from + to) * 0.5f, Quaternion.LookRotation(run), new Vector3(0.62f, 0.06f, run.magnitude), steel);
            }
            // The drop ladder stops above the shop storey.
            P(parent, In(-1.1f, Podium + 0.2f, b - 0.4f), new Vector3(0.45f, 1.0f, 0.04f), steel);
        }

        static void WaterTank(TrackSpawner track, Transform parent, Vector3 at)
        {
            var timber = DashCity.Mat(track, "waterTankTimber", new Color(.38f, .27f, .18f));
            var steel = DashCity.Mat(track, "blackSteel", new Color(.05f, .05f, .055f));
            foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                P(parent, at + new Vector3(dx * 0.9f, 1.2f, dz * 0.9f), new Vector3(0.14f, 2.4f, 0.14f), steel);
            P(parent, at + new Vector3(0f, 2.45f, 0f), new Vector3(2.6f, 0.1f, 2.6f), steel);
            P(parent, at + new Vector3(0f, 3.8f, 0f), new Vector3(2.8f, 1.3f, 2.8f), timber, PrimitiveType.Cylinder);
            P(parent, at + new Vector3(0f, 5.3f, 0f), new Vector3(3.0f, 0.2f, 3.0f), steel, PrimitiveType.Cylinder);
            foreach (float hy in new[] { 3.0f, 3.8f, 4.6f })
                P(parent, at + new Vector3(0f, hy, 0f), new Vector3(2.84f, 0.03f, 2.84f), steel, PrimitiveType.Cylinder);
        }

        // ------------------------------------------------------------------ the shop storey

        static void Storefronts(TrackSpawner track, Transform parent, float side, float z, float face, int h, WindowAtlas.Kind kind, Color tint)
        {
            Vector3 In(float d, float y, float pz) => new(face + side * d, y, pz);
            var pier = WindowAtlas.Facade(track, WindowAtlas.Plain(kind), tint, 0f);
            float z0 = z + 0.3f, z1 = z + 19.7f;
            const int shops = 3;
            float len = (z1 - z0) / shops;
            for (int i = 0; i <= shops; i++)
                P(parent, In(0.25f, Podium * 0.5f, z0 + i * len), new Vector3(0.5f, Podium, 0.7f), pier);
            // Brick between the sign bands and the belt course.
            P(parent, In(0.2f, 5.95f, (z0 + z1) * 0.5f), new Vector3(0.4f, 0.9f, z1 - z0), pier);
            for (int i = 0; i < shops; i++)
                Shop(track, parent, side, face, z0 + i * len + 0.35f, z0 + (i + 1) * len - 0.35f, Hash(h, i + 11), kind);
        }

        /// One shop between two piers (a .. b along the street): interior, glazed front, transom, sign band with barn
        /// lamps, awning, a pictogram blade sign, a planter, sometimes a shopper inside.
        static void Shop(TrackSpawner track, Transform parent, float side, float face, float a, float b, int seed, WindowAtlas.Kind kind)
        {
            Vector3 In(float d, float y, float pz) => new(face + side * d, y, pz);
            float mid = (a + b) * 0.5f, w = b - a;
            int type = (seed >> 3) % 6;
            var steel = DashCity.Mat(track, "blackSteel", new Color(.05f, .05f, .055f));
            var wall = DetailCull.MarkDetail(DashCity.Mat(track, "shopInterior" + type, InteriorColours[type], .14f));
            var floor = DetailCull.MarkDetail(DashCity.Mat(track, "shopTimberFloor", new Color(.42f, .3f, .2f)));
            var ceiling = DetailCull.MarkDetail(DashCity.Mat(track, "shopCeiling", new Color(.82f, .8f, .78f), .12f));
            var panel = DetailCull.MarkDetail(DashCity.Mat(track, "shopLightPanel", new Color(1f, .93f, .82f), 2.2f));
            var shelf = DetailCull.MarkDetail(DashCity.Mat(track, "shopShelf", new Color(.5f, .36f, .24f)));
            var board = DashCity.Mat(track, "signBoard" + (seed % BoardColours.Length), BoardColours[seed % BoardColours.Length]);
            var lampLit = DetailCull.MarkNoShadow(DashCity.Mat(track, "barnLampLit", new Color(1f, .82f, .55f), 3f));
            var transom = DashCity.Mat(track, "transomGlow", new Color(1f, .8f, .55f), .9f);
            Material Goods(int i)
            {
                int j = ((i % GoodsColours.Length) + GoodsColours.Length) % GoodsColours.Length;
                return DetailCull.MarkDetail(DashCity.Mat(track, "shopGoods" + j, GoodsColours[j], .15f));
            }

            // Interior: timber floor, lit ceiling, coloured back wall, partitions to the neighbours.
            P(parent, In(2.5f, 0.21f, mid), new Vector3(4.2f, 0.04f, w), floor);
            P(parent, In(2.5f, 4.05f, mid), new Vector3(4.2f, 0.1f, w), ceiling);
            for (float pz = a + 0.9f; pz < b - 0.5f; pz += 1.8f)
                P(parent, In(2.2f, 3.98f, pz), new Vector3(0.9f, 0.02f, 0.5f), panel);
            P(parent, In(4.55f, 2.1f, mid), new Vector3(0.1f, 4.0f, w), wall);
            foreach (float pz in new[] { a, b })
                P(parent, In(2.5f, 2.1f, pz), new Vector3(4.2f, 4.0f, 0.06f), wall);
            // Goods: wall shelving with stock, and a display table in the middle (a barber has chairs instead).
            for (int row = 0; row < 3; row++)
            {
                float y = 0.6f + row * 0.85f;
                P(parent, In(4.3f, y, mid), new Vector3(0.4f, 0.04f, w - 0.4f), shelf);
                for (float gz = a + 0.4f; gz < b - 0.3f; gz += 0.7f)
                    P(parent, In(4.3f, y + 0.18f, gz), new Vector3(0.3f, 0.3f, 0.36f), Goods(type + row + Mathf.RoundToInt(gz * 3f)));
            }
            if (type == 1)
                foreach (float cz in new[] { mid - 1f, mid + 1f })
                {
                    P(parent, In(3.2f, 0.45f, cz), new Vector3(0.6f, 0.5f, 0.6f), steel);
                    P(parent, In(3.2f, 0.75f, cz), new Vector3(0.62f, 0.12f, 0.62f), Goods(0));
                    P(parent, In(3.55f, 1.1f, cz), new Vector3(0.1f, 0.7f, 0.6f), Goods(0));
                }
            else
            {
                P(parent, In(2.1f, 0.8f, mid), new Vector3(1.0f, 0.06f, Mathf.Min(2.4f, w * 0.5f)), shelf);
                P(parent, In(2.1f, 0.4f, mid), new Vector3(0.8f, 0.8f, Mathf.Min(2.0f, w * 0.4f)), shelf);
                for (int g = -2; g <= 2; g++)
                    P(parent, In(2.1f + (g & 1) * 0.2f, 0.95f, mid + g * 0.4f), new Vector3(0.22f, 0.25f, 0.28f), type == 0 ? track.foliageMaterial : Goods(type + g + 2));
            }

            // Glazed front: kick plate, three lights between slim mullions, transom bar and head; the door at one end.
            float door = (seed & 1) == 0 ? a + 0.75f : b - 0.75f;
            P(parent, In(0.32f, 0.22f, mid), new Vector3(0.1f, 0.44f, w), steel);
            P(parent, In(0.32f, 3.5f, mid), new Vector3(0.1f, 0.08f, w), steel);
            P(parent, In(0.32f, 4.2f, mid), new Vector3(0.12f, 0.1f, w), steel);
            P(parent, In(0.34f, 3.85f, mid), new Vector3(0.03f, 0.62f, w), transom);
            for (int m = 0; m <= 3; m++)
                P(parent, In(0.32f, 2.1f, a + m * w / 3f), new Vector3(0.1f, 4.2f, 0.08f), steel);
            foreach (float dz in new[] { -0.55f, 0.55f })
                P(parent, In(0.36f, 1.55f, door + dz), new Vector3(0.08f, 3.0f, 0.08f), steel);
            P(parent, In(0.36f, 3.05f, door), new Vector3(0.08f, 0.08f, 1.1f), steel);
            P(parent, In(0.3f, 1.2f, door + 0.35f), new Vector3(0.06f, 0.4f, 0.04f), DashCity.Mat(track, "brassHandle", new Color(.75f, .6f, .3f)));

            // Sign band: a painted board (no lettering) or, on the buff building, a louvred band; barn lamps above.
            if (kind == WindowAtlas.Kind.Buff)
            {
                P(parent, In(0.22f, 4.9f, mid), new Vector3(0.16f, 1.05f, w), board);
                for (float y = 4.46f; y < 5.4f; y += 0.15f)
                    P(parent, In(0.08f, y, mid), new Vector3(0.1f, 0.06f, w), steel);
            }
            else P(parent, In(0.12f, 4.9f, mid), new Vector3(0.24f, 1.05f, w), board);
            int lamps = w > 5f ? 3 : 2;
            for (int l = 0; l < lamps; l++)
            {
                float lz = a + w * (l + 0.5f) / lamps;
                P(parent, In(-0.18f, 5.62f, lz), new Vector3(0.4f, 0.04f, 0.04f), steel);
                P(parent, In(-0.38f, 5.52f, lz), new Vector3(0.04f, 0.2f, 0.04f), steel);
                P(parent, In(-0.38f, 5.38f, lz), new Vector3(0.3f, 0.12f, 0.3f), steel);
                P(parent, In(-0.38f, 5.31f, lz), new Vector3(0.22f, 0.02f, 0.22f), lampLit);
            }

            // Awning on most shops, sloping from under the sign band out over the pavement.
            if (type % 3 != 2)
            {
                int c = (seed >> 6) % AwningColours.Length;
                var canvas = DashCity.Mat(track, "shopAwning" + c, AwningColours[c]);
                KfcStreet.AwningBay(parent, In(0f, 0f, 0f), side, mid, w, 4.28f, 1.5f, 0.8f, canvas, null, false, track);
            }

            // A hanging blade sign with the shop's pictogram, at the end away from the door.
            float bz = (seed & 1) == 0 ? b - 0.5f : a + 0.5f;
            P(parent, In(-0.55f, 3.42f, bz), new Vector3(1.1f, 0.05f, 0.05f), steel);
            foreach (float dz in new[] { -0.3f, 0.3f })
                P(parent, In(-0.78f + dz, 3.4f, bz), new Vector3(0.02f, 0.06f, 0.02f), steel);
            P(parent, In(-0.78f, 2.98f, bz), new Vector3(0.8f, 0.8f, 0.03f), steel);
            var icon = KfcStreet.Icon(track, type, 6);
            KfcStreet.Quad(parent, In(-0.78f, 2.98f, bz - 0.022f), Quaternion.identity, 0.72f, 0.72f, icon, false);
            KfcStreet.Quad(parent, In(-0.78f, 2.98f, bz + 0.022f), Quaternion.Euler(0f, 180f, 0f), 0.72f, 0.72f, icon, false);

            // Flower planter under the window on some shops.
            if ((seed & 4) != 0)
            {
                float pz = door < mid ? mid + w * 0.15f : mid - w * 0.15f, pl = w * 0.45f;
                P(parent, In(-0.28f, 0.42f, pz), new Vector3(0.5f, 0.45f, pl), DashCity.Mat(track, "planterBlack", new Color(.08f, .08f, .09f)));
                P(parent, In(-0.28f, 0.75f, pz), new Vector3(0.46f, 0.28f, pl - 0.06f), track.foliageMaterial);
                for (int f = 0; f < 6; f++)
                    P(parent, In(-0.28f + ((f & 1) - 0.5f) * 0.2f, 0.92f, pz - pl * 0.4f + f * pl * 0.16f), new Vector3(0.12f, 0.1f, 0.12f), Goods(f));
            }

            if (PlayerProfile.Graphics > 0 && (seed & 2) == 0)
                DistrictLife.Instance?.Occupant(parent, In(2.4f, 0.23f, mid + 0.4f), side > 0 ? 90f : -90f, false, seed);
        }

        // ------------------------------------------------------------------ the Toronto row behind the street

        static readonly Color[] CondoTints = { new(1f, 1f, 1f), new(.9f, 1.02f, 1.05f), new(1.04f, 1.02f, .95f), new(.86f, .93f, 1f) };
        static readonly Color[] OfficeTints = { new(1f, 1f, 1f), new(.86f, .95f, 1.08f), new(.92f, 1.02f, 1.02f) };

        /// A set-back mechanical penthouse on a condo roof, its top band lit warm white on some (Toronto condos light
        /// their crowns at night).
        static void CondoCrown(TrackSpawner track, Transform parent, Vector3 roofCentre, float width, float len, int h)
        {
            var white = DashCity.Mat(track, "condoCrownWhite", new Color(.8f, .82f, .82f));
            P(parent, roofCentre + Vector3.up * 2.5f, new Vector3(width * .62f, 5f, len * .62f), white);
            P(parent, roofCentre + Vector3.up * .5f, new Vector3(width + .3f, 1f, len + .3f), white); // parapet
            if ((h & 1) == 0)
                P(parent, roofCentre + Vector3.up * 4.85f, new Vector3(width * .62f + .12f, .3f, len * .62f + .12f),
                    DetailCull.MarkNoShadow(DashCity.Mat(track, "condoCrownLight", new Color(1f, .88f, .7f), 1.6f)));
        }

        /// The taller row behind the street buildings: glass condo towers (most of downtown Toronto's skyline), TD
        /// Centre-style black steel slabs and glass offices with stepped tops. Some carry a big KFC LED screen.
        static void MidTower(TrackSpawner track, Transform parent, float side, float z, int h)
        {
            float x = side * (29f + h % 7);
            float width = 12f + (h >> 2) % 6, len = 14f + (h >> 6) % 5;
            float mid = z + 10f, height;
            int style = (h >> 5) % 5; // 0-2 condo, 3 black steel, 4 glass office
            if (style <= 2)
            {
                height = 54f + (h >> 4) % 70;
                var facade = WindowAtlas.Facade(track, WindowAtlas.Kind.Condo, CondoTints[(h >> 3) % CondoTints.Length], 1.2f);
                P(parent, new Vector3(x, height * 0.5f, mid), new Vector3(width, height, len), facade);
                CondoCrown(track, parent, new Vector3(x, height, mid), width, len, h >> 8);
            }
            else if (style == 3)
            {
                height = 48f + (h >> 4) % 50;
                var facade = WindowAtlas.Facade(track, WindowAtlas.Kind.Mies, Color.white, 1.1f);
                var black = DashCity.Mat(track, "miesBlackSteel", new Color(.035f, .035f, .04f));
                P(parent, new Vector3(x, height * 0.5f, mid), new Vector3(width, height, len), facade);
                P(parent, new Vector3(x, height + 0.6f, mid), new Vector3(width + 0.2f, 1.2f, len + 0.2f), black);
            }
            else
            {
                height = 46f + (h >> 4) % 56;
                var kind = (h >> 7) % 2 == 0 ? WindowAtlas.Kind.Ribbon : WindowAtlas.Kind.Curtain;
                var facade = WindowAtlas.Facade(track, kind, OfficeTints[(h >> 3) % OfficeTints.Length], 1.3f);
                P(parent, new Vector3(x, height * 0.5f, mid), new Vector3(width, height, len), facade);
                P(parent, new Vector3(x, height + 0.2f, mid), new Vector3(width + 0.3f, 0.4f, len + 0.3f), DashCity.Mat(track, "roofSlab", new Color(.16f, .15f, .2f)));
                P(parent, new Vector3(x, height + 3f, mid), new Vector3(width * 0.7f, 6f, len * 0.7f), facade);
                if ((h >> 9) % 4 == 1) // a thin grey communications mast on some
                    P(parent, new Vector3(x, height + 10f, mid), new Vector3(0.25f, 8f, 0.25f), DashCity.Mat(track, "mastGrey", new Color(.55f, .56f, .58f)));
            }
            // A big KFC LED screen on the taller ones, near the top where it clears the street roofs (lower down the
            // brick buildings in front hide it), turned 35 degrees toward the street and the oncoming rider.
            if (height >= 75f && (h >> 9) % 2 == 0)
            {
                KfcStreet.TowerScreen(track, parent, new Vector3(x - side * (width * 0.5f + 1f), height - 9f, mid - len * 0.2f),
                    Quaternion.Euler(0f, side > 0 ? 35f : -35f, 0f), 12f, h >> 11);
            }
        }

        /// Facadism (King Street West, The Well): the brick building stays at the street and a glass condo tower
        /// rises set back 5 m behind its facade, over a recessed dark glass reveal storey.
        static void FacadismTower(TrackSpawner track, Transform parent, float side, float z, float face, float top, int h)
        {
            Vector3 In(float d, float y, float pz) => new(face + side * d, y, pz);
            float height = top + 30f + (h >> 14) % 46;
            const float d0 = 5f, d1 = 21f;
            float width = d1 - d0, len = 16f, mid = z + 10f;
            var facade = WindowAtlas.Facade(track, WindowAtlas.Kind.Condo, CondoTints[(h >> 3) % CondoTints.Length], 1.2f);
            var reveal = DashCity.Mat(track, "revealGlass", new Color(.06f, .07f, .08f), .25f);
            P(parent, In((d0 + d1) * 0.5f + 0.6f, top + 1.6f, mid), new Vector3(width - 1.2f, 3.2f, len - 1.2f), reveal);
            float y0 = top + 3.2f;
            P(parent, In((d0 + d1) * 0.5f, (y0 + height) * 0.5f, mid), new Vector3(width, height - y0, len), facade);
            CondoCrown(track, parent, In((d0 + d1) * 0.5f, height, mid), width, len, h >> 6);
        }
    }
}
