//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using System.Collections;
using MP;
using MP.AVI;
using System;
using System.IO;
using System.ComponentModel;// for simple threading

public class ScriptingDemoUI : MonoBehaviour
{
#if UNITY_WINRT
// These examples read and write files and since Windows Store build target doesn't support
// filesystem none of the examples here compile.
#else

	public string infile = "Assets/MoviePlayer/Sample movies/Sintel (480p mjpeg, 2ch24khz alaw).avi.bytes";
	public string outfile = "out.bytes";
	public string frameOutFile = "out.png";
	public int frame = 150;
	public string httpStreamUrl = "http://194.126.108.66:8883/1396114247132"; // just one traffic camera

	public bool runInSeparateThread;

	[ContextMenu("Load file and play forever (play mode)")]
	void LoadFileAndPlayForever ()
	{
		if (!Application.isPlaying) return;

		// Using MoviePlayer is the easiest way to play back a movie and it requires
		// zero lines of code because all can be set up an controlled in Inspector.
		// Here however we create that component from a script.
		var moviePlayer = GetComponent<MoviePlayer> ();
		if (moviePlayer == null) {
			moviePlayer = gameObject.AddComponent<MoviePlayer> ();
		}

		// The component has multiple Load overloads capable of playing back files,
		// binary TextAssets, byte[] arrays and System.IO.Streams. Optionally
		// a LoadOptions argument can be used to give more context to what and how
		// to load. For example if you're loading raw pcm audio, then there's no
		// way to know what is the sample rate, sample size and channel count without
		// LoadOptions.audioStreamInfo.
		moviePlayer.Load (infile);

		// By default the MoviePlayer will bind the framebuffer to renderer.sharedMaterial,
		// but in this case we don't have a renderer attached. Instead we instruct it to
		// draw the frames directly onto the scree.
		moviePlayer.drawToScreen = true;

		// MoviePlayer doesn't have methods for starting or stopping the playback. Instead
		// it uses state variable that you set. Besides play, seeking is another common
		// field that you will read and set.
		moviePlayer.play = true;

		// Looping a movie is as simple as setting loop=true. MoviePlayer has events for
		// loop, play and stop that you can hook your scripts onto.
		moviePlayer.loop = true;
		moviePlayer.OnLoop += delegate(MoviePlayerBase mp) {
			Debug.Log ("Loop it");
		};
	}

	[ContextMenu("Connect and play MJPEG HTTP stream")]
	void ConnectAndPlayAStream ()
	{
		if (!Application.isPlaying) return;
		
		// MovieStreamer is like a MoviePlayer that can play streams.
		// The main difference is that you can't seek a network stream.
		// It can be used with zero code, but here we'll create it from a script.
		var movieStreamer = GetComponent<MovieStreamer> ();
		if (movieStreamer == null) {
			movieStreamer = gameObject.AddComponent<MovieStreamer> ();
		}
		// Connect to a stream. We could also set movieStreamer.sourceUrl and
		// then call movieStreamer.Reconnect() for the same result.
		movieStreamer.Load (httpStreamUrl);
		
		// Set the streamer to draw on screen. The play field for a streamer only
		// controls wether the framebuffer is being updated or not. The connection
		// is kept open until movieStreamer.Unload() is called.
		movieStreamer.drawToScreen = true;
		movieStreamer.play = true;
	}

	[ContextMenu("Stop and unload (play mode)")]
	void StopAndUnload ()
	{
		if (!Application.isPlaying) return;

		// Get a MoviePlayer component and stop the playback
		var moviePlayer = GetComponent<MoviePlayer> ();
		if (moviePlayer != null) {
			// Stopping a movie is as simple as setting play=false. Unload method will
			// release all resources associated with currently loaded movie so that
			// MoviePlayer can be reused to Load and play back other movies.
			// Actually if you just call Load on MoviePlayer it'll safely unload the
			// current movie and load another. If loading the other fails, current one
			// will keep playing.
			moviePlayer.play = false;
			moviePlayer.Unload ();
		}

		// Use the same method for stopping MovieStreamer too
		var movieStreamer = GetComponent<MovieStreamer> ();
		if (movieStreamer != null) {
			// We could set play=false, but that is not necessary, becuse if the
			// stream is not connected, we're not playing it back anyway.
			movieStreamer.Unload ();
		}
	}

