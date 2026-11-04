using System.Collections;
using DallasDash.Core;
using DallasDash.Data;
using DallasDash.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.UI
{
    /// All the screens around a run: title menu, daily login bonus, settings, pause, and the busted screen.
    /// Built at startup on its own canvas above the HUD.
    public partial class MenuUI : MonoBehaviour
    {
        public static MenuUI Instance { get; private set; }
        public Font font;
        public Sprite logoSprite;
        /// Finn, Faye, Fizz-11, in PlayerProfile.Character order. Their pack sheets are the crew portraits.
        public CharacterDef[] crew;

        GameObject title, daily, settings, pause, busted, pauseButton;
        Text coinsLabel, bestLabel, dailyBadge, bustedScore, bustedBest, bustedRun, bustedCoins, toast;
        Image[] dayTiles;
        Text[] dayLabels;
        Image[] dayIcons;
        Button claimButton;
        Image[] tierButtons;
        Text muteLabel, hapticsLabel, motionLabel;
        Slider sfxVolume;
        GameObject ranksPanel;
        BoardPeriod ranksPeriod = BoardPeriod.Today;
        Image[] ranksTabs;
        Text[] rankRows;
        Text ranksEmpty;
        Slider volume;
        GameObject settingsReturn, crewPanel;
        float toastTimer, captionTimer, captionAge, popupTimer;
        Text captionTitle, captionSub, popup;
        GameObject shopPanel, missionsPanel;
        Text shopCoins, missionsFooter, bustedMissions;
        Text[] upgradeButtons, upgradeNames;
        Image[][] upgradePips;
        Text bikeButton, scooterButton;
        Image bikeCard, scooterCard;
        Text[] missionText, missionCount, missionReward;
        RectTransform[] missionBars;

        void Awake()
        {
            Instance = this;
            UiKit.Font = font;
            GameSettings.ApplyAll();
            Build();
            PlayerProfile.Changed += Refresh;
        }

        void OnDestroy()
        {
            PlayerProfile.Changed -= Refresh;
            Missions.Completed -= OnMissionDone;
            TrackSpawner.NearMiss -= OnNearMiss;
            TrackSpawner.LandmarkAhead -= OnLandmark;
        }

        void OnNearMiss(TrackObject o) => Popup($"NEAR MISS! +{TrackSpawner.NearMissBonus}");

        /// A big centre-screen callout (power-up picked up).
        public void Shout(string msg) => Popup(msg);

        void OnMissionDone(Missions.Mission m)
        {
            Toast($"MISSION COMPLETE!  +{m.Reward:N0} BURGERS");
            if (Missions.Level == Missions.ScooterMissions) Caption("DELIVERY MILESTONE!", "Keep completing missions for burgers", 2.6f);
        }

        void OnLandmark(LandmarkDef l) => Caption(string.IsNullOrEmpty(l.street) ? l.displayName.ToUpper() : l.street.ToUpper(), l.displayName, 2.4f);

        void Start()
        {
            GameManager.Instance.StateChanged += OnState;
            Missions.Completed += OnMissionDone;
            TrackSpawner.NearMiss += OnNearMiss;
            TrackSpawner.LandmarkAhead += OnLandmark;
            GameManager.Instance.MultiplierUp += m => { if (m > 1) Popup($"x{m} MULTIPLIER!"); };
            GameManager.Instance.ComboMilestone += (c, pts) => Popup($"COMBO x{c}!  +{pts:N0}");
            OnState(GameManager.Instance.State);
            // First open of the day: show the calendar straight away, except on a first-ever launch, where nothing
            // should stand between a new player and PLAY (the DAILY tile still shows its badge).
            // Disabled auto-show to prevent getting stuck on daily bonus screen
            // if (PlayerProfile.CanClaimDaily && PlayerProfile.TotalPoints > 0 && (!cityHub || !cityHub.activeSelf)) ShowDaily();
        }

        void Build()
        {
            var canvasGo = new GameObject("Menus", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            // Everything sits inside the safe area (notches, punch-hole cameras, the home indicator).
            var root = SafeArea.Create(canvasGo.transform);

            BuildTitle(root);
            BuildDaily(root);
            BuildSettings(root);
            BuildPause(root);
            BuildBusted(root);
            BuildCrew(root);
            BuildShop(root);
            BuildMissions(root);
            BuildRanks(root);
            BuildDashMenus(root);
            BuildChaseOverlays(root);

            // Scene captions (Loading Up, Ran the Red, The Pursuit, street banners) and quick stunt popups.
            captionTitle = UiKit.Label(root, "", 118, new Vector2(0, 560), new Vector2(1000, 150), UiKit.Yellow);
            captionSub = UiKit.Label(root, "", 40, new Vector2(0, 450), new Vector2(1000, 60), Color.white);
            popup = UiKit.Label(root, "", 72, new Vector2(0, 250), new Vector2(900, 100), UiKit.Yellow);
            var skip = UiKit.Label(root, "Tap to skip", 30, new Vector2(0, -860), new Vector2(600, 50), new Color(1, 1, 1, 0.7f));
            skipHint = skip.gameObject;

            pauseButton = UiKit.Button(root, "", Vector2.zero, new Vector2(130, 130), UiKit.Blue, Pause, 56, "pause").gameObject;
            var prt = (RectTransform)pauseButton.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0, -100); // Moved up slightly to avoid overlap with top HUD elements

            toast = UiKit.Label(root, "", 64, new Vector2(0, 520), new Vector2(900, 120), UiKit.Yellow);
            Refresh();
        }

        // ---------- Title

        void BuildTitle(Transform root)
        {
            title = new GameObject("Title", typeof(RectTransform));
            title.transform.SetParent(root, false);
            var rt = (RectTransform)title.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            var t = title.transform;

            // Top bar: wallet on the left, best score on the right - enhanced with glow
            coinsLabel = UiKit.Pill(t, "burger", Vector2.zero, 400, UiKit.Yellow, out var coinsPill);
            bestLabel = UiKit.Pill(t, "trophy", Vector2.zero, 400, Color.white, out var bestPill);
            PinTop(coinsPill, -320f);
            PinTop(bestPill, 320f);
            titleKeep.Add(coinsPill.gameObject); titleKeep.Add(bestPill.gameObject);

            // Logo: big amber wordmark, a tilted purple ribbon, and the tagline; the whole lockup floats.
            var logo = UiKit.Centered(t, "Logo", new Vector2(0, 680), new Vector2(760, 300));
            var lp = logo.gameObject.AddComponent<UiPulse>();
            lp.scale = 0f; lp.speed = 1f; lp.bob = 2f; lp.tilt = 0f;
            UiKit.Label(logo, "DASH", 190, new Vector2(0, 85), new Vector2(1000, 210), UiKit.Yellow, TextAnchor.MiddleCenter, true);
            var ribbon = UiKit.Panel(logo, "Ribbon", new Vector2(0, -65), new Vector2(640, 126), UiKit.Purple);
            ribbon.sprite = UiKit.Gradient;
            ribbon.transform.localRotation = Quaternion.Euler(0, 0, -3f);
            UiKit.Label(ribbon.transform, "RIDERS", 98, new Vector2(0, 4), new Vector2(640, 126), Color.white, TextAnchor.MiddleCenter, true);
            // The fourth lane: a cyan lane line under the lockup, the same marking the express deck uses.
            // It lives on the title root so it also shows when the PDF wordmark sprite replaces the type.
            var lane = UiKit.Panel(t, "Lane line", new Vector2(0, 440), new Vector2(560, 10), UiKit.Cyan);
            lane.raycastTarget = false;
            lane.sprite = UiKit.Gradient;
            foreach (float x in new[] { -320f, 320f })
            {
                var dash = UiKit.Panel(t, "Lane dash", new Vector2(x, 440), new Vector2(70, 10), new Color(1f, 1f, 1f, 0.4f));
                dash.raycastTarget = false;
            }
            var tag = UiKit.Panel(logo, "Tagline", new Vector2(0, -185), new Vector2(1080, 70), new Color(0.05f, 0.08f, 0.25f, 0.85f));
            tag.raycastTarget = false;
            UiKit.Label(tag.transform, "Crave It. Chase It. Get It.", 34, new Vector2(0, 2), new Vector2(1060, 66), UiKit.Cream);
            if (logoSprite)
            {
                foreach (Transform child in logo) child.gameObject.SetActive(false);
                var image = UiKit.Centered(logo, "PDF wordmark", new Vector2(0, 30), new Vector2(560, 310)).gameObject.AddComponent<Image>();
                image.sprite = logoSprite; image.preserveAspect = true; image.raycastTarget = false;
                UiKit.Label(logo, "Crave It. Chase It. Get It.", 32, new Vector2(0, -145), new Vector2(760, 55), Color.white, TextAnchor.MiddleCenter, true);
            }

            var play = UiKit.Button(t, "START CHASE", new Vector2(0, -290), new Vector2(830, 130), UiKit.Pink, Play, 54, "play");
            play.transform.Find("Face").gameObject.AddComponent<UiPulse>(); // the root's scale belongs to ButtonBounce

            // Five icon tiles along the bottom; settings is the gear between the top pills.
            // The same five tiles serve the menu and free roam (one menu screen).
            foreach (var tile in new[] {
                Tile(t, "ACE", "rider", new Vector2(-440, TileRowY), UiKit.Yellow, ShowCrew),
                Tile(t, "SHOP", "cart", new Vector2(-220, TileRowY), UiKit.Orange, ShowShop),
                Tile(t, "MISSIONS", "list", new Vector2(0, TileRowY), UiKit.Blue, ShowMissions),
                Tile(t, "RANKS", "trophy", new Vector2(220, TileRowY), UiKit.Purple, ShowRanks) })
            { titleKeep.Add(tile.gameObject); titleTiles.Add(tile.gameObject); }
            var dailyBtn = Tile(t, "DAILY", "calendar", new Vector2(440, TileRowY), UiKit.Red, ShowDaily);
            titleKeep.Add(dailyBtn.gameObject); titleTiles.Add(dailyBtn.gameObject);
            var badge = UiKit.Panel(dailyBtn.transform, "Badge", new Vector2(82, 98), new Vector2(76, 76), UiKit.Yellow);
            badge.sprite = UiKit.Gradient;
            UiKit.Label(badge.transform, "!", 60, new Vector2(0, 2), new Vector2(82, 82), Color.white);
            badge.gameObject.AddComponent<UiPulse>().scale = 0.12f;
            dailyBadge = badge.GetComponentInChildren<Text>();
            var gear = UiKit.Button(t, "", Vector2.zero, new Vector2(110, 110), UiKit.Blue, () => ShowSettings(title), 44, "gear");
            // Sit the gear under the best-score pill, clear of the DASH wordmark, instead of on top of it.
            var gearRect = (RectTransform)gear.transform;
            gearRect.anchorMin = gearRect.anchorMax = new Vector2(1f, 1f);
            gearRect.anchoredPosition = new Vector2(-85f, -215f);
            titleKeep.Add(gear.gameObject);
        }

        /// Title elements shared by the menu and free roam, and the tile row that lifts while roaming.
        readonly System.Collections.Generic.HashSet<GameObject> titleKeep = new(), titleTiles = new();
        const float TileRowY = -700f;

        /// Hangs a top-bar element from the top edge of the safe area (centre-relative positions drift on tall phones).
        static void PinTop(RectTransform rt, float x)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, -110f);
        }

        /// Square menu tile: big icon on top, a short word underneath.
        static Button Tile(Transform parent, string word, string icon, Vector2 pos, Color c, UnityEngine.Events.UnityAction onClick)
        {
            var b = UiKit.Button(parent, "", pos, new Vector2(190, 150), UiKit.Blue, onClick);
            var face = b.transform.Find("Face");
            UiKit.Icon(face, icon, new Vector2(0, 22), 56);
            UiKit.Panel(face,"Accent",new Vector2(0,70),new Vector2(50,4),c).raycastTarget=false;
            UiKit.Label(face, word, 30, new Vector2(0, -42), new Vector2(180, 48), Color.white);
            return b;
        }

        // ---------- Daily login bonus

        void BuildDaily(Transform root)
        {
            daily = UiKit.Shade(root, "Daily").gameObject;
            var p = UiKit.FancyCard(daily.transform, new Vector2(0, 40), new Vector2(1020, 1180), UiKit.Gold).transform;
            UiKit.Header(p, "DAILY BONUS", 480, UiKit.Gold, 84);
            UiKit.Label(p, "Ride every day to grow your streak. Miss a day and it starts over.", 34, new Vector2(0, 352), new Vector2(940, 65), UiKit.Cream);

            int n = PlayerProfile.DailyRewards.Length;
            dayTiles = new Image[n];
            dayLabels = new Text[n];
            dayIcons = new Image[n];
            for (int i = 0; i < n; i++)
            {
                // Days 1-6 in two rows of three, day 7 as a wide tile underneath.
                bool big = i == n - 1;
                Vector2 pos = big ? new Vector2(0, -240) : new Vector2(-300 + (i % 3) * 300, 200 - (i / 3) * 220);
                Vector2 size = big ? new Vector2(880, 200) : new Vector2(275, 200);
                dayTiles[i] = UiKit.Panel(p, "Day" + (i + 1), pos, size, UiKit.Blue);
                dayTiles[i].sprite = UiKit.Gradient;
                UiKit.Label(dayTiles[i].transform, "DAY " + (i + 1), 36, new Vector2(0, 62), new Vector2(255, 55), UiKit.Cream);
                dayIcons[i] = UiKit.Icon(dayTiles[i].transform, big ? "star" : "burger", new Vector2(big ? -155 : -75, -24), big ? 115 : 75);
                dayLabels[i] = UiKit.Label(dayTiles[i].transform, "", big ? 68 : 52, new Vector2(big ? 42 : 32, -24), new Vector2(255, 85), Color.white);
            }

            claimButton = UiKit.Button(p, "CLAIM", new Vector2(0, -395), new Vector2(560, 140), UiKit.Green, Claim, 68);
            UiKit.Button(p, "X", new Vector2(440, 510), new Vector2(115, 115), UiKit.Red, CloseDaily, 60);
            // A permanent, obvious way out, so the bonus can never trap the player on this screen.
            UiKit.Button(p, "LATER", new Vector2(0, -520), new Vector2(380, 95), UiKit.Blue, CloseDaily, 40);
            // Tapping the dimmed area outside the card also closes it.
            var shade = daily.GetComponent<Image>();
            if (shade)
            {
                var dismiss = daily.AddComponent<Button>();
                dismiss.targetGraphic = shade;
                dismiss.transition = Selectable.Transition.None;
                dismiss.onClick.AddListener(CloseDaily);
            }
            daily.SetActive(false);
        }

        void ShowDaily()
        {
            RefreshDaily();
            daily.SetActive(true);
        }

        /// Closes the calendar from the X, LATER, Escape or a tap on the dimmed backdrop.
        void CloseDaily()
        {
            daily.SetActive(false);
        }

        void Claim()
        {
            int got = PlayerProfile.ClaimDaily();
            if (got > 0) Toast($"+{got:N0} burgers!");
            RefreshDaily();
        }

        void RefreshDaily()
        {
            int streak = PlayerProfile.DailyStreak;
            int today = PlayerProfile.DailyIndex;
            bool can = PlayerProfile.CanClaimDaily;
            for (int i = 0; i < dayTiles.Length; i++)
            {
                bool claimed = i < streak;
                bool isToday = i == today && can;
                dayTiles[i].color = claimed ? UiKit.Green : isToday ? UiKit.Red : new Color(0.075f, 0.11f, 0.2f);
                dayLabels[i].text = claimed ? "DONE" : $"{PlayerProfile.DailyRewards[i]:N0}";
                dayIcons[i].sprite = Icons.Get(claimed ? "check" : i == dayTiles.Length - 1 ? "star" : "coin");
                dayTiles[i].transform.localScale = isToday ? Vector3.one * 1.06f : Vector3.one;
            }
            claimButton.interactable = can;
            claimButton.GetComponentInChildren<Text>().text = can ? "CLAIM" : "COME BACK TOMORROW";
            claimButton.GetComponentInChildren<Text>().fontSize = can ? 72 : 40;
            Refresh();
        }

        // ---------- Settings

        void BuildSettings(Transform root)
        {
            settings = UiKit.Shade(root, "Settings").gameObject;
            var p = UiKit.FancyCard(settings.transform, new Vector2(0, 0), new Vector2(980, 1220), UiKit.Cyan).transform;
            UiKit.Header(p, "SETTINGS", 500, UiKit.Cyan, 84);

            UiKit.Label(p, "GRAPHICS", 52, new Vector2(0, 360), new Vector2(840, 75), Color.white);
            tierButtons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                int tier = i;
                var b = UiKit.Button(p, GameSettings.TierNames[i].ToUpper(), new Vector2(-290 + i * 290, 255), new Vector2(265, 115), UiKit.Blue, () => SetTier(tier), 46);
                tierButtons[i] = b.transform.Find("Face").GetComponent<Image>();
            }
            UiKit.Label(p, "Low saves battery. High adds more lighting detail and glow.", 30, new Vector2(0, 170), new Vector2(860, 65), UiKit.Muted);

            UiKit.Label(p, "MUSIC", 48, new Vector2(-310, 80), new Vector2(320, 65), Color.white, TextAnchor.MiddleLeft);
            volume = UiKit.Slider(p, new Vector2(-75, 20), 620, PlayerProfile.MusicVolume, v =>
            {
                PlayerProfile.MusicVolume = v;
                if (PlayerProfile.Muted && v > 0f) { PlayerProfile.Muted = false; GameSettings.ApplyAudio(); }
                RefreshSettings();
            });
            UiKit.Label(p, "EFFECTS", 48, new Vector2(-310, -70), new Vector2(320, 65), Color.white, TextAnchor.MiddleLeft);
            sfxVolume = UiKit.Slider(p, new Vector2(-75, -130), 620, PlayerProfile.SfxVolume, v =>
            {
                PlayerProfile.SfxVolume = v;
                if (PlayerProfile.Muted && v > 0f) { PlayerProfile.Muted = false; GameSettings.ApplyAudio(); }
                RefreshSettings();
            });
            var mute = UiKit.Button(p, "MUTE", new Vector2(345, -60), new Vector2(190, 105), UiKit.Red, ToggleMute, 42);
            muteLabel = mute.GetComponentInChildren<Text>();

            var vib = UiKit.Button(p, "VIBRATION: ON", new Vector2(-225, -305), new Vector2(430, 115), UiKit.Blue, ToggleHaptics, 40);
            hapticsLabel = vib.GetComponentInChildren<Text>();
            var motion = UiKit.Button(p, "MOTION: FULL", new Vector2(225, -305), new Vector2(430, 115), UiKit.Blue, ToggleMotion, 40);
            motionLabel = motion.GetComponentInChildren<Text>();

            UiKit.Button(p, "DONE", new Vector2(0, -485), new Vector2(440, 140), UiKit.Green, CloseSettings, 64);
            settings.SetActive(false);
        }

        void ShowSettings(GameObject from)
        {
            settingsReturn = from;
            RefreshSettings();
            settings.SetActive(true);
        }

        void CloseSettings()
        {
            settings.SetActive(false);
            if (settingsReturn) settingsReturn.SetActive(true);
        }

        void SetTier(int tier)
        {
            PlayerProfile.Graphics = tier;
            GameSettings.ApplyGraphics();
            RefreshSettings();
        }

        void ToggleMute()
        {
            PlayerProfile.Muted = !PlayerProfile.Muted;
            GameSettings.ApplyAudio();
            RefreshSettings();
        }

        void ToggleMotion()
        {
            PlayerProfile.ReduceMotion = !PlayerProfile.ReduceMotion;
            RefreshSettings();
        }

        void ToggleHaptics()
        {
            PlayerProfile.Haptics = !PlayerProfile.Haptics;
            Haptics.Play(Haptics.Strength.Medium);
            RefreshSettings();
        }

        void RefreshSettings()
        {
            hapticsLabel.text = PlayerProfile.Haptics ? "VIBRATION: ON" : "VIBRATION: OFF";
            motionLabel.text = PlayerProfile.ReduceMotion ? "MOTION: REDUCED" : "MOTION: FULL";
            sfxVolume.SetValueWithoutNotify(PlayerProfile.Muted ? 0f : PlayerProfile.SfxVolume);
            for (int i = 0; i < tierButtons.Length; i++)
                tierButtons[i].color = i == PlayerProfile.Graphics ? UiKit.Yellow : new Color(0.075f, 0.11f, 0.2f);
            muteLabel.text = PlayerProfile.Muted ? "UNMUTE" : "MUTE";
            volume.SetValueWithoutNotify(PlayerProfile.Muted ? 0f : PlayerProfile.MusicVolume);
        }

        // ---------- Pause

        void BuildPause(Transform root)
        {
            pause = UiKit.Shade(root, "Pause").gameObject;
            var p = UiKit.FancyCard(pause.transform, new Vector2(0, 40), new Vector2(840, 900), UiKit.Purple).transform;
            UiKit.Header(p, "PAUSED", 340, UiKit.Purple, 84);

            // Add run stats display
            var statsPanel = UiKit.Panel(p, "Stats", new Vector2(0, 190), new Vector2(740, 130), new Color(0.04f, 0.06f, 0.14f, 0.75f));
            statsPanel.raycastTarget = false;
            Text pauseScore = UiKit.Label(statsPanel.transform, "SCORE: 0", 38, new Vector2(-210, 4), new Vector2(320, 55), UiKit.Yellow);
            Text pauseDist = UiKit.Label(statsPanel.transform, "DISTANCE: 0m", 34, new Vector2(210, 4), new Vector2(320, 55), Color.white);
            pauseScore.name = "PauseScore";
            pauseDist.name = "PauseDist";

            UiKit.Button(p, "RESUME", new Vector2(0, 55), new Vector2(600, 160), UiKit.Green, Resume, 68, "play");
            UiKit.Button(p, "SETTINGS", new Vector2(0, -130), new Vector2(600, 140), UiKit.Blue, () => { pause.SetActive(false); ShowSettings(pause); }, 56, "gear");
            UiKit.Button(p, "QUIT RUN", new Vector2(0, -300), new Vector2(600, 140), UiKit.Red, QuitRun, 56);
            pause.SetActive(false);
        }

        Coroutine countdown;

        void Pause()
        {
            if (GameManager.Instance.State != RunState.Running) return;
            if (revivePanel && revivePanel.activeSelf) return; // the revive offer already holds the run
            if (countdown != null) { StopCoroutine(countdown); countdown = null; popup.text = ""; }
            GameManager.Instance.SetPaused(true);

            // Update pause stats
            var scoreText = pause.transform.Find("Card/Stats/PauseScore")?.GetComponent<Text>();
            var distText = pause.transform.Find("Card/Stats/PauseDist")?.GetComponent<Text>();
            if (scoreText) scoreText.text = $"SCORE: {GameManager.Instance.Score:N0}";
            if (distText) distText.text = $"DISTANCE: {GameManager.Instance.Distance:0}m";

            pause.SetActive(true);
        }

        /// Back into the run after a 3-2-1, so the rider isn't dropped straight into an obstacle.
        void Resume()
        {
            pause.SetActive(false);
            settings.SetActive(false);
            if (countdown != null) StopCoroutine(countdown);
            countdown = StartCoroutine(Countdown());
        }

        IEnumerator Countdown()
        {
            foreach (var n in new[] { "3", "2", "1" })
            {
                Popup(n);
                for (float t = 0f; t < 0.55f; t += Time.unscaledDeltaTime) yield return null;
            }
            Popup("GO!");
            countdown = null;
            GameManager.Instance.SetPaused(false);
        }

        // Phone call, notification shade, app switch: pause the run rather than let it play on unseen.
        // Not in the autopilot capture modes, where the window may never have focus.
        void OnApplicationPause(bool paused) { if (paused) AutoPause(); }
        void OnApplicationFocus(bool focused) { if (!focused) AutoPause(); }

        void AutoPause()
        {
            if (RiderInput.Override != null || !GameManager.Instance || GameManager.Instance.Paused && countdown == null) return;
            Pause();
        }

        void QuitRun()
        {
            pause.SetActive(false);
            GameManager.Instance.SetPaused(false);
            GameManager.Instance.ToMenu();
        }

        // ---------- Busted

        Text courseResult;
        void BuildBusted(Transform root)
        {
            busted = UiKit.Shade(root, "Busted").gameObject;
            busted.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.15f, 0.35f);
            var p = UiKit.FancyCard(busted.transform, new Vector2(0, -120), new Vector2(940, 950), UiKit.Red).transform;
            courseResult = UiKit.Label(p, "BUSTED!", 88, new Vector2(0, 350), new Vector2(900, 160), UiKit.Red, TextAnchor.MiddleCenter, true);
            bustedScore = UiKit.Label(p, "", 102, new Vector2(0, 180), new Vector2(900, 130), Color.white, TextAnchor.MiddleCenter, true);
            bustedBest = UiKit.Label(p, "", 48, new Vector2(0, 85), new Vector2(900, 65), UiKit.Cream);
            bustedRun = UiKit.Label(p, "", 40, new Vector2(0, 28), new Vector2(900, 55), UiKit.Yellow);
            bustedCoins = UiKit.Label(p, "", 56, new Vector2(42, -48), new Vector2(800, 75), UiKit.Yellow);
            UiKit.Icon(p, "burger", new Vector2(-350, -48), 90);
