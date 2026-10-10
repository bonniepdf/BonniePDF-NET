using System;
using System.Collections.Generic;
using System.Threading;

namespace BonniePDF;

/// <summary>
/// How hard BonniePDF works to shrink the Flate-compressed pixel data of embedded images.
/// </summary>
public enum ImageCompressionLevel
{
    /// <summary>
    /// The slowest, smallest setting (zlib level 9). Worth it only for a document that is written once
    /// and distributed widely, or when every byte of output counts.
    /// </summary>
    Smallest,

    /// <summary>
    /// The default (zlib level 6): roughly a quarter faster than <see cref="Smallest"/> for output that is
    /// typically about 1-2% larger.
    /// </summary>
    Balanced,

    /// <summary>The quickest setting (zlib level 1): the fastest to write, with noticeably larger images.</summary>
    Fast
}

/// <summary>
/// Process-wide tuning for the image pipeline: decoding, resolution capping and compression.
/// <para>
/// Decoding a PNG, reducing it to its printed resolution and compressing the result are the most
/// expensive things BonniePDF does to an image, and they depend only on the image. The pipeline
/// therefore keeps the results of that work in a bounded in-memory cache, so a logo, letterhead or
/// signature that appears on every generated document is processed once rather than once per document.
/// The cache never changes the output; it only skips work already done.
/// </para>
/// </summary>
public static class ImagePipeline
{
    /// <summary>The default size of the image cache: 32 MiB.</summary>
    public const long DefaultCacheMaxBytes = 32L * 1024 * 1024;

    private static long _cacheMaxBytes = DefaultCacheMaxBytes;
    private static int _compression = (int)ImageCompressionLevel.Balanced;

    /// <summary>
    /// The most memory, in bytes, the image cache may hold. It counts the decoded pixels and the original file
    /// bytes of each cached image (the compressed streams kept alongside are smaller and not counted). The
    /// least recently used images are dropped to stay within it, and an image larger than the whole budget is
    /// not cached. Set it to <c>0</c> to turn caching off, which also stops compressed streams being kept:
    /// nothing is retained between documents, at the cost of redoing the work for each one. Defaults to
    /// <see cref="DefaultCacheMaxBytes"/>.
    /// </summary>
    public static long CacheMaxBytes
    {
        get => Interlocked.Read(ref _cacheMaxBytes);
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            Interlocked.Exchange(ref _cacheMaxBytes, value);
            ImageCache.Trim();
        }
    }

    /// <summary>
    /// The compression applied to image pixel data (and image colour profiles) in every document written from
    /// now on. Defaults to <see cref="ImageCompressionLevel.Balanced"/>.
    /// </summary>
    public static ImageCompressionLevel CompressionLevel
    {
        get => (ImageCompressionLevel)Volatile.Read(ref _compression);
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value), value, null);
            Volatile.Write(ref _compression, (int)value);
        }
    }

    /// <summary>The memory, in bytes, the image cache currently holds against <see cref="CacheMaxBytes"/>.</summary>
    public static long CacheBytes => ImageCache.Bytes;

    /// <summary>Empties the image cache and releases the memory it held.</summary>
    public static void ClearCache() => ImageCache.Clear();
}

/// <summary>
/// A least-recently-used cache of processed images, bounded by <see cref="ImagePipeline.CacheMaxBytes"/>.
/// Entries are shared between documents and threads, so they are never mutated after they are added.
/// </summary>
internal static class ImageCache
{
    private sealed class Entry
    {
        public string Key = string.Empty;
        public ImageInfo Info = null!;
        public long Cost;
    }

#if NET9_0_OR_GREATER
    private static readonly System.Threading.Lock Gate = new();
#else
    private static readonly object Gate = new();
#endif
    private static readonly Dictionary<string, LinkedListNode<Entry>> Map = new(StringComparer.Ordinal);
    private static readonly LinkedList<Entry> Recent = new();
    private static long _bytes;

    internal static long Bytes => Interlocked.Read(ref _bytes);

    /// <summary>
    /// Returns the cached image for <paramref name="key"/>, or creates, caches and returns it. The factory runs
    /// outside the lock, so two threads racing on a new key may both build it; the first to finish wins.
    /// </summary>
    internal static ImageInfo GetOrAdd(string key, Func<ImageInfo> create)
    {
        long max = ImagePipeline.CacheMaxBytes;
        if (max <= 0) return create();

        lock (Gate)
        {
            if (Map.TryGetValue(key, out LinkedListNode<Entry>? hit))
            {
                Recent.Remove(hit);
                Recent.AddFirst(hit);
                return hit.Value.Info;
            }
        }

        ImageInfo info = create();
        long cost = CostOf(info);
        if (cost > max) return info;

        lock (Gate)
        {
            if (Map.TryGetValue(key, out LinkedListNode<Entry>? raced)) return raced.Value.Info;

            LinkedListNode<Entry> node = Recent.AddFirst(new Entry { Key = key, Info = info, Cost = cost });
            Map[key] = node;
            Interlocked.Add(ref _bytes, cost);
            TrimLocked(max);
        }

        return info;
    }

    internal static void Trim()
    {
        lock (Gate) TrimLocked(ImagePipeline.CacheMaxBytes);
    }

    internal static void Clear()
    {
        lock (Gate)
        {
            Map.Clear();
            Recent.Clear();
            Interlocked.Exchange(ref _bytes, 0);
        }
    }

    private static void TrimLocked(long max)
    {
        while (Recent.Last is { } oldest && Interlocked.Read(ref _bytes) > max)
        {
            Recent.RemoveLast();
            Map.Remove(oldest.Value.Key);
            Interlocked.Add(ref _bytes, -oldest.Value.Cost);
        }
    }

    private static long CostOf(ImageInfo info) =>
        (long)info.Data.Length + info.AlphaData.Length + info.OriginalData.Length + info.Palette.Length +
        info.IccProfile.Length;
}

/// <summary>
/// Process-wide tuning for the parsed-font cache. Reading and parsing a font file (its character map, widths and
/// kerning) is repeated work for every document that uses it, so fonts loaded from a file path are kept after
/// the first document and reused until the file changes. Fonts supplied as byte arrays are parsed once per array
/// and need no setting: the parse is kept for exactly as long as the array.
/// </summary>
public static class FontCache
{
    private static int _maxFiles = 16;

    /// <summary>
    /// The most font files kept parsed in memory at once; the least recently loaded is dropped first. Each costs
    /// roughly the size of the font file plus its character map. Set to <c>0</c> to turn the cache off, so every
    /// document reads and parses its fonts afresh. Defaults to 16.
    /// </summary>
    public static int MaxFiles
    {
        get => Volatile.Read(ref _maxFiles);
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            Volatile.Write(ref _maxFiles, value);
            if (value == 0) Clear();
        }
    }

    /// <summary>Drops every parsed font file held by the cache.</summary>
    public static void Clear() => BonniePDF.Typography.TrueType.TtfFont.ClearFileCache();
}
