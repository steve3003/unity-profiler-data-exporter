#undef USE_AUTO_REPAINT

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Linq;
using ByteSizeLib;

public class ProfilerDataExporter : EditorWindow
{
#if USE_AUTO_REPAINT
    private const float editorUpdateTime = 1f;
    private float lastUpdateTime = 0;
#endif

    private string filePath = @"profiler_data.json";
    private int currentframe = -1;
    private Type profilerWindowType;
    private FieldInfo currentFrameFieldInfo;
    private Vector2 scrollPosition;
    private FunctionData[] worstTotaTimeFunctions;
    private FunctionData[] worstGCAllocFunctions;
    private ByteSize maxGCAlloc;
    private float maxTime;
    private string calculatedPropertyPath;

    [MenuItem("Window/Profiler Data Exporter")]
    private static void Init()
    {
        var window = GetWindow<ProfilerDataExporter>("Profiler Data Exporter");
        window.Show();
    }

    private void OnEnable()
    {
#if USE_AUTO_REPAINT
        EditorApplication.update += EditorUpdate;
#endif
        Assembly assem = typeof(Editor).Assembly;
        profilerWindowType = assem.GetType("UnityEditor.ProfilerWindow");
        currentFrameFieldInfo = profilerWindowType.GetField("m_CurrentFrame", BindingFlags.NonPublic | BindingFlags.Instance);
    }

#if USE_AUTO_REPAINT
    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }
#endif

#if USE_AUTO_REPAINT
    private void EditorUpdate()
    {
        var time = Time.realtimeSinceStartup;
        var deltaTime = time - lastUpdateTime;
        if (deltaTime >= editorUpdateTime)
        {
            lastUpdateTime = time;
            Repaint();
        }
    }
