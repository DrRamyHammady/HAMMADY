//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using System.Runtime.Serialization;
using System.IO;
using UnityEngine;

namespace MP
{
    #region RiffParserException
	public class RiffParserException : System.ApplicationException
	{
		public RiffParserException () : base()
		{
		}

		public RiffParserException (string msg) : base(msg)
		{
		}

		public RiffParserException (string msg, System.Exception inner) : base(msg, inner)
		{
		}

		#if !UNITY_WINRT
		public RiffParserException (SerializationInfo info, StreamingContext ctx) : base(info, ctx)
		{
		}
		#endif
	}
    #endregion

	/// <summary>
	/// Riff file parser.
	/// </summary>
	public class RiffParser
	{
        #region ----- Constants -----

		public const uint RIFF4CC = 0x46464952;
		public const uint RIFX4CC = 0x58464952;
		public const uint LIST4CC = 0x5453494C;

        #endregion

        #region ----- Delegates -----

		/// <summary>
		/// RIFF element callback (the file may contain more than one RIFF element).
		/// Returns wether the contents of the RIFF should be processed.
		/// </summary>
		public delegate bool ProcessRiffElement (RiffParser rp,uint fourCC,int length);

		/// <summary>
		/// LIST element callback. Returns wether the contents of the LIST should be processed.
		/// </summary>
		public delegate bool ProcessListElement (RiffParser rp,uint fourCC,int length);

		/// <summary>
		/// CHUNK element callback.
		/// </summary>
		public delegate void ProcessChunkElement (RiffParser rp,uint fourCC,int unpaddedLength,int paddedLength);

        #endregion

        #region ----- Public methods and properties -----

		/// <summary>
		/// Constructor. Header of the given RIFF stream will be read and processed here.
		/// </summary>
		/// <param name="stream">Stream.</param>
		public RiffParser (AtomicBinaryReader reader)
		{
			this.reader = reader;
			nextElementOffset = 0;

			// do the most basic file type check
			long p = 0;
			streamRiff = reader.ReadUInt32 (ref p);
			if (streamRiff != RIFF4CC && streamRiff != RIFX4CC) {
				throw new RiffParserException ("Error. Not a valid RIFF stream");
			}
		}

		/// <summary>
		/// Read the next RIFF element invoking the correct delegate.
		/// Returns FALSE if there are no more elements to read.
		/// </summary>
		/// <param name="chunk">Method to invoke if a CHUNK element is found</param>
		/// <param name="list">Method to invoke if a LIST element is found</param>
		/// <returns></returns>
		public bool ReadNext (ProcessChunkElement chunkCallback, ProcessListElement listCallback = null, ProcessRiffElement riffCallback = null)
		{
			// Done?
			if (reader.BytesLeft (nextElementOffset) < 8)
				return false; 

			// We have enough bytes, read
			uint fourCC = reader.ReadUInt32 (ref nextElementOffset);
			int size = reader.ReadInt32 (ref nextElementOffset);

			// Do we have enough bytes?
			if (reader.BytesLeft (nextElementOffset) < size) {
				// Skip over the bad data and throw an exception
				nextElementOffset = size;
				throw new RiffParserException ("Element size mismatch for element " +
					FromFourCC (fourCC) + " need " + size.ToString ());
			}

			// Examine the element, is it a list or a chunk
			if (fourCC == RIFF4CC || fourCC == RIFX4CC) {
				// we have RIFF head element
				fourCC = reader.ReadUInt32 (ref nextElementOffset);

				// Truncated?
				if (reader.StreamLength < nextElementOffset + size - 4) {
					throw new RiffParserException ("Error. Truncated stream");
				}

				if (riffCallback != null) {
					bool processRiffContents = riffCallback (this, fourCC, size - 4);
					if (!processRiffContents) {
						nextElementOffset += size - 4;
					}
				}
			} else if (fourCC == LIST4CC) {
				// We have a list
				fourCC = reader.ReadUInt32 (ref nextElementOffset);

				if (listCallback != null) {
					bool processListContents = listCallback (this, fourCC, size - 4);
					if (!processListContents) {
						nextElementOffset += size - 4;
					}
				}
			} else {
				// Calculated padded size - padded to WORD boundary
				int paddedSize = size;
				if ((size & 1) != 0)
					paddedSize++;

				if (chunkCallback != null) {
					chunkCallback (this, fourCC, size, paddedSize);
				}

				nextElementOffset += paddedSize;
			}
			return true;
		}

		public void Rewind ()
		{
			nextElementOffset = 0;
		}

		/// <summary>
		/// Return the general file type (RIFF or RIFX);
		/// </summary>
		public uint StreamRIFF { get { return streamRiff; } }

		public long Position { get { return nextElementOffset; } }

		#endregion

		#region ----- Private members -----
		
		private long nextElementOffset;
		public readonly AtomicBinaryReader reader;
		private uint streamRiff;

		#endregion

		#region ----- FourCC conversion methods -----

		public static string FromFourCC (uint fourCC)
		{
			char[] chars = new char[4];
			chars [0] = (char)(fourCC & 0xFF);
			chars [1] = (char)((fourCC >> 8) & 0xFF);
			chars [2] = (char)((fourCC >> 16) & 0xFF);
			chars [3] = (char)((fourCC >> 24) & 0xFF);
			return new string (chars);
		}

		public static uint ToFourCC (string fourCC)
		{
			if (fourCC.Length != 4) {
				throw new RiffParserException ("FourCC strings must be 4 characters long " + fourCC);
			}
			return ((uint)fourCC [3]) << 24
				| ((uint)fourCC [2]) << 16
				| ((uint)fourCC [1]) << 8
				| ((uint)fourCC [0]);
		}

		public static uint ToFourCC (char[] fourCC)
		{
			if (fourCC.Length != 4) {
				throw new RiffParserException ("FourCC char arrays must be 4 characters long " + new string (fourCC));
			}
			return ((uint)fourCC [3]) << 24
				| ((uint)fourCC [2]) << 16
				| ((uint)fourCC [1]) << 8
				| ((uint)fourCC [0]);
		}

		public static uint ToFourCC (char c0, char c1, char c2, char c3)
		{
			return ((uint)c3) << 24
				| ((uint)c2) << 16
				| ((uint)c1) << 8
				| ((uint)c0);
		}

        #endregion
	}
}
