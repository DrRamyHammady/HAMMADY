//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using MP;
using MP.Decoder;

namespace MP.RAW
{
	/// <summary>
	/// Raw MJPEG stream.
	/// 
	/// It can be used to access video frames in a file/stream that
	/// is just some JPEG images concatenated together (without JFIF
	/// container). A program called Avidemux is capable of producing
	/// such streams.
	/// 
	/// Since there is no index in raw stream, it needs to be created
	/// by scanning through the whole stream, which will be slow if
	/// it is a large file. However if the stream is already in memory
	/// and not too big, then the scanning is reasonably fast (maybe
	/// tens of milliseconds for a few tens of megabytes).
	/// </summary>
	public class RawMjpegDemux : Demux
	{
		// when reading from a file, read this many bytes ahead
		private const int FILE_READ_BUFFER_SIZE = 8096;
		private AtomicBinaryReader reader;

		// movie "index" telling where the jpg frames are
		private List<long> frameStartIndex = new List<long> ();
		private List<int> frameSize = new List<int> ();

		// frame data is returned via this buffer
		private byte[] rawJpgBuffer;
		private int nextVideoFrame;

		public override void Init (Stream sourceStream, LoadOptions loadOptions = null)
		{
			// skip the video if asked not to load it
			if (loadOptions != null && loadOptions.skipVideo)
				return;

			// check the arguments
			if (sourceStream == null) {
				throw new System.ArgumentException ("sourceStream is required");
			}

			// measure load time
			var watch = new System.Diagnostics.Stopwatch ();
			watch.Start ();

			reader = new AtomicBinaryReader (sourceStream);

			// for detecting the buffer size
			int maxRawJpgSize = 0;

			// the stream can't be seeked unless there is an index. create it
			frameStartIndex.Clear ();
			frameSize.Clear ();

			long markerCount = 0;
			long startIndex = -1;
			bool markerStart = false;
			int bytesRead = -1;
			long i = 0;
			var buffer = new byte[FILE_READ_BUFFER_SIZE];

			// read the file in chunks (more than 30x faster than reading by byte)
			long p = 0;
			do {
				bytesRead = reader.Read (ref p, buffer, 0, FILE_READ_BUFFER_SIZE);

				for (int j = 0; j < bytesRead; j++) {
					byte b = buffer [j];
					
					// wait for marker start
					if (b == 0xFF) {
						markerStart = true;
					} else if (markerStart) {
						// read the other marker byte and decide what to do
						switch (b) {
						case 0xD8: // Start of image
							startIndex = i + j - 1;
							break;
						case 0xD9: // End of image
							frameStartIndex.Add (startIndex);
							int size = (int)(i + j - startIndex + 1);
							if (size > maxRawJpgSize)
								maxRawJpgSize = size;
							frameSize.Add (size);
							//Debug.Log("Found frame OFFS: " + startIndex + " SIZE: " + size);
							break;
						}
						markerStart = false;
						markerCount++;
					}
				}
				i += bytesRead;
			} while(bytesRead >= FILE_READ_BUFFER_SIZE);

			// create a buffer for holding raw jpg data when decoding a frame
			rawJpgBuffer = new byte[maxRawJpgSize];

			watch.Stop ();
			#if MP_DEBUG
			Debug.Log("Recreated index for raw MJPEG stream in " + (watch.Elapsed.TotalMilliseconds * 0.001f) + " seconds." +
				"Frames: " + frameStartIndex.Count + ". Max jpg size: " + maxRawJpgSize + ". Markers: " + markerCount);
			#endif

			// set all the info about the video stream we know
			if (loadOptions != null && loadOptions.videoStreamInfo != null) {
				videoStreamInfo = loadOptions.videoStreamInfo;
			} else {
				videoStreamInfo = new VideoStreamInfo ();
				videoStreamInfo.codecFourCC = VideoDecoderMJPEG.FOURCC_MJPG;
			}
			videoStreamInfo.frameCount = frameSize.Count;
			videoStreamInfo.lengthBytes = reader.StreamLength;
		}

		public override void Shutdown (bool force = false)
		{
			// nothing to do here. this class instance doesn't hold any exposed resources on its own
		}
		
		public override int VideoPosition {
			get {
				return nextVideoFrame;
			}
			set {
				nextVideoFrame = value;
			}
		}

		public override int ReadVideoFrame (out byte[] targetBuf)
		{
			targetBuf = rawJpgBuffer;
			if (nextVideoFrame < 0 || nextVideoFrame >= videoStreamInfo.frameCount)
				return 0;
			nextVideoFrame++;

			long offs = frameStartIndex [nextVideoFrame - 1];
			return reader.Read (ref offs, rawJpgBuffer, 0, frameSize [nextVideoFrame - 1]);
		}

		public override int AudioPosition {
			get {
				throw new System.NotSupportedException ("There's no hidden audio in raw MJPEG stream");
			}
			set {
				throw new System.NotSupportedException ("There's no hidden audio in raw MJPEG stream");
			}
		}

		public override int ReadAudioSamples (out byte[] targetBuf, int sampleCount)
		{
			throw new System.NotSupportedException ("There's no hidden audio in raw MJPEG stream");
		}
	}
}
