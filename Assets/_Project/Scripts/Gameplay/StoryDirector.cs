using System.Collections;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DallasDash.Gameplay
{
    /// Plays the delivery briefing in-engine before a run: 13 Loading Up at the Cluck &amp; Co curb, then the
    /// order goes on the clock and the rider pulls away. There is no pursuit: the run is a timed delivery
    /// course. The first run of a session gets the full intro; later runs a short one. Tap or any key skips.
    public class StoryDirector : MonoBehaviour
    {
        public static StoryDirector Instance { get; private set; }

        public RiderController rider;
        public RiderLook look;
        /// Kept for scene compatibility; the chase system is disabled and this is null.
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

        /// 01 Cover: Finn on his bike at the start line, camera in front.
        void StageTitle()
        {
            look.Apply(PlayerProfile.Character, true);
            rider.ResetRider();
            rider.cruiseSpeed = 0f;
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
            var cam = follow.CinematicRig;

            // ---- 13 Loading Up: standing beside the bike at the curb, bag into the box. The bike is parked:
            // no pedalling motion, no leftover wobble from the last run.
            rider.ResetRider();
            rider.cruiseSpeed = 0f;
            look.Apply(PlayerProfile.Character, false);
            rider.transform.position = new Vector3(CurbX, 0f, CurbZ);
            trafficLight.Set(TrafficLight.Phase.Green);
            MenuUI.Instance.Caption("LOADING UP", "FOOD INC delivery league", 2.4f);
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

            // Hop on and pull out of the curb: the order is on the clock.
            look.Apply(PlayerProfile.Character, true);
            if (bag) bag.gameObject.SetActive(false);

            // ---- 14 On the Clock: the rider pulls away and the camera settles in behind for the run.
            MenuUI.Instance.Caption("ON THE CLOCK", "28 seconds to the drop. Beat the timer.", 2.6f);
            float z = CurbZ, speed = 4f, x = CurbX;
            for (float t = 0f; t < 2.6f; t += Time.deltaTime)
            {
                if (Skip()) { yield return Finish(); yield break; }
                speed = Mathf.MoveTowards(speed, 15f, Time.deltaTime * 6f);
                z += speed * Time.deltaTime;
                rider.cruiseSpeed = speed;
                x = Mathf.MoveTowards(x, 0f, Time.deltaTime * 2.2f);
                rider.transform.position = new Vector3(x, 0f, z);
                // Start wide beside the curb, then swing behind the rider into the run camera.
                float k = Mathf.SmoothStep(0f, 1f, t / 2.6f);
                Vector3 side = new(3.4f, 2.1f, z - 4f);
                Vector3 run = new(0f, follow.offset.y, z + follow.offset.z);
                cam.position = Vector3.Lerp(side, run, k);
                cam.LookAt(Vector3.Lerp(rider.transform.position + Vector3.up * 1.2f, new Vector3(0f, 1f, z + follow.lookAhead), k));
                yield return null;
            }

            // ---- 15 The Route: the two things that buy time, called out before the run takes over.
            MenuUI.Instance.Caption("SHORTCUTS & ROOFTOPS", "Cyan gates and rooftop runs. Both buy you time.", 2.2f);
            yield return RideToStart(z, speed);
        }

        IEnumerator ShortIntro()
        {
            var gm = GameManager.Instance;
            gm.BeginIntro();
            follow.cinematic = true;
            look.Apply(PlayerProfile.Character, true);
            trafficLight.Set(TrafficLight.Phase.Green);
            MenuUI.Instance.Caption("ON THE CLOCK", "Beat the timer.", 1.4f);
            yield return RideToStart(-26f, 15f);
        }

        /// Rolls the rider in to distance 0 exactly as the run takes over, on the run camera.
        IEnumerator RideToStart(float z, float speed)
        {
            var cam = follow.CinematicRig;
            const float beat = 2.2f;
            for (float t = 0f; t < beat; t += Time.deltaTime)
            {
                if (Skip()) break;
                // Arrive at distance 0 exactly as the run takes over.
                float remaining = beat - t;
                speed = Mathf.Max(speed, -z / Mathf.Max(remaining, 0.05f));
                z = Mathf.Min(0f, z + speed * Time.deltaTime);
                rider.cruiseSpeed = speed;
                rider.transform.position = new Vector3(0f, 0f, z);
                Vector3 run = new(0f, follow.offset.y, z + follow.offset.z);
                cam.position = Vector3.Lerp(cam.position, run, 1f - Mathf.Exp(-6f * Time.deltaTime));
                cam.LookAt(new Vector3(0f, 1f, z + follow.lookAhead));
                yield return null;
            }
            yield return Finish();
        }

        IEnumerator Finish()
        {
            look.Apply(PlayerProfile.Character, true);
            if (bag) bag.gameObject.SetActive(false);
            trafficLight.Set(TrafficLight.Phase.Green);
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
