//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;
using System.IO;
using System;

namespace MP.AVI
{
	/// <summary>
	/// Avi demux.
	/// </summary>
	public class AviDemux : Demux
	{
		#region ----- Constants -----

		// common RIFF chunk ids
		public const uint ID_AVI_ = 0x20495641;
		public const uint ID_AVIX = 0x58495641;
		public const uint ID_hdrl = 0x6C726468; // list. headers are in here
		public const uint ID_avih = 0x68697661; // main AVI header
		public const uint ID_strl = 0x6C727473; // list. header and format for one stream is here
		public const uint ID_strh = 0x68727473; // stream header
		public const uint ID_strf = 0x66727473; // stream format (bitmap info, wave format)
		public const uint ID_odml = 0x6C6D646F; // list. OpenDML extensions
		public const uint ID_dmlh = 0x686C6D64; // extended AVI header
		public const uint ID_movi = 0x69766F6D; // list. audio and video chunks are in here
		public const uint ID_00dc = 0x63643030; // "compressed" video chunk, stream 0
		public const uint ID_00db = 0x62643030; // "uncompressed" video chunk, stream 0
		public const uint ID_01wb = 0x62773130; // audio chunk, stream 1
		public const uint ID_idx1 = 0x31786469; // AVI index
		public const uint ID_indx = 0x78646E69; // OpenDML index

		public const uint FCC_vids = 0x73646976; // video stream header
		public const uint FCC_auds = 0x73647561; // audio stream header

		#endregion

		#region ----- Public methods and properties -----

		public AVIFile avi;

		/// <summary>
		/// Initialized the demux for given stream. After calling this you can
		/// query A/V stream info and create decoders to play back those streams.
		/// </summary>
		/// <param name="sourceStream">Source stream.</param>
		/// <param name="loadOptions">Load options.</param>
		public override void Init (Stream sourceStream, LoadOptions loadOptions = null)
		{
			var watch = new System.Diagnostics.Stopwatch ();
			watch.Start ();

			reader = new AtomicBinaryReader (sourceStream);
			
			var riffParser = new RiffParser (reader);
			avi = new AVIFile ();
			
			idx1EntryOffset = -1;
			idx1Offset = -1;
			currentStrh4CC = 0;
			while (riffParser.ReadNext(ProcessAviChunk, ProcessAviList, ProcessAviRiff))
				;
			
			if (avi.strhVideo != null) {
				videoStreamInfo = new VideoStreamInfo ();
				videoStreamInfo.codecFourCC = avi.strhVideo.fccHandler;
				videoStreamInfo.bitsPerPixel = avi.strfVideo.biBitCount;
				videoStreamInfo.frameCount = avi.odml != null ? (int)avi.odml.dwTotalFrames : (int)avi.avih.dwTotalFrames;
				videoStreamInfo.width = (int)avi.avih.dwWidth;
				videoStreamInfo.height = (int)avi.avih.dwHeight;
				videoStreamInfo.framerate = (float)avi.strhVideo.dwRate / (float)avi.strhVideo.dwScale;
			} else {
				videoStreamInfo = null;
			}
			if (avi.strhAudio != null) {
				audioStreamInfo = new AudioStreamInfo ();
				audioStreamInfo.codecFourCC = avi.strhAudio.fccHandler;
				audioStreamInfo.audioFormat = avi.strfAudio.wFormatTag;
				audioStreamInfo.sampleCount = (int)avi.strhAudio.dwLength;
				audioStreamInfo.sampleSize = (int)avi.strhAudio.dwSampleSize;
				audioStreamInfo.channels = (int)avi.strfAudio.nChannels;
				audioStreamInfo.sampleRate = (int)avi.strfAudio.nSamplesPerSec;
			} else {
				audioStreamInfo = null;
			}

			// we may already have indexes here. it happens when the AVI contained OpenDML indx elements.
			// if we don't have indexes yet, then try to parse then out from an old idx1 chunk.
			if (hasVideo) {
				if (avi.videoIndex == null) {
					avi.videoIndex = ParseOldIndex (idx1Offset, riffParser.reader, idx1Size, AviDemux.ID_00dc, idx1EntryOffset);
				}
				if (avi.videoIndex == null) {
					// currently we're just throwing an exception here, but we could also rebuild the index. It's slow, but doable.
					throw new MpException ("No video index found (required for playback and seeking)");
				}
				PrepareVideoStream ();
			}
			if (hasAudio) {
				if (avi.audioIndex == null) {
					avi.audioIndex = ParseOldIndex (idx1Offset, riffParser.reader, idx1Size, AviDemux.ID_01wb, idx1EntryOffset);
				}
				if (avi.audioIndex == null) {
					// currently we're just throwing an exception here, but we could also rebuild the index. It's slow, but doable.
					throw new MpException ("No audio index found (required for playback and seeking)");
				}
				PrepareAudioStream ();
			}

			// if not all the frames are indexed, fix it
			if (videoStreamInfo != null && avi.videoIndex != null && videoStreamInfo.frameCount > avi.videoIndex.entries.Count) {
				#if MP_DEBUG
				Debug.LogWarning("Not all video frames are indexed. Adjusting video length to match indexed frame count " +
				                 avi.videoIndex.entries.Count + ". AVI header told that there should be " +
				                 videoStreamInfo.frameCount + " frames." + " It's likely that your encoder has a bug.");
				#endif
				videoStreamInfo.frameCount = avi.videoIndex.entries.Count;
			}

			watch.Stop ();
			#if MP_DEBUG
			Debug.Log ("AVI loaded in " + (watch.Elapsed.TotalMilliseconds * 0.001f) + " seconds");
			#endif

			nextVideoFrame = 0;
			nextAudioSample = 0;
		}