UiKit.Button(p, "DASH AGAIN", new Vector2(0, -190), new Vector2(680, 180), UiKit.Green, Play, 72, "play").transform.Find("Face").gameObject.AddComponent<UiPulse>();
            UiKit.Button(p, "MENU", new Vector2(0, -370), new Vector2(440, 130), UiKit.Blue, () => GameManager.Instance.ToMenu(), 56);
            var strip = UiKit.Panel(busted.transform, "Missions", new Vector2(0, -800), new Vector2(940, 270), new Color(0.05f, 0.08f, 0.25f, 0.85f));
            strip.raycastTarget = false;
            UiKit.Icon(strip.transform, "list", new Vector2(-400, 75), 75);
            UiKit.Label(strip.transform, "MISSIONS", 42, new Vector2(-210, 75), new Vector2(320, 65), UiKit.Yellow, TextAnchor.MiddleLeft);
            bustedMissions = UiKit.Label(strip.transform, "", 32, new Vector2(0, -32), new Vector2(860, 160), Color.white, TextAnchor.MiddleLeft);
            bustedMissions.horizontalOverflow = HorizontalWrapMode.Wrap;
            busted.SetActive(false);
        }

        // ---------- The Crew (scene 18): a showroom carousel. The chosen crew member turns on a lit pedestal
        // (live 3D, CrewStudio); arrows flip between them; locked ones show as a dark silhouette.

        CrewStudio studio;
        int crewIndex;
        RawImage crewPortrait;
        Text crewName, crewBlurb, crewButton, crewLockText;
        Text outfitButton;
        Image crewButtonFace, crewLock;
        Image[] crewDots;

        void BuildCrew(Transform root)
        {
            crewPanel = UiKit.Shade(root, "Crew").gameObject;
            var p = UiKit.FancyCard(crewPanel.transform, new Vector2(0, 20), new Vector2(1040, 1720), UiKit.Purple).transform;
            UiKit.Header(p, "ACE", 760, UiKit.Purple, 88);

            // Pedestal: a soft spotlight pool behind, a glowing disc under the feet.
            var glow = UiKit.Centered(p, "Spotlight", new Vector2(0, 170), new Vector2(860, 950)).gameObject.AddComponent<Image>();
            glow.sprite = UiKit.Disc;
            glow.type = Image.Type.Simple;
            glow.color = new Color(.25f, .45f, .7f, .18f);
            glow.raycastTarget = false;
            var disc = UiKit.Panel(p, "Pedestal", new Vector2(0, -270), new Vector2(590, 128), new Color(0.07f, 0.11f, 0.32f, 0.92f));
            disc.raycastTarget = false;
            disc.sprite = UiKit.Disc; disc.type = Image.Type.Simple;
            var discTop = UiKit.Panel(p, "Pedestal top", new Vector2(0, -252), new Vector2(550, 96), new Color(0.35f, 0.55f, 1f, 0.58f));
            discTop.raycastTarget = false;
            discTop.sprite = UiKit.Disc; discTop.type = Image.Type.Simple;

            crewPortrait = UiKit.Centered(p, "Portrait", new Vector2(0, 185), new Vector2(760, 950)).gameObject.AddComponent<RawImage>();
            crewPortrait.raycastTarget = false;
            crewLock = UiKit.Icon(p, "lock", new Vector2(0, 210), 230);

            // One rider (client, 4 Nov 2026): no carousel arrows or dots.
            int n = 0;
            crewDots = new Image[n];
            for (int i = 0; i < n; i++)
            {
                crewDots[i] = UiKit.Panel(p, "Dot", new Vector2((i - (n - 1) / 2f) * 60f, -370), new Vector2(38, 38), Color.white);
                crewDots[i].raycastTarget = false;
            }
            var nameRibbon = UiKit.Panel(p, "Name ribbon", new Vector2(0, -465), new Vector2(660, 102), UiKit.Surface);
            nameRibbon.sprite = UiKit.Rounded;
            nameRibbon.raycastTarget = false;
            crewName = UiKit.Label(nameRibbon.transform, "", 60, Vector2.zero, new Vector2(620, 85), Color.white);
            crewBlurb = UiKit.Label(p, "", 34, new Vector2(0, -575), new Vector2(940, 85), UiKit.Cream);
            outfitButton = UiKit.Button(p, "", new Vector2(0, -670), new Vector2(840, 108), UiKit.Blue, ToggleCrewOutfit, 32).GetComponentInChildren<Text>();
            var b = UiKit.Button(p, "SELECT", new Vector2(0, -790), new Vector2(590, 118), UiKit.Green, () => PickCrew(crewIndex), 54);
            crewButton = b.GetComponentInChildren<Text>();
            crewButtonFace = b.transform.Find("Face").GetComponent<Image>();
            crewLockText = crewButton;

            UiKit.Button(p, "X", new Vector2(460, 800), new Vector2(115, 115), UiKit.Red, CloseCrew, 60);
            crewPanel.SetActive(false);
        }

        static string Blurb(CharacterDef d) => "ACE / THE SPEEDSTER\nHero courier gear, hero e-bike. Ability: speed boost.";

        static bool Unlocked(CharacterDef d) => true;

        void ToggleCrewOutfit()
        {
            if (!Unlocked(crew[crewIndex])) { Toast("Unlock this crew member first"); return; }
            if (!DashInventory.ToggleOutfit(crewIndex)) Toast($"Need {BurgerPrices.Outfit - PlayerProfile.Burgers:N0} more burgers");
            RefreshCrew();
        }

        void ShowCrew()
        {
            if (!studio) studio = CrewStudio.Create(transform);
            crewIndex = 0;
            crewPanel.SetActive(true);
            if (studio)
            {
                crewPortrait.texture = studio.Texture;
                studio.SetLive(true);
            }
            RefreshCrew();
        }

        void CloseCrew()
        {
            crewPanel.SetActive(false);
            if (studio) studio.SetLive(false);
        }

        /// For screenshot mode and the arrow buttons.
        public void FlipCrew(int dir)
        {
            if (crew == null || crew.Length == 0) return;
            crewIndex = (crewIndex + dir + crew.Length) % crew.Length;
            RefreshCrew();
        }

        void PickCrew(int i) => CloseCrew();

        void RefreshCrew()
        {
            if (crew == null || crew.Length == 0) return;
            crewIndex = 0;
            var def = crew[0];
            bool open = Unlocked(def), sel = PlayerProfile.Character == crewIndex;
            if (studio) studio.Show(crewIndex);
            crewPortrait.color = open ? Color.white : new Color(0.08f, 0.1f, 0.25f, 1f);
            crewLock.gameObject.SetActive(!open);
            crewName.text = def.displayName.ToUpper();
            crewBlurb.text = Blurb(def);
            outfitButton.text = DashInventory.Outfit(crewIndex) == 1 ? "ION OUTFIT EQUIPPED / USE ORIGINAL"
                : DashInventory.OwnsOutfit(crewIndex) ? "EQUIP ION OUTFIT" : $"ION OUTFIT  /  {BurgerPrices.Outfit} BURGERS";
            crewButton.text = "READY TO RIDE";
            crewButtonFace.color = Color.Lerp(UiKit.Green, Color.white, 0.12f);
            for (int i = 0; i < crewDots.Length; i++)
                crewDots[i].color = i == crewIndex ? UiKit.Yellow : new Color(1f, 1f, 1f, 0.3f);
        }

        // ---------- Shop: power-up upgrades for burgers, and the ride (the scooter is earned, never sold)

        void BuildShop(Transform root)
        {
            shopPanel = UiKit.Shade(root, "Shop").gameObject;
            var p = UiKit.FancyCard(shopPanel.transform, new Vector2(0, 20), new Vector2(1040, 1720), UiKit.Orange).transform;
            UiKit.Header(p, "SHOP", 760, UiKit.Orange, 88, 640);
            shopCoins = UiKit.Pill(p, "burger", new Vector2(-300, 625), 360, UiKit.Yellow, out _);
            UiKit.Label(p, "UPGRADES", 48, new Vector2(160, 625), new Vector2(420, 65), Color.white);

            upgradeButtons = new Text[PowerUps.Count];
            upgradeNames = new Text[PowerUps.Count];
            upgradePips = new Image[PowerUps.Count][];
            for (int i = 0; i < PowerUps.Count; i++)
            {
                int index = i;
                float y = 455 - i * 155;
                var row = UiKit.Panel(p, PowerUps.Names[i], new Vector2(0, y), new Vector2(960, 145), new Color(0.085f, 0.12f, 0.22f));
                UiKit.Icon(row.transform, PowerUps.IconKeys[i], new Vector2(-400, 0), 90);
                upgradeNames[i] = UiKit.Label(row.transform, PowerUps.Names[i], 32, new Vector2(-80, 40), new Vector2(420, 65), Color.white, TextAnchor.MiddleLeft);
                UiKit.Label(row.transform, PowerUps.Blurbs[i], 27, new Vector2(-60, 0), new Vector2(460, 48), UiKit.Cream, TextAnchor.MiddleLeft);
                upgradePips[i] = new Image[PowerUps.MaxLevel];
                for (int k = 0; k < PowerUps.MaxLevel; k++)
                {
                    upgradePips[i][k] = UiKit.Panel(row.transform, "Pip", new Vector2(-260 + k * 50, -45), new Vector2(40, 20), new Color(1f, 1f, 1f, 0.22f));
                    upgradePips[i][k].raycastTarget = false;
                }
                var b = UiKit.Button(row.transform, "", new Vector2(305, 0), new Vector2(305, 105), UiKit.Green, () => BuyUpgrade(index), 42);
                UiKit.Icon(b.transform.Find("Face"), "burger", new Vector2(-100, 4), 85);
                upgradeButtons[i] = UiKit.Label(b.transform.Find("Face"), "", 46, new Vector2(42, 4), new Vector2(210, 125), Color.white);
            }

            UiKit.Label(p, "CREW EQUIPMENT", 38, new Vector2(0, -500), new Vector2(840, 65), Color.white);
            UiKit.Label(p, "ONE CURRENCY: BURGERS  ·  COLLECT THEM ON EVERY RUN", 32, new Vector2(0, -800), new Vector2(980, 42), UiKit.Cream, TextAnchor.MiddleCenter);
            bikeCard = UiKit.Panel(p, "Courier e-bike", new Vector2(-240, -645), new Vector2(460, 245), new Color(0.085f, 0.12f, 0.22f));
            UiKit.Label(bikeCard.transform, "COURIER E-BIKE", 36, new Vector2(0, 80), new Vector2(440, 55), Color.white);
            UiKit.Label(bikeCard.transform, "ACE + NOVA", 32, new Vector2(0, 22), new Vector2(440, 45), UiKit.Cream);
            bikeButton = UiKit.Label(bikeCard.transform, "AUTO EQUIPPED", 32, new Vector2(0, -65), new Vector2(440, 65), UiKit.Green);
            scooterCard = UiKit.Panel(p, "Robot tires", new Vector2(240, -645), new Vector2(460, 245), new Color(0.085f, 0.12f, 0.22f));
            UiKit.Label(scooterCard.transform, "MAGNETIC WHEEL", 36, new Vector2(0, 80), new Vector2(440, 55), Color.white);
            UiKit.Label(scooterCard.transform, "BOLT", 32, new Vector2(0, 22), new Vector2(440, 45), UiKit.Cream);
            scooterButton = UiKit.Label(scooterCard.transform, "NO EXTRA VEHICLE", 30, new Vector2(0, -65), new Vector2(440, 65), UiKit.Green);

            UiKit.Button(p, "X", new Vector2(460, 800), new Vector2(115, 115), UiKit.Red, () => shopPanel.SetActive(false), 60);
            shopPanel.SetActive(false);
        }

        void ShowShop()
        {
            RefreshShop();
            shopPanel.SetActive(true);
        }

        void BuyUpgrade(int i)
        {
            int lvl = PlayerProfile.UpgradeLevel(i);
            if (lvl >= PowerUps.MaxLevel) return;
            int cost = PowerUps.UpgradeCost[lvl];
            if (!PlayerProfile.TrySpend(cost))
            {
                Toast($"Need {cost - PlayerProfile.Burgers:N0} more burgers");
                return;
            }
            PlayerProfile.SetUpgradeLevel(i, lvl + 1);
            Toast($"{PowerUps.Names[i]} lasts {PowerUps.Duration((PowerUp)i):0.#}s!");
            RefreshShop();
        }

        void RefreshShop()
        {
            for (int i = 0; i < PowerUps.Count; i++)
            {
                int lvl = PlayerProfile.UpgradeLevel(i);
                for (int k = 0; k < PowerUps.MaxLevel; k++)
                    upgradePips[i][k].color = k < lvl ? PowerUps.Colors[i] : new Color(1f, 1f, 1f, 0.2f);
                upgradeButtons[i].text = lvl >= PowerUps.MaxLevel ? "MAX" : $"{PowerUps.UpgradeCost[lvl]:N0}";
                upgradeNames[i].text = $"{PowerUps.Names[i]}  {PowerUps.Duration((PowerUp)i):0.#}s";
            }
            bool robot = PlayerProfile.Character == 2;
            bikeCard.color = robot ? new Color(0.075f, 0.11f, 0.2f) : new Color(0.15f, 0.45f, 0.3f);
            scooterCard.color = robot ? new Color(0.15f, 0.45f, 0.3f) : new Color(0.075f, 0.11f, 0.2f);
            shopCoins.text = $"{PlayerProfile.Burgers:N0}";
        }

        // ---------- Missions

        void BuildMissions(Transform root)
        {
            missionsPanel = UiKit.Shade(root, "Missions").gameObject;
            var p = UiKit.Card(missionsPanel.transform, new Vector2(0, 40), new Vector2(980, 1240)).transform;
            UiKit.Header(p, "MISSIONS", 520, UiKit.Pink, 84, 800);
            UiKit.Label(p, "Finish a mission for energy chips. A new one takes its place.", 30, new Vector2(0, 408), new Vector2(900, 50), UiKit.Cream);
            missionText = new Text[Missions.Slots];
            missionCount = new Text[Missions.Slots];
            missionReward = new Text[Missions.Slots];
            missionBars = new RectTransform[Missions.Slots];
            for (int i = 0; i < Missions.Slots; i++)
            {
                float y = 250 - i * 250;
                var row = UiKit.Panel(p, "Mission" + i, new Vector2(0, y), new Vector2(900, 220), new Color(0.075f, 0.11f, 0.2f));
                row.raycastTarget = false;
                missionText[i] = UiKit.Label(row.transform, "", 36, new Vector2(-90, 50), new Vector2(660, 60), Color.white, TextAnchor.MiddleLeft);
                var track = UiKit.Panel(row.transform, "Track", new Vector2(-90, -30), new Vector2(660, 34), new Color(1f, 1f, 1f, 0.18f));
                track.raycastTarget = false;
                var fill = UiKit.Panel(track.transform, "Fill", new Vector2(-330, 0), new Vector2(660, 34), UiKit.Yellow);
                fill.raycastTarget = false;
                missionBars[i] = fill.rectTransform;
                missionBars[i].pivot = new Vector2(0f, 0.5f);
                missionCount[i] = UiKit.Label(row.transform, "", 30, new Vector2(-90, -78), new Vector2(660, 40), UiKit.Cream, TextAnchor.MiddleLeft);
                UiKit.Icon(row.transform, "coin", new Vector2(330, 30), 90);
                missionReward[i] = UiKit.Label(row.transform, "", 40, new Vector2(330, -48), new Vector2(200, 50), UiKit.Yellow);
            }
            missionsFooter = UiKit.Label(p, "", 32, new Vector2(0, -470), new Vector2(900, 60), UiKit.Cream);
            UiKit.Button(p, "X", new Vector2(430, 560), new Vector2(110, 110), UiKit.Red, () => missionsPanel.SetActive(false), 56);
            missionsPanel.SetActive(false);
        }

        void ShowMissions()
        {
            RefreshMissions();
            missionsPanel.SetActive(true);
        }

        void RefreshMissions()
        {
            var list = Missions.Active;
            for (int i = 0; i < Missions.Slots; i++)
            {
                var m = list[i];
                missionText[i].text = m.Text;
                missionBars[i].localScale = new Vector3(Mathf.Clamp01(m.progress / (float)m.target), 1f, 1f);
                missionCount[i].text = $"{Mathf.Min(m.progress, m.target):N0} / {m.target:N0}" + (m.PerRun ? "   best run" : "");
                missionReward[i].text = $"+{m.Reward:N0}";
            }
            missionsFooter.text = Missions.ScooterEarned
                ? $"Missions completed: {Missions.Level}"
                : $"Missions completed: {Missions.Level}   -   Keep delivering for rewards";
        }

        // ---------- Ranks (leaderboard)

        const int RankRows = 10;

        void BuildRanks(Transform root)
        {
            ranksPanel = UiKit.Shade(root, "Ranks").gameObject;
            var p = UiKit.Card(ranksPanel.transform, new Vector2(0, 40), new Vector2(980, 1300)).transform;
            UiKit.Header(p, "RANKS", 550, UiKit.Cyan, 84, 800);
            UiKit.Label(p, "Your best runs on this device", 28, new Vector2(0, 445), new Vector2(840, 50), UiKit.Muted);
            ranksTabs = new Image[3];
            string[] names = { "TODAY", "THIS WEEK", "ALL TIME" };
            for (int i = 0; i < 3; i++)
            {
                var period = (BoardPeriod)i;
                var b = UiKit.Button(p, names[i], new Vector2(-300 + i * 300, 345), new Vector2(270, 100), UiKit.Blue, () => { ranksPeriod = period; RefreshRanks(); }, 36);
                ranksTabs[i] = b.transform.Find("Face").GetComponent<Image>();
            }
            rankRows = new Text[RankRows];
            for (int i = 0; i < RankRows; i++)
            {
                float y = 250 - i * 82;
                var row = UiKit.Panel(p, "Row" + i, new Vector2(0, y), new Vector2(880, 76), UiKit.Surface);
                row.raycastTarget = false;
                rankRows[i] = UiKit.Label(row.transform, "", 32, new Vector2(10, 0), new Vector2(840, 70), Color.white, TextAnchor.MiddleLeft, false);
                rankRows[i].supportRichText = true;
            }
            ranksEmpty = UiKit.Label(p, "No runs here yet. Ride to set a score!", 36, new Vector2(0, 0), new Vector2(880, 80), UiKit.Cream);
            UiKit.Button(p, "X", new Vector2(430, 590), new Vector2(110, 110), UiKit.Red, () => ranksPanel.SetActive(false), 56);
            ranksPanel.SetActive(false);
        }

        void ShowRanks()
        {
            ranksPanel.SetActive(true);
            RefreshRanks();
        }

        void RefreshRanks()
        {
            for (int i = 0; i < ranksTabs.Length; i++)
                ranksTabs[i].color = i == (int)ranksPeriod ? UiKit.Yellow : new Color(0.075f, 0.11f, 0.2f);
            var runs = Leaderboard.Top(ranksPeriod, RankRows);
            for (int i = 0; i < RankRows; i++)
            {
                rankRows[i].transform.parent.gameObject.SetActive(i < runs.Count);
                if (i >= runs.Count) continue;
                var r = runs[i];
                string medal = i == 0 ? "<color=#FFCC26>" : i == 1 ? "<color=#E0E6F0>" : i == 2 ? "<color=#E39A5B>" : "<color=#FFFFFF>";
                string when = ranksPeriod == BoardPeriod.Today ? r.When.ToLocalTime().ToString("HH:mm") : r.When.ToLocalTime().ToString("d MMM");
                rankRows[i].text = $"{medal}#{i + 1}</color>   <color=#FFFFFF>{r.score:N0} pts</color>\n<size=23><color=#AEBFD9>{r.distance:N0} m  /  {r.burgers} collected  /  x{r.bestCombo}  /  {when}</color></size>";
            }
            ranksEmpty.gameObject.SetActive(runs.Count == 0);
        }

        // ---------- Captions

        GameObject skipHint;
        /// Off for the showcase recording, where nobody can tap.
        public static bool ShowSkipHint = true;

        public void Caption(string head, string sub, float seconds)
        {
            captionTitle.fontSize = Mathf.Clamp((int)(1450f / Mathf.Max(1, head.Length)), 44, 100);
            captionTitle.text = head;
            captionSub.text = sub;
            captionTimer = seconds;
            captionAge = 0f;
        }

        void Popup(string msg)
        {
            popup.fontSize = Mathf.Clamp((int)(1350f / Mathf.Max(1, msg.Length)), 38, 72);
            popup.text = msg;
            popupTimer = 0.9f;
        }

        // ---------- Flow

        void Play()
        {
            daily.SetActive(false);
            CloseCrew();
            shopPanel.SetActive(false);
            missionsPanel.SetActive(false);
            ranksPanel.SetActive(false);
            rewardsPanel.SetActive(false); districtPanel.SetActive(false);
            if (StoryDirector.Instance) StoryDirector.Instance.Play();
            else GameManager.Instance.StartRun();
        }

        IEnumerator ShowBustedAfter(float seconds)
        {
            // Let the Busted two-shot play before the card covers it.
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
            if (GameManager.Instance.State == RunState.Busted) busted.SetActive(true);
        }

        void OnState(RunState s)
        {
            title.SetActive(s == RunState.Ready);
            // The Dash Riders menu is the home screen; free roam is a mode inside it, entered by choice.
            if (s != RunState.Ready && cityHub && cityHub.activeSelf) ExitCity();
            pauseButton.SetActive(s == RunState.Running);
            busted.SetActive(false);
            pause.SetActive(false);
            if (s != RunState.Running && revivePanel) revivePanel.SetActive(false);
            if (s != RunState.Running && couponPanel) { couponPanel.SetActive(false); couponTime = 0f; }
            skipHint.SetActive(s == RunState.Intro && ShowSkipHint);
            if (s == RunState.Ready || s == RunState.Busted) captionTimer = 0.01f;
            if (s == RunState.Running) { rewardsPanel.SetActive(false); districtPanel.SetActive(false); }
            if (s == RunState.Running) coachedHop = coachedSteer = false;
            if (s == RunState.Busted) PlayerPrefs.SetInt("coached_runs", PlayerPrefs.GetInt("coached_runs", 0) + 1);
            if (s == RunState.Busted)
            {
                StartCoroutine(ShowBustedAfter(1.4f));
                var gm = GameManager.Instance;
                bustedScore.text = gm.Score.ToString("N0");
                string best = gm.NewBest ? "NEW BEST SCORE!" : $"Best {PlayerProfile.BestScore:N0}";
                var r = gm.LastRanks;
                bustedBest.text = r.today <= 3 ? $"{best}   |   #{r.today} today" : best;
                bustedCoins.text = $"+{gm.BurgersEarned:N0} burgers   /   coupon {Mathf.Min(PlayerProfile.CouponProgress, BurgerPrices.CouponThreshold):N0} of {BurgerPrices.CouponThreshold:N0}";
                var dash = DashRun.Instance;
                courseResult.text = string.IsNullOrEmpty(DashRun.EndReason) ? "RUN OVER" : DashRun.EndReason;
                courseResult.color = UiKit.Red;
                bustedRun.text = dash ? $"{gm.Distance:N0} m   ·   {dash.Shortcuts} skypark shortcuts   ·   {gm.NearMisses} near misses   ·   {dash.Revives} revives" : "";
                var lines = new System.Text.StringBuilder();
                foreach (var m in Missions.Active) lines.AppendLine($"{m.Text}   {Mathf.Min(m.progress, m.target):N0}/{m.target:N0}");
                bustedMissions.text = lines.ToString().TrimEnd();
            }
            Refresh();
        }

        void Refresh()
        {
            if (!coinsLabel) return;
            coinsLabel.text = $"{PlayerProfile.Burgers:N0}";
            bestLabel.text = $"{PlayerProfile.BestScore:N0}";
            dailyBadge.transform.parent.gameObject.SetActive(PlayerProfile.CanClaimDaily);
            if (shopCoins) shopCoins.text = $"{PlayerProfile.Burgers:N0}";
            if (shopPanel && shopPanel.activeSelf) RefreshShop();
            if (missionsPanel && missionsPanel.activeSelf) RefreshMissions();
            RefreshCouponMeter();
        }

        void Toast(string msg)
        {
            toast.fontSize = Mathf.Clamp((int)(1350f / Mathf.Max(1, msg.Length)), 30, 64);
            toast.text = msg;
            toastTimer = 2f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (revivePanel && revivePanel.activeSelf) DeclineRevive();
                else if (settings.activeSelf) CloseSettings();
                else if (rewardsPanel.activeSelf) { DemoRewards.Save(); rewardsPanel.SetActive(false); }
                else if (districtPanel.activeSelf) districtPanel.SetActive(false);
                else if (crewPanel.activeSelf) CloseCrew();
                else if (shopPanel.activeSelf) shopPanel.SetActive(false);
                else if (missionsPanel.activeSelf) missionsPanel.SetActive(false);
                else if (ranksPanel.activeSelf) ranksPanel.SetActive(false);
                else if (daily.activeSelf) daily.SetActive(false);
                else if (cityHub && cityHub.activeSelf) ExitCity();
                else if (pause.activeSelf) Resume();
                else Pause();
                return;
            }
            TickDashMenus(dt);
            TickChaseOverlays(dt);
            if (captionTimer > 0f)
            {
                captionTimer -= dt;
                captionAge += dt;
                float punch = Mathf.Clamp01(captionTimer * 4f); // fade out over the last quarter second
                captionTitle.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.15f - captionAge) * 3f);
                captionTitle.color = new Color(UiKit.Yellow.r, UiKit.Yellow.g, UiKit.Yellow.b, punch);
                captionSub.color = new Color(1f, 1f, 1f, punch);
                if (captionTimer <= 0f) captionTitle.text = captionSub.text = "";
            }
            if (popupTimer > 0f)
            {
                popupTimer -= dt;
                popup.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, popupTimer - 0.7f) * 2.5f);
                if (popupTimer <= 0f) popup.text = "";
            }
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                toast.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, toastTimer - 1.7f) * 1.5f);
                if (toastTimer <= 0f) toast.text = "";
            }
            Coach();
        }

        // ---------- First-run coaching: the first two runs name the move the first time each kind of hurdle
        // comes up in the rider's lane (hop the low ones, steer round the ones that fill the lane).

        TrackSpawner coachTrack;
        RiderController coachRider;
        bool coachedHop, coachedSteer;

        void Coach()
        {
            var gm = GameManager.Instance;
            if (gm.State != RunState.Running || gm.Paused || PlayerPrefs.GetInt("coached_runs", 0) >= 2) return;
            if (coachedHop && coachedSteer) return;
            if (!coachTrack) coachTrack = FindFirstObjectByType<TrackSpawner>();
            if (!coachRider) coachRider = FindFirstObjectByType<RiderController>();
            if (!coachTrack || !coachRider) return;
            foreach (var o in coachTrack.Live)
            {
                if (!o || o.consumed || o.kind == TrackKind.Pickup || o.lane != coachRider.Lane) continue;
                float ahead = o.transform.position.z - gm.Distance;
                if (ahead < 6f || ahead > 26f) continue;
                if (o.kind == TrackKind.Low && !coachedHop) { coachedHop = true; Caption("SWIPE UP", "Hop over cones and barriers", 1.6f); }
                else if (o.kind == TrackKind.Block && !coachedSteer) { coachedSteer = true; Caption("SWIPE LEFT OR RIGHT", "Steer round roadworks, vans and riders", 1.8f); }
                return;
            }
        }

        /// For screenshot mode.
        public void Open(string panel)
        {
            if (panel == "districts") ShowDistricts();
            if (panel == "rewards") ShowRewards();
            if (panel == "daily") ShowDaily();
            if (panel == "settings") ShowSettings(title);
            if (panel == "crew") ShowCrew();
            if (panel == "shop") ShowShop();
            if (panel == "missions") ShowMissions();
            if (panel == "ranks") ShowRanks();
            if (panel == "pause") Pause();
            if (panel == "pause-settings") ShowSettings(pause);
            if (panel == "city") EnterCity();
            if (panel == "none")
            {
                daily.SetActive(false); settings.SetActive(false); CloseCrew();
                rewardsPanel.SetActive(false); districtPanel.SetActive(false);
                shopPanel.SetActive(false); missionsPanel.SetActive(false); ranksPanel.SetActive(false);
                if (cityHub && cityHub.activeSelf) ExitCity();
            }
        }
    }
}
