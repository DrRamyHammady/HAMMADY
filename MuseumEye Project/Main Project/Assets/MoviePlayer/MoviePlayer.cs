//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using MP;

/// <summary>
/// Movie player
/// </summary>
public class MoviePlayer : MoviePlayerBase
{
	/// <summary>
	/// Package version
	/// </summary>
	public const string PACKAGE_VERSION = "v0.10";

	#region ----- Public properties ------
	
	/// <summary>
	/// RAW MJPEG AVI asset that is loaded at Start, can be NULL.
	/// 
	/// If Load() is called on this component, this is not relevant any more.
	/// </summary>
	public TextAsset source;

	/// <summary>
	/// The audio clip to play, can be NULL.
	/// 
	/// If the source already contains audio, then audioClip will override the audio in source.
	/// </summary>
	public AudioClip audioSource;

	/// <summary>
	/// Movie load options. The Load() methods on this component will use
	/// this unless you're provinding your own.
	/// </summary>
	public LoadOptions loadOptions = LoadOptions.Default;

	/// <summary>
	/// The current playhead time. Use this for seeking.
	/// </summary>
	public float videoTime;
	
	/// <summary>
	/// The current playhead frame index. Use this for seeking.
	/// </summary>
	public int videoFrame;

	/// <summary>
	/// If TRUE, the movie will automatically loop.
	/// </summary>
	public bool loop;
	
	/// <summary>
	/// Called when the movie reaches an end, right after
	/// it is rewinded back to the beginning.
	/// </summary>
	public event MovieEvent OnLoop;

	#endregion ------ /public properties ------

	#region ------ public methods ------

	/// <summary>
	/// Loads the movie from byte array.
	/// </summary>
	public bool Load (byte[] bytes)
	{
		return Load (bytes, null);
	}

	public bool Load (byte[] bytes, LoadOptions loadOptions)
	{
		try {
			Load (new MovieSource () { stream = new MemoryStream (bytes) }, loadOptions);
			return true;
		} catch (Exception e) {
			if (ShouldRethrow (e, loadOptions))
				throw e;
			return false;
		}
	}
	
	/// <summary>
	/// Loads the movie from TextAsset
	/// </summary>
	public bool Load (TextAsset textAsset)
	{
		this.source = textAsset;
		return Load (textAsset, null);
	}

	public bool Load (TextAsset textAsset, LoadOptions loadOptions)
	{
		try {
			this.source = textAsset;
			Load (new MovieSource () { stream = new MemoryStream (textAsset.bytes) }, loadOptions);
			return true;
		} catch (Exception e) {
			if (ShouldRethrow (e, loadOptions))
				throw e;
			return false;
		}
	}

	public bool Load (Stream srcStream)
	{
		try {
			Load (new MovieSource () { stream = srcStream }, null);
			return true;
		} catch (Exception e) {
			if (ShouldRethrow (e, loadOptions))
				throw e;
			return false;
		}
	}

	public void LoadFromWwwAsync (string url, Action<MoviePlayer> doneCallback, Action<MoviePlayer, Exception> failCallback)
	{
		StartCoroutine (LoadFromWwwAsyncCoroutine (url, doneCallback, failCallback, null));
	}

	public void LoadFromWwwAsync (string url, Action<MoviePlayer> doneCallback, Action<MoviePlayer, Exception> failCallback, LoadOptions loadOptions)
	{
		StartCoroutine (LoadFromWwwAsyncCoroutine (url, doneCallback, failCallback, loadOptions));
	}

	public void LoadFromResourceAsync (string url, Action<MoviePlayer> doneCallback, Action<MoviePlayer, Exception> failCallback)
	{
		StartCoroutine (LoadFromResourceAsyncCoroutine (url, doneCallback, failCallback, null));
	}

	public void LoadFromResourceAsync (string url, Action<MoviePlayer> doneCallback, Action<MoviePlayer, Exception> failCallback, LoadOptions loadOptions)
	{
		StartCoroutine (LoadFromResourceAsyncCoroutine (url, doneCallback, failCallback, loadOptions));
	}


	#if !UNITY_WINRT
	/// <summary>
	/// Loads the movie from file path.
	/// </summary>
	public bool Load (string path)
	{
		return Load (path, null);
	}