		public override void Shutdown (bool force = false)
		{
			// nothing to do here. this class instance doesn't hold any exposed resources on its own
		}

		public override int VideoPosition { get { return nextVideoFrame; } set { nextVideoFrame = value; } }

		public override int ReadVideoFrame (out byte[] targetBuf)
		{
			targetBuf = rawVideoBuf;
			if (nextVideoFrame < 0 || nextVideoFrame >= videoStreamInfo.frameCount)
				return 0;

			var indexEntry = avi.videoIndex.entries [nextVideoFrame++];
			long offs = indexEntry.chunkOffset;
			return reader.Read (ref offs, rawVideoBuf, 0, indexEntry.chunkLength);
		}

		public override int AudioPosition { get { return nextAudioSample; } set { nextAudioSample = value; } }

		public override int ReadAudioSamples (out byte[] targetBuf, int sampleCount)
		{
			// usually 1, 2 or 4 for PCM audio (eg 16bit*2channels=4)
			int audioSampleSize = audioStreamInfo.sampleSize;
			int count = sampleCount * audioSampleSize;
			
			if (rawAudioBuf == null || rawAudioBuf.Length < count) {
				rawAudioBuf = new byte[count];
			}
			
			long position = nextAudioSample * audioSampleSize;
			nextAudioSample += sampleCount;
			
			// sanity check
			if (rawAudioBuf.Length < count) {
				//count = rawAudioBuf.Length;
				throw new ArgumentException ("array.Length < count");
			}
			
			// find the <pos> where <chunk> contains the first byte requested. <chunk>=index[audioByteIndex[pos]]
			//int pos = audioByteIndex.BinarySearch (position);
			int pos = Array.BinarySearch (audioByteIndex, position);
			if (pos == -1) {
				// Sanity check, this should never happen.
				// Only happens when this.position<audioByteIndex[0], but audioByteIndex[0] is always 0 and this.position>=0
				throw new MpException ("audioByteIndex is corrupted");
			}
			// BinarySearch returns negative values if the match was not exact. Convert it to positive.
			if (pos < 0)
				pos = -pos - 2;
			
			// now read all the requested bytes into target rawAudioBuf
			int writeOffset = 0;
			long readOffset = position;
			int bytesLeftToRead = count;
			
			int totalBytesActuallyRead = 0;
			int offsetInChunk, bytesToRead, bytesActuallyRead;
			do {
				AviStreamIndex.Entry chunkIndex = avi.audioIndex.entries [pos];
				
				offsetInChunk = (int)(readOffset - audioByteIndex [pos]);
				bytesToRead = chunkIndex.chunkLength - offsetInChunk;
				if (bytesToRead > bytesLeftToRead)
					bytesToRead = bytesLeftToRead;
				
				//UnityEngine.Debug.Log ("READ " + offsetInChunk+"("+(chunkIndex.chunkOffset + offsetInChunk)+"):"+bytesToRead+"@chunk_"+pos +
				//           " into " + writeOffset + ":" + bytesToRead + "@buffer");
				
				long offs = chunkIndex.chunkOffset + offsetInChunk;
				bytesActuallyRead = reader.Read (ref offs, rawAudioBuf, writeOffset, bytesToRead);
				
				totalBytesActuallyRead += bytesActuallyRead;
				
				//Debug.Log ("Actually read " + bytesActuallyRead);
				
				bytesLeftToRead -= bytesActuallyRead;
				offsetInChunk += bytesActuallyRead;
				writeOffset += bytesActuallyRead;
				readOffset += bytesActuallyRead;
				
				// if current chunk is fully read, only then advance to next index chunk
				if (offsetInChunk >= (int)chunkIndex.chunkLength)
					pos++;
			} while(bytesLeftToRead > 0 && bytesActuallyRead == bytesToRead && pos < audioByteIndex.Length);
			
			targetBuf = rawAudioBuf;
			return totalBytesActuallyRead / audioSampleSize;
		}

