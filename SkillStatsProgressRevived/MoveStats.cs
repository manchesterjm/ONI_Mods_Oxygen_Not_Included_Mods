using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SkillStatsProgressRevived
{
    // Speed and travel-distance readouts for the Attributes panel. The rolling
    // average speed samples total distance traveled (all nav types) against
    // game time inside the configured window; travel rows show km today and in
    // total, with the per-path breakdown in the tooltip instead of one row per
    // nav type (the old mod drew dozens of rows).
    internal static class MoveStats
    {
        private struct SpeedSample
        {
            public float Time;
            public int Distance;
        }

        private static Navigator lastNavigator;

        private static readonly LinkedList<SpeedSample> SpeedSamples = new LinkedList<SpeedSample>();

        private static readonly Dictionary<MinionIdentity, Dictionary<NavType, int>> DistanceAtDayStart =
            new Dictionary<MinionIdentity, Dictionary<NavType, int>>();

        private static readonly Dictionary<MinionIdentity, int> DayOfSnapshot =
            new Dictionary<MinionIdentity, int>();

        public static string CurrentSpeedLabel(Navigator navigator)
        {
            string speed = "0.000";
            if (navigator.transitionDriver != null && navigator.transitionDriver.GetTransition != null
                && navigator.IsMoving())
            {
                speed = navigator.transitionDriver.GetTransition.speed.ToString("f3");
            }
            Grid.PosToXY(navigator.transform.GetPosition(), out int x, out int y);
            return $"Speed: <b>{speed}</b> tile/s  x:<b>{x}</b> y:<b>{y}</b> Cell:<b>{Grid.PosToCell(navigator.gameObject)}</b>";
        }

        public static string AverageSpeedLabel(Navigator navigator)
        {
            if (lastNavigator != navigator)
            {
                SpeedSamples.Clear();
                lastNavigator = navigator;
            }
            float now = GameClock.Instance.GetTime();
            int distance = TotalDistance(navigator);
            if (SpeedSamples.First == null || SpeedSamples.First.Value.Time != now)
            {
                SpeedSamples.AddFirst(new SpeedSample { Time = now, Distance = distance });
            }
            float window = SkillStatsProgressRevivedMod.Options.AvgSpeedWindowSeconds;
            while (SpeedSamples.Last != null && now - SpeedSamples.Last.Value.Time > window + 1f)
            {
                SpeedSamples.RemoveLast();
            }
            SpeedSample oldest = SpeedSamples.Last.Value;
            float seconds = now - oldest.Time;
            int tiles = distance - oldest.Distance;
            float average = (seconds == 0f) ? 0f : (tiles / seconds);
            return $"Avg.Speed: <b>{average:f3}</b> tile/s  Dist: <b>{tiles}</b> in last <b>{seconds:f0}</b> s";
        }

        // Distance rows: one label per scope, per-path breakdown in the tooltip.
        public static void DistanceSummary(Navigator navigator, MinionIdentity dupe,
            out string todayLabel, out string todayTooltip, out string totalLabel, out string totalTooltip)
        {
            SnapshotDayStart(dupe, navigator);
            Dictionary<NavType, int> today = DistanceSince(navigator, DistanceAtDayStart[dupe]);
            todayLabel = FormatTotalKm("Traveled today", today);
            todayTooltip = BreakdownTooltip(today);
            totalLabel = FormatTotalKm("Traveled total", navigator.distanceTravelledByNavType);
            totalTooltip = BreakdownTooltip(navigator.distanceTravelledByNavType);
        }

        private static int TotalDistance(Navigator navigator)
        {
            int tiles = 0;
            foreach (KeyValuePair<NavType, int> entry in navigator.distanceTravelledByNavType)
            {
                tiles += entry.Value;
            }
            return tiles;
        }

        private static void SnapshotDayStart(MinionIdentity dupe, Navigator navigator)
        {
            int today = GameClock.Instance.GetCycle();
            if (DayOfSnapshot.TryGetValue(dupe, out int snapshotDay) && snapshotDay == today)
            {
                return;
            }
            DayOfSnapshot[dupe] = today;
            DistanceAtDayStart[dupe] = new Dictionary<NavType, int>(navigator.distanceTravelledByNavType);
        }

        private static Dictionary<NavType, int> DistanceSince(Navigator navigator, Dictionary<NavType, int> baseline)
        {
            Dictionary<NavType, int> since = new Dictionary<NavType, int>();
            foreach (KeyValuePair<NavType, int> entry in navigator.distanceTravelledByNavType)
            {
                baseline.TryGetValue(entry.Key, out int atStart);
                since[entry.Key] = entry.Value - atStart;
            }
            return since;
        }

        private static string FormatTotalKm(string caption, Dictionary<NavType, int> byNavType)
        {
            int tiles = 0;
            foreach (KeyValuePair<NavType, int> entry in byNavType)
            {
                tiles += entry.Value;
            }
            return $"{caption}: <b>{tiles / 1000f:N3}</b> km";
        }

        private static string BreakdownTooltip(Dictionary<NavType, int> byNavType)
        {
            StringBuilder breakdown = new StringBuilder("By path type:");
            foreach (KeyValuePair<NavType, int> entry in byNavType)
            {
                if (entry.Value != 0)
                {
                    breakdown.Append($"\n  {entry.Key}: {entry.Value / 1000f:N3} km");
                }
            }
            return breakdown.ToString();
        }
    }
}
