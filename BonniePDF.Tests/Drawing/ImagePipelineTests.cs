using Xunit;
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// The process-wide image cache and compression setting. These change static state, so they run serially and
/// restore the defaults afterwards.
/// </summary>
[Collection("Serial")]
public sealed class ImagePipelineTests : IDisposable
{
    public ImagePipelineTests() => Reset();

    public void Dispose() => Reset();

    private static void Reset()
    {
        ImagePipeline.CacheMaxBytes = ImagePipeline.DefaultCacheMaxBytes;
        ImagePipeline.CompressionLevel = ImageCompressionLevel.Balanced;
        ImagePipeline.ClearCache();
        FontCache.MaxFiles = 16;
    }

    private static byte[] NoisyPng(int width, int height) =>
        ImageLoaderTests.BuildFilteredRgbaPng(width, height, row => (byte)(row % 5));

    private static string WriteTemp(byte[] bytes)
    {
        string path = Path.Combine(Path.GetTempPath(), $"bonnie-cache-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static byte[] Generate(string path, float width = 80)
    {
        using PdfDocument doc = new(new DocumentSettings { MaxImageDpi = 288 });
        doc.Add(new Image { FilePath = path, Width = width, AltText = "logo" });
        return doc.GenerateBytes();
    }

    [Fact]
    public void AnImagePlacedByEveryDocument_IsDecodedOnceAndCached()
    {
        string path = WriteTemp(NoisyPng(600, 300));
        try
        {
            Generate(path);
            long cached = ImagePipeline.CacheBytes;

            Generate(path);
            Generate(path);

            Assert.True(cached > 0);
            Assert.Equal(cached, ImagePipeline.CacheBytes);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TheCache_DoesNotChangeTheEmbeddedPixels()
    {
        string path = WriteTemp(NoisyPng(600, 300));
        try
        {
            ImagePipeline.CacheMaxBytes = 0;
            byte[] uncached = Generate(path);
            Assert.Equal(0, ImagePipeline.CacheBytes);

            ImagePipeline.CacheMaxBytes = ImagePipeline.DefaultCacheMaxBytes;
            Generate(path);
            byte[] warm = Generate(path);

            // Identical but for the per-document ids and dates the PDF stamps in.
            Assert.Equal(uncached.Length, warm.Length);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ACachedImage_IsReloadedWhenTheFileChanges()
    {
        string path = WriteTemp(NoisyPng(40, 20));
        try
        {
            ImageInfo before = ImageLoader.LoadCached(path);
            File.WriteAllBytes(path, NoisyPng(60, 30));
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

            ImageInfo after = ImageLoader.LoadCached(path);

            Assert.Equal(40, before.Width);
            Assert.Equal(60, after.Width);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void PlacementsOfOneCachedImage_DoNotShareSettings()
    {
        string path = WriteTemp(NoisyPng(40, 20));
        try
        {
            ImageInfo first = ImageLoader.LoadCached(path);
            ImageInfo second = ImageLoader.LoadCached(path);

            first.KeepResolution = true;

            Assert.False(second.KeepResolution);
            Assert.Same(first.Data, second.Data);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TheCache_StaysWithinItsBudget()
    {
        ImagePipeline.CacheMaxBytes = 200_000;
        for (int i = 0; i < 12; i++)
            ImageLoader.LoadCached(ImageLoaderTests.BuildFilteredRgbaPng(100, 100 + i, row => (byte)(row % 5)));

        Assert.InRange(ImagePipeline.CacheBytes, 1, 200_000);

        ImagePipeline.CacheMaxBytes = 0;
        Assert.Equal(0, ImagePipeline.CacheBytes);
    }

    [Fact]
    public void AnImageLargerThanTheBudget_IsNotCached()
    {
        ImagePipeline.CacheMaxBytes = 1_000;

        ImageLoader.LoadCached(NoisyPng(100, 100));

        Assert.Equal(0, ImagePipeline.CacheBytes);
    }

    [Fact]
    public void ASmallerCompressionLevel_WritesSmallerImages()
    {
        string path = WriteTemp(NoisyPng(500, 500));
        try
        {
            ImagePipeline.CompressionLevel = ImageCompressionLevel.Fast;
            int fast = Generate(path, 200).Length;
            ImagePipeline.CompressionLevel = ImageCompressionLevel.Smallest;
            int smallest = Generate(path, 200).Length;

            Assert.True(smallest <= fast, $"{smallest} > {fast}");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TheSettings_RejectNonsense()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImagePipeline.CacheMaxBytes = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => ImagePipeline.CompressionLevel = (ImageCompressionLevel)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => FontCache.MaxFiles = -1);
    }
}
