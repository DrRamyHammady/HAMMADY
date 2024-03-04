//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;

namespace MP.Decoder
{
	/// <summary>
	/// Video decoder for MJPEG stream
	/// </summary>
	public class VideoDecoderMJPEG : VideoDecoderUnity
	{
		#region ----- Constants -----

		public const uint FOURCC_MJPG = 0x47504A4D;
		public const uint FOURCC_CJPG = 0x47504A43;
		public const uint FOURCC_ffds = 0x73646666; // not tested!
		public const uint FOURCC_jpeg = 0x6765706A;

		#endregion

		#region ----- Public methods and properties -----

		/// <summary>
		/// Constructor. It's always reated for a stream, so you need to provide info about it here.
		/// </summary>
		public VideoDecoderMJPEG (VideoStreamInfo streamInfo = null) : base(streamInfo)
		{
		}

		#endregion
	}

}
