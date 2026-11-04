using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Yum! Brands on the street (client, 4 Nov 2026: "the shops in track and vehicles and more things should be
    /// branded for companies owned by Yum", with their official logos; "I didn't see any delivery vehicle or any
    /// hurdle with branding"). KFC, Taco Bell, Pizza Hut and The Habit Burger Grill: shop signs and billboards
    /// (NaturalArchitecture), and the hurdles themselves: delivery vans, delivery pods, trucks and the food truck
    /// carry logo panels, taxis a roof-topper ad, other couriers a branded delivery box, roadworks barriers and
    /// the scaffolding a logo banner. Ace's own box is KFC (DashAssets). Every sign takes the next brand in one
    /// rotation, so all four appear equally and in order.
    public partial class TrackSpawner
    {
        public static readonly (string name, Color board, Color ink)[] YumBrands =
        {
            ("KFC", new Color(0.89f, 0.1f, 0.17f), Color.white),
            ("TACO BELL", new Color(0.44f, 0.13f, 0.51f), Color.white),
            ("PIZZA HUT", new Color(0.93f, 0.19f, 0.14f), Color.white),
            ("THE HABIT", new Color(0.12f, 0.12f, 0.14f), new Color(1f, 0.6f, 0.12f)),
        };
        int vehicleBrand;
        /// The n-th sign in the street's brand rotation (works for blocks behind the start too).
        public static int BrandAt(int n) => ((n % YumBrands.Length) + YumBrands.Length) % YumBrands.Length;
        /// The official logo sign textures (models/make_brand_signs.py), in YumBrands order; set by the bootstrap.
        public Texture2D[] brandSquare, brandWide, brandBoard;
        void Awake()
        {
            BrandVisibility.Square = brandSquare; BrandVisibility.Wide = brandWide; BrandVisibility.Board = brandBoard;
        }

        /// Brands a hurdle as it comes off the pool, with the next brand in the rotation.
        void BrandVehicle(GameObject go, string id)
        {
            if (!HurdleBrand.Brands(id)) return;
            var brand = go.GetComponent<HurdleBrand>() ?? go.AddComponent<HurdleBrand>();
            brand.Apply(this, id, BrandAt(vehicleBrand++));
        }
    }

    public class HurdleBrand : MonoBehaviour
    {
        readonly List<(Renderer renderer, string format)> faces = new();
        bool built;

        public static bool Brands(string id) => id is "delivery-van" or "electric-pod" or "food-truck" or "taxi" or "garbage-truck"
            or "courier-a" or "courier-b" or "scooter-courier" or "roadworks-barrier" or "roadworks" or "cargo-slide-gate";

        public void Apply(TrackSpawner track, string id, int brand)
        {
            if (!built) { built = true; Build(track, id, brand); }
            foreach (var f in faces) f.renderer.sharedMaterial = BrandVisibility.Logo(track, brand, f.format);
        }

        void Add(TrackSpawner track, int brand, string format, Vector3 local, float yaw, float width)
        {
            var f = BrandVisibility.Face(track, transform, brand, format, Vector3.zero, Quaternion.identity, width, 0f, moving: true);
            f.transform.localPosition = local;
            f.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            faces.Add((f.GetComponent<Renderer>(), format));
        }

        void Build(TrackSpawner track, string id, int brand)
        {
            // Measure the hurdle in its own space (placed along +Z; the rider comes from -Z).
            var rs = GetComponentsInChildren<MeshRenderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(transform.position, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 c = transform.InverseTransformPoint(b.center), size = b.size;
            float bottom = c.y - size.y * 0.5f, top = c.y + size.y * 0.5f, back = c.z - size.z * 0.5f;
            float wide = BrandVisibility.Aspect("wide");
            switch (id)
            {
                case "taxi":
                {
                    // A roof-topper ad, the way city cabs carry them: logos front and back.
                    var frame = DashCity.Mat(track, "brandFrame", new Color(.06f, .065f, .08f));
                    float w = 1.05f, h = w / wide;
                    var topper = DashCity.Part(transform, Vector3.zero, new Vector3(w + 0.06f, h + 0.06f, 0.24f), frame);
                    topper.name = "Taxi roof topper";
                    topper.transform.localPosition = new Vector3(c.x, top + h * 0.5f + 0.05f, c.z - size.z * 0.05f);
                    Add(track, brand, "wide", topper.transform.localPosition + new Vector3(0f, 0f, -0.125f), 0f, w);
                    Add(track, brand, "wide", topper.transform.localPosition + new Vector3(0f, 0f, 0.125f), 180f, w);
                    break;
                }
                case "courier-a": case "courier-b": case "scooter-courier":
                {
                    // Other couriers carry a branded delivery box on the back of the bike.
                    var shell = DashCity.Mat(track, "courierBox", new Color(.93f, .93f, .95f));
                    float s = 0.46f;
                    var box = DashCity.Part(transform, Vector3.zero, new Vector3(s, s, s * 0.9f), shell);
                    box.name = "Courier delivery box";
                    box.transform.localPosition = new Vector3(c.x, bottom + size.y * 0.55f, back + s * 0.35f);
                    Add(track, brand, "square", box.transform.localPosition + new Vector3(0f, 0f, -s * 0.45f - 0.012f), 0f, s * 0.92f);
                    foreach (float side in new[] { -1f, 1f })
                        Add(track, brand, "square", box.transform.localPosition + new Vector3(side * (s * 0.5f + 0.012f), 0f, 0f), side > 0 ? -90f : 90f, s * 0.82f);
                    break;
                }
                case "roadworks-barrier": case "roadworks":
                {
                    // A sponsor banner wrapped on the barrier face the rider meets.
                    float w = Mathf.Min(size.x * 0.82f, 1.7f);
                    float y = id == "roadworks" ? bottom + Mathf.Min(1.0f, size.y * 0.62f) : bottom + size.y * 0.55f;
                    Add(track, brand, "wide", new Vector3(c.x, y, back - 0.03f), 0f, w);
                    break;
                }
                case "cargo-slide-gate":
                {
                    // A banner hung from the scaffold's top rail, over the lane.
                    float w = Mathf.Min(size.x * 0.85f, 1.75f);
                    Add(track, brand, "wide", new Vector3(c.x, top - (w / wide) * 0.5f - 0.08f, back - 0.04f), 0f, w);
                    break;
                }
                default:
                {
                    // Vans, pods, trucks and the food truck: a logo panel down each side and on the back.
                    float h = Mathf.Min(0.95f, size.y * 0.36f), w = Mathf.Min(size.z * 0.62f, h * wide), y = bottom + size.y * 0.56f;
                    foreach (float side in new[] { -1f, 1f })
                        Add(track, brand, "wide", new Vector3(c.x + side * (size.x * 0.5f + 0.015f), y, c.z), side > 0 ? -90f : 90f, w);
                    float bw = Mathf.Min(size.x * 0.8f, h * wide);
                    Add(track, brand, "wide", new Vector3(c.x, y, back - 0.015f), 0f, bw);
                    break;
                }
            }
        }
    }
}
