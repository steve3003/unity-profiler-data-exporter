using System;
using System.Reflection;
using UnityEditor;

namespace ProfilerDataExporter
{
    /// <summary>
    /// Wrapper for unity internal SplitterState class
    /// </summary>
    public class SplitterState
    {
        private static Type splitterStateType = typeof(Editor).Assembly.GetType("UnityEditor.SplitterState");
        public object splitter = null;

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

        public void RealToRelativeSizes()
        {
            splitterStateType.InvokeMember("RealToRelativeSizes",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, null);
        }

        public void DoSplitter(int currentActiveSplitter, int v, int num3)
        {
            splitterStateType.InvokeMember("DoSplitter",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, new object[] { currentActiveSplitter, v, num3 });
        }
    }
}
