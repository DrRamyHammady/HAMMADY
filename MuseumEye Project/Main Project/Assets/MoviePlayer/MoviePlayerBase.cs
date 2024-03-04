//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------

using UnityEngine;
using MP;

public abstract class MoviePlayerBase : MonoBehaviour
{
	#region ----- Public properties ------

	/// <summary>
	/// Video decoder target texture that is created when movie is loaded.
	/// </summary>
	public Texture2D framebuffer;

	/// <summary>
	/// Audio decoder target buffer that is used ONLY if there is no audioSource set.
	/// </summary>
	public AudioClip audiobuffer;

	/// <summary>
	/// If TRUE then the framebuffer is drawn directly
	/// onto the screen in OnGUI method.
	/// </summary>
	public bool drawToScreen;

	[System.Obsolete("Use property named material instead, it works exactly like otherMaterial did. In later version otherMaterial will be removed")]
	public Material otherMaterial;

	/// <summary>
	/// A material to assign the framebuffer to (optional).
	/// If using drawToScreen option, then this material is used.
	/// </summary>
	public Material material;

	/// <summary>
	/// Material texture property name
	/// </summary>
	public string texturePropertyName = "_MainTex";

	public enum ScreenMode
	{
		Crop,
		Fill,
		Stretch,
		CustomRect
	}

	/// <summary>
	/// How exactly should we draw the framebuffer onto the screen.
	/// </summary>
	public ScreenMode screenMode;

	/// <summary>
	/// GUI.depth for DrawTexture when drawing onto the screen.
	/// </summary>
	public int screenGuiDepth;

	/// <summary>
	/// If ScreenMode.CustomRect, then this is the rect.
	/// </summary>
	public Rect customScreenRect = new Rect(0, 0, 100, 100);

	/// <summary>
	/// Set to TRUE to play and FALSE for pause the movie
	/// </summary>
	public bool play;

	/// <summary>
	/// Currently loaded movie.
	/// </summary>
	public Movie movie;

	/// <summary>
	/// Frame count defining max lag between audio and video stream.
	/// If the audio and video streams are synchronized too often
	/// (usually clicking sounds in audio), then increasing this value may help.
	/// </summary>
	public int maxSyncErrorFrames = 2;

	public delegate void MovieEvent (MoviePlayerBase caller);

	/// <summary>
	/// Called right before starting the playback.
	/// The movie is loaded and ready.
	/// </summary>
	public event MovieEvent OnPlay;

	/// <summary>
	/// Called right after the movie has stopped.
	/// </summary>
	public event MovieEvent OnStop;

	/// <summary>
	/// Frame drop count.
	/// </summary>
	public int framesSkipped { get { return _framesDropped; } }

	/// <summary>
	/// Audio and video stream sync events
	/// </summary>
	/// <value>The sync events.</value>
	public int syncEvents { get { return _syncEvents; } }

	/// <summary>
	/// Gets the video stream framerate.
	/// 
	/// See movie.demux.videoStreamInfo and movie.demux.audioStreamInfo for accessing all stream info.
	/// </summary>
	public float framerate {
		get {
			return (movie != null && movie.demux != null && movie.demux.videoStreamInfo != null)
				? movie.demux.videoStreamInfo.framerate : 30;
		}
	}

	/// <summary>
	/// Gets the video stream length. Total playback time is always the video stream length even if
	/// audio stream in a movie is shorter or longer.
	/// 
	/// See movie.demux.videoStreamInfo and movie.demux.audioStreamInfo for accessing all stream info.
	/// </summary>
	public float lengthSeconds {
		get {
			return (movie != null && movie.demux != null && movie.demux.videoStreamInfo != null)
				? movie.demux.videoStreamInfo.lengthSeconds : 0;
		}
	}

	/// <summary>
	/// Gets the video stream length in frames. Total playback time is always the video stream length even if
	/// audio stream in a movie is shorter or longer.
	/// 
	/// See movie.demux.videoStreamInfo and movie.demux.audioStreamInfo for accessing all stream info.
	/// </summary>
	public int lengthFrames {
		get {
			return (movie != null && movie.demux != null && movie.demux.videoStreamInfo != null)
				? movie.demux.videoStreamInfo.frameCount : 0;
		}
	}

	#endregion ------ /public properties ------

	#region ------ public methods ------

	/// <summary>
	/// Loads the movie.
	/// 
	/// In case it fails, exception is thrown
	/// </summary>
	protected virtual void Load (MovieSource source, LoadOptions loadOptions = null)
	{
		// use temporary vars when loading and then assign them to instance vars.
		// this way if the loading fails, the MoviePlayer is still in consistent state.
		AudioClip tmpAudioBuffer;
		Texture2D tmpFramebuffer;

		// Actually try to load the movie.
		// In case of failure, exception is thrown here and the currently playing movie keeps playing
		var loadedMovie = MoviePlayerUtil.Load (source, out tmpFramebuffer, out tmpAudioBuffer, loadOptions);

		// new movie loaded successfully. if there was a movie previously loaded, unload it
		if(movie != null) {
			MoviePlayerUtil.Unload(movie);
		}

		movie = loadedMovie;

		// reset stats
		_framesDropped = 0;
		_syncEvents = 0;

		// make the loaded movie visible and hearable
		framebuffer = tmpFramebuffer;
		if(tmpAudioBuffer != null) {
			audiobuffer = tmpAudioBuffer;
		}
		Bind ();
	}

