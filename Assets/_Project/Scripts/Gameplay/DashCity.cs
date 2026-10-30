using System.Collections.Generic;
using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Shared materials and static batches keep the neon city inexpensive on mobile.
    public static class DashCity
    {
        static readonly Dictionary<string, Material> materials = new();
        public static Color Accent(int district) => district == 1 ? new Color(0.15f, 0.95f, 0.65f) : district == 2 ? new Color(1f, 0.18f, 0.6f) : new Color(0.1f, 0.85f, 1f);
        public static void ApplyLighting(TrackSpawner track)
        {
            int d = DashRun.District;
            track.roadMaterial = Mat(track, "road" + d, d == 1 ? new Color(0.25f, 0.38f, 0.43f) : new Color(0.055f, 0.075f, 0.14f));
            track.sidewalkMaterial = Mat(track, "walk" + d, d == 1 ? new Color(0.55f, 0.7f, 0.68f) : new Color(0.12f, 0.18f, 0.25f));
            track.laneLineMaterial = Mat(track, "lanes" + d, Accent(d) * 0.75f, 0.15f);
            var sky = RenderSettings.skybox;
            if (sky)
            {
                sky.SetColor("_Top", d == 1 ? new Color(0.04f, 0.23f, 0.5f) : d == 2 ? new Color(0.045f, 0.008f, 0.12f) : new Color(0.025f, 0.055f, 0.16f));
                sky.SetColor("_Horizon", d == 1 ? new Color(0.45f, 0.72f, 0.78f) : d == 2 ? new Color(0.18f, 0.045f, 0.26f) : new Color(0.32f, 0.24f, 0.42f));
                sky.SetFloat("_CloudCover", d == 1 ? 0.5f : 0.14f);
            }
            RenderSettings.fogColor = d == 1 ? new Color(0.56f, 0.78f, 0.85f) : d == 2 ? new Color(0.28f, 0.13f, 0.34f) : new Color(0.53f, 0.46f, 0.58f);
            RenderSettings.ambientSkyColor = d == 1 ? new Color(0.55f, 0.72f, 0.85f) : new Color(0.28f, 0.36f, 0.6f);
            RenderSettings.ambientEquatorColor = d == 1 ? new Color(0.35f, 0.44f, 0.5f) : new Color(0.16f, 0.2f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.15f, 0.24f);
            if (RenderSettings.sun)
            {
                RenderSettings.sun.color = d == 1 ? new Color(1f, 0.95f, 0.85f) : d == 2 ? new Color(1f, 0.55f, 0.8f) : new Color(1f, 0.8f, 0.66f);
                RenderSettings.sun.intensity = d == 2 ? 0.85f : 1.4f;
            }
        }
        public static Material Mat(TrackSpawner track, string key, Color color, float glow = 0f)
        {
            if (materials.TryGetValue(key, out var material) && material) return material;
            material = new Material(track.tintMaterial);
            material.name = "Dash " + key;
            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", color * glow);
            material.SetFloat("_SurfaceStyle", 0.7f);
            material.SetFloat("_Smoothness", 0.42f);
            material.SetFloat("_Metallic", 0.08f);
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
            go.transform.rotation = Quaternion.Euler(0, 180, 0);
            var tm = go.AddComponent<TextMesh>(); tm.text = word; tm.fontSize = 64;
            tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.color = color;
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
            float checkpoint = (Mathf.Floor(z / 450f) + 1) * 450f;
            if (checkpoint > 0f && checkpoint >= z && checkpoint < z + track.segmentLength)
            {
                var delivery = Mat(track, "delivery", new Color(1f, 0.55f, 0.08f), 0.6f);
                foreach (float side in new[] { -1f, 1f })
                    Part(parent, new Vector3(side * 4.2f, 2.6f, checkpoint), new Vector3(0.22f, 5.2f, 0.22f), delivery);
                Part(parent, new Vector3(0, 5.2f, checkpoint), new Vector3(8.6f, 0.3f, 0.5f), delivery);
                Text(track, parent, "FOOD INC  /  DELIVERY DROP", new Vector3(0, 5.65f, checkpoint - 0.1f), 0.065f, Color.white);
            }
            bool wall = DashRun.WallZone(z + 0.01f), gravity = DashRun.GravityZone(z + 0.01f);
            if (!wall && !gravity) return;
            float sectionStart = (gravity ? 600f : 300f) + Mathf.Floor((z - (gravity ? 600f : 300f)) / 960f) * 960f;
            float end = Mathf.Min(z + track.segmentLength, sectionStart + 90f);
            float length = end - z, middle = (z + end) * 0.5f;
            var mat = Mat(track, gravity ? "gravity" : "wallrun", gravity ? new Color(0.7f, 0.24f, 1f) : new Color(0.08f, 0.9f, 1f), 0.7f);
            var deck = Mat(track, "gravityDeck", new Color(0.055f, 0.075f, 0.13f), 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                Part(parent, new Vector3(side * (SurfaceTraversal.HalfWidth + 0.08f), SurfaceTraversal.CeilingHeight * 0.5f, middle),
                    new Vector3(0.16f, SurfaceTraversal.CeilingHeight, length), deck);
                for (int i = 0; i < 3; i++)
                    Part(parent, new Vector3(side * (SurfaceTraversal.HalfWidth - 0.02f), 0.8f + i * 2.3f, middle), new Vector3(0.035f, 0.055f, length - 0.2f), mat);
            }
            if (gravity)
            {
                Part(parent, new Vector3(0, SurfaceTraversal.CeilingHeight + 0.1f, middle), new Vector3(7.4f, 0.2f, length), deck);
                foreach (float x in Tuning.LaneX)
                    Part(parent, new Vector3(x, SurfaceTraversal.CeilingHeight - 0.015f, middle), new Vector3(0.055f, 0.025f, length - 0.2f), mat);
            }
        }

        static void ArchitecturalSide(TrackSpawner track, Transform parent, float side, float z)
        {
            int d = DashRun.District, block = Mathf.FloorToInt(z / 20f);
            // The right-hand side of a route zone belongs to the route: its own builders put the shops
            // and the deck there, and every prop that would stand in the fourth lane is left out.
            bool route = side > 0f && DashRun.RouteZone(z + track.segmentLength * 0.5f);
            int which = Mathf.Abs(block + (side > 0 ? 1 : 0)) % track.dashBuildings.Length;
            var prefab = track.dashBuildings[which];
            float baseY = d == 1 ? -24f : 0f;
            float towerX = side * (route ? 15.5f : 14f);
            var building = Object.Instantiate(prefab, new Vector3(towerX, baseY, z + 10f), Quaternion.Euler(0, side > 0 ? -90 : 90, 0), parent);
            building.name = "Dash City / layered architecture";
            var frame = Mat(track, "billboardFrame", new Color(0.11f, 0.16f, 0.23f));
            var edge = Mat(track, "billboardEdge" + d, Accent(d), 1.6f);
            var greenery = track.foliageMaterial;
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
            if (track.hologramTextures != null && track.hologramTextures.Length > 0 && block % 2 == 0 && !route)
            {
                float h = d == 1 ? 5.5f : 7f + Mathf.Abs(block % 3) * 2f;
                var at = new Vector3(side * 6f, h, z + 9f);
                var panel = new GameObject("Angled holographic display").transform;
                panel.SetParent(parent, false); panel.position = at;
                panel.rotation = Quaternion.Euler(0, side > 0 ? -125 : 125, 0);
                var backing = Part(panel, Vector3.zero, new Vector3(7.6f, 4.7f, 0.16f), frame);
                backing.transform.localPosition = Vector3.zero; backing.transform.localRotation = Quaternion.identity;
                foreach (float edgeY in new[] { -2.3f, 2.3f })
                {
                    var rim = Part(panel, Vector3.zero, new Vector3(7.6f, 0.09f, 0.18f), edge);
                    rim.transform.localPosition = Vector3.up * edgeY; rim.transform.localRotation = Quaternion.identity;
                }
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(quad.GetComponent<Collider>());
                quad.name = "Illustrated holographic billboard"; quad.transform.SetParent(panel, false);
                quad.transform.localPosition = new Vector3(0, 0, 0.1f);
                quad.transform.localRotation = Quaternion.Euler(0, 180, 0);
                quad.transform.localScale = new Vector3(7.3f, 4.4f, 1f);
                var renderer = quad.GetComponent<Renderer>(); renderer.sharedMaterial = track.hologramMaterial;
                var props = new MaterialPropertyBlock(); props.SetTexture("_BaseMap", track.hologramTextures[Mathf.Abs(block / 2) % track.hologramTextures.Length]);
                props.SetColor("_BaseColor", Color.white); renderer.SetPropertyBlock(props);
            }
            // Connected multi-level urban walkways, with real suspension geometry and garden ends.
            if (side < 0 && track.dashBridge && Mathf.Abs(block) % 4 == 1)
            {
                var bridge = Object.Instantiate(track.dashBridge, new Vector3(0, d == 1 ? 5.8f : 8f, z + 10f), Quaternion.identity, parent);
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
