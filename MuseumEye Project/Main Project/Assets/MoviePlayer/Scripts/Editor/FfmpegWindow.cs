//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.ComponentModel;

namespace MP
{
	/// <summary>
	/// Ffmpeg window. The options are limited so that only videos that can be played back by this package can be created.
	/// </summary>
	public class FfmpegWindow : EditorWindow
	{
		#region ----- Arguments for FFMPEG -----

		public string infile = "";
		public string outfile = "Assets/out.avi";
		public string[] videoCodecList = new string[] {
			"mjpeg",
			"png"
		};
		public int videoCodec = 0;
		public int videoQuality = 4; // qscale
		public bool videoResize = false;
		public int videoNewWidth = 854;
		public int videoNewHeight = 480;
		public string[] audioCodecList = new string[] {
			"(no audio)",
			"pcm_s16le",
			"pcm_alaw",
			"pcm_ulaw",
			"pcm_u8"
		};
		public int audioCodec = 1;
		public string[] audioChannelList = new string[] { "mono", "stereo" }; // -1 offset to audioChannels
		public int audioChannels = 2;
		public int audioRate = 24000;

		public string otherArgs = "";
		public bool openDuplicateFrameRemover = false;
		public bool appendBytesExtension = true;

		#endregion

		#region ----- private fields -----

		// if set, the full command is shown too
		private bool showFfmpegCmd = false;

		/// <summary>
		/// Presence of ffmpeg is detected and registered here
		/// </summary>
		private static bool ffmpegDetected = false;
		private static string detectedFfmpegVersion = null;

		/// <summary>
		/// The worker that will hold ffmpeg process
		/// </summary>
		private BackgroundWorker worker;

		/// <summary>
		/// The ffmpeg exit code.
		/// </summary>
		private int ffmpegExitCode;

		#endregion

		#region ----- Unity specific methods -----

		[MenuItem ("Window/Movie Player Tools/Movie Encoder (uses FFMPEG)")]
		static void Init ()
		{
			EditorWindow.GetWindow (typeof(FfmpegWindow));
		}

		void OnGUI ()
		{
			GUILayout.Label ("FFMPEG options", EditorStyles.boldLabel);

			infile = MpEditorUtil.OpenFileField (infile, "Infile", "Open video file");
			outfile = MpEditorUtil.SaveFileField (outfile, "Outfile", "Save video file", "avi");

			EditorGUILayout.Space ();

			videoCodec = EditorGUILayout.Popup ("Video codec", videoCodec, videoCodecList);
			videoQuality = EditorGUILayout.IntSlider ("Video quality", videoQuality, 1, 10);
			videoResize = EditorGUILayout.Toggle ("Video resize", videoResize);
			if (videoResize) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.PrefixLabel (" ");
				videoNewWidth = EditorGUILayout.IntField (videoNewWidth);
				videoNewHeight = EditorGUILayout.IntField (videoNewHeight);
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.Space ();

			audioCodec = EditorGUILayout.Popup ("Audio codec", audioCodec, audioCodecList);
			if (audioCodec > 0) {
				audioChannels = EditorGUILayout.Popup ("Audio channels", audioChannels - 1, audioChannelList) + 1;
				audioRate = EditorGUILayout.IntField ("Audio rate", audioRate);
			}

			EditorGUILayout.Space ();

			otherArgs = EditorGUILayout.TextField ("Other ffmpeg options", otherArgs);

			showFfmpegCmd = EditorGUILayout.Toggle ("Show command", showFfmpegCmd);
			if (showFfmpegCmd) {
				var style = new GUIStyle (GUI.skin.textArea);
				style.wordWrap = true;
				//style.stretchHeight = true;
				GUILayout.TextArea (ffmpegPath + " " + BuildFfmpegArgs (), style, GUILayout.MinHeight (60));
			}

			EditorGUILayout.Space ();
			appendBytesExtension = EditorGUILayout.Toggle("Append .bytes extension", appendBytesExtension);
			openDuplicateFrameRemover = EditorGUILayout.Toggle("Open dup frame remover", openDuplicateFrameRemover);

			// show some helpful tips
			if(!string.IsNullOrEmpty(outfile) && outfile.Contains("Assets") && !appendBytesExtension) {
				EditorGUILayout.HelpBox("Helpful tip. The outfile is saved to Assets/ folder, you may want to tick" +
					"\"append .bytes extension\"", MessageType.Info);
			}
			if(openDuplicateFrameRemover) {
				EditorGUILayout.HelpBox("The Duplicate Frame Remover will help to further reduce file size " +
					"of MJPEG encoded video. This option conveniently opens DFM window after FFMPEG is done", MessageType.Info);
			}

			if (!ffmpegDetected) {
				detectedFfmpegVersion = DetectFfmpeg ();
				ffmpegDetected = true;
			}

			if (detectedFfmpegVersion != null) {
				if (worker == null) {
					GUI.enabled = !string.IsNullOrEmpty (infile) && !string.IsNullOrEmpty (outfile);
					if (GUILayout.Button ("Run ffmpeg")) {
						RunFfmpegCmd ();
					}
					GUI.enabled = true;
				} else {
					if (GUILayout.Button ("Kill ffmpeg process")) {
						KillFfmpeg ();
					}
				}

				EditorGUILayout.LabelField ("Detected", detectedFfmpegVersion);
			} else {
				#if UNITY_EDITOR_OSX
				EditorGUILayout.HelpBox ("This tool depends on FFMPEG. Can't find it from default install location " + ffmpegPath,
				                        MessageType.Error);
				#else
				EditorGUILayout.HelpBox ("This tool depends on FFMPEG. Please install it and add it to your PATH",
				                         MessageType.Error);
				#endif
				if (GUILayout.Button ("ffmpeg.org/download.html")) {
					Application.OpenURL ("http://www.ffmpeg.org/download.html");
				}
			}
		}

