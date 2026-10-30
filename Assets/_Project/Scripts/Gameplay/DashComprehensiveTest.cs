using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace DallasDash.Gameplay
{
    // Comprehensive test: every character (Ace, Nova, Bolt) with every vehicle (none, board) in every district
    public class DashComprehensiveTest : MonoBehaviour
    {
        string output;
        int district, character, vehicle;
        bool restored;
        readonly Dictionary<string, (bool exists, int value)> ints = new();
        readonly Dictionary<string, (bool exists, string value)> strings = new();
        
        static readonly string[] CharacterNames = { "Ace", "Nova", "Bolt" };
        static readonly string[] VehicleNames = { "None", "Board" };
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-dash-comprehensive")
                {
                    var test = new GameObject("Dash Comprehensive Test").AddComponent<DashComprehensiveTest>();
                    Application.runInBackground = true;
                    test.output = args[i + 1];
                    for (int j = 0; j < args.Length - 1; j++)
                    {
                        if (args[j] == "-district") int.TryParse(args[j + 1], out test.district);
                        if (args[j] == "-character") int.TryParse(args[j + 1], out test.character);
                        if (args[j] == "-vehicle") int.TryParse(args[j + 1], out test.vehicle);
                    }
                    return;
                }
            }
        }
        
        void Check(bool ok, string message)
        {
            if (ok) { Debug.Log("[DashComprehensive] PASS " + message); return; }
            Debug.LogError("[DashComprehensive] FAIL " + message); Restore(); Application.Quit(1);
            throw new Exception(message);
        }
        
        void Backup()
        {
            var keys = new List<string> { "character", "total_points", "dash_district", "coins", "ride", "coached_runs", "best", "missions_done", "missions_v", "daily_last", "daily_streak" };
            keys.AddRange(new[] { "dash_fragments", "dash_outfit_tokens", "dash_event_collectibles", "dash_deliveries" });
            for (int i = 0; i < 3; i++) { keys.Add("dash_outfit_" + i); keys.Add("dash_outfit_owned_" + i); }
            for (int i = 0; i < 3; i++) foreach (var field in new[] { "stat", "target", "progress" }) keys.Add($"mission{i}_{field}");
            foreach (var key in keys) ints[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
            string rewards = "dash_riders_demo_rewards_v1";
            strings[rewards] = (PlayerPrefs.HasKey(rewards), PlayerPrefs.GetString(rewards));
            strings["lb_runs"] = (PlayerPrefs.HasKey("lb_runs"), PlayerPrefs.GetString("lb_runs"));
        }
        
        void Restore()
        {
            if (restored) return; restored = true;
            foreach (var pair in ints) { if (pair.Value.exists) PlayerPrefs.SetInt(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key); }
            foreach (var pair in strings) { if (pair.Value.exists) PlayerPrefs.SetString(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key); }
            PlayerPrefs.Save(); Time.timeScale = 1f; RiderInput.Override = null;
        }
        
        void OnApplicationQuit() => Restore();
        
        IEnumerator CaptureScreenshot(string label)
        {
            yield return new WaitForSecondsRealtime(0.3f);
            var cam = Camera.main;
            const int width = 1080, height = 1920;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera;
                if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam; canvases[i].planeDistance = 0.5f;
            }
            Canvas.ForceUpdateCanvases();
            foreach (var auxiliary in Camera.allCameras)
                if (auxiliary != cam && auxiliary.enabled && auxiliary.targetTexture)
                    RenderPipeline.SubmitRenderRequest(auxiliary, new RenderPipeline.StandardRequest { destination = auxiliary.targetTexture });
            var request = new RenderPipeline.StandardRequest { destination = rt };
            RenderPipeline.SubmitRenderRequest(cam, request);
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            string filename = $"{CharacterNames[character]}_{VehicleNames[vehicle]}_District{district}_{label}.png";
            File.WriteAllBytes(Path.Combine(output, filename), texture.EncodeToPNG());
            RenderTexture.active = previous; Destroy(texture); RenderTexture.ReleaseTemporary(rt);
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
            yield return null;
        }
        
        IEnumerator Start()
        {
            Directory.CreateDirectory(output); Backup();
            
            // Unlock all content
            PlayerPrefs.SetInt("coached_runs", 6); // Unlock scooter
            PlayerPrefs.SetInt("dash_missions_done", 6);
            PlayerProfile.TotalPoints = Mathf.Max(30000, PlayerProfile.TotalPoints);
            PlayerProfile.Coins = 1000; // Enough for board
            
            // Set test parameters
            PlayerProfile.Character = Mathf.Clamp(character, 0, 2);
            DashRun.District = Mathf.Clamp(district, 0, 2);
            
            // Set vehicle
            PlayerProfile.Ride = vehicle == 1 ? 1 : 0;
            
            yield return new WaitForSeconds(1.5f);
            
            var menu = MenuUI.Instance;
            var gm = GameManager.Instance;
            var run = DashRun.Instance;
            var rider = FindFirstObjectByType<RiderController>();
            var track = FindFirstObjectByType<TrackSpawner>();
            
            Check(menu && gm && run && track && rider, "scene dependencies");
            
            // Open menu and capture setup
            menu.Open("none");
            track.RefreshDistrict();
            yield return CaptureScreenshot("menu");
            
            // Show crew selection
            menu.Open("crew");
            yield return CaptureScreenshot("crew");
            menu.Open("none");
            
            // Show shop (for board purchase if needed)
            menu.Open("shop");
            yield return CaptureScreenshot("shop");
            menu.Open("none");
            
            // Start run
            RiderInput.Override = () => RiderInput.Action.None;
            gm.StartRun();
            
            // Test ability based on character
            if (character == 0) // Ace - Speed boost
            {
                run.TryAbility();
                Check(run.Boost > 0f && run.Battery == 0f, "Ace boost uses battery");
                yield return new WaitForSeconds(1.3f);
                yield return CaptureScreenshot("ace-boost");
            }
            else if (character == 1) // Nova - Extended jump
            {
                run.TryAbility();
                Check(run.FloatTime > 0f && run.JumpGravityFactor < 1f, "Nova extended air time");
                yield return new WaitForSeconds(1.3f);
                yield return CaptureScreenshot("nova-ability");
            }
            else if (character == 2) // Bolt - Shield
            {
                run.TryAbility();
                Check(run.Shield && run.AbsorbHit() && !run.AbsorbHit(), "Bolt shield absorbs exactly one collision");
                yield return new WaitForSeconds(1.3f);
                yield return CaptureScreenshot("bolt-ability");
            }
            
            // Test vehicle
            if (vehicle == 1) // Board
            {
                run.ToggleBoard();
                Check(run.Board, "Board activated");
                yield return new WaitForSeconds(0.5f);
                yield return CaptureScreenshot("board-active");
            }
            
            // Test delivery dash
            gm.StartRun();
            Check(run.TryDeliveryDash(), "all crew can activate delivery dash");
            yield return new WaitForSeconds(0.5f);
            yield return CaptureScreenshot("delivery-dash");
            
            // Test a bit of gameplay
            RiderInput.Override = () =>
            {
                float d = gm.Distance;
                if (d < 50f) return RiderInput.Action.Jump;
                if (d < 100f) return RiderInput.Action.Right;
                if (d < 150f) return RiderInput.Action.Left;
                if (d < 200f) return RiderInput.Action.Jump;
                return RiderInput.Action.None;
            };
            
            yield return new WaitForSeconds(3f);
            yield return CaptureScreenshot("gameplay");
            
            // Finish
            gm.ToMenu();
            Restore();
            
            File.WriteAllText(Path.Combine(output, "comprehensive-pass.txt"), 
                $"PASS: {CharacterNames[character]} with {VehicleNames[vehicle]} in District {district}\n");
            Debug.Log($"[DashComprehensive] COMPLETE: {CharacterNames[character]} {VehicleNames[vehicle]} District {district}");
            
            yield return new WaitForSecondsRealtime(0.2f); Application.Quit(0);
        }
    }
}
