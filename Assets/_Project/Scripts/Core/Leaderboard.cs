using System;
using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Core
{
    public enum BoardPeriod { Today, Week, AllTime }

    /// Today / This week / All time boards. For now they rank this device's own runs, kept in PlayerPrefs; when
    /// the online leaderboard service exists (see the database notes), Record() also posts the run there and
    /// Top() reads the shared board, with this list as the offline fallback.
    public static class Leaderboard
    {
        [Serializable]
        public class Run
        {
            public int score, distance, burgers, bestCombo, nearMisses, durationMs;
            public long at; // UTC ticks
            public DateTime When => new(at, DateTimeKind.Utc);
        }

        [Serializable]
        class Store { public List<Run> runs = new(); }

        const string Key = "lb_runs";
        /// All-time keeps the best this many; every run from the last 8 days is kept for the day/week boards.
        const int KeepBest = 50;

        static Store store;

        static Store Data
        {
            get
            {
                if (store != null) return store;
                try { store = JsonUtility.FromJson<Store>(PlayerPrefs.GetString(Key, "")) ?? new Store(); }
                catch (Exception) { store = new Store(); }
                store.runs ??= new List<Run>();
                return store;
            }
        }

        /// Saves a finished run and returns its rank on each board (1 = top).
        public static (int today, int week, int all) Record(GameManager gm)
        {
            var run = new Run
            {
                score = gm.Score, distance = (int)gm.Distance, burgers = gm.PickupCount, bestCombo = gm.BestCombo,
                nearMisses = gm.NearMisses, durationMs = (int)(gm.RunTime * 1000f), at = DateTime.UtcNow.Ticks,
            };
            Data.runs.Add(run);
            Trim();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            return (RankOf(run, BoardPeriod.Today), RankOf(run, BoardPeriod.Week), RankOf(run, BoardPeriod.AllTime));
        }

        public static List<Run> Top(BoardPeriod period, int count)
        {
            var list = Data.runs.FindAll(r => In(r, period));
            list.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.at.CompareTo(b.at));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        static int RankOf(Run run, BoardPeriod period)
        {
            int rank = 1;
            foreach (var r in Data.runs)
                if (r != run && In(r, period) && (r.score > run.score || (r.score == run.score && r.at < run.at))) rank++;
            return rank;
        }

        /// Days run midnight to midnight local time; weeks start on Monday.
        static bool In(Run r, BoardPeriod period)
        {
            if (period == BoardPeriod.AllTime) return true;
            var local = r.When.ToLocalTime().Date;
            var today = DateTime.Now.Date;
            if (period == BoardPeriod.Today) return local == today;
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            return local >= monday;
        }

        static void Trim()
        {
            var cutoff = DateTime.UtcNow.AddDays(-8).Ticks;
            var best = Top(BoardPeriod.AllTime, KeepBest);
            Data.runs.RemoveAll(r => r.at < cutoff && !best.Contains(r));
        }
    }
}
