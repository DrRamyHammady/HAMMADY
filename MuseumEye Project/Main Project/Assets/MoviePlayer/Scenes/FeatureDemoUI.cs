//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------

using UnityEngine;
using System.Collections;
using System.IO;
using MP;

public class FeatureDemoUI : MonoBehaviour
{
	public enum Mode { FILE, STREAM }
	public Mode mode = Mode.FILE;

	public MoviePlayer moviePlayer;
	public MovieStreamer movieStreamer;

	public string fileLoadDir = "C:/";
	public string streamLoadUrl = "http://";

	private float slider;
	private Rect loadWindowRect = new Rect (20, 20, 240, 500);
	private Rect infoWindowRect = new Rect (1 * 250 + 20, 20, 240, 500);
	private Rect statsWindowRect = new Rect (2 * 250 + 20, 20, 240, 170);
	private Rect controlsWindowRect = new Rect (2 * 250 + 20, 200, 240, 170);

	private Mode lastMode = Mode.FILE;

#if !UNITY_WINRT
#if !UNITY_WEBPLAYER && !UNITY_WEBGL
	private Vector2 loadScrollPos = Vector2.zero;
	private string lastLoadDir = "";
	private string loadError = null;
#endif

	private string streamConnectTimeoutStr = "10";

	private string[] dirsInDir;
	private string[] filesInDir;

	private LoadOptions loadOptions = LoadOptions.Default;
#endif

	private MoviePlayerBase moviePlayerBase {
		get {
			return mode == Mode.FILE ? (MoviePlayerBase)moviePlayer : (MoviePlayerBase)movieStreamer;
		}
	}

	#if !UNITY_WEBPLAYER && !UNITY_WEBGL && !UNITY_WINRT
	void DoLoadWindowForFile()
	{
		GUILayout.Label ("Directory");
		fileLoadDir = GUILayout.TextArea (fileLoadDir, GUILayout.MinHeight (36));
		
		if (lastLoadDir != fileLoadDir) {
			try {
				#if MP_DEBUG
				Debug.Log ("Loading files from directory: " + fileLoadDir);
				#endif
				dirsInDir = Directory.GetDirectories(fileLoadDir);
				filesInDir = Directory.GetFiles(fileLoadDir);
				loadError = null;
			} catch (System.Exception e) {
				loadError = e.Message;
			}
		}
		lastLoadDir = fileLoadDir;
		
		if (!string.IsNullOrEmpty (loadError)) {
			GUILayout.Label (loadError);
		} else {
			loadScrollPos = GUILayout.BeginScrollView (loadScrollPos, false, true);
			
			var buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			if(dirsInDir != null) {
				if(GUILayout.Button("..", buttonStyle)) {
					fileLoadDir = Directory.GetParent(fileLoadDir).FullName;
				}
				for (int i = 0; i < dirsInDir.Length; i++) {
					string fullPath = Path.GetFullPath(dirsInDir[i]);
					if (GUILayout.Button (Path.GetFileName(fullPath) + "/", buttonStyle))
					{
						#if MP_DEBUG
						Debug.Log("Diving into " + fullPath);
						#endif
						fileLoadDir = fullPath.Replace("\\", "/");
					}
				}
			}
			if (filesInDir != null) {
				for (int i = 0; i < filesInDir.Length; i++) {
					if (GUILayout.Button (Path.GetFileName(filesInDir [i]), buttonStyle))
					{
						// These two lines let us load a raw MJPEG stream. Usually you
						// also need to set some other parameters too, for example
						// frame dimensions, but in minimum, you need to set codec.
						// If the movie to be loaded is NOT a raw stream, then the Demux
						// will override these values with the ones read from the stream.
						loadOptions.videoStreamInfo = new VideoStreamInfo();
						loadOptions.videoStreamInfo.codecFourCC = MP.Decoder.VideoDecoderMJPEG.FOURCC_MJPG;
						
						((MoviePlayer)moviePlayerBase).Load (filesInDir [i], loadOptions);
					}
				}
			}
			GUILayout.EndScrollView ();
			
			GUILayout.BeginHorizontal();
			loadOptions.skipAudio = GUILayout.Toggle(loadOptions.skipAudio, "Skip audio", GUILayout.Width(120));
			loadOptions.preloadAudio = GUILayout.Toggle(loadOptions.preloadAudio, "Preload audio");
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			loadOptions.skipVideo = GUILayout.Toggle(loadOptions.skipVideo, "Skip video", GUILayout.Width(120));
			loadOptions._3DSound = GUILayout.Toggle(loadOptions._3DSound, "3D sound");
			GUILayout.EndHorizontal();
		}
	}
	#endif

