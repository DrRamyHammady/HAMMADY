//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System.IO;
using MP.Decoder;

namespace MP.RAW
{
	/// <summary>
	/// Raw pcm stream.
	/// 
	/// It can be used to read audio samples from a raw PCM file/stream.
	/// To use it, you need to provide stream info, because it needs
	/// to know sample size and count in the file.
	/// </summary>
	public class RawPcmDemux : Demux
	{
		private AtomicBinaryReader reader;

		// frame data is returned via this buffer
		private byte[] rawAudioBuf;
		private int nextAudioSample;

		public override void Init (Stream sourceStream, LoadOptions loadOptions = null)
		{
			if (sourceStream == null || loadOptions == null || loadOptions.audioStreamInfo == null) {
				throw new System.ArgumentException ("sourceStream and loadOptions.audioStreamInfo are required");
			}

			reader = new AtomicBinaryReader (sourceStream);

			// set all the audio stream info we know
			audioStreamInfo = loadOptions.audioStreamInfo;
			audioStreamInfo.lengthBytes = reader.StreamLength;
			
			nextAudioSample = 0;
		}

		public override void Shutdown (bool force = false)
		{
			// nothing to do here. this class instance doesn't hold any exposed resources on its own
		}

		public override int VideoPosition {
			get {
				throw new System.NotSupportedException ("There's no hidden video in raw PCM audio");
			}
			set {
				throw new System.NotSupportedException ("There's no hidden video in raw PCM audio");
			}
		}

		public override int ReadVideoFrame (out byte[] targetBuf)
		{
			throw new System.NotSupportedException ("There's no hidden video in raw PCM audio");
		}

		public override int AudioPosition {
			get {
				return nextAudioSample;
			}
			set {
				nextAudioSample = value;
			}
		}

		public override int ReadAudioSamples (out byte[] targetBuf, int sampleCount)
		{
			// reduce sampleCount if trying to read past the end of the stream
			if (nextAudioSample + sampleCount > audioStreamInfo.sampleCount) {
				sampleCount = audioStreamInfo.sampleCount - nextAudioSample;
			}
			// usually 1, 2 or 4 for PCM audio (eg 16bit*2channels=4)
			int bytesToRead = sampleCount * audioStreamInfo.sampleSize;

			if (rawAudioBuf == null || rawAudioBuf.Length < bytesToRead) {
				rawAudioBuf = new byte[bytesToRead];
			}
			targetBuf = rawAudioBuf;

			// safety
			if (bytesToRead <= 0)
				return 0;

			long offs = nextAudioSample * audioStreamInfo.sampleSize;
			nextAudioSample += sampleCount;
			return reader.Read (ref offs, rawAudioBuf, 0, bytesToRead) / audioStreamInfo.sampleSize;
		}
	}
}
