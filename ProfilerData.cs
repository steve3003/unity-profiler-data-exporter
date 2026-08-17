using System;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using ByteSizeLib;

namespace ProfilerDataExporter
{
    [Serializable]
    public class ProfilerData
    {
        public List<FrameData> frames = new List<FrameData>(300);

        public override string ToString() => JsonUtility.ToJson(this);

        private static IAllocator<ProfilerData> profilerDataAllocator =
            new ObjectPool<ProfilerData>(new BaseFactory<ProfilerData>(), 1);

        public static ProfilerData GetProfilerData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "")
        {
            var profilerData = profilerDataAllocator.Allocate();

            for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(
                           frameIndex,
                           0,
                           HierarchyFrameDataView.ViewModes.Default,
                           HierarchyFrameDataView.columnTotalTime,
                           false))
                {
                    if (view == null || !view.valid) continue;

                    var frameData = FrameData.Create();
                    var itemIds = new List<int>();
                    var rootId = view.GetRootItemID();
                    CollectItems(view, rootId, rootId, itemIds, selectedPropertyPath);

                    for (int i = 0; i < itemIds.Count; ++i)
                        frameData.functions.Add(FunctionData.Create(view, itemIds[i]));

                    profilerData.frames.Add(frameData);
                }
            }
            return profilerData;
        }

        private static void CollectItems(HierarchyFrameDataView view, int rootId, int itemId, List<int> result, string pathFilter)
        {
            if (itemId != rootId)
            {
                var path = view.GetItemPath(itemId);
                if (string.IsNullOrEmpty(pathFilter) || path == pathFilter)
                    result.Add(itemId);
            }
            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            for (int i = 0; i < children.Count; ++i)
                CollectItems(view, rootId, children[i], result, pathFilter);
        }

        public void Clear()
        {
            for (int i = 0; i < frames.Count; ++i) frames[i].Clear();
            frames.Clear();
            profilerDataAllocator.Free(this);
        }
    }

    [Serializable]
    public class FrameData
    {
        public List<FunctionData> functions = new List<FunctionData>(50);

        private static IAllocator<FrameData> frameDataAllocator =
            new ObjectPool<FrameData>(new BaseFactory<FrameData>(), 300);

        public static FrameData Create() => frameDataAllocator.Allocate();

        public void Clear()
        {
            for (int i = 0; i < functions.Count; ++i) functions[i].Clear();
            functions.Clear();
            frameDataAllocator.Free(this);
        }

        public override string ToString() => JsonUtility.ToJson(this);
    }

    [Serializable]
    public class FunctionData
    {
        private static readonly string[]         columnNames = Enum.GetNames(typeof(ProfilerColumn));
        private static readonly ProfilerColumn[] columns     = (ProfilerColumn[])Enum.GetValues(typeof(ProfilerColumn));

        private static IAllocator<FunctionData> functionDataAllocator =
            new ObjectPool<FunctionData>(new BaseFactory<FunctionData>(), 300 * 50);

        public string             functionPath;
        public FunctionDataValue[] values = new FunctionDataValue[columnNames.Length];

        public string GetValue(ProfilerColumn column)
            => FindDataValue(columnNames[(int)column]).value;

        private FunctionDataValue FindDataValue(string name)
        {
            for (int i = 0; i < values.Length; ++i)
                if (values[i] != null && values[i].column == name) return values[i];
            return default(FunctionDataValue);
        }

        public override string ToString() => JsonUtility.ToJson(this);

        public void Clear()
        {
            for (int i = 0; i < values.Length; ++i) if (values[i] != null) values[i].Clear();
            functionPath = string.Empty;
            functionDataAllocator.Free(this);
        }

        public static FunctionData Create(HierarchyFrameDataView view, int itemId)
        {
            var fd = functionDataAllocator.Allocate();
            fd.functionPath = view.GetItemPath(itemId);

            for (int i = 0; i < columns.Length; ++i)
            {
                var col = columns[i];
                if (col == ProfilerColumn.DontSort) continue;

                var fdv = FunctionDataValue.Create();
                fdv.column = columnNames[i];
                switch (col)
                {
                    case ProfilerColumn.FunctionName:
                        fdv.value = view.GetItemName(itemId); break;
                    case ProfilerColumn.GCMemory:
                        fdv.value = ByteSize.FromBytes(view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnGcMemory)).ToString(); break;
                    case ProfilerColumn.TotalPercent:
                        fdv.value = view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnTotalPercent).ToString("F2") + "%"; break;
                    case ProfilerColumn.SelfPercent:
                        fdv.value = view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnSelfPercent).ToString("F2") + "%"; break;
                    case ProfilerColumn.Calls:
                        fdv.value = ((int)view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnCalls)).ToString(); break;
                    case ProfilerColumn.TotalTime:
                        fdv.value = view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnTotalTime).ToString("F2"); break;
                    case ProfilerColumn.SelfTime:
                        fdv.value = view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnSelfTime).ToString("F2"); break;
                }
                fd.values[i] = fdv;
            }
            return fd;
        }
    }

    [Serializable]
    public class FunctionDataValue
    {
        public string column;
        public string value;

        private static IAllocator<FunctionDataValue> functionDataValueAllocator =
            new ObjectPool<FunctionDataValue>(
                new BaseFactory<FunctionDataValue>(),
                300 * 50 * Enum.GetValues(typeof(ProfilerColumn)).Length);

        public static FunctionDataValue Create() => functionDataValueAllocator.Allocate();

        public void Clear()
        {
            functionDataValueAllocator.Free(this);
            column = string.Empty;
            value  = string.Empty;
        }
    }
}
