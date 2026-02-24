using System;
using System.IO;
using System.Linq;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests;

public class TtfFontTests
{
    private const string ArialFontPath = @"C:\Windows\Fonts\arial.ttf";

    [Fact]
    public void Constructor_WithValidFont_LoadsSuccessfully()
    {
        var font = new TtfFont(ArialFontPath);

        Assert.NotNull(font);
        Assert.Equal(ArialFontPath, font.FilePath);
    }

    [Fact]
    public void Constructor_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => new TtfFont("nonexistent.ttf"));
    }

    [Fact]
    public void ParsedFont_HasValidMetadata()
    {
        var font = new TtfFont(ArialFontPath);

        Assert.NotEqual("Unknown", font.PostScriptName);
        Assert.NotEqual("Unknown", font.FontFamily);
        Assert.True(font.UnitsPerEm > 0);
        Assert.True(font.Ascender > 0);
        Assert.True(font.Descender < 0); // Descender is typically negative
        Assert.True(font.NumberOfGlyphs > 0);
    }

    [Fact]
    public void GetGlyphId_ForStandardCharacters_ReturnsNonZero()
    {
        var font = new TtfFont(ArialFontPath);

        // Test common ASCII characters
        ushort glyphA = font.GetGlyphId('A');
        ushort glyphB = font.GetGlyphId('B');
        ushort glyphSpace = font.GetGlyphId(' ');

        Assert.NotEqual(0, glyphA);
        Assert.NotEqual(0, glyphB);
        Assert.NotEqual(0, glyphSpace);
        Assert.NotEqual(glyphA, glyphB); // Different characters should have different glyphs
    }

    [Fact]
    public void GetGlyphId_ForMissingCharacter_ReturnsZero()
    {
        var font = new TtfFont(ArialFontPath);

        // Test a Unicode character unlikely to be in Arial
        ushort glyph = font.GetGlyphId('\uFFFE');
        Assert.Equal(0, glyph);
    }

    [Fact]
    public void GetGlyphWidth_ForValidGlyph_ReturnsPositiveValue()
    {
        var font = new TtfFont(ArialFontPath);

        ushort glyphId = font.GetGlyphId('A');
        int width = font.GetGlyphWidth(glyphId);

        Assert.True(width > 0);
    }

    [Fact]
    public void GetGlyphWidth_ForInvalidGlyph_ReturnsZero()
    {
        var font = new TtfFont(ArialFontPath);

        int width = font.GetGlyphWidth(9999);
        Assert.Equal(0, width);
    }

    [Fact]
    public void GetCharWidth_ForStandardCharacters_ReturnsPositiveValue()
    {
        var font = new TtfFont(ArialFontPath);

        int widthA = font.GetCharWidth('A');
        int widthI = font.GetCharWidth('I');
        int widthW = font.GetCharWidth('W');

        Assert.True(widthA > 0);
        Assert.True(widthI > 0);
        Assert.True(widthW > 0);

        // 'W' should be wider than 'I' in a proportional font
        Assert.True(widthW > widthI);
    }

    [Fact]
    public void GetCharWidthInPoints_CalculatesCorrectly()
    {
        var font = new TtfFont(ArialFontPath);

        float fontSize = 12.0f;
        float widthInPoints = font.GetCharWidthInPoints('A', fontSize);

        Assert.True(widthInPoints > 0);
        Assert.True(widthInPoints < fontSize); // Width should be less than font size
    }

    [Fact]
    public void GetRawFontData_ReturnsValidData()
    {
        var font = new TtfFont(ArialFontPath);

        byte[] rawData = font.GetRawFontData();

        Assert.NotNull(rawData);
        Assert.True(rawData.Length > 0);

        // Check for TrueType signature (0x00010000 or 'true' or 'typ1')
        bool hasValidSignature =
            (rawData[0] == 0x00 && rawData[1] == 0x01 && rawData[2] == 0x00 && rawData[3] == 0x00) ||
            (rawData[0] == 't' && rawData[1] == 'r' && rawData[2] == 'u' && rawData[3] == 'e');

        Assert.True(hasValidSignature);
    }

    [Fact]
    public void GetCharToGlyphMap_ReturnsValidMapping()
    {
        var font = new TtfFont(ArialFontPath);

        var map = font.GetCharToGlyphMap();

        Assert.NotNull(map);
        Assert.True(map.Count > 0);

        // Should contain standard ASCII letters
        Assert.True(map.ContainsKey('A'));
        Assert.True(map.ContainsKey('a'));
        Assert.True(map.ContainsKey('0'));
        Assert.True(map.ContainsKey(' '));
    }

    [Fact]
    public void GetAllGlyphWidths_ReturnsValidWidths()
    {
        var font = new TtfFont(ArialFontPath);

        var widths = font.GetAllGlyphWidths();

        Assert.NotNull(widths);
        Assert.True(widths.Count > 0);

        // Glyph 0 (.notdef) should exist
        Assert.True(widths.ContainsKey(0));

        // All widths should be non-negative
        Assert.All(widths.Values, width => Assert.True(width >= 0));
    }

    [Fact]
    public void BBox_HasValidValues()
    {
        var font = new TtfFont(ArialFontPath);

        Assert.NotNull(font.BBox);
        Assert.Equal(4, font.BBox.Length);

        // xMax should be greater than xMin, yMax greater than yMin
        Assert.True(font.BBox[2] > font.BBox[0]);
        Assert.True(font.BBox[3] > font.BBox[1]);
    }

    [Fact]
    public void Flags_ContainsValidFlags()
    {
        var font = new TtfFont(ArialFontPath);

        // Flags should be set (at minimum, Nonsymbolic bit 32 for Arial)
        Assert.NotEqual(0, font.Flags);

        // Arial should have Nonsymbolic flag (32)
        Assert.True((font.Flags & 32) != 0);
    }

    [Fact]
    public void StemV_HasReasonableValue()
    {
        var font = new TtfFont(ArialFontPath);

        // StemV should be positive and reasonable (typically 50-200 for normal fonts)
        Assert.True(font.StemV > 0);
        Assert.True(font.StemV < 300);
    }

    [Fact]
    public void CapHeight_AndXHeight_HaveReasonableValues()
    {
        var font = new TtfFont(ArialFontPath);

        // Output values for debugging
        System.Diagnostics.Debug.WriteLine($"UnitsPerEm: {font.UnitsPerEm}");
        System.Diagnostics.Debug.WriteLine($"CapHeight: {font.CapHeight}");
        System.Diagnostics.Debug.WriteLine($"XHeight: {font.XHeight}");

        Assert.True(font.CapHeight > 0, $"CapHeight should be > 0, but was {font.CapHeight}");
        Assert.True(font.XHeight > 0, $"XHeight should be > 0, but was {font.XHeight}");

        // XHeight should be less than CapHeight (typically x-height is ~50-70% of cap height)
        // However, if both are 0 or invalid, we may need to skip this test or use fallback
        if (font.XHeight > 0 && font.CapHeight > 0)
        {
            Assert.True(font.XHeight < font.CapHeight,
                $"XHeight ({font.XHeight}) should be < CapHeight ({font.CapHeight})");
        }

        // Both should be less than UnitsPerEm
        Assert.True(font.CapHeight < font.UnitsPerEm);
        Assert.True(font.XHeight < font.UnitsPerEm);
    }

    [Fact]
    public void ItalicAngle_ForRegularFont_IsZeroOrSmall()
    {
        var font = new TtfFont(ArialFontPath);

        // Regular Arial should have italic angle of 0 or very close
        Assert.True(Math.Abs(font.ItalicAngle) < 5);
    }

    [Fact]
    public void MultipleFontLoads_ProduceSameResults()
    {
        var font1 = new TtfFont(ArialFontPath);
        var font2 = new TtfFont(ArialFontPath);

        Assert.Equal(font1.PostScriptName, font2.PostScriptName);
        Assert.Equal(font1.UnitsPerEm, font2.UnitsPerEm);
        Assert.Equal(font1.GetGlyphId('A'), font2.GetGlyphId('A'));
        Assert.Equal(font1.GetCharWidth('A'), font2.GetCharWidth('A'));
    }
}
