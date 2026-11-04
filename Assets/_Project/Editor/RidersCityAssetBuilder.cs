using System;
using System.Collections.Generic;
using System.IO;
using DallasDash.Gameplay;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DallasDash.EditorTools
{
    public static class RidersCityAssetBuilder
    {
        const string Root = "Assets/_Project/Art/RidersCity";
        static readonly Dictionary<Material, Material> Converted = new();
        public static void Prepare()
        {
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory("Assets/_Project/Resources");
            AssetDatabase.Refresh();
            var catalogue = AssetDatabase.LoadAssetAtPath<RidersCityAssets>("Assets/_Project/Resources/RidersCityAssets.asset");
            if (!catalogue) { catalogue = ScriptableObject.CreateInstance<RidersCityAssets>(); AssetDatabase.CreateAsset(catalogue, "Assets/_Project/Resources/RidersCityAssets.asset"); }
            catalogue.riderPrefabs = new[] { Rider("Ace-Ride", true), Rider("Nova-Ride", true), Rider("Bolt", false) };
            catalogue.buildings = new[] { Model("neon-block-a"), Model("neon-block-b"), Model("neon-block-a"), Model("neon-block-b"), Model("neon-block-a"), Free("city-a"), Free("city-b") };
            catalogue.trees = new[] { Kit("CommonTree_3"), Kit("CommonTree_4"), Kit("Bush_Common") };
            catalogue.props = new[] { Kit("street_lamp_01"), Kit("modular_street_seating"), Model("electric-pod"), Model("food-market"), Kit("metal_trash_can"), Kit("planter_box_01"), Kit("road_traffic-light"), Model("shopper"), Kit("car_sedan"), Kit("car_taxi") };
            catalogue.obstacles = new[] { Model("roadworks-barrier"), Kit("road_construction-barrier"), Model("neon-hurdle") };
            catalogue.servicePlatform = Free("delivery-platform");
            catalogue.chargingStation = Free("service-charger");
            catalogue.raceGate = Free("service-gate");
            catalogue.pedestrianBridge = Model("skybridge");
            catalogue.marketStall = Model("food-market");
            catalogue.services = new[] { Free("food-parcel"), Free("service-console"), Model("food-market"), catalogue.chargingStation, Free("service-console"), Free("city-parasol") };
            catalogue.districtRiders = new[] { DistrictModelBuilder.Build("District-Ace", catalogue.riderPrefabs[0]), DistrictModelBuilder.Build("District-Nova", catalogue.riderPrefabs[1]), DistrictModelBuilder.Build("District-Bolt", catalogue.riderPrefabs[2]) };
            catalogue.districtObstacles = new GameObject[catalogue.obstacles.Length];
            for (int i = 0; i < catalogue.obstacles.Length; i++) catalogue.districtObstacles[i] = DistrictModelBuilder.Build("District-Hurdle-" + i, catalogue.obstacles[i]);
            catalogue.districtCharger = DistrictModelBuilder.Build("District-Charger", catalogue.chargingStation);
            catalogue.districtFood = DistrictModelBuilder.Build("District-Food", catalogue.marketStall);
            catalogue.districtFinish = DistrictModelBuilder.Build("District-Finish", catalogue.raceGate);
            catalogue.materials = new[] { CitySurfaceImport.Material("asphalt_02"), CitySurfaceImport.Material("concrete_pavers_02") };
            EditorUtility.SetDirty(catalogue); AssetDatabase.SaveAssets();
            Debug.Log("RIDERS_CITY_MODELS_READY actual crew + modular kit + imported street models");
        }
        static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new Exception("Required city model missing: " + path);
        static GameObject Model(string name)
        {
            var model = ModelImport.Get(name);
            if (!model) throw new InvalidOperationException("Required source model missing: " + name);
            return Copy(name, model);
        }
        static GameObject Kit(string name) => Copy(name, Load("Assets/_Project/Prefabs/Kit/" + name + ".prefab"));
        static GameObject Free(string name)
        {
            var model = ModelImport.Get("free-" + name);
            if (!model) throw new InvalidOperationException("Required free asset missing: " + name);
            return Copy("free-" + name, model);
        }
        static GameObject Rider(string name, bool board)
        {
            var root = new GameObject(name);
            string id = name.StartsWith("Nova") ? "nova" : name == "Bolt" ? "bolt" : "ace";
            var source = ModelImport.Get(id);
            if (!source) throw new InvalidOperationException("Required original crew model missing: " + id);
            var body = Object.Instantiate(source);
            body.transform.SetParent(root.transform, false);
            if (board) {
                var deck = Object.Instantiate(Load("Assets/_Project/Prefabs/Models/electric-board.prefab"), root.transform);
                deck.transform.localPosition = Vector3.zero;
                body.transform.localPosition += Vector3.up * Mathf.Max(.06f, BoundsOf(deck).max.y - .03f);
            }
            return Save(name, root);
        }
        static GameObject Copy(string name, GameObject source)
        {
            var root = new GameObject(name); Object.Instantiate(source, root.transform); return Save(name, root);
        }
        static GameObject Building(string style, int floors)
        {
            string window = style == "Brick" ? "Brick_Window_Square_Single" : style == "Trim" ? "Trim_Window" : "Metal_FullWindow";
            var source = Load("Assets/_Project/Prefabs/Kit/" + window + ".prefab");
            var root = new GameObject(style + " mixed-use building");
            for (int side = 0; side < 4; side++)
            for (int floor = 0; floor < floors; floor++)
            for (int column = 0; column < 3; column++) {
                var anchor = new GameObject("Architectural window bay").transform; anchor.SetParent(root.transform, false);
                var node = Object.Instantiate(source, anchor);
                Bounds bounds = BoundsOf(node);
                float scale = 3f / Mathf.Max(.01f, bounds.size.y);
                node.transform.localScale *= scale;
                node.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
                Vector3 front = new Vector3((column - 1) * 2, floor * 3, 3f);
                anchor.localRotation = Quaternion.Euler(0, side * 90, 0);
                anchor.localPosition = Quaternion.Euler(0, side * 90, 0) * front;
            }
            return Save(style + "Building", root);
        }
        static Bounds BoundsOf(GameObject root)
        {
            Bounds result = default; bool first = true;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>()) { if (first) { result = renderer.bounds; first = false; } else result.Encapsulate(renderer.bounds); }
            return result;
        }
        static GameObject Save(string name, GameObject root)
        {
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) if (slots[i]) slots[i] = Convert(slots[i]);
                renderer.sharedMaterials = slots;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab"); Object.DestroyImmediate(root); return prefab;
        }
        static Material Convert(Material source)
        {
            if (Converted.TryGetValue(source, out var found)) return found;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
            string id = string.IsNullOrEmpty(guid) ? source.GetInstanceID().ToString() : guid + "_" + localId;
            string path = Root + "/Materials/" + id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
            if (source.HasProperty("_BaseMap")) { material.SetTexture("_BaseMap", source.GetTexture("_BaseMap")); material.SetTextureScale("_BaseMap", source.GetTextureScale("_BaseMap")); material.SetTextureOffset("_BaseMap", source.GetTextureOffset("_BaseMap")); }
            material.SetFloat("_Smoothness", .28f);
            material.SetFloat("_AlphaClip", 1f); material.SetFloat("_Cutoff", .5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", 0f);
            if (source.HasProperty("_Metallic")) material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
            if (source.HasProperty("_EmissionColor")) { material.SetColor("_EmissionColor", source.GetColor("_EmissionColor") * .4f); material.EnableKeyword("_EMISSION"); }
            EditorUtility.SetDirty(material); Converted[source] = material; return material;
        }
    }
}
