using System.IO.Compression;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

public class FontProgramCompressionCacheTests
{
    [Fact]
    public void ExactContentIsReusedAndCallerMutationDoesNotPoisonCache()
    {
        var cache = new FontProgramCompressionCache(4096, 4);
        byte[] source = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        byte[] original = (byte[])source.Clone();
        byte[] first = cache.Compress(source);
        Assert.Same(first, cache.Compress(original));
        source[0] = 99;
        byte[] changed = cache.Compress(source);
        Assert.NotSame(first, changed);
        Assert.Equal(original, Inflate(first));
        Assert.Equal(source, Inflate(changed));
        cache.Clear();
        Assert.NotSame(first, cache.Compress(original));
    }

    [Fact]
    public void EntryLimitEvictsLeastRecentlyUsedAndOversizedProgramsAreNotRetained()
    {
        var cache = new FontProgramCompressionCache(100, 2);
        byte[] first = cache.Compress([1]);
        byte[] second = cache.Compress([2]);
        Assert.Same(first, cache.Compress([1]));
        cache.Compress([3]);
        Assert.NotSame(second, cache.Compress([2]));
        byte[] large = new byte[101];
        Assert.NotSame(cache.Compress(large), cache.Compress(large));
        Assert.NotSame(cache.Compress([4], retain: false), cache.Compress([4], retain: false));
    }

    [Fact]
    public void ByteBudgetCountsBothOriginalAndCompressedPrograms()
    {
        byte[] source = new byte[20];
        byte[] changed = (byte[])source.Clone();
        changed[0] = 1;
        var probe = new FontProgramCompressionCache(0, 0);
        int oneEntryCost = source.Length + Math.Max(probe.Compress(source).Length, probe.Compress(changed).Length);
        var cache = new FontProgramCompressionCache(oneEntryCost, 8);
        byte[] first = cache.Compress(source);
        Assert.Same(first, cache.Compress(source));
        cache.Compress(changed);
        Assert.NotSame(first, cache.Compress(source));
    }

    private static byte[] Inflate(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }
}
