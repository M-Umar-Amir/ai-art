using UnityEngine;

namespace DallasDash.Gameplay
{
    /// One pedestrian: picks an outfit, hair and clip, and plays it with a per-person phase and pace.
    public class Pedestrian : MonoBehaviour
    {
        public bool female;
        /// The catalogue prefab this one was made from (for pooling); not serialized.
        [System.NonSerialized] public Pedestrian template;
        public SkinnedMeshRenderer body;
        public Renderer[] hair;
        public Animator animator;

        public const string Walk = "Walk_Loop", Idle = "Idle_Loop", Chat = "Idle_FoldArms_Loop", Dance = "Dance_Loop", Jog = "Jog_Fwd_Loop";

        /// Every pedestrian in the world right now, for the rider contact check.
        public static readonly System.Collections.Generic.List<Pedestrian> Active = new();
        void OnEnable() { Active.Add(this); reacting = false; }
        void OnDisable() { Active.Remove(this); StopAllCoroutines(); reacting = false; transform.localScale = Vector3.one; }

        bool reacting;
        string clipNow = Walk;
        public bool Reacting => reacting;

        /// Hit by a rider on an e-bike: knocked off their feet, away from the bike; up again after a moment.
        public void KnockDown(Vector3 rider) { if (!reacting) StartCoroutine(Fall(rider)); }

        /// Bumped by a rider on foot: they stagger aside, turn to look, fold their arms and carry on.
        public void ShrugOff(Vector3 rider) { if (!reacting) StartCoroutine(Shrug(rider)); }

        System.Collections.IEnumerator Fall(Vector3 rider)
        {
            reacting = true;
            var movers = Pause();
            float side = transform.position.x >= rider.x ? 1f : -1f;
            var start = transform.rotation; var startPos = transform.position;
            // Pivot about the feet, toppling away from the bike and a little ahead with its momentum.
            var lying = Quaternion.Euler(0f, 0f, -side * 82f) * Quaternion.Euler(18f, 0f, 0f) * start;
            var slid = startPos + new Vector3(side * 0.9f, 0f, 1.6f);
            if (animator) animator.Play(Idle, 0, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.32f)
            {
                float k = t * t;
                transform.SetPositionAndRotation(Vector3.Lerp(startPos, slid, Mathf.Sqrt(t)), Quaternion.Slerp(start, lying, k));
                yield return null;
            }
            transform.SetPositionAndRotation(slid + Vector3.up * 0.12f, lying);
            yield return new WaitForSeconds(2.2f);
            var upright = Quaternion.Euler(0f, start.eulerAngles.y, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.7f)
            {
                transform.rotation = Quaternion.Slerp(lying, upright, Mathf.SmoothStep(0f, 1f, t));
                transform.position = slid + Vector3.up * 0.12f * (1f - t);
                yield return null;
            }
            Resume(movers);
        }

        System.Collections.IEnumerator Shrug(Vector3 rider)
        {
            reacting = true;
            var movers = Pause();
            float side = transform.position.x >= rider.x ? 1f : -1f;
            var startPos = transform.position;
            var aside = startPos + new Vector3(side * 0.7f, 0f, 0.3f);
            var face = Quaternion.LookRotation(new Vector3(rider.x - aside.x, 0f, -1.5f));
            var start = transform.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                transform.position = Vector3.Lerp(startPos, aside, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.05f;
                transform.rotation = Quaternion.Slerp(start, face, t);
                yield return null;
            }
            if (animator) animator.Play(Chat, 0, 0f);
            yield return new WaitForSeconds(1.8f);
            Resume(movers);
        }

        // ---------------------------------------------------------------- knit toques
        // Downtown Toronto in late November (client, 25 Nov 2026: "people ... based on downtown toronto"): about half
        // the crowd wears a knit toque with a pom-pom. One shared mesh; one renderer per pooled body, built once on the
        // head bone while the body is still in its bind pose (straight after pooling), so it then rides the animation.

        Renderer toque;
        static Mesh toqueMesh;
        static readonly System.Collections.Generic.Dictionary<int, Material> toqueMats = new();
        static readonly Color[] ToqueColours =
        {
            new(.72f, .08f, .1f), new(.12f, .16f, .3f), new(.2f, .2f, .22f), new(.86f, .83f, .76f), new(.18f, .32f, .22f), new(.85f, .6f, .12f),
        };