	#if !UNITY_WINRT
	void DoLoadWindowForUrl()
	{
		var movieStreamer = (MovieStreamer)moviePlayerBase;
		
		GUILayout.Label ("Steram URL");
		streamLoadUrl = GUILayout.TextArea (streamLoadUrl, GUILayout.MinHeight (36));
		
		GUILayout.BeginHorizontal();
		GUILayout.Label ("Connect timeout seconds");
		streamConnectTimeoutStr = GUILayout.TextArea (streamConnectTimeoutStr);
		GUILayout.EndHorizontal();
		GUILayout.Space(10);

		// not giving any load options
		if(!movieStreamer.IsConnected)
		{
			if (GUILayout.Button ("Connect to URL and Play", GUILayout.Height(36)))
			{
				if(!float.TryParse(streamConnectTimeoutStr, out loadOptions.connectTimeout)) {
					streamConnectTimeoutStr = "10";
					loadOptions.connectTimeout = 10;
				}
				movieStreamer.Load(streamLoadUrl, loadOptions);
				movieStreamer.play = true;
			}
		}
		else {
			if (GUILayout.Button ("Disconnect", GUILayout.Height(36)))
			{
				movieStreamer.Unload();
			}
		}
		GUILayout.Space(10);
		GUILayout.Label("Status (read only)");
		GUILayout.TextArea(movieStreamer.status == null ? "" : movieStreamer.status, GUILayout.ExpandHeight(true));
	}
	#endif

	void DoLoadWindow (int windowID)
	{
		#if UNITY_WINRT
		GUILayout.Label ("Loading files or opening network is not supported in winrt");
		#else
		if(moviePlayerBase is MoviePlayer)
		{
			#if UNITY_WEBPLAYER || UNITY_WEBGL
			GUILayout.Label ("Loading files from disk\nis not supported in web player");
			#else
			DoLoadWindowForFile();
			#endif
		}
		else if(moviePlayerBase is MovieStreamer) {
			DoLoadWindowForUrl();
		}
		#endif
		GUI.DragWindow (new Rect (0, 0, 4000, 4000));
	}

	void DoStatsWindow (int windowID)
	{
		// show stats
		if (moviePlayerBase.movie != null && moviePlayerBase.movie.demux != null)
		{
			if (moviePlayerBase.movie.videoDecoder != null)
			{
				float load = moviePlayerBase.movie.videoDecoder.lastFrameDecodeTime * moviePlayerBase.movie.demux.videoStreamInfo.framerate * 100;
				float lastFrameDecodeTime = moviePlayerBase.movie.videoDecoder.lastFrameDecodeTime * 1000f;
				float totalVideoDecodeTime = moviePlayerBase.movie.videoDecoder.totalDecodeTime * 1000f;
				float lastFrameSizeBytes = moviePlayerBase.movie.videoDecoder.lastFrameSizeBytes;

				DrawLabelValue("Thread load", load.ToString("0") + "%", 120);
				DrawLabelValue("Frames skipped", moviePlayerBase.framesSkipped.ToString(), 120);
				DrawLabelValue("Sync events", moviePlayerBase.syncEvents.ToString(), 120);

				DrawLabelValue("Last frame decode", lastFrameDecodeTime.ToString() + " ms", 120);
				DrawLabelValue("Total video decode", totalVideoDecodeTime.ToString() + " ms", 120);
				DrawLabelValue("Last frame size", lastFrameSizeBytes.ToString() + " bytes", 120);
			}
			if (moviePlayerBase.movie.audioDecoder != null)
			{
				float totalAudioDecodeTime = moviePlayerBase.movie.audioDecoder.totalDecodeTime * 1000f;
				DrawLabelValue("Total audio decode", totalAudioDecodeTime.ToString() + " ms", 120);
			}
		} else {
			GUILayout.Label ("No movie loaded");
		}
		GUI.DragWindow (new Rect (0, 0, 4000, 4000));
	}

