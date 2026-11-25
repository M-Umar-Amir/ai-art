using System.Collections.Generic;
using DallasDash.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DallasDash.Gameplay
{
    /// Toronto's skyline on the horizon ahead (client, 25 Nov 2026: "CN Tower should be visible in background").
    ///
    /// The CN Tower and the Financial District are modelled in real metres, then drawn as a scale model inside the
    /// 240 m draw distance. Every frame the group is placed by a homothety about the camera (scale S, real distance
    /// RealDistance), so it covers exactly the view angles a skyline 2.3 km away would, with no parallax error when
    /// the rider changes lane or jumps. The materials use the toon shader's _Backdrop mode: no curved-world bend, no
    /// distance fog, a fixed dusk haze that thickens toward the feet. The group follows the street's sway so the
    /// tower stays at the end of the street, as it does down Front Street and King Street.
    ///
    /// The street end (26 Nov 2026, client: no pink street buildings under the towers): ahead, the curved world
    /// drops the road over a crest about 7 degrees below the horizontal, so the skyline stands on that crest rather
    /// than on the true horizon. It is drawn as a thin card 95 m out, and the street beyond it fades into the exact
    /// colour of the dusk sky (FadeStart..FadeEnd), whose horizon drops with the skyline (DuskSky _HorizonDip). What
    /// shows past the road's end is the skyline from its feet up, a continuous podium row along its front and sky.
    ///
    /// CN Tower (553.3 m): Y-section shaft of three tapering legs on a hexagonal core, merging below the main pod;
    /// main pod with the microwave radome ring (338 m), two outward-sloping glass bands (LookOut 346 m, 360
    /// Restaurant 351 m) and the EdgeWalk ledge (356 m); the upper core to the SkyPod (447 m); the 96 m antenna with
    /// red aviation lights. Around it: Royal York, Royal Bank Plaza (gold), Brookfield Place, TD Centre (black),
    /// Commerce Court, First Canadian Place (white), Scotia Plaza (red granite), Bay Adelaide, St Regis, Aura and
    /// glass condo towers, the Rogers Centre dome and a continuous podium row along the front. No logos on any of
    /// them (KFC is the only brand in the game). One mesh, ~10k triangles.
    public sealed class TorontoSkyline : MonoBehaviour
    {
        public const float RealDistance = 2300f;
        /// The street fades into the dusk sky between these distances along the street (ToonCommon FarSkyFade), so
        /// nothing of it shows past the skyline: its far end, fogged pink, used to show under and between the towers
        /// (client, 25 Nov 2026: "the pink street buildings still show at bottom"; scales 0.087 and 0.0543 had it in
        /// front of the skyline, 0.0304 behind it but in the 7 degrees between the skyline's feet and the crest).
        public const float FadeStart = 100f, FadeEnd = 124f;
        /// The skyline is drawn as a thin card this far out (ToonCommon _BackdropCard): the street nearer than that
        /// draws over it, the fading street beyond is hidden behind it. Hurdles and burgers further out still draw
        /// in front of it (they are tagged in TrackSpawner.Register).
        public const float CardDistance = 95f;
        const float CardThickness = 3f;
        /// The front of the South Core podium row, the skyline's nearest part (real metres from its origin).
        const float NearZ = -170f;
        /// Scale of the model: 124-157 m out, inside the 240 m draw distance (where it lies only matters for
        /// culling; the card sets its depth).
        public const float S = 124f / (RealDistance + NearZ);
        const float ModelNear = 124f, ModelFar = 160f;
        /// Every building runs this far (real metres) below its ground line, so no base ever shows a floating edge.
        const float Sink = 150f;
        /// The skyline follows the curved world's sway (CurvedWorld: x += d^2 * sway, so an angle of d * sway) as
        /// the street does where it meets the card.
        const float SwayDistance = CardDistance;
        /// The skyline's ground line sits this far (radians) under the road's crest, which hides its foot.
        const float CrestMargin = 0.004f;
        static readonly int CurveParams = Shader.PropertyToID("_CurveParams");
        static readonly int FarFade = Shader.PropertyToID("_FarFade");
        static readonly int HorizonDip = Shader.PropertyToID("_HorizonDip");
        static readonly int BackdropHaze = Shader.PropertyToID("_BackdropHazeColor");
        static readonly int BackdropCard = Shader.PropertyToID("_BackdropCard");
        /// The street's far fade is on (BrandVisibility counts only boards in front of it).
        public static bool FarFadeOn { get; private set; }

        static TorontoSkyline instance;
        public static TorontoSkyline Instance => instance;
        /// The CN Tower's position in the model (real metres from the skyline origin on the street's centre line).
        public static readonly Vector3 TowerAt = new(-120f, 0f, 150f);
        public const float TowerHeight = 553.3f;

        Transform root;
        TrackSpawner track;
        bool behind;
        readonly Dictionary<Material, List<int>> tris = new();
        readonly List<Vector3> verts = new();
        readonly List<Vector3> normals = new();
        readonly List<Vector2> uvs = new();
        readonly List<Material> order = new();

        public static void Ensure(TrackSpawner owner)
        {
            if (instance) return;
            if (!owner || !owner.tintMaterial) return;
            instance = new GameObject("Toronto skyline / CN Tower").AddComponent<TorontoSkyline>();
            instance.Build(owner);
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += Place;
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= Place; Globals(false); }
        void OnDestroy() { if (instance == this) instance = null; }

        /// The street's far fade, the sky's horizon dip and the skyline's haze colour, all off away from the skyline.
        static void Globals(bool on)
        {
            FarFadeOn = on;
            Shader.SetGlobalVector(FarFade, on ? new Vector4(FadeStart, 1f / (FadeEnd - FadeStart), 1f, 0f) : Vector4.zero);
            if (!on) Shader.SetGlobalFloat(HorizonDip, 0f);
            Shader.SetGlobalVector(BackdropCard, on ? new Vector4(CardDistance, ModelNear, CardThickness / (ModelFar - ModelNear), 1f) : Vector4.zero);
            // A cool dusk blue-violet: the far city reads as a dark silhouette with lit windows against the warm
            // glow, not as pastel pink slabs (the street's own fog colour is a pink-purple).
            Shader.SetGlobalColor(BackdropHaze, on ? new Color(.2f, .19f, .34f, 1f) : Color.clear);
            // The street melts into exactly the skybox's colours (DuskSkyCommon.hlsl).
            var sky = RenderSettings.skybox;
            if (on && sky && sky.HasProperty("_Zenith"))
            {
                foreach (var name in new[] { "_Zenith", "_Band", "_Horizon", "_Sunset", "_Ground" })
                    Shader.SetGlobalColor("_Dusk" + name.Substring(1), sky.GetColor(name));
                Shader.SetGlobalVector("_DuskSunDir", sky.GetVector("_SunDir"));
                Shader.SetGlobalFloat("_DuskExposure", sky.GetFloat("_Exposure"));
            }
            else if (on) Shader.SetGlobalVector(FarFade, Vector4.zero); // no dusk sky to melt into: no fade
        }

        public bool Shown => root && root.gameObject.activeSelf;

        // ------------------------------------------------------------------ review key (DashReview -dash-skyline)

        Material keyMaterial;
        readonly Dictionary<Renderer, Material[]> played = new();

        /// Review only: draws the whole skyline flat green (no haze), so a capture shows exactly where it covers
        /// the street end and where the street's own far buildings or the sky show through.
        public void Key(bool on)
        {
            if (!root) return;
            if (on && !keyMaterial)
            {
                keyMaterial = Mat("reviewKey", Color.black, new Color(0f, 1f, .1f), .001f, 0f);
                keyMaterial.SetFloat("_BackdropFeet", 0f);
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (on)
                {
                    if (!played.ContainsKey(r)) played[r] = r.sharedMaterials;
                    var keyed = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < keyed.Length; i++) keyed[i] = keyMaterial;
                    r.sharedMaterials = keyed;
                }
                else if (played.TryGetValue(r, out var materials)) r.sharedMaterials = materials;
            }
            if (!on) played.Clear();
        }

        // ------------------------------------------------------------------ placement

        void LateUpdate()
        {
            var view = Camera.main;
            bool show = view && DashRun.District == 0 && view.farClipPlane > 200f && view.transform.position.x < 1000f;
            if (root && root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
            if (FarFadeOn != (show && root)) Globals(show && root);
        }

        /// Placed just before the main camera renders (after Cinemachine has moved it), so it never lags a frame.
        void Place(ScriptableRenderContext context, Camera view)
        {
            if (!root || !root.gameObject.activeSelf || view != Camera.main) return;
            Vector3 c = view.transform.position;
            // The street is endless both ways, so the skyline stands at whichever end the camera looks down: ahead in
            // the run, behind the rider on the title and crew shots (the menu camera faces back along the street).
            float fz = view.transform.forward.z;
            if (fz < -0.25f) behind = true; else if (fz > 0.25f) behind = false;
            var curve = Shader.GetGlobalVector(CurveParams);
            // The bend only applies ahead of the camera. There the road drops away (y -= d^2 * down): for a camera
            // h metres above it, its crest, where the street visibly ends, lies 2 * sqrt(down * h) below the
            // horizontal (at d = sqrt(h / down), about 68 m). The skyline stands on that crest, as a city 2.3 km
            // off stands on the horizon. Behind (title and crew shots) the street runs on unbent.
            // Behind, where the road meets the card, a degree or so down.
            float ahead = behind ? 0f : Mathf.InverseLerp(.25f, .6f, fz);
            float h = Mathf.Max(c.y, .5f);
            float crest = Mathf.Lerp(Mathf.Atan(h / CardDistance), 2f * Mathf.Sqrt(Mathf.Max(curve.x, 0f) * h), ahead) + CrestMargin;
            // Where the curved world has swung the street end, the skyline goes too (small-angle: angle = d * sway).
            float sway = behind ? 0f : curve.y * SwayDistance;
            var origin = new Vector3(sway * RealDistance, c.y - RealDistance * Mathf.Tan(crest), c.z + (behind ? -RealDistance : RealDistance));
            root.SetPositionAndRotation(c + (origin - c) * S, behind ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity);
            root.localScale = Vector3.one * S;
            Shader.SetGlobalFloat(HorizonDip, Mathf.Sin(crest));
        }

        // ------------------------------------------------------------------ build

        void Build(TrackSpawner owner)
        {
            track = owner;
            root = new GameObject("Skyline model (real metres x " + S + ")").transform;
            root.SetParent(transform, false);

            var concrete = Mat("cnConcrete", new Color(.8f, .78f, .76f), new Color(.2f, .19f, .24f), .2f, .2f);
            var concreteDark = Mat("cnConcreteShade", new Color(.6f, .58f, .58f), new Color(.1f, .09f, .12f), .28f, .2f);
            var radome = Mat("cnRadome", new Color(.93f, .93f, .94f), new Color(.28f, .27f, .3f), .28f, .35f);
            var podGlass = Mat("cnPodGlass", new Color(.08f, .09f, .12f), new Color(1f, .72f, .42f) * 1.25f, .28f, .85f);
            var skyGlass = Mat("cnSkyPodGlass", new Color(.08f, .09f, .12f), new Color(1f, .78f, .5f) * .9f, .28f, .85f);
            var elevator = Mat("cnElevatorGlass", new Color(.2f, .18f, .16f), new Color(1f, .82f, .55f) * 1.1f, .28f, .8f);
            var antenna = Mat("cnAntenna", new Color(.86f, .86f, .88f), new Color(.16f, .16f, .2f), .28f, .4f);
            var aviation = Mat("cnAviation", new Color(1f, .1f, .08f), new Color(1f, .08f, .05f) * 4f, .1f, .2f);

            CNTower(TowerAt, concrete, concreteDark, radome, podGlass, skyGlass, elevator, antenna);
            District();
            Bake();

            // Red aviation lights at the antenna tip and down the mast, and on the pod roof: separate so they blink.
            foreach (var (y, size, blink) in new[] { (553f, 4.2f, true), (536f, 3.4f, false), (512f, 3.4f, true), (482f, 3.2f, false), (457f, 3.2f, true) })
                AviationLight(TowerAt + Vector3.up * y, size, aviation, blink);
            foreach (float a in new[] { -150f, -30f, 90f })
                AviationLight(TowerAt + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 21.6f, 357.8f, Mathf.Sin(a * Mathf.Deg2Rad) * 21.6f), 2.6f, aviation, false);
        }

        Material Mat(string key, Color albedo, Color emission, float haze, float smooth)
        {
            var m = new Material(track.tintMaterial) { name = "Skyline " + key };
            m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            m.SetColor("_BaseColor", albedo);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_EmissionMapOn", 0f);
            m.SetFloat("_EmissionUsesAlbedo", 0f);
            m.SetFloat("_WorldTextureScale", 0f); // the model moves with the camera: never world-projected
            m.SetFloat("_SurfaceStyle", 1f);
            m.SetFloat("_RimStrength", 0f);
            m.SetFloat("_OutlineWidth", 0f);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", smooth > .6f ? .4f : .05f);
            m.SetFloat("_BumpScale", 0f); m.SetFloat("_RoughnessStrength", 0f);
            m.SetFloat("_Wetness", 0f); m.SetFloat("_WindowInterior", 0f);
            m.SetFloat("_Backdrop", haze);
            m.SetFloat("_BackdropFeet", 90f); // a light ground haze only: the towers must read down to their feet
            return m;
        }

        void AviationLight(Vector3 at, float size, Material m, bool blink)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "Aviation light";
            go.transform.SetParent(root, false);
            go.transform.localPosition = at;
            go.transform.localScale = Vector3.one * size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (blink) go.AddComponent<Blinker>().perSecond = .75f;
        }

        // ------------------------------------------------------------------ CN Tower

        void CNTower(Vector3 at, Material concrete, Material shade, Material radome, Material podGlass, Material skyGlass, Material elevator, Material antenna)
        {
            // Shaft: Y-shaped legs on the hexagonal core, tapering in a slightly concave curve until they merge into
            // the core under the pod; the core carries on to the SkyPod.
            float[] rings = { -Sink, 0f, 6f, 18f, 36f, 60f, 90f, 125f, 165f, 205f, 245f, 280f, 308f, 328f, 340f, 380f, 420f, 440f };
            Vector2[] prev = null;
            float prevY = 0f;
            foreach (float y in rings)
            {
                var sec = Section(y);
                if (prev != null) Loft(at, prev, prevY, sec, y, concrete);
                prev = sec; prevY = y;
            }
            // Glass elevator shaft up the crotch facing the street (lit cabins at dusk), just proud of the face.
            for (int k = 0; k + 1 < rings.Length && rings[k + 1] <= 340f; k++)
            {
                float y0 = Mathf.Max(rings[k], 12f), y1 = rings[k + 1];
                if (y1 <= y0) continue;
                CrotchStrip(at, y0, y1, -30f, 1.6f, elevator);
            }
            // A darker plinth at the foot (the base building), hidden in the haze but solid.
            Lathe(at, new[] { (34f, -Sink), (34f, 9f), (0f, 9f) }, 12, new[] { shade, shade });

            // Main pod, bottom to top.
            var pod = new (float r, float y)[]
            {
                (5f, 318f), (11f, 324f), (18.5f, 331f), (21.5f, 334f), (23.6f, 336.6f), (23.4f, 339f), (21.2f, 340.6f),
                (20.1f, 341.4f), (20.3f, 342f), (21.7f, 346f), (21.7f, 346.7f), (21.1f, 347.1f), (21.4f, 347.4f),
                (22.7f, 352f), (22.7f, 352.7f), (21.9f, 353.1f), (21.9f, 356f), (22.6f, 356.2f), (22.6f, 356.7f),
                (20.6f, 357f), (14f, 359.2f), (5.5f, 361.5f),
            };
            var podMats = new[]
            {
                concrete, concrete, concrete, radome, radome, radome, radome, concrete, podGlass, concrete, concrete,
                concrete, podGlass, concrete, concrete, concrete, concrete, concrete, concrete, concrete, concrete,
            };
            Lathe(at, pod, 48, podMats);

            // SkyPod (447 m) on the upper core.
            var sky = new (float r, float y)[]
            {
                (4.4f, 431f), (7.6f, 437f), (8.3f, 438f), (8.5f, 443.5f), (8.3f, 444.2f), (8.6f, 448.5f), (8.2f, 449.3f),
                (6.6f, 452f), (3.9f, 454.5f), (3.6f, 456f),
            };
            Lathe(at, sky, 24, new[] { concrete, concrete, skyGlass, concrete, skyGlass, concrete, concrete, concrete, concrete });

            // The antenna mast (96 m): stepped sections narrowing to the tip.
            var mast = new (float r, float y)[]
            {
                (3.4f, 456f), (3.1f, 480f), (3.3f, 480.6f), (2.5f, 481.6f), (2.3f, 510f), (2.45f, 510.6f), (1.55f, 511.6f),
                (1.3f, 534f), (1.4f, 534.5f), (.85f, 535.5f), (.45f, 552f), (0f, 553.3f),
            };
            Lathe(at, mast, 12, new[] { antenna, shade, shade, antenna, shade, shade, antenna, shade, shade, antenna, antenna });
        }

        /// The shaft's plan at height y: 15 points, counter-clockwise, legs at -90 (toward the viewer), 30 and 150 deg.
        static Vector2[] Section(float y)
        {
            float w = Mathf.Lerp(4.9f, 3.3f, Mathf.Clamp01(y / 440f));                 // leg half-width
            float crotch = w / Mathf.Sin(60f * Mathf.Deg2Rad);                          // where two leg sides meet
            float t = Mathf.Clamp01(y / 332f);
            float reach = Mathf.Lerp(1.5f, 27f, Mathf.Pow(1f - t, 1.45f));             // leg beyond the core
            float tip = crotch + reach, chamfer = Mathf.Min(1.5f, reach * .45f);
            var pts = new Vector2[15];
            int n = 0;
            for (int i = 0; i < 3; i++)
            {
                float th = (-90f + 120f * i) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(th), Mathf.Sin(th));
                var perp = new Vector2(-dir.y, dir.x);
                float c = th - 60f * Mathf.Deg2Rad;
                pts[n++] = new Vector2(Mathf.Cos(c), Mathf.Sin(c)) * crotch;
                pts[n++] = dir * (tip - chamfer) - perp * w;
                pts[n++] = dir * tip - perp * (w * .45f);
                pts[n++] = dir * tip + perp * (w * .45f);
                pts[n++] = dir * (tip - chamfer) + perp * w;
            }
            return pts;
        }

        /// A thin lit strip on the core face between two legs (crotch at `angle` degrees), from y0 to y1.
        void CrotchStrip(Vector3 at, float y0, float y1, float angle, float halfWidth, Material m)
        {
            Vector2 Corner(float y)
            {
                float w = Mathf.Lerp(4.9f, 3.3f, Mathf.Clamp01(y / 440f));
                return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * (w / Mathf.Sin(60f * Mathf.Deg2Rad));
            }
            // The crotch is a re-entrant corner: the strip is a flat panel across it, set 0.3 m out.
            var a = Corner(y0); var b = Corner(y1);
            var outward = new Vector3(a.x, 0f, a.y).normalized;
            var side = Vector3.Cross(Vector3.up, outward) * halfWidth;
            Vector3 p0 = at + new Vector3(a.x, y0, a.y) + outward * (.3f + halfWidth * .58f);
            Vector3 p1 = at + new Vector3(b.x, y1, b.y) + outward * (.3f + halfWidth * .58f);
            Quad(p0 - side, p0 + side, p1 + side, p1 - side, outward, m);
        }

        // ------------------------------------------------------------------ Financial District and condos

        void District()
        {
            var fcp = Mat("fcpWhite", new Color(.88f, .88f, .86f), new Color(.2f, .19f, .17f), .26f, .3f);
            var scotia = Mat("scotiaGranite", new Color(.48f, .13f, .12f), new Color(.24f, .1f, .06f), .26f, .35f);
            var tdBlack = Mat("tdBlack", new Color(.05f, .05f, .06f), new Color(.2f, .16f, .1f), .26f, .7f);
            var steel = Mat("commerceSteel", new Color(.5f, .52f, .55f), new Color(.14f, .13f, .12f), .26f, .6f);
            var gold = Mat("rbcGold", new Color(.8f, .58f, .22f), new Color(.34f, .22f, .07f), .26f, .8f);
            var blueGlass = Mat("officeGlass", new Color(.28f, .36f, .46f), new Color(.2f, .19f, .18f), .26f, .8f);
            var condo = Mat("condoGlass", new Color(.2f, .34f, .37f), new Color(.3f, .22f, .12f), .28f, .8f);
            var condoBand = Mat("condoSlabEdge", new Color(.82f, .84f, .85f), new Color(.12f, .12f, .13f), .28f, .3f);
            var stone = Mat("royalYorkStone", new Color(.7f, .6f, .48f), new Color(.24f, .17f, .1f), .24f, .2f);
            var copper = Mat("royalYorkCopper", new Color(.28f, .5f, .42f), new Color(.04f, .06f, .05f), .24f, .3f);
            var roof = Mat("roofPlant", new Color(.3f, .3f, .32f), Color.black, .26f, .2f);

            // Royal York: a stepped stone hotel with its green copper chateau roof.
            Box(new Vector3(-60f, 0f, -60f), 70f, 26f, 0f, 100f, stone);
            Box(new Vector3(-60f, 0f, -60f), 36f, 22f, 100f, 112f, stone);
            Pyramid(new Vector3(-60f, 112f, -60f), 36f, 22f, 16f, copper);

            // Royal Bank Plaza: two gold triangular towers with saw-tooth corners.
            Prism(Triangle(new Vector3(-40f, 0f, 20f), 44f, 15f), -Sink, 180f, gold, roof);
            Prism(Triangle(new Vector3(-12f, 0f, 46f), 38f, 195f), -Sink, 112f, gold, roof);
            // Brookfield Place Bay Wellington: blue glass with a slimmer lantern crown.
            Box(new Vector3(10f, 0f, -40f), 44f, 40f, 0f, 190f, blueGlass);
            Box(new Vector3(10f, 0f, -40f), 30f, 28f, 190f, 207f, blueGlass);
            // TD Centre: Mies black steel and bronze glass, flat roofs.
            Box(new Vector3(42f, 0f, 30f), 60f, 37f, 0f, 223f, tdBlack);
            Box(new Vector3(84f, 0f, 74f), 56f, 34f, 0f, 175f, tdBlack);
            // Commerce Court West: stainless steel.
            Box(new Vector3(102f, 0f, -22f), 48f, 40f, 0f, 232f, steel);
            Box(new Vector3(102f, 0f, -22f), 40f, 32f, 232f, 239f, steel);
            // First Canadian Place: white, chamfered square, the tallest office tower.
            Prism(Chamfered(new Vector3(132f, 0f, 52f), 52f, 9f), -Sink, 298f, fcp, roof);
            // Scotia Plaza: red granite saw-tooth with its notched, stepped top.
            Prism(Sawtooth(new Vector3(172f, 0f, -10f), 52f, 7f), -Sink, 255f, scotia, roof);
            Prism(Sawtooth(new Vector3(172f, 0f, -10f), 36f, 6f), 255f, 275f, scotia, roof);
            // Bay Adelaide West: light glass, square.
            Box(new Vector3(212f, 0f, 30f), 46f, 46f, 0f, 218f, blueGlass);
            // St Regis: dark glass with a spire.
            Box(new Vector3(60f, 0f, 165f), 38f, 30f, 0f, 262f, blueGlass);
            Box(new Vector3(60f, 0f, 165f), 1.6f, 1.6f, 262f, 277f, steel);
            // Aura, up Yonge Street: tall glass residential tower with a stepped top.
            Box(new Vector3(330f, 0f, 380f), 40f, 30f, 0f, 250f, condo);
            Box(new Vector3(330f, 0f, 380f), 30f, 22f, 250f, 272f, condo);

            // Glass condo towers (CityPlace and the South Core round the tower's foot, more uptown): teal glass with
            // white slab edges every few floors, a set-back mechanical crown.
            var condos = new (float x, float z, float w, float d, float h)[]
            {
                (-262f, 120f, 26f, 34f, 200f), (-305f, 40f, 24f, 30f, 165f), (-200f, 262f, 28f, 28f, 182f), (-382f, 205f, 26f, 30f, 150f),
                (262f, 120f, 26f, 32f, 192f), (302f, -50f, 24f, 28f, 152f), (422f, 205f, 28f, 30f, 212f), (372f, 60f, 26f, 26f, 168f),
            };
            foreach (var (x, z, w, d, h) in condos) Condo(new Vector3(x, 0f, z), w, d, h, condo, condoBand);

            // CityPlace and Liberty Village to the west, the St Lawrence and East Bayfront towers to the east: a wide
            // field of glass condos, lower toward the edges, so the skyline spans the street's whole opening even at
            // full sway and no sky shows at its feet beside the core.
            for (int i = 0; i < 36; i++)
            {
                float x = -1080f + i * 62f + (Hash01(i, 1) - .5f) * 28f;
                if (x > -300f && x < 450f) continue; // the core above is dense already
                float z = 30f + Hash01(i, 2) * 280f;
                float w = 22f + Hash01(i, 3) * 14f, d = 22f + Hash01(i, 4) * 12f;
                float h = Mathf.Lerp(185f, 85f, Mathf.InverseLerp(300f, 1080f, Mathf.Abs(x))) * (.72f + .42f * Hash01(i, 5));
                Condo(new Vector3(x, 0f, z), w, d, h, Hash01(i, 6) < .3f ? blueGlass : condo, condoBand);
            }

            // Rogers Centre south-west of the CN Tower: the ring wall and the shallow dome roof over the podium row.
            // Dusk grey, not white: lit from above, a pale dome read as a mauve smudge at the street end (26 Nov).
            var dome = Mat("rogersDome", new Color(.38f, .4f, .44f), new Color(.05f, .05f, .07f), .26f, .45f);
            var ring = Mat("rogersWall", new Color(.3f, .29f, .3f), new Color(.1f, .07f, .05f), .26f, .2f);
            var domeAt = new Vector3(-200f, 0f, -40f);
            Lathe(domeAt, new[] { (104f, -Sink), (104f, 31f), (100f, 46f) }, 32, new[] { ring, ring });
            Lathe(domeAt, new[] { (100f, 46f), (88f, 62f), (68f, 75f), (40f, 83f), (0f, 86f) }, 32, new[] { dome });

            // The podium row along the skyline's front (Harbourfront, the Esplanade, the Union Station blocks):
            // mid-rise offices, hotels and warehouses shoulder to shoulder across the whole width, each a metre wider
            // than its slot so no seam opens. It closes the street end: from the crest up there is only city.
            var podium = new[]
            {
                Mat("podiumBrick", new Color(.42f, .26f, .2f), new Color(.16f, .09f, .05f), .26f, .2f),
                Mat("podiumStone", new Color(.62f, .56f, .48f), new Color(.14f, .11f, .08f), .26f, .2f),
                Mat("podiumGlass", new Color(.15f, .19f, .25f), new Color(.34f, .25f, .13f), .26f, .75f),
                Mat("podiumConcrete", new Color(.5f, .5f, .52f), new Color(.1f, .1f, .11f), .26f, .25f),
            };
            int slot = 0;
            for (float x = -1400f; x < 1400f; slot++)
            {
                float w = 38f + Hash01(slot, 11) * 46f, h = 24f + Hash01(slot, 12) * 44f;
                float front = NearZ + Hash01(slot, 13) * 12f;
                var m = podium[Mathf.Min((int)(Hash01(slot, 14) * podium.Length), podium.Length - 1)];
                Box(new Vector3(x + w * .5f, 0f, front + 24f), w + 1f, 48f, 0f, h, m);
                if (Hash01(slot, 15) < .45f) Box(new Vector3(x + w * .5f, 0f, front + 30f), w * .55f, 24f, h, h + 6f, podium[3]); // roof plant
                x += w;
            }
        }

        /// A glass condo tower: teal glass, white slab edges every 24 m, a set-back mechanical crown.
        void Condo(Vector3 at, float w, float d, float h, Material glass, Material band)
        {
            Box(at, w, d, 0f, h, glass);
            for (float y = 24f; y < h - 6f; y += 24f) Box(at, w + .8f, d + .8f, y, y + 1.4f, band);
            Box(at, w * .6f, d * .6f, h, h + 8f, band);
        }

        /// A stable 0..1 hash, so the generated parts of the skyline are the same every run.
        static float Hash01(int a, int b)
        {
            unchecked
            {
                uint h = (uint)a * 73856093u ^ (uint)b * 19349663u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return (h & 0xffffff) / 16777216f;
            }
        }

        static List<Vector2> Rect(Vector3 c, float w, float d) => new()
        {
            new(c.x - w * .5f, c.z - d * .5f), new(c.x + w * .5f, c.z - d * .5f), new(c.x + w * .5f, c.z + d * .5f), new(c.x - w * .5f, c.z + d * .5f),
        };

        static List<Vector2> Chamfered(Vector3 c, float size, float cut)
        {
            float h = size * .5f;
            return new()
            {
                new(c.x - h + cut, c.z - h), new(c.x + h - cut, c.z - h), new(c.x + h, c.z - h + cut), new(c.x + h, c.z + h - cut),
                new(c.x + h - cut, c.z + h), new(c.x - h + cut, c.z + h), new(c.x - h, c.z + h - cut), new(c.x - h, c.z - h + cut),
            };
        }

        /// A square plan whose sides fold in a shallow V (Scotia Plaza's saw-tooth faces).
        static List<Vector2> Sawtooth(Vector3 c, float size, float fold)
        {
            float h = size * .5f;
            var corners = new[] { new Vector2(-h, -h), new Vector2(h, -h), new Vector2(h, h), new Vector2(-h, h) };
            var pts = new List<Vector2>();
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i]; var b = corners[(i + 1) % 4];
                var mid = (a + b) * .5f;
                pts.Add(new Vector2(c.x, c.z) + a);
                pts.Add(new Vector2(c.x, c.z) + mid - mid.normalized * fold);
            }
            return pts;
        }

        /// An equilateral-ish triangle with clipped corners (Royal Bank Plaza), rotated by `yaw` degrees.
        static List<Vector2> Triangle(Vector3 c, float size, float yaw)
        {
            var pts = new List<Vector2>();
            float r = size * .58f;
            for (int i = 0; i < 3; i++)
            {
                float a0 = (yaw + 120f * i - 12f) * Mathf.Deg2Rad, a1 = (yaw + 120f * i + 12f) * Mathf.Deg2Rad;
                pts.Add(new Vector2(c.x + Mathf.Cos(a0) * r, c.z + Mathf.Sin(a0) * r));
                pts.Add(new Vector2(c.x + Mathf.Cos(a1) * r, c.z + Mathf.Sin(a1) * r));
            }
            return pts;
        }

        // ------------------------------------------------------------------ mesh kit (real metres, flat-shaded sides)

        void Box(Vector3 c, float w, float d, float y0, float y1, Material m) => Prism(Rect(c, w, d), y0 <= 0f ? -Sink : y0, y1, m, m);

        /// A vertical prism on a star-shaped plan (points counter-clockwise from above), with a flat roof cap.
        void Prism(List<Vector2> plan, float y0, float y1, Material side, Material cap)
        {
            var c = Vector2.zero;
            foreach (var p in plan) c += p;
            c /= plan.Count;
            for (int i = 0; i < plan.Count; i++)
            {
                var a = plan[i]; var b = plan[(i + 1) % plan.Count];
                var e = b - a;
                var outward = new Vector3(e.y, 0f, -e.x).normalized;
                var mid = (a + b) * .5f - c;
                if (Vector3.Dot(outward, new Vector3(mid.x, 0f, mid.y)) < 0f) outward = -outward;
                Quad(new Vector3(a.x, y0, a.y), new Vector3(b.x, y0, b.y), new Vector3(b.x, y1, b.y), new Vector3(a.x, y1, a.y), outward, side);
            }
            var top = new Vector3(c.x, y1, c.y);
            for (int i = 0; i < plan.Count; i++)
            {
                var a = plan[i]; var b = plan[(i + 1) % plan.Count];
                Tri(top, new Vector3(a.x, y1, a.y), new Vector3(b.x, y1, b.y), Vector3.up, cap);
            }
        }

        void Pyramid(Vector3 baseCentre, float w, float d, float h, Material m)
        {
            var apex = baseCentre + Vector3.up * h;
            var plan = Rect(baseCentre, w, d);
            for (int i = 0; i < 4; i++)
            {
                var a = plan[i]; var b = plan[(i + 1) % 4];
                var pa = new Vector3(a.x, baseCentre.y, a.y); var pb = new Vector3(b.x, baseCentre.y, b.y);
                var n = Vector3.Cross(pb - pa, apex - pa).normalized;
                var mid = (pa + pb) * .5f - baseCentre;
                if (Vector3.Dot(n, mid) < 0f) n = -n;
                Tri(pa, pb, apex, n, m);
            }
        }

        /// The shaft between two plans of the same point count (flat-shaded faces).
        void Loft(Vector3 at, Vector2[] lower, float y0, Vector2[] upper, float y1, Material m)
        {
            for (int i = 0; i < lower.Length; i++)
            {
                int j = (i + 1) % lower.Length;
                var a = at + new Vector3(lower[i].x, y0, lower[i].y); var b = at + new Vector3(lower[j].x, y0, lower[j].y);
                var cTop = at + new Vector3(upper[j].x, y1, upper[j].y); var d = at + new Vector3(upper[i].x, y1, upper[i].y);
                // The plans run counter-clockwise (angle rising from +x toward +z), so (e.z, -e.x) points out.
                var e = lower[j] - lower[i];
                Quad(a, b, cTop, d, new Vector3(e.y, 0f, -e.x).normalized, m);
            }
        }

        /// A surface of revolution through (radius, height) points, bottom to top; mats[k] covers band k..k+1.
        /// Normals are smooth round the axis and follow each band's slope.
        void Lathe(Vector3 at, IList<(float r, float y)> profile, int segments, IList<Material> mats)
        {
            for (int k = 0; k + 1 < profile.Count; k++)
            {
                var (r0, y0) = profile[k];
                var (r1, y1) = profile[k + 1];
                var m = mats[Mathf.Min(k, mats.Count - 1)];
                float dr = r1 - r0, dy = y1 - y0;
                var slope = new Vector2(dy, -dr).normalized; // (radial, up) outward normal of this band
                for (int s = 0; s < segments; s++)
                {
                    float a0 = s * Mathf.PI * 2f / segments, a1 = (s + 1) * Mathf.PI * 2f / segments;
                    var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)); var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                    var n0 = d0 * slope.x + Vector3.up * slope.y; var n1 = d1 * slope.x + Vector3.up * slope.y;
                    var p00 = at + d0 * r0 + Vector3.up * y0; var p10 = at + d1 * r0 + Vector3.up * y0;
                    var p11 = at + d1 * r1 + Vector3.up * y1; var p01 = at + d0 * r1 + Vector3.up * y1;
                    var faceN = ((n0 + n1) * .5f).normalized;
                    if (r0 > 0.001f) Tri(p00, p10, p11, faceN, m, n0, n1, n1);
                    if (r1 > 0.001f) Tri(p00, p11, p01, faceN, m, n0, n1, n0);
                }
            }
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Material m)
        {
            Tri(a, b, c, normal, m);
            Tri(a, c, d, normal, m);
        }

        void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 facing, Material m) => Tri(a, b, c, facing, m, facing, facing, facing);

        /// One triangle, wound so its front face looks along `facing` (Unity: front = Cross(b - a, c - a)).
        void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 facing, Material m, Vector3 na, Vector3 nb, Vector3 nc)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f) { (b, c) = (c, b); (nb, nc) = (nc, nb); }
            if (!tris.TryGetValue(m, out var list)) { tris[m] = list = new List<int>(); order.Add(m); }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            normals.Add(na.normalized); normals.Add(nb.normalized); normals.Add(nc.normalized);
            uvs.Add(new Vector2(a.x + a.z, a.y) / 12.8f); uvs.Add(new Vector2(b.x + b.z, b.y) / 12.8f); uvs.Add(new Vector2(c.x + c.z, c.y) / 12.8f);
            list.Add(i); list.Add(i + 1); list.Add(i + 2);
        }

        void Bake()
        {
            var mesh = new Mesh { name = "Toronto skyline", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = order.Count;
            for (int i = 0; i < order.Count; i++) mesh.SetTriangles(tris[order[i]], i);
            mesh.RecalculateBounds();
            var go = new GameObject("Skyline mesh");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = order.ToArray();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            Debug.Log($"[TorontoSkyline] CN Tower + {order.Count} materials, {verts.Count / 3} triangles");
            verts.Clear(); normals.Clear(); uvs.Clear(); tris.Clear(); order.Clear();
        }
    }
}
