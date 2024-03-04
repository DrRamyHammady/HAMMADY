//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System;

#if !UNITY_WINRT
// There's no sockets or threading API that we could use for Windows Store build target
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
#endif

namespace MP.Net
{
	/// <summary>
	/// HTTP MJPEG streamer supporting Content-Type: "multipart/x-mixed-replace"
	/// 
	/// On Windows Store target platform it compiles into "Not Implemented"
	/// </summary>
	public class HttpMjpegStreamer : Streamer
	{
		#region ----- public members -----

		/// <summary>
		/// Gets the current status of the background thread.
		/// </summary>
		public string Status {
			get {
				lock (locker) {
					return _status;
				}
			}
			private set {
				lock (locker) {
					_status = value;
				}
			}
		}

		public long BytesReceived {
			get {
				lock (locker) {
					return _bytesReceived;
				}
			}
			private set {
				lock (locker) {
					_bytesReceived = value;
				}
			}
		}

		/// <summary>
		/// Connects to an url and starts waiting for mjpeg video frames
		/// </summary>
		public override void Connect (string url, LoadOptions loadOptions = null)
		{
			#if !UNITY_WINRT

			if (loadOptions != null) {
				videoStreamInfo = loadOptions.videoStreamInfo != null ? loadOptions.videoStreamInfo : new VideoStreamInfo ();
				timeout = loadOptions.connectTimeout;
			} else {
				videoStreamInfo = new VideoStreamInfo ();
				timeout = 10;
			}
			videoStreamInfo.codecFourCC = MP.Decoder.VideoDecoderMJPEG.FOURCC_MJPG;
			videoStreamInfo.frameCount = 0;
			videoStreamInfo.framerate = 0;

			frameRingBuffer = new byte[1][];
			receivedFrameCount = 0;

			shouldStop = false;
			thread = new Thread (ThreadRun);
			thread.Start (url);

			#else
			Status = "Streaming not supported on Windows Store build target";
			throw new NotSupportedException("Streaming is currently not possible on Windows Store build target");
			#endif
		}

		/// <summary>
		/// Disconnects
		/// </summary>
		public override void Shutdown (bool force = false)
		{
			#if !UNITY_WINRT

			// tell the thread to exit by itself.
			// graceful stop will fail if the socket is waiting for data, but nothing
			// is received, because the control doesn't come back to this class.
			shouldStop = true;

			// interrupt the thread. it's safer than Abort, because we 
			// can catch the Interrupt exception and close the stream properly.
			if (force && thread != null) {
				thread.Interrupt ();
				thread = null;
			}
			#endif
		}

		/// <summary>
		/// Returns wether we're connected and receiving data.
		/// </summary>
		public override bool IsConnected {
			get {
				#if !UNITY_WINRT
				return thread != null && thread.IsAlive && connected;
				#else
				return false;
				#endif
			}
		}

		public override int VideoPosition {
			get {
				return receivedFrameCount;
			}
			set {
				throw new NotSupportedException ("Can't seek a live stream");
			}
		}

		/// <summary>
		/// Reads last received frame into buffer. Returns bytes read.
		/// </summary>
		public override int ReadVideoFrame (out byte[] targetBuf)
		{
			#if !UNITY_WINRT

			lock (locker) {
				targetBuf = receivedFrameCount > 0 ? frameRingBuffer [receivedFrameCount % frameRingBuffer.Length] : null;
			}
			return targetBuf != null ? targetBuf.Length : 0;

			#else
			throw new NotSupportedException("Streaming is currently not possible on Windows Store build target");
			#endif
		}

		public override int AudioPosition {
			get {
				throw new NotSupportedException ("There's no audio stream in HTTP MJPEG stream");
			}
			set {
				throw new NotSupportedException ("Can't seek a live stream");
			}
		}

		public override int ReadAudioSamples (out byte[] targetBuf, int sampleCount)
		{
			throw new NotSupportedException ("There's no audio stream in HTTP MJPEG stream");
		}

		#endregion

		#region ----- private members -----

		private object locker = new object ();
		private string _status; // _ discourages usage of it directly, even inside this class. Use this.Status
		private int receivedFrameCount = 0;
		private long _bytesReceived = 0;

		#if !UNITY_WINRT

		/// <summary>
		/// The initial byte buffer size for storing encoded frame.
		/// If it's a bit bigger than the biggest jpeg in the stream, then
		/// no memory reallocations are made while streaming, which is good
		/// for performance.
		/// </summary>
		private const int INITIAL_BYTE_BUFFER_SIZE = 131072;

		/// <summary>
		/// The maximum byte buffer for one frame. If this is exceeded, then
		/// the current frame being read is dropped. This prevents
		/// excessive memory allocations if the stream is corrupt.
		/// </summary>
		private const int MAX_BYTE_BUFFER_SIZE = 1048576;
		private Thread thread;
		private float timeout;
		private byte[][] frameRingBuffer;
		private volatile bool shouldStop;
		private volatile bool connected;

