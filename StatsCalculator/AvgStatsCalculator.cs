using System.Collections.Generic;
using System.Linq;

namespace ProfilerDataExporter
{
    public class AvgStatsCalculator : StatsCalculatorBase
    {
        protected override float AggregateValues(IEnumerable<float> values)
        {
            return values.Average();
        }
    }
}