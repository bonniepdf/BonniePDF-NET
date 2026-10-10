using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace BonniePDF.Typography.TrueType;

/// <summary>Bounded reuse of exact font programs. PDF object IDs and encryption remain document-owned.</summary>
internal sealed class FontProgramCompressionCache
{
    internal static readonly FontProgramCompressionCache Shared = new(4 * 1024 * 1024, 16);
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private readonly LinkedList<(byte[] Source, byte[] Compressed)> _entries = new();
    private readonly int _maxBytes;
    private readonly int _maxEntries;
    private int _bytes;

    internal FontProgramCompressionCache(int maxBytes, int maxEntries)
    {
        _maxBytes = maxBytes;
        _maxEntries = maxEntries;
    }

    // Returned bytes are immutable to callers. Keep a source copy so caller mutation cannot poison a hit.
    internal byte[] Compress(byte[] source, bool retain = true)
    {
        if (retain)
        {
            lock (_gate)
            {
                for (var node = _entries.First; node != null; node = node.Next)
                {
                    if (!source.AsSpan().SequenceEqual(node.Value.Source)) continue;
                    _entries.Remove(node);
                    _entries.AddFirst(node);
                    return node.Value.Compressed;
                }
            }
        }

        using var stream = new MemoryStream();
        using (var zlib = new ZLibStream(stream, CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(source);
        byte[] compressed = stream.ToArray();
        long cost = (long)source.Length + compressed.Length;
        if (!retain || cost > _maxBytes || _maxEntries <= 0) return compressed;
        lock (_gate)
        {
            while (_entries.Count >= _maxEntries || _bytes + cost > _maxBytes)
            {
                var last = _entries.Last!.Value;
                _bytes -= last.Source.Length + last.Compressed.Length;
                _entries.RemoveLast();
            }
            _entries.AddFirst(((byte[])source.Clone(), compressed));
            _bytes += (int)cost;
        }
        return compressed;
    }

    internal void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
            _bytes = 0;
        }
    }
}
