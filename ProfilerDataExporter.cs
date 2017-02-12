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
using System.Globalization;

public class ProfilerDataExporter : EditorWindow
{
#if USE_AUTO_REPAINT
    private const float editorUpdateTime = 1f;
    private float lastUpdateTime = 0;
#endif

    private static ProfilerColumn[] columnsToShow =
    {
        ProfilerColumn.FunctionName,
        ProfilerColumn.TotalPercent,
        ProfilerColumn.SelfPercent,
        ProfilerColumn.Calls,
        ProfilerColumn.GCMemory,
        ProfilerColumn.TotalTime,
        ProfilerColumn.SelfTime,
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
    private Vector2 worstSelfTimeScrollPosition;
    private Vector2 worstGCAllocScrollPosition;
    private SplitterState splitter;

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
            if (splitter == null)
            {
                var splitterRelativeSizes = new float[columnsToShow.Length + 1];
                var splitterMinWidths = new int[columnsToShow.Length + 1];
                for (int i = 0; i < columnsToShow.Length; i++)
                {
                    splitterMinWidths[i] = (int)GUI.skin.GetStyle("OL title").CalcSize(new GUIContent(columnsToShow[i].ToString())).x;
                    splitterRelativeSizes[i] = 70f;
                }
                splitterMinWidths[columnsToShow.Length] = 16;
                splitterRelativeSizes[columnsToShow.Length] = 0f;
                if (columnsToShow[0] == ProfilerColumn.FunctionName)
                {
                    splitterRelativeSizes[0] = 400f;
                    splitterMinWidths[0] = 100;
                }
                splitter = new SplitterState(splitterRelativeSizes, splitterMinWidths, null);
            }
            GUILayout.Label("Worst Self time functions", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            SplitterGUILayout.BeginHorizontalSplit(splitter);
            for (int i = 0; i < columnsToShow.Length; ++i)
            {
                var column = columnsToShow[i];
                GUILayout.Toggle(false, column.ToString(), GUI.skin.GetStyle("OL title"));
            }
            SplitterGUILayout.EndHorizontalSplit();
            GUILayout.EndHorizontal();
            GUILayout.Space(1f);
            worstSelfTimeScrollPosition = EditorGUILayout.BeginScrollView(worstSelfTimeScrollPosition, GUI.skin.GetStyle("OL Box"), GUILayout.MinHeight(100f), GUILayout.MaxHeight(500f));
            for (int i = 0; i < worstSelfTimeFunctions.Length; ++i)
            {
                var function = worstSelfTimeFunctions[i];
                GUIStyle rowBackgroundStyle = null;
                if ((i & 1) == 0)
                {
                    rowBackgroundStyle = GUI.skin.GetStyle("OL EntryBackEven");
                }
                else
                {
                    rowBackgroundStyle = GUI.skin.GetStyle("OL EntryBackOdd");
                }
                Rect rowRect = new Rect(1f, 16f * i, GUIClip.visibleRect.width, 16f);
                if (Event.current.type == EventType.Repaint)
                {
                    rowBackgroundStyle.Draw(rowRect, GUIContent.none, false, false, false, false);
                    var numberLabel = GUI.skin.GetStyle("OL Label");
                    for (int j = 0; j < columnsToShow.Length; j++)
                    {
                        var column = columnsToShow[j];
                        if (column != ProfilerColumn.FunctionName)
                        {
                            numberLabel.alignment = TextAnchor.MiddleRight;
                        }
                        rowRect.width = (float)splitter.realSizes[j] - 4f;
                        numberLabel.Draw(rowRect, function.GetValue(column), false, false, false, false);
                        rowRect.x += (float)splitter.realSizes[j];
                        numberLabel.alignment = TextAnchor.MiddleLeft;
                    }
                }
            }
            //foreach (var f in worstSelfTimeFunctions)
            //{
            //    var name = f.functionPath;
            //    var selfTime = f.GetValue(ProfilerColumn.SelfTime);
            //    GUILayout.Label(string.Format("{1,-10} {0}", name, selfTime));
            //}
            EditorGUILayout.EndScrollView();
        }

        if (worstGCAllocFunctions != null)
        {
            GUILayout.Label("Worst GC Alloc functions", EditorStyles.boldLabel);
            worstGCAllocScrollPosition = EditorGUILayout.BeginScrollView(worstGCAllocScrollPosition, GUILayout.MinHeight(100f), GUILayout.MaxHeight(500f));
            foreach (var f in worstGCAllocFunctions)
            {
                var name = f.functionPath;
                var gcAlloc = f.GetValue(ProfilerColumn.GCMemory);
                GUILayout.Label(string.Format("{1,-10} {0}", name, gcAlloc));
            }
            EditorGUILayout.EndScrollView();
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

    private class SplitterGUILayout
    {
        private static Type splitterGUILayoutType = typeof(Editor).Assembly.GetType("UnityEditor.SplitterGUILayout");

        private static int splitterHash = "Splitter".GetHashCode();

        public static void EndHorizontalSplit()
        {
            guiLayoutUtilityType.InvokeMember("EndLayoutGroup",
                BindingFlags.DeclaredOnly |
                BindingFlags.Static | BindingFlags.NonPublic |
                BindingFlags.InvokeMethod, null, null, null);
            //GUILayout.EndHorizontal();
            //splitterGUILayoutType.InvokeMember("EndHorizontalSplit",
            //    BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, null);
        }

        public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            BeginSplit(state, GUIStyle.none, false, options);
            //GUILayout.BeginHorizontal();
            //splitterGUILayoutType.InvokeMember("BeginSplit",
            //    BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new object[] { state.splitter, GUIStyle.none, false, options });
        }

        public static void BeginSplit(SplitterState state, GUIStyle style, bool vertical, params GUILayoutOption[] options)
        {
            var gUISplitterGroup = BeginLayoutGroup(style, null, GUISplitterGroup.guiSplitterGroupType);

            state.ID = GUIUtility.GetControlID(SplitterGUILayout.splitterHash, FocusType.Passive);
            switch (Event.current.GetTypeForControl(state.ID))
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && Event.current.clickCount == 1)
                    {
                        int num = (!gUISplitterGroup.isVertical) ? ((int)gUISplitterGroup.rect.x) : ((int)gUISplitterGroup.rect.y);
                        int num2 = (!gUISplitterGroup.isVertical) ? ((int)Event.current.mousePosition.x) : ((int)Event.current.mousePosition.y);
                        for (int i = 0; i < state.relativeSizes.Length - 1; i++)
                        {
                            if (((!gUISplitterGroup.isVertical) ? new Rect(state.xOffset + (float)num + (float)state.realSizes[i] - (float)(state.splitSize / 2), gUISplitterGroup.rect.y, (float)state.splitSize, gUISplitterGroup.rect.height) : new Rect(state.xOffset + gUISplitterGroup.rect.x, (float)(num + state.realSizes[i] - state.splitSize / 2), gUISplitterGroup.rect.width, (float)state.splitSize)).Contains(Event.current.mousePosition))
                            {
                                state.splitterInitialOffset = num2;
                                state.currentActiveSplitter = i;
                                GUIUtility.hotControl = state.ID;
                                Event.current.Use();
                                break;
                            }
                            num += state.realSizes[i];
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == state.ID)
                    {
                        GUIUtility.hotControl = 0;
                        state.currentActiveSplitter = -1;
                        state.RealToRelativeSizes();
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == state.ID && state.currentActiveSplitter >= 0)
                    {
                        int num2 = (!gUISplitterGroup.isVertical) ? ((int)Event.current.mousePosition.x) : ((int)Event.current.mousePosition.y);
                        int num3 = num2 - state.splitterInitialOffset;
                        if (num3 != 0)
                        {
                            state.splitterInitialOffset = num2;
                            state.DoSplitter(state.currentActiveSplitter, state.currentActiveSplitter + 1, num3);
                        }
                        Event.current.Use();
                    }
                    break;
                case EventType.Repaint:
                    {
                        int num4 = (!gUISplitterGroup.isVertical) ? ((int)gUISplitterGroup.rect.x) : ((int)gUISplitterGroup.rect.y);
                        for (int j = 0; j < state.relativeSizes.Length - 1; j++)
                        {
                            Rect position = (!gUISplitterGroup.isVertical) ? new Rect(state.xOffset + (float)num4 + (float)state.realSizes[j] - (float)(state.splitSize / 2), gUISplitterGroup.rect.y, (float)state.splitSize, gUISplitterGroup.rect.height) : new Rect(state.xOffset + gUISplitterGroup.rect.x, (float)(num4 + state.realSizes[j] - state.splitSize / 2), gUISplitterGroup.rect.width, (float)state.splitSize);
                            EditorGUIUtility.AddCursorRect(position, (!gUISplitterGroup.isVertical) ? MouseCursor.SplitResizeLeftRight : MouseCursor.ResizeVertical, state.ID);
                            num4 += state.realSizes[j];
                        }
                        break;
                    }
                case EventType.Layout:
                    gUISplitterGroup.state = state;
                    gUISplitterGroup.resetCoords = false;
                    gUISplitterGroup.isVertical = vertical;
                    gUISplitterGroup.ApplyOptions(options);
                    break;
            }
        }

        private static Type guiLayoutUtilityType = typeof(GUILayoutUtility);

        private static GUISplitterGroup BeginLayoutGroup(GUIStyle style, GUILayoutOption[] options, Type layoutType)
        {
            return new GUISplitterGroup(guiLayoutUtilityType.InvokeMember("BeginLayoutGroup",
                BindingFlags.DeclaredOnly |
                BindingFlags.Static | BindingFlags.NonPublic |
                BindingFlags.InvokeMethod, null, null, new object[] { style, options, layoutType }));
        }

        private class GUISplitterGroup
        {
            public static Type guiSplitterGroupType = splitterGUILayoutType.GetNestedType("GUISplitterGroup", BindingFlags.NonPublic);
            private object guiSplitterGroup;
            private SplitterState myState;

            public GUISplitterGroup(object guiSplitterGroup)
            {
                this.guiSplitterGroup = guiSplitterGroup;
            }

            public bool isVertical
            {
                get
                {
                    return (bool)guiSplitterGroupType.InvokeMember("isVertical",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, guiSplitterGroup, null);
                }
                internal set
                {
                    guiSplitterGroupType.InvokeMember("isVertical",
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.SetField, null, guiSplitterGroup, new object[] { value });
                }
            }
            public Rect rect
            {
                get
                {
                    return (Rect)guiSplitterGroupType.InvokeMember("rect",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, guiSplitterGroup, null);
                }
            }
            public bool resetCoords
            {
                get
                {
                    return (bool)guiSplitterGroupType.InvokeMember("resetCoords",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, guiSplitterGroup, null);
                }
                internal set
                {
                    guiSplitterGroupType.InvokeMember("resetCoords",
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.SetField, null, guiSplitterGroup, new object[] { value });
                }
            }
            public SplitterState state
            {
                get
                {
                    return myState;
                }
                internal set
                {
                    myState = value;
                    guiSplitterGroupType.InvokeMember("state",
                         BindingFlags.DeclaredOnly |
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.SetField, null, guiSplitterGroup, new object[] { value.splitter });
                }
            }

            internal void ApplyOptions(GUILayoutOption[] options)
            {
                guiSplitterGroupType.InvokeMember("ApplyOptions",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, guiSplitterGroup, new object[] { options });
            }
        }
    }

