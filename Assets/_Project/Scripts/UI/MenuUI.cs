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
    public class MenuUI : MonoBehaviour
    {
        public static MenuUI Instance { get; private set; }
        public Font font;
        /// Finn, Faye, Fizz-11, in PlayerProfile.Character order. Their pack sheets are the crew portraits.
        public CharacterDef[] crew;

        GameObject title, daily, settings, pause, busted, pauseButton;
        Text coinsLabel, bestLabel, dailyBadge, bustedScore, bustedBest, bustedCoins, toast;
        Image[] dayTiles;
        Text[] dayLabels;
        Image[] dayIcons;
        Button claimButton;
        Image[] tierButtons;
        Text muteLabel;
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
            Toast($"MISSION COMPLETE!  +{m.Reward:N0}");
            if (Missions.Level == Missions.ScooterMissions) Caption("SCOOTER EARNED!", "Pick it in the Shop under Rides", 2.6f);
        }

        void OnLandmark(LandmarkDef l) => Caption(string.IsNullOrEmpty(l.street) ? l.displayName.ToUpper() : l.street.ToUpper(), l.displayName, 2.4f);

        void Start()
        {
            GameManager.Instance.StateChanged += OnState;
            Missions.Completed += OnMissionDone;
            TrackSpawner.NearMiss += OnNearMiss;
            TrackSpawner.LandmarkAhead += OnLandmark;
            GameManager.Instance.MultiplierUp += m => { if (m > 1) Popup($"x{m} MULTIPLIER!"); };
            OnState(GameManager.Instance.State);
            // First open of the day: show the calendar straight away.
            if (PlayerProfile.CanClaimDaily) ShowDaily();
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
            var root = canvasGo.transform;

            BuildTitle(root);
            BuildDaily(root);
            BuildSettings(root);
            BuildPause(root);
            BuildBusted(root);
            BuildCrew(root);
            BuildShop(root);
            BuildMissions(root);

            // Scene captions (Loading Up, Ran the Red, The Pursuit, street banners) and quick stunt popups.
            captionTitle = UiKit.Label(root, "", 118, new Vector2(0, 560), new Vector2(1000, 150), UiKit.Yellow);
            captionSub = UiKit.Label(root, "", 40, new Vector2(0, 450), new Vector2(1000, 60), Color.white);
            popup = UiKit.Label(root, "", 72, new Vector2(0, 250), new Vector2(900, 100), UiKit.Yellow);
            var skip = UiKit.Label(root, "Tap to skip", 30, new Vector2(0, -860), new Vector2(600, 50), new Color(1, 1, 1, 0.7f));
            skipHint = skip.gameObject;

            pauseButton = UiKit.Button(root, "", Vector2.zero, new Vector2(120, 120), UiKit.Blue, Pause, 52, "pause").gameObject;
            var prt = (RectTransform)pauseButton.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0, -110);

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

            // Top bar: wallet on the left, best score on the right.
            coinsLabel = UiKit.Pill(t, "coin", new Vector2(-300, 850), 380, UiKit.Yellow, out _);
            bestLabel = UiKit.Pill(t, "trophy", new Vector2(300, 850), 380, Color.white, out _);

            // Logo: big yellow wordmark, a tilted red ribbon, and the tagline; the whole lockup floats.
            var logo = UiKit.Centered(t, "Logo", new Vector2(0, 560), new Vector2(1000, 420));
            var lp = logo.gameObject.AddComponent<UiPulse>();
            lp.scale = 0.012f; lp.speed = 1.6f; lp.bob = 10f; lp.tilt = 1.2f;
            UiKit.Label(logo, "FOOD INC", 180, new Vector2(0, 80), new Vector2(1000, 200), UiKit.Yellow);
            var ribbon = UiKit.Panel(logo, "Ribbon", new Vector2(0, -60), new Vector2(440, 116), UiKit.Red);
            ribbon.sprite = UiKit.Gradient;
            ribbon.transform.localRotation = Quaternion.Euler(0, 0, -3f);
            UiKit.Label(ribbon.transform, "GAME", 92, new Vector2(0, 4), new Vector2(440, 116), Color.white);
            var tag = UiKit.Panel(logo, "Tagline", new Vector2(0, -172), new Vector2(1000, 64), new Color(0.05f, 0.08f, 0.25f, 0.78f));
            tag.raycastTarget = false;
            UiKit.Label(tag.transform, "Deliver across Dallas. Dodge the Christmas rush. Outride the officer.", 32, new Vector2(0, 2), new Vector2(980, 60), UiKit.Cream);

            var play = UiKit.Button(t, "PLAY", new Vector2(0, -360), new Vector2(660, 210), UiKit.Green, Play, 104, "play");
            play.transform.Find("Face").gameObject.AddComponent<UiPulse>(); // the root's scale belongs to ButtonBounce

            // Four icon tiles along the bottom; settings is the gear under the best-score pill.
            Tile(t, "CREW", "rider", new Vector2(-390, -640), UiKit.Yellow, ShowCrew);
            Tile(t, "SHOP", "cart", new Vector2(-130, -640), new Color(1f, 0.5f, 0.15f), ShowShop);
            Tile(t, "MISSIONS", "list", new Vector2(130, -640), UiKit.Blue, ShowMissions);
            var dailyBtn = Tile(t, "DAILY", "calendar", new Vector2(390, -640), UiKit.Red, ShowDaily);
            var badge = UiKit.Panel(dailyBtn.transform, "Badge", new Vector2(92, 96), new Vector2(76, 76), UiKit.Yellow);
            badge.sprite = UiKit.Gradient;
            UiKit.Label(badge.transform, "!", 56, new Vector2(0, 2), new Vector2(76, 76), Color.white);
            badge.gameObject.AddComponent<UiPulse>().scale = 0.12f;
            dailyBadge = badge.GetComponentInChildren<Text>();
            UiKit.Button(t, "", new Vector2(0, 850), new Vector2(100, 100), UiKit.Blue, () => ShowSettings(title), 40, "gear");
        }

        /// Square menu tile: big icon on top, a short word underneath.
        static Button Tile(Transform parent, string word, string icon, Vector2 pos, Color c, UnityEngine.Events.UnityAction onClick)
        {
            var b = UiKit.Button(parent, "", pos, new Vector2(236, 230), c, onClick);
            var face = b.transform.Find("Face");
            UiKit.Icon(face, icon, new Vector2(0, 28), 124);
            UiKit.Label(face, word, word.Length > 6 ? 32 : 40, new Vector2(0, -74), new Vector2(230, 50), Color.white);
            return b;
        }

        // ---------- Daily login bonus

        void BuildDaily(Transform root)
        {
            daily = UiKit.Shade(root, "Daily").gameObject;
            var p = UiKit.Card(daily.transform, new Vector2(0, 40), new Vector2(980, 1120)).transform;
            UiKit.Label(p, "DAILY BONUS", 92, new Vector2(0, 450), new Vector2(900, 120), UiKit.Yellow);
            UiKit.Label(p, "Ride every day to grow your streak. Miss a day and it starts over.", 32, new Vector2(0, 360), new Vector2(900, 60), UiKit.Cream);

            int n = PlayerProfile.DailyRewards.Length;
            dayTiles = new Image[n];
            dayLabels = new Text[n];
            dayIcons = new Image[n];
            for (int i = 0; i < n; i++)
            {
                // Days 1-6 in two rows of three, day 7 as a wide tile underneath.
                bool big = i == n - 1;
                Vector2 pos = big ? new Vector2(0, -230) : new Vector2(-290 + (i % 3) * 290, 190 - (i / 3) * 210);
                Vector2 size = big ? new Vector2(850, 190) : new Vector2(260, 190);
                dayTiles[i] = UiKit.Panel(p, "Day" + (i + 1), pos, size, UiKit.Blue);
                dayTiles[i].sprite = UiKit.Gradient;
                UiKit.Label(dayTiles[i].transform, "DAY " + (i + 1), 34, new Vector2(0, 58), new Vector2(240, 50), UiKit.Cream);
                dayIcons[i] = UiKit.Icon(dayTiles[i].transform, big ? "star" : "coin", new Vector2(big ? -150 : -70, -22), big ? 110 : 70);
                dayLabels[i] = UiKit.Label(dayTiles[i].transform, "", big ? 64 : 48, new Vector2(big ? 40 : 30, -22), new Vector2(240, 80), Color.white);
            }

            claimButton = UiKit.Button(p, "CLAIM", new Vector2(0, -420), new Vector2(520, 150), UiKit.Green, Claim, 72);
            UiKit.Button(p, "X", new Vector2(420, 480), new Vector2(110, 110), UiKit.Red, () => daily.SetActive(false), 56);
            daily.SetActive(false);
        }

        void ShowDaily()
        {
            RefreshDaily();
            daily.SetActive(true);
        }

        void Claim()
        {
            int got = PlayerProfile.ClaimDaily();
            if (got > 0) Toast($"+{got:N0} coins!");
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
                dayTiles[i].color = claimed ? UiKit.Green : isToday ? UiKit.Red : new Color(0.2f, 0.26f, 0.55f);
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
            var p = UiKit.Card(settings.transform, new Vector2(0, 40), new Vector2(940, 1000)).transform;
            UiKit.Label(p, "SETTINGS", 92, new Vector2(0, 400), new Vector2(900, 120), UiKit.Yellow);

            UiKit.Label(p, "GRAPHICS", 48, new Vector2(0, 260), new Vector2(800, 70), Color.white);
            tierButtons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                int tier = i;
                var b = UiKit.Button(p, GameSettings.TierNames[i].ToUpper(), new Vector2(-280 + i * 280, 150), new Vector2(250, 120), UiKit.Blue, () => SetTier(tier), 44);
                tierButtons[i] = b.transform.Find("Face").GetComponent<Image>();
            }
            UiKit.Label(p, "Low saves battery on older phones. High adds soft shadows and glow.", 28, new Vector2(0, 55), new Vector2(860, 50), UiKit.Cream);

            UiKit.Label(p, "VOLUME", 48, new Vector2(0, -60), new Vector2(800, 70), Color.white);
            volume = UiKit.Slider(p, new Vector2(-70, -160), 600, PlayerProfile.Volume, v =>
            {
                PlayerProfile.Volume = v;
                if (PlayerProfile.Muted && v > 0f) PlayerProfile.Muted = false;
                GameSettings.ApplyAudio();
                RefreshSettings();
            });
            var mute = UiKit.Button(p, "MUTE", new Vector2(330, -160), new Vector2(180, 100), UiKit.Red, ToggleMute, 40);
            muteLabel = mute.GetComponentInChildren<Text>();

            UiKit.Button(p, "DONE", new Vector2(0, -370), new Vector2(420, 140), UiKit.Green, CloseSettings, 64);
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

        void RefreshSettings()
        {
            for (int i = 0; i < tierButtons.Length; i++)
                tierButtons[i].color = i == PlayerProfile.Graphics ? UiKit.Yellow : new Color(0.2f, 0.26f, 0.55f);
            muteLabel.text = PlayerProfile.Muted ? "UNMUTE" : "MUTE";
            volume.SetValueWithoutNotify(PlayerProfile.Muted ? 0f : PlayerProfile.Volume);
        }

        // ---------- Pause

        void BuildPause(Transform root)
        {
            pause = UiKit.Shade(root, "Pause").gameObject;
            var p = UiKit.Card(pause.transform, new Vector2(0, 40), new Vector2(800, 760)).transform;
            UiKit.Label(p, "PAUSED", 96, new Vector2(0, 260), new Vector2(760, 130), UiKit.Yellow);
            UiKit.Button(p, "RESUME", new Vector2(0, 90), new Vector2(560, 150), UiKit.Green, Resume, 64, "play");
            UiKit.Button(p, "SETTINGS", new Vector2(0, -90), new Vector2(560, 130), UiKit.Blue, () => { pause.SetActive(false); ShowSettings(pause); }, 52, "gear");
            UiKit.Button(p, "QUIT RUN", new Vector2(0, -250), new Vector2(560, 130), UiKit.Red, QuitRun, 52);
            pause.SetActive(false);
        }

        void Pause()
        {
            if (GameManager.Instance.State != RunState.Running) return;
            GameManager.Instance.SetPaused(true);
            pause.SetActive(true);
        }

        void Resume()
        {
            pause.SetActive(false);
            settings.SetActive(false);
            GameManager.Instance.SetPaused(false);
        }

        void QuitRun()
        {
            pause.SetActive(false);
            GameManager.Instance.SetPaused(false);
            GameManager.Instance.ToMenu();
        }

        // ---------- Busted

        void BuildBusted(Transform root)
        {
            busted = UiKit.Shade(root, "Busted").gameObject;
            busted.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.15f, 0.35f);
            var p = UiKit.Card(busted.transform, new Vector2(0, -120), new Vector2(900, 900)).transform;
            UiKit.Label(p, "BUSTED!", 120, new Vector2(0, 330), new Vector2(860, 150), UiKit.Red);
            bustedScore = UiKit.Label(p, "", 96, new Vector2(0, 170), new Vector2(860, 120), Color.white);
            bustedBest = UiKit.Label(p, "", 44, new Vector2(0, 70), new Vector2(860, 60), UiKit.Cream);
            bustedCoins = UiKit.Label(p, "", 52, new Vector2(40, -20), new Vector2(760, 70), UiKit.Yellow);
            UiKit.Icon(p, "coin", new Vector2(-330, -20), 84);
