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
		private static readonly Func<Rect> VisibleRectDelegate;

		static GUIClip()
		{
			var guiClipType = Type.GetType("UnityEngine.GUIClip,UnityEngine");

			var visibleRectMethodInfo = guiClipType.GetProperty(
				"visibleRect",
				BindingFlags.DeclaredOnly |
				BindingFlags.Static |
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.GetProperty)
				.GetGetMethod(true);

			VisibleRectDelegate = (Func<Rect>)Delegate.CreateDelegate(typeof(Func<Rect>), visibleRectMethodInfo);
		}

		public static Rect visibleRect
		{
			get
			{
				return VisibleRectDelegate();
			}
		}
	}
}
