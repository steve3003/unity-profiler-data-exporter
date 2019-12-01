using System;
using System.Collections.Generic;
using UnityEditorInternal;
#if !UNITY_2019_1_OR_NEWER
using UnityEditorInternal.Profiling;
#endif
using UnityEngine;

namespace ProfilerDataExporter
{
    [Serializable]
    public class ProfilerData
    {
        public List<FrameData> frames = new List<FrameData>(300);

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        private static IAllocator<ProfilerData> profilerDataAllocator = new ObjectPool<ProfilerData>(new BaseFactory<ProfilerData>(), 1);
        private static ProfilerProperty profilerProperty = new ProfilerProperty();

        public static ProfilerData GetProfilerData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "")
        {
            //using (Profiler.AddSample(Profiler.SamplerType.GetProfilerData))
            {
                var profilerSortColumn = ProfilerColumn.TotalTime;
                var viewType = ProfilerViewType.Hierarchy;
                profilerProperty.Cleanup();

                var profilerData = profilerDataAllocator.Allocate();
                for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
                {
#if UNITY_2019_1_OR_NEWER
                    profilerProperty.SetRoot(frameIndex, (int)profilerSortColumn, (int)viewType);
#else
                    profilerProperty.SetRoot(frameIndex, profilerSortColumn, viewType);
#endif
                    profilerProperty.onlyShowGPUSamples = false;

                    var frameData = FrameData.Create();
                    const bool enterChildren = true;
                    while (profilerProperty.Next(enterChildren))
                    {
                        bool shouldSaveProperty = string.IsNullOrEmpty(selectedPropertyPath) || profilerProperty.propertyPath == selectedPropertyPath;
                        if (shouldSaveProperty)
                        {
                            var functionData = FunctionData.Create(profilerProperty);
                            frameData.functions.Add(functionData);
                            //Debug.Log(functionData.ToString());
                        }
                    }
                    profilerProperty.Cleanup();
                    profilerData.frames.Add(frameData);
                    //Debug.Log(frameData.ToString());
                }
                //Debug.Log(profilerData.ToString());
                return profilerData;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < frames.Count; ++i)
            {
                frames[i].Clear();
            }
            frames.Clear();
            profilerDataAllocator.Free(this);
        }
    }

    [Serializable]
    public class FrameData
    {
        public List<FunctionData> functions = new List<FunctionData>(50);

        private static IAllocator<FrameData> frameDataAllocator = new ObjectPool<FrameData>(new BaseFactory<FrameData>(), 300);

        public static FrameData Create()
        {
            return frameDataAllocator.Allocate();
        }

        public void Clear()
        {
            for (int i = 0; i < functions.Count; ++i)
            {
                functions[i].Clear();
            }
            functions.Clear();
            frameDataAllocator.Free(this);
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class FunctionData
    {
        private static readonly string[] columnNames = Enum.GetNames(typeof(ProfilerColumn));
        private static readonly ProfilerColumn[] columns = (ProfilerColumn[])Enum.GetValues(typeof(ProfilerColumn));

        private static IAllocator<FunctionData> functionDataAllocator = new ObjectPool<FunctionData>(new BaseFactory<FunctionData>(), 300 * 50);

        public string functionPath;
        public FunctionDataValue[] values = new FunctionDataValue[columnNames.Length];

        public string GetValue(ProfilerColumn column)
        {
            var columnName = columnNames[(int)column];
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

        public void Clear()
        {
            for (int i = 0; i < values.Length; ++i)
            {
                var functionDataValue = values[i];
                if (functionDataValue != null)
                {
                    functionDataValue.Clear();
                }
            }
            functionPath = string.Empty;
            functionDataAllocator.Free(this);
        }

        public static FunctionData Create(ProfilerProperty property)
        {
            var functionData = functionDataAllocator.Allocate();
            functionData.functionPath = property.propertyPath;
            for (int i = 0; i < columns.Length; ++i)
            {
                var column = columns[i];
#if UNITY_5_5_OR_NEWER
                if (column == ProfilerColumn.DontSort)
                {
                    continue;
                }
#endif
                var functionDataValue = FunctionDataValue.Create();
                functionDataValue.column = columnNames[i];
#if UNITY_2019_1_OR_NEWER
                functionDataValue.value = property.GetColumn((int)column);
#else
                functionDataValue.value = property.GetColumn(column);
#endif

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

        private static IAllocator<FunctionDataValue> functionDataValueAllocator = new ObjectPool<FunctionDataValue>(new BaseFactory<FunctionDataValue>(), 300 * 50 * Enum.GetValues(typeof(ProfilerColumn)).Length);

        public static FunctionDataValue Create()
        {
            return functionDataValueAllocator.Allocate();
        }

        public void Clear()
        {
            functionDataValueAllocator.Free(this);
            column = string.Empty;
            value = string.Empty;
        }
    }
}