//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace MP
{
	#region RiffWriterException
	public class RiffWriterException : System.ApplicationException
	{
		public RiffWriterException () : base()
		{
		}
		
		public RiffWriterException (string msg) : base(msg)
		{
		}
		
		public RiffWriterException (string msg, System.Exception inner) : base(msg, inner)
		{
		}

		#if !UNITY_WINRT
		public RiffWriterException (SerializationInfo info, StreamingContext ctx) : base(info, ctx)
		{
		}
		#endif
	}
	#endregion

	/// <summary>
	/// Riff file writer
	/// </summary>
	public class RiffWriter
	{
        #region ----- Constants -----

		public const uint RIFF4CC = 0x46464952;
		public const uint LIST4CC = 0x5453494C;

        #endregion

		#region ----- Public methods and properties -----

		public RiffWriter (Stream stream)
		{
			this.writer = new BinaryWriter (stream);
			stack = new Stack<long> ();
		}

		public RiffWriter (BinaryWriter writer)
		{
			this.writer = writer;
			stack = new Stack<long> ();
		}

		public void BeginRiff (uint fourCC)
		{
			Begin (RIFF4CC, fourCC);
		}

		public void BeginList (uint fourCC)
		{
			Begin (LIST4CC, fourCC);
		}

		public void BeginChunk (uint fourCC)
		{
			writer.Write (fourCC);
			stack.Push (writer.BaseStream.Position);
			writer.Write ((int)0); // dummy length
		}

		public void EndRiff ()
		{
			End ();
		}

		public void EndList ()
		{
			End ();
		}

		public void EndChunk ()
		{
			End ();
		}

		public void WriteChunk (uint fourCC, byte[] data, int size = -1)
		{
			if (size < 0)
				size = data.Length;

			writer.Write (fourCC);
			writer.Write (size);
			writer.Write (data, 0, size);
			if (size % 2 != 0)
				writer.Write ((byte)0); // padding
		}

		public BinaryWriter binaryWriter { get { return writer; } }

		public long currentElementSize {
			get {
				return writer.BaseStream.Position - stack.Peek ();
			}
		}

		public void Close ()
		{
			// end all loose RIFF and LIST elements, this way at least the file
			// will be consistent, although not exactly having elements you expect
			while (stack.Count > 0)
				End ();

			#if !UNITY_WINRT
			writer.Close ();
			writer.BaseStream.Close ();
			#else
			writer.BaseStream.Dispose();
			#endif
		}

        #endregion

		#region ----- Private members -----

		private BinaryWriter writer;
		private Stack<long> stack;

		private void Begin (uint what, uint fourCC)
		{
			writer.Write (what);
			stack.Push (writer.BaseStream.Position);
			writer.Write ((int)0); // dummy length
			writer.Write (fourCC);
		}
		
		private void End ()
		{
			long length = writer.BaseStream.Position - stack.Pop ();
			if (length > (long)int.MaxValue) {
				throw new RiffWriterException ("RIFF or LIST element too large for writing (" + length + " bytes)");
			}
			int intLength = (int)length;
			int padding = intLength % 2;
			if (padding > 0)
				writer.Write ((byte)0);
			writer.Seek (-intLength - padding, SeekOrigin.Current);
			writer.Write (intLength - 4);
			writer.Seek (intLength - 4 + padding, SeekOrigin.Current);
		}

		#endregion
	}
}
