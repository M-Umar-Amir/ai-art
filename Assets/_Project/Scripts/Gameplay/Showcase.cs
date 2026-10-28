using System.Collections;
using System.IO;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.Gameplay
{
    /// Client showcase recording: launch with `-showcase <dir>`. Tours the menus, plays the full story intro,
    /// lets the autopilot ride a run, then a bust, with a caption strip explaining each part.
    /// Game time is locked to 30 fps and every frame is saved as a JPG, so the video plays smoothly
    /// however slowly the machine renders.
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

        void Awake()
        {
            Time.captureFramerate = Fps;
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

            Say("MAIN MENU", "Play, Crew, Daily Bonus and Settings. Coins and best score are saved on the device.", 3.5f);
            yield return new WaitForSeconds(4f);
            menu.Open("daily");
            Say("DAILY LOGIN BONUS", "A 7-day streak calendar. Rewards climb from 100 to 1,000 coins.", 3.5f);
            yield return new WaitForSeconds(4f);
            menu.Open("none");
            menu.Open("settings");
            Say("SETTINGS", "Graphics quality Low / Medium / High, a volume slider and mute.", 3.5f);
            yield return new WaitForSeconds(4f);
            menu.Open("none");
            menu.Open("crew");
            Say("THE CREW", "Pick a rider: Finn and Faye on bikes. Fizz-11 unlocks at 25,000 points.", 3.5f);
            yield return new WaitForSeconds(4f);
            menu.Open("none");
            yield return new WaitForSeconds(0.6f);

            autopilot = true;
            // The intro carries its own scene titles, so the strip stays out of the way.
            StoryDirector.Instance.Play();
            while (GameManager.Instance.State != RunState.Running) yield return null;

            Say("THE RUN", "Swipe left and right to change lanes, up to jump, down to roll.", 4f);
            yield return new WaitForSeconds(6f);
            Say("COLLECT BURGERS", "Every burger is a coin. The score multiplier grows the further you ride.", 4f);
            yield return new WaitForSeconds(6f);
            Say("NEAR MISSES", "Dodge close to an obstacle for bonus points and a slow-motion beat.", 4f);
            yield return new WaitForSeconds(8f);
            Say("THE CHASE", "Hit an obstacle and the officer closes in, siren blaring. Hit again while he's close and you're busted.", 5f);
            yield return new WaitForSeconds(4f);

            // Let go of the bars: the next hits pull the officer in and end the run.
            autopilot = false;
            for (float t = 0f; GameManager.Instance.State == RunState.Running && t < 25f; t += Time.deltaTime) yield return null;
            Say("BUSTED", "The run ends with a cutscene, then the score, coins earned and best score.", 5f);
            yield return new WaitForSeconds(5.5f);

            yield return Card("FOOD INC GAME", "Next up: graphics polish, performance and the Android build.", 2.5f);
            card.alpha = 1f;
            yield return null;
            yield return null;
            Application.Quit();
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
            }
        }
    }
}
