using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BDArmory.Core
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PerformanceLogger : MonoBehaviour, IDisposable
    {
        private const string FileName = "BDArmory_PerformanceLog.txt";

        private static readonly Dictionary<string, PerformanceData> PerformanceEntries =
            new Dictionary<string, PerformanceData>();

        private readonly string _id;

        private readonly Stopwatch _stopwatch;

        public PerformanceLogger(string id)
        {
            if (!BDArmorySettings.PERFORMANCE_LOGGING)
                return;

            _stopwatch = Stopwatch.StartNew();
            _id = id;
        }

        public void Dispose()
        {
            if (!BDArmorySettings.PERFORMANCE_LOGGING)
                return;
            _stopwatch.Stop();

            if (PerformanceEntries.ContainsKey(_id))
                UpdatePerformanceRecord();
            else
                CreatePerformanceRecord();
        }

        private void OnDestroy()
        {
            if (PerformanceEntries.Count == 0)
                return;

            Debug.Log("PerformanceLogger.OnDestroy");

            var sb = new StringBuilder();
            foreach (var performanceEntry in PerformanceEntries.OrderByDescending(x => x.Value.TotalTicks))
            {
                sb.AppendFormat("{0:yyyy/MM/dd HH:mm:ss.ff} - Performance Entry Id: {1} => {2}", DateTime.Now,
                    performanceEntry.Key, performanceEntry.Value);
                sb.AppendLine();
            }

            File.AppendAllText(GetFilePath(), sb.ToString());

            PerformanceEntries.Clear();
        }

        private string GetFilePath()
        {
            return Path.Combine(Path.Combine(Application.dataPath, ".."),
                FileName);
        }

        private void CreatePerformanceRecord()
        {
            PerformanceEntries.Add(_id, new PerformanceData
            {
                Calls = 1,
                TotalTicks = _stopwatch.ElapsedTicks,
                MaxTick = _stopwatch.ElapsedTicks,
                MinTick = _stopwatch.ElapsedTicks
            });
        }

        private void UpdatePerformanceRecord()
        {
            var performanceData = PerformanceEntries[_id];
            performanceData.Calls += 1;
            performanceData.TotalTicks += _stopwatch.ElapsedTicks;

            performanceData.MaxTick = _stopwatch.ElapsedTicks > performanceData.MaxTick
                ? _stopwatch.ElapsedTicks
                : performanceData.MaxTick;

            performanceData.MinTick = _stopwatch.ElapsedTicks < performanceData.MinTick
                ? _stopwatch.ElapsedTicks
                : performanceData.MinTick;
        }
    }

    internal class PerformanceData
    {
        public int Calls { get; set; }
        public long TotalTicks { get; set; }
        public float AverageTicks => (float) Math.Round(TotalTicks / (float) Calls, 1);

        public long MaxTick { get; set; }
        public long MinTick { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Average ticks {0} | Max ticks {1} | Min ticks {2} | Total ticks {3} | Calls {4}",
                AverageTicks, MaxTick, MinTick, TotalTicks, Calls);

            return sb.ToString();
        }
    }
}