//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using MP.Decoder;

namespace MP
{
	/// <summary>
	/// Base class for all video decoder implementations. A video decoder
	/// can read compressed frame data from video stream and decode it
	/// into a framebuffer texture.
	/// </summary>
	public abstract class VideoDecoder
	{
		/// <summary>
		/// Factory method for instantiating the right decoder instance based on streamInfo.
		/// </summary>
		public static VideoDecoder CreateFor (VideoStreamInfo streamInfo)
		{
			if (streamInfo == null) {
				throw new System.ArgumentException ("Can't choose VideoDecoder without streamInfo (with at least codecFourCC)");
			}

			// list of FourCC codes http://www.fourcc.org/codecs.php
			switch (streamInfo.codecFourCC) {
			case VideoDecoderMJPEG.FOURCC_MJPG:
			case VideoDecoderMJPEG.FOURCC_CJPG:
			case VideoDecoderMJPEG.FOURCC_ffds:
			case VideoDecoderMJPEG.FOURCC_jpeg:
				return new VideoDecoderMJPEG (streamInfo);
			
			case VideoDecoderMPNG.FOURCC_MPNG:
				return new VideoDecoderMPNG (streamInfo);

			case VideoDecoderRGB.FOURCC_DIB_:
			case VideoDecoderRGB.FOURCC_NULL:
				return new VideoDecoderRGB (streamInfo);
			}
			throw new MpException ("No decoder for video fourCC 0x" + streamInfo.codecFourCC.ToString ("X") + 
				" (" + RiffParser.FromFourCC (streamInfo.codecFourCC) + ")");
		}

		public abstract void Init (out Texture2D framebuffer, Demux demux, LoadOptions loadOptions = null);

		public abstract void Shutdown ();

		/// <summary>
		/// Gets or sets the video playhead position (frame number).
		/// In most cases this property should proxy Demux.VideoPosition.
		/// </summary>
		public abstract int Position { get; set; }

		/// <summary>
		/// Decodes the video frame at current playhead Position (frame number),
		/// then the Position is incremented by one.
		/// If a particular decoder needs to access multiple frames for decoding
		/// current frame, then it can use demux.VideoPosition and demux.ReadVideoFrame
		/// for it, but it must ensure the Position increments exactly by one.
		/// </summary>
		public abstract void DecodeNext ();

		/// <summary>
		/// Set playhead position and decode this frame.
		/// </summary>
		public void Decode (int frame)
		{
			Position = frame;
			DecodeNext ();
		}

		public abstract float lastFrameDecodeTime { get; }
		public abstract int lastFrameSizeBytes { get; }

		public abstract float totalDecodeTime { get; }
		public abstract long totalSizeBytes { get; }
	}
}