		private void FrameReceived (byte[] bytes)
		{
			//File.WriteAllBytes("frames/" + receivedFrameCount + ".jpg", bytes);

			Status = "Received frame " + receivedFrameCount;
			lock (locker) {
				frameRingBuffer [receivedFrameCount % frameRingBuffer.Length] = bytes;
				receivedFrameCount++;
			}
		}

		private void ThreadRun (object url)
		{
			Stream stream = null;
			try {
				connected = false;
				Status = "Connecting to " + url;

// In the web player build HttpWebRequest.Create will throw NotSupportedException, it doesn't happen in Editor.
// Therefore we have to use lower level sockets, which is not as flexible as HttpWebRequest, but should handle
// most of the cases. For example no HTTP redirects or response headers are supported in web player.
#if UNITY_WEBPLAYER

				// resolve host and port from given URI so that we can connect
				var uri = new Uri((string)url);

				// using async BeginConnect instead of synchronious Connect(...), because we want to use timeout.
				//
				// NOTE. The actual timeout will be longer, because Unity tries to request security policy from the host
				// you're connecting to. Strangely, this connect may throw timeout exception before secirity policy exception is thrown.
				//
				TcpClient client = new TcpClient();
				client.SendTimeout = (int)(timeout * 1000f);
				client.ReceiveTimeout = client.SendTimeout;
				var connResult = client.BeginConnect(Dns.GetHostEntry(uri.DnsSafeHost).AddressList, uri.Port <= 0 ? 80 : uri.Port, null, null);
				if(!connResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(timeout))) {
					throw new MpException("Failed to connect. Timeout.");
				}
				client.EndConnect(connResult);
				stream = client.GetStream();

				// send GET request
				string getRequestStr = "GET " + uri.PathAndQuery + " HTTP/1.0\r\n" +
					"Accept: image/png,image/*;q=0.8,*/*;q=0.5\r\n" +
					"Cache-Control: no-cache\r\n" +
					"Connection: close\r\n\r\n";
				byte[] getRequestBytes = System.Text.Encoding.ASCII.GetBytes(getRequestStr);
				stream.Write(getRequestBytes, 0, getRequestBytes.Length);

				// using BufferedStream here gives security exception for unknown reasons, so it's not buffered
				var reader = new BinaryReader (stream, new System.Text.ASCIIEncoding ());

				// check the result
				string responseStatusLine = ReadLine(reader);
				if(!responseStatusLine.Contains("200 OK")) {
					throw new MpException("Failed to connect. " + responseStatusLine);
				}
#else
				var request = HttpWebRequest.Create ((string)url);
				request.Timeout = (int)(timeout * 1000f);
				request.ContentType = "image/png,image/*;q=0.8,*"+"/*;q=0.5";

				BytesReceived = 0;
				stream = request.GetResponse ().GetResponseStream ();

				var reader = new BinaryReader (new BufferedStream (stream), new System.Text.ASCIIEncoding ());
#endif

				List<byte> byteBuf = new List<byte> (INITIAL_BYTE_BUFFER_SIZE);

				Status = "Connected. Waiting for the first frame...";
				connected = true;

				// run the main loop
				int s = 0;
				bool frameStarted = false;
				while (!shouldStop)
				{
					byte c = reader.ReadByte ();
					BytesReceived++;

					if (frameStarted) {
						// if the current frame we are reading is just too large, then drop it.
						// there's something wrong, possibly on sender side.
						if (byteBuf.Count > MAX_BYTE_BUFFER_SIZE) {
							byteBuf.Clear ();
							frameStarted = false;
							// @todo log corrupt data discard
						} else {
							byteBuf.Add (c);
						}
					}

					if (s == 0) {
						if (c == 0xFF)
							s = 1;
					} else if (s == 1) {
						if (c == 0xD8) { // JPEG start marker
							byteBuf.Clear ();
							byteBuf.Add (0xFF);
							byteBuf.Add (0xD8);
							frameStarted = true;
						} else if (c == 0xD9) { // JPEG end marker
							FrameReceived ((byte[])byteBuf.ToArray ());
							frameStarted = false;
							Thread.Sleep (1);
						}
						s = 0;
					}
				}

				Status = "Closing the connection"; // in finally block
			} catch (Exception e) {
				Status = e.ToString ();
			} finally {
				connected = false;
				if (stream != null)
					stream.Close ();
			}
		}

		private static string ReadLine(BinaryReader reader)
		{
			var line = new System.Text.StringBuilder(100);
			char c;
			while((c = reader.ReadChar()) != '\n') {
				if(c != '\r' && c != '\n') {
					line.Append(c);
				}
			}
			return line.ToString();
		}
		#endif // !UNITY_WINRT

		#endregion
	}
}