UiKit.Button(p, "RIDE AGAIN", new Vector2(0, -180), new Vector2(640, 170), UiKit.Green, Play, 68, "play").transform.Find("Face").gameObject.AddComponent<UiPulse>();
            UiKit.Button(p, "MENU", new Vector2(0, -350), new Vector2(420, 120), UiKit.Blue, () => GameManager.Instance.ToMenu(), 52);
            var strip = UiKit.Panel(busted.transform, "Missions", new Vector2(0, -760), new Vector2(900, 250), new Color(0.05f, 0.08f, 0.25f, 0.8f));
            strip.raycastTarget = false;
            UiKit.Icon(strip.transform, "list", new Vector2(-380, 70), 70);
            UiKit.Label(strip.transform, "MISSIONS", 40, new Vector2(-200, 70), new Vector2(300, 60), UiKit.Yellow, TextAnchor.MiddleLeft);
            bustedMissions = UiKit.Label(strip.transform, "", 30, new Vector2(0, -30), new Vector2(820, 150), Color.white, TextAnchor.MiddleLeft);
            bustedMissions.horizontalOverflow = HorizontalWrapMode.Wrap;
            busted.SetActive(false);
        }

        // ---------- The Crew (scene 18): a showroom carousel. The chosen crew member turns on a lit pedestal
        // (live 3D, CrewStudio); arrows flip between them; locked ones show as a dark silhouette.

        CrewStudio studio;
        int crewIndex;
        RawImage crewPortrait;
        Text crewName, crewBlurb, crewButton, crewLockText;
        Image crewButtonFace, crewLock;
        Image[] crewDots;

        void BuildCrew(Transform root)
        {
            crewPanel = UiKit.Shade(root, "Crew").gameObject;
            var p = UiKit.Card(crewPanel.transform, new Vector2(0, 20), new Vector2(1000, 1640)).transform;
            UiKit.Label(p, "THE CREW", 96, new Vector2(0, 720), new Vector2(900, 120), UiKit.Yellow);

            // Pedestal: a soft spotlight pool behind, a glowing disc under the feet.
            var glow = UiKit.Centered(p, "Spotlight", new Vector2(0, 160), new Vector2(820, 900)).gameObject.AddComponent<Image>();
            glow.sprite = UiKit.Soft;
            glow.type = Image.Type.Sliced;
            glow.color = new Color(0.45f, 0.65f, 1f, 0.28f);
            glow.raycastTarget = false;
            var disc = UiKit.Panel(p, "Pedestal", new Vector2(0, -255), new Vector2(560, 120), new Color(0.06f, 0.1f, 0.3f, 0.9f));
            disc.raycastTarget = false;
            var discTop = UiKit.Panel(p, "Pedestal top", new Vector2(0, -238), new Vector2(520, 90), new Color(0.3f, 0.5f, 1f, 0.55f));
            discTop.raycastTarget = false;

            crewPortrait = UiKit.Centered(p, "Portrait", new Vector2(0, 175), new Vector2(720, 900)).gameObject.AddComponent<RawImage>();
            crewPortrait.raycastTarget = false;
            crewLock = UiKit.Icon(p, "lock", new Vector2(0, 200), 220);

            var left = UiKit.Button(p, "", new Vector2(-400, 180), new Vector2(130, 150), UiKit.Blue, () => FlipCrew(-1), 40, "play");
            left.transform.Find("Face/play Icon").localRotation = Quaternion.Euler(0f, 0f, 180f);
            UiKit.Button(p, "", new Vector2(400, 180), new Vector2(130, 150), UiKit.Blue, () => FlipCrew(1), 40, "play");

            int n = crew != null ? crew.Length : 0;
            crewDots = new Image[n];
            for (int i = 0; i < n; i++)
            {
                crewDots[i] = UiKit.Panel(p, "Dot", new Vector2((i - (n - 1) / 2f) * 56f, -350), new Vector2(34, 34), Color.white);
                crewDots[i].raycastTarget = false;
            }
            var nameRibbon = UiKit.Panel(p, "Name ribbon", new Vector2(0, -440), new Vector2(620, 116), UiKit.Red);
            nameRibbon.sprite = UiKit.Gradient;
            nameRibbon.raycastTarget = false;
            crewName = UiKit.Label(nameRibbon.transform, "", 80, new Vector2(0, 4), new Vector2(620, 116), Color.white);
            crewBlurb = UiKit.Label(p, "", 34, new Vector2(0, -560), new Vector2(900, 100), UiKit.Cream);
            var b = UiKit.Button(p, "SELECT", new Vector2(0, -700), new Vector2(560, 150), UiKit.Green, () => PickCrew(crewIndex), 60);
            crewButton = b.GetComponentInChildren<Text>();
            crewButtonFace = b.transform.Find("Face").GetComponent<Image>();
            crewLockText = crewButton;

            UiKit.Button(p, "X", new Vector2(440, 760), new Vector2(110, 110), UiKit.Red, CloseCrew, 56);
            crewPanel.SetActive(false);
        }

        static string Blurb(CharacterDef d) => d.id switch
        {
            "finn" => "Royal blue denim and a cream hat.\nThe Cluck & Co regular.",
            "faye" => "Forest green, red bandana,\nnever late twice.",
            _ => "Courier robot on one tire.\nRolls where bikes can't.",
        };

        static bool Unlocked(CharacterDef d) => d.unlock != Unlock.Points || PlayerProfile.TotalPoints >= d.unlockPoints;

        void ShowCrew()
        {
            if (!studio) studio = CrewStudio.Create(transform);
            crewIndex = PlayerProfile.Character;
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

        void PickCrew(int i)
        {
            if (!Unlocked(crew[i]))
            {
                Toast($"Score {crew[i].unlockPoints - PlayerProfile.TotalPoints:N0} more points to unlock");
                return;
            }
            PlayerProfile.Character = i;
            FindFirstObjectByType<RiderLook>()?.Apply(i, true);
            RefreshCrew();
        }

        void RefreshCrew()
        {
            if (crew == null || crew.Length == 0) return;
            var def = crew[crewIndex];
            bool open = Unlocked(def), sel = PlayerProfile.Character == crewIndex;
            if (studio) studio.Show(crewIndex);
            crewPortrait.color = open ? Color.white : new Color(0.08f, 0.1f, 0.25f, 1f);
            crewLock.gameObject.SetActive(!open);
            crewName.text = def.displayName.ToUpper();
            crewBlurb.text = open ? Blurb(def) : $"Score {def.unlockPoints:N0} points across your runs to unlock.\n{PlayerProfile.TotalPoints:N0} so far.";
            crewButton.text = sel ? "RIDING" : open ? "SELECT" : "LOCKED";
            crewButtonFace.color = sel ? Color.Lerp(UiKit.Yellow, Color.white, 0.12f) : open ? Color.Lerp(UiKit.Green, Color.white, 0.12f) : new Color(0.4f, 0.42f, 0.55f);
            for (int i = 0; i < crewDots.Length; i++)
                crewDots[i].color = i == crewIndex ? UiKit.Yellow : new Color(1f, 1f, 1f, 0.3f);
        }

        // ---------- Shop: power-up upgrades for coins, and the ride (the scooter is earned, never sold)

        void BuildShop(Transform root)
        {
            shopPanel = UiKit.Shade(root, "Shop").gameObject;
            var p = UiKit.Card(shopPanel.transform, new Vector2(0, 20), new Vector2(1000, 1640)).transform;
            UiKit.Label(p, "SHOP", 96, new Vector2(0, 720), new Vector2(600, 120), UiKit.Yellow);
            shopCoins = UiKit.Pill(p, "coin", new Vector2(-290, 590), 340, UiKit.Yellow, out _);
            UiKit.Label(p, "UPGRADES", 44, new Vector2(150, 590), new Vector2(400, 60), Color.white);

            upgradeButtons = new Text[PowerUps.Count];
            upgradeNames = new Text[PowerUps.Count];
            upgradePips = new Image[PowerUps.Count][];
            for (int i = 0; i < PowerUps.Count; i++)
            {
                int index = i;
                float y = 400 - i * 250;
                var row = UiKit.Panel(p, PowerUps.Names[i], new Vector2(0, y), new Vector2(920, 230), new Color(0.2f, 0.26f, 0.55f));
                UiKit.Icon(row.transform, PowerUps.IconKeys[i], new Vector2(-370, 10), 150);
                upgradeNames[i] = UiKit.Label(row.transform, PowerUps.Names[i], 42, new Vector2(-75, 70), new Vector2(400, 60), Color.white, TextAnchor.MiddleLeft);
                UiKit.Label(row.transform, PowerUps.Blurbs[i], 24, new Vector2(-75, 18), new Vector2(400, 40), UiKit.Cream, TextAnchor.MiddleLeft);
                upgradePips[i] = new Image[PowerUps.MaxLevel];
                for (int k = 0; k < PowerUps.MaxLevel; k++)
                {
                    upgradePips[i][k] = UiKit.Panel(row.transform, "Pip", new Vector2(-245 + k * 56, -50), new Vector2(46, 30), new Color(1f, 1f, 1f, 0.2f));
                    upgradePips[i][k].raycastTarget = false;
                }
                var b = UiKit.Button(row.transform, "", new Vector2(280, -10), new Vector2(300, 140), UiKit.Green, () => BuyUpgrade(index), 40);
                UiKit.Icon(b.transform.Find("Face"), "coin", new Vector2(-95, 4), 80);
                upgradeButtons[i] = UiKit.Label(b.transform.Find("Face"), "", 44, new Vector2(40, 4), new Vector2(200, 120), Color.white);
            }

            UiKit.Label(p, "RIDES", 44, new Vector2(0, -360), new Vector2(400, 60), Color.white);
            bikeCard = UiKit.Panel(p, "Bicycle", new Vector2(-230, -560), new Vector2(440, 320), new Color(0.2f, 0.26f, 0.55f));
            UiKit.Label(bikeCard.transform, "DELIVERY BICYCLE", 34, new Vector2(0, 110), new Vector2(420, 50), Color.white);
            UiKit.Label(bikeCard.transform, "The starter ride.", 26, new Vector2(0, 50), new Vector2(420, 40), UiKit.Cream);
            bikeButton = UiKit.Button(bikeCard.transform, "SELECT", new Vector2(0, -70), new Vector2(340, 120), UiKit.Green, () => PickRide(0), 40).GetComponentInChildren<Text>();
            scooterCard = UiKit.Panel(p, "Scooter", new Vector2(230, -560), new Vector2(440, 320), new Color(0.2f, 0.26f, 0.55f));
            UiKit.Label(scooterCard.transform, "COURIER SCOOTER", 34, new Vector2(0, 110), new Vector2(420, 50), Color.white);
            UiKit.Label(scooterCard.transform, $"Earned by finishing {Missions.ScooterMissions} missions.", 26, new Vector2(0, 50), new Vector2(420, 40), UiKit.Cream);
            scooterButton = UiKit.Button(scooterCard.transform, "SELECT", new Vector2(0, -70), new Vector2(340, 120), UiKit.Green, () => PickRide(1), 40).GetComponentInChildren<Text>();

            UiKit.Button(p, "X", new Vector2(440, 760), new Vector2(110, 110), UiKit.Red, () => shopPanel.SetActive(false), 56);
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
            if (PlayerProfile.Coins < cost)
            {
                Toast($"Need {cost - PlayerProfile.Coins:N0} more coins");
                return;
            }
            PlayerProfile.Coins -= cost;
            PlayerProfile.SetUpgradeLevel(i, lvl + 1);
            Toast($"{PowerUps.Names[i]} lasts {PowerUps.Duration((PowerUp)i):0.#}s!");
            RefreshShop();
        }

        void PickRide(int ride)
        {
            if (ride == 1 && !Missions.ScooterEarned)
            {
                Toast($"Finish {Missions.ScooterMissions - Missions.Level} more missions");
                return;
            }
            PlayerProfile.Ride = ride;
            FindFirstObjectByType<RiderLook>()?.Apply(PlayerProfile.Character, true);
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
            bool scooter = PlayerProfile.Ride == 1;
            bikeButton.text = scooter ? "SELECT" : "RIDING";
            bikeCard.color = scooter ? new Color(0.2f, 0.26f, 0.55f) : new Color(0.15f, 0.45f, 0.3f);
            scooterButton.text = scooter ? "RIDING" : Missions.ScooterEarned ? "SELECT" : $"{Mathf.Min(Missions.Level, Missions.ScooterMissions)}/{Missions.ScooterMissions} MISSIONS";
            scooterButton.fontSize = Missions.ScooterEarned ? 40 : 32;
            scooterCard.color = scooter ? new Color(0.15f, 0.45f, 0.3f) : new Color(0.2f, 0.26f, 0.55f);
            shopCoins.text = $"{PlayerProfile.Coins:N0}";
        }

        // ---------- Missions

        void BuildMissions(Transform root)
        {
            missionsPanel = UiKit.Shade(root, "Missions").gameObject;
            var p = UiKit.Card(missionsPanel.transform, new Vector2(0, 40), new Vector2(980, 1240)).transform;
            UiKit.Label(p, "MISSIONS", 96, new Vector2(0, 520), new Vector2(800, 120), UiKit.Yellow);
            UiKit.Label(p, "Finish a mission for coins. A new one takes its place.", 30, new Vector2(0, 430), new Vector2(900, 50), UiKit.Cream);
            missionText = new Text[Missions.Slots];
            missionCount = new Text[Missions.Slots];
            missionReward = new Text[Missions.Slots];
            missionBars = new RectTransform[Missions.Slots];
            for (int i = 0; i < Missions.Slots; i++)
            {
                float y = 250 - i * 250;
                var row = UiKit.Panel(p, "Mission" + i, new Vector2(0, y), new Vector2(900, 220), new Color(0.2f, 0.26f, 0.55f));
                row.raycastTarget = false;
                missionText[i] = UiKit.Label(row.transform, "", 36, new Vector2(-90, 50), new Vector2(660, 60), Color.white, TextAnchor.MiddleLeft);
                var track = UiKit.Panel(row.transform, "Track", new Vector2(-90, -30), new Vector2(660, 34), new Color(1f, 1f, 1f, 0.18f));
                track.raycastTarget = false;
                var fill = UiKit.Panel(track.transform, "Fill", new Vector2(-330, 0), new Vector2(660, 34), UiKit.Yellow);
                fill.raycastTarget = false;
                missionBars[i] = fill.rectTransform;
                missionBars[i].pivot = new Vector2(0f, 0.5f);
                missionCount[i] = UiKit.Label(row.transform, "", 28, new Vector2(-90, -78), new Vector2(660, 40), UiKit.Cream, TextAnchor.MiddleLeft);
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
                : $"Missions completed: {Missions.Level}   -   Courier scooter at {Missions.ScooterMissions}";
        }

        // ---------- Captions

        GameObject skipHint;
        /// Off for the showcase recording, where nobody can tap.
        public static bool ShowSkipHint = true;

        public void Caption(string head, string sub, float seconds)
        {
            captionTitle.text = head;
            captionSub.text = sub;
            captionTimer = seconds;
            captionAge = 0f;
        }

        void Popup(string msg)
        {
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
            pauseButton.SetActive(s == RunState.Running);
            busted.SetActive(false);
            pause.SetActive(false);
            skipHint.SetActive(s == RunState.Intro && ShowSkipHint);
            if (s == RunState.Ready || s == RunState.Busted) captionTimer = 0.01f;
            if (s == RunState.Busted)
            {
                StartCoroutine(ShowBustedAfter(1.4f));
                var gm = GameManager.Instance;
                bustedScore.text = gm.Score.ToString("N0");
                bustedBest.text = gm.NewBest ? "NEW BEST SCORE!" : $"Best {PlayerProfile.BestScore:N0}";
                bustedCoins.text = $"+{gm.CoinsEarned:N0} coins   ({gm.PickupCount} burgers)";
                var lines = new System.Text.StringBuilder();
                foreach (var m in Missions.Active) lines.AppendLine($"{m.Text}   {Mathf.Min(m.progress, m.target):N0}/{m.target:N0}");
                bustedMissions.text = lines.ToString().TrimEnd();
            }
            Refresh();
        }

        void Refresh()
        {
            if (!coinsLabel) return;
            coinsLabel.text = $"{PlayerProfile.Coins:N0}";
            bestLabel.text = $"{PlayerProfile.BestScore:N0}";
            dailyBadge.transform.parent.gameObject.SetActive(PlayerProfile.CanClaimDaily);
            if (shopCoins) shopCoins.text = $"{PlayerProfile.Coins:N0}";
            if (shopPanel && shopPanel.activeSelf) RefreshShop();
            if (missionsPanel && missionsPanel.activeSelf) RefreshMissions();
        }

        void Toast(string msg)
        {
            toast.text = msg;
            toastTimer = 2f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
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
            // Escape / Android back: pause during a run, close the top panel otherwise.
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (crewPanel.activeSelf) CloseCrew();
                else if (shopPanel.activeSelf) shopPanel.SetActive(false);
                else if (missionsPanel.activeSelf) missionsPanel.SetActive(false);
                else if (settings.activeSelf) CloseSettings();
                else if (daily.activeSelf) daily.SetActive(false);
                else if (pause.activeSelf) Resume();
                else Pause();
            }
        }

        /// For screenshot mode.
        public void Open(string panel)
        {
            if (panel == "daily") ShowDaily();
            if (panel == "settings") ShowSettings(title);
            if (panel == "crew") ShowCrew();
            if (panel == "shop") ShowShop();
            if (panel == "missions") ShowMissions();
            if (panel == "none")
            {
                daily.SetActive(false); settings.SetActive(false); CloseCrew();
                shopPanel.SetActive(false); missionsPanel.SetActive(false);
            }
        }
    }
}
