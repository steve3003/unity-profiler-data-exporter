using System.Collections.Generic;
using System.Linq;
using ByteSizeLib;
using UnityEditorInternal;

namespace ProfilerDataExporter
{
    public abstract class StatsCalculatorBase
    {
        public FunctionData[] CalculateStats(ProfilerColumn[] columnsToShow)
        {
            var firstFrameIndex = ProfilerDriver.firstFrameIndex;
            var lastFrameIndex = ProfilerDriver.lastFrameIndex;
            var profilerData = ProfilerData.GetProfilerData(firstFrameIndex, lastFrameIndex);
            var functionsData = profilerData.frames.SelectMany(f => f.functions);
            var groupedFunctionData = functionsData.GroupBy(f => f.GetValue(ProfilerColumn.FunctionName)).ToArray();

            var functionStats =
                groupedFunctionData
                    .Select(g =>
                    {
                        var function = new FunctionData { values = new FunctionDataValue[columnsToShow.Length] };
                        function.values[0] = new FunctionDataValue
                        {
                            column = ProfilerColumn.FunctionName.ToString(),
                            value = g.Key
                        };
                        var framesData = g.ToArray();
                        for (var i = 1; i < columnsToShow.Length; ++i)
                        {
                            var column = columnsToShow[i];
                            var functionDataValue = GetValue(framesData, column);
                            function.values[i] = functionDataValue;
                        }
                        return function;
                    })
                    .OrderByDescending(f => float.Parse(f.GetValue(ProfilerColumn.SelfTime)))
                    .ToArray();
            return functionStats;
        }

        private FunctionDataValue GetValue(FunctionData[] framesData, ProfilerColumn column)
        {
            var functionDataValue = new FunctionDataValue { column = column.ToString() };
            if (column != ProfilerColumn.GCMemory)
            {
                functionDataValue.value =
                    AggregateValues(framesData.Select(f => float.Parse(f.GetValue(column).Replace("%", "")))).ToString("F2");
            }
            else
            {
                functionDataValue.value =
                    ByteSize.FromBytes(AggregateValues(framesData.Select(f => (float)ByteSize.Parse(f.GetValue(column)).Bytes)))
                        .ToString();
            }
            return functionDataValue;
        }

        protected abstract float AggregateValues(IEnumerable<float> values);
    }
}