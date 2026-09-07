using System;
using System.Globalization;
using Xunit;

namespace BonniePDF.Tests.Document;

public class DocumentTests
{
    [Fact]
    public void CanCreateDocument()
    {
        using BonniePDF.Document doc = new();
        Assert.NotNull(doc);
        Assert.Single(doc.Pages);
    }

    [Fact]
    public void CanAddPage()
    {
        using BonniePDF.Document doc = new();
        doc.AddPage();
        Assert.Equal(2, doc.Pages.Count);
    }

    [Fact]
    public void CanSaveToStream()
    {
        using BonniePDF.Document doc = new();
        using MemoryStream ms = new();

        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
        // Check PDF header
        ms.Position = 0;
        using StreamReader reader = new(ms);
        string? line = reader.ReadLine();
        Assert.NotNull(line);
        Assert.StartsWith("%PDF-1.4", line);
    }

    // The constructor creates page 1 before any post-construction assignment can
    // land, so a page whose margin came from the settings must resolve it at
    // render time rather than freezing the value it saw at construction.
    // Otherwise the object-initializer idiom - new Document { Settings = ... } -
    // is silently ignored on the first page only.
    [Fact]
    public void AppliesMarginFromSettingsAssignedAfterConstruction()
    {
        using BonniePDF.Document doc = new()
        {
            Settings = new BonniePDF.Configuration.DocumentSettings { Margin = 72 },
        };
        doc.Add(new BonniePDF.Elements.Text.TextBlock { Text = "Body copy." });

        string svg = doc.GenerateSvgDocument().Pages[0];
        Assert.Equal(72f, FirstGlyphX(svg), 3);
    }

    [Fact]
    public void KeepsExplicitPageMarginInPreferenceToDocumentMargin()
    {
        using BonniePDF.Document doc = new();
        BonniePDF.Elements.Layout.Page page = doc.AddPage(BonniePDF.Configuration.PdfSize.A4, 20f);

        doc.Settings.Margin = 72;

        Assert.Equal(20f, page.Margin, 3);
    }

    /// <summary>x of the first positioned glyph in an SVG page - the left text origin.</summary>
    private static float FirstGlyphX(string svg)
    {
        // Depending on the font path the exporter emits either <text x="..."> or
        // a space-separated per-glyph x list; the first x value is the text origin either way.
        int start = svg.IndexOf("<text", StringComparison.Ordinal);
        Assert.True(start >= 0, "expected at least one text element in the SVG");
        int attr = svg.IndexOf(" x=\"", start, StringComparison.Ordinal);
        Assert.True(attr >= 0, "expected a positioned text element in the SVG");
        attr += 4;
        int end = svg.IndexOfAny(new[] { '"', ' ' }, attr);
        return float.Parse(svg[attr..end], CultureInfo.InvariantCulture);
    }
}
