//--------------------------------------------
// Movie Player
// Copyright © 2014-2015 SHUU Games
//--------------------------------------------
using UnityEngine;

namespace MP
{
	/// <summary>
	/// Duplicate frame finder.
	///
	/// Simple usage:
	///   var dff = new DuplicateFrameFinder(...);
	///   while(dff.Progress());
	///   ... = dff.duplicates;
	/// </summary>
	public class DuplicateFrameFinder
	{
		#region ----- public members -----

		/// <summary>
		/// All the options that control how images are compared.
		/// Default values should give a good balance between speed, accuracy and memory consumption.
		/// </summary>
		public class Options
		{
			public float maxImageDiff = 5f;
			public int maxPixelDiff = 50;
			public int maxLookbackFrames = 100;
			public int toneCompareDistrust = 2;
			public int pixelCacheSize = 101; // frame count
			public bool otherStreamsAvailable;

			public static Options Default { get { return new Options (); } }
		}

		/// <summary>
		/// Returns how many frames are processed through at any given time.
		/// Compare it to frameCount to get progress.
		/// </summary>
		public int framesProcessed { get { return currentFrame; } }

		/// <summary>
		/// Returns the duplicates found so far. This is the main output of this class.
		/// 
		/// Frames are processed in sequential manner. All entries in this
		/// array that are within [0..framesProcessed-1] range are fixed and
		/// won't change while processing continues.
		/// 
		/// duplicate[i]=j means that frame i and j are duplicates.
		/// If j=-1, this frame i is unique. Also, j<i always.
		/// </summary>
		public int[] duplicates { get { return duplicateOf; } }

		/// <summary>
		/// Stats that are updated while processing.
		/// </summary>
		public struct Stats
		{

			public long totalFramesCompared;
			public long framesPartiallyCompared;
			public long framesFullyCompared;

			public float framesPartiallyComparedPercent { get { return (float)framesPartiallyCompared / (float)totalFramesCompared * 100f; } }

			public float framesFullyComparedPercent { get { return (float)framesFullyCompared / (float)totalFramesCompared * 100f; } }
			
			public long pixelCacheQueries;
			public long pixelCacheHits;

			public float pixelCacheHitPercent { get { return (float)pixelCacheHits / (float)pixelCacheQueries * 100f; } }
			
			public int duplicateCount;
		}
		public Stats stats;

		/// <summary>
		/// Initializes a duplicate frame finder.
		/// </summary>
		/// <param name="videoDecoder">Video decoder is used to access frame pixel info for comparison</param>
		/// <param name="framebuffer">Provide the same framebuffer here that was returned by video decoder init</param>
		/// <param name="frameOffset">Frame offset. Usually 0</param>
		/// <param name="frameCount">Frame count. Since the decoder doesn't know the frame count, provide it here</param>
		/// <param name="options">Options.</param>
		public DuplicateFrameFinder (VideoDecoder videoDecoder, Texture2D framebuffer, int frameOffset, int frameCount, Options options = null)
		{
			if (options == null)
				options = Options.Default;
			this.options = options;
			this.videoDecoder = videoDecoder;
			this.framebuffer = framebuffer;
			this.frameOffset = frameOffset;
			this.frameCount = frameCount;
			Reset ();
		}

		/// <summary>
		/// Reset this instance.
		/// </summary>
		public void Reset ()
		{
			Reset (frameOffset, frameCount, options);
		}

		/// <summary>
		/// Call this to start processing over again with different frame range and options.
		/// </summary>
		public void Reset (int frameOffset, int frameCount, Options options = null)
		{
			this.frameOffset = frameOffset;
			this.frameCount = frameCount;

			frameTones = new Color32[frameCount];
			duplicateOf = new int[frameCount];
			for (int i = 0; i < frameCount; i++)
				duplicateOf [i] = -1;

			if (options.pixelCacheSize > 0) {
				// can be arbitrary, but for good use, it should be at least options.maxLookbackFrames+1
				pixelCache = new Color32[options.pixelCacheSize][];
			} else {
				pixelCache = null;
			}

			// reset the state
			currentFrame = 0;

			stats = new Stats ();
		}

