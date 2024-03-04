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
	/// Base class for all Remux implementations. A remux mixes encoded
	/// audio and video streams into one container stream.
	/// 
	/// Usage:
	///   var videoStreamInfo = ...; // set up the codec info, dimensions, etc
	///   var remux = new AviRemux();
	///   remux.Init (File.OpenWrite ("out.avi"), videoStreamInfo, null);
	///   remux.WriteNextVideoFrame(encodedFrame0);
	///   remux.WriteNextVideoFrame(encodedFrame1);
	///   remux.Close();
	/// </summary>
	public abstract class Remux
	{
		protected Stream dstStream;
		private VideoStreamInfo _videoStreamInfo;
		private AudioStreamInfo _audioStreamInfo;

		/// <summary>
		/// Initializes the remux. For convenience, call base.Init(...) in your subclass.
		/// 
		/// Depending on the output format, videoStreamInfo and audioStreamInfo can be NULL
		/// to indicate, for example, that the AVI won't have audio.
		/// </summary>
		public virtual void Init (Stream dstStream, VideoStreamInfo videoStreamInfo, AudioStreamInfo audioStreamInfo)
		{
			this.dstStream = dstStream;
			this._videoStreamInfo = videoStreamInfo;
			this._audioStreamInfo = audioStreamInfo;
		}

		/// <summary>
		/// Close the dstStream. If there are some finishing touches to the stream (like
		/// writing the total frame count somewhere) you should do it here.
		/// </summary>
		public abstract void Shutdown ();

		/// <summary>
		/// Writes a video frame "at the end" of the dstStream.
		/// If size is not given (-1) then all bytes from frameBytes are used.
		/// </summary>
		public abstract void WriteNextVideoFrame (byte[] frameBytes, int size = -1);

		/// <summary>
		/// Writes a video frame to arbitrary frameOffset position in dstStream.
		/// </summary>
		public abstract void WriteVideoFrame (int frameOffset, byte[] frameBytes, int size = -1);

		/// <summary>
		/// Writes audio samples "at the end" of the dstStream.
		/// </summary>
		public abstract void WriteNextAudioSamples (byte[] sampleBytes, int size = -1);

		/// <summary>
		/// Writes audio samples to arbitrary sampleOffset position in dstStream.
		/// </summary>
		public abstract void WriteAudioSamples (int sampleOffset, byte[] sampleBytes, int size = -1);

		public VideoStreamInfo videoStreamInfo { get { return _videoStreamInfo; } }

		public AudioStreamInfo audioStreamInfo { get { return _audioStreamInfo; } }
	}
}
