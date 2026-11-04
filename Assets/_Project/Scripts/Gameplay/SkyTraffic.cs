using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Air taxis and delivery drones gliding over Dash City, as on the PDF cover. Ten craft built once from
    /// shared materials, flown in loops around the camera, so they cost a few draw calls and no streaming.
    public sealed class SkyTraffic : MonoBehaviour
    {
        static SkyTraffic instance;
        const int Count = 10;
        readonly Transform[] craft = new Transform[Count];
        readonly Vector3[] lanes = new Vector3[Count];
        readonly float[] speed = new float[Count], phase = new float[Count];

        TrackSpawner track;

        public static void Ensure(TrackSpawner track)
        {
            if (instance || !track || !track.tintMaterial) return;
            instance = new GameObject("Dash City / sky traffic").AddComponent<SkyTraffic>();
            instance.Build(track);
        }

        void Build(TrackSpawner owner)
        {
            track = owner;
            var hull = DashCity.Mat(track, "skyTaxiHull", new Color(.06f, .07f, .12f));
            var cyan = DashCity.Mat(track, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
            var pink = DashCity.Mat(track, "roofNeonPink", new Color(1f, 0.1f, 0.55f), 2.2f);
            var warm = DashCity.Mat(track, "occupied-window", new Color(1f, .65f, .3f), 1.1f);
            for (int i = 0; i < Count; i++)
            {
                var root = new GameObject("Air taxi " + i).transform;
                root.SetParent(transform, false);
                bool drone = i % 3 == 2;
                Part(root, PrimitiveType.Capsule, Vector3.zero, drone ? new Vector3(.9f, .35f, .9f) : new Vector3(1.4f, .7f, 3.4f), Quaternion.Euler(drone ? 0 : 90, 0, 0), hull);
                Part(root, PrimitiveType.Cube, new Vector3(0, -.36f, 0), drone ? new Vector3(.8f, .05f, .8f) : new Vector3(1.1f, .06f, 2.8f), Quaternion.identity, cyan);
                Part(root, PrimitiveType.Cube, new Vector3(0, .05f, drone ? 0 : -1.65f), drone ? new Vector3(.2f, .2f, .2f) : new Vector3(1f, .12f, .1f), Quaternion.identity, pink);
                if (!drone) Part(root, PrimitiveType.Cube, new Vector3(0, .3f, .6f), new Vector3(1f, .3f, 1.2f), Quaternion.identity, warm);
                craft[i] = root;
                float side = i % 2 == 0 ? -1f : 1f;
                lanes[i] = new Vector3(side * (16f + (i * 7) % 22), 24f + (i * 11) % 22, (i * 37) % 240);
                speed[i] = (i % 4 == 0 ? -1f : 1f) * (9f + (i * 5) % 11);
                phase[i] = i * 1.7f;
            }
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 at, Vector3 size, Quaternion rot, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = size; go.transform.localRotation = rot;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void LateUpdate()
        {
            var view = Camera.main;
            if (!view || !craft[0]) return;
            Vector3 cam = view.transform.position;
            bool city = cam.x > 1000f;
            float t = Time.time;
            for (int i = 0; i < Count; i++)
            {
                // Each craft runs its own lane along the street, wrapping round a 240 m window ahead of the lens.
                float z = Mathf.Repeat(lanes[i].z + speed[i] * t - cam.z + 40f, 240f) + cam.z - 40f;
                var p = new Vector3((city ? 5000f : 0f) + lanes[i].x + Mathf.Sin(t * .3f + phase[i]) * 3f,
                    (city ? 100f : 0f) + lanes[i].y + Mathf.Sin(t * .7f + phase[i]) * .8f, z);
                craft[i].SetPositionAndRotation(p, Quaternion.Euler(0, speed[i] < 0 ? 180f : 0f, Mathf.Sin(t * .5f + phase[i]) * 6f));
                craft[i].gameObject.SetActive(PlayerProfile.Graphics > 0 || i < 5);
            }
        }
    }
}
