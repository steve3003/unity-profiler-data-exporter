using System;
using System.Collections.Generic;
using System.Linq;
using ByteSizeLib;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;

namespace ProfilerDataExporter
{
    public abstract class StatsCalculatorBase
    {
        private static readonly ProfilerColumn[] ProfilerColumns = (ProfilerColumn[])Enum.GetValues(typeof(ProfilerColumn));
        private static readonly string[] ProfilerColumnNames = Enum.GetNames(typeof(ProfilerColumn));

        private static readonly Func<FunctionData, float> GetSelfTime = f => float.Parse(f.GetValue(ProfilerColumn.SelfTime));
        private static readonly Func<FrameData, IEnumerable<FunctionData>> GetFunctions = f => f.functions;
        private static readonly Func<FunctionData, string> GetFunctionName = f => f.GetValue(ProfilerColumn.FunctionName);

        private static Func<FunctionData, float>[] getFunctionValues;

        private ProfilerColumn[] columnsToShow;

        protected StatsCalculatorBase()
        {
            if (getFunctionValues == null)
            {
                getFunctionValues = new Func<FunctionData, float>[ProfilerColumns.Length];
                for (var i = 0; i < ProfilerColumns.Length; ++i)
                {
                    var column = ProfilerColumns[i];
                    if (column != ProfilerColumn.GCMemory)
                    {
                        getFunctionValues[i] = f => float.Parse(f.GetValue(column).Replace("%", ""));
                    }
                    else
                    {
                        getFunctionValues[i] = f => (float)ByteSize.Parse(f.GetValue(column)).Bytes;
                    }
                }
            }
        }

        public FunctionData[] CalculateStats(ProfilerColumn[] columnsToShow)
        {
            var firstFrameIndex = ProfilerDriver.firstFrameIndex;
            var lastFrameIndex = ProfilerDriver.lastFrameIndex;
            var profilerData = ProfilerData.GetProfilerData(firstFrameIndex, lastFrameIndex);
            var functionsData = profilerData.frames.SelectMany(GetFunctions);
            var groupedFunctionData = functionsData.GroupBy(GetFunctionName).ToArray();

            this.columnsToShow = columnsToShow;

            var functionStats =
                groupedFunctionData
                    .Select<IGrouping<string, FunctionData>, FunctionData>(AggregateFunction)
                    .OrderByDescending(GetSelfTime)
                    .ToArray();
            return functionStats;
        }

        private FunctionData AggregateFunction(IGrouping<string, FunctionData> functionGroup)
        {
            var function = new FunctionData { values = new FunctionDataValue[columnsToShow.Length] };
            function.values[0] = new FunctionDataValue
            {
                column = "FunctionName",
                value = functionGroup.Key
            };
            var framesData = functionGroup.ToArray();
            for (var i = 1; i < columnsToShow.Length; ++i)
            {
                var column = columnsToShow[i];
                var functionDataValue = GetValue(framesData, column);
                function.values[i] = functionDataValue;
            }
            return function;
        }

        private FunctionDataValue GetValue(FunctionData[] framesData, ProfilerColumn column)
        {
            var functionDataValue = new FunctionDataValue { column = ProfilerColumnNames[(int)column] };
            var getFunctionValue = getFunctionValues[(int)column];
            if (column != ProfilerColumn.GCMemory)
            {
                functionDataValue.value =
                    AggregateValues(framesData.Select(getFunctionValue)).ToString("F2");
            }
            else
            {
                functionDataValue.value =
                    ByteSize.FromBytes(AggregateValues(framesData.Select(getFunctionValue)))
                        .ToString();
            }
            return functionDataValue;
        }

        protected abstract float AggregateValues(IEnumerable<float> values);
    }
}