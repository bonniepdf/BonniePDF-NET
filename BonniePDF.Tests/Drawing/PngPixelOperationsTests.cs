using Xunit;

namespace BonniePDF.Tests.Drawing;

public sealed class PngPixelOperationsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(393)]
    public void ChannelSeparation_IsExactAndStaysInsideEachSpan(int pixels)
    {
        var random = new Random(20261010 + pixels);
        byte[] source = new byte[pixels * 4 + 6];
        random.NextBytes(source);
        byte[] colors = Enumerable.Repeat((byte)197, pixels * 3 + 6).ToArray();
        byte[] alphas = Enumerable.Repeat((byte)197, pixels + 6).ToArray();
        byte[] before = (byte[])source.Clone();

        PngPixelOperations.SplitRgbaRow(source.AsSpan(3, pixels * 4),
            colors.AsSpan(3, pixels * 3), alphas.AsSpan(3, pixels));

        for (int p = 0; p < pixels; p++)
        {
            Assert.Equal(source[3 + p * 4], colors[3 + p * 3]);
            Assert.Equal(source[4 + p * 4], colors[4 + p * 3]);
            Assert.Equal(source[5 + p * 4], colors[5 + p * 3]);
            Assert.Equal(source[6 + p * 4], alphas[3 + p]);
        }
        Assert.Equal(before, source);
        Assert.All(colors.Take(3).Concat(colors.TakeLast(3)), value => Assert.Equal(197, value));
        Assert.All(alphas.Take(3).Concat(alphas.TakeLast(3)), value => Assert.Equal(197, value));
    }

    [Fact]
    public void InconsistentPlaneLengths_AreRejectedBeforeWriting()
    {
        byte[] color = [197, 197, 197];
        Assert.Throws<ArgumentException>(() => PngPixelOperations.SplitRgbaRow(new byte[5], color, new byte[1]));
        Assert.Throws<ArgumentException>(() => PngPixelOperations.SplitRgbaRow(new byte[8], color, new byte[2]));
        Assert.Equal(new byte[] { 197, 197, 197 }, color);
    }
}
