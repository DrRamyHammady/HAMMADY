//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System;
using System.IO;
using System.Text;

namespace MP
{
	/// <summary>
	/// Like BinaryReader, but it's safe for multiple threads to read a stream using this class.
	/// </summary>
	public class AtomicBinaryReader
	{
		private readonly object locker = new object ();
		private BinaryReader reader;

		#region ----- Common public methods -----

		public AtomicBinaryReader (Stream stream) : this(stream, Encoding.UTF8)
		{
		}

		public AtomicBinaryReader (Stream stream, Encoding encoding)
		{
			reader = new BinaryReader (stream, encoding);
		}

		public void Close ()
		{
			if (reader != null) {
				#if !UNITY_WINRT
				reader.Close ();
				#else
				reader.BaseStream.Dispose ();
				#endif
			}
		}

		public long StreamLength {
			get {
				// locking isn't needed here, because we're treating the stream as read only
				return reader.BaseStream.Length;
			}
		}

		public long BytesLeft (long offset)
		{
			// locking isn't needed here, because we're treating the stream as read only
			return reader.BaseStream.Length - offset;
		}

		#endregion

		#region ----- Methods for reading -----

		public int Read (ref long offset, byte[] buffer, int index, int count)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var bytesRead = reader.Read (buffer, index, count);
				offset += bytesRead;
				return bytesRead;
			}
		}

		public int Read (ref long offset, uint[] buffer, int index, int count)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				int i = 0;
				try {
					for (; i < count; i++) {
						buffer [index + i] = reader.ReadUInt32 ();
					}
				} catch (EndOfStreamException) {
				}
				return i;
			}
		}

		public byte ReadByte (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadByte ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public sbyte ReadSByte (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadSByte ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public short ReadInt16 (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadInt16 ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public ushort ReadUInt16 (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadUInt16 ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public int ReadInt32 (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadInt32 ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public uint ReadUInt32 (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadUInt32 ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public long ReadInt64 (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadInt64 ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public ulong ReadUInt64 (ref long offset)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadUInt64 ();
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		public byte[] ReadBytes (ref long offset, int count)
		{
			lock (locker) {
				reader.BaseStream.Seek (offset, SeekOrigin.Begin);
				var retval = reader.ReadBytes (count);
				offset = reader.BaseStream.Position;
				return retval;
			}
		}

		#endregion
	}
}
