using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public class MinStatsCalculator : StatsCalculatorBase
    {
        protected override float AggregateValues(IList<float> values)
        {
            var min = float.PositiveInfinity;
            for (int i = 0; i < values.Count; ++i)
            {
                var value = values[i];
                if (value < min)
                {
                    min = value;
                }
            }
            return min;
        }
    }
}