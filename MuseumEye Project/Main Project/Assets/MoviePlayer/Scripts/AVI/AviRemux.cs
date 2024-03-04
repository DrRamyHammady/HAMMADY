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
	/// Avi remux.
	/// </summary>
	public class AviRemux : Remux
	{
		#region ----- private members -----

		private int maxSuperindexEntries;
		private int maxRiffElementSize; // technically it's editable while remux is in progress, but why would you

		private RiffWriter writer;
		private AviStreamIndex videoIndex;
		private int videoSuperIndexEntryCount; // used slots

		private AviStreamIndex audioIndex;
		private int audioSuperIndexEntryCount; // used slots

		private bool usingMultipleRiffs;
		private int totalFramesOld; // frame count in first riff only
		private int totalFrames;
		private int totalSamples;

		private bool hasAudioStream { get { return audioStreamInfo != null; } }

		private struct ByteOffsets
		{
			public long indexBase; // used for both audio and video indexes. points to last movi chunk
			public int avih;
			public int videoStrh;
			public int videoIndx; // superindex
			public int audioStrh;
			public int audioIndx; // superindex
			public int dmlh;
		};
		private ByteOffsets offsets;

		#endregion

		#region ----- constructor and public methods -----

		/// <summary>
		/// Sets how many superindex entries to reserve in AVI header. Since the superindex
		/// can't grow, this will roughly limit the maximum file size. 32 = 64Gb (roughly)
		/// </summary>
		public AviRemux (int maxSuperindexEntries = 32, int maxRiffElementSize = 2000000000)
		{
			this.maxSuperindexEntries = maxSuperindexEntries;
			this.maxRiffElementSize = maxRiffElementSize;
		}

		public override void Init (Stream dstStream, VideoStreamInfo videoStreamInfo, AudioStreamInfo audioStreamInfo)
		{
			if (dstStream == null || videoStreamInfo == null) {
				throw new ArgumentException ("At least destination stream and video stream info is needed");
			}
			base.Init (dstStream, videoStreamInfo, audioStreamInfo);

			usingMultipleRiffs = false;
			totalFramesOld = 0;
			totalFrames = 0;
			totalSamples = 0;

			writer = new RiffWriter (dstStream);
			writer.BeginRiff (AviDemux.ID_AVI_);
			writer.BeginList (AviDemux.ID_hdrl);

			// main header
			offsets.avih = WriteMainHeader (writer, videoStreamInfo, hasAudioStream);

			// video stream header
			writer.BeginList (AviDemux.ID_strl);
			offsets.videoStrh = WriteVideoStreamHeader (writer, videoStreamInfo);
			WriteVideoFormatHeader (writer, videoStreamInfo);
			offsets.videoIndx = WriteDummySuperIndex (writer, AviDemux.ID_00dc, maxSuperindexEntries);
			videoSuperIndexEntryCount = 0;
			writer.EndList (); // end of strl

			videoIndex = new AviStreamIndex ();
			videoIndex.streamId = AviDemux.ID_00dc;

			if (hasAudioStream) {
				// audio stream header
				writer.BeginList (AviDemux.ID_strl);
				offsets.audioStrh = WriteAudioStreamHeader (writer, audioStreamInfo);
				WriteAudioFormatHeader (writer, audioStreamInfo);
				offsets.audioIndx = WriteDummySuperIndex (writer, AviDemux.ID_01wb, maxSuperindexEntries);
				audioSuperIndexEntryCount = 0;
				writer.EndList (); // end of strl

				audioIndex = new AviStreamIndex ();
				audioIndex.streamId = AviDemux.ID_01wb;
			}

			// odml header
			writer.BeginList (AviDemux.ID_odml);
			offsets.dmlh = WriteDmlhHeader (writer, videoStreamInfo.frameCount);
			writer.EndList ();

			writer.EndList (); // end of hdrl

			writer.BeginList (AviDemux.ID_movi);
			offsets.indexBase = writer.binaryWriter.Seek (0, SeekOrigin.Current);
		}

		public override void WriteNextVideoFrame (byte[] frameBytes, int size = -1)
		{
			// the 'movi' element is getting too big (1Gb+).
			// close it and start new RIFF AVIX element
			if (writer.currentElementSize > maxRiffElementSize)
				StartNewRiff ();

			if (size < 0)
				size = frameBytes.Length;

			var entry = new AviStreamIndex.Entry ();
			entry.chunkOffset = writer.binaryWriter.Seek (0, SeekOrigin.Current) + 8;
			entry.chunkLength = size;
			videoIndex.entries.Add (entry);

			writer.WriteChunk (AviDemux.ID_00dc, frameBytes, size);
			totalFrames++;
			if (!usingMultipleRiffs)
				totalFramesOld++;
		}

		public override void WriteVideoFrame (int frameOffset, byte[] frameBytes, int size = -1)
		{
			throw new System.NotSupportedException ("Only adding frames at the end is supported");
		}

		public bool WriteLookbackVideoFrame (int frame)
		{
			// if given frame number is negative, use it as relative lookback index
			// (useful when writing frames sequentially using WriteNextVideoFrame)
			if (frame < 0)
				frame = totalFrames - frame;

			int i = frame - videoIndex.globalOffset;
			if (i < 0 || i >= videoIndex.entries.Count) {
				// can't look up that frame, it's in different RIFF block
				return false;
			}
			var lookbackEntry = videoIndex.entries [i];

			var entry = new AviStreamIndex.Entry ();
			entry.chunkOffset = lookbackEntry.chunkOffset;
			entry.chunkLength = lookbackEntry.chunkLength;
			videoIndex.entries.Add (entry);
			
			totalFrames++;
			if (!usingMultipleRiffs)
				totalFramesOld++;
			return true;
		}

		// NB! always provide a multiple of sampleSize bytes!
		public override void WriteNextAudioSamples (byte[] sampleBytes, int size = -1)
		{
			// the 'movi' element is getting too big (1Gb+).
			// close it and start new RIFF AVIX element
			if (writer.currentElementSize > maxRiffElementSize)
				StartNewRiff ();

			if (size < 0)
				size = sampleBytes.Length;

			var entry = new AviStreamIndex.Entry ();
			entry.chunkOffset = writer.binaryWriter.Seek (0, SeekOrigin.Current) + 8;
			entry.chunkLength = size;
			audioIndex.entries.Add (entry);
			
			writer.WriteChunk (AviDemux.ID_01wb, sampleBytes);
			totalSamples += size / audioStreamInfo.sampleSize; // expecting this to be exact
		}

		public override void WriteAudioSamples (int sampleOffset, byte[] frameBytes, int size = -1)
		{
			throw new System.NotSupportedException ("Only adding samples at the end is supported");
		}

		public override void Shutdown ()
		{
			var bw = writer.binaryWriter;
			long pos = bw.Seek (0, SeekOrigin.Current);
			bw.Seek (offsets.avih + 4 * 4, SeekOrigin.Begin);
			bw.Write (totalFramesOld); // avih.dwTotalFrames
			bw.Seek (offsets.videoStrh + 8 * 4, SeekOrigin.Begin);
			bw.Write (totalFrames); // strh[vids].dwLength
			bw.Seek (offsets.dmlh, SeekOrigin.Begin);
			bw.Write (totalFrames); // dmlh.dwTotalFrames
			if (hasAudioStream) {
				bw.Seek (offsets.audioStrh + 8 * 4, SeekOrigin.Begin);
				bw.Write (totalSamples); // strh[auds].dwLength
			}
			bw.BaseStream.Seek (pos, SeekOrigin.Begin);

			if (videoIndex.entries.Count > 0) {
				WriteChunkIndex (writer, videoIndex, offsets.videoIndx, ref videoSuperIndexEntryCount, offsets.indexBase, maxSuperindexEntries);
			}
			if (hasAudioStream && audioIndex.entries.Count > 0) {
				WriteChunkIndex (writer, audioIndex, offsets.audioIndx, ref audioSuperIndexEntryCount, offsets.indexBase, maxSuperindexEntries);
			}

			writer.EndList (); // end of movi
			writer.EndRiff ();
			writer.Close ();
		}

		#endregion

		#region ----- private members -----

		private void StartNewRiff ()
		{
			if (videoIndex.entries.Count > 0) {
				WriteChunkIndex (writer, videoIndex, offsets.videoIndx, ref videoSuperIndexEntryCount, offsets.indexBase, maxSuperindexEntries);
			}
			if (hasAudioStream && audioIndex.entries.Count > 0) {
				WriteChunkIndex (writer, audioIndex, offsets.audioIndx, ref audioSuperIndexEntryCount, offsets.indexBase, maxSuperindexEntries);
			}
			writer.EndList (); // end of movi
			writer.EndRiff ();
			writer.BeginRiff (AviDemux.ID_AVIX);
			writer.BeginList (AviDemux.ID_movi);
			offsets.indexBase = writer.binaryWriter.Seek (0, SeekOrigin.Current);

			usingMultipleRiffs = true;
		}

		private static int WriteMainHeader (RiffWriter rw, VideoStreamInfo vsi, bool hasAudioStream)
		{
			rw.BeginChunk (AviDemux.ID_avih);
			int offset = (int)rw.binaryWriter.Seek (0, SeekOrigin.Current);
			var bw = rw.binaryWriter;
			bw.Write (Mathf.RoundToInt (1000000f / vsi.framerate)); // swMicroSecPerFrame
			bw.Write ((int)0); // dwMaxBytesPerSec
			bw.Write ((int)0); // dwPaddingGranularity
			bw.Write ((int)(AVIMainHeader.AVIF_HASINDEX | AVIMainHeader.AVIF_MUSTUSEINDEX)); // dwFlags. Maybe use AVIMainHeader.AVIF_MUSTUSEINDEX too?
			bw.Write (vsi.frameCount); // dwTotalFrames. this will be written over later!
			bw.Write ((int)0); // dwInitialFrames
			bw.Write (hasAudioStream ? 2 : 1);
			bw.Write ((int)0); // dwSuggestedBufferSize, not suggesting any value
			bw.Write (vsi.width);
			bw.Write (vsi.height);
			bw.Write ((long)0); // dwReserver0 and dwReserver1
			bw.Write ((long)0); // dwReserver2 and dwReserver3
			rw.EndChunk ();
			return offset;
		}

		private static int WriteVideoStreamHeader (RiffWriter rw, VideoStreamInfo vsi)
		{
			rw.BeginChunk (AviDemux.ID_strh);
			int offset = (int)rw.binaryWriter.Seek (0, SeekOrigin.Current);
			var bw = rw.binaryWriter;
			bw.Write (AviDemux.FCC_vids);
			bw.Write (vsi.codecFourCC);
			bw.Write ((int)0); // dwFlags
			bw.Write ((short)0); // wPriority
			bw.Write ((short)0); // wLanguage
			bw.Write ((int)0); // dwInitialFrames
			int scale, rate;
			FindScaleAndRate (out scale, out rate, vsi.framerate);
			bw.Write (scale); // dwScale
			bw.Write (rate); // dwRate
			bw.Write ((int)0); // dwStart
			bw.Write (vsi.frameCount); // dwLength. that's how many frames will be in this RIFF element, written over later
			bw.Write ((int)0); // dwSuggestedBufferSize, not suggesting any value
			bw.Write ((int)-1); // dwQuality = -1 meaning "default quality"
			bw.Write ((int)0); // dwSampleSize = 0 for video
			bw.Write ((short)0);
			bw.Write ((short)0);
			bw.Write ((short)vsi.width);
			bw.Write ((short)vsi.height);
			rw.EndChunk ();
			return offset;
		}

		private static int WriteAudioStreamHeader (RiffWriter rw, AudioStreamInfo asi)
		{
			rw.BeginChunk (AviDemux.ID_strh);
			int offset = (int)rw.binaryWriter.Seek (0, SeekOrigin.Current);
			var bw = rw.binaryWriter;
			bw.Write (AviDemux.FCC_auds);
			bw.Write (asi.codecFourCC);
			bw.Write ((int)0); // dwFlags
			bw.Write ((short)0); // wPriority
			bw.Write ((short)0); // wLanguage
			bw.Write ((int)0); // dwInitialFrames
			bw.Write ((int)1); // dwScale
			bw.Write (asi.sampleRate); // dwRate @xxx This is true for PCM audio only!
			bw.Write ((int)0); // dwStart
			bw.Write (asi.sampleCount); // dwLength. will be written over later
			bw.Write ((int)0); // dwSuggestedBufferSize, not suggesting it
			bw.Write ((int)-1); // dwQuality = -1 meaning "default quality"
			bw.Write (asi.sampleSize); // dwSampleSize
			bw.Write ((long)0); // rcFrame
			rw.EndChunk ();
			return offset;
		}

		private static void FindScaleAndRate (out int scale, out int rate, float framerate)
		{
			rate = Mathf.FloorToInt (framerate);
			scale = 1;
			while (rate < 1e+5) { // the check is just for safety
				float diff = (float)rate / (float)scale - framerate;
				if (Mathf.Abs (diff) < 1e-5)
					break; // found
				if (diff > 0) {
					scale++;
				} else {
					rate++;
				}
			}
		}
		
		private static void WriteVideoFormatHeader (RiffWriter rw, VideoStreamInfo vsi)
		{
			rw.BeginChunk (AviDemux.ID_strf);
			var bw = rw.binaryWriter;
			bw.Write ((int)40); // biSize
			bw.Write (vsi.width); // biWidth
			bw.Write (vsi.height); // biHeight
			bw.Write ((short)1); // biPlanes
			bw.Write ((short)vsi.bitsPerPixel);
			bw.Write (vsi.codecFourCC); // biCompression
			bw.Write (vsi.width * vsi.height * vsi.bitsPerPixel / 8); // biSizeImage
			bw.Write ((int)0); // biXPelsPerMeter
			bw.Write ((int)0); // biYPelsPerMeter
			bw.Write ((int)0); // biClrUsed
			bw.Write ((int)0); // biClrImportant
			rw.EndChunk ();
		}

		private static void WriteAudioFormatHeader (RiffWriter rw, AudioStreamInfo asi)
		{
			rw.BeginChunk (AviDemux.ID_strf);
			var bw = rw.binaryWriter;
			bw.Write ((ushort)asi.audioFormat); // wFormatTag
			bw.Write ((short)asi.channels); // nChannels
			bw.Write (asi.sampleRate); // nSamplesPerSec
			bw.Write (asi.sampleRate * asi.sampleSize * asi.channels); // nAvgBytesPerSec @xxx true for PCM audio only, but this is a "soft" property
			bw.Write ((short)asi.sampleSize); // nBlockAlign
			bw.Write ((short)(8 * asi.sampleSize / asi.channels)); // wBitsPerSample
			bw.Write ((short)0); // cbSize. no extra coded info
			rw.EndChunk ();
		}

		private static int WriteDmlhHeader (RiffWriter rw, int totalFrames)
		{
			rw.BeginChunk (AviDemux.ID_dmlh);
			int offset = (int)rw.binaryWriter.Seek (0, SeekOrigin.Current);
			rw.binaryWriter.Write (totalFrames); // will be written over later
			rw.EndChunk ();
			return offset;
		}

		private static int WriteDummySuperIndex (RiffWriter rw, uint streamId, int entriesToReserve)
		{
			rw.BeginChunk (AviDemux.ID_indx);
			int offset = (int)rw.binaryWriter.Seek (0, SeekOrigin.Current);
			var bw = rw.binaryWriter;
			bw.Write ((short)4); // wLongsPerEntry is always 4 for super index
			bw.Write ((byte)0); // bIndexSubType is always 0 for super index
			bw.Write ((byte)AviStreamIndex.Type.SUPERINDEX); // bIndexType = AVI_INDEX_OF_INDEXES
			bw.Write ((int)0); // nEntriesInUse. this'll be updated later!
			bw.Write (streamId); // dwChunkId ("##dc" and similar)
			// write 3 reserved UINTs and reserve some space for entries
			bw.Write (new byte[4 * 3 + entriesToReserve * 16]);
			rw.EndChunk ();
			return offset;
		}

		private static void WriteChunkIndex (RiffWriter rw, AviStreamIndex index, int superIndexChunkOffset, ref int superIndexEntryCount, long indexBaseOffset, int maxSuperindexEntries)
		{
			var bw = rw.binaryWriter;

			// the offset where this index will be written
			long streamIndexOffset = bw.Seek (0, SeekOrigin.Current);

			// update stream superindex
			superIndexEntryCount++;
			if (superIndexEntryCount > maxSuperindexEntries) {
				throw new MpException ("Not enough space was reserved for superindex. Please increase maxSuperindexEntries");
			}

			bw.Seek (superIndexChunkOffset + 1 * 4, SeekOrigin.Begin);
			bw.Write (superIndexEntryCount); // overwrite nEntriesInUse
			bw.Seek (superIndexChunkOffset + 6 * 4 + (superIndexEntryCount - 1) * 16, SeekOrigin.Begin);
			bw.Write (streamIndexOffset);
			bw.Write (32 + 8 * index.entries.Count); // dwSize
			bw.Write (index.entries.Count); // dwDuration in stream ticks. @todo this is OK only for video, for audio stream this should be ???

			// write stream chunk index
			// @xxx MSDN suggests not seeking BaseStream when using BinaryWriter, but there are no Seek(long)
			//      in BinaryWriter. According to this forum post, BinaryWriter.Seek is just a wrapper
			//      to BinaryWriter.BaseStream.Seek, so all should be ok.
			//      http://www.pcreview.co.uk/forums/binarywriter-seek-vs-binarywriter-basestream-seek-t1223754.html
			bw.BaseStream.Seek (streamIndexOffset, SeekOrigin.Begin);

			rw.BeginChunk ((RiffParser.ToFourCC ("ix__") & 0x0000FFFF) | ((index.streamId << 16) & 0xFFFF0000));
			bw.Write ((short)2); // wLongsPerEntry is always 2 here
			bw.Write ((byte)0); // bIndexSubType is always 0 here
			bw.Write ((byte)AviStreamIndex.Type.CHUNKS); // bIndexType = AVI_INDEX_OF_CHUNKS
			bw.Write (index.entries.Count); // nEntriesInUse.
			bw.Write (index.streamId); // dwChunkId ("##dc" and similar)
			bw.Write (indexBaseOffset); // qwBaseOffset
			bw.Write ((int)0); // dwReserved3

			foreach (var entry in index.entries) {
				long offset = entry.chunkOffset - indexBaseOffset;
				if (offset > int.MaxValue) {
					throw new MpException ("Internal error. Can't write index, because chunk offset won't fit into 31 bits: " + offset);
				}
				bw.Write ((uint)offset); // bit31==0 indicating that this is a keyframe
				bw.Write (entry.chunkLength);
			}
			rw.EndChunk ();

			index.globalOffset += index.entries.Count;
			index.entries.Clear ();
		}

		#endregion
	}
}
