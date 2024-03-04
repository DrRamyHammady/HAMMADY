//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using UnityEditor;
using System.IO;
using MP;
using MP.AVI;

namespace MP
{
	/// <summary>
	/// AVI file remuxer. It detects duplicate frames and updates AVI index, so that
	/// those are only stored once, therefore reducing file size. It also removes
	/// unknown streams and cleans up any unnecessary garbage from the AVI.
	/// </summary>
	public class DuplicateFrameRemoverWindow : EditorWindow
	{
		// slider limiters. for special cases, increase these. for normal use, these should be ok
		private const float MAX_IMAGE_DIFF = 20f;
		private const int MAX_PIXEL_DIFF = 150;
		private const int MAX_TONE_COMPARE_DISTRUST = 6;

		// common and advanced parameters
		public string srcPath = "";
		public string dstPath = "Assets/out.avi";
		public string logDuplicatesPath = "";
		public bool discardAudio;
		public DuplicateFrameFinder.Options options = DuplicateFrameFinder.Options.Default;

		// status
		private bool running;
		private bool livePreviewFoldout;
		private bool advancedSettingsFoldout;

		// other instances
		private AviRemux remux;
		private DuplicateFrameFinder dupFinder;
		private Movie movie;
		private Texture2D framebuffer;
		private Stream dstStream;

		[MenuItem ("Window/Movie Player Tools/Duplicate Frame Remover")]
		static void Init ()
		{
			EditorWindow.GetWindow (typeof(DuplicateFrameRemoverWindow));
		}

		void OnGUI ()
		{
			// show processing options. some of them are editable while running.
			GUILayout.Label ("Duplicate frame remover (MJPEG/PCM AVI remux)", EditorStyles.boldLabel);

			if (!running) {
				srcPath = MpEditorUtil.OpenFileField (srcPath, "Source path", "Source file");
				dstPath = MpEditorUtil.SaveFileField (dstPath, "Destination path", "Destination file");
				discardAudio = EditorGUILayout.Toggle ("Discard audio", discardAudio);
			} else {
				EditorGUILayout.LabelField ("Source path", srcPath);
				EditorGUILayout.LabelField ("Destination path", dstPath);
				EditorGUILayout.LabelField ("Discard audio", discardAudio ? "true" : "false");
			}
			options.maxLookbackFrames = EditorGUILayout.IntField ("Max lookback frames", options.maxLookbackFrames);

			// advances procession options
			advancedSettingsFoldout = EditorGUILayout.Foldout (advancedSettingsFoldout, "Advanced settings");
			if (advancedSettingsFoldout) {
				options.maxImageDiff = EditorGUILayout.Slider ("  Max image diff", options.maxImageDiff, 0, MAX_IMAGE_DIFF);
				options.maxPixelDiff = EditorGUILayout.IntSlider ("  Max pixel diff", options.maxPixelDiff, 0, MAX_PIXEL_DIFF);
				options.toneCompareDistrust = EditorGUILayout.IntSlider ("  Tone compare distrust", options.toneCompareDistrust, 0, MAX_TONE_COMPARE_DISTRUST);
				if (!running) {
					options.pixelCacheSize = EditorGUILayout.IntField ("  Pixel cache size", options.pixelCacheSize);
					EditorGUILayout.LabelField (" ", (int)(3.69f * options.pixelCacheSize) + "Mb (720p estimate)");
					if (options.pixelCacheSize < options.maxLookbackFrames || options.pixelCacheSize > 1000) {
						EditorGUILayout.HelpBox ("For optimal performance, pixel cache should be larger than \"max lookback frames\"," +
							"but if it's too large, you may get out of memory errors", MessageType.Warning);
					}

					logDuplicatesPath = MpEditorUtil.SaveFileField (logDuplicatesPath, "  Log duplicates to file", "Log duplicates to file");
				} else {
					int pixelCacheMb = movie.demux.videoStreamInfo.width * movie.demux.videoStreamInfo.height * 4 * options.pixelCacheSize / 1000000;
					EditorGUILayout.LabelField ("  Pixel cache size", options.pixelCacheSize.ToString () + " (" + pixelCacheMb + "Mb)");
					EditorGUILayout.LabelField ("  Log duplicates to file", logDuplicatesPath);
				}
			}

			// controls
			if (!running) {
				GUI.enabled = File.Exists (srcPath) && !string.IsNullOrEmpty (dstPath);
				if (GUILayout.Button ("Process file")) {
					running = true;
					if (File.Exists (dstPath)) {
						running = EditorUtility.DisplayDialog ("File exists. Overwrite?", dstPath, "Yes", "No");
					}
					if (running) {
						File.WriteAllText (dstPath, ""); // just empty the file
						StartProcessing ();
						Debug.Log ("AVI remux started");
					}
				}
				GUI.enabled = true;
			} else {
				if (GUILayout.Button ("Stop")) {
					StopProcessing ();
					Debug.Log ("AVI remux stopped");
					running = false;
				}
			}

			// if we have a duplicate finder (we're not necessarily running), then show progress and stats
			if (dupFinder != null) {
				// progress bar
				string progressStr = dupFinder.framesProcessed + "/" + (float)movie.demux.videoStreamInfo.frameCount;

				#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
				EditorGUILayout.LabelField("Progress", progressStr);
				#else
				float progress = (float)dupFinder.framesProcessed / (float)movie.demux.videoStreamInfo.frameCount;
				var rect = EditorGUILayout.GetControlRect ();
				EditorGUI.ProgressBar (rect, progress, progressStr);
				#endif

				// duplicates found so far
				EditorGUILayout.LabelField ("Duplicates found", dupFinder.stats.duplicateCount.ToString ());

				// love preview and stats foldout
				livePreviewFoldout = EditorGUILayout.Foldout (livePreviewFoldout, "Live preview and stats");
				if (livePreviewFoldout && framebuffer != null) {
					int height = 160 * movie.demux.videoStreamInfo.height / movie.demux.videoStreamInfo.width;
					GUILayout.Box (framebuffer, GUILayout.Width (160), GUILayout.Height (height));

					EditorGUILayout.LabelField ("Frames partially compared", dupFinder.stats.framesPartiallyComparedPercent.ToString ("0.00") + " %");
					EditorGUILayout.LabelField ("Frames fully compared", dupFinder.stats.framesFullyComparedPercent.ToString ("0.00") + " %");
					EditorGUILayout.LabelField ("Pixel cache hits", dupFinder.stats.pixelCacheHitPercent.ToString ("0.00") + " %");
				}
			}
		}

