using System.Collections.Generic;
using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Regression coverage for two-pass TOC resolution: a <see cref="VisualToc"/>
/// placed *before* the headings it references (the Dickens-example layout).
/// A single-pass serializer would link every entry to page 1.
/// </summary>
[Collection("Serial")]
public class VisualTocResolutionTests
{
    private static string BuildPdf()
    {
        using var doc = new BonniePDF.Document();

        var toc = new VisualToc { FontName = StandardFonts.Helvetica, FontSize = 12 };
        toc.HeadingIdentifiers.Add("ch1");
        toc.HeadingIdentifiers.Add("ch2");
        toc.HeadingIdentifiers.Add("ch3");
        doc.Add(toc);
        doc.Add(new PageBreak());

        doc.Add(new Heading("Chapter One") { TocIdentifier = "ch1", FontSize = 20 });
        doc.Add(new TextBlock { Text = "Body one.", FontSize = 12 });
        doc.Add(new PageBreak());
        doc.Add(new Heading("Chapter Two") { TocIdentifier = "ch2", FontSize = 20 });
        doc.Add(new TextBlock { Text = "Body two.", FontSize = 12 });
        doc.Add(new PageBreak());
        doc.Add(new Heading("Chapter Three") { TocIdentifier = "ch3", FontSize = 20 });
        doc.Add(new TextBlock { Text = "Body three.", FontSize = 12 });

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }

    /// <summary>Counts non-overlapping occurrences of <paramref name="needle"/>.</summary>
    private static int Count(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    /// <summary>Extracts the destination page-object id from each GoTo action.</summary>
    private static List<string> GoToDestinations(string pdf)
    {
        const string prefix = "/S /GoTo /D [";
        var dests = new List<string>();
        int idx = 0;
        while ((idx = pdf.IndexOf(prefix, idx, System.StringComparison.Ordinal)) >= 0)
        {
            int start = idx + prefix.Length;
            int space = pdf.IndexOf(' ', start);
            if (space > start) dests.Add(pdf.Substring(start, space - start));
            idx = start;
        }
        return dests;
    }

    [Fact]
    public void EmitsThreeInternalGoToLinks()
    {
        string pdf = BuildPdf();
        Assert.Equal(3, Count(pdf, "/S /GoTo"));
    }

    [Fact]
    public void LinksTargetDistinctPagesNotAllPageOne()
    {
        string pdf = BuildPdf();
        var dests = GoToDestinations(pdf);
        Assert.Equal(3, dests.Count);
        Assert.Equal(3, new HashSet<string>(dests).Count);
    }

    [Fact]
    public void EachLinkHasNonZeroClickableRectangle()
    {
        string pdf = BuildPdf();
        var rects = LinkRectangles(pdf);
        Assert.Equal(3, rects.Count);
        foreach (string rect in rects)
        {
            Assert.NotEqual("0 0 0 0", rect);
            string[] parts = rect.Split(' ');
            Assert.True(float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)
                        > float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture));
            Assert.True(float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture)
                        > float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Extracts the /Rect contents of each /Link annotation.</summary>
    private static List<string> LinkRectangles(string pdf)
    {
        var rects = new List<string>();
        int idx = 0;
        while ((idx = pdf.IndexOf("/Subtype /Link", idx, System.StringComparison.Ordinal)) >= 0)
        {
            int rectKey = pdf.IndexOf("/Rect [", idx, System.StringComparison.Ordinal);
            if (rectKey < 0) break;
            int start = rectKey + "/Rect [".Length;
            int end = pdf.IndexOf(']', start);
            if (end > start) rects.Add(pdf.Substring(start, end - start).Trim());
            idx = rectKey + 1;
        }
        return rects;
    }
}
