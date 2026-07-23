using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// this feature advanced typography controls (phase 1a): overline + decoration
/// colour/thickness, and super/subscript baseline shift. These are vertical /
/// visual-only controls that do not change glyph advances, so measurement and
/// pagination are unaffected.
/// </summary>
[Collection("Serial")]
public class AdvancedTypographyTests
{
    private static int Count(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    private static string AllContentStreams(byte[] bytes)
    {
        string text = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        var sb = new StringBuilder();
        int idx = 0;
        while (true)
        {
            int start = text.IndexOf("stream", idx, StringComparison.Ordinal);
            if (start == -1) break;
            int contentStart = start + "stream".Length;
            if (contentStart < text.Length && text[contentStart] == '\r') contentStart++;
            if (contentStart < text.Length && text[contentStart] == '\n') contentStart++;
            int end = text.IndexOf("endstream", contentStart, StringComparison.Ordinal);
            if (end == -1) break;
            sb.Append(Decompress(Encoding.GetEncoding("ISO-8859-1").GetBytes(text, contentStart, end - contentStart)));
            sb.Append('\n');
            idx = end + "endstream".Length;
        }
        return sb.ToString();
    }

    private static string Decompress(byte[] raw)
    {
        try
        {
            using MemoryStream infl = new(raw);
            using ZLibStream zlib = new(infl, CompressionMode.Decompress);
            using StreamReader reader = new(zlib, Encoding.GetEncoding("ISO-8859-1"));
            return reader.ReadToEnd();
        }
        catch
        {
            return Encoding.GetEncoding("ISO-8859-1").GetString(raw);
        }
    }

    private static byte[] Render(BonniePDF.Document doc)
    {
        using var ms = new MemoryStream();
        doc.Save(ms, leaveOpen: true);
        return ms.ToArray();
    }

    [Fact]
    public void ResolveBaselineShift_ReturnsZeroForBaseline()
    {
        Assert.Equal(0f, BaselineShiftResolver.Resolve(BaselineAlignment.Baseline, null, 12));
    }

    [Fact]
    public void ResolveBaselineShift_DerivesFromFontSize()
    {
        Assert.Equal(10 * BaselineShiftResolver.SuperscriptShiftFraction,
            BaselineShiftResolver.Resolve(BaselineAlignment.Superscript, null, 10), 3);
        Assert.Equal(-10 * BaselineShiftResolver.SubscriptShiftFraction,
            BaselineShiftResolver.Resolve(BaselineAlignment.Subscript, null, 10), 3);
    }

    [Fact]
    public void ResolveBaselineShift_ExplicitOverridesAlignment()
    {
        Assert.Equal(-3f, BaselineShiftResolver.Resolve(BaselineAlignment.Superscript, -3f, 12));
    }

    [Fact]
    public void TextStyle_FluentControls()
    {
        TextStyle style = new TextStyle().Overline().Superscript().WithDecorationThickness(2).WithDecorationColor(Color.Red);
        Assert.True(style.Decoration.HasFlag(TextDecoration.Overline));
        Assert.Equal(BaselineAlignment.Superscript, style.BaselineAlignment);
        Assert.Equal(2f, style.DecorationThickness);
        Assert.Equal(Color.Red, style.DecorationColor);
    }

    [Fact]
    public void TextStyle_MergeInheritsNewProperties()
    {
        TextStyle baseStyle = new TextStyle().WithDecorationColor(Color.Red).WithDecorationThickness(1.5f).Subscript();
        TextStyle merged = new TextStyle().MergeWith(baseStyle);
        Assert.Equal(Color.Red, merged.DecorationColor);
        Assert.Equal(1.5f, merged.DecorationThickness);
        Assert.Equal(BaselineAlignment.Subscript, merged.BaselineAlignment);
    }

    [Fact]
    public void TextStyle_SpanOverridesInheritedAlignment()
    {
        TextStyle baseStyle = new TextStyle().Subscript();
        TextStyle merged = new TextStyle().Superscript().MergeWith(baseStyle);
        Assert.Equal(BaselineAlignment.Superscript, merged.BaselineAlignment);
    }

    [Fact]
    public void Overline_EmitsStrokedLine()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Overlined heading", TextDecoration = TextDecoration.Overline });
        string content = AllContentStreams(Render(doc));
        Assert.Matches(@"l\s+S", content);
    }