		void Update ()
		{
			// detect when ffmpeg process completes
			if (worker != null && !worker.IsBusy)
			{
				worker = null;
				if(ffmpegExitCode == 0)
				{
					Debug.Log ("ffmpeg exited successfully");

					// adds .bytes extension if requested
					if(appendBytesExtension) {
						AddBytesExtension(outfile);
					}

					// opens up duplicate frame remover window for convenience
					if(openDuplicateFrameRemover)
					{
						var dupFrameRemoverWindow = EditorWindow.GetWindow<DuplicateFrameRemoverWindow>();
						dupFrameRemoverWindow.srcPath = FinalOutfile;
						dupFrameRemoverWindow.dstPath = Path.GetDirectoryName(FinalOutfile) + "/smaller_" + Path.GetFileName(FinalOutfile);
					}
				}
				else {
					Debug.LogError ("ffmpeg exited with error (code " + ffmpegExitCode + ")");
				}
				GetWindow (typeof(FfmpegWindow)).Repaint ();
			}
		}

		#endregion

		#region ----- other methods -----

		private string FinalOutfile { get { return appendBytesExtension ? outfile + ".bytes" : outfile; } }

		/// <summary>
		/// Builds a string with ffmpeg arguments
		/// </summary>
		public string BuildFfmpegArgs ()
		{
			var sb = new System.Text.StringBuilder ();

			if (!string.IsNullOrEmpty (infile)) {
				sb.Append (" -i \"").Append (infile).Append ('"');
			}
			sb.Append (" -vcodec ").Append (videoCodecList [videoCodec]);
			sb.Append (" -q:v ").Append (videoQuality);
			if (videoResize) {
				sb.Append (" -vf scale=").Append (videoNewWidth).Append (":").Append (videoNewHeight);
			}
			if (audioCodec > 0) {
				sb.Append (" -acodec ").Append (audioCodecList [audioCodec]);
				sb.Append (" -ac ").Append (audioChannels);
				sb.Append (" -ar ").Append (audioRate);
			} else {
				sb.Append (" -an");
			}
			//if(overwrite) {
				sb.Append (" -y");
			//}
			sb.Append (" ").Append (otherArgs);

			if (!string.IsNullOrEmpty (outfile)) {
				sb.Append (" \"").Append (outfile).Append ('"');
			}
			return sb.ToString ();
		}

