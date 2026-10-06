using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Section / internal cross-reference primitives. A <see cref="SectionLink"/>
/// and a <see cref="PageNumberOf"/> both point at a named anchor declared *later*
/// in the document, so resolving them requires the two-pass layout that already
/// backs <see cref="VisualToc"/>. These tests place the reference on page 1 and the
/// anchor on a later page and assert the resolved destination + printed page number.
/// </summary>
[Collection("Serial")]
public class SectionLinkTests
{
    /// <summary>SectionLink + PageNumberOf on page 1; a Section anchor on page 3.</summary>
    private static byte[] BuildDoc()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new SectionLink("Jump to Chapter Three", "ch3"));
        doc.Add(new PageNumberOf("ch3"));
        doc.Add(new PageBreak()); // -> page 2
        doc.Add(new TextBlock { Text = "Body of chapter one." });
        doc.Add(new PageBreak()); // -> page 3
        doc.Add(new Section("ch3"));
        doc.Add(new Heading("Chapter Three") { TocIdentifier = "ch3", FontSize = 20 });

        using var ms = new MemoryStream();
        doc.Save(ms, leaveOpen: true);
        return ms.ToArray();
    }

    private static string Raw(byte[] bytes) => Encoding.GetEncoding("ISO-8859-1").GetString(bytes);

    /// <summary>Counts non-overlapping occurrences of <paramref name="needle"/>.</summary>
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

    /// <summary>Concatenates every decompressed content stream in the file.</summary>
    private static string AllContentStreams(byte[] bytes)
    {
        string text = Raw(bytes);
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

    /// <summary>Extracts the /Rect contents of each /Link annotation.</summary>
    private static List<string> LinkRectangles(string pdf)
    {
        var rects = new List<string>();
        int idx = 0;
        while ((idx = pdf.IndexOf("/Subtype /Link", idx, StringComparison.Ordinal)) >= 0)
        {
            int rectKey = pdf.IndexOf("/Rect [", idx, StringComparison.Ordinal);
            if (rectKey < 0) break;
            int start = rectKey + "/Rect [".Length;
            int end = pdf.IndexOf(']', start);
            if (end > start) rects.Add(pdf.Substring(start, end - start).Trim());
            idx = rectKey + 1;
        }
        return rects;
    }

    [Fact]
    public void FlagsDocumentWithCrossReferencesAsTwoPass()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new SectionLink("Jump", "ch3"));
        doc.Add(new Section("ch3"));
        Assert.True(BonniePDF.TocLayout.RequiresTwoPass(doc));

        using var plain = new BonniePDF.Document();
        plain.Add(new TextBlock { Text = "No cross references here." });
        Assert.False(BonniePDF.TocLayout.RequiresTwoPass(plain));
    }

    [Fact]
    public void EmitsSingleGoToFromSectionLink()
    {
        string pdf = Raw(BuildDoc());
        Assert.Equal(1, Count(pdf, "/S /GoTo"));
    }

    [Fact]
    public void SectionLinkHasNonZeroClickableRectangle()
    {
        var rects = LinkRectangles(Raw(BuildDoc()));
        Assert.Single(rects);
        Assert.NotEqual("0 0 0 0", rects[0]);
        string[] p = rects[0].Split(' ');
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        Assert.True(float.Parse(p[2], ci) > float.Parse(p[0], ci));
        Assert.True(float.Parse(p[3], ci) > float.Parse(p[1], ci));
    }

    [Fact]
    public void PageNumberOfPrintsResolvedPageNoPlaceholder()
    {
        string content = AllContentStreams(BuildDoc());
        // The anchor sits on page 3; the "?" placeholder must not survive.
        Assert.DoesNotContain("(?)", content);
        Assert.Contains("(3)", content);
    }

    [Fact]
    public void HeadingActsAsAnchorViaAnchorId()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new SectionLink("Go to the appendix", "appendix"));
        doc.Add(new PageNumberOf("appendix"));
        doc.Add(new PageBreak()); // -> page 2
        doc.Add(new Heading("Appendix A") { TocIdentifier = "appx", AnchorId = "appendix" });

        using var ms = new MemoryStream();
        doc.Save(ms, leaveOpen: true);
        byte[] bytes = ms.ToArray();

        Assert.Equal(1, Count(Raw(bytes), "/S /GoTo"));
        Assert.Contains("(2)", AllContentStreams(bytes));
    }

    [Fact]
    public void UnknownAnchorFallsBackToPlaceholder()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new PageNumberOf("does-not-exist"));

        using var ms = new MemoryStream();
        doc.Save(ms, leaveOpen: true);
        Assert.Contains("(?)", AllContentStreams(ms.ToArray()));
    }

    [Fact]
    public void RejectsEmptyAnchorIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => new Section(string.Empty));
        Assert.Throws<ArgumentException>(() => new SectionLink("text", string.Empty));
        Assert.Throws<ArgumentException>(() => new PageNumberOf(string.Empty));
    }
}
