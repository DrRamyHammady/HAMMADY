//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using System;

namespace MP.Decoder
{
	/// <summary>
	/// Audio decoder for PCM stream.
	/// 
	/// Features:
	///  1, 2 or more channels
	///  pcm_alaw, pcm_mulaw, pcm_s16le, pcm_u8 formats (8 and 16 bit)
	///  any frequency
	/// </summary>
	public class AudioDecoderPCM : AudioDecoder
	{
		#region ----- Constants -----

		// Microsoft FourCCs
		public const uint FOURCC_MS = 0x00000001;

		// Avidemux fourCCs
		public const uint FOURCC_0 = 0x00000000;

		// QuickTime FourCCs
		// @xxx need samples to test those FourCC codes
		//public const uint FOURCC_RAW__QT = 0x20776172; // 'raw ' 16 bit LE signed
		//public const uint FOURCC_ALAW_QT = 0x77616C61; // 'alaw'
		//public const uint FOURCC_ULAW_QT = 0x77616C75; // 'ulaw'

		// Microsoft wFormats
		public const uint FORMAT_UNCOMPRESSED = 0x00000001;
		public const uint FORMAT_ALAW = 0x00000006;
		public const uint FORMAT_ULAW = 0x00000007;

		#endregion

		#region ----- Public methods and properties -----

		/// <summary>
		/// Constructor. It's always reated for a stream, so you need to provide info about it here.
		/// </summary>
		public AudioDecoderPCM (AudioStreamInfo streamInfo)
		{
			this.streamInfo = streamInfo;

			// can we decode this stream?
			if (streamInfo == null) {
				throw new ArgumentException ("Can't initialize stream decoder without proper AudioStreamInfo");
			}
			if (streamInfo.audioFormat != FORMAT_UNCOMPRESSED &&
				streamInfo.audioFormat != FORMAT_ALAW &&
				streamInfo.audioFormat != FORMAT_ULAW) {
				throw new ArgumentException ("Unsupported PCM format=0x" + streamInfo.audioFormat.ToString ("X"));
			}
			
			int bytesPerChannelSample = streamInfo.sampleSize / streamInfo.channels;
			if (bytesPerChannelSample > 2) {
				throw new ArgumentException ("Only 8bit and 16bit_le audio is supported. " + (bytesPerChannelSample * 8) + "bits given");
			}
		}
		
		/// <summary>
		/// Initializes the decoder for playing back an audio stream. It returns an audio clip
		/// which is either streaming or preloaded. Unity will use callback methods here to
		/// get the actual audio data.
		/// </summary>
		/// <param name="audioClip">Audio clip.</param>
		/// <param name="demux">Demux.</param>
		/// <param name="loadOptions">Load options.</param>
		public override void Init (out AudioClip audioClip, Demux demux, LoadOptions loadOptions = null)
		{
			if (loadOptions == null)
				loadOptions = LoadOptions.Default;

			if (demux == null) {
				throw new ArgumentException ("Missing Demux to get audio samples for decoding");
			}

			this.demux = demux;
			this._totalDecodeTime = 0;
			this.watch = new System.Diagnostics.Stopwatch ();

			// it'd be safer to do inside lock, but Unity tends to crash on that
			#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6
			this.audioClip = AudioClip.Create ("_movie_audio_",
			                                   streamInfo.sampleCount, streamInfo.channels, streamInfo.sampleRate,
			                                   loadOptions._3DSound,
			                                   !loadOptions.preloadAudio,
			                                   OnAudioRead,
			                                   OnAudioSeek);
			#else
			// starting from Unity 5 the 3D audio parameter is deprecated.
			// It has moved into AudioSource spatialBlend.
			this.audioClip = AudioClip.Create ("_movie_audio_",
			                                   streamInfo.sampleCount, streamInfo.channels, streamInfo.sampleRate,
			                                   !loadOptions.preloadAudio,
			                                   OnAudioRead,
			                                   OnAudioSeek);
			#endif

			audioClip = this.audioClip;
		}

		public override void Shutdown ()
		{
			if (audioClip != null) {
				if (Application.isEditor) {
					AudioClip.DestroyImmediate (audioClip);
				} else {
					AudioClip.Destroy (audioClip);
				}
			}
		}

