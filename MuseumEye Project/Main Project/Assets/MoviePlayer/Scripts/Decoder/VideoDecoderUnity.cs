//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;

namespace MP.Decoder
{
	/// <summary>
	/// Video decoder that's built around Texture2D.LoadImage
	/// </summary>
	public abstract class VideoDecoderUnity : VideoDecoder
	{
		#region ----- Public methods and properties -----

		/// <summary>
		/// Constructor. It's always reated for a stream, so you need to provide info about it here.
		/// </summary>
		public VideoDecoderUnity (VideoStreamInfo streamInfo = null)
		{
			this.streamInfo = streamInfo;
		}

		/// <summary>
		/// Initializes the decoder for playing back given video stream. It returns a framebuffer
		/// which is updated with decoded frame pixel data.
		/// </summary>
		/// <param name="framebuffer">Framebuffer.</param>
		/// <param name="stream">Stream.</param>
		/// <param name="loadOptions">Load options.</param>
		public override void Init (out Texture2D framebuffer, Demux demux, LoadOptions loadOptions = null)
		{
			// can we decode this stream?
			if (demux == null) {
				throw new System.ArgumentException ("Missing Demux to get video frames from");
			}

			// create framebuffer and initialize vars. Texture size and format are not important here,
			// becase they'll be overwritten when a frame is decoded.
			this.framebuffer = new Texture2D (4, 4, TextureFormat.RGB24, false);
			framebuffer = this.framebuffer;
			this.demux = demux;

			this._lastFrameDecodeTime = 0;
			this._totalDecodeTime = 0;
			this.watch = new System.Diagnostics.Stopwatch ();
		}

		public override void Shutdown ()
		{
			if (framebuffer != null) {
				if (Application.isEditor) {
					Texture2D.DestroyImmediate (framebuffer);
				} else {
					Texture2D.Destroy (framebuffer);
				}
			}
		}

		public override int Position {
			get {
				return demux.VideoPosition;
			}
			set {
				demux.VideoPosition = value;
			}
		}

		public override void DecodeNext ()
		{
			// for safety
			if (framebuffer == null)
				return;

			// start the stopwatch
			watch.Reset ();
			watch.Start ();
			
			// read frame data from the steam
			byte[] buf;
			int bytesRead = demux.ReadVideoFrame (out buf);

			// Decode the frame. Since it's actually JPEG or PNG, Unity's
			// LoadImage method can load it pretty fast. Maybe 15ms per 720p frame.
			// Unfortunately this method is a bit buggy. It won't return FALSE as
			// documentation say if the buf contains invalid data.
			bool success = framebuffer.LoadImage (buf);
			
			// Double check if the image contruction failed. We're doing it by checking
			// wether frame dimensions change or not (they shouldn't).
			if (success && lastFbWidth > 0) {
				if (framebuffer.width != lastFbWidth || framebuffer.height != lastFbHeight) {
					success = false;
				}
				lastFbWidth = framebuffer.width;
				lastFbHeight = framebuffer.height;
			}
			
			// only upload the texture to GPU if LoadImage went well
			if (success) {
				framebuffer.Apply (false);
			} else {
				// not using "#if MP_DEBUG" here, because you want to know about it!
				Debug.LogError ("Couldn't decode frame " + (demux.VideoPosition - 1) + " from " + buf.Length + " bytes");
			}
			
			// register frame decode time
			watch.Stop ();
			_lastFrameDecodeTime = (float)(0.001f * watch.Elapsed.TotalMilliseconds);
			_lastFrameSizeBytes = bytesRead;
			_totalDecodeTime += _lastFrameDecodeTime;
			_totalSizeBytes += _lastFrameSizeBytes;
		}

		public override float lastFrameDecodeTime { get { return _lastFrameDecodeTime; } }
		public override int lastFrameSizeBytes { get { return _lastFrameSizeBytes; } }

		public override float totalDecodeTime { get { return _totalDecodeTime; } }
		public override long totalSizeBytes { get { return _totalSizeBytes; } }

		#endregion

		#region ----- Private members -----

		protected Texture2D framebuffer;
		protected VideoStreamInfo streamInfo;
		protected Demux demux;
		private float _lastFrameDecodeTime;
		private int _lastFrameSizeBytes;
		private float _totalDecodeTime;
		private long _totalSizeBytes;
		private System.Diagnostics.Stopwatch watch;

		// used to check if framebuffer dimensions change from frame to frame
		private int lastFbWidth = -1, lastFbHeight = -1;

		#endregion
	}

}
