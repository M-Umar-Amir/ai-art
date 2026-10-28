using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.Gameplay
{
    /// Client showcase recording: launch with `-showcase <dir>`. Tours the menus, plays the full story intro,
    /// lets the autopilot ride a run, then a bust, with a caption strip explaining each part.
    /// Game time is locked to 30 fps and every frame is saved as a JPG, so the video plays smoothly
    /// however slowly the machine renders. The game's mixed audio is captured offline in step with those
    /// frames (AudioRenderer) and written to audio.wav beside them, ready to mux.
    public class Showcase : MonoBehaviour
    {
        const int Fps = 30;
        string dir;
        int frame;
        bool autopilot;
        TrackSpawner track;
        RiderController rider;
        Text head, sub;
        CanvasGroup strip, card;
        Text cardHead, cardSub;
        float stripTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-showcase") continue;
                new GameObject("Showcase").AddComponent<Showcase>().dir = args[i + 1];
                return;
            }
        }

        readonly List<float> audio = new();
        int channels;

        void Awake()
        {
            Time.captureFramerate = Fps;
            channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
            AudioRenderer.Start();
            MenuUI.ShowSkipHint = false;
            BuildOverlay();
        }

        void BuildOverlay()
        {
            var go = new GameObject("ShowcaseOverlay", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // Caption strip along the bottom edge.
            var band = UiKit.Panel(go.transform, "Strip", new Vector2(0f, -835f), new Vector2(1010f, 190f), new Color(0.05f, 0.07f, 0.2f, 0.88f));
            strip = band.gameObject.AddComponent<CanvasGroup>();
            UiKit.Panel(band.transform, "Accent", new Vector2(-485f, 0f), new Vector2(14f, 150f), UiKit.Yellow);
            head = UiKit.Label(band.transform, "", 58, new Vector2(20f, 36f), new Vector2(920f, 70f), UiKit.Yellow, TextAnchor.MiddleLeft);
            sub = UiKit.Label(band.transform, "", 36, new Vector2(20f, -38f), new Vector2(920f, 90f), UiKit.Cream, TextAnchor.MiddleLeft, false);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            strip.alpha = 0f;

            // Full-screen title card for the opening and closing.
            var shade = UiKit.Rect(go.transform, "Card", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            shade.color = new Color(0.05f, 0.07f, 0.2f, 0.92f);
            card = shade.gameObject.AddComponent<CanvasGroup>();
            cardHead = UiKit.Label(shade.transform, "", 120, new Vector2(0f, 80f), new Vector2(1000f, 300f), UiKit.Yellow);
            cardSub = UiKit.Label(shade.transform, "", 46, new Vector2(0f, -120f), new Vector2(900f, 300f), UiKit.Cream, TextAnchor.MiddleCenter, false);
            cardSub.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.alpha = 0f;
        }

        void Say(string h, string s, float seconds)
        {
            head.text = h;
            sub.text = s;
            stripTimer = seconds;
        }

        void Update()
        {
            float dt = 1f / Fps;
            stripTimer -= dt;
            strip.alpha = Mathf.MoveTowards(strip.alpha, stripTimer > 0f ? 1f : 0f, dt * 5f);
        }

        IEnumerator Card(string h, string s, float hold)
        {
            cardHead.text = h;
            cardSub.text = s;
            for (float t = 0f; t < 0.5f; t += 1f / Fps) { card.alpha = t / 0.5f; yield return null; }
            card.alpha = 1f;
            yield return new WaitForSeconds(hold);
            for (float t = 0.5f; t > 0f; t -= 1f / Fps) { card.alpha = t / 0.5f; yield return null; }
            card.alpha = 0f;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir, "*.jpg")) File.Delete(f);
            track = FindFirstObjectByType<TrackSpawner>();
            rider = FindFirstObjectByType<RiderController>();
            RiderInput.Override = () => autopilot ? Autopilot.Steer(track, rider) : RiderInput.Action.None;
            StartCoroutine(Record());
            TrackSpawner.LandmarkAhead += l => Say("DALLAS LANDMARKS", $"{l.displayName} on {l.street}: real Dallas streets and landmarks line the route.", 4f);

            var menu = MenuUI.Instance;
            menu.Open("none");
            card.alpha = 1f;
            cardHead.text = "FOOD INC GAME";
            cardSub.text = "Gameplay preview\nWork-in-progress build";
            yield return new WaitForSeconds(2.5f);
            for (float t = 0.5f; t > 0f; t -= 1f / Fps) { card.alpha = t / 0.5f; yield return null; }
            card.alpha = 0f;

            Say("MAIN MENU", "Play, Crew, Shop, Missions and the Daily Bonus. Coins and best score are saved on the device.", 3.5f);
            yield return new WaitForSeconds(4f);
            menu.Open("crew");
            Say("THE CREW", "A live 3D showroom: flip between Finn, Faye and Fizz-11. Fizz-11 unlocks at 25,000 points.", 3.5f);
            yield return new WaitForSeconds(2.2f);
            menu.FlipCrew(1);
            yield return new WaitForSeconds(1.6f);
            menu.FlipCrew(1);
            yield return new WaitForSeconds(1.6f);
            menu.Open("none");
            menu.Open("shop");
            Say("SHOP", "Spend coins to make power-ups last longer. The courier scooter is earned through missions, never sold.", 4f);
            yield return new WaitForSeconds(4.5f);
            menu.Open("none");
            menu.Open("missions");
            Say("MISSIONS", "Three goals at a time. Finish one for coins and a new one takes its place.", 3.5f);
            yield return new WaitForSeconds(4f);
            menu.Open("none");
            menu.Open("daily");
            Say("DAILY LOGIN BONUS", "A 7-day streak calendar. Rewards climb from 100 to 1,000 coins.", 3f);
            yield return new WaitForSeconds(3.5f);
            menu.Open("none");
            yield return new WaitForSeconds(0.6f);

            autopilot = true;
            // The intro carries its own scene titles, so the strip stays out of the way.
            StoryDirector.Instance.Play();
            while (GameManager.Instance.State != RunState.Running) yield return null;

            Say("THE RUN", "Swipe left and right to change lanes, up to jump, down to roll. Downtown Dallas at Christmas.", 4f);
            yield return new WaitForSeconds(6f);
            Say("COLLECT BURGERS", "Every burger is a coin. The score multiplier grows the further you ride.", 4f);
            yield return new WaitForSeconds(5f);
            PowerUps.Activate(PowerUp.Magnet);
            Say("POWER-UPS: MAGNET", "Burgers fly in from every lane.", 3.5f);
            yield return new WaitForSeconds(5f);
            PowerUps.Activate(PowerUp.Double);
            Say("POWER-UPS: DOUBLE SCORE", "Every point counts twice while the timer runs.", 3.5f);
            yield return new WaitForSeconds(4.5f);
            PowerUps.Activate(PowerUp.Rush);
            Say("POWER-UPS: SUGAR RUSH", "Faster, and obstacles go flying instead of slowing you down.", 4f);
            yield return new WaitForSeconds(6f);
            PowerUps.Clear();
            Say("THE CHASE", "Hit an obstacle and it topples, you lose speed, and the officer closes in. Hit again while he's close and you're busted.", 5f);
            yield return new WaitForSeconds(3f);

            // Let go of the bars: the next hits pull the officer in and end the run.
            autopilot = false;
            for (float t = 0f; GameManager.Instance.State == RunState.Running && t < 25f; t += Time.deltaTime) yield return null;
            Say("BUSTED", "A two-shot cutscene, then the score, coins earned and mission progress.", 5f);
            yield return new WaitForSeconds(6f);

            yield return Card("FOOD INC GAME", "Work-in-progress build\nNext: the Android build for phone testing.", 2.5f);
            card.alpha = 1f;
            yield return null;
            yield return null;
            AudioRenderer.Stop();
            WriteWav(Path.Combine(dir, "audio.wav"));
            Application.Quit();
        }

        /// 16-bit PCM WAV of everything AudioRenderer captured.
        void WriteWav(string path)
        {
            int rate = AudioSettings.outputSampleRate;
            using var w = new BinaryWriter(File.Create(path));
            int bytes = audio.Count * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)channels);
            w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (float f in audio) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(f * 32767f), -32768, 32767));
        }

        IEnumerator Record()
        {
            var wait = new WaitForEndOfFrame();
            while (true)
            {
                yield return wait;
                var tex = ScreenCapture.CaptureScreenshotAsTexture(2);
                File.WriteAllBytes(Path.Combine(dir, $"f{frame++:00000}.jpg"), tex.EncodeToJPG(92));
                Destroy(tex);
                // This frame's slice of the mix, so audio and video stay in step however slow the render.
                int n = AudioRenderer.GetSampleCountForCaptureFrame();
                if (n > 0)
                {
                    var buf = new NativeArray<float>(n * channels, Allocator.Temp);
                    AudioRenderer.Render(buf);
                    audio.AddRange(buf);
                    buf.Dispose();
                }
            }
        }
    }
}
