using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// <see cref="TextBlock.PreserveWhitespace"/> - shared conformance fixture
/// layout/preserve-whitespace (spec 03 "Preserved whitespace"). Courier at 10pt is 6pt per code
/// point, so a width of 60pt holds exactly ten code points on a line whatever the platform. The
/// TypeScript suite asserts the same cases.
/// </summary>
[Collection("Serial")]
public class PreserveWhitespaceTests
{
    private const float CodePoint = 6f;

    private static readonly MethodInfo GetOrComputeLines =
        typeof(TextBlock).GetMethod("GetOrComputeLines", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static IList<string> Lines(string text, float widthPoints, bool preserve, int firstLineIndentColumns = 0, bool heading = false)
    {
        TextBlock block = heading ? new Heading(text) : new TextBlock { Text = text };
        block.FontName = StandardFonts.Courier;
        block.FontSize = 10;
        block.PreserveWhitespace = preserve;
        block.FirstLineIndent = firstLineIndentColumns * CodePoint;
        return (IList<string>)GetOrComputeLines.Invoke(block, [widthPoints])!;
    }

    private static IList<string> Lines(string text, int columns, bool preserve, int firstLineIndentColumns = 0)
        => Lines(text, columns * CodePoint, preserve, firstLineIndentColumns);

    [Fact]
    public void Default_DropsLeadingSpaces() =>
        Assert.Equal(["x"], Lines("    x", 10, preserve: false));

    [Fact]
    public void Default_KeepsRunsBetweenWords() =>
        Assert.Equal(["a   b"], Lines("a   b", 10, preserve: false));

    [Fact]
    public void LeadingSpaces_AreKept() =>
        Assert.Equal(["    x"], Lines("    x", 10, preserve: true));

    [Fact]
    public void InteriorRuns_AreKept() =>
        Assert.Equal(["a   b"], Lines("a   b", 10, preserve: true));

    [Fact]
    public void Wraps_AtTheLastSpaceThatFits() =>
        Assert.Equal(["aaaa bbbb", "cc"], Lines("aaaa bbbb cc", 10, preserve: true));

    [Fact]
    public void SpacesAtABreak_AreDropped() =>
        Assert.Equal(["aaaaaaaaaa", "bbb"], Lines("aaaaaaaaaa   bbb", 10, preserve: true));

    [Fact]
    public void UnbrokenToken_BreaksByCodePointRatherThanOverflowing() =>
        Assert.Equal(["abcdefghij", "klmno"], Lines("abcdefghijklmno", 10, preserve: true));

    [Fact]
    public void LeadingSpaces_AreNotABreakPoint_AndContinuationIsNotReIndented() =>
        Assert.Equal(["    abcdef", "gh ij"], Lines("    abcdefgh ij", 10, preserve: true));

    [Fact]
    public void EachSegment_KeepsItsOwnLeadingSpaces() =>
        Assert.Equal(["a", "", "  b"], Lines("a\n\n  b", 10, preserve: true));

    [Fact]
    public void FirstLineIndent_NarrowsOnlyTheFirstLine() =>
        Assert.Equal(["abcdefgh", "ij"], Lines("abcdefghij", 10, preserve: true, firstLineIndentColumns: 2));

    [Fact]
    public void Terminates_WhenTheWidthIsSmallerThanOneCodePoint() =>
        Assert.Equal(["a", "b", "c"], Lines("abc", 0.5f, preserve: true));

    [Fact]
    public void Heading_InheritsTheOption() =>
        Assert.Equal(["    x"], Lines("    x", 10 * CodePoint, preserve: true, heading: true));

    [Fact]
    public void LeadingSpaces_ReachTheContentStream()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            using var doc = new BonniePDF.Document();
            doc.Add(new TextBlock { Text = "    indented", FontName = StandardFonts.Courier, FontSize = 10, PreserveWhitespace = true });
            using var ms = new MemoryStream();
            doc.Save(ms, true);
            Assert.Contains("(    indented)", Encoding.ASCII.GetString(ms.ToArray()), StringComparison.Ordinal);
        }
        finally
        {
            InternalConfiguration.EnableCompression = true;
        }
    }
}
