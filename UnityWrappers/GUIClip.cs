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
        public static Rect visibleRect
        {
            get
            {
                Func<Rect> VisibleRect = null; 
                var tyGUIClip = Type.GetType("UnityEngine.GUIClip,UnityEngine");
                if (tyGUIClip != null)
                {
                    var piVisibleRect = tyGUIClip.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (piVisibleRect != null)
                    {
                        var getMethod = piVisibleRect.GetGetMethod(true) ?? piVisibleRect.GetGetMethod(false);
                        VisibleRect = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), getMethod);
                    }
                }

                return VisibleRect();
            }
        }
    }
}
