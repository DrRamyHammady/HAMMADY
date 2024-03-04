//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System;
using System.IO;
using MP.Net;

namespace MP
{
	/// <summary>
	/// Streamer is a special kind of demux. Instead of initializing it for one
	/// readable and seekable Stream like regular Demux, a Streamer is "connected"
	/// to some audio/video source. Normally that source is an URL, but in essence
	/// it's up to a streamer what it returns, it could even generate content
	/// procedurally.
	/// </summary>
	public abstract class Streamer : Demux
	{
		public static Streamer forUrl (string url)
		{
			if (url.StartsWith ("http")) {
				return new HttpMjpegStreamer ();
			}
			throw new MpException ("Can't detect suitable Streamer for given url: " + url);
		}

		public abstract void Connect (string url, LoadOptions loadOptions = null);

		public abstract bool IsConnected { get; }

		public override void Init (Stream stream, LoadOptions loadOptions = null)
		{
			throw new MpException ("Streamer requires you to call Connect() instead of Init()");
		}
		
		public override int ReadVideoFrame (out byte[] targetBuf)
		{
			throw new NotSupportedException ("Can't read arbitrary frame from a stream");
		}

		public override int ReadAudioSamples (out byte[] targetBuf, int sampleCount)
		{
			throw new NotSupportedException ("Can't read arbitrary audio from a stream");
		}
	}
}
