using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace ProfilerDataExporter
{
    [Serializable]
    public class ProfilerData
    {
        public List<FrameData> frames = new List<FrameData>();

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        public static ProfilerData GetProfilerData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "")
        {
            var profilerSortColumn = ProfilerColumn.TotalTime;
            var viewType = ProfilerViewType.Hierarchy;
            var property = new ProfilerProperty();

            var profilerData = new ProfilerData();
            for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
            {
                property.SetRoot(frameIndex, profilerSortColumn, viewType);
                property.onlyShowGPUSamples = false;

                var frameData = new FrameData();
                const bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    bool shouldSaveProperty = string.IsNullOrEmpty(selectedPropertyPath) || property.propertyPath == selectedPropertyPath;
                    if (shouldSaveProperty)
                    {
                        var functionData = FunctionData.Create(property);
                        frameData.functions.Add(functionData);
                        //Debug.Log(functionData.ToString());
                    }
                }
                property.Cleanup();
                profilerData.frames.Add(frameData);
                //Debug.Log(frameData.ToString());
            }
            //Debug.Log(profilerData.ToString());
            return profilerData;
        }
    }

    [Serializable]
    public class FrameData
    {
        public List<FunctionData> functions = new List<FunctionData>();

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class FunctionData
    {
        private static readonly string[] ColumnNames = Enum.GetNames(typeof(ProfilerColumn));

        public string functionPath;
        public FunctionDataValue[] values;

        public string GetValue(ProfilerColumn column)
        {
            var columnName = ColumnNames[(int)column];
            return FindDataValue(columnName).value;
        }

        private FunctionDataValue FindDataValue(string columnName)
        {
            int length = values.Length;
            for (int i = 0; i < length; ++i)
            {
                var value = values[i];
                if (value.column == columnName)
                {
                    return value;
                }
            }
            return default(FunctionDataValue);
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        public static FunctionData Create(ProfilerProperty property)
        {
            var functionData = new FunctionData();
            var columns = Enum.GetValues(typeof(ProfilerColumn));
            functionData.values = new FunctionDataValue[columns.Length];
            functionData.functionPath = property.propertyPath;
            for (int i = 0; i < columns.Length; ++i)
            {
                var column = (ProfilerColumn)columns.GetValue(i);
#if UNITY_5_5
                if (column == ProfilerColumn.DontSort)
                {
                    continue;
                }
#endif
                var functionDataValue = new FunctionDataValue();
                functionDataValue.column = column.ToString();
                functionDataValue.value = property.GetColumn(column);
                functionData.values[i] = functionDataValue;
            }
            return functionData;
        }
    }

    [Serializable]
    public class FunctionDataValue
    {
        public string column;
        public string value;
    }
}