	void DoControlsWindow (int windowID)
	{
		int ivalue;
		float fvalue;

		// show MoviePlayer specific controls (playing from a file)
		if(moviePlayerBase is MoviePlayer)
		{
			var moviePlayer = (MoviePlayer)moviePlayerBase;

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Video time", GUILayout.Width (100));
			if (float.TryParse (GUILayout.TextField (moviePlayer.videoTime.ToString ()), out fvalue)) {
				moviePlayer.videoTime = fvalue;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Video frame", GUILayout.Width (100));
			if (int.TryParse (GUILayout.TextField (moviePlayer.videoFrame.ToString ()), out ivalue)) {
				moviePlayer.videoFrame = ivalue;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Loop", GUILayout.Width (100));
			moviePlayer.loop = GUILayout.Toggle (moviePlayer.loop, "");
			GUILayout.EndHorizontal ();

			var audioSource = moviePlayer.GetComponent<AudioSource> ();
			if (audioSource != null) {

				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Mute audio", GUILayout.Width (100));
				audioSource.mute = GUILayout.Toggle (audioSource.mute, "");
				GUILayout.EndHorizontal ();
			}
		}
		// show MovieStreamer specific controls (playing from a web stream)
		else if(moviePlayerBase is MovieStreamer)
		{
			GUILayout.Label("Not much here (use Load window to connect to http streams)");
		}

		moviePlayerBase.drawToScreen = GUILayout.Toggle(moviePlayerBase.drawToScreen, " draw directly to screen");
		if(moviePlayerBase.drawToScreen) {
			moviePlayerBase.screenMode = (MoviePlayerBase.ScreenMode)GUILayout.SelectionGrid(
				(int)moviePlayerBase.screenMode, System.Enum.GetNames(typeof(MoviePlayerBase.ScreenMode)), 4);
		}

		GUI.DragWindow (new Rect (0, 0, 4000, 4000));
	}

	void DrawLabelValue(string label, string value, int labelWidth = 100)
	{
		GUILayout.BeginHorizontal ();
		GUILayout.Label (label, GUILayout.Width (labelWidth));
		GUILayout.Label (value);
		GUILayout.EndHorizontal ();
	}

	void DoInfoWindow (int windowID)
	{
		if (moviePlayerBase.movie != null)
		{
			var demux = moviePlayerBase.movie.demux;
			DrawLabelValue("Demux", demux != null ? demux.GetType().ToString() : "N/A");

			if(demux != null)
			{
				if(demux.hasVideo)
				{
					var videoStreamInfo = moviePlayerBase.movie.demux.videoStreamInfo;
					var videoDecoder = moviePlayerBase.movie.videoDecoder;

					DrawLabelValue("Video fourCC", RiffParser.FromFourCC(videoStreamInfo.codecFourCC));
					DrawLabelValue("Video decoder", videoDecoder!=null ? videoDecoder.GetType().ToString() : "N/A");

					DrawLabelValue("bitsPerPixel", videoStreamInfo.bitsPerPixel.ToString());
					DrawLabelValue("frameCount", videoStreamInfo.frameCount.ToString());
					DrawLabelValue("frame size", videoStreamInfo.width + "x" + videoStreamInfo.height);
					DrawLabelValue("framerate", videoStreamInfo.framerate.ToString());
					DrawLabelValue("lengthBytes", videoStreamInfo.lengthBytes.ToString());
					DrawLabelValue("lengthSeconds", videoStreamInfo.lengthSeconds.ToString());
				}
				else {
					GUILayout.Label("No video stream found");
				}

				if(demux.hasAudio)
				{
					var audioStreamInfo = moviePlayerBase.movie.demux.audioStreamInfo;
					var audioDecoder = moviePlayerBase.movie.audioDecoder;

					DrawLabelValue("Audio fourCC", RiffParser.FromFourCC(audioStreamInfo.codecFourCC));
					DrawLabelValue("Audio decoder", audioDecoder!=null ? audioDecoder.GetType().ToString() : "N/A");

					DrawLabelValue("audioFormat", audioStreamInfo.audioFormat.ToString("X"));
					DrawLabelValue("channels", audioStreamInfo.channels.ToString());
					DrawLabelValue("sampleCount", audioStreamInfo.sampleCount.ToString());
					DrawLabelValue("sampleSize", audioStreamInfo.sampleSize.ToString());
					DrawLabelValue("sampleRate", audioStreamInfo.sampleRate.ToString());
					DrawLabelValue("lengthBytes", audioStreamInfo.lengthBytes.ToString());
					DrawLabelValue("lengthSeconds", audioStreamInfo.lengthSeconds.ToString());
				}
				else {
					GUILayout.Label("No audio stream found");
				}
			}
		}
		else {
			GUILayout.Label ("No movie loaded");
		}
		GUI.DragWindow (new Rect (0, 0, 4000, 4000));
	}

	void DoPlaybackControls ()
	{
		// show the Play/Stop button
		if (GUI.Button (new Rect (0, Screen.height - 40, 40, 40), moviePlayerBase.play ? "||" : ">")) {
			moviePlayerBase.play = !moviePlayerBase.play;
		}

		// show MovieStreamer specific controls (playing from a web stream)
		if(moviePlayerBase is MoviePlayer)
		{
			var moviePlayer = (MoviePlayer)moviePlayerBase;

			// show the playhead slider control if movie is loaded
			if (moviePlayer.movie != null && moviePlayer.movie.demux != null && moviePlayer.movie.demux.hasVideo) {
				slider = moviePlayer.videoFrame;
				slider = GUI.HorizontalSlider (new Rect (50, Screen.height - 20, Screen.width - 60, 20), slider, 0,
				                              moviePlayer.movie.demux.videoStreamInfo.frameCount);
				moviePlayer.videoFrame = (int)slider;
			}
		}
		else {
			// fow MovieStream, show only the latest status line
			var movieStreamer = (MovieStreamer)moviePlayerBase;
			GUI.Label(new Rect (50, Screen.height - 40, 200, 20), movieStreamer.bytesReceived + " bytes received");
			GUI.Label (new Rect (50, Screen.height - 20, Screen.width - 60, 20),
			               movieStreamer.status == null ? "" : movieStreamer.status);
		}
	}

	bool hideAllUI = false;
	public bool showLoadWindow;
	public bool showStatsWindow;
	public bool showControlsWindow;
	public bool showInfoWindow;

	void OnGUI ()
	{
		if(hideAllUI) return;

		if (showLoadWindow) {
			loadWindowRect = GUI.Window (0, loadWindowRect, DoLoadWindow, "Load");
		}
		if (showStatsWindow) {
			statsWindowRect = GUI.Window (1, statsWindowRect, DoStatsWindow, "Stats");
		}
		if (showControlsWindow) {
			controlsWindowRect = GUI.Window (2, controlsWindowRect, DoControlsWindow, "Controls");
		}
		if (showInfoWindow) {
			infoWindowRect = GUI.Window (3, infoWindowRect, DoInfoWindow, "Info");
		}

		DoPlaybackControls ();

		// me
		GUI.Label (new Rect (Screen.width - 150, 10, 200, 70),
			"MoviePlayer " + MoviePlayer.PACKAGE_VERSION + "\n" +
		    "feature demo\n" +
			"by SHUU Games 2014");
		mode = (Mode)GUI.SelectionGrid(new Rect (Screen.width - 150, 66, 138, 22),
		                               (int)mode, new string[] {"FILE", "STREAM"}, 2);

		#if (UNITY_WII || UNITY_IPHONE || UNITY_ANDROID || UNITY_PS3 || UNITY_XBOX360 || UNITY_BLACKBERRY || UNITY_WP8)
		GUILayout.BeginArea(new Rect(Screen.width - 150, Screen.height - 150, 150, 150));
		if(GUILayout.Button("Show load window")) showLoadWindow = !showLoadWindow;
		if(GUILayout.Button("Show stats window")) showStatsWindow = !showStatsWindow;
		if(GUILayout.Button("Show controls window")) showControlsWindow = !showControlsWindow;
		if(GUILayout.Button("Show info window")) showInfoWindow = !showInfoWindow;
		if(GUILayout.Button("Hide all UI")) hideAllUI = true;
		GUILayout.EndArea();
		#else
		GUI.Label (new Rect (Screen.width - 150, Screen.height - 105, 200, 105),
		           "L - Load window\n" +
		           "S - Stats window\n" +
		           "C - Controls window\n" +
		           "I - Info window\n" +
		           "H - show/hide all UI");
		#endif
	}

	void Update ()
	{
		if(mode != lastMode) {
			moviePlayer.gameObject.SetActive(mode == Mode.FILE);
			movieStreamer.gameObject.SetActive(mode == Mode.STREAM);
			lastMode = mode;
		}

		if (Input.GetKeyUp (KeyCode.L)) {
			showLoadWindow = !showLoadWindow;
		}
		if (Input.GetKeyUp (KeyCode.S)) {
			showStatsWindow = !showStatsWindow;
		}
		if (Input.GetKeyUp (KeyCode.C)) {
			showControlsWindow = !showControlsWindow;
		}
		if (Input.GetKeyUp (KeyCode.I)) {
			showInfoWindow = !showInfoWindow;
		}
		if (Input.GetKeyUp (KeyCode.H)) {
			hideAllUI = !hideAllUI;
		}

		if(hideAllUI && Input.GetMouseButtonDown(0)) {
			hideAllUI = false;
		}

		if (Input.GetKey (KeyCode.Escape)) {
			Application.Quit ();
		}
	}
}
