//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------

using UnityEngine;
using System;
using MP;
using MP.Net;

/// <summary>
/// Movie streamer
/// </summary>
public class MovieStreamer : MoviePlayerBase
{
	#region ----- public ------

	/// <summary>
	/// Movie source url
	/// </summary>
	public string sourceUrl;

	/// <summary>
	/// Movie load options. The Load() methods on this component will use
	/// this unless you're provinding your own.
	/// </summary>
	public LoadOptions loadOptions = LoadOptions.Default;

	/// <summary>
	/// Background thread status
	/// </summary>
	public string status;

	public long bytesReceived;

	public bool IsConnected
	{
		get {
			return movie==null || movie.demux==null ? false : ((Streamer)movie.demux).IsConnected;
		}
	}

	/// <summary>
	/// Connects to an URL for streaming.
	/// 
	/// In case it fails, exception text is logged and FALSE is returned
	/// </summary>
	public bool Load (string srcUrl)
	{
		return Load (srcUrl, null);
	}
	public bool Load (string srcUrl, LoadOptions loadOptions)
	{
		this.sourceUrl = srcUrl;
		if(loadOptions == null) {
			loadOptions = this.loadOptions;
		} else {
			this.loadOptions = loadOptions;
		}

		try {
			base.Load(new MovieSource() { url = srcUrl }, loadOptions);
			return true;
		}
		catch (Exception e) {
			if(loadOptions.enableExceptionThrow) {
				throw e;
			} else {
				Debug.LogError (e);
				return false;
			}
		}
	}

	[ContextMenu("Reconnect")]
	public bool ReConnect ()
	{
		bool success = true;
		if (!string.IsNullOrEmpty(sourceUrl)) {
			success = Load (sourceUrl, loadOptions);
		}
		return success;
	}

	#endregion ------ / public ------

	#region ----- private -----

	private int lastVideoFrame = -1;

	void Start ()
	{
		ReConnect ();
	}

	void OnGUI ()
	{
		if (!IsConnected || !movie.demux.hasVideo)
			return;

		// if we're playing the movie directly to screen, but don't
		// show it before we've received at least one frame
		if (drawToScreen && framebuffer != null && ((Streamer)movie.demux).VideoPosition > 0) {
			DrawFramebufferToScreen ();
		}
	}

	void Update ()
	{
		// get the thread status and write it here
		if(movie != null && movie.demux != null)
		{
			if(movie.demux is HttpMjpegStreamer) {
				status = ((HttpMjpegStreamer)movie.demux).Status;
				bytesReceived = ((HttpMjpegStreamer)movie.demux).BytesReceived;
			}
		}

		// if this.play changed, Play or Stop the movie
		HandlePlayStop ();

		// decode a frame when necessary
		if(play) {
			HandleFrameDecode ();
		}
	}

	protected void HandleFrameDecode ()
	{
		if (!IsConnected || !movie.demux.hasVideo || movie.videoDecoder == null)
			return;

		// decode a frame if there's a new one available
		if (movie.videoDecoder.Position != lastVideoFrame)
		{
			if(movie.videoDecoder.Position >= 0)
			{
				movie.videoDecoder.DecodeNext ();

				// update the aspect ration of the video
				movie.demux.videoStreamInfo.width = framebuffer.width;
				movie.demux.videoStreamInfo.height = framebuffer.height;
			}

			lastVideoFrame = movie.videoDecoder.Position;
		}
	}

	#endregion
}
