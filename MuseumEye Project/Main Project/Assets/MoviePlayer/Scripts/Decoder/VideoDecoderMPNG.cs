//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;

namespace MP.Decoder
{
	/// <summary>
	/// Video decoder for MPNG stream
	/// </summary>
	public class VideoDecoderMPNG : VideoDecoderUnity
	{
		#region ----- Constants -----

		public const uint FOURCC_MPNG = 0x474E504D;

		#endregion

		#region ----- Public methods and properties -----

		/// <summary>
		/// Constructor. It's always reated for a stream, so you need to provide info about it here.
		/// </summary>
		public VideoDecoderMPNG (VideoStreamInfo streamInfo = null) : base(streamInfo)
		{
		}

		#endregion
	}

}
