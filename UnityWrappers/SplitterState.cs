using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ProfilerDataExporter
{
    /// <summary>
    /// Wrapper for unity internal SplitterState class
    /// </summary>
    public class SplitterState
    {
        private static readonly Type SplitterStateType = typeof(Editor).Assembly.GetType("UnityEditor.SplitterState");
        public object splitter = null;
        public int[] realSizes;

        private static readonly FieldInfo RealSizesInfo = SplitterStateType.GetField(
            "realSizes",
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.GetField);

        public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            splitter = SplitterStateType.InvokeMember(null,
            BindingFlags.DeclaredOnly |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.CreateInstance, null, null, new object[] { relativeSizes, minSizes, maxSizes });
            realSizes = (int[])RealSizesInfo.GetValue(splitter);
        }

        public SplitterState(object splitter)
        {
            this.splitter = splitter;
            realSizes = (int[])RealSizesInfo.GetValue(splitter);
        }

        public int ID
        {
            get
            {
                return (int)SplitterStateType.InvokeMember("ID",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                SplitterStateType.InvokeMember("ID",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }

        public float xOffset
        {
            get
            {
                return (float)SplitterStateType.InvokeMember("xOffset",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public int splitSize
        {
            get
            {
                return (int)SplitterStateType.InvokeMember("splitSize",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public float[] relativeSizes
        {
            get
            {
                return (float[])SplitterStateType.InvokeMember("relativeSizes",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public int splitterInitialOffset
        {
            get
            {
                return (int)SplitterStateType.InvokeMember("splitterInitialOffset",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                SplitterStateType.InvokeMember("splitterInitialOffset",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }
        public int currentActiveSplitter
        {
            get
            {
                return (int)SplitterStateType.InvokeMember("currentActiveSplitter",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                SplitterStateType.InvokeMember("currentActiveSplitter",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }

        public void RealToRelativeSizes()
        {
            SplitterStateType.InvokeMember("RealToRelativeSizes",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, null);
        }

        public void DoSplitter(int currentActiveSplitter, int v, int num3)
        {
            SplitterStateType.InvokeMember("DoSplitter",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, new object[] { currentActiveSplitter, v, num3 });
        }
    }
}
