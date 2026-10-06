using System.Text;
using BonniePDF.Tests.Accessibility;
using Xunit;

namespace BonniePDF.Tests.Compliance;

[Collection("Serial")]

/// <summary>
/// Three representative tagged documents
/// exercising the stage-1 surface area:
/// <list type="bullet">
///   <item><description><c>business-invoice</c> shape - Heading + body + table with header row, totals;</description></item>
///   <item><description><c>multi-page-flow</c> shape - long table that overflows multiple pages;</description></item>
///   <item><description><c>dickens-novel</c> shape - headings + VisualToc + paragraphs.</description></item>
/// </list>
///
/// Each test asserts the byte-level structural shape that VeraPDF
/// profile <c>ua1</c> verifies (struct tree contents, per-page
/// BDC/EMC balance). The full rule-by-rule pass lives in
/// <see cref="VeraPdfStageOneCloseoutTests"/>, gated on
/// <c>VERAPDF=1</c> + a local VeraPDF service.
/// </summary>
public class Stage1CloseoutTests
{
    private static readonly TextBlock _EmptyMarker = new() { Text = string.Empty };

    // ── Document builders (also used by the opt-in VeraPDF tests) ───

    internal static BonniePDF.Document BuildBusinessInvoiceDocument()
    {
        BonniePDF.Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Invoice #12345",
                Author = "Acme Co.",
                Subject = "Sample tagged invoice for stage-1 closeout.",
            },
            Settings = new DocumentSettings
            {
                Version = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
            },
        };

        doc.Add(new Heading("Invoice")
        {
            TocIdentifier = "invoice", Level = 1,
            FontName = StandardFonts.HelveticaBold, FontSize = 24,
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text = "Customer: Wile E. Coyote.  Invoice number: 12345.  Date: 1 January 2026.",
            FontName = StandardFonts.Helvetica, FontSize = 12,
            Margin = new Thickness(0, 0, 0, 16),
        });

        Table items = new(220f, 80f, 90f, 100f);
        items.AddHeader(
            new TableCell { Content = new TextBlock { Text = "Description", FontName = StandardFonts.HelveticaBold } },
            new TableCell { Content = new TextBlock { Text = "Qty",         FontName = StandardFonts.HelveticaBold } },
            new TableCell { Content = new TextBlock { Text = "Unit price",  FontName = StandardFonts.HelveticaBold } },
            new TableCell { Content = new TextBlock { Text = "Total",       FontName = StandardFonts.HelveticaBold } });
        items.AddRow(new TextBlock { Text = "Roadrunner trap" },     new TextBlock { Text = "2" }, new TextBlock { Text = "£25.00" }, new TextBlock { Text = "£50.00" });
        items.AddRow(new TextBlock { Text = "Rocket-powered skates" }, new TextBlock { Text = "1" }, new TextBlock { Text = "£99.00" }, new TextBlock { Text = "£99.00" });
        items.AddRow(new TextBlock { Text = "Anvil (assorted)" },    new TextBlock { Text = "3" }, new TextBlock { Text = "£14.00" }, new TextBlock { Text = "£42.00" });
        doc.Add(items);

        doc.Add(new Hyperlink("https://acme.example.com/invoice/12345", "View invoice online")
        {
            FontName = StandardFonts.Helvetica, FontSize = 12,
            Margin = new Thickness(0, 24, 0, 0),
        });

        return doc;
    }

    internal static BonniePDF.Document BuildMultiPageFlowDocument()
    {
        BonniePDF.Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Multi-Page Flow",
                Author = "BonniePDF tests",
                Subject = "Long tagged table that overflows multiple pages.",
            },
            Settings = new DocumentSettings
            {
                Version = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
            },
        };

        Table table = new(60f, 240f, 100f);
        table.AddHeader(
            new TableCell { Content = new TextBlock { Text = "#",       FontName = StandardFonts.HelveticaBold } },
            new TableCell { Content = new TextBlock { Text = "Item",    FontName = StandardFonts.HelveticaBold } },
            new TableCell { Content = new TextBlock { Text = "Amount",  FontName = StandardFonts.HelveticaBold } });
        for (int i = 1; i <= 80; i++)
        {
            table.AddRow(
                new TextBlock { Text = i.ToString() },
                new TextBlock { Text = $"Line item {i}: description that fills the row reasonably." },
                new TextBlock { Text = $"£{i * 1.50:F2}" });
        }

        doc.Add(table);
        return doc;
    }

    internal static BonniePDF.Document BuildDickensNovelDocument()
    {
        BonniePDF.Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "The Pickwick Papers (excerpt)",
                Author = "Charles Dickens (public domain)",
                Subject = "Headings + VisualToc + paragraphs.",
            },
            Settings = new DocumentSettings
            {
                Version = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
            },
        };

        var toc = new VisualToc
        {
            FontName = StandardFonts.Helvetica, FontSize = 12,
            Margin = new Thickness(0, 0, 0, 24),
        };
        toc.HeadingIdentifiers.Add("ch1");
        toc.HeadingIdentifiers.Add("ch2");
        toc.HeadingIdentifiers.Add("ch3");
        doc.Add(toc);

        doc.Add(new Heading("Chapter I")           { TocIdentifier = "ch1", Level = 1, FontName = StandardFonts.HelveticaBold, FontSize = 24 });
        doc.Add(new TextBlock { Text = "The first ray of light which illumines the gloom...", FontSize = 12, Margin = new Thickness(0, 0, 0, 12) });
        doc.Add(new Heading("Chapter II")          { TocIdentifier = "ch2", Level = 1, FontName = StandardFonts.HelveticaBold, FontSize = 24 });
        doc.Add(new TextBlock { Text = "That punctual servant of all work, the sun...",        FontSize = 12, Margin = new Thickness(0, 0, 0, 12) });
        doc.Add(new Heading("Chapter III")         { TocIdentifier = "ch3", Level = 1, FontName = StandardFonts.HelveticaBold, FontSize = 24 });
        doc.Add(new TextBlock { Text = "Mr. Pickwick’s apartments in Goswell Street...",        FontSize = 12, Margin = new Thickness(0, 0, 0, 12) });

        return doc;
    }

    private static string Generate(BonniePDF.Document doc)
    {
        using (doc)
        using (var ms = new MemoryStream())
        {
            doc.Save(ms, true);
            return Encoding.ASCII.GetString(ms.ToArray());
        }
    }

    // ── Business invoice ────────────────────────────────────────────

    [Fact]
    public void BusinessInvoice_StructTree_HasHeadingTableLink()
    {
        string pdf = Generate(BuildBusinessInvoiceDocument());

        Assert.Contains("/S /Document", pdf);
        Assert.Contains("/S /H1", pdf);
        Assert.Contains("/S /Table", pdf);
        Assert.Contains("/S /TR", pdf);
        Assert.Contains("/S /TH", pdf);
        Assert.Contains("/S /TD", pdf);
        Assert.Contains("/S /Link", pdf);
        Assert.Contains("/Contents (View invoice online)", pdf);
    }

    [Fact]
    public void BusinessInvoice_AllPagesBdcEmcBalanced()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = Generate(BuildBusinessInvoiceDocument());
            AssertAllPagesBalanced(pdf);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    // ── Multi-page flow ─────────────────────────────────────────────

    [Fact]
    public void MultiPageFlow_OverflowsAndKeepsBdcEmcBalanced()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = Generate(BuildMultiPageFlowDocument());
            string[] streams = PdfPageStreamSplitter.Split(pdf);
            Assert.True(streams.Length >= 2,
                $"expected multi-page-flow to overflow onto >1 page, got {streams.Length}");
            AssertAllPagesBalanced(pdf);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public void MultiPageFlow_StructTreeIncludesTableAndHeader()
    {
        string pdf = Generate(BuildMultiPageFlowDocument());
        Assert.Contains("/S /Table", pdf);
        Assert.Contains("/S /TR", pdf);
        Assert.Contains("/S /TH", pdf);
        Assert.Contains("/S /TD", pdf);
    }

    [Fact]
    public void MultiPageFlow_PageBreakAwareTagging_ProducesSingleTHead()
    {
        // The table overflows across pages. With RepeatHeadersOnNewPage=true,
        // headers are re-drawn visually on each continuation page, but only the
        // first page's headers are tagged as THead (structural). Repeated headers
        // are Artifacts, so there must be exactly one /THead in the structure tree
        // (PDF/UA 7.2-11: Table shall contain zero or one THead kid).
        string pdf = Generate(BuildMultiPageFlowDocument());
        Assert.Equal(1, SubstringCounter.Count(pdf, "/S /THead"));
    }

    // ── Dickens novel / TOC ─────────────────────────────────────────

    [Fact]
    public void DickensNovel_StructTreeHasTocAndHeadings()
    {
        string pdf = Generate(BuildDickensNovelDocument());
        Assert.Contains("/S /TOC", pdf);
        Assert.Contains("/S /TOCI", pdf);
        Assert.Contains("/S /H1", pdf);
        Assert.Contains("/S /P", pdf);
    }

    [Fact]
    public void DickensNovel_TociCountMatchesChapters()
    {
        // 3 chapters listed in the VisualToc => exactly 3 TOCIs.
        string pdf = Generate(BuildDickensNovelDocument());
        Assert.Equal(3, SubstringCounter.Count(pdf, "/S /TOCI"));
    }

    [Fact]
    public void DickensNovel_AllPagesBdcEmcBalanced()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = Generate(BuildDickensNovelDocument());
            AssertAllPagesBalanced(pdf);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    // ── Common assertions ──────────────────────────────────────────

    private static void AssertAllPagesBalanced(string pdf)
    {
        string[] streams = PdfPageStreamSplitter.Split(pdf);
        Assert.NotEmpty(streams);
        foreach (string stream in streams)
        {
            // Container roles (Table, TR, TOC, L, LI, …) emit BMC; leaf roles emit BDC.
            // Both contribute one EMC each, so total opens must equal total EMC.
            int opens = SubstringCounter.Count(stream, "BDC") + SubstringCounter.Count(stream, "BMC");
            int emc = SubstringCounter.Count(stream, "EMC");
            Assert.True(opens == emc, $"unbalanced marked-content on a page: opens={opens} EMC={emc}");
        }
    }
}
