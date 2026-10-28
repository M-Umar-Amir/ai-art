using System.Collections;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DallasDash.Gameplay
{
    /// Plays the asset-pack story beats in-engine around each run:
    /// 13 Loading Up at the Cluck &amp; Co curb, 14 Ran the Red at the junction light, 15 The Pursuit as the
    /// officer swings out behind. The first run of a session gets the full intro; later runs a short one.
    /// Tap or any key skips. The title (01 Cover) and Busted (19) framings live in FollowCamera and ChaseController.
    public class StoryDirector : MonoBehaviour
    {
        public static StoryDirector Instance { get; private set; }

        public RiderController rider;
        public RiderLook look;
        public ChaseController chase;
        public FollowCamera follow;
        public TrafficLight trafficLight;
        /// Brown paper bag that Finn drops into the delivery box.
        public Transform bag;

        /// The run proper starts at distance 0; the intro rolls the rider in from the curb behind it.
        public const float CurbZ = -60f;
        const float CurbX = 2.9f;

        bool playedFull;
        Coroutine running;

        void Awake() => Instance = this;

        void Start()
        {
            GameManager.Instance.StateChanged += s => { if (s == RunState.Ready) StageTitle(); };
            StageTitle();
        }

        /// 01 Cover: Finn on his bike at the start line, officer behind, camera in front.
        void StageTitle()
        {
            look.Apply(PlayerProfile.Character, true);
            rider.transform.position = new Vector3(0f, 0f, CurbZ + 6f);
            if (bag) bag.gameObject.SetActive(false);
            trafficLight.Set(TrafficLight.Phase.Green);
        }

        public void Play()
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(playedFull ? ShortIntro() : FullIntro());
            playedFull = true;
        }

        IEnumerator FullIntro()
        {
            var gm = GameManager.Instance;
            gm.BeginIntro();
            follow.cinematic = true;
            var cam = follow.transform;

            // ---- 13 Loading Up: standing beside the bike at the curb, bag into the box.
            look.Apply(PlayerProfile.Character, false);
            rider.transform.position = new Vector3(CurbX, 0f, CurbZ);
            chase.Place(new Vector3(-6f, 0f, CurbZ - 14f));
            trafficLight.Set(TrafficLight.Phase.Green);
            MenuUI.Instance.Caption("LOADING UP", "Order up at Cluck & Co", 2.4f);
            Vector3 bagFrom = rider.transform.position + new Vector3(0.9f, 1.05f, 0.35f);
            Vector3 bagTo = rider.transform.position + new Vector3(0f, 1.15f, -0.55f);
            if (bag) bag.gameObject.SetActive(true);
            for (float t = 0f; t < 2.4f; t += Time.deltaTime)
            {
                if (Skip()) { yield return Finish(); yield break; }
                Vector3 subject = rider.transform.position + new Vector3(0.4f, 1.1f, 0f);
                cam.position = subject + new Vector3(-3.4f + t * 0.25f, 0.5f, 2.6f);
                cam.LookAt(subject);
                if (bag)
                {
                    float k = Mathf.Clamp01((t - 0.9f) / 0.7f);
                    bag.position = Vector3.Lerp(bagFrom, bagTo, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.5f;
                    if (k >= 1f) bag.gameObject.SetActive(false);
                }
                yield return null;
            }

            // Hop on and pull out of the curb.
            look.Apply(PlayerProfile.Character, true);
            if (bag) bag.gameObject.SetActive(false);

            // ---- 14 Ran the Red: the junction light goes amber then red just before the rider reaches it.
            MenuUI.Instance.Caption("RAN THE RED!", "No time to wait: the order's getting cold", 2.6f);
            float z = CurbZ, speed = 4f, x = CurbX;
            for (float t = 0f; t < 2.6f; t += Time.deltaTime)
            {
                if (Skip()) { yield return Finish(); yield break; }
                speed = Mathf.MoveTowards(speed, 13f, Time.deltaTime * 6f);
                z += speed * Time.deltaTime;
                x = Mathf.MoveTowards(x, 0f, Time.deltaTime * 2.2f);
                rider.transform.position = new Vector3(x, 0f, z);
                trafficLight.Set(t < 0.6f ? TrafficLight.Phase.Green : t < 1.2f ? TrafficLight.Phase.Amber : TrafficLight.Phase.Red);
                // Wide shot from beyond the junction looking back: the light hangs in the top of frame
                // and the rider rides under it as it turns red.
                float lz = trafficLight.transform.position.z;
                Vector3 ahead = new(-2.6f, 3.2f, lz + 13f);
                cam.position = Vector3.Lerp(cam.position, ahead, 1f - Mathf.Exp(-5f * Time.deltaTime));
                Vector3 focus = Vector3.Lerp(new Vector3(0f, 3.4f, lz), rider.transform.position + Vector3.up * 1.2f, Mathf.Clamp01((z - lz + 6f) / 14f));
                cam.LookAt(focus);
                yield return null;
            }

            yield return Pursuit(z, speed);
        }

        IEnumerator ShortIntro()
        {
            var gm = GameManager.Instance;
            gm.BeginIntro();
            follow.cinematic = true;
            look.Apply(PlayerProfile.Character, true);
            trafficLight.Set(TrafficLight.Phase.Red);
            yield return Pursuit(-30f, 13f);
        }

        /// 15 The Pursuit: siren on, the officer swings out of the side street and falls in behind.
        IEnumerator Pursuit(float z, float speed)
        {
            var cam = follow.transform;
            MenuUI.Instance.Caption("THE PURSUIT", "Swipe to dodge. Two slips and you're busted.", 2.2f);
            chase.SirenBurst();
            Vector3 officerFrom = new(-4f, 0f, z - 9f);
            for (float t = 0f; t < 2.2f; t += Time.deltaTime)
            {
                if (Skip()) break;
                // Arrive at distance 0 exactly as the run takes over.
                float remaining = 2.2f - t;
                speed = Mathf.Max(speed, -z / Mathf.Max(remaining, 0.05f));
                z = Mathf.Min(0f, z + speed * Time.deltaTime);
                rider.transform.position = new Vector3(0f, 0f, z);
                float k = Mathf.SmoothStep(0f, 1f, t / 2.2f);
                chase.Place(Vector3.Lerp(officerFrom, new Vector3(0f, 0f, z - 4f), k));
                // Camera starts low behind both riders so the officer swinging in reads, then rises to the run camera.
                Vector3 pair = (rider.transform.position + chase.transform.position) * 0.5f + Vector3.up * 1.1f;
                Vector3 low = new(2.4f, 1.9f, z - 15f);
                Vector3 run = new(0f, follow.offset.y, z + follow.offset.z);
                cam.position = Vector3.Lerp(low, run, k);
                cam.LookAt(Vector3.Lerp(pair, new Vector3(0f, 1f, z + follow.lookAhead), k));
                yield return null;
            }
            yield return Finish();
        }

        IEnumerator Finish()
        {
            look.Apply(PlayerProfile.Character, true);
            if (bag) bag.gameObject.SetActive(false);
            trafficLight.Set(TrafficLight.Phase.Red);
            follow.cinematic = false;
            GameManager.Instance.StartRun();
            running = null;
            yield break;
        }

        static bool Skip()
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) return true;
            return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        }
    }
}
