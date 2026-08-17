using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public class MaxStatsCalculator : StatsCalculatorBase
    {
        protected override float AggregateValues(IList<float> values)
        {
            var max = float.NegativeInfinity;
            for (int i = 0; i < values.Count; ++i)
            {
                var value = values[i];
                if (value > max)
                {
                    max = value;
                }
            }
            return max;
        }
    }
}