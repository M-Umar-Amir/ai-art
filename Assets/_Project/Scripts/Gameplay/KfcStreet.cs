using System.Collections.Generic;
using DallasDash.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DallasDash.Gameplay
{
    /// KFC on Downtown's brick street (client, 25 Nov 2026):
    ///   - "only kfc outlets, remove others, only kfc branding": the only outlet brand is KFC. A free-standing KFC
    ///     restaurant (after the client's reference photo: tan walls and parapet, a red awning with the white wordmark,
    ///     the red/white striped tent over the doors and the cupola tower with the logo panel and striped roof) stands
    ///     on every third block, alternating sides, with a brick building set back behind it to keep the street wall.
    ///   - "kfc billboard and hoarding branding like these references": a rooftop bulletin on steel legs with a
    ///     catwalk and lamps (the Twosdays board on the brick building) on every block, alternating sides, and the
    ///     curved corner screen with a flat red screen under it (KFC Canada at Yonge-Dundas) on the corner building
    ///     the rider meets across every junction.
    /// The ad art is models/kfc-street/make_kfc_art.py (our layouts around the official wordmark and our bucket render)
    /// until the client sends official campaign artwork; any image of the same aspect drops in through the bootstrap.
    public static class KfcStreet
    {
        // ------------------------------------------------------------------ placement

        static int FloorDiv(int a, int b) => Mathf.FloorToInt(a / (float)b);

        /// One KFC outlet every third block (60 m), alternating sides; the first stands 20 m into the run on the right.
        public static bool OutletAt(int block, float side)
        {
            int k = FloorDiv(block, 3);
            return block - k * 3 == 1 && (k & 1) == (side > 0f ? 0 : 1);
        }

        /// A rooftop bulletin every 40 m, alternating sides (every fourth block on each side), so a facadism tower is
        /// never the block just in front of one on its side (it would hide the board's outer end).
        public static bool BillboardAt(int block, float side) => ((block + (side > 0f ? 2 : 0)) & 3) == 0;
        /// A projecting vertical board on the opposite side to each bulletin, so every 40 m has one of each.
        public static bool ProjectingAt(int block, float side) => ((block + (side > 0f ? 0 : 2)) & 3) == 0;

        // ------------------------------------------------------------------ materials and primitives

        static readonly Dictionary<string, Material> mats = new();

        /// A lit ad face (billboard, LED screen, logo panel): the image once (1x1), lit from behind.
        public static Material Ad(TrackSpawner track, Texture texture, float glow = 1.2f, bool twoSided = false)
        {
            if (!texture) return DashCity.Mat(track, "kfcAdFallback", new Color(.89f, 0f, .17f), .8f);
            string key = "ad/" + texture.GetInstanceID() + "/" + glow + "/" + twoSided;
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(track.tintMaterial) { name = "KFC ad " + texture.name };
            BrandVisibility.Backlit(m, texture);
            m.SetColor("_EmissionColor", Color.white * (glow / 0.12f));
            if (twoSided) m.SetFloat("_Cull", (float)CullMode.Off);
            DetailCull.MarkNoShadow(m);
            return mats[key] = m;
        }

        /// A printed surface lit by the scene (awning canvas, the bucket stripes), tiled by `tiling` over the mesh UVs.
        public static Material Canvas(TrackSpawner track, Texture texture, Vector2 tiling, string name, float glow = 0f)
        {
            string key = "canvas/" + name;
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(track.tintMaterial) { name = "KFC " + name };
            m.SetTexture("_BaseMap", texture);
            m.SetTextureScale("_BaseMap", tiling); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetColor("_BaseColor", texture ? Color.white : new Color(.89f, 0f, .17f));
            m.SetColor("_EmissionColor", Color.white * glow);
            m.SetFloat("_EmissionUsesAlbedo", glow > 0f ? 1f : 0f);
            m.SetFloat("_WorldTextureScale", 0f);
            m.SetFloat("_SurfaceStyle", 1f); m.SetFloat("_RimStrength", 0f);
            m.SetFloat("_Smoothness", .28f); m.SetFloat("_Metallic", 0f);
            m.SetTexture("_BumpMap", null); m.SetFloat("_BumpScale", 0f);
            if (m.HasProperty("_RoughnessStrength")) m.SetFloat("_RoughnessStrength", 0f);
            if (m.HasProperty("_WindowInterior")) m.SetFloat("_WindowInterior", 0f);
            if (m.HasProperty("_Wetness")) m.SetFloat("_Wetness", 0f);
            if (glow > 0f) m.EnableKeyword("_EMISSION");
            return mats[key] = m;
        }

        /// One cell of a horizontal icon strip (the unbranded shop pictograms); blade signs use a quad on each side.
        public static Material Icon(TrackSpawner track, int index, int count)
        {
            string key = "icon/" + index;
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(Ad(track, track.shopIcons, 0.55f)) { name = "Shop pictogram " + index };
            m.SetTextureScale("_BaseMap", new Vector2(1f / count, 1f));
            m.SetTextureOffset("_BaseMap", new Vector2(index / (float)count, 0f));
            return mats[key] = m;
        }

        /// A flat face (ad, poster, awning print, pictogram) w x h, visible from its -Z side, merged into the block's
        /// meshes (StreetMesh). `brand` registers it as a KFC mark for the brand-in-view check.
        public static void Quad(Transform owner, Vector3 at, Quaternion facing, float w, float h, Material m, bool brand)
        {
            var world = Matrix4x4.TRS(at, facing, new Vector3(w, h, 1f));
            StreetMesh.Add(QuadMesh(), world, m);
            if (brand) BrandVisibility.Static(owner, StreetMesh.QuadBounds(world), facing * Vector3.back);
        }

        static Mesh quadMesh;
        static Mesh QuadMesh()
        {
            if (quadMesh) return quadMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return quadMesh;
        }

        static void Part(Vector3 at, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube) =>
            StreetMesh.Add(type, at, Quaternion.identity, size, m);

        /// A box under an arbitrary rotation (awnings, wire runs).
        static void Box(Vector3 at, Quaternion rotation, Vector3 size, Material m) =>
            StreetMesh.Add(PrimitiveType.Cube, at, rotation, size, m);

        static Mesh pyramid;
        /// A unit square pyramid (base 1 x 1 at y 0, apex at y 1); each face maps the full texture, so the bucket
        /// stripes run up to the apex like the tower roof on the reference outlet.
        static Mesh Pyramid()
        {
            if (pyramid) return pyramid;
            var apex = new Vector3(0f, 1f, 0f);
            Vector3[] corners = { new(-.5f, 0f, -.5f), new(.5f, 0f, -.5f), new(.5f, 0f, .5f), new(-.5f, 0f, .5f) };
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i], b = corners[(i + 1) % 4];
                var normal = Vector3.Cross(apex - a, b - a).normalized;
                if (Vector3.Dot(normal, (a + b) * 0.5f) < 0f) normal = -normal;
                int s = v.Count;
                v.Add(a); v.Add(b); v.Add(apex);
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(.5f, 1f));
                n.Add(normal); n.Add(normal); n.Add(normal);
                // Unity's front face is the one whose normal is cross(p1 - p0, p2 - p0) (clockwise seen from outside).
                if (Vector3.Dot(Vector3.Cross(b - a, apex - a), normal) > 0f) { t.Add(s); t.Add(s + 1); t.Add(s + 2); }
                else { t.Add(s); t.Add(s + 2); t.Add(s + 1); }
            }
            pyramid = new Mesh { name = "KFC tower roof" };
            pyramid.SetVertices(v); pyramid.SetUVs(0, uv); pyramid.SetNormals(n); pyramid.SetTriangles(t, 0);
            pyramid.RecalculateBounds();
            return pyramid;
        }

        /// A quarter-cylinder band of radius r between heights y0 and y1, around a centre at the origin, wrapping a
        /// building corner from the street face (theta 0, outward -side*x) to the face toward the rider (theta 90, -z).
        /// u runs left to right as the approaching rider reads it.
        static Mesh Arc(float side, float r, float y0, float y1, int segments = 28)
        {
            var v = new Vector3[(segments + 1) * 2];
            var uv = new Vector2[v.Length];
            var n = new Vector3[v.Length];
            var t = new int[segments * 12];
            for (int i = 0; i <= segments; i++)
            {
                float f = i / (float)segments, th = f * Mathf.PI * 0.5f;
                var dir = new Vector3(-side * Mathf.Cos(th), 0f, -Mathf.Sin(th));
                float u = side > 0f ? f : 1f - f;
                v[i * 2] = dir * r + Vector3.up * y0; v[i * 2 + 1] = dir * r + Vector3.up * y1;
                uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
                n[i * 2] = n[i * 2 + 1] = dir;
            }
            for (int i = 0, k = 0; i < segments; i++, k += 12)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                // Both windings: the material is two-sided anyway, and this keeps the band solid from any angle.
                t[k] = a; t[k + 1] = b; t[k + 2] = c; t[k + 3] = c; t[k + 4] = b; t[k + 5] = d;
                t[k + 6] = a; t[k + 7] = c; t[k + 8] = b; t[k + 9] = c; t[k + 10] = d; t[k + 11] = b;
            }
            var mesh = new Mesh { name = "Curved corner screen" };
            mesh.vertices = v; mesh.uv = uv; mesh.normals = n; mesh.triangles = t;
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------ the KFC outlet

        /// The free-standing KFC restaurant on a 20 m lot (z .. z+20) on `side`, street face at `face`.
        public static void Outlet(TrackSpawner track, Transform parent, float side, float z, float face, int h)
        {
            Vector3 In(float d, float y, float pz) => new(face + side * d, y, pz);
            var toStreet = Quaternion.Euler(0f, side * 90f, 0f);
            float z0 = z + 2.5f, z1 = z + 17.5f, mid = z + 10f, len = z1 - z0;

            var stucco = DashCity.Mat(track, "kfcStucco", new Color(.86f, .79f, .66f));
            var taupe = DashCity.Mat(track, "kfcTaupe", new Color(.55f, .49f, .44f));
            var cap = DashCity.Mat(track, "kfcCap", new Color(.8f, .79f, .76f));
            var plinth = DashCity.Mat(track, "kfcPlinth", new Color(.4f, .38f, .37f));
            var frameW = DashCity.Mat(track, "kfcFrame", new Color(.88f, .89f, .91f));
            frameW.SetFloat("_Metallic", .5f); frameW.SetFloat("_Smoothness", .6f);
            var red = DashCity.Mat(track, "kfcRed", new Color(.89f, 0f, .17f));
            var white = DashCity.Mat(track, "kfcWhite", new Color(.95f, .95f, .94f));
            var steel = DashCity.Mat(track, "roofSteel", new Color(.16f, .18f, .21f));
            var floor = DetailCull.MarkDetail(DashCity.Mat(track, "kfcFloorTile", new Color(.74f, .72f, .7f)));
            var ceiling = DetailCull.MarkDetail(DashCity.Mat(track, "kfcCeiling", new Color(.92f, .9f, .87f), .18f));
            var panel = DetailCull.MarkDetail(DashCity.Mat(track, "shopLightPanel", new Color(1f, .93f, .82f), 2.2f));
            var menu = DetailCull.MarkDetail(DashCity.Mat(track, "kfcMenuBoard", new Color(.12f, .03f, .04f), .5f));
            var photo = DetailCull.MarkDetail(DashCity.Mat(track, "shopMenuPhoto", new Color(1f, .62f, .25f), 1.2f));
            var menuLine = DetailCull.MarkDetail(DashCity.Mat(track, "shopMenuLine", new Color(1f, .96f, .88f), 1.4f));
            var chair = DetailCull.MarkDetail(DashCity.Mat(track, "kfcChair", new Color(.75f, .05f, .12f)));
            var yellow = DashCity.Mat(track, "bollardYellow", new Color(.95f, .74f, .05f));
            var spill = DetailCull.MarkNoShadow(DashCity.Mat(track, "shopSpill", new Color(1f, .78f, .5f), .35f));
            var awning = Canvas(track, track.kfcAwning, Vector2.one, "awning");
            var stripes = Canvas(track, track.kfcStripes, new Vector2(2f, 1f), "stripes tent");
            var roofStripes = Canvas(track, track.kfcStripes, Vector2.one, "stripes roof");

            // Lot: paved forecourts either side of the restaurant (the neighbours' brick end walls show across them).
            foreach (var (a, b) in new[] { (z + 0.3f, z0), (z1, z + 19.7f) })
                Part(In(6f, 0.1f, (a + b) * 0.5f), new Vector3(12f, 0.2f, b - a), track.sidewalkMaterial);

            // Shell: kitchen/back-of-house volume, end walls, roof slab; the front 6 m is the glazed dining room.
            Part(In(9f, 2.2f, mid), new Vector3(6f, 4.4f, len), stucco);
            foreach (float ez in new[] { z0 + 0.15f, z1 - 0.15f })
                Part(In(3f, 2.2f, ez), new Vector3(6f, 4.4f, 0.3f), stucco);
            Part(In(6f, 4.45f, mid), new Vector3(12f, 0.3f, len), cap);
            // Street elevation over the glazing: tan fascia, the taupe parapet band and its light cap (as on the photo).
            Part(In(0.25f, 3.6f, mid), new Vector3(0.5f, 1.2f, len + 0.1f), stucco);
            Part(In(0.2f, 4.75f, mid), new Vector3(0.6f, 1.1f, len + 0.2f), taupe);
            Part(In(0.15f, 5.36f, mid), new Vector3(0.75f, 0.12f, len + 0.3f), cap);
            foreach (float ez in new[] { z0 + 0.1f, z1 - 0.1f })
                Part(In(6f, 4.95f, ez), new Vector3(12f, 0.7f, 0.2f), taupe);
            Part(In(11.9f, 4.95f, mid), new Vector3(0.2f, 0.7f, len), taupe);

            // Glazed front: plinth either side of the doors, white mullions every 1.5 m, transom and head.
            foreach (var (a, b) in new[] { (z0, mid - 1.1f), (mid + 1.1f, z1) })
                Part(In(0.2f, 0.25f, (a + b) * 0.5f), new Vector3(0.4f, 0.5f, b - a), plinth);
            for (float mz = z0 + 0.1f; mz <= z1 - 0.05f; mz += 1.5f)
                if (Mathf.Abs(mz - mid) > 1.15f) Part(In(0.1f, 1.75f, mz), new Vector3(0.12f, 2.5f, 0.1f), frameW);
            foreach (float y in new[] { 0.52f, 2.35f, 2.98f })
                Part(In(0.1f, y, mid), new Vector3(0.12f, 0.08f, len), frameW);
            // Double doors with red push bars and the logo on the glass.
            foreach (float dz in new[] { -1.05f, 0f, 1.05f })
                Part(In(0.12f, 1.2f, mid + dz), new Vector3(0.1f, 2.35f, 0.08f), frameW);
            foreach (float dz in new[] { -0.52f, 0.52f })
            {
                Part(In(0.06f, 1.1f, mid + dz), new Vector3(0.06f, 0.06f, 0.7f), red);
                BrandVisibility.Face(track, parent, 0, "wide", In(0.04f, 1.75f, mid + dz), toStreet, 0.62f, 0f);
            }
            // Window posters inside the glass.
            var poster = Ad(track, track.kfcPoster, 0.9f);
            foreach (float pz in new[] { z0 + 2.2f, z1 - 2.2f })
            {
                Quad(parent, In(0.22f, 1.55f, pz), toStreet, 0.9f, 1.35f, poster, true);
            }

            // Dining room: tiled floor, lit ceiling, the red back wall with the bucket stripes, menu boards, counter.
            Part(In(3f, 0.21f, mid), new Vector3(6f, 0.04f, len - 0.4f), floor);
            Part(In(3f, 3.08f, mid), new Vector3(6f, 0.1f, len - 0.4f), ceiling);
            for (int row = 0; row < 2; row++)
                for (float pz = z0 + 1.2f; pz < z1 - 0.8f; pz += 2f)
                    Part(In(1.4f + row * 2.6f, 3.02f, pz), new Vector3(0.9f, 0.02f, 0.5f), panel);
            Part(In(5.96f, 1.6f, mid), new Vector3(0.06f, 2.8f, len - 0.6f), red);
            for (float sz = mid - 3.75f; sz <= mid + 3.8f; sz += 1f)
                Part(In(5.92f, 1.6f, sz), new Vector3(0.03f, 2.8f, 0.5f), white);
            foreach (float mz in new[] { mid - 2.3f, mid, mid + 2.3f })
            {
                Part(In(5.9f, 2.45f, mz), new Vector3(0.06f, 0.95f, 2.1f), menu);
                Part(In(5.86f, 2.62f, mz - 0.45f), new Vector3(0.02f, 0.45f, 0.95f), photo);
                for (int l = 0; l < 3; l++)
                    Part(In(5.86f, 2.18f + l * 0.16f, mz + 0.55f), new Vector3(0.02f, 0.05f, 0.85f), menuLine);
            }
            BrandVisibility.Face(track, parent, 0, "wide", In(5.88f, 1.45f, z0 + 1.9f), toStreet, 2.2f, 0f);
            Part(In(4.5f, 0.6f, mid), new Vector3(0.7f, 1.0f, 7f), red);
            Part(In(4.5f, 1.13f, mid), new Vector3(0.85f, 0.06f, 7.2f), white);
            foreach (float tz in new[] { mid - 1.5f, mid + 1.5f })
                Part(In(4.5f, 1.32f, tz), new Vector3(0.3f, 0.3f, 0.38f), steel);
            // Tables and red chairs along the window.
            foreach (float tz in new[] { z0 + 1.6f, z0 + 3.9f, z1 - 3.9f, z1 - 1.6f })
            {
                Part(In(1.25f, 0.76f, tz), new Vector3(0.75f, 0.05f, 0.75f), white);
                Part(In(1.25f, 0.48f, tz), new Vector3(0.08f, 0.55f, 0.08f), steel);
                foreach (float cz in new[] { -0.62f, 0.62f })
                {
                    Part(In(1.25f, 0.48f, tz + cz), new Vector3(0.42f, 0.06f, 0.42f), chair);
                    Part(In(1.25f, 0.75f, tz + cz * 1.3f), new Vector3(0.42f, 0.5f, 0.05f), chair);
                }
            }

            // Awnings: a red canvas bay with the white wordmark either side, the striped tent over the doors.
            AwningBay(parent, In(0f, 0f, 0f), side, (z0 + 0.2f + mid - 2.05f) * 0.5f, mid - 2.05f - z0 - 0.2f, 4.05f, 1.4f, 1.05f, red, awning, true, track);
            AwningBay(parent, In(0f, 0f, 0f), side, (mid + 2.05f + z1 - 0.2f) * 0.5f, z1 - 0.2f - mid - 2.05f, 4.05f, 1.4f, 1.05f, red, awning, true, track);
            AwningBay(parent, In(0f, 0f, 0f), side, mid, 4.1f, 4.55f, 1.65f, 1.5f, stripes, stripes, false, track);
            Part(In(-0.9f, 0.205f, mid), new Vector3(1.8f, 0.01f, len - 1f), spill);

            // The cupola tower over the doors: white body, red trim, the logo panel on three faces, a striped roof.
            float td = 1.75f, half = 1.4f;
            Part(In(td, 6.2f, mid), new Vector3(2.8f, 3.2f, 2.8f), white);
            Part(In(td, 7.86f, mid), new Vector3(3.1f, 0.16f, 3.1f), red);
            var logoPanel = Ad(track, track.kfcPanel, 1.0f);
            var faces = new (Vector3 at, Quaternion rot)[]
            {
                (In(td - half - 0.02f, 6.25f, mid), toStreet),
                (In(td, 6.25f, mid - half - 0.02f), Quaternion.identity),
                (In(td, 6.25f, mid + half + 0.02f), Quaternion.Euler(0f, 180f, 0f)),
            };
            foreach (var (at, rot) in faces)
            {
                Quad(parent, at, rot, 2.2f, 2.2f, logoPanel, true);
            }
            StreetMesh.Add(Pyramid(), Matrix4x4.TRS(In(td, 7.94f, mid), Quaternion.identity, new Vector3(3.5f, 1.9f, 3.5f)), roofStripes);
            Part(In(td, 9.9f, mid), new Vector3(0.14f, 0.3f, 0.14f), white);

            // Roof plant, bollards, bins with the logo, an A-board.
            Part(In(8.5f, 5.2f, z0 + 3f), new Vector3(2.2f, 1.3f, 1.7f), steel);
            Part(In(9.5f, 5.0f, z1 - 3f), new Vector3(1.5f, 0.9f, 1.5f), steel);
            foreach (float bz in new[] { mid - 1.8f, mid + 1.8f, z0 - 0.3f, z1 + 0.3f })
                Part(In(-0.55f, 0.45f, bz), new Vector3(0.2f, 0.45f, 0.2f), yellow, PrimitiveType.Cylinder);
            foreach (float bz in new[] { z0 + 0.6f, z1 - 0.6f })
            {
                Part(In(-0.45f, 0.5f, bz), new Vector3(0.6f, 1.0f, 0.6f), red);
                BrandVisibility.Face(track, parent, 0, "square", In(-0.76f, 0.62f, bz), toStreet, 0.42f, 0.42f);
            }

            // Staff behind the counter, customers at the counter and the tables.
            var life = DistrictLife.Instance;
            if (life)
            {
                float faceStreet = side > 0 ? -90f : 90f, faceBack = side > 0 ? 90f : -90f;
                life.Occupant(parent, In(5.2f, 0.23f, mid - 0.8f), faceStreet, false, h);
                if (PlayerProfile.Graphics > 0)
                {
                    life.Occupant(parent, In(3.7f, 0.23f, mid + 0.4f), faceBack, false, h + 1);
                    life.Occupant(parent, In(1.25f, 0.23f, z0 + 2.4f), 180f, true, h + 2);
                    life.Occupant(parent, In(1.25f, 0.23f, z1 - 2.4f), 0f, true, h + 3);
                }
            }

            // The street wall continues behind the lot: a brick building set back 14 m. (No bulletin on it: 14 m back
            // the buildings in front would hide it; the outlet's own tower and awnings carry KFC here.)
            int block = Mathf.FloorToInt(z / 20f);
            var kind = (h & 1) == 0 ? WindowAtlas.Kind.Loft : WindowAtlas.Kind.Warehouse;
            float top = DowntownArchitecture.Podium + (3 + h / 3 % 3) * DowntownArchitecture.Floor;
            Part(In(20f, top * 0.5f, mid), new Vector3(12f, top, 19.4f), WindowAtlas.Facade(track, kind, Color.white, 1.3f));
            Part(In(19.8f, top + 0.12f, mid), new Vector3(12.5f, 0.3f, 19.7f), DashCity.Mat(track, "copingStone", new Color(.74f, .71f, .64f)));
        }

        /// A shed awning bay along the street face: a canvas box sloping from `topY` at the wall down `drop` over
        /// `reach` toward the street, `length` long, centred at `zc`; with `logo`, the canvas texture is laid on the
        /// slope as its own quad (one logo per bay), with a plain valance at the outer edge.
        public static void AwningBay(Transform parent, Vector3 faceAt, float side, float zc, float length, float topY, float reach, float drop,
            Material body, Material printed, bool logo, TrackSpawner track)
        {
            float slope = Mathf.Sqrt(reach * reach + drop * drop);
            float tilt = 90f - Mathf.Atan2(drop, reach) * Mathf.Rad2Deg;
            var rot = Quaternion.Euler(0f, side * 90f, 0f) * Quaternion.Euler(tilt, 0f, 0f);
            var centre = new Vector3(faceAt.x - side * reach * 0.5f, topY - drop * 0.5f, zc);
            Box(centre, rot, new Vector3(length, slope, 0.05f), body);
            if (logo) Quad(parent, centre + rot * new Vector3(0f, 0f, -0.032f), rot, length, slope, printed, true);
            var valance = new Vector3(faceAt.x - side * (reach + 0.015f), topY - drop - 0.16f, zc);
            Part(valance, new Vector3(0.03f, 0.32f, length), body);
        }

        // ------------------------------------------------------------------ billboards and screens

        /// Client, 25 Nov 2026: "make sure hoardings and billboards are prominent and do not hide behind buildings or
        /// at an off-putting angle". Seen from the middle of the street, anything behind the facade line is hidden by
        /// the buildings in front unless it stands above their roofs. So the rooftop bulletin (12 m x 3.6 m, the
        /// Twosdays board on the reference) stands on a steel frame with its bottom edge at 24 m, clear of the tallest
        /// street building (22.6 m), its inner end over the pavement 1.3 m inside the facade line, turned only 22
        /// degrees from square-on to the oncoming rider. It reads nearly face-on from 170 m down to about 60 m.
        public const float BillboardBottom = 24f;
        public static void RooftopBillboard(TrackSpawner track, Transform parent, float side, float z, float face, float roof, int block)
        {
            var steel = DashCity.Mat(track, "billboardSteel", new Color(.11f, .12f, .14f));
            var grating = DashCity.Mat(track, "billboardGrating", new Color(.2f, .21f, .23f));
            var lamp = DetailCull.MarkNoShadow(DashCity.Mat(track, "billboardLamp", new Color(1f, .9f, .72f), 2.6f));
            float w = 12f, hgt = w * 614f / 2048f;
            float bottom = Mathf.Max(roof + 2.2f, BillboardBottom);
            var pivotRot = Quaternion.Euler(0f, side * 22f, 0f);
            var outward = side * (pivotRot * Vector3.right); // along the board, away from the street
            var inner = new Vector3(face - side * 1.3f, bottom + hgt * 0.5f, z + 10f);
            var pivotAt = inner + outward * (w * 0.5f);
            void L(Vector3 local, Vector3 size, Material m) => StreetMesh.Add(PrimitiveType.Cube, pivotAt + pivotRot * local, pivotRot, size, m);
            L(new Vector3(0f, 0f, 0.16f), new Vector3(w + 0.3f, hgt + 0.3f, 0.24f), steel);
            for (int i = 0; i < 5; i++)
                L(new Vector3(-w * 0.5f + i * w / 4f, 0f, 0.36f), new Vector3(0.14f, hgt + 0.2f, 0.18f), steel);
            // The frame down to the roof: two legs and two back props, tied every 3 m.
            float legH = pivotAt.y - hgt * 0.5f - roof;
            foreach (float lx in new[] { -w * 0.3f, w * 0.3f })
            {
                L(new Vector3(lx, -hgt * 0.5f - legH * 0.5f, 0.4f), new Vector3(0.32f, legH, 0.32f), steel);
                L(new Vector3(lx, -hgt * 0.5f - legH * 0.5f, 2.2f), new Vector3(0.22f, legH, 0.22f), steel);
                for (float ty = 3f; ty < legH - 0.5f; ty += 3f)
                    L(new Vector3(lx, -hgt * 0.5f - ty, 1.3f), new Vector3(0.12f, 0.12f, 1.8f), steel);
            }
            for (float ty = 3f; ty < legH - 0.5f; ty += 6f)
                L(new Vector3(0f, -hgt * 0.5f - ty, 0.4f), new Vector3(w * 0.6f, 0.12f, 0.12f), steel);
            // Catwalk under the face with a rail, and the lamp heads lighting the board from below.
            float deckY = -hgt * 0.5f - 0.95f;
            L(new Vector3(0f, deckY, -0.45f), new Vector3(w, 0.06f, 1.0f), grating);
            L(new Vector3(0f, deckY + 0.85f, -0.93f), new Vector3(w, 0.05f, 0.05f), steel);
            for (int i = 0; i < 7; i++)
                L(new Vector3(-w * 0.5f + i * w / 6f, deckY + 0.42f, -0.93f), new Vector3(0.05f, 0.85f, 0.05f), steel);
            for (int i = 0; i < 5; i++)
            {
                float lx = -w * 0.4f + i * w * 0.2f;
                L(new Vector3(lx, deckY + 0.3f, -0.75f), new Vector3(0.05f, 0.6f, 0.05f), steel);
                L(new Vector3(lx, deckY + 0.62f, -0.75f), new Vector3(0.36f, 0.14f, 0.22f), lamp);
            }
            var art = (block & 4) == 0 ? track.kfcAdRed : track.kfcAdDark;
            Quad(parent, pivotAt + pivotRot * new Vector3(0f, 0f, 0.02f), pivotRot, w, hgt, Ad(track, art, 1.25f), true);
        }

        /// A double-sided vertical billboard projecting square from the facade over the pavement, between the first
        /// floor and the roof, at the block's far pier (clear of balconies and fire escapes). It stands inside the
        /// street canyon, so nothing along the street front hides it, and it faces the rider square-on all the way up
        /// the street. Outer edge 4.9 m from the centre line: over the pavement, never over the road.
        public static void ProjectingBillboard(TrackSpawner track, Transform parent, float side, float z, float face, float top)
        {
            float y0 = DowntownArchitecture.Podium + 2.2f;
            float w = Mathf.Abs(face) - 0.25f - 4.9f;
            float hgt = Mathf.Min(w * 768f / 512f, top - 0.7f - y0);
            if (hgt < 2.4f) return;
            w = hgt * 512f / 768f;
            float zc = z + 19.75f;
            var centre = new Vector3(face - side * (0.25f + w * 0.5f), y0 + hgt * 0.5f, zc);
            var steel = DashCity.Mat(track, "billboardSteel", new Color(.11f, .12f, .14f));
            Part(centre, new Vector3(w + 0.24f, hgt + 0.24f, 0.26f), steel);
            foreach (float ay in new[] { hgt * 0.5f - 0.3f, -hgt * 0.5f + 0.3f })
                Part(new Vector3(face - side * 0.2f, centre.y + ay, zc), new Vector3(0.5f, 0.14f, 0.14f), steel); // wall brackets
            var poster = Ad(track, track.kfcPoster, 1.1f);
            Quad(parent, centre + new Vector3(0f, 0f, -0.135f), Quaternion.identity, w, hgt, poster, true);
            Quad(parent, centre + new Vector3(0f, 0f, 0.135f), Quaternion.Euler(0f, 180f, 0f), w, hgt, poster, false);
        }

        /// The corner LED screen (KFC Canada at Yonge-Dundas): a curved screen wrapping the near corner of the building
        /// across the junction, from the street face round to the face toward the rider, over the shop storey; and a
        /// flat red screen under it on the rider-facing face. The building's corner is rounded to match
        /// (DowntownArchitecture.BrickBody).
        public const float CornerRadius = 5f;
        public static void CornerScreen(TrackSpawner track, Transform parent, float side, float z, float face, float top)
        {
            float z0 = z + 0.3f, r = CornerRadius + 0.35f;
            float y0 = DowntownArchitecture.Podium + 2.6f;
            float arc = Mathf.PI * 0.5f * r, hgt = Mathf.Min(arc * 1400f / 1536f, top - 1.2f - y0);
            var centre = new Vector3(face + side * CornerRadius, 0f, z0 + CornerRadius);
            var screenMesh = Arc(side, r, y0, y0 + hgt);
            var bounds = screenMesh.bounds; bounds.center += centre;
            StreetMesh.Add(screenMesh, Matrix4x4.Translate(centre), Ad(track, track.kfcAdPink, 1.35f, true), true);
            BrandVisibility.Static(parent, bounds, new Vector3(-side * 0.5f, 0f, -0.87f));
            var housing = DashCity.Mat(track, "screenHousing", new Color(.06f, .065f, .08f));
            housing.SetFloat("_Cull", (float)CullMode.Off);
            StreetMesh.Add(Arc(side, r + 0.04f, y0 + hgt, y0 + hgt + 0.35f), Matrix4x4.Translate(centre), housing, true);
            StreetMesh.Add(Arc(side, r + 0.04f, y0 - 0.3f, y0), Matrix4x4.Translate(centre), housing, true);
            // The flat red screen under it, on the face toward the rider, beside the curve.
            float bw = 6f, bh = bw / 4f, bd = CornerRadius + 0.6f + bw * 0.5f;
            var at = new Vector3(face + side * bd, DowntownArchitecture.Podium + 0.25f + bh * 0.5f, z0 - 0.07f);
            Part(at + new Vector3(0f, 0f, 0.05f), new Vector3(bw + 0.3f, bh + 0.3f, 0.1f), housing);
            Quad(parent, at, Quaternion.identity, bw, bh, Ad(track, track.kfcAdBanner, 1.3f), true);
        }

        /// A flat LED ad screen on a skyline tower (replacing the old holograms), w wide at the ad's aspect.
        public static void TowerScreen(TrackSpawner track, Transform owner, Vector3 at, Quaternion facing, float w, int index)
        {
            var art = (index & 1) == 0 ? track.kfcAdRed : track.kfcAdDark;
            float hgt = w * 614f / 2048f;
            var housing = DashCity.Mat(track, "screenHousing", new Color(.06f, .065f, .08f));
            StreetMesh.Add(PrimitiveType.Cube, at + facing * new Vector3(0f, 0f, 0.17f), facing, new Vector3(w + 0.4f, hgt + 0.4f, 0.3f), housing);
            Quad(owner, at, facing, w, hgt, Ad(track, art, 1.3f), true);
        }

        // ------------------------------------------------------------------ string lights

        /// Festoon lights strung across the street between the brick facades (Bishop Arts), in place of the futuristic
        /// skybridges: two sagging strands per block, 7.6 m up at the facades.
        public static void StringLights(TrackSpawner track, Transform parent, float z)
        {
            var bulb = DetailCull.MarkNoShadow(DashCity.Mat(track, "festoonBulbWarm", new Color(1f, .7f, .36f), 1.7f));
            var wire = DetailCull.MarkNoShadow(DashCity.Mat(track, "festoonWire", new Color(.05f, .05f, .05f)));
            StreetMesh.Begin(parent);
            try
            {
                foreach (float sz in new[] { z + 5f, z + 14f })
                {
                    const int n = 16;
                    Vector3 prev = default;
                    for (int i = 0; i <= n; i++)
                    {
                        float t = i / (float)n;
                        var p = new Vector3(-8.3f + 16.6f * t, 7.6f - 3.6f * t * (1f - t), sz + 2.5f * (t - 0.5f));
                        if (i > 0)
                        {
                            var d = p - prev;
                            Box((p + prev) * 0.5f, Quaternion.LookRotation(d), new Vector3(0.025f, 0.025f, d.magnitude), wire);
                            if (i < n) Part(p + Vector3.down * 0.07f, new Vector3(0.07f, 0.1f, 0.07f), bulb);
                        }
                        prev = p;
                    }
                }
            }
            finally { StreetMesh.End(); }
        }
    }
}
