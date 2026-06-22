using System;
using BonniePDF.Core;
using Xunit;

namespace BonniePDF.Tests;

// ColorPalette is the single home for the Tailwind v4 tokens (Color is just the RGBA primitive).
// These tests pin the indexer against the exact published swatch values and check shade handling.
public class ColorPaletteTests
{
    [Fact]
    public void Indexer_ReturnsExactTailwindSwatches()
    {
        Assert.Equal("#FB2C36", ColorPalette.Red[500].ToHexString());
        Assert.Equal("#1D293D", ColorPalette.Slate[800].ToHexString());
        Assert.Equal("#EFF6FF", ColorPalette.Blue[50].ToHexString());
        Assert.Equal("#4D0218", ColorPalette.Rose[950].ToHexString());
    }

    [Fact]
    public void Shades_AreTheElevenCanonicalStops()
    {
        Assert.Equal(new[] { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 }, ColorPalette.Shades);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(550)]
    [InlineData(1000)]
    public void Indexer_RejectsUnknownShade(int shade)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ColorPalette.Red[shade]);
    }
}
