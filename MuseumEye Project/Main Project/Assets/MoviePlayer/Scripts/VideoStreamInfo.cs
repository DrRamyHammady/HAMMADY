//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------

namespace MP
{
	/// <summary>
	/// Video stream info.
	/// </summary>
	public class VideoStreamInfo
	{
		public uint codecFourCC;
		public int bitsPerPixel;
		public int frameCount = 1; // default value
		public int width = 1; // default value
		public int height = 1; // default value
		public float framerate = 30; // default value
		public long lengthBytes;

		public float lengthSeconds { get { return (float)frameCount / framerate; } }

		public VideoStreamInfo ()
		{
		}

		public VideoStreamInfo (VideoStreamInfo vi)
		{
			codecFourCC = vi.codecFourCC;
			bitsPerPixel = vi.bitsPerPixel;
			frameCount = vi.frameCount;
			width = vi.width;
			height = vi.height;
			framerate = vi.framerate;
			lengthBytes = vi.lengthBytes;
		}
	}
}
