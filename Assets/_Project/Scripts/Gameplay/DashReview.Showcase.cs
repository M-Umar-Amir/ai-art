using System.Collections;
using System.IO;
using System.Reflection;
using DallasDash.Core;
using DallasDash.UI;
using Unity.Cinemachine;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The showcase (-dash-showcase <out>): one continuous take, no narration, game music only, covering every part
    /// of the run. 19 Nov 2026 round: the approved concept sheets (new Ace, Nova and Bolt back, new officer and hurdles).
    /// Shot list (camera state in brackets):
    ///   1. Title screen, coupon meter and START CHASE [menu camera, wide] - decompression beat.
    ///   1b. The crew carousel: Ace, Nova, Bolt on the lit pedestal [menu, medium portrait].
    ///   2. (Retired 25 Nov 2026, client: no 360-degree model view.) The rider orbits are gone.
    ///   3. The short "police chase" intro [authored intro rig] handing off to the run's follow camera. (25 Nov 2026,
    ///      client: no "Loading Up" and no "Ran the Red" scenes in the showcase.)
    ///   4. The chase on the branded street [gameplay follow]: police meter, shops, billboards, branded hurdles.
    ///   5. A crash: knocked off, the officer rides into frame, Ace grabs his bike back [follow].
    ///   6. (Retired 11 Nov 2026: the Skypark portal shortcut.) Long clean riding down Downtown [follow].
    ///   7. The 10% coupon on the road and the coupon-won card [follow].
    ///   8. Caught: revive offer, revive for burgers, ride on; caught again, decline, BUSTED card [follow, then card].
    /// The run is ridden through the normal input path by the bot; the only scripted events are the crash and the
    /// two catches (logged in showcase-events.csv), so every system in the video is the real game.
    public partial class DashReview
    {
        bool showcase;
        readonly System.Text.StringBuilder showcaseLog = new();

        void Event(string what)
        {
            var gm = GameManager.Instance;
            showcaseLog.AppendLine($"{filmFrame / 30f:F2},{(gm ? gm.Distance : 0f):F0},{what}");
            Debug.Log("[Showcase] " + what);
        }

        IEnumerator Frames(int n) { int start = filmFrame; while (filmFrame < start + n) yield return null; }

        IEnumerator Showcase()
        {
            PlayerPrefs.SetInt("coached_runs", 2);
            // The run is ridden as Bolt by default (user, 25 Nov 2026: "for recording later, use bolt"; his updated
            // mesh is in, Ace and Nova follow); -rider 0|1|2 picks another.
            PlayerProfile.Character = showcaseRider;
            BurgerPrices.UnlockAllForReview();
            DashRun.AutoDeclineRevive = false;
            PlayerProfile.Burgers = 30000;
            // The coupon is 60 pickups away, so it comes up on the road during the take.
            PlayerProfile.CouponProgress = BurgerPrices.CouponThreshold - 60 * BurgerPrices.PickupValue;
            yield return new WaitForSeconds(2f);
            var menu = MenuUI.Instance; var gm = GameManager.Instance; var run = DashRun.Instance;
            var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            var look = FindFirstObjectByType<RiderLook>();
            Check(menu && gm && run && track && rider && look, "showcase scene dependencies");
            typeof(MenuUI).GetMethod("ExitCity", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(menu, null);
            menu.Open("none"); track.RefreshDistrict();
            for (int f = 0; f < 90; f++) yield return null;

            filmSeconds = 200; // upper bound; the take ends when the shot list does
            PrepareFilmCamera(); SaveFilmMusic(); StartFilmEncoder();
            Time.captureFramerate = 30; Application.targetFrameRate = 30; recording = true;

            // 1. Title.
            Event("TITLE");
            yield return Frames(120);

            // 1b. The crew carousel (browsing only: the selected rider stays the showcase rider).
            menu.Open("crew"); Event("CREW ACE");
            yield return Frames(66);
            menu.FlipCrew(1); Event("CREW NOVA");
            yield return Frames(66);
            menu.FlipCrew(1); Event("CREW BOLT");
            yield return Frames(66);
            menu.FlipCrew(1); menu.Open("none");
            yield return Frames(15);

            // 3. The short intro (no Loading Up, no Ran the Red), then the run.
            Event("INTRO");
            RiderInput.Override = () => ShowcaseInput(track, rider, run, gm);
            if (StoryDirector.Instance) StoryDirector.Instance.PlayShort(); else gm.StartRun();
            for (int guard = 0; gm.State != RunState.Running && guard < 30 * 20; guard++) yield return null;
            Check(gm.State == RunState.Running, "showcase run starts");
            Event("RUN");

            // 4-7. Ride: the crash, the remount, Downtown, the coupon. Unscheduled catches are revived (that is
            // shown properly in beat 8); the take ends early only if the run ends.
            bool crashed = false, back = false, coupon = false, couponWon = false;
            int coupons = DemoRewards.Ledger.coupons.Count;
            float backAt = -1f;
            int frameLimit = 175 * 30;
            while (filmFrame < frameLimit && gm.State == RunState.Running)
            {
                float d = gm.Distance;
                if (run.AwaitingRevive) { yield return Frames(30); Revive(menu); Event("REVIVED (unscheduled)"); continue; }
                if (!crashed && d > 230f && !run.OnFoot && rider.Traversal.Surface == RunSurface.Road && !rider.Airborne
                    && !TrackSpawner.NearJunction(d + 15f) && !TrackSpawner.NearJunction(d + 60f))
                {
                    crashed = true;
                    // Bolt rolls on his own wheels: a crash staggers him instead of knocking him off a bike.
                    if (DashRun.RidesOwnWheels) { run.Crash(); Event("CRASH, STAGGERED"); }
                    else { run.KnockOffBike(); run.PoliceGains(DashRun.CrashOnBike); Event("CRASH, ON FOOT"); }
                }
                if (crashed && !run.OnFoot && !back) { back = true; backAt = d; Event("BACK ON THE E-BIKE"); }
                if (track.Coupon && !coupon) { coupon = true; Event("COUPON ON THE ROAD"); }
                if (!couponWon && DemoRewards.Ledger.coupons.Count > coupons) { couponWon = true; Event("COUPON WON"); }
                if (crashed && back && (couponWon || d > backAt + 500f) && d > backAt + 120f && !run.OnFoot) break;
                yield return null;
            }

            // 8. Caught, revive, ride on; caught again, decline, results.
            if (gm.State == RunState.Running)
            {
                yield return Frames(45);
                run.PoliceGains(1000f); Event("CAUGHT");
                yield return Frames(50);
                Revive(menu); Event("REVIVED FOR BURGERS");
                yield return Frames(200);
                run.PoliceGains(1000f);
                for (int guard = 0; !run.AwaitingRevive && guard < 120; guard++) { yield return Frames(5); run.PoliceGains(1000f); }
                Event("CAUGHT AGAIN");
                yield return Frames(45);
                typeof(MenuUI).GetMethod("DeclineRevive", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
                Event("BUSTED");
            }
            yield return Frames(170); // the bust two-shot and the results card
            Event("END");
            RiderInput.Override = null;
            recording = false;
            FinishFilmEncoder();
            File.WriteAllText(Path.Combine(output, "showcase-events.csv"), "seconds,distance,event\n" + showcaseLog);
            File.WriteAllText(Path.Combine(output, "recording-pass.txt"),
                $"frames={filmFrame}; seconds={filmFrame / 30f:F1}; crash={crashed}; remounted={back}; coupon={coupon}; couponWon={couponWon}; distance={gm.Distance:F0}\n");
            gm.ToMenu(); Restore();
            yield return new WaitForSecondsRealtime(0.3f); Application.Quit(0);
        }

        void Revive(MenuUI menu) => typeof(MenuUI).GetMethod("AcceptRevive", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);

        /// The bot's normal input (the same rules as the playtest).
        static RiderInput.Action ShowcaseInput(TrackSpawner track, RiderController rider, DashRun run, GameManager gm) => Autopilot.Steer(track, rider);
    }
}
