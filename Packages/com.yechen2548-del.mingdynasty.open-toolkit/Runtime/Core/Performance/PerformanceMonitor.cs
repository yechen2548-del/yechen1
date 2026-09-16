using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MingDynasty.OpenToolkit.Core
{
    public struct PerformanceSample
    {
        public string Name;
        public double DurationMilliseconds;
    }

    public interface IPerformanceMonitor
    {
        PerformanceSample Measure(string name, Action action);
        IReadOnlyList<PerformanceSample> Samples { get; }
        void Clear();
    }

    public sealed class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly List<PerformanceSample> samples = new List<PerformanceSample>();

        public IReadOnlyList<PerformanceSample> Samples { get { return samples; } }

        public PerformanceSample Measure(string name, Action action)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A performance sample needs a name.", nameof(name));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            PerformanceSample sample = new PerformanceSample
            {
                Name = name,
                DurationMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            };
            samples.Add(sample);
            return sample;
        }

        public void Clear()
        {
            samples.Clear();
        }
    }
}