	[ContextMenu("Extract one frame")]
	void ExtractOneFrame ()
	{
		#if UNITY_WEBPLAYER || UNITY_WEBGL
		Debug.Log("This example doesn't work in web player");
		#else
		// First we load the movie, but not using a MoviePlayer component, but a
		// MoviePlayerUtil class. The Load method requires a System.IO.Stream as an input
		// and returns a framebuffer Texture2D and AudioClip which is optional.
		// The Load method detects the stream type, initializes a Demux that can read audio and
		// video streams in it and then it creates stream Decoder instances that you will use
		// to extract video frames or audio samples.
		Texture2D framebuffer;
		Movie movie = MoviePlayerUtil.Load (File.OpenRead (infile), out framebuffer);
		if (movie.demux.hasVideo) {
			// Invoking a Decoder like this will fetch an encoded frame bytes from Demux
			// and decode it into framebuffer texture specified earlier. There is
			// another overload that doesn't take a frame as an argument, but returns it
			// instead. This is used with Streamer demux where you have sequential access only.
			movie.videoDecoder.Decode (frame);
		}

		// Just encode the frame as PNG and write to disk
		File.WriteAllBytes (frameOutFile, framebuffer.EncodeToPNG ());
		Debug.Log ("Extracted frame " + frame);
		#endif
	}

	[ContextMenu("Drop half the frames remux")]
	void DropHalfTheFramesRemux ()
	{
		// In this example we're going one level deeper in the API and work directly
		// with Demux class. We could use MoviePlayerUtil.Load too, but for remuxing
		// we don't need Decoders to be instantiated, because we're just copying encoded
		// frame bytes around.
		//
		// Since we're not using decoders, we're not referencing anything from Unity API.
		// Therefore it's possible to run it in separate thread.
		RunInBackgroundOrNot (delegate() {
			// Instantiate a demux for an input stream based on stream type.
			Stream instream = File.OpenRead (infile);
			Demux demux = Demux.forSource (instream);
			demux.Init (instream);

			// Instantiate a remux for an output stream. Here we have to explicity
			// instantiate the remux we want, in this case, AviRemux, and set its
			// properties. Since we're not doing much here, we can use the same
			// videoStreamInfo and audioStreamInfo for remux as demux. For the video
			// however we clone the stream info, because we want to change it. Since
			// we're going to drop every other frame, we also need to lower the
			// video framerate.
			Stream outstream = File.OpenWrite (outfile);
			Remux remux = new AviRemux ();
			var remuxVideoStreamInfo = new VideoStreamInfo (demux.videoStreamInfo);
			remuxVideoStreamInfo.framerate /= 2;
			remux.Init (outstream, remuxVideoStreamInfo, demux.audioStreamInfo);

			// Just sum buffers and variables needed later
			byte[] videoBuffer, audioBuffer;
			int videoBytesRead, audioBytesRead;

			// Loop until we've processed all the video frames. If we wanted to run this code
			// in main Unity thread without blocking, then we could wrap it all in a coroutine
			// and do "yield return 1" inside the loop.
			do {
				// Here we're using sequential access to video (and audio) stream. The same could
				// be achieved with random access, but then only demuxes that can seek in a file
				// can be used (no streaming from network or webcam).
				videoBytesRead = demux.ReadVideoFrame (out videoBuffer);
				if (videoBytesRead > 0) {
					// Read the exact number of audio samples that are to be played during this frame
					int samplesPerVideoFrame = (int)(demux.audioStreamInfo.sampleRate / demux.videoStreamInfo.framerate);
					audioBytesRead = demux.ReadAudioSamples (out audioBuffer, samplesPerVideoFrame);

					// Only write every second video frame, but all the audio samples. The total stream
					// lengths will still be the same, because we've set the framerate for remuxed stream
					// to half of the original.
					if (demux.VideoPosition % 2 == 1) {
						remux.WriteNextVideoFrame (videoBuffer, videoBytesRead);
					}
					remux.WriteNextAudioSamples (audioBuffer, audioBytesRead);
				}
			} while(videoBytesRead > 0);

			// Close the remux and demux. While it's possible to leave demux just hanging there unclosed and
			// possibly introducing a memory leak, we have to Close the remux for the output to be playable.
			// The reason is that AviDemux needs to write all unwritten index chunks and update the avi header
			// after all frames have been written.
			remux.Shutdown ();
			demux.Shutdown ();
		});
	}