		#endregion

		#region ----- Private members -----

		private AtomicBinaryReader reader;
		private uint currentStrh4CC;
		private long idx1EntryOffset;
		private long idx1Offset;
		private int idx1Size; // bytes

		private byte[] rawVideoBuf;
		private byte[] rawAudioBuf;
		
		// maps continous stream chunks to idx1 which contains other streams too
		// idx1 [ index [ <thisStreamChunkNr> ] ]
		private long[] audioByteIndex;

		// state for ReadNext
		private int nextVideoFrame;
		private int nextAudioSample;

		private static AVIMainHeader ParseMainHeader (AtomicBinaryReader br, long p)
		{
			var avih = new AVIMainHeader ();
			avih.dwMicroSecPerFrame = br.ReadUInt32 (ref p);
			avih.dwMaxBytesPerSec = br.ReadUInt32 (ref p);
			avih.dwPaddingGranularity = br.ReadUInt32 (ref p);
			avih.dwFlags = br.ReadUInt32 (ref p);
			avih.dwTotalFrames = br.ReadUInt32 (ref p);
			avih.dwInitialFrames = br.ReadUInt32 (ref p);
			avih.dwStreams = br.ReadUInt32 (ref p);
			avih.dwSuggestedBufferSize = br.ReadUInt32 (ref p);
			avih.dwWidth = br.ReadUInt32 (ref p);
			avih.dwHeight = br.ReadUInt32 (ref p);
			avih.dwReserved0 = br.ReadUInt32 (ref p);
			avih.dwReserved1 = br.ReadUInt32 (ref p);
			avih.dwReserved2 = br.ReadUInt32 (ref p);
			avih.dwReserved3 = br.ReadUInt32 (ref p);
			return avih;
		}

		private static AVIStreamHeader ParseStreamHeader (AtomicBinaryReader br, long p)
		{
			var strh = new AVIStreamHeader ();
			strh.fccType = br.ReadUInt32 (ref p);
			strh.fccHandler = br.ReadUInt32 (ref p);
			strh.dwFlags = br.ReadUInt32 (ref p);
			strh.wPriority = br.ReadUInt16 (ref p);
			strh.wLanguage = br.ReadUInt16 (ref p);
			strh.dwInitialFrames = br.ReadUInt32 (ref p);
			strh.dwScale = br.ReadUInt32 (ref p);
			strh.dwRate = br.ReadUInt32 (ref p);
			strh.dwStart = br.ReadUInt32 (ref p);
			strh.dwLength = br.ReadUInt32 (ref p);
			strh.dwSuggestedBufferSize = br.ReadUInt32 (ref p);
			strh.dwQuality = br.ReadUInt32 (ref p);
			strh.dwSampleSize = br.ReadUInt32 (ref p);
			strh.rcFrameLeft = br.ReadInt16 (ref p);
			strh.rcFrameTop = br.ReadInt16 (ref p);
			strh.rcFrameRight = br.ReadInt16 (ref p);
			strh.rcFrameBottom = br.ReadInt16 (ref p);
			return strh;
		}

