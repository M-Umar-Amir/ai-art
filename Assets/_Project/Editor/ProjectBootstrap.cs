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

        // Palette from the pack: Finn's royal blue, cream, warm yellow rides; sign red and park green for the street.
        static readonly Color RoyalBlue = new(0.11f, 0.27f, 0.85f);
        static readonly Color Cream = new(0.96f, 0.93f, 0.84f);
        static readonly Color Charcoal = new(0.2f, 0.2f, 0.22f);
        static readonly Color WarmYellow = new(1f, 0.8f, 0.15f);
        static readonly Color SignRed = new(0.85f, 0.12f, 0.15f);
        static readonly Color ParkGreen = new(0.1f, 0.5f, 0.22f);
        static readonly Color SafetyOrange = new(1f, 0.45f, 0.08f);

        [MenuItem("Dallas Dash/Bootstrap Project")]
        public static void Run()
        {
            DashAssets.ImportVisuals();
            ConfigurePlayer();
            ConfigureUrp();
            var mats = CreateMaterials();
            var data = CreateData();
            var truck = data.obstacles.Find(o => o.id == "food-truck");
            if (truck) { truck.prefab = BuildFoodTruck(mats); EditorUtility.SetDirty(truck); }
            BuildStreetHurdles(mats, data);
            BuildScene(mats, data);
            TrimOutlinePasses();
            AssetDatabase.SaveAssets();
            Debug.Log("[DallasDash] Bootstrap complete: " + ScenePath);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "PAGE STUDIO INC";
            PlayerSettings.productName = "Dash Riders";
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
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
                ["Road"] = new(0.055f, 0.075f, 0.14f),
                ["Sidewalk"] = new(0.12f, 0.18f, 0.25f),
                ["Landmark"] = new(0.55f, 0.7f, 0.85f),
                ["WarmYellow"] = WarmYellow,
                ["SignRed"] = SignRed,
                ["ParkGreen"] = ParkGreen,
                ["SafetyOrange"] = SafetyOrange,
                ["Amber"] = new(1f, 0.62f, 0.1f),
                ["BrakeRed"] = new(1f, 0.12f, 0.1f),
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
                ["Foliage"] = new(0.1f, 0.42f, 0.2f),
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
                    "Amber" => kv.Value * 2.2f,
                    "BrakeRed" => kv.Value * 1.6f,
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

            d.characters.Add(Character("ace", "Ace", "Ace, the Speedster. Electric orange bomber jacket, black cargo trousers, futuristic sneakers. Electric board. Short speed boost.",
                Unlock.Free, 0, false, "Characters/02-finn.jpeg"));
            d.characters.Add(Character("nova", "Nova", "Nova, the parkour queen. Neon purple cropped jacket, athletic cargo trousers, high-tech sneakers. Extended jump duration.",
                Unlock.Free, 0, false, "Characters/03-faye.jpeg"));
            d.characters.Add(Character("bolt", "Bolt", "Bolt, the experimental robot courier. Silver and yellow chassis, expressive LED eyes, magnetic wheels, delivery compartment. Temporary collision protection.",
                Unlock.Points, 25000, true, "Characters/04-fizz-11.jpeg"));

            d.rides.Add(Ride("bicycle", "Delivery bicycle", "Starter ride. Warm yellow and cream frame, chunky tyres, front basket, insulated delivery backpack on the rear rack.",
                Unlock.Free, false, 1.1f, "Rides/05-delivery-bicycle.jpeg"));
            d.rides.Add(Ride("scooter", "Courier scooter", "Compact warm yellow body, cream panels, front basket, rear insulated box. The single earnable ride, never sold.",
                Unlock.Objective, false, 1f, "Rides/06-courier-scooter.jpeg"));

            d.landmarks.Add(Landmark("reunion-tower", "Reunion Tower", 300f, 1f, "Landmarks/08-reunion-tower.jpeg", "Reunion Blvd"));
            d.landmarks.Add(Landmark("margaret-hunt-hill-bridge", "Margaret Hunt Hill Bridge", 900f, -1f, "Landmarks/09-margaret-hunt-hill-bridge.jpeg", "Singleton Blvd"));
            d.landmarks.Add(Landmark("bank-of-america-plaza", "Bank of America Plaza", 1500f, 1f, "Landmarks/10-bank-of-america-plaza.jpeg", "Main Street"));
            d.landmarks.Add(Landmark("fountain-place", "Fountain Place", 2100f, -1f, "Landmarks/11-fountain-place.jpeg", "Ross Avenue"));
            d.landmarks.Add(Landmark("omni-dallas", "Omni Dallas hotel", 2700f, 1f, "Landmarks/12-omni-dallas-hotel.jpeg", "Lamar Street"));

            // One pickup only: Bellagreen's Wagyu Bacon Burger (menu read off bellagreen.com).
            foreach (var id in new[] { "energy-chip", "burger", "pizza", "soft-drink", "taco" })
            {
                var pickup = Pickup(id, id.Replace('-', ' '), id == "energy-chip" ? 5 : 15, id == "energy-chip" ? 4 : 1, WarmYellow);
                pickup.prefab = DashAssets.Pickup(id); EditorUtility.SetDirty(pickup); d.pickups.Add(pickup);
            }

            // Everyday downtown hurdles (client feedback, 28 Oct: no Christmas theme, no ducking under things).
            // Low ones get bunny-hopped; the rest fill the lane and are steered round. Sizes here are placeholders:
            // MeasureHurdles sets each one from its built model, so the hit box is what the player sees.
            d.obstacles.Add(Obstacle("cones", "Row of traffic cones", TrackKind.Low, new Vector3(2f, 0.75f, 0.5f), 3, SafetyOrange));
            d.obstacles.Add(Obstacle("roadworks-barrier", "Roadworks barrier", TrackKind.Low, new Vector3(1.9f, 0.9f, 0.5f), 2, Color.white));
            d.obstacles.Add(Obstacle("roadworks", "Lane closed for roadworks", TrackKind.Block, new Vector3(2.1f, 1.4f, 6f), 3, SafetyOrange, heavy: true));
            d.obstacles.Add(Obstacle("delivery-van", "Delivery van stopped with its hazards on", TrackKind.Block, new Vector3(2f, 2.4f, 5f), 2, Color.white, heavy: true));
            d.obstacles.Add(Obstacle("taxi", "Taxi pulled up for a fare", TrackKind.Block, new Vector3(1.9f, 1.6f, 4.4f), 1, WarmYellow, heavy: true));
            d.obstacles.Add(Obstacle("garbage-truck", "Garbage truck on its round", TrackKind.Block, new Vector3(2.1f, 2.8f, 6f), 1, ParkGreen, heavy: true));
            // Traffic going the same way, slower than the rider: other couriers on bikes and a scooter, and cars.
            d.obstacles.Add(Obstacle("courier-a", "Courier on a bike", TrackKind.Block, new Vector3(0.8f, 1.8f, 1.8f), 3, ParkGreen, heavy: true, drift: 0.25f));
            d.obstacles.Add(Obstacle("courier-b", "Courier on a bike", TrackKind.Block, new Vector3(0.8f, 1.8f, 1.8f), 2, SafetyOrange, heavy: true, drift: 0.3f));
            d.obstacles.Add(Obstacle("scooter-courier", "Courier on a scooter", TrackKind.Block, new Vector3(0.8f, 1.8f, 1.8f), 2, RoyalBlue, heavy: true, drift: 0.38f));
            d.obstacles.Add(Obstacle("slow-car", "Car in slow traffic", TrackKind.Block, new Vector3(1.9f, 1.5f, 4.4f), 2, Color.white, heavy: true, drift: 0.35f));
            // The Slice House food truck from scene 13 (Loading Up).
            d.obstacles.Add(Obstacle("food-truck", "Parked food truck", TrackKind.Block, new Vector3(1.9f, 2.4f, 4.5f), 1, Cream, heavy: true));
            // Dash City's traffic replaces the old western rider cast and conventional cars.
            d.obstacles.RemoveAll(o => o.id is "courier-a" or "courier-b" or "scooter-courier" or "slow-car" or "taxi" or "garbage-truck");
            d.obstacles.Add(Obstacle("electric-pod", "Autonomous delivery pod", TrackKind.Block, new Vector3(1.9f, 2.2f, 3.6f), 4, RoyalBlue, heavy: true, drift: 0.28f));
            d.obstacles.Add(Obstacle("delivery-bot", "Dash City robot courier", TrackKind.Block, new Vector3(1f, 1.8f, 0.7f), 3, WarmYellow, heavy: true, drift: 0.3f));
            d.obstacles.Add(Obstacle("cargo-slide-gate", "Cargo transfer gate / slide", TrackKind.Overhead, new Vector3(1.95f, 2.32f, 0.8f), 2, RoyalBlue));
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
            sky.SetColor("_Top", new Color(0.025f, 0.035f, 0.13f));
            sky.SetColor("_Horizon", new Color(0.17f, 0.21f, 0.4f));
            sky.SetColor("_Bottom", new Color(0.24f, 0.3f, 0.45f));
            sky.SetColor("_SunColor", new Color(1f, 0.95f, 0.78f));
            sky.SetVector("_SunDir", new Vector4(0.45f, 0.55f, 1f, 0f));
            sky.SetColor("_CloudColor", Color.white);
            sky.SetColor("_CloudShade", new Color(0.64f, 0.74f, 0.92f));
            sky.SetFloat("_CloudCover", 0.2f);
            sky.SetFloat("_CloudSpeed", 0.006f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = mats["Sky"];
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun)
            {
                // High midday sun from behind-left: crisp, readable shadows without drowning the street in shade.
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                sun.color = new Color(1f, 0.96f, 0.86f);
                sun.intensity = 1.15f;
                RenderSettings.sun = sun;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.7f;
            }

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.gameObject.AddComponent<DashRun>();
            gm.gameObject.AddComponent<DallasDash.UI.DashHud>();

            // Rider: Finn on the warm yellow starter bicycle, greybox until the meshes from the character sheets land.
            var rider = new GameObject("Rider (Finn)");
            var riderVisual = new GameObject("Visual").transform;
            riderVisual.SetParent(rider.transform);
            // The crew from the pack, as generated meshes. *-ride are bent into a seated pose by
            // tools/img2mesh/pose_rider.py with the seat at their origin; the standing ones are for Loading Up.
            var look = rider.AddComponent<RiderLook>();
            look.bicycle = Model(riderVisual, DashAssets.Board(), Vector3.zero, "Electric board");
            look.scooter = Model(riderVisual, DashAssets.Board(true), Vector3.zero, "Glide board");
            look.scooter.SetActive(false);
            look.scooterSeat = Vector3.up * 0.2f;
            look.seated = new GameObject[3]; look.standing = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var crew = DashAssets.Crew(i);
                look.seated[i] = Model(riderVisual, crew, i==0 ? Vector3.up*.2f : Vector3.zero, "Crew riding " + i);
                look.standing[i] = Model(riderVisual, crew, new Vector3(.75f,0f,-.35f), "Crew standing " + i);
                if (i==1) look.seated[i].AddComponent<DashMeshMotion>();
                look.seated[i].AddComponent<DashWardrobe>().character=i;
                look.standing[i].AddComponent<DashWardrobe>().character=i;
            }
            look.Apply(0, true);
            var rc = rider.AddComponent<RiderController>();
            rc.visual = riderVisual;
            rc.handling = data.rides[0].handling;

            // No pursuit: the game is a timed delivery course, so there is no officer and no chase rig.
            var spawner = new GameObject("Track").AddComponent<TrackSpawner>();
            spawner.rider = rc;
            spawner.dashBuildings = System.Array.FindAll(new[] { ModelImport.Get("neon-block-a"), ModelImport.Get("neon-block-b") }, g => g);
            spawner.dashBridge = ModelImport.Get("skybridge");
            spawner.rooftopBuilding = ModelImport.Get("rooftop-apartment");
            // Authored street dressing: a lit contemporary lamp and a planted terrace box.
            spawner.dashLamp = PropModels.Lamp();
            spawner.dashPlanter = PropModels.Planter();
            spawner.skyparkTower = ModelImport.Get("skypark-tower");
            spawner.hologramTextures = new[] {
                AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/DashRiders/Visuals/billboard-ace.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/DashRiders/Visuals/billboard-nova.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/DashRiders/Visuals/billboard-food.png") };
            spawner.hologramMaterial = DashAssets.HologramMaterial();
            spawner.chase = null;
            spawner.pickups = data.pickups.ToArray();
            spawner.obstacles = data.obstacles.ToArray();
            spawner.landmarks = System.Array.Empty<LandmarkDef>();
            spawner.roadMaterial = mats["Road"];
            spawner.sidewalkMaterial = mats["Sidewalk"];
            spawner.landmarkMaterial = mats["Landmark"];
            spawner.facadeMaterials = new[] { mats["BrickRed"], mats["BrickTan"], mats["BrickBrown"], mats["PaintTeal"], mats["PaintMint"], mats["PaintPink"], mats["PaintCream"], mats["PaintNavy"] };
            spawner.bulbMaterial = mats["Bulb"];
            spawner.tintMaterial = mats["Tint"];
            spawner.laneLineMaterial = mats["LaneLine"];
            spawner.curbMaterial = mats["Curb"];
            spawner.windowMaterials = new[] { mats["Window"], mats["WindowWarm"] };
            spawner.awningMaterials = new[] { mats["AwningRed"], mats["AwningGreen"], mats["AwningBlue"] };
            spawner.trimMaterial = mats["Trim"];
            spawner.lampPostMaterial = mats["LampPost"];
            spawner.foliageMaterial = mats["Foliage"];
            spawner.skylineMaterials = new[] { mats["Skyline"], mats["SkylineWarm"] };
            spawner.haloMaterial = mats["Halo"];
            spawner.doorMaterial = mats["Door"];
            spawner.plazaMaterial = mats["Plaza"];
            spawner.signFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            spawner.signTextMaterial = mats["SignText"];
            if (KitImport.Available)
            {
                // Modular facades from the Quaternius Downtown City MegaKit (CC0), flattened by KitImport.
                GameObject[] K(params string[] ids) => System.Array.ConvertAll(ids, KitImport.Get);
                spawner.kitGround = K("Trim_FirstFloor_Window_001", "Metal_FirstFloor_Window");
                spawner.kitDoorFrame = KitImport.Get("DoorFrame_Metal_Single");
                spawner.kitBrickWindows = K("Brick_Window_Square_Single", "Brick_Window_Trim_Single");
                spawner.kitTrimWindows = K("Trim_Window");
                spawner.kitMetalWindows = K("Metal_Window_Half", "Metal_FullWindow");
                spawner.kitBrickPlain = KitImport.Get("Brick_Plain_3");
                spawner.kitTrimPlain = KitImport.Get("Trim_Plain_3");
                spawner.kitMetalPlain = KitImport.Get("Metal_Plain_3");
                spawner.kitBrickCornice = KitImport.Get("Cornice_Brick_Center");
                spawner.kitTrimCornice = KitImport.Get("Cornice_Trim_Center");
                spawner.kitMetalCornice = KitImport.Get("Cornice_Metal_Center");
                spawner.kitTrim = KitImport.Material("MI_Trim");
                spawner.kitPaints = KitImport.PaintVariants();
                spawner.kitGlass = KitImport.Material("MI_Glass");
                spawner.kitShopGlass = KitImport.ShopGlass();
                spawner.kitFrame = KitImport.Material(KitImport.FrameMaterial);
                spawner.kitBrickBody = mats["BrickRed"];
                spawner.kitMetalBody = mats["Skyline"];
                spawner.kitTrees = System.Array.ConvertAll(new[] { "CommonTree_3", "CommonTree_4" }, KitImport.GetProp);
                spawner.kitLamp = KitImport.GetProp("street_lamp_01");
                spawner.kitTrash = KitImport.GetProp("metal_trash_can");
                spawner.kitUtility = KitImport.GetProp("utility_box_02");
                spawner.kitBush = KitImport.GetProp("Bush_Common");
                spawner.kitTrafficLight = Fitted("traffic-signal", KitImport.GetProp("road_traffic-light"), 4.2f, Fit.Height);
                // Poly Haven furniture is already in metres.
                spawner.decorPrefabs = System.Array.FindAll(new[] { KitImport.GetProp("planter_box_01"), KitImport.GetProp("wooden_crate_01") }, g => g);
            }
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
            menus.font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Rajdhani-SemiBold.ttf") ?? AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            menus.logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/DashRiders/Visuals/dash-riders-logo.png");
            menus.crew = data.characters.ToArray();

            // Loading Up + On the Clock play from StoryDirector before each run. No pursuit scene.
            var story = new GameObject("Story").AddComponent<StoryDirector>();
            story.rider = rc;
            story.look = look;
            story.chase = null;
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

        /// Bright, punchy grade: bloom on the lit windows and lamps, a little extra saturation and contrast, soft vignette.
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
            bloom.intensity.Override(0.65f);
            bloom.scatter.Override(0.65f);
            var tone = Comp<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);
            var grade = Comp<ColorAdjustments>();
            grade.saturation.Override(12f);
            grade.contrast.Override(12f);
            grade.postExposure.Override(0f);
            grade.colorFilter.Override(Color.white);
            var vignette = Comp<Vignette>();
            vignette.intensity.Override(0.15f);
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
            Part(root, PrimitiveType.Cube, new Vector3(0f, 3.1f, -0.2f), new Vector3(1.7f, 0.62f, 0.12f), mats["SignRed"], "Roof sign");
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
                rt.color = SignRed;
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
                tm.color = SignRed;
                var truckFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
                if (truckFont) { tm.font = truckFont; sign.GetComponent<MeshRenderer>().sharedMaterial = mats["SignText"]; }
                else tm.fontStyle = FontStyle.Bold;
            }
            Directory.CreateDirectory(Root + "/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Root + "/Prefabs/food-truck.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        // ---------- Street hurdles built from the CC0 kits (Kenney cars and roadworks, the pack's bicycle and crew).

        enum Fit { Height, Length, Width }

        /// Wraps a kit prop in a prefab scaled to a real size (Kenney pieces come in kit units), standing on y = 0,
        /// centred, and turned so its long side runs along the street when `alongZ`.
        static GameObject Fitted(string id, GameObject piece, float size, Fit fit, bool alongZ = false)
        {
            if (!piece) return null;
            var root = new GameObject(id).transform;
            var go = FittedChild(root, piece, size, fit, alongZ);
            if (!go) { Object.DestroyImmediate(root.gameObject); return null; }
            return SavePrefab(root);
        }

        static GameObject FittedChild(Transform root, GameObject piece, float size, Fit fit, bool alongZ, float yaw = 0f)
        {
            if (!piece) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(piece);
            go.transform.SetParent(root, false);
            var mf = go.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) return go;
            var b = mf.sharedMesh.bounds;
            bool turn = alongZ && b.size.x > b.size.z;
            float measure = fit == Fit.Height ? b.size.y : fit == Fit.Length ? Mathf.Max(b.size.x, b.size.z) : Mathf.Min(b.size.x, b.size.z);
            float k = size / Mathf.Max(measure, 0.001f);
            go.transform.localScale = Vector3.one * k;
            go.transform.localRotation = Quaternion.Euler(0f, (turn ? 90f : 0f) + yaw, 0f);
            go.transform.localPosition = go.transform.localRotation * new Vector3(-b.center.x * k, 0f, -b.center.z * k) + Vector3.up * (-b.min.y * k);
            return go;
        }

        static GameObject SavePrefab(Transform root)
        {
            Directory.CreateDirectory(Root + "/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{Root}/Prefabs/{root.name}.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        static void SetPrefab(PackData data, string id, GameObject prefab)
        {
            var def = data.obstacles.Find(o => o.id == id);
            if (!def || !prefab) return;
            def.prefab = prefab;
            EditorUtility.SetDirty(def);
        }

        static void BuildStreetHurdles(Dictionary<string, Material> mats, PackData data)
        {
            // Build every authored prop first. Each build rewrites its mesh assets, so a prefab that
            // embeds a prop must be saved only after that prop's meshes are final; otherwise the
            // embedded copy points at a deleted mesh and measures as zero size.
            var cone = PropModels.Cone();
            var barricade = PropModels.Barrier(false);
            var neonBarrier = PropModels.Barrier(true);
            var pod = PropModels.Pod();
            var gate = PropModels.CargoGate();

            SetPrefab(data, "cones", BuildConeRow(cone));
            if (KitImport.Available)
            {
                SetPrefab(data, "roadworks", BuildRoadworks(mats, barricade, cone));
                SetPrefab(data, "delivery-van", BuildStoppedVehicle(mats, "delivery-van", "car_delivery", 5f, true));
                SetPrefab(data, "taxi", BuildStoppedVehicle(mats, "taxi", "car_taxi", 4.4f, true));
                SetPrefab(data, "garbage-truck", BuildStoppedVehicle(mats, "garbage-truck", "car_garbage-truck", 6f, true));
                SetPrefab(data, "slow-car", BuildStoppedVehicle(mats, "slow-car", "car_sedan", 4.4f, false));
            }
            SetPrefab(data, "courier-a", BuildCourier(mats, "courier-a", "finn-ride", new Color(0.12f, 0.6f, 0.5f), "ZIPPY", false));
            SetPrefab(data, "courier-b", BuildCourier(mats, "courier-b", "faye-ride", SafetyOrange, "QUIKBITE", false));
            SetPrefab(data, "scooter-courier", BuildCourier(mats, "scooter-courier", "finn-ride", new Color(0.45f, 0.25f, 0.85f), "SNAPCHOW", true));
            SetPrefab(data, "electric-pod", pod);
            SetPrefab(data, "roadworks-barrier", neonBarrier);
            SetPrefab(data, "cargo-slide-gate", gate);
            SetPrefab(data, "delivery-bot", CrewModels.Build(2, true));
            MeasureHurdles(data);
        }

        /// Hit boxes from the models themselves: each obstacle's size (and so its hit length along the street and,
        /// for low ones, the height a hop has to clear) is the bounds of what is drawn.
        static void MeasureHurdles(PackData data)
        {
            // The props just rewrote their mesh assets, so let the importer finish before measuring.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var def in data.obstacles)
            {
                if (!def.prefab) continue;
                // Re-load from disk: the in-session prefab object still holds null mesh references.
                string path = AssetDatabase.GetAssetPath(def.prefab);
                var source = string.IsNullOrEmpty(path) ? def.prefab : AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!source) source = def.prefab;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
                Bounds b = default;
                bool any = false;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    if (r.GetComponent<TextMesh>()) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (any && b.size.y < 0.01f)
                    foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                    {
                        var mesh = r.GetComponent<MeshFilter>() ? r.GetComponent<MeshFilter>().sharedMesh : null;
                        Debug.Log($"[DallasDash]   zero bounds {def.id}/{r.name}: mesh={(mesh ? mesh.name : "null")} " +
                            $"verts={(mesh ? mesh.vertexCount : 0)} meshBounds={(mesh ? mesh.bounds.size.ToString("0.00") : "-")} " +
                            $"rendererBounds={r.bounds.size.ToString("0.00")}");
                    }
                Object.DestroyImmediate(go);
                if (!any) continue;
                def.size = new Vector3(b.size.x, b.max.y, Mathf.Max(0.3f, b.size.z));
                EditorUtility.SetDirty(def);
                Debug.Log($"[DallasDash] Hurdle {def.id}: {def.kind}, {def.size.x:F2} x {def.size.y:F2} x {def.size.z:F2} m");
            }
        }

        /// Three traffic cones across a lane, so the whole lane that the hit test covers is coned.
        static GameObject BuildConeRow(GameObject cone)
        {
            if (!cone) return null;
            var root = new GameObject("cones").transform;
            foreach (float x in new[] { -0.72f, 0f, 0.72f })
            {
                var c = Embedded(root, cone, new Vector3(x, 0f, 0f), "Cone");
                c.transform.localRotation = Quaternion.Euler(0f, x * 40f, 0f);
            }
            return SavePrefab(root);
        }

        /// Bakes a prop into the prefab being built as plain geometry. A nested prefab instance would
        /// keep a live link to the prop prefab, and the prop's meshes are rewritten (new GUIDs) on the
        /// next bootstrap run, which leaves stale references behind.
        static GameObject Embedded(Transform parent, GameObject prefab, Vector3 pos, string name)
        {
            var go = Model(parent, prefab, pos, name);
            if (go && PrefabUtility.IsPartOfPrefabInstance(go))
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return go;
        }

        /// A car from the Kenney kit facing the way the rider rides, so the rider comes up on its back end;
        /// stopped ones blink their hazards, moving ones show brake lights.
        static GameObject BuildStoppedVehicle(Dictionary<string, Material> mats, string id, string piece, float length, bool stopped)
        {
            var src = KitImport.GetProp(piece);
            if (!src) return null;
            var root = new GameObject(id).transform;
            var car = FittedChild(root, src, length, Fit.Length, true);
            float h = car.GetComponent<MeshFilter>() ? car.GetComponent<MeshRenderer>().bounds.size.y : 1.6f;
            foreach (float x in new[] { -0.72f, 0.72f })
            {
                Part(root, PrimitiveType.Cube, new Vector3(x, h * 0.42f, -length / 2f - 0.02f), new Vector3(0.24f, 0.14f, 0.06f), mats[stopped ? "Amber" : "BrakeRed"], stopped ? "Hazard" : "Brake light");
                if (stopped) root.GetChild(root.childCount - 1).gameObject.AddComponent<Blinker>();
            }
            return SavePrefab(root);
        }

        /// A lane closed for roadworks, 6 m long: the pack's striped barriers front and back with a flashing lamp,
        /// concrete Jersey barriers down both sides, cones across the approach, a ROAD WORK sign, and the dug-up
        /// trench with its spoil heap inside.
        static GameObject BuildRoadworks(Dictionary<string, Material> mats, GameObject barrier, GameObject cone)
        {
            var root = new GameObject("roadworks").transform;
            var jersey = KitImport.GetProp("concrete_road_barrier_02");
            foreach (float z in new[] { -2.55f, 2.55f })
            {
                if (!barrier) break;
                Embedded(root, barrier, new Vector3(0f, 0f, z), "Barrier");
            }
            Part(root, PrimitiveType.Cube, new Vector3(0.7f, 1.0f, -2.75f), new Vector3(0.16f, 0.16f, 0.06f), mats["Amber"], "Lamp");
            root.GetChild(root.childCount - 1).gameObject.AddComponent<Blinker>().perSecond = 1.1f;
            foreach (float x in new[] { -0.72f, 0.72f })
                if (cone) Embedded(root, cone, new Vector3(x, 0f, -2.95f), "Cone");
            foreach (float x in new[] { -0.92f, 0.92f })
            foreach (float z in new[] { -1.05f, 1.05f })
            {
                var f = FittedChild(root, jersey, 2f, Fit.Length, true);
                if (f) f.transform.localPosition += new Vector3(x, 0f, z);
            }
            // The trench and the spoil heap beside it.
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.012f, -0.3f), new Vector3(1.3f, 0.02f, 2.6f), mats["Charcoal"], "Trench");
            Part(root, PrimitiveType.Sphere, new Vector3(0.1f, 0.05f, 1.4f), new Vector3(1.1f, 0.7f, 1f), mats["Kraft"], "Spoil");
            // ROAD WORK diamond on a post, just behind the front barrier.
            Part(root, PrimitiveType.Cylinder, new Vector3(-0.55f, 0.9f, -2.2f), new Vector3(0.06f, 0.9f, 0.06f), mats["Charcoal"], "Sign post");
            Part(root, PrimitiveType.Cube, new Vector3(-0.55f, 1.95f, -2.25f), new Vector3(0.66f, 0.66f, 0.04f), mats["SafetyOrange"], "Sign");
            root.GetChild(root.childCount - 1).localRotation = Quaternion.Euler(0f, 0f, 45f);
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            if (font)
            {
                var label = new GameObject("ROAD WORK", typeof(TextMesh));
                label.transform.SetParent(root, false);
                label.transform.localPosition = new Vector3(-0.55f, 1.95f, -2.28f);
                var tm = label.GetComponent<TextMesh>();
                tm.text = "ROAD\nWORK";
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.characterSize = 0.028f;
                tm.fontSize = 64;
                tm.lineSpacing = 0.85f;
                tm.color = new Color(0.08f, 0.08f, 0.1f);
                tm.font = font;
                label.GetComponent<MeshRenderer>().sharedMaterial = mats["SignText"];
            }
            return SavePrefab(root);
        }

        /// Another courier riding the same way: an authored crew model on a board with a big insulated
        /// backpack in an invented courier service's colours, so from behind they read as someone else.
        /// The Ride child bobs as they ride.
        static GameObject BuildCourier(Dictionary<string, Material> mats, string id, string riderId, Color brand, string service, bool scooter)
        {
            int who = riderId.Contains("nova") || riderId.Contains("faye") ? 1
                : riderId.Contains("bolt") || riderId.Contains("fizz") ? 2 : 0;
            var root = new GameObject(id).transform;
            var ride = new GameObject("Ride").transform;
            ride.SetParent(root, false);
            // Rival couriers ride the same authored crew models as the player, on a board. Bolt keeps his wheel.
            if (who != 2)
            {
                var board = DashAssets.Board(scooter);
                if (board) Model(ride, board, Vector3.zero, "Board");
            }
            var rider = Model(ride, CrewModels.Build(who, true), Vector3.zero, "Rider");
            var bounds = rider.GetComponentInChildren<Renderer>().bounds;
            foreach (var r in rider.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            float top = bounds.max.y;
            // Branded delivery box on the rider's back (the rider faces +Z, so the back is -Z).
            string brandPath = $"{MatDir}/Models/{id}-brand.mat";
            var brandMat = AssetDatabase.LoadAssetAtPath<Material>(brandPath);
            if (!brandMat) { brandMat = new Material(mats["Tint"]); AssetDatabase.CreateAsset(brandMat, brandPath); }
            brandMat.CopyPropertiesFromMaterial(mats["Tint"]);
            brandMat.SetColor("_BaseColor", brand);
            brandMat.SetFloat("_OutlineWidth", 0f);
            EditorUtility.SetDirty(brandMat);
            float backZ = bounds.min.z + 0.05f;
            Part(ride, PrimitiveType.Cube, new Vector3(bounds.center.x, top - 0.62f, backZ - 0.18f), new Vector3(0.5f, 0.5f, 0.36f), brandMat, "Backpack");
            Part(ride, PrimitiveType.Cube, new Vector3(bounds.center.x, top - 0.62f, backZ - 0.37f), new Vector3(0.42f, 0.2f, 0.02f), mats["Trim"], "Backpack panel");
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LilitaOne-Regular.ttf");
            if (font)
            {
                var label = new GameObject(service, typeof(TextMesh));
                label.transform.SetParent(ride, false);
                label.transform.localPosition = new Vector3(bounds.center.x, top - 0.62f, backZ - 0.39f);
                var tm = label.GetComponent<TextMesh>();
                tm.text = service;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.characterSize = 0.012f * 8f / Mathf.Max(6, service.Length);
                tm.fontSize = 96;
                tm.color = brand * 0.8f;
                tm.font = font;
                label.GetComponent<MeshRenderer>().sharedMaterial = mats["SignText"];
            }
            var bob = ride.gameObject.AddComponent<Bobber>();
            bob.stepsPerSecond = 1.6f;
            bob.bounce = 0.025f;
            bob.sway = 2.5f;
            return SavePrefab(root);
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

        static ObstacleDef Obstacle(string id, string name, TrackKind kind, Vector3 size, int weight, Color c, bool heavy = false, float drift = 0f)
        {
            var a = Load<ObstacleDef>($"{DataDir}/Obstacles/{id}.asset");
            a.id = id; a.displayName = name; a.kind = kind; a.size = size; a.weight = weight; a.placeholderColor = c;
            a.heavy = heavy; a.drift = drift;
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
