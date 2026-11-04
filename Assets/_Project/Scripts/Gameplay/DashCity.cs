using System.Collections.Generic;
using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Shared materials and static batches keep the neon city inexpensive on mobile.
    public static class DashCity
    {
        static readonly Dictionary<string, Material> materials = new();
        static readonly HashSet<Material> naturalFoliage = new();
        public static Color Accent(int district) => district == 2 ? new Color(.77f, .57f, .24f) : new Color(.84f, .79f, .64f);
        public static void ApplyLighting(TrackSpawner track)
        {
            int d = DashRun.District;
            // Festivals is a snowy Christmas street: the road is packed snow (tyre ruts are added per block by
            // FestivalDecor), the pavements are snow-covered and the lane paint shows through dark.
            track.roadMaterial = d == 2 ? Mat(track, "snowPackedRoad", new Color(.86f,.89f,.95f), .04f)
                : Mat(track, "road" + d, d == 1 ? new Color(.48f,.49f,.46f) : new Color(.42f,.43f,.44f));
            track.sidewalkMaterial = Mat(track, "walk" + d, d == 2 ? new Color(.95f,.97f,1f) : new Color(.66f,.62f,.54f));
            track.laneLineMaterial = Mat(track, "lanes" + d, d == 2 ? new Color(.32f,.36f,.46f) : new Color(.83f, .8f, .68f));
            ApplyNaturalLight(d);
            NightLighting.Ensure();
            SkyTraffic.Ensure(track);
            ChristmasSnow.Ensure(d == 2, ChristmasSnow.FlakeMaterial(track));
            if (track.kitTrees != null) foreach (var tree in track.kitTrees) {
                if (!tree) continue;
                foreach (var renderer in tree.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials) {
                    if (!material || !naturalFoliage.Add(material)) continue;
                    if (material.HasProperty("_SurfaceStyle")) material.SetFloat("_SurfaceStyle",1);
                    if (material.HasProperty("_RimStrength")) material.SetFloat("_RimStrength",0);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",material.GetColor("_BaseColor")*new Color(.65f,.78f,.6f,1));
                }
            }
        }
        /// The PDF cover's dusk: a violet sky with the sunset glowing on the horizon ahead, the city lit by
        /// its own neon and windows. The low sun backlights the crew and throws long shadows toward camera.
        public static void ApplyNaturalLight(int district)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fogColor = district == 2 ? new Color(.3f,.13f,.3f) : new Color(.26f,.13f,.33f);
            RenderSettings.fogStartDistance = 85f; RenderSettings.fogEndDistance = 240f;
            RenderSettings.ambientSkyColor = new Color(.34f,.27f,.58f);
            RenderSettings.ambientEquatorColor = new Color(.36f,.2f,.38f);
            RenderSettings.ambientGroundColor = new Color(.07f,.05f,.1f);
            var dusk = Resources.Load<Material>("DuskSky");
            var night = Resources.Load<Material>("NightSky");
            if (dusk) RenderSettings.skybox = dusk;
            else if (night) RenderSettings.skybox = night;
            if (night && night.GetTexture("_Tex") is Cubemap reflections) {
                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
                RenderSettings.customReflection = reflections;
            }
            RenderSettings.reflectionIntensity = .8f;
            if (RenderSettings.sun)
            {
                RenderSettings.sun.color = new Color(1f,.58f,.5f);
                RenderSettings.sun.intensity = .85f;
                RenderSettings.sun.transform.rotation = Quaternion.Euler(24f,195f,0);
                RenderSettings.sun.shadowStrength = .8f;
                RenderSettings.sun.shadowBias = .035f;
                RenderSettings.sun.shadowNormalBias = .2f;
            }
        }
        public static Material Mat(TrackSpawner track, string key, Color color, float glow = 0f)
        {
            if (materials.TryGetValue(key, out var material) && material) return material;
            material = new Material(track.tintMaterial);
            material.name = "Dash " + key;
            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", color * glow);
            material.SetFloat("_SurfaceStyle", 1f);
            material.SetFloat("_RimStrength", 0f);
            material.SetFloat("_Smoothness", 0.42f);
            material.SetFloat("_Metallic", 0.08f);
            var surface = key.StartsWith("road") ? track.asphaltTexture : key.StartsWith("walk") || key.StartsWith("promenade") && !key.Contains("Warm") ? track.pavingTexture : null;
            if (surface) {
                material.SetTexture("_BaseMap", surface); material.SetFloat("_WorldTextureScale", .32f);
                material.SetTexture("_BumpMap", key.StartsWith("road") ? track.asphaltNormal : track.pavingNormal);
                material.SetFloat("_BumpScale", .65f); material.SetFloat("_Smoothness", key.StartsWith("road") ? .22f : .16f);
                material.SetFloat("_Metallic", 0f);
                material.SetTexture("_RoughnessMap",key.StartsWith("road") ? track.asphaltRoughness : track.pavingRoughness);
                material.SetFloat("_RoughnessStrength",1);
                material.SetFloat("_Wetness",key.StartsWith("road") ? .78f : .4f);
            }
            materials[key] = material;
            return material;
        }
        public static GameObject Part(Transform parent, Vector3 position, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        public static void Text(TrackSpawner track, Transform parent, string word, Vector3 at, float size, Color color)
        {
            var go = new GameObject(word);
            go.transform.SetParent(parent, false); go.transform.position = at;
            go.transform.rotation = Quaternion.identity;
            var tm = go.AddComponent<TextMesh>(); tm.text = word; tm.fontSize = 64;
            tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.color = WorldPalette.Surface(color);
            if (track.signFont) tm.font = track.signFont;
            go.GetComponent<MeshRenderer>().sharedMaterial = track.signTextMaterial;
        }
        public static void Side(TrackSpawner track, Transform parent, float side, float z)
        {
            if (track.dashBuildings != null && track.dashBuildings.Length > 0)
            {
                ArchitecturalSide(track, parent, side, z);
                return;
            }
            int d = DashRun.District;
            int block = Mathf.FloorToInt(z / 20f);
            var accent = Mat(track, "accent" + d, Accent(d), 0.8f);
            var pink = Mat(track, "pink", new Color(1f, 0.12f, 0.5f), 0.8f);
            var ink = Mat(track, "ink", new Color(0.055f, 0.075f, 0.16f));
            var wall = Mat(track, "wall" + d, d == 1 ? new Color(0.32f, 0.48f, 0.55f) : d == 2 ? new Color(0.16f, 0.09f, 0.3f) : new Color(0.12f, 0.17f, 0.3f));
            var glass = Mat(track, "glass" + d, d == 1 ? new Color(0.16f, 0.5f, 0.56f) : new Color(0.13f, 0.23f, 0.43f));
            var warm = Mat(track, "warm", new Color(1f, 0.59f, 0.2f), 0.5f);
            float mid = z + 10f;
            if (d == 1)
            {
                // The course is the roof deck; the neighbouring towers continue far below it.
                Part(parent, new Vector3(side * 13f, -20f, mid), new Vector3(13f, 48f, 18f), wall);
                Part(parent, new Vector3(side * 6f, -0.5f, mid), new Vector3(4f, 1f, 20f), wall);
                Part(parent, new Vector3(side * 7.3f, 0.65f, mid), new Vector3(0.16f, 1.3f, 20f), glass);
                Part(parent, new Vector3(side * 7.3f, 1.32f, mid), new Vector3(0.18f, 0.08f, 20f), accent);
                for (int k = 0; k < 3; k++)
                {
                    float pz = z + 3f + k * 6f;
                    Part(parent, new Vector3(side * 5.8f, 0.3f, pz), new Vector3(2f, 0.6f, 2f), ink);
                    Part(parent, new Vector3(side * 5.8f, 0.8f, pz), new Vector3(1.8f, 0.7f, 1.8f), track.foliageMaterial);
                }

                // NEW: Building parkour elements - climbable ledges and rooftop shortcuts
                if (block % 3 == 0)
                {
                    // Horizontal parkour ledge at rooftop height
                    float ledgeZ = z + 5f + (block % 2) * 10f;
                    var ledgeMat = Mat(track, "parkourLedge", new Color(0.45f, 0.65f, 0.85f), 0.3f);
                    Part(parent, new Vector3(side * 8.5f, 2.8f, ledgeZ), new Vector3(1.2f, 0.25f, 6f), ledgeMat);

                    // Vertical climb rails
                    if (block % 2 == 0)
                    {
                        var railMat = Mat(track, "climbRail", new Color(0.85f, 0.45f, 0.25f), 0.5f);
                        Part(parent, new Vector3(side * 9.2f, 1.5f, ledgeZ), new Vector3(0.08f, 3f, 0.12f), railMat);
                        Part(parent, new Vector3(side * 9.2f, 3.2f, ledgeZ), new Vector3(0.15f, 0.15f, 0.15f), railMat);
                    }

                    // Rooftop platforms with bonus pickups
                    if (block % 4 == 0)
                    {
                        var platformMat = Mat(track, "rooftopPlatform", new Color(0.35f, 0.55f, 0.75f), 0.2f);
                        Part(parent, new Vector3(side * 7.8f, 3.5f, ledgeZ), new Vector3(2.5f, 0.15f, 3f), platformMat);
                        // Support pillars
                        Part(parent, new Vector3(side * 7f, 1.75f, ledgeZ), new Vector3(0.15f, 3.5f, 0.15f), wall);
                        Part(parent, new Vector3(side * 8.6f, 1.75f, ledgeZ), new Vector3(0.15f, 3.5f, 0.15f), wall);
                    }
                }
            }
            else
            {
                float height = 10f + Mathf.Abs(block * 7 % 4) * 5f;
                Part(parent, new Vector3(side * 12f, height / 2f, mid), new Vector3(10f, height, 19f), wall);
                Part(parent, new Vector3(side * 6.9f, 2.25f, mid), new Vector3(0.2f, 4.1f, 18f), glass);
                for (int level = 1; level < 5; level++)
                {
                    float h = level * 3f + 3f;
                    if (h > height - 1f) break;
                    Part(parent, new Vector3(side * 6.85f, h, mid), new Vector3(0.12f, 0.12f, 18f), accent);
                    for (int j = 0; j < 4; j++)
                        Part(parent, new Vector3(side * 6.8f, h - 1.2f, z + 2.4f + j * 4.5f), new Vector3(0.16f, 1.6f, 2.6f), glass);
                }
                Part(parent, new Vector3(side * 6.65f, 4.65f, mid), new Vector3(0.35f, 0.25f, 19f), block % 2 == 0 ? accent : pink);
                // Food stalls, lit service counters and original street-market signs.
                Part(parent, new Vector3(side * 5.65f, 1.1f, z + 7f), new Vector3(1.8f, 2.2f, 4.5f), ink);
                Part(parent, new Vector3(side * 5.65f, 2.3f, z + 7f), new Vector3(2f, 0.18f, 4.8f), warm);
                Part(parent, new Vector3(side * 5.65f, 1.05f, z + 4.72f), new Vector3(1.7f, 0.14f, 0.3f), accent);
                if (track.pickups != null && track.pickups.Length > 1)
                {
                    var food = track.pickups[1 + Mathf.Abs(block) % (track.pickups.Length - 1)].prefab;
                    if (food)
                    {
                        var display = Object.Instantiate(food, parent);
                        display.name = "Market food display";
                        display.transform.position = new Vector3(side * 5.65f, 1.12f, z + 4.65f);
                        display.transform.localScale = Vector3.one * 0.65f;
                    }
                }
                Part(parent, new Vector3(side * 10f, 3.15f, z + 0.2f), new Vector3(7f, 1.3f, 0.16f), ink);
                string[] words = { "FOOD INC", "TACO LAB", "PIZZA ORBIT", "BURGER BYTE", "DASH MARKET" };
                Text(track, parent, words[Mathf.Abs(block) % words.Length], new Vector3(side * 10f, 3.15f, z + 0.08f), 0.075f, Accent(d));
            }
            // Tall silhouettes and vertical light strips replace the Dallas landmarks.
            float towerHeight = 28f + Mathf.Abs(block * 13 % 5) * 7f;
            Part(parent, new Vector3(side * 29f, towerHeight / 2f - (d == 1 ? 12f : 0f), mid), new Vector3(11f, towerHeight, 15f), wall);
            Part(parent, new Vector3(side * 23.4f, towerHeight / 2f, mid), new Vector3(0.2f, towerHeight, 0.18f), accent);
            if (d == 2)
            {
                // Festival light pylons and a stage beside the course.
                Part(parent, new Vector3(side * 5f, 3f, mid), new Vector3(0.25f, 6f, 0.25f), pink);
                for (int b = 0; b < 5; b++)
                    Part(parent, new Vector3(side * (8f + b * 0.65f), 0.8f + b % 3 * 0.4f, z + 1f), new Vector3(0.4f, 1.5f + b % 3 * 0.8f, 0.3f), b % 2 == 0 ? pink : accent);
            }
            if (side < 0 && block % 4 == 0)
            {
                Part(parent, new Vector3(0, 7.8f, mid), new Vector3(14f, 0.4f, 3f), ink);
                Part(parent, new Vector3(0, 8.1f, mid), new Vector3(14f, 0.12f, 3f), accent);
                Text(track, parent, "DASH CITY  /  " + DashRun.DistrictNames[d], new Vector3(0, 8.4f, mid - 1.56f), 0.065f, Color.white);
            }
        }
        public static void Signature(TrackSpawner track, Transform parent, float z)
        {
            if (DashRun.District == 2) FestivalDecor.Build(track, parent, z);
            float checkpoint = (Mathf.Floor(z / 450f) + 1) * 450f;
            if (checkpoint > 0f && checkpoint >= z && checkpoint < z + track.segmentLength)
            {
                var delivery = Mat(track, "delivery", new Color(1f, 0.55f, 0.08f), 0.6f);
                foreach (float side in new[] { -1f, 1f })
                    Part(parent, new Vector3(side * 4.2f, 2.6f, checkpoint), new Vector3(0.22f, 5.2f, 0.22f), delivery);
                Part(parent, new Vector3(0, 5.2f, checkpoint), new Vector3(8.6f, 0.3f, 0.5f), delivery);
                Text(track, parent, "FOOD INC  /  DELIVERY DROP", new Vector3(0, 5.65f, checkpoint - 0.1f), 0.065f, Color.white);
            }
            // The wall ride uses the buildings' own facades (FacadeWall). The gravity section is a passage
            // through a building: its soffit is the ceiling, with the block's upper floors over the street.
            bool inStart = DashRun.GravityZone(z + 0.01f), inEnd = DashRun.GravityZone(z + track.segmentLength - 0.01f);
            if (!inStart && !inEnd) return;
            float start = DashRun.GravityZoneStart(inStart ? z + 0.01f : z + track.segmentLength - 0.01f);
            float z0 = Mathf.Max(z, start), z1 = Mathf.Min(z + track.segmentLength, start + 90f);
            float len = z1 - z0, mid = (z0 + z1) * 0.5f, ceiling = SurfaceTraversal.CeilingHeight;
            var soffit = Mat(track, "passageSoffit", new Color(0.09f, 0.1f, 0.14f));
            var cyan = Mat(track, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
            var pink = Mat(track, "roofNeonPink", new Color(1f, 0.1f, 0.55f), 2.2f);
            var upper = Mat(track, "passageUpper", new Color(0.26f, 0.25f, 0.36f));
            var warm = Mat(track, "occupied-window", new Color(1f, .65f, .3f), 1.1f);
            float wide = SurfaceTraversal.FacadeX * 2f;
            Part(parent, new Vector3(0, ceiling + 0.12f, mid), new Vector3(wide + 0.2f, 0.24f, len), soffit);
            Part(parent, new Vector3(0, ceiling + 0.24f + 7f, mid), new Vector3(wide + 0.2f, 14f, len), upper);
            // Recessed light lines in the soffit mark the three ceiling lanes.
            foreach (float lx in new[] { -1.15f, 1.15f })
                Part(parent, new Vector3(lx, ceiling - 0.01f, mid), new Vector3(0.06f, 0.02f, len - 0.4f), (Mathf.FloorToInt(z0 / 20f) & 1) == 0 ? cyan : pink);
            for (float wz = z0 + 2f; wz < z1; wz += 4f)
                Part(parent, new Vector3(0, ceiling - 0.005f, wz), new Vector3(wide - 1f, 0.01f, 0.08f), soffit);
            // The block's street elevation over the passage mouth, facing the rider as they arrive.
            if (Mathf.Abs(z0 - start) < 0.01f)
            {
                Part(parent, new Vector3(0, ceiling + 0.6f, z0 - 0.05f), new Vector3(wide + 0.4f, 0.5f, 0.12f), pink);
                for (int f = 0; f < 4; f++)
                    for (int k = -3; k <= 3; k++)
                        Part(parent, new Vector3(k * 2f, ceiling + 2.2f + f * 3.2f, z0 - 0.04f), new Vector3(1.4f, 1.9f, 0.06f), ((f + k) & 3) == 0 ? warm : soffit);
                Text(track, parent, "DASH ARCADE  /  GRAVITY FLIP", new Vector3(0, ceiling + 0.6f, z0 - 0.13f), 0.05f, Color.white);
            }
        }

        /// Wall and gravity sections run along buildings built right up to the facade line, so the wall the
        /// rider rides is a real street elevation, kept flat across the three wall lanes.
        public static bool FacadeZone(float z, float length) =>
            DashRun.WallZone(z + 0.01f) || DashRun.WallZone(z + length - 0.01f) || DashRun.GravityZone(z + 0.01f) || DashRun.GravityZone(z + length - 0.01f);

        /// The street edge is open here: no kerbside furniture or parapet between the lanes and the wall/roofs.
        public static bool OpenEdge(float side, float z, float length) =>
            FacadeZone(z, length) || side > 0f && (RooftopCourse.OwnsRightSide(z) || RooftopCourse.OwnsRightSide(z + length));

        public static void FacadeWall(TrackSpawner track, Transform parent, float side, float z)
        {
            int d = DashRun.District, block = Mathf.FloorToInt(z / 20f);
            float baseY = d == 1 ? TrackSpawner.CityGround : 0f;
            float top = 15f + Mathf.Abs(block * 5 % 3) * 3.2f;
            float face = side * SurfaceTraversal.FacadeX, mid = z + 10f;
            int variant = Mathf.Abs(block * 3 + (side > 0 ? 1 : 0)) % 4;
            Color[] tones = { new(.3f, .28f, .4f), new(.22f, .27f, .36f), new(.38f, .28f, .34f), new(.25f, .28f, .3f) };
            var body = Mat(track, "facadeBlock-" + variant, tones[variant]);
            var trim = Mat(track, "limestone-trim", new Color(.59f, .56f, .49f));
            var warm = Mat(track, "occupied-window", new Color(1f, .65f, .3f), 1.1f);
            var cool = Mat(track, "office-window", new Color(.35f, .65f, 1f), .65f);
            var dark = Mat(track, "architectural-glass", new Color(.085f, .18f, .23f));
            var cyan = Mat(track, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
            var pink = Mat(track, "roofNeonPink", new Color(1f, 0.1f, 0.55f), 2.2f);
            var steel = Mat(track, "roofSteel", new Color(0.16f, 0.18f, 0.21f));
            const float depth = 10f;
            Part(parent, new Vector3(face + side * depth * 0.5f, (baseY + top) * 0.5f, mid), new Vector3(depth, top - baseY, 20f), body);
            // Ground-floor shopfronts, flush with the face, under a neon fascia.
            Part(parent, new Vector3(face - side * 0.02f, 1.5f, mid), new Vector3(0.05f, 2.4f, 18.6f), (block & 1) == 0 ? warm : cool);
            Part(parent, new Vector3(face - side * 0.03f, 2.95f, mid), new Vector3(0.04f, 0.16f, 19.6f), side < 0 ? cyan : pink);
            // Window bays on every floor, inset only a few centimetres so the wall stays rideable.
            for (float y = 4.6f; y < top - 1f; y += 3.2f)
            {
                Part(parent, new Vector3(face - side * 0.015f, y - 1.42f, mid), new Vector3(0.03f, 0.12f, 20f), trim);
                for (int k = 0; k < 5; k++)
                {
                    int lit = (Mathf.RoundToInt(y) * 7 + k * 5 + variant + block) % 6;
                    Part(parent, new Vector3(face - side * 0.01f, y, z + 2f + k * 4f), new Vector3(0.02f, 2f, 2.6f), lit < 2 ? warm : lit == 2 ? cool : dark);
                }
            }
            // Lit sills between the wall lanes double as the wall ride's lane lines.
            foreach (float y in new[] { SurfaceTraversal.WallMid - 1.15f, SurfaceTraversal.WallMid + 1.15f })
                for (float dz = 1f; dz < 20f; dz += 5f)
                    Part(parent, new Vector3(face - side * 0.02f, y, z + dz + 1.25f), new Vector3(0.03f, 0.06f, 2.5f), side < 0 ? cyan : pink);
            // Cornice, rooftop plant and a lit sign on top of the block.
            Part(parent, new Vector3(face + side * depth * 0.5f, top + 0.15f, mid), new Vector3(depth + 0.3f, 0.3f, 20.2f), trim);
            Part(parent, new Vector3(face + side * 3f, top + 1f, z + 6f), new Vector3(2.4f, 1.6f, 3f), steel);
            if (block % 2 == 0)
            {
                Part(parent, new Vector3(face + side * 1.2f, top + 2.2f, mid), new Vector3(0.2f, 3.4f, 9f), steel);
                Part(parent, new Vector3(face + side * 1.08f, top + 2.2f, mid), new Vector3(0.04f, 3f, 8.6f), side < 0 ? pink : cyan);
            }
        }
        // Outside the playable lanes: real-scale curb joints, drainage and service equipment.
        // The existing segment mesh combiner batches these with the surrounding architecture.
        static void StreetDetail(TrackSpawner track, Transform parent, float side, float z, int district)
        {
            var concrete = Mat(track, "curbConcrete", new Color(.48f,.45f,.39f));
            var iron = Mat(track, "drainIron", new Color(.065f,.075f,.08f));
            var steel = Mat(track, "serviceSteel", new Color(.28f,.31f,.32f));
            var timber = Mat(track, "benchTimber", new Color(.23f,.11f,.045f));
            var joint = Mat(track, "curbJoint", new Color(.11f,.105f,.095f));
            float x = side * 4.28f;
            Part(parent, new Vector3(x,.10f,z+10), new Vector3(.26f,.24f,20), concrete);
            for (int i=0;i<10;i++)
                Part(parent, new Vector3(x,.224f,z+i*2+.05f), new Vector3(.27f,.008f,.025f), joint);
            Part(parent, new Vector3(side*4.02f,.012f,z+5), new Vector3(.3f,.018f,1.15f), iron);
            for (int i=0;i<7;i++)
                Part(parent, new Vector3(side*4.02f,.024f,z+4.52f+i*.15f), new Vector3(.29f,.012f,.035f), steel);
            // Slatted seating has separate feet/back, rather than a solid debug block.
            for (int i=0;i<4;i++)
                Part(parent, new Vector3(side*(5.25f+i*.13f),.58f,z+15), new Vector3(.10f,.055f,1.8f), timber);
            foreach (float dz in new[]{-.65f,.65f})
                Part(parent, new Vector3(side*5.45f,.3f,z+15+dz), new Vector3(.42f,.55f,.065f), iron);
            if (district == 1)
            {
                // Roof mechanical plant, with a raised plinth and repeated louvres.
                Part(parent,new Vector3(side*9f,.12f,z+4),new Vector3(1.8f,.24f,2.8f),concrete);
                Part(parent,new Vector3(side*9f,.8f,z+4),new Vector3(1.5f,1.15f,2.4f),steel);
                for(int i=0;i<6;i++)
                    Part(parent,new Vector3(side*8.23f,.4f+i*.14f,z+4),new Vector3(.025f,.045f,2.1f),iron);
            }
        }

        static void ArchitecturalSide(TrackSpawner track, Transform parent, float side, float z)
        {
            int d = DashRun.District, block = Mathf.FloorToInt(z / 20f);
            // The right-hand side of a rooftop zone is the shortcut's own block row (TrackSpawner.Rooftops),
            // and wall/gravity sections are built up to the facade line; both keep the kerb clear.
            if (side > 0f && (RooftopCourse.OwnsRightSide(z) || RooftopCourse.OwnsRightSide(z + track.segmentLength))) return;
            if (FacadeZone(z, track.segmentLength)) { FacadeWall(track, parent, side, z); return; }
            const bool route = false;
            NaturalArchitecture.Build(track, parent, side, z, route);

            var frame = Mat(track, "billboardFrame", new Color(0.11f, 0.16f, 0.23f));
            var edge = Mat(track, "billboardEdge" + d, Accent(d), 0f);
            var greenery = track.foliageMaterial;
            // A continuous promenade gives the route a clear edge. Keep the express entry clear.
            if (!route)
            {
                StreetDetail(track, parent, side, z, d);
                var stone = Mat(track, "promenade" + d, d == 1 ? new Color(.43f,.54f,.53f) : new Color(.14f,.19f,.27f));
                var warmLight = Mat(track, "promenadeWarm", new Color(.45f,.33f,.19f), .08f);
                Part(parent, new Vector3(side * 6f, -.12f, z + 10f), new Vector3(3.8f,.22f,20f), stone);
                Part(parent, new Vector3(side * 4.12f, .03f, z + 10f), new Vector3(.09f,.06f,19.8f), edge);
                for (int tile = 0; tile < 4; tile++)
                    Part(parent, new Vector3(side * 6f, .005f, z + 2.5f + tile * 5f), new Vector3(3.6f,.012f,.035f), frame);
                if (block % 2 == 0)
                {
                    Part(parent, new Vector3(side * 6.5f,.24f,z+6f), new Vector3(1.1f,.48f,2.4f), frame);
                    Part(parent, new Vector3(side * 6.5f,.65f,z+6f), new Vector3(1f,.7f,2.2f), greenery, PrimitiveType.Sphere);
                    Part(parent, new Vector3(side * 5.3f,.45f,z+12f), new Vector3(.7f,.12f,2.2f), stone);
                    foreach (float dz in new[] {-0.8f,.8f})
                        Part(parent, new Vector3(side * 5.3f,.2f,z+12f+dz), new Vector3(.5f,.4f,.12f), frame);
                }
                // Warm service windows and shaded awnings break up the repeated cool facades.
                if (d != 1)
                {
                    Part(parent, new Vector3(side * 8.4f,1.65f,z+8f), new Vector3(.12f,2.1f,4.2f), warmLight);
                    Part(parent, new Vector3(side * 7.7f,2.85f,z+8f), new Vector3(1.6f,.16f,4.6f), frame);
                }
                if (d == 1)
                {
                    Part(parent, new Vector3(side * 7.8f,.7f,z+10f), new Vector3(.12f,1.4f,20f), frame);
                    Part(parent, new Vector3(side * 7.8f,1.42f,z+10f), new Vector3(.15f,.06f,20f), edge);
                }
                // Festivals: fabric banners on the lamp posts (real street-festival dressing; the old sphere
                // bulbs here sat in the wall-run line).
                if (d == 2)
                    foreach (float lampZ in new[] { z + 4f, z + 15f })
                    {
                        Part(parent, new Vector3(side * 4.72f, 3.85f, lampZ), new Vector3(.03f, .05f, .62f), frame);
                        Part(parent, new Vector3(side * 4.72f, 3.25f, lampZ), new Vector3(.02f, 1.15f, .56f), block % 2 == 0 ? edge : warmLight);
                    }
            }

            // Garden terraces and balconies continue through the foreground, as on the cover.
            if (d == 1 && !route)
            {
                Part(parent, new Vector3(side * 5.8f, -0.3f, z + 10f), new Vector3(4f, 0.6f, 20f), frame);
                for (int i = 0; i < 3; i++)
                {
                    var at = new Vector3(side * 6f, 0.3f, z + 3f + i * 6f);
                    if (track.dashPlanter)
                    {
                        var planter = Object.Instantiate(track.dashPlanter, at, Quaternion.identity, parent);
                        planter.name = "Planted terrace box";
                        continue;
                    }
                    Part(parent, at, new Vector3(1.6f, 0.6f, 2.8f), track.trimMaterial);
                    Part(parent, new Vector3(side * 6f, 0.75f, z + 3f + i * 6f), new Vector3(1.5f, 0.7f, 2.6f), greenery);
                }

                // NEW: Skypark building parkour - climbable structures for vertical traversal
                if (block % 4 == 0)
                {
                    // Rooftop access structure
                    var parkourMat = Mat(track, "parkourStruct", new Color(0.4f, 0.6f, 0.8f), 0.35f);
                    // Side-mounted ladder
                    for (int rung = 0; rung < 4; rung++)
                    {
                        Part(parent, new Vector3(side * 6.5f, 0.5f + rung * 0.8f, z + 12f), new Vector3(0.1f, 0.06f, 0.4f), parkourMat);
                    }
                    // Vertical rails
                    Part(parent, new Vector3(side * 6.4f, 1.8f, z + 12f), new Vector3(0.06f, 3.2f, 0.06f), parkourMat);
                    Part(parent, new Vector3(side * 6.6f, 1.8f, z + 12f), new Vector3(0.06f, 3.2f, 0.06f), parkourMat);
                    // Top platform
                    Part(parent, new Vector3(side * 6.5f, 3.6f, z + 12f), new Vector3(1.8f, 0.12f, 2.5f), parkourMat);
                }
            }
            if (block % 4 == 0 && !route)
            {
                float h = d == 1 ? 5.5f : 4.3f;
                var at = new Vector3(side * 5f, h, z + 9f); // street side of the wall-run line (x 6.2-8)
                var panel = new GameObject("Painted district sign").transform;
                panel.SetParent(parent, false); panel.position = at;
                panel.rotation = Quaternion.Euler(0, side > 0 ? -125 : 125, 0);
                var backing = Part(panel, Vector3.zero, new Vector3(3.8f, 1.6f, 0.16f), frame);
                backing.transform.localPosition = Vector3.zero; backing.transform.localRotation = Quaternion.identity;
                foreach (float edgeY in new[] { -.77f, .77f })
                {
                    var rim = Part(panel, Vector3.zero, new Vector3(3.8f, 0.045f, 0.18f), edge);
                    rim.transform.localPosition = Vector3.up * edgeY; rim.transform.localRotation = Quaternion.identity;
                }
                var lettering = new GameObject("Painted sign lettering", typeof(TextMesh)); lettering.transform.SetParent(panel, false);
                lettering.transform.localPosition = new Vector3(0, 0, -.12f);
                var text = lettering.GetComponent<TextMesh>(); text.text = d == 2 ? "CHRISTMAS MARKET\nFOOD INC" : "FOOD INC\nDELIVERY DISTRICT";
                text.font = track.signFont; text.fontSize = 64; text.characterSize = .06f; text.anchor = TextAnchor.MiddleCenter;
                text.color = new Color(.94f, .89f, .74f); lettering.GetComponent<Renderer>().sharedMaterial = track.signTextMaterial;
            }
            // Connected multi-level urban walkways, with real suspension geometry and garden ends.
            if (side < 0 && track.dashBridge && Mathf.Abs(block) % 4 == 1)
            {
                var bridge = Object.Instantiate(track.dashBridge, new Vector3(0, 8f, z + 10f), Quaternion.identity, parent); // above the 5.2 m wall-run line
                bridge.name = "Suspended garden walkway";
            }
            // A clean contemporary lamp, replacing the previous Victorian street lamps.
            if (!route)
                foreach (float lampZ in new[] { z + 4f, z + 15f })
                {
                    if (track.dashLamp)
                    {
                        var lamp = Object.Instantiate(track.dashLamp, new Vector3(side * 4.6f, 0f, lampZ),
                            Quaternion.Euler(0f, side > 0f ? 0f : 180f, 0f), parent);
                        lamp.name = "Dash City lamp";
                        continue;
                    }
                    Part(parent, new Vector3(side * 4.6f, 2.5f, lampZ), new Vector3(0.11f, 5f, 0.11f), frame);
                    Part(parent, new Vector3(side * 4.1f, 5.05f, lampZ), new Vector3(1.15f, 0.09f, 0.22f), edge);
                }
            if (d == 2 && !route) for (int i = 0; i < 6; i++)
                Part(parent, new Vector3(side * (5.2f + i * 0.3f), 0.5f + i % 3 * 0.25f, z + 2f), new Vector3(0.16f, 1f + i % 3 * 0.5f, 0.18f), edge);
        }
    }
}
