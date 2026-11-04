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
            for (int i = 0; i < (hair?.Length ?? 0); i++)
            {
                if (!hair[i]) continue;
                hair[i].gameObject.SetActive(i == pick);
                if (i == pick) hair[i].SetPropertyBlock(tint);
            }
            if (!animator) return;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
            animator.speed = 0.88f + (Mathf.Abs(variant) % 5) * 0.06f;
            animator.Play(clip, 0, (Mathf.Abs(variant) * 0.37f) % 1f);
        }
    }
}