		private static BitmapInfoHeader ParseVideoFormatHeader (AtomicBinaryReader br, long p)
		{
			var strf = new BitmapInfoHeader ();
			strf.biSize = br.ReadUInt32 (ref p);
			strf.biWidth = br.ReadInt32 (ref p);
			strf.biHeight = br.ReadInt32 (ref p);
			strf.biPlanes = br.ReadUInt16 (ref p);
			strf.biBitCount = br.ReadUInt16 (ref p);
			strf.biCompression = br.ReadUInt32 (ref p);
			strf.biSizeImage = br.ReadUInt32 (ref p);
			strf.biXPelsPerMeter = br.ReadInt32 (ref p);
			strf.biYPelsPerMeter = br.ReadInt32 (ref p);
			strf.biClrUsed = br.ReadUInt32 (ref p);
			strf.biClrImportant = br.ReadUInt32 (ref p);
			return strf;
		}

		private static WaveFormatEx ParseAudioFormatHeader (AtomicBinaryReader br, long p)
		{
			var strf = new WaveFormatEx ();
			strf.wFormatTag = br.ReadUInt16 (ref p);
			strf.nChannels = br.ReadUInt16 (ref p);
			strf.nSamplesPerSec = br.ReadUInt32 (ref p);
			strf.nAvgBytesPerSec = br.ReadUInt32 (ref p);
			strf.nBlockAlign = br.ReadUInt16 (ref p);
			strf.wBitsPerSample = br.ReadUInt16 (ref p);
			strf.cbSize = br.ReadUInt16 (ref p);
			return strf;
		}

		private static ODMLHeader ParseOdmlHeader (AtomicBinaryReader br, long p)
		{
			var odml = new ODMLHeader ();
			odml.dwTotalFrames = br.ReadUInt32 (ref p);
			return odml;
		}

		private static AviStreamIndex ParseOldIndex (long idx1Offset, AtomicBinaryReader abr, int size, uint streamId, long idx1EntryOffset)
		{
			int count = (int)(size / 16);

			var index = new AviStreamIndex ();
			index.streamId = streamId;
			index.entries.Capacity = count; // less memory allocation, more used temporarily

			long p = idx1Offset;
			var uintBuf = new uint[count * 4];
			abr.Read (ref p, uintBuf, 0, count * 4);
			
			for (int i = 0; i < count; i++) {
				uint ckid = uintBuf [i * 4];
				if (ckid == streamId || (ckid == AviDemux.ID_00db && streamId == AviDemux.ID_00dc)) {
					var entry = new AviStreamIndex.Entry ();
					entry.isKeyframe = (uintBuf [i * 4 + 1] & 0x00000010) != 0;
					entry.chunkOffset = idx1EntryOffset + uintBuf [i * 4 + 2];
					entry.chunkLength = (int)uintBuf [i * 4 + 3];
					index.entries.Add (entry);
				}
			}
			return index;
		}