#endif

    private void OnGUI()
    {
        var profilerWindow = EditorWindow.GetWindow(profilerWindowType);
        currentframe = (int)currentFrameFieldInfo.GetValue(profilerWindow);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawStats();
        DrawFilePath();
        DrawExportButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawStats()
    {
        var firstFrameIndex = ProfilerDriver.firstFrameIndex;
        var lastFrameIndex = ProfilerDriver.lastFrameIndex;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Statistics", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
        var calulateStatistics = GUILayout.Button("Calculate", GUILayout.ExpandWidth(false));
        GUILayout.EndHorizontal();
        if (currentframe > 0)
        {
            GUILayout.Label(string.Format("Current frame = {0}", currentframe));
        }

        if (calulateStatistics)
        {
            var profilerData = GetProfilerData(firstFrameIndex, lastFrameIndex);
            var functionsData = profilerData.frames.SelectMany(f => f.functions);
            var groupedFunctionData = functionsData.GroupBy(f => f.GetValue(ProfilerColumn.FunctionName));
            worstTotaTimeFunctions =
                groupedFunctionData
                    .Select(g => g.OrderByDescending(f => f.GetValue(ProfilerColumn.TotalTime)).First())
                    .OrderByDescending(f => f.GetValue(ProfilerColumn.TotalTime))
                    .Take(5)
                    .ToArray();
            worstGCAllocFunctions =
                groupedFunctionData
                    .Select(g => g.OrderByDescending(f => ByteSize.Parse(f.GetValue(ProfilerColumn.GCMemory))).First())
                    .OrderByDescending(f => ByteSize.Parse(f.GetValue(ProfilerColumn.GCMemory)))
                    .Take(5)
                    .ToArray();
        }

        if (worstTotaTimeFunctions != null)
        {
            GUILayout.Label("Worst Total time functions", EditorStyles.boldLabel);
            foreach (var f in worstTotaTimeFunctions)
            {
                var name = f.GetValue(ProfilerColumn.FunctionName);
                var totalTime = f.GetValue(ProfilerColumn.TotalTime);
                GUILayout.Label(string.Format("{0} = {1}", name, totalTime));
            }
        }

        if (worstGCAllocFunctions != null)
        {
            GUILayout.Label("Worst GC Alloc functions", EditorStyles.boldLabel);
            foreach (var f in worstGCAllocFunctions)
            {
                var name = f.GetValue(ProfilerColumn.FunctionName);
                var gcAlloc = f.GetValue(ProfilerColumn.GCMemory);
                GUILayout.Label(string.Format("{0} = {1}", name, gcAlloc));
            }
        }

        var selectedPropertyPath = ProfilerDriver.selectedPropertyPath;
        if (!string.IsNullOrEmpty(selectedPropertyPath))
        {
            if (calculatedPropertyPath != selectedPropertyPath)
            {
                var functionData = GetProfilerData(firstFrameIndex, lastFrameIndex, selectedPropertyPath);
                var functionFrameData = functionData.frames.Where(f => f.functions.Count > 0);
                maxTime = functionFrameData.Max(f => float.Parse(f.functions[0].GetValue(ProfilerColumn.TotalTime)));
                maxGCAlloc = functionFrameData.Max(f => ByteSize.Parse(f.functions[0].GetValue(ProfilerColumn.GCMemory)));
                calculatedPropertyPath = selectedPropertyPath;
            }

            GUILayout.Label("Function Statistics", EditorStyles.boldLabel);
            GUILayout.Label(string.Format("Selected function = {0}", selectedPropertyPath));
            GUILayout.Label(string.Format("Max Total Time = {0}", maxTime));
            GUILayout.Label(string.Format("Max GC Alloc = {0}", maxGCAlloc));
        }
    }

    private void DrawExportButtons()
    {
        GUILayout.Label("Export", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Profiler Data"))
        {
            ExportProfilerData();
        }
        GUI.enabled = currentframe > 0;
        if (GUILayout.Button("Current Frame Data"))
        {
            ExportCurrentFrameData();
        }
        GUI.enabled = true;
        GUI.enabled = !string.IsNullOrEmpty(ProfilerDriver.selectedPropertyPath);
        if (GUILayout.Button("Selected Function Data"))
        {
            ExportSelectedFunctionData();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawFilePath()
    {
        GUILayout.Label("File path", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        var currentDirectory = Directory.GetCurrentDirectory();
        var displayedFilePath = Path.GetFullPath(filePath).Replace(currentDirectory, "");
        GUILayout.Label(displayedFilePath, GUILayout.ExpandWidth(false));
        if (GUILayout.Button("Open", GUILayout.ExpandWidth(false)))
        {
            Process.Start(filePath);
        }
        if (GUILayout.Button("Edit", GUILayout.ExpandWidth(false)))
        {
            var currentPathDirectory = Path.GetDirectoryName(filePath);
            var currentName = Path.GetFileNameWithoutExtension(filePath);
            var newfilePath = EditorUtility.SaveFilePanel("Select file path", currentPathDirectory, currentName, "json");
            var isPathValid = !string.IsNullOrEmpty(newfilePath);
            if (isPathValid)
            {
                filePath = newfilePath;
            }
        }

        GUILayout.EndHorizontal();
    }

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

    private void ExportProfilerData()
    {
        var firstFrameIndex = ProfilerDriver.firstFrameIndex;
        var lastFrameIndex = ProfilerDriver.lastFrameIndex;
        ExtractData(firstFrameIndex, lastFrameIndex);
    }

    private void ExportCurrentFrameData()
    {
        if (currentframe > -1)
        {
            ExtractData(currentframe, currentframe);
        }
    }

    private void ExportSelectedFunctionData()
    {
        var firstFrameIndex = ProfilerDriver.firstFrameIndex;
        var lastFrameIndex = ProfilerDriver.lastFrameIndex;
        ExtractData(firstFrameIndex, lastFrameIndex, ProfilerDriver.selectedPropertyPath);
    }

    private void ExtractData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "")
    {
        var profilerData = GetProfilerData(firstFrameIndex, lastFrameIndex, selectedPropertyPath);
        File.WriteAllText(filePath, profilerData.ToString());
    }

    private static ProfilerData GetProfilerData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "")
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
            property.Cleanup();
            profilerData.frames.Add(frameData);
            //Debug.Log(frameData.ToString());
        }
        //Debug.Log(profilerData.ToString());
        return profilerData;
    }
}