		/// <summary>
		/// Runs the ffmpeg command
		/// </summary>
		public void RunFfmpegCmd ()
		{
			// can we run it?
			if (worker != null) {
				Debug.LogError ("Please command is still running. Wait it to complete or kill it first");
				return;
			}

			if(File.Exists(FinalOutfile)) {
				if (!EditorUtility.DisplayDialog ("Overwrite?", FinalOutfile + " exists. Overwrite?", "Yes", "No")) {
					Debug.Log ("Output file '" + FinalOutfile + "' exists, didn't overwrite");
					return;
				}
			}

			string ffmpegArgs = BuildFfmpegArgs ();
			Debug.Log ("Running: " + ffmpegPath + " " + ffmpegArgs);

			// start ffmpeg process inside a background worker so that it won't block the UI
			worker = new BackgroundWorker ();
			worker.WorkerReportsProgress = true;
			worker.WorkerSupportsCancellation = true;
			worker.DoWork += delegate(object sender, DoWorkEventArgs e) {
				// start the process.
				// currently we're doing it the easy way and not capturing standard input and output
				// for creating better UX.
				var process = new System.Diagnostics.Process ();
				process.StartInfo = new System.Diagnostics.ProcessStartInfo (ffmpegPath, ffmpegArgs);
				process.StartInfo.CreateNoWindow = false;
				process.StartInfo.UseShellExecute = true;
				process.Start ();
				do {
					process.WaitForExit (200);
					if (worker.CancellationPending) {
						process.Kill ();
					}
				} while(!process.HasExited);
				ffmpegExitCode = process.ExitCode;
			};
			worker.RunWorkerAsync ();
		}

		/// <summary>
		/// Kills the ffmpeg process
		/// </summary>
		public void KillFfmpeg ()
		{
			if (worker != null && worker.IsBusy) {
				if (EditorUtility.DisplayDialog ("KILL ffmpeg?", "It is still running. Are you sure?", "Yes", "No")) {
					worker.CancelAsync ();
				}
			}
		}

		/// <summary>
		/// Detects the presence and version of installed ffmpeg.
		/// If ffmpeg is not available, NULL is returned.
		/// </summary>
		private static string DetectFfmpeg ()
		{
			try {
				var process = new System.Diagnostics.Process ();
				process.StartInfo = new System.Diagnostics.ProcessStartInfo (ffmpegPath, "-version");
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.Start ();

				string firstLine = process.StandardOutput.ReadLine ();
				process.WaitForExit (800); // use 800ms timeout, just for safety
				return firstLine;
			} catch (Exception e) {
				Debug.LogWarning ("ffmpeg not found. " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Adds the .bytes extension to given filename. Preserves target file .meta so
		/// that scene and prefab references are preserved when target file is overwritten.
		/// </summary>
		private static void AddBytesExtension(string filename)
		{
			// not using FileUtil, because they don't provide enough control, have
			// poor error checking and won't do exactly what their name says they should.
			// FileUtil.MoveFileOrDirectory(filename, filename + ".bytes");
			// FileUtil.ReplaceFile(filename, filename + ".bytes");

			if(!File.Exists(filename)) {
				Debug.LogWarning("Can't add .bytes extension to nonexistant file: " + filename);
				return;
			}

			string filenameMeta = filename + ".meta";
			string filenameBytes = filename + ".bytes";
			string filenameBytesMeta = filename + ".bytes.meta";

			// handles the case when target file already exists
			if(File.Exists(filenameBytes)) {
				File.Delete(filenameBytes);
			}

			// move the file
			File.Move(filename, filenameBytes);

			// only move the .meta file if target file already doesn't have it and there is
			// something to move, this tries to preserve references in scene and prefab
			if(File.Exists(filenameMeta)) {
				if(!File.Exists(filenameBytesMeta)) {
					File.Move(filenameMeta, filenameBytesMeta);
				}
				File.Delete(filenameMeta);
			}
			Debug.Log(".bytes extension added");
		}

		/// <summary>
		/// ffmpeg command line program name
		/// </summary>
		private static string ffmpegPath {
			get {
				#if UNITY_EDITOR_OSX
				return "/usr/local/bin/ffmpeg";
				#else
				return "ffmpeg";
				#endif
			}
		}


		#endregion
	}
}
