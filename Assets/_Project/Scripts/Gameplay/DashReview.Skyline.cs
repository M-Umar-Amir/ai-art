using System.Collections;
using System.IO;
using System.Reflection;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// -dash-skyline <dir>: the Toronto skyline check (client, 25 Nov 2026: the CN Tower and the Financial District
    /// must reach right down to the street end, with no pink fogged street buildings under or between them).
    /// Captures the title camera, then the chase camera at the street's full right sway, centre and full left sway.
    /// Each frame is saved as played and with the skyline keyed flat green, so what is skyline and what is the
    /// street's own far end can be told apart at a glance.
    public partial class DashReview
    {
        bool skylineProbe;

        IEnumerator SkylineProbe()
        {
            PlayerPrefs.SetInt("coached_runs", 2);
            yield return new WaitForSeconds(1.5f);
            var menu = MenuUI.Instance; var gm = GameManager.Instance;
            var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            Check(menu && gm && track && rider, "scene dependencies");
            typeof(MenuUI).GetMethod("ExitCity", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(menu, null);
            yield return null;
            menu.Open("none"); track.RefreshDistrict();
            yield return new WaitForSeconds(1f);
            Check(TorontoSkyline.Instance, "the Toronto skyline is built downtown");
            yield return SkylinePair("title");

            DashRun.AutoDeclineRevive = true;
            RiderInput.Override = () => RiderInput.Action.None;
            // CurvedWorld: side = sin(distance / 900 m * 2 pi) * sway, so full right at 225 m, centre at 450 m and
            // full left at 675 m. Placed a little short of each, the rider rides into the peak while the blocks ahead
            // stream in and the camera settles.
            foreach (var (at, label) in new[] { (225f, "sway-right"), (450f, "sway-centre"), (675f, "sway-left") })
            {
                gm.StartRun(); yield return null;
                Place(at - 8f, track, rider); rider.BounceBack(1); track.ClearAhead(at - 8f, 240f);
                yield return new WaitForSeconds(0.35f);
                // Frozen for the pair, so a crash or the officer cannot change the frame between the two captures.
                Time.timeScale = 0f;
                Check(TorontoSkyline.Instance.Shown, "the skyline shows ahead in the run: " + label);
                yield return SkylinePair(label);
                Time.timeScale = 1f;
            }
            gm.Bust(); gm.ToMenu(); Restore();
            File.WriteAllText(Path.Combine(output, "skyline-pass.txt"), $"PASS {assertions} assertions; {shot} screenshots\n");
            Debug.Log($"[DashReview] SKYLINE COMPLETE {assertions} assertions");
            yield return new WaitForSecondsRealtime(0.2f); Application.Quit(0);
        }

        /// The frame as played, then the same frame with the skyline keyed green; HUD and banners hidden for both.
        IEnumerator SkylinePair(string label)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var shown = new bool[canvases.Length];
            for (int i = 0; i < canvases.Length; i++) { shown[i] = canvases[i].enabled; canvases[i].enabled = false; }
            yield return Shot(label);
            TorontoSkyline.Instance.Key(true);
            yield return Shot(label + "-key");
            TorontoSkyline.Instance.Key(false);
            for (int i = 0; i < canvases.Length; i++) if (canvases[i]) canvases[i].enabled = shown[i];
        }
    }
}