		public override float totalDecodeTime { get { return _totalDecodeTime; } }

		public override int Position {
			get {
				return demux.AudioPosition;
			}
			set {
				demux.AudioPosition = value;
			}
		}

		// called in Audio thread
		public override void DecodeNext (float[] data, int sampleCount)
		{
			// if called before PCM callbacks can not return data yet
			if (data == null || this.demux == null)
				return;

			watch.Reset ();
			watch.Start ();
			
			int channels = streamInfo.channels;

			// left channel, right channel, ?other channels?
			try {

				// read into byte buffer
				byte[] buf;
				int samplesActuallyRead = demux.ReadAudioSamples (out buf, sampleCount);
				// always: samplesActuallyRead <= 2 * data.Length
				
				for (int c = 0; c < channels; c++) {
					if (streamInfo.audioFormat == FORMAT_UNCOMPRESSED) {
						int bytesPerChannelSample = streamInfo.sampleSize / channels;
						if (bytesPerChannelSample == 2) {
							for (int i = 0; i < samplesActuallyRead; i++) {
								int p = i * channels + c;
								int p2 = p * 2;
								short s16value = (short)((buf [p2 + 1] << 8) | buf [p2]);
								data [p] = (float)s16value / 32768f;
							}
						} else {
							for (int i = 0; i < samplesActuallyRead; i++) {
								int p = i * channels + c;
								data [p] = (float)(buf [p] - 128) / 128f;
							}
						}
					} else if (streamInfo.audioFormat == FORMAT_ALAW) {
						for (int i = 0; i < samplesActuallyRead; i++) {
							int p = i * channels + c;
							data [p] = ALawExpandLookupTable [buf [p]];
						}
					} else if (streamInfo.audioFormat == FORMAT_ULAW) {
						for (int i = 0; i < samplesActuallyRead; i++) {
							int p = i * channels + c;
							data [p] = uLawExpandLookupTable [buf [p]];
						}
					}
				}
			} catch (Exception e) {
				if (e is IndexOutOfRangeException || e is ObjectDisposedException) {
					// *** IndexOutOfRangeException ***
					// data[p] may throw this exception, because data.Length changes
					// during execution of this method (caused by main or Loader thread).
					//
					// *** ObjectDisposedException ***
					// demux.ReadAudioSamples may throw it if the underlying stream
					// is closed. Again, we're avoiding locking by catching this exception.
				} else
					throw;
			}
		
			watch.Stop ();
			_totalDecodeTime += (float)(0.001f * watch.Elapsed.TotalMilliseconds);
		}

		#endregion
		
		#region ----- Callbacks used by Unity's audio system -----

		public void OnAudioRead (float[] data)
		{
			DecodeNext (data, data.Length / streamInfo.channels);
		}

		// called in Audio thread
		public void OnAudioSeek (int newPosition)
		{
			Position = newPosition;
		}

		#endregion
		
		#region ----- Private members and lookup tables -----

