using System.Collections.Generic;
using System.IO;
using DallasDash.Core;
using DallasDash.Data;
using DallasDash.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DallasDash.EditorTools
{
    /// One-shot project setup from the Food Inc Game asset pack (24 Oct 2026):
    /// data assets for every character, ride, landmark, pickup and obstacle, flat-colour materials,
    /// a playable greybox Main scene, and mobile player settings.
    /// Menu: Dallas Dash > Bootstrap Project. Headless: -executeMethod DallasDash.EditorTools.ProjectBootstrap.Run
    public static class ProjectBootstrap
    {
        const string Root = "Assets/_Project";
        const string Ref = Root + "/Art/Reference";
        const string DataDir = Root + "/Data";
        const string MatDir = Root + "/Materials";
        const string ScenePath = Root + "/Scenes/Main.unity";

        // Palette from the pack: Finn's royal blue, cream, warm yellow rides, festive red and green.
        static readonly Color RoyalBlue = new(0.11f, 0.27f, 0.85f);
        static readonly Color Cream = new(0.96f, 0.93f, 0.84f);
        static readonly Color Charcoal = new(0.2f, 0.2f, 0.22f);
        static readonly Color WarmYellow = new(1f, 0.8f, 0.15f);
        static readonly Color FestiveRed = new(0.85f, 0.12f, 0.15f);
        static readonly Color FestiveGreen = new(0.1f, 0.5f, 0.22f);

        [MenuItem("Dallas Dash/Bootstrap Project")]
        public static void Run()
        {
            ConfigurePlayer();
            ConfigureUrp();
            var mats = CreateMaterials();
            var data = CreateData();
            var truck = data.obstacles.Find(o => o.id == "food-truck");
            if (truck) { truck.prefab = BuildFoodTruck(mats); EditorUtility.SetDirty(truck); }
            var banner = data.obstacles.Find(o => o.id == "garland-banner");
            if (banner) { banner.prefab = BuildGarlandBanner(mats); EditorUtility.SetDirty(banner); }
            BuildScene(mats, data);
            TrimOutlinePasses();
            AssetDatabase.SaveAssets();
            Debug.Log("[DallasDash] Bootstrap complete: " + ScenePath);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Epical Studio";
            PlayerSettings.productName = "Food Inc Game";
            PlayerSettings.enableFrameTimingStats = true; // CPU vs GPU frame times in the review builds
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "art.epical.dallasdash");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "art.epical.dallasdash");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        /// URP asset + renderer, assigned to Graphics and every quality level, so URP/Lit materials don't render pink.
        static void ConfigureUrp()
        {
            const string dir = Root + "/Settings";
            Directory.CreateDirectory(dir);
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(dir + "/URP.asset");
            if (!urp)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, dir + "/URP_Renderer.asset");
                urp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(urp, dir + "/URP.asset");
            }
            urp.supportsHDR = true;
            urp.shadowCascadeCount = 2;
            var so = new SerializedObject(urp);
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
            so.ApplyModifiedPropertiesWithoutUndo();
            urp.msaaSampleCount = 2;
            urp.shadowDistance = 40f;
            EditorUtility.SetDirty(urp);
            GraphicsSettings.defaultRenderPipeline = urp;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
        }

        static Dictionary<string, Material> CreateMaterials()
        {
            var colors = new Dictionary<string, Color>
            {
                ["Road"] = new(0.4f, 0.4f, 0.43f),
                ["Sidewalk"] = new(0.86f, 0.78f, 0.68f),
                ["Landmark"] = new(0.55f, 0.7f, 0.85f),
                ["WarmYellow"] = WarmYellow,
                ["FestiveRed"] = FestiveRed,
                ["FestiveGreen"] = FestiveGreen,
                ["Bulb"] = new(1f, 0.9f, 0.6f),
                ["Tint"] = Color.white,
                ["BrickRed"] = new(0.62f, 0.28f, 0.2f),
                ["BrickTan"] = new(0.78f, 0.6f, 0.42f),
                ["BrickBrown"] = new(0.45f, 0.27f, 0.18f),
                ["PaintTeal"] = new(0.26f, 0.62f, 0.66f),
                ["PaintMint"] = new(0.62f, 0.84f, 0.72f),
                ["PaintPink"] = new(0.93f, 0.6f, 0.62f),
                ["PaintCream"] = new(0.95f, 0.86f, 0.66f),
                ["PaintNavy"] = new(0.22f, 0.3f, 0.52f),
                ["RoyalBlue"] = RoyalBlue,
                ["Cream"] = Cream,
                ["Charcoal"] = Charcoal,
                ["PoliceBlack"] = new(0.08f, 0.08f, 0.1f),
                ["PoliceWhite"] = Color.white,
                ["HiVis"] = new(0.85f, 1f, 0.1f),
                ["LaneLine"] = new(0.96f, 0.96f, 0.92f),
                ["Curb"] = new(0.9f, 0.88f, 0.84f),
                ["Window"] = new(0.55f, 0.78f, 0.95f),
                ["WindowWarm"] = new(1f, 0.86f, 0.55f),
                ["AwningRed"] = new(0.88f, 0.16f, 0.2f),
                ["AwningGreen"] = new(0.12f, 0.58f, 0.3f),
                ["AwningBlue"] = RoyalBlue,
                ["Trim"] = new(0.96f, 0.92f, 0.82f),
                ["Lamp"] = Color.white,
                ["LampPost"] = new(0.14f, 0.16f, 0.2f),
                ["Wreath"] = new(0.1f, 0.42f, 0.2f),
                ["Skyline"] = new(0.62f, 0.7f, 0.86f),
                ["Door"] = new(0.36f, 0.2f, 0.12f),
                ["Plaza"] = new(0.8f, 0.74f, 0.66f),
                ["SkylineWarm"] = new(0.86f, 0.74f, 0.66f),
                ["Kraft"] = new(0.74f, 0.56f, 0.36f),
            };
            var shader = Shader.Find("FoodInc/ToonCurved");
            var result = new Dictionary<string, Material>();
            foreach (var kv in colors)
            {
                string path = $"{MatDir}/{kv.Key}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m)
                {
                    m = new Material(shader);
                    AssetDatabase.CreateAsset(m, path);
                }
                m.shader = shader;
                m.SetColor("_BaseColor", kv.Value);
                m.SetColor("_EmissionColor", kv.Key switch
                {
                    "Bulb" => kv.Value * 2.5f,
                    "WindowWarm" => kv.Value * 0.28f, // was 0.6: shop windows bloomed into blank white slabs
                    _ => Color.black,
                });
                bool hero = kv.Key is "RoyalBlue" or "Cream" or "PoliceBlack" or "PoliceWhite" or "HiVis" or "WarmYellow";
                m.SetFloat("_OutlineWidth", hero ? 0.012f : 0f);
                EditorUtility.SetDirty(m);
                result[kv.Key] = m;
            }
            var particles = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Particle.mat");
            if (!particles)
            {
                particles = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(particles, $"{MatDir}/Particle.mat");
            }
            // Alpha-blended soft dot, white so each system tints it.
            particles.SetFloat("_Surface", 1f);
            particles.SetFloat("_Blend", 0f);
            particles.SetOverrideTag("RenderType", "Transparent");
            particles.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            particles.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            particles.SetInt("_ZWrite", 0);
            particles.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particles.renderQueue = (int)RenderQueue.Transparent;
            particles.SetTexture("_BaseMap", SoftDot());
            EditorUtility.SetDirty(particles);
            result["Particle"] = particles;

            var sky = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Sky.mat");
            if (!sky)
            {
                sky = new Material(Shader.Find("FoodInc/SkyGradient"));
                AssetDatabase.CreateAsset(sky, $"{MatDir}/Sky.mat");
            }
            result["Sky"] = sky;

            // Additive gold glow for the halo under each burger (as in the pack scenes).
            var halo = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Halo.mat");
            if (!halo)
            {
                halo = new Material(Shader.Find("FoodInc/GlowCurved"));
                AssetDatabase.CreateAsset(halo, $"{MatDir}/Halo.mat");
            }
            halo.shader = Shader.Find("FoodInc/GlowCurved");
            halo.SetTexture("_BaseMap", SoftDot());
            halo.SetColor("_BaseColor", new Color(1f, 0.78f, 0.25f, 0.85f));
            EditorUtility.SetDirty(halo);
            result["Halo"] = halo;

            // Sign lettering that bends with the street and depth-tests against its board (built-in font
            // material does neither, so text floated off the shop signs and banners down the street).
            var signText = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/SignText.mat");
            if (!signText)
            {
                signText = new Material(Shader.Find("FoodInc/TextCurved"));
                AssetDatabase.CreateAsset(signText, $"{MatDir}/SignText.mat");
            }
            signText.shader = Shader.Find("FoodInc/TextCurved");
            var signFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            if (signFont) signText.mainTexture = signFont.material.mainTexture;
            EditorUtility.SetDirty(signText);
            result["SignText"] = signText;
            return result;
        }

        /// Procedural asphalt (speckle + faint cracks) and sidewalk pavers, tiled to world metres on the segment cubes.
        static void ApplyGroundTextures(Dictionary<string, Material> mats)
        {
            var asphalt = Procedural("asphalt", 256, (x, y, rnd) =>
            {
                float n = 0.86f + (float)rnd.NextDouble() * 0.2f;
                n -= Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.12f;
                return new Color(n, n * 0.98f, n * 0.97f);
            });
            var pavers = Procedural("pavers", 256, (x, y, rnd) =>
            {
                bool joint = x % 64 < 3 || y % 64 < 3;
                float n = joint ? 0.72f : 0.95f + ((float)rnd.NextDouble() - 0.5f) * 0.06f + Mathf.PerlinNoise(x / 64 * 3.1f, y / 64 * 1.7f) * 0.06f;
                return new Color(n, n, n);
            });
            // Road cube top is 8 x 20 m, sidewalk 3 x 20 m: one asphalt tile per 4 m, one paver per metre.
            mats["Road"].SetTexture("_BaseMap", asphalt);
            mats["Road"].SetTextureScale("_BaseMap", new Vector2(2f, 5f));
            mats["Sidewalk"].SetTexture("_BaseMap", pavers);
            mats["Sidewalk"].SetTextureScale("_BaseMap", new Vector2(0.75f, 5f));
            EditorUtility.SetDirty(mats["Road"]);
            EditorUtility.SetDirty(mats["Sidewalk"]);
        }

        static Texture2D Procedural(string name, int n, System.Func<int, int, System.Random, Color> px)
        {
            string path = $"{Root}/Art/Textures/{name}.png";
            if (!File.Exists(path))
            {
                var rnd = new System.Random(name.GetHashCode());
                var t = new Texture2D(n, n, TextureFormat.RGB24, true);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++) t.SetPixel(x, y, px(x, y, rnd));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D SoftDot()
        {
            string path = Root + "/Art/Textures/soft-dot.png";
            if (!File.Exists(path))
            {
                const int n = 64;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, t.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        class PackData
        {
            public List<CharacterDef> characters = new();
            public List<RideDef> rides = new();
            public List<LandmarkDef> landmarks = new();
            public List<PickupDef> pickups = new();
            public List<ObstacleDef> obstacles = new();
        }

        static PackData CreateData()
        {
            var d = new PackData();

            d.characters.Add(Character("finn", "Finn", "Delivery rider - the player. Dallas western look in royal blue: cream cowboy hat with a blue band, blue denim jacket with cream stitching and pearl snaps over a white tee, blue bolo tie, indigo jeans, brown leather boots.",
                Unlock.Free, 0, false, "Characters/02-finn.jpeg"));
            d.characters.Add(Character("faye", "Faye", "Delivery rider. Same western family in forest green: cream hat with a green band, green western shirt with cream piping, cream suede vest, red bandana, indigo jeans, tan boots.",
                Unlock.Free, 0, false, "Characters/03-faye.jpeg"));
            d.characters.Add(Character("fizz-11", "Fizz-11", "Courier robot on his own single tire: cream and yellow pod, one wide curved face screen with big expressive cartoon eyes, raised brows and an open smile. Never on a vehicle.",
                Unlock.Points, 25000, true, "Characters/04-fizz-11.jpeg"));

            d.rides.Add(Ride("bicycle", "Delivery bicycle", "Starter ride. Warm yellow and cream frame, chunky tyres, front basket, insulated delivery backpack on the rear rack.",
                Unlock.Free, false, 1.1f, "Rides/05-delivery-bicycle.jpeg"));
            d.rides.Add(Ride("scooter", "Courier scooter", "Compact warm yellow body, cream panels, front basket, rear insulated box. The single earnable ride, never sold.",
                Unlock.Objective, false, 1f, "Rides/06-courier-scooter.jpeg"));
            d.rides.Add(Ride("police-bicycle", "Police bicycle", "Black mountain-bike style bicycle, plain white POLICE saddle bag, small red-and-blue bar light, hi-vis yellow accents. One officer.",
                Unlock.Free, true, 1.1f, "Rides/07-police-bicycle.jpeg"));

            d.landmarks.Add(Landmark("reunion-tower", "Reunion Tower", 300f, 1f, "Landmarks/08-reunion-tower.jpeg", "Reunion Blvd"));
            d.landmarks.Add(Landmark("margaret-hunt-hill-bridge", "Margaret Hunt Hill Bridge", 900f, -1f, "Landmarks/09-margaret-hunt-hill-bridge.jpeg", "Singleton Blvd"));
            d.landmarks.Add(Landmark("bank-of-america-plaza", "Bank of America Plaza", 1500f, 1f, "Landmarks/10-bank-of-america-plaza.jpeg", "Main Street"));
            d.landmarks.Add(Landmark("fountain-place", "Fountain Place", 2100f, -1f, "Landmarks/11-fountain-place.jpeg", "Ross Avenue"));
            d.landmarks.Add(Landmark("omni-dallas", "Omni Dallas hotel", 2700f, 1f, "Landmarks/12-omni-dallas-hotel.jpeg", "Lamar Street"));

            // One pickup only: Bellagreen's Wagyu Bacon Burger (menu read off bellagreen.com).
            d.pickups.Add(Pickup("wagyu-bacon-burger", "Wagyu Bacon Burger", 10, 1, new Color(0.85f, 0.55f, 0.2f)));

            // Christmas obstacle set (sheet 21) plus the Slice House food truck from scene 13 (Loading Up).
            d.obstacles.Add(Obstacle("presents", "Stack of presents", TrackKind.Low, new Vector3(1.6f, 0.9f, 1f), 3, FestiveRed));
            d.obstacles.Add(Obstacle("cone", "Traffic cone", TrackKind.Low, new Vector3(0.8f, 0.8f, 0.8f), 2, new Color(1f, 0.45f, 0.1f)));
            d.obstacles.Add(Obstacle("roadworks-barrier", "Roadworks barrier", TrackKind.Low, new Vector3(2f, 0.8f, 0.4f), 2, Color.white));
            d.obstacles.Add(Obstacle("tree-planter", "Christmas tree in a planter", TrackKind.Block, new Vector3(1.8f, 3f, 1.8f), 2, FestiveGreen));
            d.obstacles.Add(Obstacle("shopper", "Jaywalking shopper with gift bags", TrackKind.Block, new Vector3(0.9f, 1.8f, 0.6f), 2, new Color(0.3f, 0.6f, 0.35f)));
            d.obstacles.Add(Obstacle("food-truck", "Parked food truck", TrackKind.Block, new Vector3(1.9f, 2.4f, 4.5f), 1, Cream));
            // NOT in the pack: an overhead garland banner so the roll move still has something to roll under. Needs client approval.
            d.obstacles.Add(Obstacle("garland-banner", "Low garland banner (proposed)", TrackKind.High, new Vector3(2f, 0.5f, 0.4f), 2, FestiveGreen));
            return d;
        }

        static void BuildScene(Dictionary<string, Material> mats, PackData data)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.85f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.85f, 0.85f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.5f, 0.5f);

            // Bright winter day (user, 2026-10-28: the pale golden-hour sky looked unfinished): deep blue sky with
            // toon clouds, a high warm sun, cool blue-tinted shade, fog that melts into the horizon colour.
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.72f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.8f, 0.86f, 0.96f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.5f, 0.55f);
            var sky = mats["Sky"];
            sky.SetColor("_Top", new Color(0.06f, 0.26f, 0.74f));
            sky.SetColor("_Horizon", new Color(0.5f, 0.76f, 1f));
            sky.SetColor("_Bottom", new Color(0.72f, 0.85f, 1f));
            sky.SetColor("_SunColor", new Color(1f, 0.95f, 0.78f));
            sky.SetVector("_SunDir", new Vector4(0.45f, 0.55f, 1f, 0f));
            sky.SetColor("_CloudColor", Color.white);
            sky.SetColor("_CloudShade", new Color(0.64f, 0.74f, 0.92f));
            sky.SetFloat("_CloudCover", 0.5f);
            sky.SetFloat("_CloudSpeed", 0.006f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = mats["Sky"];
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun)
            {
                // High midday sun from behind-left: crisp, readable shadows without drowning the street in shade.
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                sun.color = new Color(1f, 0.96f, 0.86f);
                sun.intensity = 1.45f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.7f;
            }

            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            // Rider: Finn on the warm yellow starter bicycle, greybox until the meshes from the character sheets land.
            var rider = new GameObject("Rider (Finn)");
            var riderVisual = new GameObject("Visual").transform;
            riderVisual.SetParent(rider.transform);
            // The crew from the pack, as generated meshes. *-ride are bent into a seated pose by
            // tools/img2mesh/pose_rider.py with the seat at their origin; the standing ones are for Loading Up.
            var look = rider.AddComponent<RiderLook>();
            var bike = ModelImport.Get("bicycle");
            look.bicycle = bike ? Model(riderVisual, bike, Vector3.zero, "Bicycle")
                : PartGo(riderVisual, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.25f, 0.9f, 1.6f), mats["WarmYellow"], "Bicycle");
            // The courier scooter (earned through missions) sits in the same spot, hidden until chosen. Its seat
            // is lower than the bicycle saddle: scale the saddle height by the two rides' heights.
            if (ModelImport.Get("scooter") is { } scooterPrefab && look.bicycle)
            {
                look.scooter = Model(riderVisual, scooterPrefab, Vector3.zero, "Scooter");
                float ratio = Height(look.scooter) / Mathf.Max(0.01f, Height(look.bicycle));
                // Scooter scaled down a touch (its leg shield hid the rider), rider raised to sit on its seat.
                look.scooter.transform.localScale *= 0.85f;
                look.scooterSeat = new Vector3(0f, SaddleOffset.y * (ratio * 0.85f * 0.92f - 1f) + 0.06f, -0.3f);
                look.scooter.SetActive(false);
            }
            GameObject Seated(string id, Vector3 at) => ModelImport.Get(id) is { } m ? Model(riderVisual, m, at, id)
                : PartGo(riderVisual, PrimitiveType.Capsule, at + new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.55f, 0.6f), mats["RoyalBlue"], id);
            look.seated = new[] { Seated("finn-ride", SaddleOffset), Seated("faye-ride", SaddleOffset), Seated("fizz-11", Vector3.zero) };
            // Standing at the curb beside the bike, turned toward its delivery box.
            var curbSide = new Vector3(0.75f, 0f, -0.35f);
            look.standing = new[] { Seated("finn", curbSide), Seated("faye", curbSide), Seated("fizz-11", curbSide) };
            foreach (var g in look.standing) g.transform.localRotation = Quaternion.Euler(0f, -100f, 0f);
            look.Apply(0, true);
            var rc = rider.AddComponent<RiderController>();
            rc.visual = riderVisual;
            rc.handling = data.rides[0].handling;

            // One officer on a police bicycle: black frame, white POLICE saddle bag, hi-vis vest, small red-and-blue bar light.
            var chaseRoot = new GameObject("Police");
            var chase = chaseRoot.AddComponent<ChaseController>();
            chase.rider = rc;
            chase.officerSpread = 0f;
            var o = new GameObject("Officer").transform;
            o.SetParent(chaseRoot.transform);
            // Officer meshes generated from scene crops: riding (15 The Pursuit, seen from behind) for the chase,
            // standing beside his bike (19 Busted) for the end of a run.
            var riding = ModelImport.Get("officer-riding");
            var standing = ModelImport.Get("officer-standing");
            if (riding)
            {
                chase.ridingVisual = Model(o, riding, Vector3.zero, "Officer riding");
                if (standing) chase.standingVisual = Model(o, standing, Vector3.zero, "Officer standing");
            }
            else
            {
                var policeBike = ModelImport.Get("police-bicycle");
                if (policeBike) Model(o, policeBike, Vector3.zero, "Police bicycle");
                Part(o, PrimitiveType.Capsule, new Vector3(0f, 1.3f, 0f), new Vector3(0.6f, 0.55f, 0.6f), mats["PoliceBlack"], "Officer");
                Part(o, PrimitiveType.Cube, new Vector3(0f, 1.4f, 0f), new Vector3(0.64f, 0.4f, 0.64f), mats["HiVis"], "Hi-vis vest");
            }
            var lb = new GameObject("BarLight").AddComponent<Light>();
            lb.transform.SetParent(o, false);
            lb.transform.localPosition = new Vector3(0f, 1.1f, 0.7f);
            lb.type = LightType.Point;
            lb.range = 4f;
            lb.intensity = 2.5f;
            chase.officers = new[] { o };
            chase.lightBars = new[] { lb };

            var spawner = new GameObject("Track").AddComponent<TrackSpawner>();
            spawner.rider = rc;
            spawner.chase = chase;
            spawner.pickups = data.pickups.ToArray();
            spawner.obstacles = data.obstacles.ToArray();
            spawner.landmarks = data.landmarks.ToArray();
            spawner.roadMaterial = mats["Road"];
            spawner.sidewalkMaterial = mats["Sidewalk"];
            spawner.landmarkMaterial = mats["Landmark"];
            spawner.facadeMaterials = new[] { mats["BrickRed"], mats["BrickTan"], mats["BrickBrown"], mats["PaintTeal"], mats["PaintMint"], mats["PaintPink"], mats["PaintCream"], mats["PaintNavy"] };
            spawner.garlandMaterial = mats["FestiveGreen"];
            spawner.bulbMaterial = mats["Bulb"];
            spawner.tintMaterial = mats["Tint"];
            spawner.laneLineMaterial = mats["LaneLine"];
            spawner.curbMaterial = mats["Curb"];
            spawner.windowMaterials = new[] { mats["Window"], mats["WindowWarm"] };
            spawner.awningMaterials = new[] { mats["AwningRed"], mats["AwningGreen"], mats["AwningBlue"] };
            spawner.trimMaterial = mats["Trim"];
            spawner.lampPostMaterial = mats["LampPost"];
            spawner.wreathMaterial = mats["Wreath"];
            spawner.skylineMaterials = new[] { mats["Skyline"], mats["SkylineWarm"] };
            spawner.haloMaterial = mats["Halo"];
            spawner.doorMaterial = mats["Door"];
            spawner.plazaMaterial = mats["Plaza"];
            spawner.signFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            spawner.signTextMaterial = mats["SignText"];
            spawner.decorPrefabs = new[] { ModelImport.Get("tree-planter"), ModelImport.Get("presents") };
            spawner.bulbColors = new[] { new Color(1f, 0.25f, 0.2f), new Color(0.3f, 1f, 0.4f), new Color(1f, 0.82f, 0.3f), new Color(1f, 0.95f, 0.85f) };
            ApplyGroundTextures(mats);

            var cam = Camera.main;
            cam.backgroundColor = new Color(0.72f, 0.88f, 1f);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;
            cam.allowHDR = true;
            cam.transform.position = new Vector3(0f, 4.2f, -7.5f);
            var follow = cam.gameObject.AddComponent<FollowCamera>();
            follow.target = rider.transform;
            cam.gameObject.AddComponent<CurvedWorld>();
            var camData = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.62f, 0.8f, 1f); // the sky's horizon, so far blocks melt into it
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 230f;

            BuildPostFx();

            var feel = new GameObject("GameFeel").AddComponent<GameFeel>();
            feel.cam = follow;
            feel.rider = rc;
            feel.particleMaterial = mats["Particle"];

            BuildHud();

            var menus = new GameObject("MenuUI").AddComponent<DallasDash.UI.MenuUI>();
            menus.font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            menus.crew = data.characters.ToArray();

            // Scenes 13-15 (Loading Up, Ran the Red, The Pursuit) play from StoryDirector before each run.
            var story = new GameObject("Story").AddComponent<StoryDirector>();
            story.rider = rc;
            story.look = look;
            story.chase = chase;
            story.follow = follow;
            story.trafficLight = BuildTrafficLight(mats, -46f);
            var bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(bag.GetComponent<Collider>());
            bag.name = "Delivery bag";
            bag.transform.localScale = new Vector3(0.3f, 0.36f, 0.22f);
            bag.GetComponent<Renderer>().sharedMaterial = mats["Kraft"];
            story.bag = bag.transform;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// Bright, punchy grade: bloom on the string lights, a little extra saturation and contrast, soft vignette.
        static void BuildPostFx()
        {
            string path = Root + "/Settings/PostFx.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            T Comp<T>() where T : VolumeComponent
            {
                if (profile.TryGet<T>(out var c)) return c;
                c = profile.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, profile);
                return c;
            }
            var bloom = Comp<Bloom>();
            bloom.threshold.Override(1.15f); // emissive bulbs and windows only, not sunlit walls
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.65f);
            var tone = Comp<Tonemapping>();
            tone.mode.Override(TonemappingMode.Neutral);
            var grade = Comp<ColorAdjustments>();
            grade.saturation.Override(25f);
            grade.contrast.Override(18f);
            grade.postExposure.Override(0.05f);
            grade.colorFilter.Override(Color.white);
            var vignette = Comp<Vignette>();
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.5f);
            EditorUtility.SetDirty(profile);

            var vol = new GameObject("PostFx").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        /// Saddle position on the generated bicycle, measured from the bike prefab (ground at y = 0).
        static readonly Vector3 SaddleOffset = new(0f, 0.95f, -0.15f);

        static float Height(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 0f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b.size.y;
        }

        static GameObject Model(Transform parent, GameObject prefab, Vector3 pos, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go;
        }

        static GameObject PartGo(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, string name)
        {
            Part(parent, type, pos, scale, m, name);
            return parent.GetChild(parent.childCount - 1).gameObject;
        }

        /// Scene 14's junction light: pole on the left sidewalk, arm over the road, lamps facing the rider.
        static TrafficLight BuildTrafficLight(Dictionary<string, Material> mats, float z)
        {
            var root = new GameObject("Traffic Light").transform;
            root.position = new Vector3(0f, 0f, z);
            Part(root, PrimitiveType.Cylinder, new Vector3(-4.6f, 3f, 0f), new Vector3(0.22f, 3f, 0.22f), mats["Charcoal"], "Pole");
            Part(root, PrimitiveType.Cube, new Vector3(-2.2f, 5.9f, 0f), new Vector3(4.9f, 0.16f, 0.16f), mats["Charcoal"], "Arm");
            Part(root, PrimitiveType.Cube, new Vector3(0.2f, 5.0f, 0f), new Vector3(0.62f, 1.75f, 0.45f), mats["Charcoal"], "Housing");
            var tl = root.gameObject.AddComponent<TrafficLight>();
            Renderer[] Lamp(float y, string name)
            {
                var lenses = new Renderer[2];
                for (int i = 0; i < 2; i++)
                {
                    Part(root, PrimitiveType.Sphere, new Vector3(0.2f, y, i == 0 ? -0.24f : 0.24f), Vector3.one * 0.4f, mats["Lamp"], name);
                    lenses[i] = root.GetChild(root.childCount - 1).GetComponent<Renderer>();
                }
                return lenses;
            }
            tl.red = Lamp(5.55f, "Red");
            tl.amber = Lamp(5.0f, "Amber");
            tl.green = Lamp(4.45f, "Green");
            tl.Set(TrafficLight.Phase.Green);
            return tl;
        }

        static void BuildHud()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = canvasGo.AddComponent<Hud>();
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud.score = Label(canvasGo.transform, "Score", font, 72, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-40, -40));
            hud.pickups = Label(canvasGo.transform, "Pickups", font, 56, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -40));
            hud.banner = Label(canvasGo.transform, "Banner", font, 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero);

            if (!Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
                var inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModule != null) es.AddComponent(inputModule);
            }
        }

        static Text Label(Transform parent, string name, Font font, int size, TextAnchor anchor, Vector2 pivot, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900, 400);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.12f, 0.3f, 0.9f);
            outline.effectDistance = new Vector2(4f, -4f);
            return t;
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        static CharacterDef Character(string id, string name, string desc, Unlock unlock, int points, bool rolling, string sheet)
        {
            var a = Load<CharacterDef>($"{DataDir}/Characters/{id}.asset");
            a.id = id; a.displayName = name; a.description = desc;
            a.unlock = unlock; a.unlockPoints = points; a.rollingBase = rolling;
            a.referenceSheet = Sheet(sheet);
            EditorUtility.SetDirty(a);
            return a;
        }

        static RideDef Ride(string id, string name, string desc, Unlock unlock, bool police, float handling, string sheet)
        {
            var a = Load<RideDef>($"{DataDir}/Rides/{id}.asset");
            a.id = id; a.displayName = name; a.description = desc;
            a.unlock = unlock; a.isPolice = police; a.handling = handling;
            a.referenceSheet = Sheet(sheet);
            EditorUtility.SetDirty(a);
            return a;
        }

        static LandmarkDef Landmark(string id, string name, float at, float side, string sheet, string street)
        {
            var a = Load<LandmarkDef>($"{DataDir}/Landmarks/{id}.asset");
            a.id = id; a.displayName = name; a.atDistance = at; a.side = side; a.street = street;
            a.referenceSheet = Sheet(sheet);
            a.prefab = ModelImport.Get(id);
            EditorUtility.SetDirty(a);
            return a;
        }

        /// The yellow Slice House van parked in scene 13, built from primitives in pack colours
        /// (the frame shows too little of it to generate a mesh). Nose faces the oncoming rider (-Z).
        static GameObject BuildFoodTruck(Dictionary<string, Material> mats)
        {
            var root = new GameObject("food-truck").transform;
            Part(root, PrimitiveType.Cube, new Vector3(0f, 1.35f, 0.35f), new Vector3(1.9f, 2.0f, 3.7f), mats["WarmYellow"], "Body");
            // Detail so the van isn't one flat yellow slab: dark skirt, bumpers, lights, a striped awning over the hatch.
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.32f, -0.1f), new Vector3(1.92f, 0.22f, 4.6f), mats["Charcoal"], "Skirt");
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.42f, -2.33f), new Vector3(1.9f, 0.22f, 0.12f), mats["Trim"], "Front bumper");
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.42f, 2.22f), new Vector3(1.9f, 0.22f, 0.12f), mats["Trim"], "Rear bumper");
            foreach (float x in new[] { -0.7f, 0.7f })
            {
                Part(root, PrimitiveType.Cube, new Vector3(x, 0.78f, -2.31f), new Vector3(0.34f, 0.2f, 0.06f), mats["WindowWarm"], "Headlight");
                Part(root, PrimitiveType.Cube, new Vector3(x, 0.9f, 2.21f), new Vector3(0.24f, 0.3f, 0.06f), mats["AwningRed"], "Tail light");
            }
            Part(root, PrimitiveType.Cube, new Vector3(0f, 1.05f, -2.31f), new Vector3(0.9f, 0.18f, 0.05f), mats["Charcoal"], "Grille");
            for (int i = 0; i < 6; i++)
                Part(root, PrimitiveType.Cube, new Vector3(1.16f, 2.08f - 0.02f * (i % 2), -0.3f + i * 0.36f), new Vector3(0.5f, 0.06f, 0.36f),
                     mats[i % 2 == 0 ? "AwningRed" : "Trim"], "Awning");
            // Rooftop sign board facing oncoming riders (it used to stand edge-on and read as a slab), on two posts.
            Part(root, PrimitiveType.Cube, new Vector3(0f, 3.1f, -0.2f), new Vector3(1.7f, 0.62f, 0.12f), mats["FestiveRed"], "Roof sign");
            Part(root, PrimitiveType.Cube, new Vector3(0f, 3.1f, -0.27f), new Vector3(1.54f, 0.46f, 0.04f), mats["Cream"], "Roof sign face");
            foreach (float x in new[] { -0.6f, 0.6f })
                Part(root, PrimitiveType.Cube, new Vector3(x, 2.68f, -0.2f), new Vector3(0.08f, 0.3f, 0.08f), mats["Charcoal"], "Sign post");
            var roofFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            if (roofFont)
            {
                var roofLabel = new GameObject("Roof lettering", typeof(TextMesh));
                roofLabel.transform.SetParent(root, false);
                roofLabel.transform.localPosition = new Vector3(0f, 3.1f, -0.3f);
                var rt = roofLabel.GetComponent<TextMesh>();
                rt.text = "SLICE HOUSE";
                rt.anchor = TextAnchor.MiddleCenter;
                rt.characterSize = 0.05f;
                rt.fontSize = 64;
                rt.color = FestiveRed;
                rt.font = roofFont;
                roofLabel.GetComponent<MeshRenderer>().sharedMaterial = mats["SignText"];
            }
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.95f, -1.85f), new Vector3(1.86f, 1.2f, 0.9f), mats["WarmYellow"], "Cab");
            Part(root, PrimitiveType.Cube, new Vector3(0f, 1.75f, -1.55f), new Vector3(1.7f, 0.75f, 0.5f), mats["Window"], "Windshield");
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.35f), new Vector3(1.96f, 0.35f, 4.6f), mats["Cream"], "Stripe");
            Part(root, PrimitiveType.Cube, new Vector3(0f, 2.45f, 0.6f), new Vector3(1.4f, 0.25f, 2.4f), mats["Cream"], "Roof box");
            Part(root, PrimitiveType.Cube, new Vector3(0.97f, 1.55f, 0.6f), new Vector3(0.06f, 0.9f, 2f), mats["WindowWarm"], "Serving hatch");
            foreach (float x in new[] { -0.9f, 0.9f })
            foreach (float z in new[] { -1.55f, 1.4f })
            {
                Part(root, PrimitiveType.Cylinder, new Vector3(x, 0.4f, z), new Vector3(0.8f, 0.14f, 0.8f), mats["Charcoal"], "Wheel");
                root.GetChild(root.childCount - 1).localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            foreach (float x in new[] { -0.99f, 0.99f })
            {
                var sign = new GameObject("Slice House", typeof(TextMesh));
                sign.transform.SetParent(root, false);
                sign.transform.localPosition = new Vector3(x, 1.95f, 0.35f);
                sign.transform.localRotation = Quaternion.Euler(0f, x > 0 ? -90f : 90f, 0f);
                var tm = sign.GetComponent<TextMesh>();
                tm.text = "SLICE HOUSE";
                tm.anchor = TextAnchor.MiddleCenter;
                tm.characterSize = 0.12f;
                tm.fontSize = 48;
                tm.color = FestiveRed;
                var truckFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
                if (truckFont) { tm.font = truckFont; sign.GetComponent<MeshRenderer>().sharedMaterial = mats["SignText"]; }
                else tm.fontStyle = FontStyle.Bold;
            }
            Directory.CreateDirectory(Root + "/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Root + "/Prefabs/food-truck.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        /// Overhead obstacle to roll under: a "Season's Eatings" banner strung across one lane between two
        /// lamp-post-style poles, trimmed with garland, bulbs and a bow. Origin is the banner's centre, which the
        /// spawner hangs 1.6 m up; the poles reach down to the road.
        static GameObject BuildGarlandBanner(Dictionary<string, Material> mats)
        {
            var root = new GameObject("garland-banner").transform;
            foreach (float x in new[] { -1.12f, 1.12f })
            {
                Part(root, PrimitiveType.Cylinder, new Vector3(x, -0.45f, 0f), new Vector3(0.12f, 1.25f, 0.12f), mats["LampPost"], "Pole");
                Part(root, PrimitiveType.Cylinder, new Vector3(x, -1.58f, 0f), new Vector3(0.34f, 0.04f, 0.34f), mats["LampPost"], "Foot");
                Part(root, PrimitiveType.Sphere, new Vector3(x, 0.86f, 0f), new Vector3(0.2f, 0.2f, 0.2f), mats["WarmYellow"], "Finial");
            }
            Part(root, PrimitiveType.Cube, Vector3.zero, new Vector3(2.1f, 0.56f, 0.1f), mats["FestiveRed"], "Board");
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, -0.03f), new Vector3(1.96f, 0.42f, 0.1f), mats["Cream"], "Panel");
            // Garland along the top edge with bulbs tucked in, and a bow at the centre.
            for (int i = 0; i <= 10; i++)
            {
                float x = Mathf.Lerp(-1.1f, 1.1f, i / 10f);
                Part(root, PrimitiveType.Sphere, new Vector3(x, 0.3f, 0f), new Vector3(0.26f, 0.22f, 0.24f), mats["Wreath"], "Garland");
                if (i % 2 == 1) Part(root, PrimitiveType.Sphere, new Vector3(x, 0.26f, -0.13f), Vector3.one * 0.1f, mats["Bulb"], "Bulb");
            }
            Part(root, PrimitiveType.Sphere, new Vector3(-0.13f, 0.36f, -0.08f), new Vector3(0.24f, 0.16f, 0.1f), mats["FestiveRed"], "Bow");
            Part(root, PrimitiveType.Sphere, new Vector3(0.13f, 0.36f, -0.08f), new Vector3(0.24f, 0.16f, 0.1f), mats["FestiveRed"], "Bow");
            Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.36f, -0.1f), Vector3.one * 0.11f, mats["WarmYellow"], "Knot");
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            {
                // Lettered on the face the rider approaches (-Z). Text meshes draw over the board, so a second,
                // back-facing copy would show through mirrored.
                var label = new GameObject("Season's Eatings", typeof(TextMesh));
                label.transform.SetParent(root, false);
                label.transform.localPosition = new Vector3(0f, -0.01f, -0.085f);
                var tm = label.GetComponent<TextMesh>();
                tm.text = "SEASON'S EATINGS";
                tm.anchor = TextAnchor.MiddleCenter;
                tm.characterSize = 0.043f;
                tm.fontSize = 64;
                tm.color = FestiveRed;
                if (font) { tm.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = mats["SignText"]; }
            }
            Directory.CreateDirectory(Root + "/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Root + "/Prefabs/garland-banner.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        /// Toon materials with no outline skip the outline pass entirely. Otherwise every street prop still
        /// draws it (as a clipped no-op), and alternating passes object by object defeats batching.
        static void TrimOutlinePasses()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatDir }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!m || !m.shader || m.shader.name != "FoodInc/ToonCurved") continue;
                m.SetShaderPassEnabled("SRPDefaultUnlit", m.GetFloat("_OutlineWidth") > 0f);
                EditorUtility.SetDirty(m);
            }
        }

        static ObstacleDef Obstacle(string id, string name, TrackKind kind, Vector3 size, int weight, Color c)
        {
            var a = Load<ObstacleDef>($"{DataDir}/Obstacles/{id}.asset");
            a.id = id; a.displayName = name; a.kind = kind; a.size = size; a.weight = weight; a.placeholderColor = c;
            a.prefab = ModelImport.Get(id);
            EditorUtility.SetDirty(a);
            return a;
        }

        static PickupDef Pickup(string id, string name, int points, int weight, Color c)
        {
            var a = Load<PickupDef>($"{DataDir}/Pickups/{id}.asset");
            a.id = id; a.displayName = name; a.points = points; a.weight = weight; a.placeholderColor = c;
            a.prefab = ModelImport.Get(id);
            EditorUtility.SetDirty(a);
            return a;
        }

        static T Load<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a) return a;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static Texture2D Sheet(string rel) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Ref}/{rel}");
    }
}