	/// <summary>
	/// Closes the movie source stream.
	/// 
	/// Don't leave cleaning up to Garbage Colletor, at least
	/// if it is a file stream. Clean it up yourself.
	/// </summary>
	[ContextMenu("Unload (disconnect)")]
	public void Unload()
	{
		if(movie != null) {
			// stop the audio
			var audio = GetComponent<AudioSource>();
			if(audio != null) audio.Stop();

			// unload all other resources associated with the movie
			MoviePlayerUtil.Unload(movie);
			movie = null;
		}
	}

	#endregion ------ / public methods ------

	protected int _framesDropped;
	protected int _syncEvents;
	protected bool lastPlay;

	/// <summary>
	/// Binds or rebinds the output of audio and video decoder to framebuffer texture and audioClip.
	/// </summary>
	protected void Bind ()
	{
		// if we have a renderer, bind the framebuffer to its material (most convenient use case probably)
		var renderer = GetComponent<Renderer>();
		if(renderer != null) {
			renderer.sharedMaterial.SetTexture(texturePropertyName, framebuffer);
			#if MP_DEBUG
			Debug.Log("Framebuffer bound to renderer material. Property name: " + texturePropertyName);
			#endif
		}

		// bind the framebuffer to given material
		if(material != null) {
			material.SetTexture(texturePropertyName, framebuffer);
			#if MP_DEBUG
			Debug.Log("Framebuffer bound to " + material.name + " material. Property name: " + texturePropertyName);
			#endif
		}

		// if there is audio, bind it to the AudioSource component.
		// if AudioSource is not there, then it is added automatically
		var audio = GetComponent<AudioSource>();
		if (audiobuffer != null)
		{
			if (audio == null) {
				audio = gameObject.AddComponent<AudioSource> ();
			}
			audio.clip = audiobuffer;
			audio.playOnAwake = false;
			#if MP_DEBUG
			Debug.Log("Audio buffer bound to GameObject's AudioSource");
			#endif
		}
		// if there's no audio in the movie, but there is AudioSource component, then stop whatever is playing there
		else if(audio != null) {
			audio.Stop();
			audio.clip = null;
			#if MP_DEBUG
			Debug.Log("Removed audio buffer from GameObject's AudioSource");
			#endif
		}
	}

	protected void DrawFramebufferToScreen (Rect? sourceUV = null)
	{
		Rect drawRect = new Rect (0, 0, Screen.width, Screen.height);
		
		if (screenMode == ScreenMode.CustomRect) {
			drawRect = customScreenRect;
		} else if (screenMode != ScreenMode.Stretch) {
			float movieAspect = (float)movie.demux.videoStreamInfo.width / (float)movie.demux.videoStreamInfo.height;
			float screenAspect = (float)Screen.width / (float)Screen.height;
			
			if (screenMode == ScreenMode.Crop) {
				if (screenAspect < movieAspect) { // black bars on top and bottom
					drawRect.height = Mathf.Round (drawRect.width / movieAspect);
				} else { // black bars on left and right
					drawRect.width = Mathf.Round (drawRect.height * movieAspect);
				}
			} else if (screenMode == ScreenMode.Fill) {
				if (screenAspect < movieAspect) { // lock top and bottom
					drawRect.width = Mathf.Round (Screen.height * movieAspect);
				} else { // lock left and right
					drawRect.height = Mathf.Round (Screen.width / movieAspect);
				}
			}
			drawRect.x = Mathf.Round ((Screen.width - drawRect.width) / 2f);
			drawRect.y = Mathf.Round ((Screen.height - drawRect.height) / 2f);
		}
		GUI.depth = screenGuiDepth;

		var e = Event.current;
		if(e == null || e.type == EventType.Repaint)
		{
			if(sourceUV == null) {
				Graphics.DrawTexture(drawRect, framebuffer, material);
			} else {
				Graphics.DrawTexture(drawRect, framebuffer, (Rect)sourceUV, 0, 0, 0, 0, material);
			}
		}
	}

	protected void HandlePlayStop ()
	{
		if (play != lastPlay)
		{
			var audio = GetComponent<AudioSource>();
			if (play)
			{
				#if MP_DEBUG
				Debug.Log ("PLAY");
				#endif

				if (OnPlay != null)
					OnPlay (this);

				SendMessage("OnPlay", this, SendMessageOptions.DontRequireReceiver);

				if (audio != null)
				{
					audio.Play ();
					if(this is MoviePlayer) {
						audio.time = ((MoviePlayer)this).videoTime;
					}
				}
			}
			else {
				#if MP_DEBUG
				Debug.Log ("STOP");
				#endif

				if (OnStop != null)
					OnStop (this);

				SendMessage("OnStop", this, SendMessageOptions.DontRequireReceiver);

				if (audio != null) {
					audio.Stop ();
				}
			}
			lastPlay = play;
		}
	}
}
