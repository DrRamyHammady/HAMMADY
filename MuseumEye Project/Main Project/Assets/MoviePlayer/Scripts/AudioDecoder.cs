//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using MP.Decoder;

namespace MP
{
	/// <summary>
	/// Base class for all audio decoder implementations. An audio decoder
	/// provides callback methods for Unitys audio system. When Unity requests
	/// audio data then the decoder will read what it needs to from audio stream
	/// and reproduce that audio data.
	/// </summary>
	public abstract class AudioDecoder
	{
		/// <summary>
		/// Factory method for instantiating the right audio decoder based on stream info.
		/// </summary>
		public static AudioDecoder CreateFor (AudioStreamInfo streamInfo)
		{
			if (streamInfo == null) {
				throw new System.ArgumentException ("Can't choose AudioDecoder without streamInfo (with at least codecFourCC)");
			}

			switch (streamInfo.codecFourCC) {
			case AudioDecoderPCM.FOURCC_MS:
			case AudioDecoderPCM.FOURCC_0:
				return new AudioDecoderPCM (streamInfo);
			}
			throw new MpException ("No decoder for audio fourCC 0x" + streamInfo.codecFourCC.ToString ("X") + 
				" (" + RiffParser.FromFourCC (streamInfo.codecFourCC) + ")");
		}

		public abstract void Init (out AudioClip audioClip, Demux demux, LoadOptions loadOptions = null);

		public abstract void Shutdown ();

		/// <summary>
		/// Gets or sets the audio sample offset.
		/// In most cases this property should proxy Demux.AudioPosition.
		/// </summary>
		public abstract int Position { get; set; }

		/// <summary>
		/// Decodes the sampleCount audio frames starting from Position,
		/// then the Position is incremented by sampleCount.
		/// </summary>
		public abstract void DecodeNext (float[] data, int sampleCount);

		public abstract float totalDecodeTime { get; }
	}
}
