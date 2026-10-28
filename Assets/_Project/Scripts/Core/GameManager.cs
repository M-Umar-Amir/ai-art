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
        public bool Paused { get; private set; }
        /// Set when a run ends: coins paid out (one per burger) and whether the score beat the saved best.
        public int CoinsEarned { get; private set; }
        public bool NewBest { get; private set; }
        /// Frame the run started on, so the input that started it isn't also read as a move.
        public int StartFrame { get; private set; } = -1;

        public event Action<RunState> StateChanged;
        public event Action<int> MultiplierUp;

        float scoreCarry;
        /// 1 right after a head-on hit, easing back to 0: the rider is slowed while it lasts.
        float bonk;

        /// Ran into something head on: the rider loses most of their speed and builds it back up.
        public void Bonk() => bonk = 1f;

        void Awake() => Instance = this;

        public void StartRun()
        {
            Distance = 0f;
            Speed = Tuning.SpeedBase;
            Score = 0;
            PickupCount = 0;
            Multiplier = 1;
            scoreCarry = 0f;
            bonk = 0f;
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
            CoinsEarned = PickupCount;
            PlayerProfile.TotalPoints += Score;
            NewBest = Score > PlayerProfile.BestScore;
            if (NewBest) PlayerProfile.BestScore = Score;
            PlayerProfile.Coins += CoinsEarned;
            SetState(RunState.Busted);
        }

        /// The story intro is playing; the run itself starts with StartRun when it ends.
        public void BeginIntro()
        {
            SetPaused(false);
            Distance = 0f;
            Speed = Tuning.SpeedBase;
            Score = 0;
            PickupCount = 0;
            SetState(RunState.Intro);
        }

        /// Near misses and other stunts.
        public void AddBonus(int points) => Score += points * ScoreFactor;

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

        public void AddPickup(int points)
        {
            PickupCount++;
            Score += points * ScoreFactor;
            Missions.Add(MissionStat.Burgers, 1);
        }

        void Update()
        {
            // Runs start from the menu's Play button (MenuUI), not from any key.
            if (State != RunState.Running || Paused) return;

            PowerUps.Tick(Time.deltaTime);
            Speed = Mathf.Min(Tuning.SpeedMax, Tuning.SpeedBase + Distance * Tuning.SpeedRamp);
            if (PowerUps.Active(PowerUp.Rush)) Speed *= PowerUps.RushSpeedFactor;
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
