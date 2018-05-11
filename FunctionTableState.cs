using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace ProfilerDataExporter
{
    public class FunctionTableState : TableGUILayout.ITableState
    {
        private SplitterState splitterState;
        private IEnumerable<string> columnHeaders;

        public FunctionTableState(ProfilerColumn[] columnsToShow, Dictionary<ProfilerColumn, string> columnHeaders)
        {
            this.columnHeaders = columnHeaders.Values;
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
                return columnHeaders;
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
}