		/// <summary>
		/// Starts the processing.
		/// </summary>
		void StartProcessing ()
		{
			movie = new Movie ();
			movie.sourceStream = File.OpenRead (srcPath);
		
			dstStream = File.OpenWrite (dstPath);
			remux = new AviRemux ();
		
			// create and initialize demux for the source data
			movie.demux = Demux.forSource (movie.sourceStream);
			movie.demux.Init (movie.sourceStream);

			// create video stream and decoder too. without a decoder we can't access pixels to compare
			if (!movie.demux.hasVideo) {
				throw new MpException ("Remux needs video stream inside an AVI");
			}
			movie.videoDecoder = VideoDecoder.CreateFor (movie.demux.videoStreamInfo);
			movie.videoDecoder.Init (out framebuffer, movie.demux);

			// create a remux. this will write into dstStream
			bool outputHasAudio = movie.demux.hasAudio && !discardAudio;
			remux.Init (dstStream, movie.demux.videoStreamInfo, outputHasAudio ? movie.demux.audioStreamInfo : null);

			// create a duplicate finder. most of the options control how the frames are actually compared
			options.otherStreamsAvailable = outputHasAudio;
			dupFinder = new DuplicateFrameFinder (movie.videoDecoder, framebuffer, 0, movie.demux.videoStreamInfo.frameCount, options);

			// if we want a to log the duplicate indexes into a file, then clear the file first
			if (!string.IsNullOrEmpty (logDuplicatesPath)) {
				File.WriteAllText (logDuplicatesPath, "# Duplicate frame index for " + srcPath + "\n");
			}
		}

		/// <summary>
		/// Stops the processing. This can be called any time to properly close the
		/// AVI file so that it'll be playable anywhere.
		/// </summary>
		void StopProcessing ()
		{
			remux.Shutdown ();
			dstStream.Close ();
		}

		/// <summary>
		/// Reads one frame from srcStream, performs duplicate check, writes to dstStream.
		/// Call it repeatedly until all frames are processed (until false is returned).
		/// </summary>
		bool ProcessNextFrame ()
		{
			if (!dupFinder.Progress ()) {
				return false;
			}

			var dups = dupFinder.duplicates;
		
			int lastProcessedFrameIndex = dupFinder.framesProcessed - 1;
			int lookbackFrameIndex = dups [lastProcessedFrameIndex];
		
			if (!string.IsNullOrEmpty (logDuplicatesPath)) {
				using (var file = File.AppendText(logDuplicatesPath)) {
					file.WriteLine (lastProcessedFrameIndex + " " + lookbackFrameIndex);
				}
			}
		
			byte[] buf;
			if (lookbackFrameIndex < 0 || !remux.WriteLookbackVideoFrame (lookbackFrameIndex)) {
				movie.demux.VideoPosition = lastProcessedFrameIndex;
				int bytesRead = movie.demux.ReadVideoFrame (out buf);
				remux.WriteNextVideoFrame (buf, bytesRead);
			}
		
			if (!discardAudio && movie.demux.hasAudio) {
				int wantCnt = Mathf.RoundToInt (movie.demux.audioStreamInfo.sampleRate / movie.demux.videoStreamInfo.framerate);
				movie.demux.AudioPosition = lastProcessedFrameIndex * wantCnt;
				int samplesRead = movie.demux.ReadAudioSamples (out buf, wantCnt);
				remux.WriteNextAudioSamples (buf, samplesRead * movie.demux.audioStreamInfo.sampleSize);
			}
			return true;
		}

		void Update ()
		{
			if (running) {
				if (dupFinder == null) {
					Debug.LogError ("References lost, probably due to script script recompilation. Aborting");
					running = false;
					return;
				}

				if (!ProcessNextFrame ()) {
					StopProcessing ();
					running = false;
					Debug.Log ("AVI remux finished");
				}

				// we have the window, because no one else is calling Update() than MoviePacker window
				GetWindow<DuplicateFrameRemoverWindow> ().Repaint ();
			}
		}
	}
}
