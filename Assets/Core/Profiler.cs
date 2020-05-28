using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Source
{
    public static class Profiler
    {
        public struct SampleInfo : IDisposable
        {
            public readonly string Ident;
            public readonly TimeSpan StartTime;

            public SampleInfo(string ident, TimeSpan startTime)
            {
                Ident = ident;
                StartTime = startTime;
            }

            public void Dispose()
            {
                EndSample(this);
            }
        }

        private static readonly Stopwatch _sStopwatch = new Stopwatch();
        private static readonly Dictionary<string, TimeSpan> _sTotals = new Dictionary<string, TimeSpan>(); 

        static Profiler()
        {
            _sStopwatch.Start();
        }

        public static IDisposable Begin(string nameFormat, params object[] args)
        {
            var ident = args.Length == 0 ? nameFormat : string.Format(nameFormat, args);
            return new SampleInfo(ident, _sStopwatch.Elapsed);
        }

        private static void EndSample(SampleInfo sampleInfo)
        {
            TimeSpan cur;
            if (!_sTotals.TryGetValue(sampleInfo.Ident, out cur))
            {
                _sTotals.Add(sampleInfo.Ident, _sStopwatch.Elapsed - sampleInfo.StartTime);
                return;
            }
            else
            {
                _sTotals[sampleInfo.Ident] = cur + (_sStopwatch.Elapsed - sampleInfo.StartTime);
            }
        }

        public static void Print()
        {
            foreach (var pair in _sTotals)
            {
                UnityEngine.Debug.LogFormat("[Profiler] {0}: {1:F2}ms", pair.Key, pair.Value.TotalSeconds * 1000d);
            }
        }
    }
}