    private class GUIClip
    {
        private static Type guiClipType = typeof(GameObject).Assembly.GetType("UnityEngine.GUIClip");

        public static Rect visibleRect
        {
            get
            {
                var result = guiClipType.InvokeMember("visibleRect",
                    BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty, null, null, null);
                return (Rect)result;
            }
        }
    }

    private class SplitterState
    {
        private static Type splitterStateType = typeof(Editor).Assembly.GetType("UnityEditor.SplitterState");
        public object splitter = null;

        public int[] realSizes
        {
            get
            {
                return (int[])splitterStateType.InvokeMember("realSizes",
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }

        public int ID
        {
            get
            {
                return (int)splitterStateType.InvokeMember("ID",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                splitterStateType.InvokeMember("ID",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }

        public float xOffset
        {
            get
            {
                return (float)splitterStateType.InvokeMember("xOffset",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public int splitSize
        {
            get
            {
                return (int)splitterStateType.InvokeMember("splitSize",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public float[] relativeSizes
        {
            get
            {
                return (float[])splitterStateType.InvokeMember("relativeSizes",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public int splitterInitialOffset
        {
            get
            {
                return (int)splitterStateType.InvokeMember("splitterInitialOffset",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                splitterStateType.InvokeMember("splitterInitialOffset",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }
        public int currentActiveSplitter
        {
            get
            {
                return (int)splitterStateType.InvokeMember("currentActiveSplitter",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                splitterStateType.InvokeMember("currentActiveSplitter",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }

        public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            splitter = splitterStateType.InvokeMember(null,
            BindingFlags.DeclaredOnly |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.CreateInstance, null, null, new object[] { relativeSizes, minSizes, maxSizes });
        }

        public SplitterState(object splitter)
        {
            this.splitter = splitter;
        }

        internal void RealToRelativeSizes()
        {
            splitterStateType.InvokeMember("RealToRelativeSizes",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, null);
        }

        internal void DoSplitter(int currentActiveSplitter, int v, int num3)
        {
            splitterStateType.InvokeMember("DoSplitter",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, new object[] { currentActiveSplitter, v, num3 });
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
