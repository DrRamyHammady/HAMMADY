//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using System;

namespace MP.Decoder
{
	/// <summary>
	/// Video decoder for raw RGB video stream.
	/// 
	/// (Technically YCbCr color space can be played back too, but then
	/// the conversion to RGB must happen in shader, because it's too
	/// expensive to do in managed C#)
	/// </summary>
	public class VideoDecoderRGB : VideoDecoder
	{
		#region ----- Constants -----

		public const uint FOURCC_NULL = 0x00000000;
		public const uint FOURCC_DIB_ = 0x20424944;

		#endregion

		#region ----- Public methods and properties -----

		/// <summary>
		/// Constructor. It's always reated for a stream, so you need to provide info about it here.
		/// </summary>
		public VideoDecoderRGB (VideoStreamInfo info = null)
		{
			this.info = info;
		}

		/// <summary>
		/// Initializes the decoder. Always call this before trying to use this class.
		/// </summary>
		/// <param name="framebuffer">Framebuffer to be created. Decoded frames will be written here.</param>
		/// <param name="demux">Demux to read the video data from</param>
		public override void Init (out Texture2D framebuffer, Demux demux, LoadOptions loadOptions = null)
		{
			// can we decode this stream?
			if (demux == null) {
				throw new System.ArgumentException ("Missing Demux to get video frames from");
			}
			if (info == null || info.width <= 0 || info.height <= 0 || info.bitsPerPixel <= 0) {
				throw new ArgumentException ("Can't initialize stream decoder without proper VideoStreamInfo");
			}
			if (info.bitsPerPixel != 16 && info.bitsPerPixel != 24 && info.bitsPerPixel != 32) {
				throw new ArgumentException ("Only RGB555, RGB24 and ARGB32 pixel formats are supported");
			}

			// create framebuffer and initialize vars
			this.framebuffer = new Texture2D (info.width, info.height, info.bitsPerPixel == 32 ? TextureFormat.ARGB32 : TextureFormat.RGB24, false);
			framebuffer = this.framebuffer;

			rgbBuffer = new Color32[info.width * info.height];

			this.demux = demux;

			this._lastFrameDecodeTime = 0;
			this._lastFrameSizeBytes = 0;
			this._totalDecodeTime = 0;
			this._totalSizeBytes = 0;
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

		public override int Position { get { return demux.VideoPosition; } set { demux.VideoPosition = value; } }

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
			int bytesRead;
			bytesRead = demux.ReadVideoFrame (out buf);

			// how many pixels are there in the frame
			int length = bytesRead / (info.bitsPerPixel / 8);
			if (length > rgbBuffer.Length) {
				throw new MpException ("Too much data in frame " + (demux.VideoPosition - 1) + " to decode. Broken AVI?");
			}
			
			// "decode" frame data
			if (info.bitsPerPixel == 32) {
				for (int i = 0; i < length; i++) {
					int p = i * 4;
					rgbBuffer [i].b = buf [p];
					rgbBuffer [i].g = buf [p + 1];
					rgbBuffer [i].r = buf [p + 2];
					rgbBuffer [i].a = buf [p + 3];
				}
			} else if (info.bitsPerPixel == 24) {
				for (int i = 0; i < length; i++) {
					int p = i * 3;
					//rgbBuffer[i] = lookup[buf[p+1] << 8 | buf[p]];
					rgbBuffer [i].b = buf [p];
					rgbBuffer [i].g = buf [p + 1];
					rgbBuffer [i].r = buf [p + 2];
				}
			} else if (info.bitsPerPixel == 16) {
				for (int i = 0; i < length; i++) {
					int p = i * 2;
					int u15 = buf [p + 1] << 8 | buf [p];
					rgbBuffer [i].b = (byte)(u15 << 3);
					rgbBuffer [i].g = (byte)((u15 >> 2) & 0xF8);
					rgbBuffer [i].r = (byte)((u15 >> 7) & 0xF8);
				}
			}
			// else we didn't do proper job in Init
			
			framebuffer.SetPixels32 (rgbBuffer);
			framebuffer.Apply (false);
			
			// register frame decode time
			watch.Stop ();
			_lastFrameDecodeTime = (float)(0.001f * watch.Elapsed.TotalMilliseconds);
			_lastFrameSizeBytes = rgbBuffer.Length;
			_totalDecodeTime += _lastFrameDecodeTime;
			_totalSizeBytes += _lastFrameSizeBytes;
		}

		public override float lastFrameDecodeTime { get { return _lastFrameDecodeTime; } }
		public override int lastFrameSizeBytes { get { return _lastFrameSizeBytes; } }

		public override float totalDecodeTime { get { return _totalDecodeTime; } }
		public override long totalSizeBytes { get { return _totalSizeBytes; } }

		#endregion

		#region ----- Private members -----

		private Texture2D framebuffer;
		private Color32[] rgbBuffer;
		private Demux demux;
		private VideoStreamInfo info;
		private float _lastFrameDecodeTime;
		private int _lastFrameSizeBytes;
		private float _totalDecodeTime;
		private long _totalSizeBytes;
		private System.Diagnostics.Stopwatch watch;

		#endregion
	}

}