    [Fact]
    public void UndecoratedText_EmitsNoDecorationStroke()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "The quick brown fox jumps over the lazy dog." });
        string content = AllContentStreams(Render(doc));
        Assert.DoesNotMatch(@"l\s+S", content);
    }

    [Fact]
    public void LetterSpacing_WidensMeasurementByGlyphs()
    {
        var plain = new TextBlock { Text = "ABCDE", FontSize = 12 };
        var spaced = new TextBlock { Text = "ABCDE", FontSize = 12, LetterSpacing = 3 };
        // 5 glyphs × 3pt = 15pt wider.
        Assert.Equal(plain.GetPreferredWidth() + 15, spaced.GetPreferredWidth(), 3);
    }

    [Fact]
    public void WordSpacing_WidensMeasurementBySpaces()
    {
        var plain = new TextBlock { Text = "a b c", FontSize = 12 };
        var spaced = new TextBlock { Text = "a b c", FontSize = 12, WordSpacing = 5 };
        // 2 spaces × 5pt = 10pt wider.
        Assert.Equal(plain.GetPreferredWidth() + 10, spaced.GetPreferredWidth(), 3);
    }

    [Fact]
    public void LetterSpacing_EmitsTcOperator()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Spaced out heading", LetterSpacing = 2 });
        string content = AllContentStreams(Render(doc));
        Assert.Matches(@"\bTc\b", content);
    }

    [Fact]
    public void PlainText_EmitsNoTc()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Normal text" });
        Assert.DoesNotMatch(@"\bTc\b", AllContentStreams(Render(doc)));
    }

    [Fact]
    public void TextStyle_PropagatesSpacingThroughMerge()
    {
        TextStyle baseStyle = new TextStyle().WithLetterSpacing(1.5f).WithWordSpacing(4f);
        TextStyle merged = new TextStyle().MergeWith(baseStyle);
        Assert.Equal(1.5f, merged.LetterSpacing);
        Assert.Equal(4f, merged.WordSpacing);
    }

    [Fact]
    public void LineClamp_ClampsHeightToMaxLines()
    {
        const string longText =
            "This is a deliberately long paragraph that will wrap across several lines when constrained to a narrow column so the clamp has something to truncate.";
        var unclamped = new TextBlock { Text = longText, FontSize = 12 };
        var clamped = new TextBlock { Text = longText, FontSize = 12, MaxLines = 2 };

        Assert.True(clamped.MeasureHeight(160) < unclamped.MeasureHeight(160));
        // Two lines tall (line height = 12 * 1.2 = 14.4).
        Assert.Equal(2 * 12 * 1.2f, clamped.MeasureHeight(160), 2);
    }

    [Fact]
    public void ParagraphSpacing_AddsHeightBetweenParagraphs()
    {
        const string twoParas = "First paragraph here.\nSecond paragraph here.";
        var plain = new TextBlock { Text = twoParas, FontSize = 12 };
        var spaced = new TextBlock { Text = twoParas, FontSize = 12, ParagraphSpacing = 10 };
        Assert.Equal(plain.MeasureHeight(400) + 10, spaced.MeasureHeight(400), 2);
    }

    [Fact]
    public void WavyUnderline_EmitsManySegmentsVsOneForSolid()
    {
        using var solid = new BonniePDF.Document();
        solid.Add(new TextBlock { Text = "Underlined", TextDecoration = TextDecoration.Underline });
        int solidStrokes = Count(AllContentStreams(Render(solid)), "l S");

        using var wavy = new BonniePDF.Document();
        wavy.Add(new TextBlock
        {
            Text = "Underlined",
            TextDecoration = TextDecoration.Underline,
            DecorationStyle = DecorationStyle.Wavy,
        });
        int wavyStrokes = Count(AllContentStreams(Render(wavy)), "l S");

        Assert.Equal(1, solidStrokes);
        Assert.True(wavyStrokes > 3);
    }

    [Fact]
    public void SuperSubscriptRichText_RendersSinglePage()
    {
        using var doc = new BonniePDF.Document();
        var rt = new RichText();
        rt.Span("H", new TextStyle().WithSize(12));
        rt.Span("2", new TextStyle().WithSize(8).Subscript());
        rt.Span("O and E=mc", new TextStyle().WithSize(12));
        rt.Span("2", new TextStyle().WithSize(8).Superscript());
        doc.Add(rt);

        string pdf = Encoding.GetEncoding("ISO-8859-1").GetString(Render(doc));
        Assert.Contains("/Type /Page", pdf);
    }
}