	public bool Load (string path, LoadOptions loadOptions)
	{
		try {
			Load (new MovieSource () { stream = File.OpenRead (path) }, loadOptions);
			return true;
		} catch (Exception e) {
			if (ShouldRethrow (e, loadOptions))
				throw e;
			return false;
		}
	}
	#endif

	/// <summary>
	/// Reloads the movie from "source".
	/// </summary>
	[ContextMenu("Reload")]
	public bool Reload ()
	{
		bool success = true;
		if (source != null) {
			success = Load (source.bytes, loadOptions);
			lastVideoFrame = -1; // will make HandleFrameDecode decode one frame even if not play=true
		}
		return success;
	}

	void Start ()
	{
		Reload ();
	}

	#endregion ------ / public methods ------

	protected float lastVideoTime;
	protected int lastVideoFrame;

	bool ShouldRethrow (Exception e, LoadOptions loadOptions)
	{
		if (loadOptions.enableExceptionThrow) {
			return true;
		}
		Debug.LogError (e);
		return false;
	}

	protected override void Load (MovieSource source, LoadOptions loadOptions = null)
	{
		if (loadOptions == null) {
			loadOptions = this.loadOptions;
		} else {
			this.loadOptions = loadOptions;
		}
		
		// if we have audioSource set here to override audio in the source stream
		// don't load the audio in the demux.
		bool overrideAudio = audioSource != null && !loadOptions.skipAudio;
		if (overrideAudio)
			loadOptions.skipAudio = true;

		if (overrideAudio)
			audiobuffer = audioSource;
		
		base.Load (source, loadOptions);
		
		if (!loadOptions.preloadVideo) {
			if (movie.videoDecoder != null) {
				movie.videoDecoder.Decode (videoFrame);
			}
		}
		UpdateRendererUVRect ();
	}

	IEnumerator LoadFromWwwAsyncCoroutine (string url, Action<MoviePlayer> doneCallback, Action<MoviePlayer, Exception> failCallback, LoadOptions loadOptions)
	{
		// download url contents, this will take some time
		var www = new WWW (url);
		double startTime = Time.realtimeSinceStartup;
		while (startTime + loadOptions.connectTimeout > Time.realtimeSinceStartup) {
			yield return 1;
		}
		
		// check for errors and try to load the movie
		Exception exception = null; // this variable is used to take failCallback out from catch block for safety
		try {
			
			// did we time out?
			if (!www.isDone && (startTime + loadOptions.connectTimeout > Time.realtimeSinceStartup)) {
				throw new TimeoutException ("Timeout " + loadOptions.connectTimeout + " seconds happens while loading \"" + url + "\"");
			}
			// or was there www error
			if (!string.IsNullOrEmpty (www.error)) {
				throw new MpException ("WWW error \"" + www.error + "\" while loading \"" + url + "\"");
			}
			
			Load (www.bytes, loadOptions);
		} catch (Exception e) {
			exception = e;
		}
		
		// call the callbacks
		if (exception == null || failCallback == null) {
			doneCallback (this);
		} else {
			failCallback (this, exception);
		}
	}

	IEnumerator LoadFromResourceAsyncCoroutine (string path, Action<MoviePlayer> doneCallback, Action<MoviePlayer, Exception> failCallback, LoadOptions loadOptions)
	{
		// strip ".bytes" extension if it's there
		string resourceName = path.EndsWith (".bytes") ? path.Remove (path.Length - 6) : path;

		var resourceRequest = Resources.LoadAsync (resourceName);
		while (!resourceRequest.isDone) {
			yield return 1;
		}

		// check for errors and try to load the movie
		Exception exception = null; // this variable is used to take failCallback out from catch block for safety
		try {
			if (resourceRequest.asset == null || resourceRequest.asset.GetType () != typeof(TextAsset)) {
				throw new MpException ("Resources.LoadAsync couldn't load \"" + resourceName + "\" as TextAsset");
			}
			Load (resourceRequest.asset as TextAsset, loadOptions);
		} catch (Exception e) {
			exception = e;
		}
		
		// call the callbacks
		if (exception == null || failCallback == null) {
			doneCallback (this);
		} else {
			failCallback (this, exception);
		}
	}

	void OnGUI ()
	{
		if (movie == null || movie.demux == null || movie.demux.videoStreamInfo == null)
			return;

		// if we're playing the movie directly to screen
		if (drawToScreen && framebuffer != null) {
			var uv = movie.frameUV [videoFrame % movie.frameUV.Length];
			DrawFramebufferToScreen (uv);
		}
	}

