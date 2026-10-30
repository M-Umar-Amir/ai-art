using System.Collections;
using System.Collections.Generic;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Watchable tour for review: launch with `-dash-tour`. Runs every crew member in every district on
    /// every ride (bicycle, scooter, hoverboard) in one visible window, with a caption naming the combo.
    /// The player's saved profile is backed up and restored, so the tour never spends coins or changes
    /// what the crew/district selection was. No screenshots: this is for watching the game, not evidence.
    public class DashTour : MonoBehaviour
    {
        static readonly string[] CharacterNames = { "ACE", "NOVA", "BOLT" };
        static readonly string[] DistrictNames = { "NEON DOWNTOWN", "SKYPARK", "FESTIVALS" };
        static readonly string[] RideNames = { "BICYCLE", "SCOOTER", "HOVERBOARD" };

        /// Seconds of riding per combination. The delivery clock is 28 s, so a combo never ends on the timer.
        const float RideSeconds = 16f;
        /// How long the title card holds before each run, so the crew member and district can be read.
        const float TitleSeconds = 1.6f;

        bool restored;
        readonly Dictionary<string, (bool exists, int value)> ints = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-dash-tour")
                {
                    new GameObject("Dash Tour").AddComponent<DashTour>();
                    Application.runInBackground = false;
                    return;
                }
        }

        void Backup()
        {
            foreach (var key in new[] { "character", "ride", "total_points", "coins", "dash_district", "coached_runs", "best", "missions_done" })
                ints[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
            for (int i = 0; i < 3; i++)
                ints["dash_outfit_" + i] = (PlayerPrefs.HasKey("dash_outfit_" + i), PlayerPrefs.GetInt("dash_outfit_" + i, 0));
        }

        void Restore()
        {
            if (restored) return;
            restored = true;
            PlayerProfile.ForceRide = -1;
            foreach (var pair in ints)
            {
                if (pair.Value.exists) PlayerPrefs.SetInt(pair.Key, pair.Value.value);
                else PlayerPrefs.DeleteKey(pair.Key);
            }
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            RiderInput.Override = null;
        }

        void OnApplicationQuit() => Restore();

        IEnumerator Start()
        {
            Backup();
            // Everything unlocked for the tour: the scooter and the hoverboard are earned items, and the
            // hoverboard costs 50 chips each time it is switched on.
            PlayerPrefs.SetInt("coached_runs", 6);
            PlayerPrefs.SetInt("missions_done", 6);
            PlayerProfile.TotalPoints = Mathf.Max(30000, PlayerProfile.TotalPoints);
            PlayerProfile.Coins = 5000;

            yield return new WaitForSeconds(1.2f);
            var menu = MenuUI.Instance;
            var gm = GameManager.Instance;
            var run = DashRun.Instance;
            var track = FindFirstObjectByType<TrackSpawner>();
            var rider = FindFirstObjectByType<RiderController>();
            var look = FindFirstObjectByType<RiderLook>();
            if (!menu || !gm || !run || !track || !rider || !look) { Debug.LogError("[DashTour] scene not ready"); Application.Quit(1); yield break; }
            MenuUI.ShowSkipHint = false;

            const int total = 3 * 3 * 3;
            int combo = 0;
            for (int district = 0; district < 3; district++)
            {
                DashRun.District = district;
                track.RefreshDistrict();
                for (int character = 0; character < 3; character++)
                {
                    for (int ride = 0; ride < 3; ride++)
                    {
                        combo++;
                        string label = CharacterNames[character] + "  ·  " + DistrictNames[district] + "  ·  " + RideNames[ride];
                        string progress = combo + " / " + total + "   (crew × district × ride)";
                        PlayerProfile.Character = character;
                        // Bicycle and scooter are the two rides; the hoverboard is a timed in-run vehicle.
                        PlayerProfile.ForceRide = ride == 1 ? 1 : 0;
                        look.Apply(character, true);
                        menu.Open("none");
                        menu.Caption(label, progress, TitleSeconds + 1.5f);
                        yield return new WaitForSeconds(TitleSeconds);

                        RiderInput.Override = () => Autopilot.Steer(track, rider);
                        gm.StartRun();
                        if (ride == 2)
                        {
                            yield return new WaitForSeconds(0.4f);
                            run.ToggleBoard();
                        }
                        yield return new WaitForSeconds(RideSeconds);

                        RiderInput.Override = null;
                        gm.ToMenu();
                        yield return new WaitForSeconds(0.9f);
                    }
                }
            }

            menu.Caption("TOUR COMPLETE", total + " combinations shown.", 3f);
            yield return new WaitForSeconds(2.5f);
            Restore();
            Application.Quit(0);
        }
    }
}