	[ContextMenu("Instant movie switching")]
	void InstantMovieSwitching ()
	{
		if (!Application.isPlaying)
			return;
		StartCoroutine (InstantMovieSwitchingCoroutine ());
	}

	IEnumerator InstantMovieSwitchingCoroutine ()
	{
		// Load the file and start playing
		LoadFileAndPlayForever ();

		var moviePlayer = GetComponent<MoviePlayer> ();
		while (true) {
			// Wait for a bit, random
			yield return new WaitForSeconds (UnityEngine.Random.Range (0.25f, 2f));

			// Reload the file again. In this case it's the same file, but it doesn't have to be.
			// The expected behaviour is that the playback will continue smoothly, because no
			// state variables are changed prior to loading. While it's true for video, there'll
			// be a tiny switching delay in audio due threading. When a new clip is loaded,
			// the first frame is decoded automatically, so if you want to start playing the new
			// clip from a certain position, then set the new videoFrame or videoTime before calling Load.
			//moviePlayer.videoFrame = 150;
			moviePlayer.Load (infile);
		}
	}

	[ContextMenu("Capture webcam to file")]
	void CaptureWebcamToFile ()
	{
		if (!Application.isPlaying)
			return;
		StartCoroutine (CaptureWebcamToFileCoroutine ());
	}

	IEnumerator CaptureWebcamToFileCoroutine ()
	{
		// Open a webcam streamer. The url prefix for this is webcam://
		// Optionally a webcam device id can be added (to get a list, use WebCamTexture.devices)
		string webcamStreamUrl = "webcam://";
		Streamer streamer = Streamer.forUrl (webcamStreamUrl);
		streamer.Connect (webcamStreamUrl);

		// Set up a remux @ 15fps
		var vi = new VideoStreamInfo (streamer.videoStreamInfo);
		vi.framerate = 15; // must be lower than framerate with this approach!
		AviRemux remux = new AviRemux ();
		remux.Init (File.OpenWrite (outfile), vi, null);

		// Do fixed time capture, 10 seconds (150 frames @ 15fps)
		// The webcam framerate can be lower or higher than this. If it is lower then
		// a frame is written multiple times, if higher, then some frames are now written.
		float captureStartTime = Time.realtimeSinceStartup;
		int realFrameNr, lastRealFrameNr = -1;
		do {
			// Read a frame from webcam. It returns a frame number, but we're not using it.
			byte[] buf;
			frame = streamer.VideoPosition;
			int bytesCnt = streamer.ReadVideoFrame (out buf);

			// Calculate the video frame number that we should be writing.
			realFrameNr = Mathf.RoundToInt ((Time.realtimeSinceStartup - captureStartTime) * vi.framerate);

			// If the loop is being executed too seldom compared to vi.framerate, write a warning to console.
			if (realFrameNr - lastRealFrameNr > 1) {
				Debug.LogWarning ("Output framerate too high, possibly just skipped " + (realFrameNr - lastRealFrameNr) + " frames");
			}

			// Write as many frames as we need. Normally this is 0 or 1, but can be higher (see the warning a few lines above)
			while (lastRealFrameNr < realFrameNr) {
				remux.WriteNextVideoFrame (buf, bytesCnt);
				lastRealFrameNr ++;
			}

			// Give control back to Unity for one frame
			yield return 1;
		} while(realFrameNr < 150);

		// We're done. Close the remux and streamer
		remux.Shutdown ();
		streamer.Shutdown ();
		Debug.Log ("Done capturing");
	}