	void Update ()
	{
		// if this.play changed, Play or Stop the movie
		HandlePlayStop ();

		// advance playhead time or handle seeking
		bool wasSeeked = HandlePlayheadMove ();

		// decode a frame when necessary
		HandleFrameDecode (wasSeeked);

		if (play) {
			// synchronize audio and video
			HandleAudioSync ();

			// movie has been played back. should we restart it or loop
			HandleLoop ();
		}
	}

	protected bool HandlePlayheadMove ()
	{
		// let the videoTime advance normally, but in case
		// frameIndex has changed, use it to find new videoTime
		bool seekedByVideoFrame = videoFrame != lastVideoFrame;
		bool seekedByVideoTime = videoTime != lastVideoTime;

		if (seekedByVideoFrame) {
			videoTime = videoFrame / framerate;
		} else if (play) {
			videoTime += Time.deltaTime;
		}
		return seekedByVideoFrame || seekedByVideoTime;
	}

	protected void HandleFrameDecode (bool wasSeeked)
	{
		if (movie == null)
			return;
		
		// now when videoTime is known, find the corresponding
		// frameIndex and decode it if was not decoded last time
		videoFrame = Mathf.FloorToInt (videoTime * framerate);
		if (lastVideoFrame != videoFrame) {
			if (!loadOptions.preloadVideo) {
				// Decode a video frame only if there is a decoder.
				if (movie.videoDecoder != null) {
					movie.videoDecoder.Decode (videoFrame);
					// we could compensate for loading frame decode time here,
					// but it seems to not make timing better for some reason
					//videoTime += movie.videoDecoder.lastFrameDecodeTime;
				}
			}
			// just update the UV rect, the frame was already decoded
			UpdateRendererUVRect ();

			if (!wasSeeked && lastVideoFrame != videoFrame - 1) {
				int dropCnt = videoFrame - lastVideoFrame - 1;
				#if MP_DEBUG
				Debug.Log ("Frame drop. offset=" + (lastVideoFrame + 1) + ", count=" + dropCnt + " @ " + videoTime);
				#endif
				_framesDropped += dropCnt;
			}
		}
		lastVideoFrame = videoFrame;
		lastVideoTime = videoTime;
	}

	protected void HandleAudioSync ()
	{
		var audio = GetComponent<AudioSource> ();

		if (audio == null || !audio.enabled || audio.clip == null)
			return;
		
		if (videoTime <= audio.clip.length && (Mathf.Abs (videoTime - audio.time) > (float)maxSyncErrorFrames / framerate)) {
			#if MP_DEBUG
			Debug.Log ("Synchronizing audio and video. Drift: " + (videoTime - audio.time));
			#endif
			audio.Stop ();
			audio.time = videoTime;
			audio.Play ();
			_syncEvents++;
		}
	}

	protected void HandleLoop ()
	{
		if (movie == null || movie.demux == null || movie.demux.videoStreamInfo == null)
			return;
		
		if (videoTime >= movie.demux.videoStreamInfo.lengthSeconds) {
			if (loop) {
				// seek to the beginning
				videoTime = 0;
				#if MP_DEBUG
				Debug.Log ("LOOP");
				#endif
				if (OnLoop != null)
					OnLoop (this);
				SendMessage ("OnLoop", this, SendMessageOptions.DontRequireReceiver);
			} else {
				// stop the playback
				play = false;
			}
		}
	}

	/// <summary>
	/// It's public only because MoviePlayerEditor needs to call it. You shouldn't call it
	/// </summary>
	public void UpdateRendererUVRect ()
	{
		var renderer = GetComponent<Renderer> ();
		if (movie != null && movie.frameUV != null && movie.frameUV.Length > 0) {
			var uvRect = movie.frameUV [videoFrame % movie.frameUV.Length];
			if (renderer != null && renderer.sharedMaterial != null) {
				renderer.sharedMaterial.SetTextureOffset (texturePropertyName, new Vector2 (uvRect.x, uvRect.y));
				renderer.sharedMaterial.SetTextureScale (texturePropertyName, new Vector2 (uvRect.width, uvRect.height));
			}
			if (material != null) {
				material.SetTextureOffset (texturePropertyName, new Vector2 (uvRect.x, uvRect.y));
				material.SetTextureScale (texturePropertyName, new Vector2 (uvRect.width, uvRect.height));
			}
		}
	}
}
