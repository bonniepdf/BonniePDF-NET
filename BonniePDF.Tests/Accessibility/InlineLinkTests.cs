using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>Spec 03 (RichText link spans, Hyperlink underline) and 11 §1: links inside a paragraph.</summary>
[Collection("Serial")]
public sealed partial class InlineLinkTests
{
    private const string LinkText = "terms of service and the privacy notice before you place an order with us today";

    private static string Pdf(bool tagged, Action<BonniePDF.Document> build)
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            using BonniePDF.Document doc = new();
            if (tagged)
            {
                doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
                doc.Settings.Language = "en-GB";
                doc.Metadata.Title = "Inline links";
            }
            build(doc);
            return Encoding.Latin1.GetString(doc.GenerateBytes());
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    private static void Paragraph(BonniePDF.Document doc) =>
        doc.Add(new RichText().Span("Please read our ").Link(LinkText, "https://example.com/terms").Span(" and keep a copy."));

    [Fact]
    public void WrappedLinkSpan_GetsARegionPerLine_DescribedByTheWholeSpan()
    {
        string pdf = Pdf(false, Paragraph);
        Assert.Equal(2, Occurrences(pdf, "/Subtype /Link"));
        Assert.Equal(2, Occurrences(pdf, $"/Contents ({LinkText})"));
        Assert.Equal(2, Occurrences(pdf, "/URI (https://example.com/terms)"));
    }

    [Fact]
    public void LinkRegion_LeavesOutTheSpacesAroundIt()
    {
        float[][] rects = LinkRect().Matches(Pdf(false, Paragraph))
            .Select(m => m.Groups[1].Value.Split(' ').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray())
            .ToArray();
        // The first region starts after "Please read our " and its space (Helvetica 12), the second at the left margin.
        Assert.Equal(50 + FontMetrics.GetStringWidth("Please read our ", "Helvetica", 12), rects[0][0], 2);
        Assert.Equal(50f, rects[1][0]);
    }

    [Fact]
    public void LinkRegion_IsTaggedAsALinkInsideTheParagraph()
    {
        string pdf = Pdf(true, Paragraph);
        Assert.Equal(2, Occurrences(pdf, "/S /Link"));
        Assert.Equal(2, Occurrences(pdf, "/OBJR"));
        Assert.Equal(2, StructParent().Count(pdf));
        Assert.True(ParagraphContent().Count(pdf) >= 3, "The paragraph resumes after each link.");
    }

    [Fact]
    public void Hyperlink_IsUnderlinedOnlyWhenAsked()
    {
        Assert.DoesNotMatch(UnderlineSegment(), Pdf(false, d => d.Add(new Hyperlink("mailto:support@example.com", "Contact us"))));
        Assert.Matches(UnderlineSegment(), Pdf(false, d => d.Add(new Hyperlink("mailto:support@example.com", "Contact us") { Underline = true })));
    }

    private static int Occurrences(string text, string value)
    {
        int count = 0;
        for (int i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal)) count++;
        return count;
    }

    [GeneratedRegex(@"/Rect \[([\d. ]+)\]")]
    private static partial Regex LinkRect();

    [GeneratedRegex(@"/StructParent \d+")]
    private static partial Regex StructParent();

    [GeneratedRegex(@"/P << /MCID \d+ >> BDC")]
    private static partial Regex ParagraphContent();

    [GeneratedRegex(@"\d[\d.]* [\d.]+ m [\d.]+ [\d.]+ l")]
    private static partial Regex UnderlineSegment();
}