        bool Toque(int variant)
        {
            bool wear = DallasDash.Core.DashRun.District == 0 && Mathf.Abs(variant * 13 + 5) % 100 < 48;
            // On/off by GameObject: DetailCull and the pool switch renderers, not objects.
            if (!wear) { if (toque) toque.gameObject.SetActive(false); return false; }
            if (!toque && !BuildToque()) return false;
            int c = Mathf.Abs(variant * 5 + 1) % ToqueColours.Length;
            if (!toqueMats.TryGetValue(c, out var m) || !m)
            {
                m = new Material(body.sharedMaterial) { name = "Toque " + c };
                m.SetTexture("_BaseMap", Texture2D.whiteTexture);
                m.SetColor("_BaseColor", ToqueColours[c]);
                m.SetColor("_EmissionColor", Color.black);
                if (m.HasProperty("_EmissionMapOn")) m.SetFloat("_EmissionMapOn", 0f);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .1f);
                toqueMats[c] = m;
            }
            toque.sharedMaterial = m;
            toque.gameObject.SetActive(true);
            return true;
        }

        bool BuildToque()
        {
            if (!body) return false;
            Transform head = null;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "Head") { head = t; break; }
            if (!head) return false;
            if (!toqueMesh)
            {
                // Crown (lower half hidden in the head), ribbed cuff and pom-pom, merged; origin at the cuff.
                var parts = new System.Collections.Generic.List<CombineInstance>();
                Mesh Shape(PrimitiveType type)
                {
                    var go = GameObject.CreatePrimitive(type);
                    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                    Destroy(go);
                    return mesh;
                }
                var sphere = Shape(PrimitiveType.Sphere); var cylinder = Shape(PrimitiveType.Cylinder);
                parts.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(new Vector3(0f, .05f, 0f), Quaternion.identity, new Vector3(.215f, .15f, .23f)) });
                parts.Add(new CombineInstance { mesh = cylinder, transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(.226f, .03f, .238f)) });
                parts.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(new Vector3(0f, .135f, 0f), Quaternion.identity, Vector3.one * .065f) });
                toqueMesh = new Mesh { name = "Toque" };
                toqueMesh.CombineMeshes(parts.ToArray(), true, true);
            }
            // The crown of the head is the top of the bind-pose body; the cuff sits 8.5 cm below it.
            float top = body.bounds.max.y;
            var hat = new GameObject("Toque", typeof(MeshFilter), typeof(MeshRenderer));
            hat.transform.SetPositionAndRotation(new Vector3(head.position.x, top - .085f, head.position.z), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            hat.transform.SetParent(head, true);
            hat.GetComponent<MeshFilter>().sharedMesh = toqueMesh;
            toque = hat.GetComponent<MeshRenderer>();
            toque.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return true;
        }

        Behaviour[] Pause()
        {
            var movers = System.Array.FindAll(GetComponents<Behaviour>(), b => b is LifeStroller || b is StreetCrosser);
            foreach (var m in movers) m.enabled = false;
            return movers;
        }

        void Resume(Behaviour[] movers)
        {
            foreach (var m in movers) if (m) m.enabled = true;
            if (animator) animator.Play(clipNow, 0, 0f);
            reacting = false;
        }

        public void Dress(PeopleCatalogue catalogue, int variant, string clip)
        {
            clipNow = clip;
            var outfits = female ? catalogue.femaleOutfits : catalogue.maleOutfits;
            if (body && outfits != null && outfits.Length > 0) body.sharedMaterial = outfits[Mathf.Abs(variant) % outfits.Length];
            int pick = hair != null && hair.Length > 0 ? Mathf.Abs(variant * 7 + 3) % hair.Length : -1;
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", catalogue.hairTints[Mathf.Abs(variant * 3) % catalogue.hairTints.Length] * 2.2f);
            bool toqued = Toque(variant);
            for (int i = 0; i < (hair?.Length ?? 0); i++)
            {
                if (!hair[i]) continue;
                // Under a toque only long hair (and a beard) still shows.
                string n = hair[i].name;
                bool hidden = toqued && (n.Contains("Buns") || n.Contains("Buzzed") || n.Contains("SimpleParted"));
                hair[i].gameObject.SetActive(i == pick && !hidden);
                if (i == pick) hair[i].SetPropertyBlock(tint);
            }
            if (!animator) return;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
            animator.speed = 0.88f + (Mathf.Abs(variant) % 5) * 0.06f;
            animator.Play(clip, 0, (Mathf.Abs(variant) * 0.37f) % 1f);
        }
    }
}
