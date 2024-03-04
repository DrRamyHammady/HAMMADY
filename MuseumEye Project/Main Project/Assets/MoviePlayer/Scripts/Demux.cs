//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System.IO;
using MP.AVI;
using MP.RAW;

namespace MP
{
	/// <summary>
	/// Base class for all Demux implementations. A demux will make audio and 
	/// video streams available for a decoders.
	/// 
	/// Usage:
	///   var demux = Demux.forSource(srcStream);
	///   demux.Init(srcStream);
	///   demux.ReadVideoFrameNext(out encodedVideoFrameBuf);
	/// </summary>
	public abstract class Demux
	{
		/// <summary>
		/// Factory method for creating the right Demux instance for given stream.
		/// </summary>
		public static Demux forSource (Stream sourceStream)
		{
			byte[] buf = new byte[4];

			sourceStream.Seek (0, SeekOrigin.Begin);
			if (sourceStream.Read (buf, 0, 4) < 4) {
				throw new MpException ("Stream too small");
			}

			// is it AVI RIFF stream?
			if (buf [0] == 'R' && buf [1] == 'I' && buf [2] == 'F' && (buf [3] == 'F' || buf [3] == 'X')) {
				return new AviDemux ();
			}

			// is it RAW stream
			if (buf [0] == 0xFF && buf [1] == 0xD8) {
				sourceStream.Seek (-2, SeekOrigin.End);
				sourceStream.Read (buf, 0, 2);
				if (buf [0] == 0xFF && buf [1] == 0xD9) {
					return new RawMjpegDemux ();
				}
			}
			throw new MpException ("Can't detect suitable DEMUX for given stream");
		}

		public abstract void Init (Stream sourceStream, LoadOptions loadOptions = null);

		public abstract void Shutdown (bool force = false);

		public VideoStreamInfo videoStreamInfo { get; protected set; }

		public AudioStreamInfo audioStreamInfo { get; protected set; }

		public bool hasVideo { get { return videoStreamInfo != null; } }

		public bool hasAudio { get { return audioStreamInfo != null; } }

		/// <summary>
		/// Gets or sets video playhead position (frame number).
		/// If the stream is not seekable, then NotSupportedException must be thrown
		/// when trying to set this value, but get must always return a meaningful value.
		/// </summary>
		public abstract int VideoPosition { get; set; }

		/// <summary>
		/// Reads bytes for an encoded video frame at VideoPosition, then
		/// video position is incremented by one. Returns bytes read.
		/// </summary>
		public abstract int ReadVideoFrame (out byte[] targetBuf);

		/// <summary>
		/// Gets or sets audio playhead position (sample offset).
		/// If the stream is not seekable, then NotSupportedException must be thrown
		/// when trying to set this value, but get must always return a meaningful value.
		/// </summary>
		public abstract int AudioPosition { get; set; }

		/// <summary>
		/// Reads bytes for sampleCount encoded audio samples at AudioPosition, then
		/// audio position is incremented by sampleCount. Returns bytes read.
		/// </summary>
		public abstract int ReadAudioSamples (out byte[] targetBuf, int sampleCount);
	}
}
