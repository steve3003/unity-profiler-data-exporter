using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public enum StatsType
    {
        MaxValues,
        AverageValues,
        MinValues, 
        SumValues, 
    }

    public static class StatsCalculatorProvider
    {
        private static readonly Dictionary<StatsType, StatsCalculatorBase> Calculators = new Dictionary<StatsType, StatsCalculatorBase>()
        {
            {StatsType.MaxValues,  new MaxStatsCalculator()},
            {StatsType.AverageValues,  new AvgStatsCalculator()},
            {StatsType.MinValues,  new MinStatsCalculator()}, 
            {StatsType.SumValues,  new SumStatsCalculator()}, 
        };

        public static StatsCalculatorBase GetStatsCalculator(StatsType statsType)
        {
            return Calculators[statsType];
        }
    }
}