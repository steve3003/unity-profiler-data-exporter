using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProfilerDataExporter
{
    public class TableGUILayout
    {
        public interface ITableState
        {
            SplitterState SplitterState { get; }
            IEnumerable<string> Headers { get; }
            Vector2 ScrollPosition { get; set; }
            string SortHeader { get; set; }
        }

        private static GUIStyle evenRowStyle = GUI.skin.GetStyle("OL EntryBackEven");
        private static GUIStyle oddRowStyle = GUI.skin.GetStyle("OL EntryBackOdd");
        private static GUIStyle valueStyle = GUI.skin.GetStyle("OL Label");
        private static GUIStyle headerStyle = GUI.skin.GetStyle("OL title");

        public static void BeginTable(ITableState tableState, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();
            SplitterGUILayout.BeginHorizontalSplit(tableState.SplitterState);
            foreach (var header in tableState.Headers)
            {
                bool isSortHeader = header == tableState.SortHeader;
                GUILayout.Toggle(isSortHeader, header, headerStyle);
            }
            SplitterGUILayout.EndHorizontalSplit();
            GUILayout.EndHorizontal();
            GUILayout.Space(1f);
            tableState.ScrollPosition = EditorGUILayout.BeginScrollView(tableState.ScrollPosition, style, options);
        }

        public static void EndTable()
        {
            EditorGUILayout.EndScrollView();
        }

        public static void AddRow(ITableState tableState, int rowIndex, IEnumerable<string> values)
        {
            var splitter = tableState.SplitterState;
            var rowBackgroundStyle = (rowIndex & 1) == 0 ? evenRowStyle : oddRowStyle;
            Rect rowRect = GUILayoutUtility.GetRect(GUIClip.visibleRect.width, 16f);
            rowRect.x += 2;
            if (Event.current.type == EventType.Repaint)
            {
                rowBackgroundStyle.Draw(rowRect, GUIContent.none, false, false, false, false);
                int columnIndex = 0;
                foreach (var value in values)
                {
                    if (columnIndex != 0)
                    {
                        valueStyle.alignment = TextAnchor.MiddleRight;
                    }
                    rowRect.width = (float)splitter.realSizes[columnIndex] - 4f;
                    valueStyle.Draw(rowRect, value, false, false, false, false);
                    rowRect.x += (float)splitter.realSizes[columnIndex];
                    valueStyle.alignment = TextAnchor.MiddleLeft;
                    ++columnIndex;
                }
            }
        }
    }
}
