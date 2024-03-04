//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

namespace MP
{
	/// <summary>
	/// Custom Inspector for MovieStreamer component
	/// </summary>
	[CustomEditor(typeof(MovieStreamer))]
	public class MovieStreamerEditor : Editor
	{
		private bool statsFoldout;

		private MovieStreamer mp { get { return target as MovieStreamer; } }

		public override void OnInspectorGUI ()
		{
			HandleVersionUpgrades();

#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
			Undo.SetSnapshotTarget(mp, "MovieStreamer change");
			Undo.CreateSnapshot();
#else
			Undo.RecordObject (mp, "MovieStreamer change");
#endif

			// source properties
			mp.sourceUrl = EditorGUILayout.TextField ("Source*", mp.sourceUrl);
			mp.loadOptions.connectTimeout = EditorGUILayout.FloatField ("Connect timeout", mp.loadOptions.connectTimeout);

			// where to bind the framebuffer
			mp.material = EditorGUILayout.ObjectField ("Material*", mp.material, typeof(Material), false) as Material;
			mp.texturePropertyName = EditorGUILayout.TextField ("Texture property*", mp.texturePropertyName);

			// audio properties
			mp.loadOptions._3DSound = EditorGUILayout.Toggle ("3D sound*", mp.loadOptions._3DSound);

			// should we draw it on the screen
			mp.drawToScreen = EditorGUILayout.Toggle ("Draw to screen", mp.drawToScreen);
			if (mp.drawToScreen) {
				mp.screenMode = (MoviePlayer.ScreenMode)EditorGUILayout.EnumPopup ("Screen mode", mp.screenMode);
				if (mp.screenMode == MoviePlayer.ScreenMode.CustomRect) {
					mp.customScreenRect = EditorGUILayout.RectField ("Custom rect", mp.customScreenRect);
				}
				mp.screenGuiDepth = EditorGUILayout.IntField ("Screen GUI depth", mp.screenGuiDepth);
			}

			mp.play = EditorGUILayout.Toggle ("Play", mp.play);

			// some action buttons
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("(Re)connect")) {
				mp.ReConnect ();
			}
			if (GUILayout.Button ("Disconnect")) {
				mp.Unload ();
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			if (mp.movie != null) {
				// background thread status
				GUILayout.Label (mp.status);

				// if the movie is loaded, show LIVE stats from decoders
				statsFoldout = EditorGUILayout.Foldout (statsFoldout, "Show decode stats");
				if (statsFoldout) {
					EditorGUILayout.LabelField ("  Frames dropped", mp.framesSkipped.ToString ());
					EditorGUILayout.LabelField ("  Sync events", mp.syncEvents.ToString ());
					if (mp.movie != null) {
						if (mp.movie.videoDecoder != null) {
							EditorGUILayout.LabelField ("  Last frame decode time", mp.movie.videoDecoder.lastFrameDecodeTime.ToString ());
							EditorGUILayout.LabelField ("  Total video decode time", mp.movie.videoDecoder.totalDecodeTime.ToString ());
							EditorGUILayout.LabelField ("  Last frame size bytes", mp.movie.videoDecoder.lastFrameSizeBytes.ToString ());
						}
						if (mp.movie.audioDecoder != null) {
							EditorGUILayout.LabelField ("  Total audio decode time", mp.movie.audioDecoder.totalDecodeTime.ToString ());
						}
					}
				}
			}

			if (GUI.changed) {
				EditorUtility.SetDirty (target);
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
				Undo.RestoreSnapshot();
#endif
			}
		}

		void HandleVersionUpgrades()
		{
			// temporarily disable obsolete warning
			#pragma warning disable 618
			
			// 0.6 -> 0.7: copy deprecated otherMaterial over to material
			if(mp.material == null && mp.otherMaterial != null) {
				mp.material = mp.otherMaterial;
			}
			
			#pragma warning restore 618
		}
	}
}
