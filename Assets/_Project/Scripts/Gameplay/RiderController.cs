using System;
using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Three-lane movement and the bunny-hop for the player rider. Forward travel follows GameManager.Distance.
    public class RiderController : MonoBehaviour
    {
        public float handling = 1f;
        public Transform visual;

        public int Lane { get; private set; } = 1;
        public bool Airborne => y > 0f;

        public event Action Jumped, Dropped, Landed, LaneChanged;

        /// Ground speed outside a run (the story intro): 0 while parked at the curb, so the bike stands still
        /// instead of pedalling in place. During a run the pace comes from GameManager.Speed.
        [NonSerialized] public float cruiseSpeed;

        float x, y, vy, squash, lean, wobble, pedal, jumpBuffer;

        /// Knocked off balance by an obstacle: a quick side-to-side wobble (TrackSpawner.Struck).
        public void Wobble() => wobble = 1f;

        /// Swiped the side of a lane-filling obstacle: knocked back into the lane the rider came from.
        public void BounceBack(int lane)
        {
            Lane = Mathf.Clamp(lane, 0, Tuning.Lanes - 1);
            wobble = 1f;
            squash = 0.25f;
        }
        Vector3 visualScale, visualPos;

        void Start()
        {
            if (visual) { visualScale = visual.localScale; visualPos = visual.localPosition; }
            GameManager.Instance.StateChanged += s => { if (s == RunState.Running) ResetRider(); };
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != RunState.Running || gm.Paused) { Pose(Time.deltaTime, 0f); return; }

            var action = Time.frameCount == gm.StartFrame ? RiderInput.Action.None : RiderInput.Poll();
            int lane = Lane;
            float dt = Time.deltaTime;
            switch (action)
            {
                case RiderInput.Action.Left: Lane = Mathf.Max(0, Lane - 1); break;
                case RiderInput.Action.Right: Lane = Mathf.Min(Tuning.Lanes - 1, Lane + 1); break;
                case RiderInput.Action.Jump: jumpBuffer = Tuning.JumpBufferSeconds; break;
                case RiderInput.Action.Down:
                    // Bunny-hopped too early: pull the bike back down to the road. Does nothing on the ground.
                    if (Airborne && vy > Tuning.DiveV) { vy = Tuning.DiveV; jumpBuffer = 0f; Dropped?.Invoke(); }
                    break;
            }
            if (lane != Lane) LaneChanged?.Invoke();
            if (jumpBuffer > 0f && !Airborne && vy <= 0f)
            {
                jumpBuffer = 0f;
                vy = Tuning.JumpV;
                squash = -0.25f;
                Jumped?.Invoke();
                Missions.Add(MissionStat.Jumps, 1);
            }
            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);

            float step = Tuning.LaneSlewPerSecond * handling * dt;
            float dx = Mathf.Clamp(Tuning.LaneX[Lane] - x, -step, step);
            x += dx;

            if (Airborne || vy > 0f)
            {
                vy -= Tuning.Gravity * dt;
                y = Mathf.Max(0f, y + vy * dt);
                if (y == 0f) { vy = 0f; squash = 0.3f; Landed?.Invoke(); }
            }

            transform.position = new Vector3(x, y, gm.Distance);
            Pose(dt, dt > 0f ? dx / dt : 0f);
        }

        /// Lean into lane changes, pitch with the jump arc, squash on take-off and landing.
        void Pose(float dt, float lateralSpeed)
        {
            if (!visual) return;
            lean = Mathf.Lerp(lean, -lateralSpeed * 1.4f, 1f - Mathf.Exp(-12f * dt));
            float pitch = Airborne ? Mathf.Clamp(-vy * 1.6f, -18f, 18f) : 0f;
            // Pedalling: cadence follows speed; a small bob and alternating sway while on the ground.
            var gm = GameManager.Instance;
            // Only while the bike is actually moving: in the Loading Up scene it is parked at the curb, and the
            // run's base speed used to make it sway and bob on the spot (the client saw it shaking).
            float pace = gm && gm.State == RunState.Running ? gm.Speed : gm && gm.State == RunState.Intro ? cruiseSpeed : 0f;
            bool riding = pace > 0.5f && !Airborne;
            pedal += dt * pace * 0.55f;
            // Ease the pedalling motion in as the rider pulls away rather than snapping to full sway.
            float amount = Mathf.Clamp01(pace / 8f);
            float bob = riding ? Mathf.Abs(Mathf.Sin(pedal)) * 0.035f * amount : 0f;
            float sway = riding ? Mathf.Sin(pedal) * 2.2f * amount : 0f;
            wobble = Mathf.MoveTowards(wobble, 0f, dt * 1.6f);
            float wob = Mathf.Sin(Time.time * 28f) * 14f * wobble;
            squash = Mathf.MoveTowards(squash, 0f, dt * 2.2f);
            float s = Mathf.Sin(squash * Mathf.PI * 2f) * 0.12f;
            var scale = Vector3.Scale(visualScale, new Vector3(1f + s, 1f - s, 1f + s));
            visual.localScale = scale;
            visual.localRotation = Quaternion.Euler(pitch, wob * 0.4f, Mathf.Clamp(lean, -22f, 22f) + sway + wob);
            visual.localPosition = visualPos + Vector3.up * bob;
        }

        public void ResetRider()
        {
            Lane = 1;
            x = y = vy = squash = lean = wobble = jumpBuffer = 0f;
        }
    }
}