		// streamInfo and stream are used by 2 threads
		private AudioStreamInfo streamInfo;
		private Demux demux;
		private AudioClip audioClip;
		private float _totalDecodeTime = 0;
		private System.Diagnostics.Stopwatch watch;
		private static float[] ALawExpandLookupTable = new float[256]
		{
			-0.167969f, -0.160156f, -0.183594f, -0.175781f, -0.136719f, -0.128906f, -0.152344f, -0.144531f,
			-0.230469f, -0.222656f, -0.246094f, -0.238281f, -0.199219f, -0.191406f, -0.214844f, -0.207031f,
			-0.083984f, -0.080078f, -0.091797f, -0.087891f, -0.068359f, -0.064453f, -0.076172f, -0.072266f,
			-0.115234f, -0.111328f, -0.123047f, -0.119141f, -0.099609f, -0.095703f, -0.107422f, -0.103516f,
			-0.671875f, -0.640625f, -0.734375f, -0.703125f, -0.546875f, -0.515625f, -0.609375f, -0.578125f,
			-0.921875f, -0.890625f, -0.984375f, -0.953125f, -0.796875f, -0.765625f, -0.859375f, -0.828125f,
			-0.335938f, -0.320312f, -0.367188f, -0.351562f, -0.273438f, -0.257812f, -0.304688f, -0.289062f,
			-0.460938f, -0.445312f, -0.492188f, -0.476562f, -0.398438f, -0.382812f, -0.429688f, -0.414062f,
			-0.010498f, -0.010010f, -0.011475f, -0.010986f, -0.008545f, -0.008057f, -0.009521f, -0.009033f,
			-0.014404f, -0.013916f, -0.015381f, -0.014893f, -0.012451f, -0.011963f, -0.013428f, -0.012939f,
			-0.002686f, -0.002197f, -0.003662f, -0.003174f, -0.000732f, -0.000244f, -0.001709f, -0.001221f,
			-0.006592f, -0.006104f, -0.007568f, -0.007080f, -0.004639f, -0.004150f, -0.005615f, -0.005127f,
			-0.041992f, -0.040039f, -0.045898f, -0.043945f, -0.034180f, -0.032227f, -0.038086f, -0.036133f,
			-0.057617f, -0.055664f, -0.061523f, -0.059570f, -0.049805f, -0.047852f, -0.053711f, -0.051758f,
			-0.020996f, -0.020020f, -0.022949f, -0.021973f, -0.017090f, -0.016113f, -0.019043f, -0.018066f,
			-0.028809f, -0.027832f, -0.030762f, -0.029785f, -0.024902f, -0.023926f, -0.026855f, -0.025879f,
			0.167969f, 0.160156f, 0.183594f, 0.175781f, 0.136719f, 0.128906f, 0.152344f, 0.144531f,
			0.230469f, 0.222656f, 0.246094f, 0.238281f, 0.199219f, 0.191406f, 0.214844f, 0.207031f,
			0.083984f, 0.080078f, 0.091797f, 0.087891f, 0.068359f, 0.064453f, 0.076172f, 0.072266f,
			0.115234f, 0.111328f, 0.123047f, 0.119141f, 0.099609f, 0.095703f, 0.107422f, 0.103516f,
			0.671875f, 0.640625f, 0.734375f, 0.703125f, 0.546875f, 0.515625f, 0.609375f, 0.578125f,
			0.921875f, 0.890625f, 0.984375f, 0.953125f, 0.796875f, 0.765625f, 0.859375f, 0.828125f,
			0.335938f, 0.320312f, 0.367188f, 0.351562f, 0.273438f, 0.257812f, 0.304688f, 0.289062f,
			0.460938f, 0.445312f, 0.492188f, 0.476562f, 0.398438f, 0.382812f, 0.429688f, 0.414062f,
			0.010498f, 0.010010f, 0.011475f, 0.010986f, 0.008545f, 0.008057f, 0.009521f, 0.009033f,
			0.014404f, 0.013916f, 0.015381f, 0.014893f, 0.012451f, 0.011963f, 0.013428f, 0.012939f,
			0.002686f, 0.002197f, 0.003662f, 0.003174f, 0.000732f, 0.000244f, 0.001709f, 0.001221f,
			0.006592f, 0.006104f, 0.007568f, 0.007080f, 0.004639f, 0.004150f, 0.005615f, 0.005127f,
			0.041992f, 0.040039f, 0.045898f, 0.043945f, 0.034180f, 0.032227f, 0.038086f, 0.036133f,
			0.057617f, 0.055664f, 0.061523f, 0.059570f, 0.049805f, 0.047852f, 0.053711f, 0.051758f,
			0.020996f, 0.020020f, 0.022949f, 0.021973f, 0.017090f, 0.016113f, 0.019043f, 0.018066f,
			0.028809f, 0.027832f, 0.030762f, 0.029785f, 0.024902f, 0.023926f, 0.026855f, 0.025879f
		};
		private static float[] uLawExpandLookupTable = new float[256]
		{
			-0.980347f, -0.949097f, -0.917847f, -0.886597f, -0.855347f, -0.824097f, -0.792847f, -0.761597f,
			-0.730347f, -0.699097f, -0.667847f, -0.636597f, -0.605347f, -0.574097f, -0.542847f, -0.511597f,
			-0.488159f, -0.472534f, -0.456909f, -0.441284f, -0.425659f, -0.410034f, -0.394409f, -0.378784f,
			-0.363159f, -0.347534f, -0.331909f, -0.316284f, -0.300659f, -0.285034f, -0.269409f, -0.253784f,
			-0.242065f, -0.234253f, -0.226440f, -0.218628f, -0.210815f, -0.203003f, -0.195190f, -0.187378f,
			-0.179565f, -0.171753f, -0.163940f, -0.156128f, -0.148315f, -0.140503f, -0.132690f, -0.124878f,
			-0.119019f, -0.115112f, -0.111206f, -0.107300f, -0.103394f, -0.099487f, -0.095581f, -0.091675f,
			-0.087769f, -0.083862f, -0.079956f, -0.076050f, -0.072144f, -0.068237f, -0.064331f, -0.060425f,
			-0.057495f, -0.055542f, -0.053589f, -0.051636f, -0.049683f, -0.047729f, -0.045776f, -0.043823f,
			-0.041870f, -0.039917f, -0.037964f, -0.036011f, -0.034058f, -0.032104f, -0.030151f, -0.028198f,
			-0.026733f, -0.025757f, -0.024780f, -0.023804f, -0.022827f, -0.021851f, -0.020874f, -0.019897f,
			-0.018921f, -0.017944f, -0.016968f, -0.015991f, -0.015015f, -0.014038f, -0.013062f, -0.012085f,
			-0.011353f, -0.010864f, -0.010376f, -0.009888f, -0.009399f, -0.008911f, -0.008423f, -0.007935f,
			-0.007446f, -0.006958f, -0.006470f, -0.005981f, -0.005493f, -0.005005f, -0.004517f, -0.004028f,
			-0.003662f, -0.003418f, -0.003174f, -0.002930f, -0.002686f, -0.002441f, -0.002197f, -0.001953f,
			-0.001709f, -0.001465f, -0.001221f, -0.000977f, -0.000732f, -0.000488f, -0.000244f, 0.000000f,
			0.980347f, 0.949097f, 0.917847f, 0.886597f, 0.855347f, 0.824097f, 0.792847f, 0.761597f,
			0.730347f, 0.699097f, 0.667847f, 0.636597f, 0.605347f, 0.574097f, 0.542847f, 0.511597f,
			0.488159f, 0.472534f, 0.456909f, 0.441284f, 0.425659f, 0.410034f, 0.394409f, 0.378784f,
			0.363159f, 0.347534f, 0.331909f, 0.316284f, 0.300659f, 0.285034f, 0.269409f, 0.253784f,
			0.242065f, 0.234253f, 0.226440f, 0.218628f, 0.210815f, 0.203003f, 0.195190f, 0.187378f,
			0.179565f, 0.171753f, 0.163940f, 0.156128f, 0.148315f, 0.140503f, 0.132690f, 0.124878f,
			0.119019f, 0.115112f, 0.111206f, 0.107300f, 0.103394f, 0.099487f, 0.095581f, 0.091675f,
			0.087769f, 0.083862f, 0.079956f, 0.076050f, 0.072144f, 0.068237f, 0.064331f, 0.060425f,
			0.057495f, 0.055542f, 0.053589f, 0.051636f, 0.049683f, 0.047729f, 0.045776f, 0.043823f,
			0.041870f, 0.039917f, 0.037964f, 0.036011f, 0.034058f, 0.032104f, 0.030151f, 0.028198f,
			0.026733f, 0.025757f, 0.024780f, 0.023804f, 0.022827f, 0.021851f, 0.020874f, 0.019897f,
			0.018921f, 0.017944f, 0.016968f, 0.015991f, 0.015015f, 0.014038f, 0.013062f, 0.012085f,
			0.011353f, 0.010864f, 0.010376f, 0.009888f, 0.009399f, 0.008911f, 0.008423f, 0.007935f,
			0.007446f, 0.006958f, 0.006470f, 0.005981f, 0.005493f, 0.005005f, 0.004517f, 0.004028f,
			0.003662f, 0.003418f, 0.003174f, 0.002930f, 0.002686f, 0.002441f, 0.002197f, 0.001953f,
			0.001709f, 0.001465f, 0.001221f, 0.000977f, 0.000732f, 0.000488f, 0.000244f, 0.000000f
		};

		#endregion
	}

}