		private static AviStreamIndex ParseOdmlIndex (AtomicBinaryReader reader, long p, out uint streamId)
		{
			ushort wLongsPerEntry = reader.ReadUInt16 (ref p);
			byte bSubIndexType = reader.ReadByte (ref p);
			byte bIndexType = reader.ReadByte (ref p);
			uint nEntriesInUse = reader.ReadUInt32 (ref p);
			streamId = reader.ReadUInt32 (ref p);

			var index = new AviStreamIndex ();
			index.streamId = streamId;

			// if there is AVI_INDEX_OF_CHUNKS (superindex) in this element
			if (bIndexType == (byte)AviStreamIndex.Type.SUPERINDEX) {
				p += 3 * 4; // not caring about reserved bytes

				#if MP_DEBUG
				//Debug.Log("Parsing superindex for " + RiffParser.FromFourCC(streamId));
				#endif

				// sanity check
				if (bSubIndexType != 0 || wLongsPerEntry != 4) {
					#if MP_DEBUG
					Debug.LogWarning("Broken superindex for stream " + RiffParser.FromFourCC(streamId) +
					                 ", but trying to continue. " + bSubIndexType + " " + wLongsPerEntry);
					#endif
				}

				for (uint i = 0; i < nEntriesInUse; i++) {
					long qwOffset = reader.ReadInt64 (ref p);
					int dwSize = reader.ReadInt32 (ref p);
					reader.ReadInt32 (ref p); // dwDuration. don't care

					if (qwOffset != 0) {
						long currentStreamPos = p;
						p = qwOffset;

						// reduce memory allocations by (over)estimating entry count from index size in bytes
						index.entries.Capacity += dwSize / 8;
						ParseChunkIndex (reader, p, ref index);

						p = currentStreamPos;
					}
				}
			}
			// if there is AVI_INDEX_OF_CHUNKS (chunk index) in here
			else if (bIndexType == (byte)AviStreamIndex.Type.CHUNKS) {
				// seek back to the beginning of this chunk (12bytes read here, 8bytes read by RiffParser)
				ParseChunkIndex (reader, p - 20, ref index);
			} else {
				throw new MpException ("Unsupported index type " + bIndexType +
					" encountered for stream " + RiffParser.FromFourCC (streamId));
			}
			index.entries.TrimExcess ();
			return index;
		}

		private static void ParseChunkIndex (AtomicBinaryReader reader, long p, ref AviStreamIndex index)
		{
			// read ix.. chunk id and size. do sanity check
			uint ixChunkFCC = reader.ReadUInt32 (ref p);
			uint ixChunkFCCb = (ixChunkFCC & 0x0000FFFF) | 0x20200000;
			if (ixChunkFCCb != RiffParser.ToFourCC ("ix  ") && ixChunkFCC != RiffParser.ToFourCC ("indx")) {
				throw new MpException ("Unexpected chunk id for index " + RiffParser.FromFourCC (ixChunkFCC) +
					" for stream " + RiffParser.FromFourCC (index.streamId));
			}
			uint ixChunkSize = reader.ReadUInt32 (ref p);

			// read index data header and do sanity check
			ushort wLongsPerEntry = reader.ReadUInt16 (ref p);
			byte bSubIndexType = reader.ReadByte (ref p);
			byte bIndexType = reader.ReadByte (ref p);
			uint nEntriesInUse = reader.ReadUInt32 (ref p);
			uint streamId = reader.ReadUInt32 (ref p);

			#if MP_DEBUG
			//Debug.Log("Parsing index for " + RiffParser.FromFourCC(index.streamId));
			#endif

			if (bIndexType != (int)AviStreamIndex.Type.CHUNKS || bSubIndexType != 0 || streamId != index.streamId ||
				wLongsPerEntry != 2 || ixChunkSize < 4 * wLongsPerEntry * nEntriesInUse + 24) {
				throw new MpException ("Broken or unsupported index for stream " + RiffParser.FromFourCC (streamId) +
					". " + streamId + "!=" + index.streamId + ", wLongsPerEntry=" + wLongsPerEntry +
					", bIndexType=" + bIndexType + ", bSubIndexType=" + bSubIndexType);
			}

			long qwBaseOffset = reader.ReadInt64 (ref p);
			p += 4; // not caring about reserved bytes

			// reading it all at once is about 10x faster than reading individual uints.
			// the index chunk is not that big, so it's ok for GC too.
			var uintBuf = new uint[nEntriesInUse * 2];
			reader.Read (ref p, uintBuf, 0, (int)nEntriesInUse * 2);

			for (int i = 0; i < nEntriesInUse; i++) {
				var entry = new AviStreamIndex.Entry ();
				entry.chunkOffset = qwBaseOffset + uintBuf [2 * i];
				uint len = uintBuf [2 * i + 1];
				entry.chunkLength = (int)(len & 0x7FFFFFFF);
				if ((len & 0x80000000) == 0)
					entry.isKeyframe = true;
				index.entries.Add (entry);
			}
		}

		private bool ProcessAviRiff (RiffParser rp, uint fourCC, int length)
		{
			//Debug.Log("RIFF " + RiffParser.FromFourCC(fourCC) + " " + rp.Position + " " + length);
			if (fourCC != AviDemux.ID_AVI_ && fourCC != AviDemux.ID_AVIX) {
				throw new MpException ("Not an AVI");
			}
			return true;
		}

