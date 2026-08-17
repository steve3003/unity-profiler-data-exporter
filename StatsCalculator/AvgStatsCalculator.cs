using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public class AvgStatsCalculator : StatsCalculatorBase
    {
        protected override float AggregateValues(IList<float> values)
        {
            var sum = 0f;
            for (int i = 0; i < values.Count; ++i)
            {
                sum += values[i];
            }
            return sum / values.Count;
        }
    }
}