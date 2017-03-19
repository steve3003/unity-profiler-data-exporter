using System;
using System.Reflection;
using UnityEngine;

namespace ProfilerDataExporter
{
    /// <summary>
    /// Wrapper for unity internal GUIClip class
    /// </summary>
    public class GUIClip
    {
        private static readonly Type GuiClipType = typeof(GameObject).Assembly.GetType("UnityEngine.GUIClip");

        private static readonly MethodInfo VisibleRectMethodInfo = GuiClipType.GetProperty(
            "visibleRect",
            BindingFlags.DeclaredOnly |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.GetProperty)
            .GetGetMethod();

        private static readonly Func<Rect> VisibleRectDelegate = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), VisibleRectMethodInfo);

        public static Rect visibleRect
        {
            get
            {
                return VisibleRectDelegate();
            }
        }
    }
}