		private bool ProcessAviList (RiffParser rp, uint fourCC, int length)
		{
			//Debug.Log("LIST " + RiffParser.FromFourCC(fourCC) + " " + rp.Position + " " + length);
			if (fourCC == ID_movi) {
				// only if this is the first movi element
				if (idx1EntryOffset < 0) {
					// point to the starting offset of "movi" LIST data
					idx1EntryOffset = rp.Position + 4;
				}
			}

			// process chunks and lists only that are inside these two LIST elements
			return fourCC == ID_hdrl || fourCC == ID_strl || fourCC == ID_odml;
		}

		private void ProcessAviChunk (RiffParser rp, uint fourCC, int unpaddedLength, int paddedLength)
		{
			//Debug.Log("CHUNK " + RiffParser.FromFourCC(fourCC) + " " + rp.Position + " " + unpaddedLength + " " + paddedLength);
			switch (fourCC) {
			case ID_avih:
				avi.avih = ParseMainHeader (rp.reader, rp.Position);
				break;
			
			case ID_strh:
				AVIStreamHeader strh = ParseStreamHeader (rp.reader, rp.Position);
				currentStrh4CC = strh.fccType;
				if (currentStrh4CC == FCC_vids) {
					avi.strhVideo = strh;
				} else if (currentStrh4CC == FCC_auds) {
					avi.strhAudio = strh;
				} else {
					#if MP_DEBUG
					Debug.LogWarning("Skipping unknown stream header with fccType=" + currentStrh4CC);
					#endif
				}
				break;
			
			case ID_strf:
				if (currentStrh4CC == FCC_vids) {
					avi.strfVideo = ParseVideoFormatHeader (rp.reader, rp.Position);
				} else if (currentStrh4CC == FCC_auds) {
					avi.strfAudio = ParseAudioFormatHeader (rp.reader, rp.Position);
				}
				break;
			
			case ID_idx1:
				idx1Offset = rp.Position;
				idx1Size = unpaddedLength;
				// index.idx1 will be filled later in ParseIndex method
				break;
			
			case ID_dmlh: // OpenDML header
				avi.odml = ParseOdmlHeader (rp.reader, rp.Position);
				break;
			
			case ID_indx: // OpenDML index
				uint streamId;
				var index = ParseOdmlIndex (rp.reader, rp.Position, out streamId);
				if (streamId == ID_00dc || streamId == ID_00db) {
					avi.videoIndex = index;
				} else if (streamId == ID_01wb) {
					avi.audioIndex = index;
				}
				#if MP_DEBUG
				else {
					Debug.LogWarning("Ignoring index for unknown stream " + RiffParser.FromFourCC(streamId));
				}
				#endif
				break;

			#if MP_DEBUG
			default:
				// comment in for logging chunks that are ignored (not relevant for us)
				//Debug.Log("Ignoring CHUNK " + RiffParser.FromFourCC(fourCC) + " " + unpaddedLength + " " + paddedLength);
				break;
			#endif
			}
		}

		private void PrepareAudioStream ()
		{
			long totalLength = 0;
			int maxChunkLength = 0;

			var entries = avi.audioIndex.entries;
			audioByteIndex = new long[entries.Count];
			
			for (int i = 0; i < entries.Count; i++) {
				var entry = entries [i];
				audioByteIndex [i] = totalLength;
				
				totalLength += entry.chunkLength;
				
				if (entry.chunkLength > maxChunkLength) {
					maxChunkLength = entry.chunkLength;
				}
			}
			rawAudioBuf = new byte[maxChunkLength];
			audioStreamInfo.lengthBytes = totalLength;
		}

		private void PrepareVideoStream ()
		{
			long totalLength = 0;
			int maxChunkLength = 0;
			
			var entries = avi.videoIndex.entries;

			for (int i = 0; i < entries.Count; i++) {
				var entry = entries [i];
				totalLength += entry.chunkLength;
				if (entry.chunkLength > maxChunkLength) {
					maxChunkLength = entry.chunkLength;
				}
			}
			rawVideoBuf = new byte[maxChunkLength];
			videoStreamInfo.lengthBytes = totalLength;
		}

		#endregion
	}
}
