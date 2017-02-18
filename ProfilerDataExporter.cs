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

namespace ProfilerDataExporter
{
    public class ProfilerDataExporter : EditorWindow
    {
#if USE_AUTO_REPAINT
    private const float editorUpdateTime = 1f;
    private float lastUpdateTime = 0;
#endif

        private static ProfilerColumn[] columnsToShow = new ProfilerColumn[]
        {
            ProfilerColumn.FunctionName,
            ProfilerColumn.TotalPercent,
            ProfilerColumn.SelfPercent,
            ProfilerColumn.Calls,
            ProfilerColumn.GCMemory,
            ProfilerColumn.TotalTime,
            ProfilerColumn.SelfTime,
        };

        private static Dictionary<ProfilerColumn, string> columnHeaders = new Dictionary<ProfilerColumn, string>
        {
            { ProfilerColumn.FunctionName, "Function"},
            { ProfilerColumn.TotalPercent, "Total"},
            { ProfilerColumn.SelfPercent, "Self"},
            { ProfilerColumn.Calls, "Calls"},
            { ProfilerColumn.GCMemory, "GC Alloc"},
            { ProfilerColumn.TotalTime, "Time ms"},
            { ProfilerColumn.SelfTime, "Self ms"},
        };

        private string filePath = @"profiler_data.json";
        private int currentframe = -1;
        private Type profilerWindowType;
        private FieldInfo currentFrameFieldInfo;
        private Vector2 scrollPosition;
        private FunctionData[] worstSelfTimeFunctions;
        private FunctionData[] worstGCAllocFunctions;
        private ByteSize maxGCAlloc;
        private float maxTime;
        private string calculatedPropertyPath;
        private FunctionTableState worstGCAllocTableState;
        private FunctionTableState worstSelfTimeTableState;

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
                var groupedFunctionData = functionsData.GroupBy(f => f.functionPath);
                worstSelfTimeFunctions =
                    groupedFunctionData
                        .Select(g => g.OrderByDescending(f => float.Parse(f.GetValue(ProfilerColumn.SelfTime))).First())
                        .OrderByDescending(f => float.Parse(f.GetValue(ProfilerColumn.SelfTime)))
                        .ToArray();
                worstGCAllocFunctions =
                    groupedFunctionData
                        .Select(g => g.OrderByDescending(f => ByteSize.Parse(f.GetValue(ProfilerColumn.GCMemory))).First())
                        .OrderByDescending(f => ByteSize.Parse(f.GetValue(ProfilerColumn.GCMemory)))
                        .ToArray();
            }

            if (worstSelfTimeFunctions != null)
            {
                if (worstSelfTimeTableState == null)
                {
                    worstSelfTimeTableState = new FunctionTableState();
                }
                GUILayout.Label("Worst Self Time functions", EditorStyles.boldLabel);
                TableGUILayout.BeginTable(worstSelfTimeTableState, GUI.skin.GetStyle("OL Box"), GUILayout.MinHeight(100f), GUILayout.MaxHeight(500f));
                for (int i = 0; i < worstSelfTimeFunctions.Length; ++i)
                {
                    var values = columnsToShow.Select(c => worstSelfTimeFunctions[i].GetValue(c));
                    TableGUILayout.AddRow(worstSelfTimeTableState, i, values);
                }
                TableGUILayout.EndTable();
            }

            if (worstGCAllocFunctions != null)
            {
                if (worstGCAllocTableState == null)
                {
                    worstGCAllocTableState = new FunctionTableState();
                }
                GUILayout.Label("Worst GC Alloc functions", EditorStyles.boldLabel);
                TableGUILayout.BeginTable(worstGCAllocTableState, GUI.skin.GetStyle("OL Box"), GUILayout.MinHeight(100f), GUILayout.MaxHeight(500f));
                for (int i = 0; i < worstGCAllocFunctions.Length; ++i)
                {
                    var values = columnsToShow.Select(c => worstGCAllocFunctions[i].GetValue(c));
                    TableGUILayout.AddRow(worstGCAllocTableState, i, values);
                }
                TableGUILayout.EndTable();
            }

            var selectedPropertyPath = ProfilerDriver.selectedPropertyPath;
            if (!string.IsNullOrEmpty(selectedPropertyPath))
            {
                if (calculatedPropertyPath != selectedPropertyPath)
                {
                    var functionData = GetProfilerData(firstFrameIndex, lastFrameIndex, selectedPropertyPath);
                    var functionFrameData = functionData.frames.Where(f => f.functions.Count > 0);
                    maxTime = functionFrameData.Max(f => float.Parse(f.functions[0].GetValue(ProfilerColumn.SelfTime)));
                    maxGCAlloc = functionFrameData.Max(f => ByteSize.Parse(f.functions[0].GetValue(ProfilerColumn.GCMemory)));
                    calculatedPropertyPath = selectedPropertyPath;
                }

                GUILayout.Label("Function Statistics", EditorStyles.boldLabel);
                GUILayout.Label(string.Format("Selected function = {0}", selectedPropertyPath));
                GUILayout.Label(string.Format("Max Self Time = {0}", maxTime));
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

        private class FunctionTableState : TableGUILayout.ITableState
        {
            private SplitterState splitterState;

            public FunctionTableState()
            {
                var splitterRelativeSizes = new float[columnsToShow.Length + 1];
                var splitterMinWidths = new int[columnsToShow.Length + 1];
                for (int i = 0; i < columnsToShow.Length; i++)
                {
                    var column = columnHeaders[columnsToShow[i]];
                    splitterMinWidths[i] = (int)GUI.skin.GetStyle("OL title").CalcSize(new GUIContent(column)).x;
                    splitterRelativeSizes[i] = 70f;
                }
                splitterMinWidths[columnsToShow.Length] = 16;
                splitterRelativeSizes[columnsToShow.Length] = 0f;
                if (columnsToShow[0] == ProfilerColumn.FunctionName)
                {
                    splitterRelativeSizes[0] = 400f;
                    splitterMinWidths[0] = 100;
                }
                splitterState = new SplitterState(splitterRelativeSizes, splitterMinWidths, null);
            }

            IEnumerable<string> TableGUILayout.ITableState.Headers
            {
                get
                {
                    return columnHeaders.Values;
                }
            }

            Vector2 TableGUILayout.ITableState.ScrollPosition
            {
                get;
                set;
            }

            SplitterState TableGUILayout.ITableState.SplitterState
            {
                get
                {
                    return splitterState;
                }
            }
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

}