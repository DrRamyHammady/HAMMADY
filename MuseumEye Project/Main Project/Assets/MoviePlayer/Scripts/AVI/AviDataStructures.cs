//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System.Collections.Generic;

namespace MP.AVI
{
	/// <summary>
	/// Main AVI header.
	/// </summary>
	public class AVIMainHeader
	{
		public const uint AVIF_COPYRIGHTED = 0x00020000;
		public const uint AVIF_HASINDEX = 0x00000010;
		public const uint AVIF_ISINTERLEAVED = 0x00000100;
		public const uint AVIF_MUSTUSEINDEX = 0x00000020;
		public const uint AVIF_TRUSTCKTYPE = 0x00000800;
		public const uint AVIF_WASCAPTUREFILE = 0x00010000;
		public uint		dwMicroSecPerFrame;
		public uint		dwMaxBytesPerSec;
		public uint		dwPaddingGranularity; // pad to multiples of this size
		public uint		dwFlags; // AVIF_ flags
		public uint		dwTotalFrames; // frame count in first RIFF element (if no OpenDML, then that's the frame count)
		public uint		dwInitialFrames;
		public uint		dwStreams;
		public uint		dwSuggestedBufferSize;
		public uint		dwWidth;
		public uint		dwHeight;
		public uint		dwReserved0, dwReserved1, dwReserved2, dwReserved3;
	}
	
	/// <summary>
	/// AVI stream header.
	/// </summary>
	public class AVIStreamHeader
	{
		public const uint AVISF_DISABLED = 0x00000001;
		public const uint AVISF_VIDEO_PALCHANGES = 0x00010000;
		public uint		fccType;
		public uint		fccHandler;
		public uint		dwFlags; // AVISF_ flags
		public ushort	wPriority;
		public ushort	wLanguage;
		public uint		dwInitialFrames;
		public uint		dwScale;
		public uint		dwRate; // dwRate / dwScale == samples/second
		public uint		dwStart;
		public uint		dwLength; // In units above...
		public uint		dwSuggestedBufferSize;
		public uint		dwQuality;
		public uint		dwSampleSize;
		public short	rcFrameLeft, rcFrameTop, rcFrameRight, rcFrameBottom;
	}
	
	/// <summary>
	/// Extended AVI header
	/// </summary>
	public class ODMLHeader
	{
		public uint dwTotalFrames; // frame count if this is OpenDML AVI
	}

	/// <summary>
	/// Bitmap info header (video stream format header)
	/// </summary>
	public class BitmapInfoHeader
	{
		public uint		biSize;
		public int		biWidth;
		public int		biHeight;
		public ushort	biPlanes;
		public ushort	biBitCount;
		public uint		biCompression;
		public uint		biSizeImage;
		public int		biXPelsPerMeter;
		public int		biYPelsPerMeter;
		public uint		biClrUsed;
		public uint		biClrImportant;
	}
	
	/// <summary>
	/// Wave format ex (audio stream format header)
	/// </summary>
	public class WaveFormatEx
	{
		public ushort	wFormatTag;
		public ushort	nChannels;
		public uint		nSamplesPerSec;
		public uint		nAvgBytesPerSec;
		public ushort	nBlockAlign;
		public ushort	wBitsPerSample;
		public ushort	cbSize;
	}

	/// <summary>
	/// AVI stream index. It can be constructed from OpenDML AVI indexes or from old idx1 chunk.
	/// </summary>
	public class AviStreamIndex
	{
		public class Entry
		{
			public long		chunkOffset; // relative to the beginning of the stream
			public int		chunkLength;
			public bool		isKeyframe;
		}
		
		public enum Type
		{
			SUPERINDEX = 0x00,
			CHUNKS = 0x01,
			DATA = 0x80
		}
		;
		
		public uint streamId; // eg "00dc"
		public List<Entry> entries = new List<Entry> ();
		public int globalOffset;
	}

	/// <summary>
	/// AVI file.
	/// </summary>
	public class AVIFile
	{
		public AVIMainHeader avih;
		public AVIStreamHeader strhVideo;
		public BitmapInfoHeader strfVideo;
		public AVIStreamHeader strhAudio;
		public WaveFormatEx strfAudio;
		public ODMLHeader odml;
		public AviStreamIndex videoIndex;
		public AviStreamIndex audioIndex;
	}
}
