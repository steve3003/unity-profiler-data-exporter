using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

public static class ProfilerDataExporter
{
    [Serializable]
    private class FunctionDataValue
    {
        public string column;
        public string value;
    }

    [Serializable]
    private class FunctionData
    {
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
    private class FrameData
    {
        public List<FunctionData> functions = new List<FunctionData>();

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    private class ProfilerData
    {
        public List<FrameData> frames = new List<FrameData>();

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [MenuItem("Profiler Data Exporter/Export Data")]
    private static void ExportProfilerData()
    {
        var firstFrameIndex = ProfilerDriver.firstFrameIndex;
        var lastFrameIndex = ProfilerDriver.lastFrameIndex;
        ExtractData(firstFrameIndex, lastFrameIndex);
    }

    [MenuItem("Profiler Data Exporter/Export Current Frame Data")]
    private static void ExportCurrentFrameData()
    {
        Assembly assem = typeof(Editor).Assembly;
        Type type = assem.GetType("UnityEditor.ProfilerWindow");
        var window = EditorWindow.GetWindow(type);
        var fieldInfo = type.GetField("m_CurrentFrame", BindingFlags.NonPublic | BindingFlags.Instance);
        var frame = (int)fieldInfo.GetValue(window);
        if (frame > -1)
        {
            ExtractData(frame, frame);
        }
    }

    [MenuItem("Profiler Data Exporter/Export Selected Function Data")]
    private static void ExportSelectedFunctionData()
    {
        var firstFrameIndex = ProfilerDriver.firstFrameIndex;
        var lastFrameIndex = ProfilerDriver.lastFrameIndex;
        ExtractData(firstFrameIndex, lastFrameIndex, ProfilerDriver.selectedPropertyPath);
    }

    private static void ExtractData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "")
    {
        var profilerSortColumn = ProfilerColumn.TotalTime;
        var viewType = ProfilerViewType.Hierarchy;

        var profilerData = new ProfilerData();
        for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
        {
            var property = new ProfilerProperty();
            property.SetRoot(frameIndex, profilerSortColumn, viewType);
            property.onlyShowGPUSamples = false;

            var frameData = new FrameData();
            bool enterChildren = true;
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
            profilerData.frames.Add(frameData);
            //Debug.Log(frameData.ToString());
        }
        //Debug.Log(profilerData.ToString());
        File.WriteAllText(@"profiler_data.json", profilerData.ToString());
    }
}
