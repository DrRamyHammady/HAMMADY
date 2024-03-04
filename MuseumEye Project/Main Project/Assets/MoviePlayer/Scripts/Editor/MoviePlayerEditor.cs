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
	/// Custom Inspector for MoviePlayer component
	/// </summary>
	[CustomEditor(typeof(MoviePlayer))]
	public class MoviePlayerEditor : Editor
	{
		private bool statsFoldout;
		private TextAsset lastSource;
		private int sourceSize;
		private bool reloadFail;

		private MoviePlayer mp { get { return target as MoviePlayer; } }

		private string GetSourceInfo ()
		{
			// Has the source changed? if so, cache it's length, because it's too resouce hungry to call freely.
			if (mp.source != lastSource) {
				if (mp.source != null) {
					sourceSize = mp.source.bytes.Length;
				}
				lastSource = mp.source;
			}

			// build source info string
			StringBuilder infoSb = new StringBuilder ("");
			if (mp.source == null) {
				infoSb.Append ("Movie not loaded");
			} else {
				// Source info
				infoSb.Append (mp.source.name).Append (" ").Append (((float)sourceSize / 1000000f).ToString ("#.##")).Append (" Mb\n");

				// Video info
				infoSb.Append ("Video: ");
				if (mp.movie != null && mp.movie.demux != null && mp.movie.demux.videoStreamInfo != null) {
					var videoStreamInfo = mp.movie.demux.videoStreamInfo;
					infoSb.AppendFormat ("{0} {1}fps {2}x{3} ({4:#} kbps)\n", RiffParser.FromFourCC (videoStreamInfo.codecFourCC),
					                    videoStreamInfo.framerate, videoStreamInfo.width, videoStreamInfo.height,
					                    0.001f * videoStreamInfo.lengthBytes / videoStreamInfo.lengthSeconds);
				} else {
					infoSb.Append ("N/A\n");
				}

				// Audio info
				infoSb.Append ("Audio: ");
				if (mp.movie != null && mp.movie.demux != null && mp.movie.demux.audioStreamInfo != null) {
					var audioStreamInfo = mp.movie.demux.audioStreamInfo;
					var audioFourCC = RiffParser.FromFourCC (audioStreamInfo.codecFourCC);
					infoSb.AppendFormat ("{0} {1}kHz {2}ch ({3:#} kbps)", audioFourCC,
					                    audioStreamInfo.sampleRate / 1000f, audioStreamInfo.channels,
					                    0.001f * audioStreamInfo.lengthBytes / audioStreamInfo.lengthSeconds);
				} else {
					infoSb.Append ("N/A");
				}
			}
			return infoSb.ToString ();
		}

		public override void OnInspectorGUI ()
		{
			HandleVersionUpgrades();

#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
			Undo.SetSnapshotTarget(mp, "MoviePlayer change");
			Undo.CreateSnapshot();
#else
			Undo.RecordObject (mp, "MoviePlayer change");
#endif

			// source properties
			mp.source = EditorGUILayout.ObjectField ("Source*", mp.source, typeof(TextAsset), false) as TextAsset;
			mp.audioSource = EditorGUILayout.ObjectField ("Audio source*", mp.audioSource, typeof(AudioClip), false) as AudioClip;

			// show info about source, video and audio
			string sourceInfo = GetSourceInfo ();
			if (reloadFail) {
				EditorGUILayout.HelpBox ("Loading movie failed, see Console", MessageType.Error);
			} else {
				EditorGUILayout.HelpBox (sourceInfo, MessageType.Info);
			}

			// where to bind the framebuffer
			mp.material = EditorGUILayout.ObjectField ("Material*", mp.material, typeof(Material), false) as Material;
			mp.texturePropertyName = EditorGUILayout.TextField ("Texture property*", mp.texturePropertyName);

			mp.loadOptions.preloadVideo = EditorGUILayout.Toggle ("Preload video*", mp.loadOptions.preloadVideo);

			// audio properties
			mp.loadOptions.preloadAudio = EditorGUILayout.Toggle ("Preload audio*", mp.loadOptions.preloadAudio);

			#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6
			// Since Unity 5 this option in on AudioSource and controllable during playback
			mp.loadOptions._3DSound = EditorGUILayout.Toggle ("3D sound*", mp.loadOptions._3DSound);
			#endif

			// should we draw it on the screen
			mp.drawToScreen = EditorGUILayout.Toggle ("Draw to screen", mp.drawToScreen);
			if (mp.drawToScreen) {
				mp.screenMode = (MoviePlayer.ScreenMode)EditorGUILayout.EnumPopup ("Screen mode", mp.screenMode);
				if (mp.screenMode == MoviePlayer.ScreenMode.CustomRect) {
					mp.customScreenRect = EditorGUILayout.RectField ("Custom rect", mp.customScreenRect);
				}
				mp.screenGuiDepth = EditorGUILayout.IntField ("Screen GUI depth", mp.screenGuiDepth);
			}

			// some action buttons
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("(re)load *")) {
				reloadFail = !mp.Reload ();
				lastVideoFrame = -1; // force seeking, so that a preview frame gets decoded
				lastDecodedFrame = -1; // make the HandlePreview decode a frame at the same playhead position
			}
			if (GUILayout.Button ("unload")) {
				mp.Unload ();
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Space ();

			// seeking properties
			mp.videoTime = EditorGUILayout.FloatField ("Current video time", mp.videoTime);
			mp.videoFrame = EditorGUILayout.IntField ("Current video frame", mp.videoFrame);
			mp.play = EditorGUILayout.Toggle ("Play", mp.play);
			mp.loop = EditorGUILayout.Toggle ("Loop", mp.loop);
			
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
#else
			// if the movie is loaded, then show the playhead "progress" bar
			if (mp.movie != null && mp.movie.demux != null && mp.movie.demux.videoStreamInfo != null) {
				string progressText = mp.videoTime.ToString ("0.00") + " / " + mp.movie.demux.videoStreamInfo.lengthSeconds.ToString ("0.00");
				var rect = EditorGUILayout.GetControlRect ();
				EditorGUI.ProgressBar (rect, (float)mp.videoFrame / (float)mp.movie.demux.videoStreamInfo.frameCount, progressText);
			}
#endif

			// if the movie is loaded, show LIVE stats from decoders
			if (mp.movie != null) {
				statsFoldout = EditorGUILayout.Foldout (statsFoldout, "Show decode stats");
				if (statsFoldout) {
					EditorGUILayout.LabelField ("  Frames dropped", mp.framesSkipped.ToString ());
					EditorGUILayout.LabelField ("  Sync events", mp.syncEvents.ToString ());
					if (mp.movie != null) {
						if (mp.movie.videoDecoder != null) {
							EditorGUILayout.LabelField ("  Last frame decode time", mp.movie.videoDecoder.lastFrameDecodeTime.ToString ());
							EditorGUILayout.LabelField ("  Total video decode time", mp.movie.videoDecoder.totalDecodeTime.ToString ());
						}
						if (mp.movie.audioDecoder != null) {
							EditorGUILayout.LabelField ("  Total audio decode time", mp.movie.audioDecoder.totalDecodeTime.ToString ());
						}
					}
				}

				// clamp frame index and time to valid bounds
				if (mp.movie.demux != null && mp.movie.demux.hasVideo) {
					mp.videoFrame = Mathf.Clamp (mp.videoFrame, 0, mp.movie.demux.videoStreamInfo.frameCount);
					mp.videoTime = Mathf.Clamp (mp.videoTime, 0, mp.movie.demux.videoStreamInfo.lengthSeconds);

					// show preview if we're in edit mode
					if (!Application.isPlaying) {
						HandlePreview ();
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

		private int lastVideoFrame;
		private float lastVideoTime;
		private int lastDecodedFrame;

		private void HandlePreview ()
		{
			// has the playhead moved?
			bool seekedByVideoFrame = mp.videoFrame != lastVideoFrame;
			bool seekedByVideoTime = mp.videoTime != lastVideoTime;

			if (seekedByVideoFrame) {
				mp.videoTime = mp.videoFrame / mp.framerate;
			} else if (seekedByVideoTime) {
				mp.videoFrame = Mathf.RoundToInt (mp.videoTime * mp.framerate);
			}

			lastVideoFrame = mp.videoFrame;
			lastVideoTime = mp.videoTime;

			if (seekedByVideoFrame || seekedByVideoTime) {
				if (mp.videoFrame != lastDecodedFrame)
				{
					// just decode the video frame, no audio
					if(!mp.loadOptions.preloadVideo && mp.movie.videoDecoder != null) {
						mp.movie.videoDecoder.Decode (mp.videoFrame);
					}
					mp.UpdateRendererUVRect();

					lastDecodedFrame = mp.videoFrame;
				}
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
