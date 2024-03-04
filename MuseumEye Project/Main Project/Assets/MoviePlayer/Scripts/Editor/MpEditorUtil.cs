//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using UnityEditor;
using System;

namespace MP
{
	/// <summary>
	/// Util methods used in Movie Player package
	/// </summary>
	public class MpEditorUtil
	{
		/// <summary>
		/// Renders TextField with a Open File dialogue button
		/// </summary>
		public static string OpenFileField (string path, string label, string dialogueTitle, string extension = "")
		{
			GUILayout.BeginHorizontal ();
			path = EditorGUILayout.TextField (label, path);
			if (GUILayout.Button (new GUIContent ("...", dialogueTitle), GUILayout.Width (22), GUILayout.Height (13))) {
				string newFileName = EditorUtility.OpenFilePanel (dialogueTitle, GetDirectoryName (path), extension);
				if (!string.IsNullOrEmpty (newFileName))
					path = newFileName;
			}
			GUILayout.EndHorizontal ();
			return path;
		}

		/// <summary>
		/// Renders TextField with a Save File dialogue button
		/// </summary>
		public static string SaveFileField (string fileName, string label, string dialogueTitle, string extension = "")
		{
			GUILayout.BeginHorizontal ();
			fileName = EditorGUILayout.TextField (label, fileName);
			if (GUILayout.Button (new GUIContent ("...", dialogueTitle), GUILayout.Width (22), GUILayout.Height (13))) {
				string newFileName = EditorUtility.SaveFilePanel (dialogueTitle, GetDirectoryName (fileName), GetFileName (fileName), extension);
				if (!string.IsNullOrEmpty (newFileName))
					fileName = newFileName;
			}
			GUILayout.EndHorizontal ();
			return fileName;
		}

		private static string GetDirectoryName (string fileNameWithPath)
		{
			try {
				return System.IO.Path.GetDirectoryName (fileNameWithPath);
			} catch (Exception) {
			}
			return null;
		}

		private static string GetFileName (string fileNameWithPath)
		{
			try {
				return System.IO.Path.GetFileName (fileNameWithPath);
			} catch (Exception) {
			}
			return null;
		}
	}
}
