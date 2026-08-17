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
        private static readonly Type SplitterGuiLayoutType = typeof(Editor).Assembly.GetType("UnityEditor.SplitterGUILayout");

        private static readonly int SplitterHash = "Splitter".GetHashCode();

        private static readonly Type GuiLayoutUtilityType = typeof(GUILayoutUtility);

        private static readonly MethodInfo EndLayoutGroupInfo = GuiLayoutUtilityType.GetMethod(
            "EndLayoutGroup",
            BindingFlags.DeclaredOnly |
            BindingFlags.Static |
            BindingFlags.NonPublic |
            BindingFlags.InvokeMethod);

        private static readonly MethodInfo BeginLayoutGroupInfo = GuiLayoutUtilityType.GetMethod(
            "BeginLayoutGroup",
            BindingFlags.DeclaredOnly |
            BindingFlags.Static |
            BindingFlags.NonPublic |
            BindingFlags.InvokeMethod);
        private static readonly Func<GUIStyle, GUILayoutOption[], Type, object> BeginLayoutGroupDelegate =
            (Func<GUIStyle, GUILayoutOption[], Type, object>)
            Delegate.CreateDelegate(typeof(Func<GUIStyle, GUILayoutOption[], Type, object>), BeginLayoutGroupInfo);

        private static GUISplitterGroup BeginLayoutGroup(GUIStyle style, GUILayoutOption[] options, Type layoutType)
        {
            return new GUISplitterGroup(BeginLayoutGroupDelegate(style, options, layoutType));
        }

        public static readonly Action EndHorizontalSplit = (Action)Delegate.CreateDelegate(typeof(Action), EndLayoutGroupInfo);

        public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            BeginSplit(state, GUIStyle.none, false, options);
        }

        public static void BeginSplit(SplitterState state, GUIStyle style, bool vertical, params GUILayoutOption[] options)
        {
            var guiSplitterGroup = BeginLayoutGroup(style, null, GUISplitterGroup.GuiSplitterGroupType);

            state.ID = GUIUtility.GetControlID(SplitterGUILayout.SplitterHash, FocusType.Passive);
            switch (Event.current.GetTypeForControl(state.ID))
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && Event.current.clickCount == 1)
                    {
                        int num = (!guiSplitterGroup.isVertical) ? ((int)guiSplitterGroup.rect.x) : ((int)guiSplitterGroup.rect.y);
                        int num2 = (!guiSplitterGroup.isVertical) ? ((int)Event.current.mousePosition.x) : ((int)Event.current.mousePosition.y);
                        for (int i = 0; i < state.relativeSizes.Length - 1; i++)
                        {
                            if (((!guiSplitterGroup.isVertical) ? new Rect(state.xOffset + (float)num + (float)state.realSizes[i] - (float)(state.splitSize / 2), guiSplitterGroup.rect.y, (float)state.splitSize, guiSplitterGroup.rect.height) : new Rect(state.xOffset + guiSplitterGroup.rect.x, (float)(num + state.realSizes[i] - state.splitSize / 2), guiSplitterGroup.rect.width, (float)state.splitSize)).Contains(Event.current.mousePosition))
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
                        int num2 = (!guiSplitterGroup.isVertical) ? ((int)Event.current.mousePosition.x) : ((int)Event.current.mousePosition.y);
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
                        int num4 = (!guiSplitterGroup.isVertical) ? ((int)guiSplitterGroup.rect.x) : ((int)guiSplitterGroup.rect.y);
                        for (int j = 0; j < state.relativeSizes.Length - 1; j++)
                        {
                            Rect position = (!guiSplitterGroup.isVertical) ? new Rect(state.xOffset + (float)num4 + (float)state.realSizes[j] - (float)(state.splitSize / 2), guiSplitterGroup.rect.y, (float)state.splitSize, guiSplitterGroup.rect.height) : new Rect(state.xOffset + guiSplitterGroup.rect.x, (float)(num4 + state.realSizes[j] - state.splitSize / 2), guiSplitterGroup.rect.width, (float)state.splitSize);
                            EditorGUIUtility.AddCursorRect(position, (!guiSplitterGroup.isVertical) ? MouseCursor.SplitResizeLeftRight : MouseCursor.ResizeVertical, state.ID);
                            num4 += state.realSizes[j];
                        }
                        break;
                    }
                case EventType.Layout:
                    guiSplitterGroup.state = state;
                    guiSplitterGroup.resetCoords = false;
                    guiSplitterGroup.isVertical = vertical;
                    guiSplitterGroup.ApplyOptions(options);
                    break;
            }
        }

        /// <summary>
        /// Wrapper for unity internal SplitterGUILayout.GUISplitterGroup class
        /// </summary>
        private class GUISplitterGroup
        {
            public static readonly Type GuiSplitterGroupType = SplitterGuiLayoutType.GetNestedType("GUISplitterGroup", BindingFlags.NonPublic);
            private readonly object guiSplitterGroup;
            private SplitterState myState;

            public GUISplitterGroup(object guiSplitterGroup)
            {
                this.guiSplitterGroup = guiSplitterGroup;
            }

            public bool isVertical
            {
                get
                {
                    return (bool)GuiSplitterGroupType.InvokeMember("isVertical",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, guiSplitterGroup, null);
                }
                internal set
                {
                    GuiSplitterGroupType.InvokeMember("isVertical",
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.SetField, null, guiSplitterGroup, new object[] { value });
                }
            }
            public Rect rect
            {
                get
                {
                    return (Rect)GuiSplitterGroupType.InvokeMember("rect",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, guiSplitterGroup, null);
                }
            }
            public bool resetCoords
            {
                get
                {
                    return (bool)GuiSplitterGroupType.InvokeMember("resetCoords",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, guiSplitterGroup, null);
                }
                internal set
                {
                    GuiSplitterGroupType.InvokeMember("resetCoords",
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
                    GuiSplitterGroupType.InvokeMember("state",
                         BindingFlags.DeclaredOnly |
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.SetField, null, guiSplitterGroup, new object[] { value.splitter });
                }
            }

            public void ApplyOptions(GUILayoutOption[] options)
            {
                GuiSplitterGroupType.InvokeMember("ApplyOptions",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, guiSplitterGroup, new object[] { options });
            }
        }
    }
}
