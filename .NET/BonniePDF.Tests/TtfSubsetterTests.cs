using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests;

public class TtfSubsetterTests
{
    private const string ArialFontPath = @"C:\Windows\Fonts\arial.ttf";

    [Fact]
    public void Constructor_WithValidFont_CreatesSubsetter()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        Assert.NotNull(subsetter);
    }

    [Fact]
    public void Constructor_WithNullFont_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TtfSubsetter(null));
    }

    [Fact]
    public void CreateSubset_WithSingleGlyph_ProducesValidFont()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        // Create subset with just 'A'
        ushort glyphIdA = font.GetGlyphId('A');
        var usedGlyphs = new HashSet<ushort> { glyphIdA };
        var charMap = new Dictionary<int, ushort> { ['A'] = glyphIdA };

        byte[] subsetData = subsetter.CreateSubset(usedGlyphs, charMap);

        Assert.NotNull(subsetData);
        Assert.True(subsetData.Length > 0);
        Assert.True(subsetData.Length < font.GetRawFontData().Length); // Subset should be smaller

        // Verify TrueType signature
        Assert.Equal(0x00, subsetData[0]);
        Assert.Equal(0x01, subsetData[1]);
        Assert.Equal(0x00, subsetData[2]);
        Assert.Equal(0x00, subsetData[3]);
    }

    [Fact]
    public void CreateSubset_WithMultipleGlyphs_ProducesValidFont()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        // Create subset with 'A', 'B', 'C'
        var usedGlyphs = new HashSet<ushort>
        {
            font.GetGlyphId('A'),
            font.GetGlyphId('B'),
            font.GetGlyphId('C')
        };

        var charMap = new Dictionary<int, ushort>
        {
            ['A'] = font.GetGlyphId('A'),
            ['B'] = font.GetGlyphId('B'),
            ['C'] = font.GetGlyphId('C')
        };

        byte[] subsetData = subsetter.CreateSubset(usedGlyphs, charMap);

        Assert.NotNull(subsetData);
        Assert.True(subsetData.Length > 0);
    }

    [Fact]
    public void CreateSubset_AlwaysIncludesNotDefGlyph()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        // Create subset WITHOUT explicitly including glyph 0
        ushort glyphIdA = font.GetGlyphId('A');
        var usedGlyphs = new HashSet<ushort> { glyphIdA };
        var charMap = new Dictionary<int, ushort> { ['A'] = glyphIdA };

        subsetter.CreateSubset(usedGlyphs, charMap);

        // .notdef (glyph 0) should be included in the mapping
        Assert.Contains((ushort)0, subsetter.OldToNewGidMap.Keys);
    }

    [Fact]
    public void OldToNewGidMap_AfterSubsetting_HasCorrectMappings()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        ushort glyphIdA = font.GetGlyphId('A');
        ushort glyphIdB = font.GetGlyphId('B');

        var usedGlyphs = new HashSet<ushort> { glyphIdA, glyphIdB };
        var charMap = new Dictionary<int, ushort>
        {
            ['A'] = glyphIdA,
            ['B'] = glyphIdB
        };

        subsetter.CreateSubset(usedGlyphs, charMap);

        var mapping = subsetter.OldToNewGidMap;

        // Should have .notdef + 2 glyphs = 3 total
        Assert.Equal(3, mapping.Count);

        // .notdef should map to 0
        Assert.Equal(0, mapping[0]);

        // Original glyphs should map to new sequential IDs
        Assert.True(mapping.ContainsKey(glyphIdA));
        Assert.True(mapping.ContainsKey(glyphIdB));
        Assert.True(mapping[glyphIdA] < 3);
        Assert.True(mapping[glyphIdB] < 3);
    }

    [Fact]
    public void MaxOriginalGid_AfterSubsetting_IsCorrect()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        ushort glyphIdA = font.GetGlyphId('A');
        ushort glyphIdZ = font.GetGlyphId('Z');

        var usedGlyphs = new HashSet<ushort> { glyphIdA, glyphIdZ };
        var charMap = new Dictionary<int, ushort>
        {
            ['A'] = glyphIdA,
            ['Z'] = glyphIdZ
        };

        subsetter.CreateSubset(usedGlyphs, charMap);

        // MaxOriginalGid should be the highest original GID used
        ushort expectedMax = Math.Max(glyphIdA, glyphIdZ);
        Assert.Equal(expectedMax, subsetter.MaxOriginalGid);
    }

    [Fact]
    public void CreateSubset_WithEmptyGlyphSet_StillIncludesNotDef()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        var usedGlyphs = new HashSet<ushort>();
        var charMap = new Dictionary<int, ushort>();

        byte[] subsetData = subsetter.CreateSubset(usedGlyphs, charMap);

        Assert.NotNull(subsetData);
        Assert.True(subsetData.Length > 0);

        // Should have at least .notdef glyph
        Assert.Single(subsetter.OldToNewGidMap);
        Assert.Contains((ushort)0, subsetter.OldToNewGidMap.Keys);
    }

    [Fact]
    public void CreateSubset_ProducesSmaller_FontThanOriginal()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        // Subset with just a few characters
        var chars = "ABC".ToCharArray();
        var usedGlyphs = new HashSet<ushort>(chars.Select(c => font.GetGlyphId(c)));
        var charMap = chars.ToDictionary(c => (int)c, c => font.GetGlyphId(c));

        byte[] subsetData = subsetter.CreateSubset(usedGlyphs, charMap);
        byte[] originalData = font.GetRawFontData();

        // Subset should be significantly smaller
        Assert.True(subsetData.Length < originalData.Length);
        Assert.True(subsetData.Length < originalData.Length / 2); // At least 50% smaller
    }

    [Fact]
    public void CreateSubset_WithLargeCharacterSet_ProducesValidFont()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        // Subset with all printable ASCII
        var chars = Enumerable.Range(32, 95).Select(i => (char)i).ToArray();
        var usedGlyphs = new HashSet<ushort>(chars.Select(c => font.GetGlyphId(c)).Where(g => g != 0));
        var charMap = chars.Where(c => font.GetGlyphId(c) != 0)
                           .ToDictionary(c => (int)c, c => font.GetGlyphId(c));

        byte[] subsetData = subsetter.CreateSubset(usedGlyphs, charMap);

        Assert.NotNull(subsetData);
        Assert.True(subsetData.Length > 0);

        // Should have .notdef + printable ASCII glyphs
        Assert.True(subsetter.OldToNewGidMap.Count > 90);
    }

    [Fact]
    public void CreateSubset_PreservesGlyphOrdering()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        ushort gidA = font.GetGlyphId('A');
        ushort gidB = font.GetGlyphId('B');
        ushort gidC = font.GetGlyphId('C');

        var usedGlyphs = new HashSet<ushort> { gidA, gidB, gidC };
        var charMap = new Dictionary<int, ushort>
        {
            ['A'] = gidA,
            ['B'] = gidB,
            ['C'] = gidC
        };

        subsetter.CreateSubset(usedGlyphs, charMap);

        var mapping = subsetter.OldToNewGidMap;

        // New GIDs should be sequential starting from 0 (.notdef)
        var newGids = mapping.Values.OrderBy(g => g).ToList();
        for (int i = 0; i < newGids.Count; i++)
        {
            Assert.Equal(i, newGids[i]);
        }
    }

    [Fact]
    public void CreateSubset_MultipleTimes_ProducesConsistentResults()
    {
        var font = new TtfFont(ArialFontPath);

        var chars = "World".ToCharArray().Distinct().ToArray();
        var usedGlyphs = new HashSet<ushort>(chars.Select(c => font.GetGlyphId(c)));
        var charMap = chars.ToDictionary(c => (int)c, c => font.GetGlyphId(c));

        // Create two subsetters
        var subsetter1 = new TtfSubsetter(font);
        var subsetter2 = new TtfSubsetter(font);

        byte[] subset1 = subsetter1.CreateSubset(usedGlyphs, charMap);
        byte[] subset2 = subsetter2.CreateSubset(usedGlyphs, charMap);

        // Results should be identical
        Assert.Equal(subset1.Length, subset2.Length);
        Assert.Equal(subset1, subset2);
    }

    [Fact]
    public void CreateSubset_WithUnicodeCharacters_HandlesCorrectly()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        // Test with Unicode characters that exist in Arial
        var chars = new[] { 'é', 'ñ', 'ü', 'A' };
        var usedGlyphs = new HashSet<ushort>(chars.Select(c => font.GetGlyphId(c)).Where(g => g != 0));
        var charMap = chars.Where(c => font.GetGlyphId(c) != 0)
                           .ToDictionary(c => (int)c, c => font.GetGlyphId(c));

        byte[] subsetData = subsetter.CreateSubset(usedGlyphs, charMap);

        Assert.NotNull(subsetData);
        Assert.True(subsetData.Length > 0);
    }

    [Fact]
    public void OldToNewGidMap_IsReadOnly()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        ushort glyphId = font.GetGlyphId('A');
        var usedGlyphs = new HashSet<ushort> { glyphId };
        var charMap = new Dictionary<int, ushort> { ['A'] = glyphId };

        subsetter.CreateSubset(usedGlyphs, charMap);

        var mapping = subsetter.OldToNewGidMap;

        // Verify it's read-only by checking it's of the correct type
        Assert.IsAssignableFrom<IReadOnlyDictionary<ushort, ushort>>(mapping);
    }

    [Fact]
    public void CreateSubset_WithDuplicateGlyphs_HandlesCorrectly()
    {
        var font = new TtfFont(ArialFontPath);
        var subsetter = new TtfSubsetter(font);

        ushort glyphIdA = font.GetGlyphId('A');

        // Add same glyph multiple times (should be deduplicated)
        var usedGlyphs = new HashSet<ushort> { glyphIdA, glyphIdA, glyphIdA };
        var charMap = new Dictionary<int, ushort> { ['A'] = glyphIdA };

        byte[] subsetData = subsetter.CreateSubset(usedGlyphs, charMap);

        Assert.NotNull(subsetData);

        // Should only have .notdef + 1 glyph = 2 total
        Assert.Equal(2, subsetter.OldToNewGidMap.Count);
    }
}
