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
}