	[ContextMenu("Extract raw video")]
	void ExtractRawVideo ()
	{
		#if UNITY_WEBPLAYER || UNITY_WEBGL
		Debug.Log("This example doesn't work in web player");
		#else
		// ExtractRawVideo is thread safe. Multiple instances of it can run on the same System.IO.Stream
		RunInBackgroundOrNot (delegate() {
			File.WriteAllBytes (outfile, MoviePlayerUtil.ExtractRawVideo (File.OpenRead (infile)));
		});
		#endif
	}
	
	[ContextMenu("Extract raw audio")]
	void ExtractRawAudio ()
	{
		#if UNITY_WEBPLAYER || UNITY_WEBGL
		Debug.Log("This example doesn't work in web player");
		#else
		// ExtractRawVideo is thread safe. Multiple instances of it can run on the same System.IO.Stream
		RunInBackgroundOrNot (delegate() {
			File.WriteAllBytes (outfile, MoviePlayerUtil.ExtractRawAudio (File.OpenRead (infile)));
		});
		#endif
	}

	void OnGUI ()
	{
		GUI.depth = -1;

		GUI.Label (new Rect (10, 3, Screen.width - 220, 70), "Open ScriptingDemoUI.cs an see what these methods do, then try them out. Most of these don't need PLAY mode");

		int buttonHeight = 25;

		GUILayout.BeginArea (new Rect (10, 40, Screen.width - 220, 200));
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("LoadFileAndPlayForever", GUILayout.Height (buttonHeight))) {
			LoadFileAndPlayForever ();
		}
		if (GUILayout.Button ("ConnectAndPlayAStream", GUILayout.Height (buttonHeight))) {
			ConnectAndPlayAStream ();
		}
		if (GUILayout.Button ("StopAndUnload", GUILayout.Height (buttonHeight))) {
			StopAndUnload ();
		}
		if (GUILayout.Button ("ExtractOneFrame", GUILayout.Height (buttonHeight))) {
			ExtractOneFrame ();
		}
		if (GUILayout.Button ("DropHalfTheFramesRemux", GUILayout.Height (buttonHeight))) {
			DropHalfTheFramesRemux ();
		}
		GUILayout.EndHorizontal ();
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("InstantMovieSwitching", GUILayout.Height (buttonHeight))) {
			InstantMovieSwitching ();
		}
		if (GUILayout.Button ("ExtractRawVideo", GUILayout.Height (buttonHeight))) {
			ExtractRawVideo ();
		}
		if (GUILayout.Button ("ExtractRawAudio", GUILayout.Height (buttonHeight))) {
			ExtractRawVideo ();
		}
		if (GUILayout.Button ("CaptureWebcamToFile", GUILayout.Height (buttonHeight))) {
			CaptureWebcamToFile ();
		}
		GUILayout.EndHorizontal ();
		GUILayout.EndArea ();

		// me
		GUI.Label (new Rect (Screen.width - 170, 10, 200, 70),
		           "MoviePlayer " + MoviePlayer.PACKAGE_VERSION + "\n" +
			"scripting demo\n" +
			"by SHUU Games 2014");
	}

	#region ----- simple threading -----

	delegate void Action ();

	void RunInBackgroundOrNot (Action action)
	{	
		if (runInSeparateThread) {
			var worker = new BackgroundWorker ();
			worker.DoWork += delegate(object sender, DoWorkEventArgs e) {
				action ();
			};
			worker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
				Debug.Log ("Background thread done");
			};
			worker.RunWorkerAsync ();
		} else {
			action ();
			Debug.Log ("Done");
		}
	}

	#endregion

#endif // UNITY_WINRT
}
