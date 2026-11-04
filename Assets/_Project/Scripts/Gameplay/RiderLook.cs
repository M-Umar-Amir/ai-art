using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Ace and Nova ride their courier e-bikes (the bike is part of their seated slot); Bolt rolls on his
    /// built-in magnetic wheel.
    public class RiderLook : MonoBehaviour
    {
        // Serialized field names retained for existing scene compatibility; the boards are retired.
        public GameObject bicycle;
        public GameObject scooter;
        public Vector3 scooterSeat;
        public GameObject[] seated;
        public GameObject[] standing;

        /// Leaves the e-bike lying on its side in the road where the crash happened, for a few seconds.
        public void DropBike(int character, Vector3 at)
        {
            if (character < 0 || character >= seated.Length || !seated[character]) return;
            var bike = seated[character].transform.Find("E-bike");
            if (!bike) return;
            var dropped = Instantiate(bike.gameObject, at + new Vector3(0.4f, 0.32f, 1.2f), Quaternion.Euler(0f, 24f, 78f));
            dropped.name = "Dropped e-bike";
            dropped.transform.localScale = bike.lossyScale;
            Destroy(dropped, 6f);
        }

        System.Collections.IEnumerator Start()
        {
            // First render of a skinned, morphing mesh uploads its buffers; doing it on the crash frame
            // stuttered. Every slot is drawn once here, too small to see, then the real selection returns.
            var all = new System.Collections.Generic.List<GameObject>();
            foreach (var slot in seated) if (slot) all.Add(slot);
            foreach (var slot in standing) if (slot) all.Add(slot);
            var scales = new Vector3[all.Count];
            for (int i = 0; i < all.Count; i++) { scales[i] = all[i].transform.localScale; all[i].transform.localScale = Vector3.one * 0.001f; all[i].SetActive(true); }
            yield return null; yield return null;
            for (int i = 0; i < all.Count; i++) all[i].transform.localScale = scales[i];
            Apply(DallasDash.Core.PlayerProfile.Character, !(DallasDash.Core.DashRun.Instance && DallasDash.Core.DashRun.Instance.OnFoot));
        }

        public void Apply(int character, bool onBike)
        {
            character = Mathf.Clamp(character, 0, seated.Length - 1);
            if (bicycle) bicycle.SetActive(false);
            if (scooter) scooter.SetActive(false);
            for (int i = 0; i < seated.Length; i++)
            {
                if (seated[i]) seated[i].SetActive(onBike && i == character);
                if (i < standing.Length && standing[i]) standing[i].SetActive(!onBike && i == character);
            }
        }
    }
}
