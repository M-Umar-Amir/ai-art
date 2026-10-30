using System;
using UnityEngine;

namespace DallasDash.Core
{
    /// Ready: title menu. Intro: the Loading Up / Ran the Red / Pursuit scenes before the run. Busted: run over.
    public enum RunState { Ready, Intro, Running, Busted }

    /// Owns the run: state, distance, speed, score. Busted ends the run but points earned are kept.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState State { get; private set; } = RunState.Ready;
        public float Distance { get; private set; }
        public float Speed { get; private set; } = Tuning.SpeedBase;
        public int Score { get; private set; }
        public int Multiplier { get; private set; } = 1;
        public int PickupCount { get; private set; }
        public int EnergyCollected { get; private set; }
        public bool Paused { get; private set; }
        /// Set when a run ends: coins paid out (one per burger) and whether the score beat the saved best.
        public int CoinsEarned { get; private set; }
        public bool NewBest { get; private set; }
        /// Where the run just ended placed on the Today / This week / All time boards.
        public (int today, int week, int all) LastRanks { get; private set; }
        /// Frame the run started on, so the input that started it isn't also read as a move.
        public int StartFrame { get; private set; } = -1;

        public event Action<RunState> StateChanged;
        public event Action<int> MultiplierUp;

        // ---- Burger combo: pickups in quick succession build a streak; a hit or a gap ends it.
        /// Burgers in the current streak, and the longest streak this run.
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        /// 1 just after a pickup, draining to 0 when the streak runs out.
        public float ComboFraction => Combo > 0 ? Mathf.Clamp01(comboTimer / ComboWindow) : 0f;
        /// Every ComboStep burgers in a row pays ComboBonus points per step reached (x the score factor).
        public const int ComboStep = 10, ComboBonus = 25;
        const float ComboWindow = 1.1f;
        float comboTimer;
        /// (combo reached, points paid) at each milestone.
        public event Action<int, int> ComboMilestone;

        /// Seconds of riding this run (excludes pauses), and near misses pulled off: for the leaderboard.
        public float RunTime { get; private set; }
        public int NearMisses { get; private set; }

        /// A hit ends the streak.
        public void BreakCombo() { Combo = 0; comboTimer = 0f; DashRun.Instance?.BreakTricks(); }

        float scoreCarry;
        /// 1 right after a head-on hit, easing back to 0: the rider is slowed while it lasts.
        float bonk;

        /// Ran into something head on: the rider loses most of their speed and builds it back up.
        public void Bonk() => bonk = 1f;

        void Awake() => Instance = this;

        public void StartRun()
        {
            FindFirstObjectByType<DallasDash.Gameplay.RiderLook>()?.Apply(PlayerProfile.Character, true);
            Distance = 0f;
            Speed = Tuning.SpeedBase;
            Score = 0;
            PickupCount = EnergyCollected = 0;
            Multiplier = 1;
            scoreCarry = 0f;
            bonk = 0f;
            Combo = BestCombo = NearMisses = 0;
            comboTimer = RunTime = 0f;
            StartFrame = Time.frameCount;
            PowerUps.Clear();
            Missions.BeginRun();
            SetPaused(false);
            SetState(RunState.Running);
        }

        public void Bust()
        {
            if (State != RunState.Running) return;
            PowerUps.Clear();
            Missions.Save();
            CoinsEarned = EnergyCollected;
            PlayerProfile.TotalPoints += Score;
            NewBest = Score > PlayerProfile.BestScore;
            if (NewBest) PlayerProfile.BestScore = Score;
            PlayerProfile.Coins += CoinsEarned;
            LastRanks = Leaderboard.Record(this);
            Haptics.Play(Haptics.Strength.Heavy);
            SetState(RunState.Busted);
        }

        /// The story intro is playing; the run itself starts with StartRun when it ends.
        public void BeginIntro()
        {
            SetPaused(false);
            Distance = 0f;
            Speed = Tuning.SpeedBase;
            Score = 0;
            PickupCount = EnergyCollected = 0;
            SetState(RunState.Intro);
        }

        /// Near misses and other stunts.
        public void AddBonus(int points) => Score += points * ScoreFactor;

        public void AddNearMiss(int points)
        {
            NearMisses++;
            AddBonus(points);
        }

        /// Score multiplier with Double Score folded in.
        public int ScoreFactor => Multiplier * (PowerUps.Active(PowerUp.Double) ? 2 : 1);

        /// Back to the title menu, abandoning a run without paying out.
        public void ToMenu()
        {
            SetPaused(false);
            PowerUps.Clear();
            Missions.Save();
            Distance = 0f;
            Speed = Tuning.SpeedBase;
            SetState(RunState.Ready);
        }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void AddPickup(int points, bool energy = false)
        {
            PickupCount++;
            if (energy) EnergyCollected++;
            Score += points * ScoreFactor;
            if (!energy) Missions.Add(MissionStat.Burgers, 1);
            Combo = comboTimer > 0f ? Combo + 1 : 1;
            comboTimer = ComboWindow;
            BestCombo = Mathf.Max(BestCombo, Combo);
            if (Combo % ComboStep == 0)
            {
                int pay = ComboBonus * (Combo / ComboStep) * ScoreFactor;
                Score += pay;
                ComboMilestone?.Invoke(Combo, pay);
            }
        }

        void Update()
        {
            // Runs start from the menu's Play button (MenuUI), not from any key.
            if (State != RunState.Running || Paused) return;

            PowerUps.Tick(Time.deltaTime);
            RunTime += Time.deltaTime;
            if (comboTimer > 0f && (comboTimer -= Time.deltaTime) <= 0f) Combo = 0;
            Speed = Mathf.Min(Tuning.SpeedMax, Tuning.SpeedBase + Distance * Tuning.SpeedRamp);
            Speed *= DallasDash.Gameplay.VehicleDynamics.Current.speed;
            if (PowerUps.Active(PowerUp.Rush) || DashRun.Instance && DashRun.Instance.Boost > 0f) Speed *= PowerUps.RushSpeedFactor;
            else if (DashRun.Instance && DashRun.Instance.DashTime > 0f) Speed *= 1.55f;
            else if (DashRun.Instance && DashRun.Instance.ShortcutTime > 0f) Speed *= DashRun.RouteSpeedFactor; // side-route surge
            bonk = Mathf.MoveTowards(bonk, 0f, Time.deltaTime * 0.8f);
            Speed *= 1f - 0.55f * bonk * bonk;
            int mult = Mathf.Min(Tuning.MaxMult, 1 + (int)(Distance / Tuning.MultEvery));
            if (mult != Multiplier) { Multiplier = mult; MultiplierUp?.Invoke(mult); }
            float move = Speed * Time.deltaTime;
            Distance += move;

            scoreCarry += move * Tuning.PointsPerUnit * ScoreFactor;
            int whole = (int)scoreCarry;
            Score += whole;
            scoreCarry -= whole;
            Missions.SetRunValue(MissionStat.Distance, (int)Distance);
            Missions.SetRunValue(MissionStat.Score, Score);
        }

        void SetState(RunState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }
    }
}
