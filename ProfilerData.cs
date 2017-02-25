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
        public string functionPath;
        public FunctionDataValue[] values;

        public string GetValue(ProfilerColumn column)
        {
            var columnName = column.ToString();
            return Array.Find(values, v => v.column == columnName).value;
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