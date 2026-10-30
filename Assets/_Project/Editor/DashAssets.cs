using System.Collections.Generic;
using DallasDash.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    public static class DashAssets
    {
        const string Root = "Assets/_Project/Art/DashRiders";
        static readonly Dictionary<string, Material> mats = new();
        public static void ImportVisuals()
        {
            AssetDatabase.Refresh();
            string path = Root + "/Visuals/dash-riders-logo.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            }
        }
        public static Material HologramMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Hologram.mat");
            if (!m) { m = new Material(Shader.Find("FoodInc/HologramCurved")); AssetDatabase.CreateAsset(m, Root + "/Hologram.mat"); }
            m.shader = Shader.Find("FoodInc/HologramCurved"); m.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(m); return m;
        }
        static Material Mat(string id, Color color, float glow = 0f)
        {
            if (mats.TryGetValue(id, out var m) && m) return m;
            System.IO.Directory.CreateDirectory(Root);
            m = AssetDatabase.LoadAssetAtPath<Material>(Root + "/" + id + ".mat");
            if (!m) { m = new Material(Shader.Find("FoodInc/ToonCurved")); AssetDatabase.CreateAsset(m, Root + "/" + id + ".mat"); }
            m.SetColor("_BaseColor", color); m.SetColor("_EmissionColor", color * glow);
            m.SetFloat("_OutlineWidth", 0.008f); m.SetShaderPassEnabled("Outline", false);
            EditorUtility.SetDirty(m); mats[id] = m; return m;
        }
        static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 at, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(type); Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat; return go;
        }
        static Transform Joint(Transform parent, string name, Vector3 at)
        {
            var joint = new GameObject(name).transform; joint.SetParent(parent, false); joint.localPosition = at; return joint;
        }
        static GameObject Save(GameObject root, string name)
        {
            System.IO.Directory.CreateDirectory(Root);
            var result = PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
            Object.DestroyImmediate(root); return result;
        }
        public static GameObject Crew(int character)
        {
            string name = character == 0 ? "Ace" : character == 1 ? "Nova" : "Bolt";
            var generated = ModelImport.Get(name.ToLowerInvariant());
            if (generated) return generated;
            var root = new GameObject(name); var t = root.transform;
            var dark = Mat("cargo", new Color(0.055f, 0.065f, 0.1f));
            var trim = Mat("trim", new Color(0.65f, 0.74f, 0.85f));
            var accent = Mat(name, character == 0 ? new Color(1f, 0.32f, 0.04f) : character == 1 ? new Color(0.58f, 0.13f, 0.9f) : new Color(1f, 0.78f, 0.08f));
            var cyan = Mat("led", new Color(0.12f, 0.9f, 1f), 1.1f);
            var skin = Mat(name + "skin", character == 0 ? new Color(0.65f, 0.38f, 0.22f) : new Color(0.87f, 0.57f, 0.4f));
            var white = Mat("white", new Color(0.93f, 0.96f, 1f));
            var hair = Mat("hair", new Color(0.035f, 0.025f, 0.065f));
            var body = character == 2 ? trim : accent;
            var pose = root.AddComponent<DashPose>(); pose.robot = character == 2;
            // Bomber jacket / cropped athletic shell / compact robot chassis.
            Part(t, "Jacket", PrimitiveType.Capsule, new Vector3(0, 1.25f, 0), new Vector3(character == 1 ? 0.5f : 0.62f, 0.38f, 0.4f), body);
            Part(t, "Waist", PrimitiveType.Cube, new Vector3(0, 0.92f, 0), new Vector3(0.45f, 0.17f, 0.3f), dark);
            Part(t, "Zipper", PrimitiveType.Cube, new Vector3(0, 1.25f, 0.205f), new Vector3(0.035f, 0.45f, 0.035f), trim);
            Part(t, "Collar", PrimitiveType.Cylinder, new Vector3(0, 1.55f, 0), new Vector3(0.34f, 0.06f, 0.3f), dark);
            Part(t, "Head", character == 2 ? PrimitiveType.Cube : PrimitiveType.Sphere, new Vector3(0, 1.79f, 0.02f), new Vector3(0.5f, 0.5f, 0.44f), character == 2 ? trim : skin);
            if (character == 2)
            {
                Part(t, "LED visor", PrimitiveType.Cube, new Vector3(0, 1.8f, 0.247f), new Vector3(0.43f, 0.29f, 0.04f), dark);
                foreach (float side in new[] { -1f, 1f })
                {
                    Part(t, "Expressive eye", PrimitiveType.Cube, new Vector3(side * 0.105f, 1.83f, 0.273f), new Vector3(0.07f, 0.12f, 0.015f), cyan);
                    Part(t, "Ear actuator", PrimitiveType.Cylinder, new Vector3(side * 0.285f, 1.8f, 0), new Vector3(0.16f, 0.045f, 0.16f), accent).transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
                Part(t, "Friendly smile", PrimitiveType.Cube, new Vector3(0, 1.71f, 0.274f), new Vector3(0.13f, 0.026f, 0.015f), cyan);
                Part(t, "Antenna", PrimitiveType.Cube, new Vector3(0.14f, 2.1f, 0), new Vector3(0.04f, 0.2f, 0.04f), dark);
                Part(t, "Signal", PrimitiveType.Sphere, new Vector3(0.14f, 2.22f, 0), Vector3.one * 0.09f, cyan);
            }
            else
            {
                Part(t, "Hair cap", PrimitiveType.Sphere, new Vector3(0, 1.97f, -0.025f), new Vector3(0.51f, 0.23f, 0.46f), character == 1 ? accent : hair);
                for (int i = 0; i < 4; i++)
                {
                    var tuft = Part(t, "Swept fringe", PrimitiveType.Cube, new Vector3(-0.15f + i * 0.1f, 2f + i * 0.018f, 0.14f), new Vector3(0.12f, 0.18f, 0.22f), character == 1 ? accent : hair);
                    tuft.transform.localRotation = Quaternion.Euler(-20, 0, -20);
                }
                foreach (float side in new[] { -1f, 1f })
                {
                    Part(t, "Eye white", PrimitiveType.Sphere, new Vector3(side * 0.105f, 1.82f, 0.207f), new Vector3(0.1f, 0.11f, 0.035f), white);
                    Part(t, "Pupil", PrimitiveType.Sphere, new Vector3(side * 0.105f, 1.82f, 0.227f), new Vector3(0.052f, 0.07f, 0.02f), hair);
                    Part(t, "Brow", PrimitiveType.Cube, new Vector3(side * 0.105f, 1.91f, 0.21f), new Vector3(0.11f, 0.025f, 0.025f), hair);
                }
                Part(t, "Nose", PrimitiveType.Sphere, new Vector3(0, 1.76f, 0.24f), new Vector3(0.07f, 0.08f, 0.08f), skin);
                Part(t, "Smile", PrimitiveType.Cube, new Vector3(0, 1.68f, 0.212f), new Vector3(0.12f, 0.025f, 0.022f), white);
                if (character == 1) Part(t, "Ponytail", PrimitiveType.Capsule, new Vector3(0.05f, 1.78f, -0.3f), new Vector3(0.17f, 0.25f, 0.17f), accent).transform.localRotation = Quaternion.Euler(25, 0, 0);
                Part(t, "Headset", PrimitiveType.Cube, new Vector3(-0.26f, 1.8f, 0), new Vector3(0.06f, 0.14f, 0.14f), cyan);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                var leg = Joint(t, side < 0 ? "Left leg" : "Right leg", new Vector3(side * 0.145f, 0.89f, 0));
                Part(leg, "Cargo trouser", PrimitiveType.Capsule, new Vector3(0, -0.31f, 0), new Vector3(0.23f, 0.32f, 0.25f), character == 2 ? trim : dark);
                Part(leg, "Knee pad", PrimitiveType.Cube, new Vector3(0, -0.36f, 0.135f), new Vector3(0.18f, 0.15f, 0.08f), accent);
                Part(leg, "Sneaker", PrimitiveType.Cube, new Vector3(0, -0.73f, 0.08f), new Vector3(0.25f, 0.18f, 0.42f), dark);
                Part(leg, "Luminous sole", PrimitiveType.Cube, new Vector3(0, -0.82f, 0.08f), new Vector3(0.26f, 0.045f, 0.43f), cyan);
                if (character == 2) Part(leg, "Magnetic wheel", PrimitiveType.Cylinder, new Vector3(side * 0.1f, -0.72f, 0), new Vector3(0.3f, 0.08f, 0.3f), dark).transform.localRotation = Quaternion.Euler(0, 0, 90);
                var arm = Joint(t, side < 0 ? "Left arm" : "Right arm", new Vector3(side * 0.34f, 1.48f, 0));
                Part(arm, "Sleeve", PrimitiveType.Capsule, new Vector3(0, -0.2f, 0), new Vector3(0.21f, 0.23f, 0.23f), body);
                Part(arm, "Glove", PrimitiveType.Sphere, new Vector3(0, -0.48f, 0.015f), new Vector3(0.18f, 0.2f, 0.19f), dark);
                Part(arm, "Cuff", PrimitiveType.Cube, new Vector3(0, -0.38f, 0.03f), new Vector3(0.22f, 0.07f, 0.24f), accent);
                if (side < 0) { pose.leftLeg = leg; pose.leftArm = arm; } else { pose.rightLeg = leg; pose.rightArm = arm; }
            }
            Part(t, "Delivery compartment", PrimitiveType.Cube, new Vector3(0, 1.23f, -0.33f), new Vector3(0.45f, 0.5f, 0.25f), dark);
            Part(t, "FOOD INC badge", PrimitiveType.Cube, new Vector3(0, 1.24f, -0.465f), new Vector3(0.23f, 0.24f, 0.03f), accent);
            Part(t, "Badge stripe", PrimitiveType.Cube, new Vector3(0, 1.24f, -0.486f), new Vector3(0.05f, 0.18f, 0.02f), white);
            return Save(root, name);
        }
        public static GameObject Board(bool upgraded = false)
        {
            var generated = ModelImport.Get(upgraded ? "glide-board" : "electric-board");
            if (generated) return generated;
            var root = new GameObject("Electric board");
            var dark = Mat("cargo", new Color(0.055f, 0.065f, 0.1f));
            var led = Mat("led", new Color(0.12f, 0.9f, 1f), 1.1f);
            Part(root.transform, "Deck", PrimitiveType.Cube, new Vector3(0, 0.04f, 0), new Vector3(0.72f, 0.12f, 1.25f), dark);
            foreach (float side in new[] { -1f, 1f })
                Part(root.transform, "Edge glow", PrimitiveType.Cube, new Vector3(side * 0.35f, 0.035f, 0), new Vector3(0.035f, 0.1f, 1.18f), led);
            foreach (float z in new[] { -0.4f, 0.4f })
                Part(root.transform, "Hover engine", PrimitiveType.Cube, new Vector3(0, -0.045f, z), new Vector3(0.54f, 0.1f, 0.18f), led);
            return Save(root, upgraded ? "GlideBoard" : "ElectricBoard");
        }
        public static GameObject Pickup(string id)
        {
            var root = new GameObject(id); var t = root.transform;
            var gold = Mat("foodGold", new Color(1f, 0.63f, 0.14f));
            var red = Mat("foodRed", new Color(0.95f, 0.18f, 0.16f));
            var green = Mat("foodGreen", new Color(0.22f, 0.8f, 0.27f));
            var cream = Mat("foodCream", new Color(1f, 0.92f, 0.65f));
            var dark = Mat("cargo", new Color(0.055f, 0.065f, 0.1f));
            switch (id)
            {
                case "energy-chip":
                    var chip = Part(t, "Energy chip", PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(0.5f, 0.075f, 0.5f), Mat("chip", new Color(0.08f, 0.9f, 1f), 0.7f));
                    chip.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Part(t, "Chip core", PrimitiveType.Cube, new Vector3(0, 0.35f, 0.09f), new Vector3(0.22f, 0.22f, 0.04f), cream); break;
                case "pizza":
                    Part(t, "Pizza crust", PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(0.65f, 0.07f, 0.65f), gold);
                    Part(t, "Cheese", PrimitiveType.Cylinder, new Vector3(0, 0.28f, 0), new Vector3(0.55f, 0.025f, 0.55f), cream);
                    foreach (var v in new[] { new Vector3(-0.12f, 0.32f, 0), new Vector3(0.12f, 0.32f, 0), new Vector3(0, 0.32f, 0.16f) }) Part(t, "Pepperoni", PrimitiveType.Cylinder, v, new Vector3(0.12f, 0.01f, 0.12f), red);
                    break;
                case "soft-drink":
                    Part(t, "Drink cup", PrimitiveType.Cylinder, new Vector3(0, 0.32f, 0), new Vector3(0.35f, 0.29f, 0.35f), red);
                    Part(t, "Lid", PrimitiveType.Cylinder, new Vector3(0, 0.63f, 0), new Vector3(0.39f, 0.025f, 0.39f), cream);
                    Part(t, "Straw", PrimitiveType.Cylinder, new Vector3(0.07f, 0.77f, 0), new Vector3(0.035f, 0.15f, 0.035f), cream); break;
                case "taco":
                    Part(t, "Taco shell", PrimitiveType.Sphere, new Vector3(0, 0.27f, 0), new Vector3(0.65f, 0.48f, 0.25f), gold);
                    Part(t, "Lettuce", PrimitiveType.Cube, new Vector3(0, 0.43f, 0), new Vector3(0.53f, 0.15f, 0.22f), green);
                    Part(t, "Tomato", PrimitiveType.Cube, new Vector3(0.13f, 0.5f, 0), new Vector3(0.13f, 0.1f, 0.15f), red); break;
                default:
                    Part(t, "Bottom bun", PrimitiveType.Sphere, new Vector3(0, 0.15f, 0), new Vector3(0.65f, 0.17f, 0.65f), gold);
                    Part(t, "Patty", PrimitiveType.Cylinder, new Vector3(0, 0.25f, 0), new Vector3(0.6f, 0.065f, 0.6f), dark);
                    Part(t, "Lettuce", PrimitiveType.Cube, new Vector3(0, 0.34f, 0), new Vector3(0.61f, 0.06f, 0.56f), green);
                    Part(t, "Top bun", PrimitiveType.Sphere, new Vector3(0, 0.46f, 0), new Vector3(0.65f, 0.28f, 0.65f), gold); break;
            }
            return Save(root, id);
        }
    }
}
