using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ProfilerDataExporter
{
    /// <summary>
    /// Wrapper for unity internal SplitterGUILayout class
    /// </summary>
    public class SplitterGUILayout
    {
        private static Type splitterGUILayoutType = typeof(Editor).Assembly.GetType("UnityEditor.SplitterGUILayout");

        private static int splitterHash = "Splitter".GetHashCode();

        public static void EndHorizontalSplit()
        {
            guiLayoutUtilityType.InvokeMember("EndLayoutGroup",
                BindingFlags.DeclaredOnly |
                BindingFlags.Static | BindingFlags.NonPublic |
                BindingFlags.InvokeMethod, null, null, null);
        }

        public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            BeginSplit(state, GUIStyle.none, false, options);
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

        /// <summary>
        /// Wrapper for unity internal SplitterGUILayout.GUISplitterGroup class
        /// </summary>
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

            public void ApplyOptions(GUILayoutOption[] options)
            {
                guiSplitterGroupType.InvokeMember("ApplyOptions",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, guiSplitterGroup, new object[] { options });
            }
        }
    }
}