		/// <summary>
		/// Progresses forward by one frame. Returns TRUE if there are more.
		/// </summary>
		public bool Progress ()
		{
			if (currentFrame >= frameCount)
				return false;

			videoDecoder.Decode (currentFrame + frameOffset);
			Color32[] pixels;
			if (pixelCache != null) {
				pixels = pixelCache [currentFrame % pixelCache.Length] = framebuffer.GetPixels32 ();
			} else {
				pixels = framebuffer.GetPixels32 ();
			}

			// find the tone of this frame.
			// (32bit uint is fine if the image is up to 4Kx4K)
			uint r = 0, g = 0, b = 0, a = 0;
			foreach (var pixel in pixels) {
				r += pixel.r;
				g += pixel.g;
				b += pixel.b;
				a += pixel.a;
			}
			frameTones [currentFrame] = new Color32 ((byte)(r / (uint)pixels.Length),
			                            (byte)(g / (uint)pixels.Length),
			                            (byte)(b / (uint)pixels.Length),
			                            (byte)(a / (uint)pixels.Length));

			// considering frame j, is it similar enough to i so that we can say it's a duplicate?
			int compareToFrame = currentFrame - 1;
			int dupCandidatesConsidered = 0;
			while (dupCandidatesConsidered < options.maxLookbackFrames && compareToFrame >= 0) {
				// don't allow luukup chains
				if (duplicateOf [compareToFrame] < 0) {
					// if currentFrame and compareToFrame have the same tone, compare them pixel by pixel
					stats.totalFramesCompared++;
					if (Mathf.Abs (SqrPixelDiff (frameTones [currentFrame], frameTones [compareToFrame])) <= options.toneCompareDistrust * options.toneCompareDistrust) {
						// take the pixels from cache or decode them
						Color32[] pixels2;
						stats.pixelCacheQueries++;
						if (pixelCache != null && currentFrame - compareToFrame < pixelCache.Length) {
							stats.pixelCacheHits++;
							pixels2 = pixelCache [compareToFrame % pixelCache.Length];
						} else {
							videoDecoder.Decode (compareToFrame + frameOffset);
							pixels2 = framebuffer.GetPixels32 ();
						}

						// at first compare frames quickly using about 2% of all the pixels (every 53th pixel).
						// if the difference is below thresholds set in options, compare all the pixels
						stats.framesPartiallyCompared++;
						float sqrMeanDiff;
						int maxPixelDiff;
						ImageDiff (out sqrMeanDiff, out maxPixelDiff, pixels, pixels2, true, 53);
						if (sqrMeanDiff <= options.maxImageDiff && maxPixelDiff <= options.maxPixelDiff) {
							// compare all the pixels
							stats.framesFullyCompared++;
							ImageDiff (out sqrMeanDiff, out maxPixelDiff, pixels, pixels2, false, 1);
							if (sqrMeanDiff <= options.maxImageDiff && maxPixelDiff <= options.maxPixelDiff) {
								// duplicate found. register it.
								duplicateOf [currentFrame] = compareToFrame;
								stats.duplicateCount++;
								//Debug.Log("Duplicate " + currentFrame + " to " + compareToFrame);
								break;
							}
						}
					}
					if (!options.otherStreamsAvailable)
						dupCandidatesConsidered++;
				}
				if (options.otherStreamsAvailable)
					dupCandidatesConsidered++;
				compareToFrame--;
			}

			currentFrame++;
			return true;
		}

		#endregion

		#region ----- private members -----
		
		// the result this class generates.
		// each element is either -1, meaning that's it's unique
		// or a frame index, which contains the same video frame.
		private int[] duplicateOf;
		
		// cache and runtime variables
		private Color32[] frameTones;
		private Color32[][] pixelCache; // ring buffer [frameIndex % pixelCache.Length][pixelIndex]
		private int currentFrame;
		
		// properties set during construct or Reset()
		private VideoDecoder videoDecoder;
		private Texture2D framebuffer;
		private int frameOffset;
		private int frameCount;
		private Options options;

		private static int SqrPixelDiff (Color32 c1, Color32 c2)
		{
			int r = c1.r - c2.r;
			int g = c1.g - c2.g;
			int b = c1.b - c2.b;
			int a = c1.a - c2.a;
			return r * r + g * g + b * b + a * a;
		}

		/// <summary>
		/// "Mean Squares" comparison.
		/// Returns the sum of squared differences between intensity values.
		/// Turing fasterPixelCompare on, makes it compare only green channel giving 3x speed boost.
		/// Increasing considerEveryNthPixel makes it about N times faster, because less pixels are compared.
		/// </summary>
		private static void ImageDiff (out float sqrImageDiff, out int maxPixelDiff, Color32[] a, Color32[] b, bool fasterPixelCompare = false, int considerEveryNthPixel = 1)
		{
			long sum = 0;
			maxPixelDiff = 0;

			if (considerEveryNthPixel < 1)
				considerEveryNthPixel = 1; // safety
			int cnt = a.Length;

			if (fasterPixelCompare) {
				// fast (green channel only)
				for (int i = 0; i < cnt; i += considerEveryNthPixel) {
					int diff = a [i].g - b [i].g; // chose green, because it's in the middle of spectrum
					if (diff > maxPixelDiff)
						maxPixelDiff = diff;
					sum += diff * diff;
				}
				sum *= 4;
			} else {
				// accurate
				for (int i = 0; i < cnt; i += considerEveryNthPixel) {
					int diff = SqrPixelDiff (a [i], b [i]);
					sum += diff;
					if (diff > maxPixelDiff)
						maxPixelDiff = diff;
				}
				maxPixelDiff = Mathf.RoundToInt (Mathf.Sqrt (maxPixelDiff));
			}
			sqrImageDiff = (float)((double)sum / (double)a.Length);
		}
	}

	#endregion
}
