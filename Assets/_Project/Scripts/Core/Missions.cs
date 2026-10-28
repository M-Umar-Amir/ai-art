using System;
using UnityEngine;

namespace DallasDash.Core
{
    public enum MissionStat { Burgers, Distance, Score, NearMisses, Jumps, Overtakes, PowerUps }

    /// Three missions at a time. "In one run" goals keep the best single run; the others add up across runs.
    /// Finishing one pays coins and deals a new one; targets climb with every mission completed, and the
    /// courier scooter is earned after ScooterMissions of them (the pack's one earnable ride, never sold).
    public static class Missions
    {
        public const int Slots = 3;
        public const int ScooterMissions = 6;

        public class Mission
        {
            public MissionStat stat;
            public int target, progress;
            public bool PerRun => stat is MissionStat.Burgers or MissionStat.Distance or MissionStat.Score;
            public bool Done => progress >= target;
            public int Reward => 200 + 100 * Mathf.Min(Level, 10);
            public string Text => stat switch
            {
                MissionStat.Burgers => $"Collect {target} burgers in one run",
                MissionStat.Distance => $"Ride {target:N0} m in one run",
                MissionStat.Score => $"Score {target:N0} points in one run",
                MissionStat.NearMisses => $"Pull off {target} near misses",
                MissionStat.Jumps => $"Jump {target} times",
                MissionStat.Overtakes => $"Overtake {target} other riders",
                _ => $"Grab {target} power-ups",
            };
        }

        public static event Action<Mission> Completed;

        static Mission[] active;
        static readonly int[] runValue = new int[7];

        /// Missions finished so far, which also sets how hard new ones are.
        public static int Level => PlayerPrefs.GetInt("missions_done", 0);

        public static Mission[] Active
        {
            get
            {
                if (active != null) return active;
                active = new Mission[Slots];
                for (int i = 0; i < Slots; i++)
                {
                    int stat = PlayerPrefs.GetInt($"mission{i}_stat", -1);
                    if (stat < 0) { active[i] = Deal(i); continue; }
                    active[i] = new Mission
                    {
                        stat = (MissionStat)stat,
                        target = PlayerPrefs.GetInt($"mission{i}_target", 10),
                        progress = PlayerPrefs.GetInt($"mission{i}_progress", 0),
                    };
                    // Saves from before 28 Oct hold a "roll under" mission in this slot (rolling was removed):
                    // it becomes an overtake mission with its own target.
                    if (active[i].stat == MissionStat.Overtakes && PlayerPrefs.GetInt("missions_v", 1) < 2)
                    {
                        active[i].target = Target(MissionStat.Overtakes, Level);
                        active[i].progress = 0;
                    }
                }
                PlayerPrefs.SetInt("missions_v", 2);
                Save();
                return active;
            }
        }

        static int Target(MissionStat s, int lvl) => s switch
        {
            MissionStat.Burgers => 20 + lvl * 10,
            MissionStat.Distance => 500 + lvl * 250,
            MissionStat.Score => 5000 + lvl * 4000,
            MissionStat.NearMisses => 3 + lvl,
            MissionStat.Jumps => 20 + lvl * 10,
            MissionStat.Overtakes => 8 + lvl * 4,
            _ => 2 + lvl,
        };

        /// A new mission for a slot, never repeating a stat another slot already has.
        static Mission Deal(int slot)
        {
            var stats = (MissionStat[])Enum.GetValues(typeof(MissionStat));
            for (int tries = 0; tries < 30; tries++)
            {
                var s = stats[UnityEngine.Random.Range(0, stats.Length)];
                bool taken = false;
                for (int i = 0; i < Slots; i++)
                    if (i != slot && active != null && active[i] != null && active[i].stat == s) taken = true;
                if (!taken) return new Mission { stat = s, target = Target(s, Level) };
            }
            return new Mission { stat = MissionStat.Jumps, target = Target(MissionStat.Jumps, Level) };
        }

        public static void BeginRun() => Array.Clear(runValue, 0, runValue.Length);

        /// Counted events: burgers, near misses, jumps, overtakes, power-ups.
        public static void Add(MissionStat stat, int amount)
        {
            runValue[(int)stat] += amount;
            Update(stat, amount);
        }

        /// Running totals for this run (distance, score).
        public static void SetRunValue(MissionStat stat, int value)
        {
            if (runValue[(int)stat] == value) return;
            runValue[(int)stat] = value;
            Update(stat, 0);
        }

        static void Update(MissionStat stat, int delta)
        {
            var list = Active;
            for (int i = 0; i < Slots; i++)
            {
                var m = list[i];
                if (m.stat != stat || m.Done) continue;
                m.progress = m.PerRun ? Mathf.Max(m.progress, runValue[(int)stat]) : m.progress + delta;
                if (!m.Done) continue;
                m.progress = m.target;
                PlayerPrefs.SetInt("missions_done", Level + 1);
                PlayerProfile.Coins += m.Reward;
                Completed?.Invoke(m);
                list[i] = Deal(i);
                Save();
            }
        }

        /// Progress lives in memory during a run; written at the end of it and whenever a mission completes.
        public static void Save()
        {
            if (active == null) return;
            for (int i = 0; i < Slots; i++)
            {
                PlayerPrefs.SetInt($"mission{i}_stat", (int)active[i].stat);
                PlayerPrefs.SetInt($"mission{i}_target", active[i].target);
                PlayerPrefs.SetInt($"mission{i}_progress", active[i].progress);
            }
            PlayerPrefs.Save();
        }

        public static bool ScooterEarned => Level >= ScooterMissions;
    }
}
