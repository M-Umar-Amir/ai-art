using System.Collections.Generic;
using DallasDash.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DallasDash.Gameplay
{
    /// Social hub presentation; multiplayer riders use the existing streamed district course.
    public sealed class RidersCityWorld : MonoBehaviour
    {
        // One place per thing to do, each opening the same screen as the main menu: no duplicate shops.
        public static readonly Vector3[] Destinations = {
            new(-18, 0, 16), new(18, 0, 16), new(-18, 0, -16), new(18, 0, -16)
        };
        public static readonly string[] DestinationNames = { "DELIVERIES", "GARAGE", "SHOP", "REWARDS" };
        /// Which catalogue service model dresses each destination.
        static readonly int[] ServiceModel = { 0, 1, 2, 4 };
        const int CityLayer = 30;
        readonly Vector3 origin = new(5000, 100, 0);
        readonly Dictionary<string, Material> materials = new();
        readonly List<Material> surfaceMaterials = new();
        readonly Dictionary<ulong, Transform> remote = new();
        readonly Dictionary<ulong, TextMesh> names = new();
        readonly List<ulong> stale = new();
        GameObject hub;
        Camera cityCamera;
        Camera previousCamera;
        Transform localRider;
        RidersCityAssets assets;
        Light cityFill;
        public string AssetError { get; private set; } = "";
        readonly Dictionary<Transform, Vector3> riderVisualBases = new();
        Vector3 localPosition = new(0, 0, -8);
        Vector3 cameraVelocity;
        bool active, cameraSnap;
        readonly bool automatedProbe = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-riders-host-test" || a == "-riders-client-test");
        Vector3 FollowOffset => new Vector3(0, 4.8f, -9);
        public Vector3 Position => localPosition;
        public Vector2 Input { get; set; }
        public bool IsRace => false;
        public bool Active => active;
        public int NearestDestination
        {
            get
            {
                int nearest = 0;
                for (int i = 1; i < Destinations.Length; i++)
                    if ((localPosition - Destinations[i]).sqrMagnitude < (localPosition - Destinations[nearest]).sqrMagnitude) nearest = i;
                return nearest;
            }
        }
        public bool CanVisit => Vector3.Distance(localPosition, Destinations[NearestDestination]) < 5f;

        Material Mat(string key, Color color, bool glow = false)
        {
            if (materials.TryGetValue(key, out var found)) return found;
            Material template = Resources.Load<Material>("RidersCityMaterial");
            int sourceIndex = key == "road" ? 0 : key == "pavement" ? 1 : -1;
            if (sourceIndex >= 0 && assets && assets.materials != null && assets.materials.Length > sourceIndex && assets.materials[sourceIndex]) template = assets.materials[sourceIndex];
            var m = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = sourceIndex >= 0 ? (key == "road" ? new Color(.5f, .55f, .65f) : new Color(.65f, .72f, .8f)) : color;
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 1.6f); }
            materials[key] = m; return m;
        }
        GameObject Shape(Transform parent, string name, Vector3 position, Vector3 scale, Material mat, PrimitiveType type = PrimitiveType.Cube)
        {
            var o = GameObject.CreatePrimitive(type); o.name = name; o.layer = CityLayer;
            o.transform.SetParent(parent, false); o.transform.localPosition = position; o.transform.localScale = scale;
            if (mat && mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap"))
            {
                var tiled = new Material(mat);
                tiled.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(.25f, scale.x / 4), Mathf.Max(.25f, scale.z / 4)));
                surfaceMaterials.Add(tiled); o.GetComponent<Renderer>().sharedMaterial = tiled;
            }
            else o.GetComponent<Renderer>().sharedMaterial = mat;
            var c = o.GetComponent<Collider>(); if (c) Destroy(c);
            return o;
        }
        TextMesh Label(Transform parent, string text, Vector3 position, float size, Color color)
        {
            var o = new GameObject(text) { layer = CityLayer }; o.transform.SetParent(parent, false); o.transform.localPosition = position;
            var t = o.AddComponent<TextMesh>(); t.text = text; t.fontSize = 48; t.characterSize = size; t.anchor = TextAnchor.MiddleCenter; t.color = color;
            t.color = WorldPalette.Surface(color);
            t.font = DallasDash.UI.UiKit.Font ? DallasDash.UI.UiKit.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font) o.GetComponent<MeshRenderer>().sharedMaterial = t.font.material;
            return t;
        }
        GameObject Pick(GameObject[] prefabs, int index) => prefabs[index % prefabs.Length];
        bool CatalogueReady()
        {
            assets = Resources.Load<RidersCityAssets>("RidersCityAssets");
            if (!assets) AssetError = "Missing RidersCityAssets resource. Rebuild the city model catalogue.";
            else if (assets.riderPrefabs == null || assets.riderPrefabs.Length < 3 || System.Array.Exists(assets.riderPrefabs, p => !p)) AssetError = "City catalogue requires all three real rider prefabs.";
            else if (assets.buildings == null || assets.buildings.Length == 0 || System.Array.Exists(assets.buildings, p => !p)) AssetError = "City building model catalogue is missing.";
            else if (assets.trees == null || assets.trees.Length == 0 || assets.props == null || assets.props.Length == 0 || assets.obstacles == null || assets.obstacles.Length == 0) AssetError = "City trees, streetscape props or race obstacle models are missing.";
            if (AssetError.Length == 0 && (!assets.servicePlatform || !assets.chargingStation || !assets.raceGate || assets.services == null || assets.services.Length != 6 || System.Array.Exists(assets.services, p => !p)))
                AssetError = "City service models are missing. Rebuild the free asset catalogue.";
            if (AssetError.Length == 0 && (assets.districtRiders == null || assets.districtRiders.Length != 3 || System.Array.Exists(assets.districtRiders, p => !p) || assets.districtObstacles == null || assets.districtObstacles.Length == 0 || !assets.districtCharger || !assets.districtFood || !assets.districtFinish))
                AssetError = "District-grounded model catalogue is missing. Rebuild the Windows player.";
            if (AssetError.Length == 0) return true;
            Debug.LogError(AssetError); return false;
        }
        Transform Model(GameObject prefab, Transform parent, string name, Vector3 position, float size, bool fitHeight, float yaw = 0)
        {
            if (!prefab) throw new System.InvalidOperationException("City catalogue contains a missing model: " + name);
            var wrapper = new GameObject(name) { layer = CityLayer }; wrapper.transform.SetParent(parent, false);
            wrapper.transform.localPosition = position; wrapper.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var mesh = Instantiate(prefab, wrapper.transform, false); mesh.SetActive(true);
            mesh.transform.localPosition = Vector3.zero; mesh.transform.localRotation = Quaternion.identity;
            foreach (var node in mesh.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = CityLayer;
            foreach (var behaviour in mesh.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (var collider in mesh.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach (var body in mesh.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            var renderers = mesh.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new System.InvalidOperationException("City prefab has no model renderers: " + prefab.name);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer) continue;
                bounds.Encapsulate(renderer.bounds);
                foreach (var material in renderer.sharedMaterials)
                    if (material && material.shader && material.shader.name.IndexOf("Curved", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new System.InvalidOperationException("Curved runner material in city catalogue: " + prefab.name);
            }
            float extent = fitHeight ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.z);
            float scale = size / Mathf.Max(.01f, extent);
            mesh.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) if (!(renderer is ParticleSystemRenderer)) bounds.Encapsulate(renderer.bounds);
            Vector3 center = wrapper.transform.InverseTransformPoint(bounds.center);
            Vector3 floor = wrapper.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            mesh.transform.localPosition -= new Vector3(center.x, floor.y, center.z);
            return wrapper.transform;
        }
        Transform Rider(Transform parent, string name, int riderIndex)
        {
            var rider = Model(Pick(assets.riderPrefabs, Mathf.Abs(riderIndex)), parent, name, Vector3.zero, 2.1f, true);
            var visual = rider.GetChild(0); riderVisualBases[visual] = visual.localPosition;
            return rider;
        }
        void Build()
        {
            if (hub || AssetError.Length > 0) return;
            if (!CatalogueReady()) return;
            hub = new GameObject("Riders City / shared social hub"); hub.transform.position = origin;
            var pavement = Mat("pavement", new Color(.34f, .37f, .4f));
            var road = Mat("road", new Color(.075f, .085f, .1f));
            var white = Mat("white", new Color(.9f, .92f, .85f));
            var cyan = Mat("cyan", new Color(.1f, .85f, 1f), true);
            var colors = new[] { new Color(1f, .38f, .12f), new Color(.6f, .3f, .9f), new Color(.1f, .7f, .5f), new Color(.1f, .7f, .9f), new Color(.85f, .25f, .55f), new Color(.95f, .7f, .15f) };
            Shape(hub.transform, "City square paving", new Vector3(0, -.25f, 0), new Vector3(130, .5f, 130), pavement);
            Shape(hub.transform, "North south avenue", new Vector3(0, .015f, 0), new Vector3(13, .04f, 100), road);
            Shape(hub.transform, "Market avenue", new Vector3(0, .02f, 0), new Vector3(100, .04f, 11), road);
            foreach (float side in new[] {-1f, 1f})
                Shape(hub.transform, "Raised sidewalk", new Vector3(side * 14, .02f, 0), new Vector3(14, .12f, 100), pavement);
            for (int z = -36; z <= 36; z += 6)
                Shape(hub.transform, "Lane marking", new Vector3(0, .06f, z), new Vector3(.15f, .03f, 2.5f), white);
            for (int i = -3; i <= 3; i++)
                foreach (float side in new[] { -1f, 1f })
                    Shape(hub.transform, "Crosswalk", new Vector3(i * 1.6f, .08f, side * 8), new Vector3(1, .03f, 2.2f), white);
            for (int i = 0; i < Destinations.Length; i++)
            {
                var d = Destinations[i]; float side = Mathf.Sign(d.x);
                Model(Pick(assets.buildings, i), hub.transform, DestinationNames[i] + " architecture", new Vector3(side * 28, .1f, d.z), 14, false, side > 0 ? -90 : 90);
                Model(assets.servicePlatform, hub.transform, "Delivery service platform", new Vector3(d.x, .12f, d.z), 3.5f, false);
                Model(assets.services[ServiceModel[i]], hub.transform, DestinationNames[i] + " service equipment", new Vector3(d.x + side * 1.8f, .14f, d.z + 2.4f), i == 2 ? 3.2f : 1.4f, true, side > 0 ? -90 : 90);
                var sign = Label(hub.transform, DestinationNames[i], new Vector3(side * 20.3f, 3.4f, d.z), .075f, colors[i]);
                sign.transform.localRotation = Quaternion.Euler(0, side > 0 ? 90 : -90, 0);
                Model(Pick(assets.props, i % 2 == 0 ? 0 : 5), hub.transform, "Shop street furniture", new Vector3(side * 20.5f, .12f, d.z + 7), i % 2 == 0 ? 4.2f : .75f, true);
            }
            // Authored shopper meshes populate storefronts without blocking social traversal.
            if (assets.props.Length > 7)
            {
                for (int i = 0; i < 4; i++)
                {
                    float side = i % 2 == 0 ? -1 : 1;
                    Model(Pick(assets.props, 7), hub.transform, "City shopper", new Vector3(side * 17, .1f, -24 + i * 16), 1.75f, true, i * 47);
                }
            }
            // Imported facades form a city skyline beyond the playable promenades.
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1 : 1; float z = -42 + (i / 2) * 14;
                Model(Pick(assets.buildings, i + 3), hub.transform, "City skyline building", new Vector3(side * 44, 0, z), 12 + i % 3 * 2, false, side > 0 ? -90 : 90);
            }
            // Imported end-cap architecture closes both vistas so the street reads as a city,
            // with no exposed platform edge when a rider approaches the north/south shops.
            for (int i = -3; i <= 3; i++)
                foreach (float end in new[] {-1f, 1f})
                    Model(assets.buildings[3 + Mathf.Abs(i) % 2], hub.transform, "Avenue end-cap tower", new Vector3(i * 14, 0, end * 51), 14.5f, false, end > 0 ? 180 : 0);
            foreach (float crossing in new[] { -18f, 18f })
                Model(assets.pedestrianBridge, hub.transform, "Suspended garden pedestrian bridge", new Vector3(0, 7.5f, crossing), 29f, false);
            if (assets.props.Length > 9)
            {
                for (int i = 0; i < 4; i++)
                {
                    float side = i % 2 == 0 ? -1 : 1;
                    float end = i < 2 ? -1 : 1;
                    Model(Pick(assets.props, 8 + i % 2), hub.transform, "Parked city vehicle", new Vector3(side * 5, .08f, end * 36), 4.3f, false, side > 0 ? 180 : 0);
                }
            }
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1 : 1; float z = -33 + (i / 2) * 12;
                Model(Pick(assets.trees, i % Mathf.Min(2, assets.trees.Length)), hub.transform, "Street tree", new Vector3(side * 11.8f, .1f, z), 4.6f + i % 2 * .5f, true);
                if (assets.trees.Length > 2 && i % 3 == 0)
                    Model(assets.trees[2], hub.transform, "Low ornamental shrub", new Vector3(side * 20.5f, .12f, z + 4), .8f, true);
                if (i % 3 == 0) continue;
                Model(Pick(assets.props, 0), hub.transform, "Street lamp", new Vector3(side * 8.5f, .1f, z + 4), 5.2f, true);
                Model(Pick(assets.props, 1), hub.transform, "Promenade seating", new Vector3(side * 13.3f, .1f, z + 4), 2.5f, false, side > 0 ? -90 : 90);
            }
            localRider = Rider(hub.transform, "Your rider", DashTour.Active ? 0 : Mathf.Clamp(PlayerProfile.Character, 0, 2));
            var cameraObject = new GameObject("Riders City camera"); cameraObject.transform.SetParent(transform, false);
            cityCamera = cameraObject.AddComponent<Camera>(); cityCamera.cullingMask = 1 << CityLayer; cityCamera.fieldOfView = 53;
            cityCamera.clearFlags = CameraClearFlags.Skybox; cityCamera.backgroundColor = new Color(.25f, .34f, .45f); cityCamera.nearClipPlane = .1f; cityCamera.farClipPlane = 160;
            cityCamera.allowHDR = true; cityCamera.allowMSAA = true; cityCamera.depth = 20;
            var cameraData = cityCamera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = PlayerProfile.Graphics > 0;
            cameraData.antialiasing = PlayerProfile.Graphics > 0 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
            cameraData.volumeLayerMask = 1;
            DashCity.ApplyNaturalLight(0);
            NightLighting.Ensure();
            var fillObject = new GameObject("City soft fill"); fillObject.transform.SetParent(transform, false);
            cityFill = fillObject.AddComponent<Light>(); cityFill.type = LightType.Directional; cityFill.cullingMask = 1 << CityLayer;
            cityFill.color = new Color(.62f,.74f,1f); cityFill.intensity = .08f; cityFill.shadows = LightShadows.None;
            cityFill.transform.rotation = Quaternion.Euler(38, 145, 0);
        }
        public void Show(bool show)
        {
            if (show) Build();
            if (show && (!hub || !cityCamera)) return;
            if (active == show) return;
            active = show;
            if (hub) hub.SetActive(show);
            if (cityCamera) cityCamera.enabled = show;
            if (cityFill) cityFill.enabled = show;
            if (show)
            {
                previousCamera = Camera.main;
                if (previousCamera && previousCamera != cityCamera) previousCamera.enabled = false;
                cityCamera.transform.position = origin + localPosition + FollowOffset; cameraSnap = true;
            }
            else
            {
                if (previousCamera) previousCamera.enabled = true;
            }
        }
        public void MoveTo(Vector3 point) { localPosition = new Vector3(Mathf.Clamp(point.x, -19, 19), 0, Mathf.Clamp(point.z, -31, 31)); }
        void Update()
        {
            if (!active) return;
            MoveTo(localPosition + new Vector3(Input.x, 0, Input.y) * Time.unscaledDeltaTime * 8);
            localRider.localPosition = localPosition;
            if (Input.sqrMagnitude > .01f) localRider.localRotation = Quaternion.Slerp(localRider.localRotation, Quaternion.LookRotation(new Vector3(Input.x, 0, Input.y)), Time.unscaledDeltaTime * 10);
        }
        void LateUpdate()
        {
            if (!active || !cityCamera || !localRider) return;
            foreach (var pair in riderVisualBases)
            {
                if (!pair.Key || !pair.Key.gameObject.activeInHierarchy) continue;
                float movement = Input.magnitude;
                pair.Key.localPosition = pair.Value + Vector3.up * (Mathf.Sin(Time.unscaledTime * 4) * .012f * movement);
            }
            Vector3 target = localRider.position;
            if (cameraSnap)
            {
                cityCamera.transform.position = target + FollowOffset;
                cameraVelocity = Vector3.zero; cameraSnap = false;
            }
            else cityCamera.transform.position = Vector3.SmoothDamp(cityCamera.transform.position, target + FollowOffset, ref cameraVelocity, .18f, Mathf.Infinity, Time.unscaledDeltaTime);
            cityCamera.transform.LookAt(target + Vector3.up * 1.3f + Vector3.forward * 3.5f);
        }
        void OnDestroy()
        {
            if (active && previousCamera) previousCamera.enabled = true;
            if (hub) Destroy(hub);
            foreach (var mat in materials.Values) if (mat) Destroy(mat);
            foreach (var mat in surfaceMaterials) if (mat) Destroy(mat);
        }
    }
}
