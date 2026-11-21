using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// PDF/UA-2 output (spec 04 §3.4). Mirrors the shared fixture accessibility/pdf-ua-2
/// and core/tests/accessibility/pdf-ua-2.test.ts. Every document these tests build
/// passes veraPDF's ua2 profile (and 4 with PDF/A-4); that is checked outside the suite.
/// </summary>
[Collection("Serial")]
public sealed partial class PdfUa2Tests
{
    // ── Settings ────────────────────────────────────────────────────

    [Fact]
    public void RaisesTheDefaultVersionToPdf20()
    {
        using BonniePDF.Document doc = Probe();
        string pdf = Latin1(doc.GenerateBytes());
        Assert.StartsWith("%PDF-2.0", pdf, StringComparison.Ordinal);
        Assert.Equal(PdfVersion.Pdf20, doc.Settings.Version);
    }

    [Fact]
    public void AnExplicitPdf17Throws()
    {
        using BonniePDF.Document doc = Probe();
        doc.Settings.Version = PdfVersion.Pdf17;
        var ex = Assert.Throws<InvalidOperationException>(() => doc.GenerateBytes());
        Assert.Contains("Pdf20", ex.Message, StringComparison.Ordinal);
        Assert.Contains("PdfUa1", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PdfAConformance.PdfA1b)]
    [InlineData(PdfAConformance.PdfA1a)]
    [InlineData(PdfAConformance.PdfA2b)]
    [InlineData(PdfAConformance.PdfA2a)]
    [InlineData(PdfAConformance.PdfA3b)]
    [InlineData(PdfAConformance.PdfA3a)]
    public void PdfA1To3Throw(PdfAConformance conformance)
    {
        using BonniePDF.Document doc = Probe();
        doc.Settings.Conformance = conformance;
        var ex = Assert.Throws<InvalidOperationException>(() => doc.GenerateBytes());
        Assert.Contains("PdfA4", ex.Message, StringComparison.Ordinal);
        Assert.Contains("PdfUa1", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CombinesWithPdfA4()
    {
        using BonniePDF.Document doc = Probe();
        doc.Settings.Conformance = PdfAConformance.PdfA4;
        string pdf = Latin1(doc.GenerateBytes());
        Assert.Equal("4", Xmp(pdf, "pdfaid:part"));
        Assert.Equal("2", Xmp(pdf, "pdfuaid:part"));
    }

    [Fact]
    public void PdfUa1OnPdf20PointsToPdfUa2()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Version = PdfVersion.Pdf20;
        doc.Add(new TextBlock("x"));
        var ex = Assert.Throws<InvalidOperationException>(() => doc.GenerateBytes());
        Assert.Contains("PdfUa2", ex.Message, StringComparison.Ordinal);
    }

    // ── Metadata ────────────────────────────────────────────────────

    [Fact]
    public void IdentifiesPart2Revision2024()
    {
        using BonniePDF.Document doc = Probe();
        string pdf = Latin1(doc.GenerateBytes());
        Assert.Equal("2", Xmp(pdf, "pdfuaid:part"));
        Assert.Equal("2024", Xmp(pdf, "pdfuaid:rev"));
    }

    [Fact]
    public void TheExtensionSchemaDescribesPartAndRevWithPdfA4()
    {
        using BonniePDF.Document doc = Probe();
        doc.Settings.Conformance = PdfAConformance.PdfA4;
        string pdf = Latin1(doc.GenerateBytes());
        Assert.Contains(">part</pdfaProperty:name>", pdf, StringComparison.Ordinal);
        Assert.Contains(">rev</pdfaProperty:name>", pdf, StringComparison.Ordinal);
    }

    // ── Structure ───────────────────────────────────────────────────

    [Fact]
    public void WritesThePdf20NamespaceOnEveryTypeItDefines()
    {
        Dictionary<int, string> objects = Objects(Probe());
        int ns = objects.Single(o => o.Value.Contains("/Type /Namespace", StringComparison.Ordinal)).Key;
        Assert.Contains("/NS (http://iso.org/pdf2/ssn)", objects[ns], StringComparison.Ordinal);
        string root = objects.Values.Single(v => v.Contains("/Type /StructTreeRoot", StringComparison.Ordinal));
        Assert.Contains($"/Namespaces [ {ns} 0 R ]", root, StringComparison.Ordinal);

        var withoutNs = StructElems(objects).Where(e => !e.Value.Contains($"/NS {ns} 0 R", StringComparison.Ordinal))
            .Select(e => Role(e.Value)).Distinct().Order(StringComparer.Ordinal);
        Assert.Equal(["Reference", "TOC", "TOCI"], withoutNs);
        Assert.Contains($"/NS {ns} 0 R", StructElems(objects).Single(e => Role(e.Value) == "Document").Value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(NumberFormat.Decimal, "Decimal")]
    [InlineData(NumberFormat.UpperRoman, "UpperRoman")]
    [InlineData(NumberFormat.LowerRoman, "LowerRoman")]
    [InlineData(NumberFormat.UpperAlpha, "UpperAlpha")]
    [InlineData(NumberFormat.LowerAlpha, "LowerAlpha")]
    public void OrderedListsNameTheirNumbering(NumberFormat format, string expected)
    {
        var list = new OrderedList { NumberFormat = format };
        list.AddItem("One");
        Assert.Equal([expected], ListNumberings(list));
    }

    [Theory]
    [InlineData(BulletStyle.Disc, "Disc")]
    [InlineData(BulletStyle.Circle, "Circle")]
    [InlineData(BulletStyle.Square, "Square")]
    [InlineData(BulletStyle.Dash, "Unordered")]
    [InlineData(BulletStyle.Custom, "Unordered")]
    public void UnorderedListsNameTheirNumbering(BulletStyle style, string expected)
    {
        var list = new UnorderedList { BulletStyle = style };
        list.AddItem("One");
        Assert.Equal([expected], ListNumberings(list));
    }

    [Fact]
    public void TocEntriesAreTociAroundAReferenceThatRefersToTheHeading()
    {
        Dictionary<int, string> objects = Objects(Probe());
        int[] pages = PageIds(objects);
        List<KeyValuePair<int, string>> entries = StructElems(objects).Where(e => Role(e.Value) == "TOCI").ToList();
        Assert.Equal(2, entries.Count);
        string[] headings = ["H1:Intro", "H1:Details"];
        for (int i = 0; i < entries.Count; i++)
        {
            string entry = entries[i].Value;
            // Exactly one kid, a structure element: no marked content of its own.
            Assert.Matches(SingleRef(), Kids(entry));
            int[] kids = Refs(Kids(entry));
            Assert.Equal("Reference", Role(objects[kids[0]]));
            Assert.Contains("/Type /OBJR", objects[kids[0]], StringComparison.Ordinal);
            int target = int.Parse(RefEntry().Match(entry).Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.Equal(headings[i], $"{Role(objects[target])}:{HeadingOnPage(objects, pages, target)}");
        }
        Assert.DoesNotContain(StructElems(objects), e => Role(e.Value) == "Link" && Role(objects[Parent(e.Value)]) is "TOCI" or "Reference");
    }

    // ── Destinations ────────────────────────────────────────────────

    [Fact]
    public void LinksBookmarksAndTheOpeningViewAreStructureDestinations()
    {
        Dictionary<int, string> objects = Objects(Probe());
        int[] pages = PageIds(objects);

        // The TOC's two links, the section link, then the two bookmarks, then the opening view.
        List<(int Page, string Target)> goTos = objects
            .Where(o => o.Value.Contains("/S /GoTo", StringComparison.Ordinal))
            .OrderBy(o => o.Key)
            .Select(o => GoTo(objects, pages, o.Value)).ToList();
        Assert.Equal(
            [(0, "H1:Intro"), (1, "H1:Details"), (1, "H1:Details"), (0, "Reference"), (1, "H1:Details"), (0, "Reference")],
            goTos);

        string catalog = objects.Values.Single(v => v.Contains("/Type /Catalog", StringComparison.Ordinal));
        Assert.Matches(@"/OpenAction \d+ 0 R", catalog);
        Assert.All(objects.Values.Where(v => v.Contains("/Title (", StringComparison.Ordinal) && v.Contains("/Parent", StringComparison.Ordinal)),
            item => Assert.DoesNotContain("/Dest", item, StringComparison.Ordinal));
        Assert.All(objects.Values.Where(v => v.Contains("/Subtype /Link", StringComparison.Ordinal) && !v.Contains("/URI", StringComparison.Ordinal)),
            link => Assert.Matches(@"/A \d+ 0 R", link));
    }

    [Fact]
    public void APageWhereNoElementStartsTargetsTheElementContinuingOntoIt()
    {
        using var doc = new BonniePDF.Document();
        Configure(doc);
        doc.Settings.Bookmarks.Add(new BookmarkItem { Title = "Page two", PageIndex = 1 });
        doc.Add(new TextBlock(string.Concat(Enumerable.Repeat("A sentence that keeps the paragraph going. ", 200))));
        Dictionary<int, string> objects = Objects(doc);
        int[] pages = PageIds(objects);
        Assert.Equal(2, pages.Length);
        string bookmarkAction = objects.Where(o => o.Value.Contains("/S /GoTo", StringComparison.Ordinal)).MinBy(o => o.Key).Value;
        Assert.Equal((1, "P"), GoTo(objects, pages, bookmarkAction));
    }

    [Fact]
    public void TheOpeningActionUsesTheDefaultZoomView()
    {
        using var doc = new BonniePDF.Document();
        Configure(doc);
        doc.Settings.DefaultZoom = ZoomLevel.FitWidth;
        doc.Add(new TextBlock("x"));
        Dictionary<int, string> objects = Objects(doc);
        string action = objects.Values.Single(v => v.Contains("/S /GoTo", StringComparison.Ordinal));
        Assert.Matches(@"/D \[\d+ 0 R /FitH 0\]\s+/SD \[\d+ 0 R /FitH 0\]", action);
    }

    // ── PDF/UA-1 unchanged, honest claim, PDF 2.0 bookmarks ─────────

    [Fact]
    public void PdfUa1OutputHasNoneOfThePdf20Structure()
    {
        using BonniePDF.Document doc = Probe();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        string pdf = Latin1(doc.GenerateBytes());
        foreach (string absent in new[] { "/Namespaces", "/NS ", "/ListNumbering", "/Ref ", "/SD", "/S /Reference", "pdfuaid:rev" })
            Assert.DoesNotContain(absent, pdf, StringComparison.Ordinal);
        Assert.Equal("1", Xmp(pdf, "pdfuaid:part"));
    }

    [Fact]
    public void WithoutFontsTheClaimIsWithheldAndTheStructureKept()
    {
        FontManager.Clear();
        InternalConfiguration.AllowSystemFontFallback = false;
        try
        {
            using BonniePDF.Document doc = Probe();
            string pdf = Latin1(doc.GenerateBytes());
            Assert.Single(doc.Diagnostics, d => d.Category == DiagnosticCategory.Compliance && d.Source == "pdf/ua-2");
            Assert.DoesNotContain("pdfuaid", pdf, StringComparison.Ordinal);
            foreach (string kept in new[] { "%PDF-2.0", "/Namespaces", "/ListNumbering", "/SD" })
                Assert.Contains(kept, pdf, StringComparison.Ordinal);
        }
        finally
        {
            InternalConfiguration.AllowSystemFontFallback = true;
            FontManager.RestoreBundledFonts();
        }
    }

    [Fact]
    public void Pdf20DocumentsKeepTheirBookmarks()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA4;
        doc.Settings.Bookmarks.Add(new BookmarkItem { Title = "Start", PageIndex = 0 });
        doc.Add(new TextBlock("x"));
        string pdf = Latin1(doc.GenerateBytes());
        Assert.Contains("/Type /Outlines", pdf, StringComparison.Ordinal);
        Assert.Contains("/Outlines ", pdf, StringComparison.Ordinal);
    }

    // ── The probe document (fixture input) ──────────────────────────

    internal static BonniePDF.Document Probe()
    {
        var doc = new BonniePDF.Document();
        Configure(doc);
        doc.Settings.Bookmarks.Add(new BookmarkItem { Title = "Start", PageIndex = 0 });
        doc.Settings.Bookmarks.Add(new BookmarkItem { Title = "Second page", PageIndex = 1 });
        doc.Add(new VisualToc { HeadingIdentifiers = { "Intro", "Details" } });
        doc.Add(new Heading("Intro") { TocIdentifier = "Intro", Level = 1 });
        doc.Add(new TextBlock("Some text. " + string.Concat(Enumerable.Repeat("More words here. ", 40))));
        var ordered = new OrderedList();
        ordered.AddItem("One");
        ordered.AddItem("Two");
        doc.Add(ordered);
        var unordered = new UnorderedList();
        unordered.AddItem("Apple");
        unordered.AddItem("Pear");
        doc.Add(unordered);
        var table = new Table(1, 1);
        table.AddHeader("Item", "Amount");
        table.AddRow("A", "1");
        table.AddRow("B", "2");
        doc.Add(table);
        doc.Add(new SectionLink("Go to details", "Details"));
        doc.Add(new Hyperlink("https://example.com", "External"));
        doc.Add(new PageBreak());
        doc.Add(new Heading("Details") { TocIdentifier = "Details", Level = 1 });
        doc.Add(new TextBlock("Second page text."));
        return doc;
    }

    private static void Configure(BonniePDF.Document doc)
    {
        doc.Settings.Accessibility = AccessibilityMode.PdfUa2;
        doc.Settings.Language = "en-GB";
        doc.Metadata.Title = "PDF/UA-2 probe";
    }

    // ── Reading the output ──────────────────────────────────────────

    private static string Latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    /// <summary>The value of an XMP property; each element declares its own namespace.</summary>
    private static string Xmp(string pdf, string property) =>
        Regex.Match(pdf, $"<{Regex.Escape(property)}[^>]*>([^<]*)</{Regex.Escape(property)}>", RegexOptions.None, TimeSpan.FromSeconds(1)).Groups[1].Value;

    private static Dictionary<int, string> Objects(BonniePDF.Document doc)
    {
        using (doc)
        {
            string pdf = Latin1(doc.GenerateBytes());
            return IndirectObject().Matches(pdf).ToDictionary(
                m => int.Parse(m.Groups["id"].Value, CultureInfo.InvariantCulture), m => m.Groups["body"].Value);
        }
    }

    private static IEnumerable<KeyValuePair<int, string>> StructElems(Dictionary<int, string> objects) =>
        objects.Where(o => o.Value.Contains("/Type /StructElem", StringComparison.Ordinal)).OrderBy(o => o.Key);

    private static string Role(string dict) => RoleEntry().Match(dict).Groups[1].Value;

    private static string Kids(string dict) => KidsEntry().Match(dict).Groups[1].Value;

    private static int Parent(string dict) => int.Parse(ParentEntry().Match(dict).Groups[1].Value, CultureInfo.InvariantCulture);

    private static int[] Refs(string array) =>
        StructRef().Matches(array).Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToArray();

    private static int[] PageIds(Dictionary<int, string> objects)
    {
        string pages = objects.Values.Single(v => v.Contains("/Type /Pages", StringComparison.Ordinal));
        return Refs(PagesKids().Match(pages).Groups[1].Value);
    }

    /// <summary>(page index of /D, role of /SD's element) for a go-to action; headings carry their text.</summary>
    private static (int Page, string Target) GoTo(Dictionary<int, string> objects, int[] pages, string action)
    {
        Match m = GoToEntries().Match(action);
        int page = Array.IndexOf(pages, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
        int element = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        string role = Role(objects[element]);
        return (page, role.StartsWith('H') ? $"{role}:{HeadingOnPage(objects, pages, element)}" : role);
    }

    /// <summary>The probe's two headings are one per page: "Intro" on the first, "Details" on the second.</summary>
    private static string HeadingOnPage(Dictionary<int, string> objects, int[] pages, int element)
    {
        Match pg = PageEntry().Match(objects[element]);
        return Array.IndexOf(pages, int.Parse(pg.Groups[1].Value, CultureInfo.InvariantCulture)) == 0 ? "Intro" : "Details";
    }

    private static string[] ListNumberings(ListElementBase list)
    {
        using var doc = new BonniePDF.Document();
        Configure(doc);
        doc.Add(list);
        return StructElems(Objects(doc)).Where(e => Role(e.Value) == "L")
            .Select(e => ListNumberingEntry().Match(e.Value).Groups[1].Value).ToArray();
    }

    [GeneratedRegex(@"(?<id>\d+) 0 obj\s*(?<body>.*?)endobj", RegexOptions.Singleline | RegexOptions.ExplicitCapture)]
    private static partial Regex IndirectObject();

    [GeneratedRegex(@"/S /(\w+)")]
    private static partial Regex RoleEntry();

    [GeneratedRegex(@"/K \[([^\]]*)\]")]
    private static partial Regex KidsEntry();

    [GeneratedRegex(@"/P (\d+) 0 R")]
    private static partial Regex ParentEntry();

    [GeneratedRegex(@"/Pg (\d+) 0 R")]
    private static partial Regex PageEntry();

    [GeneratedRegex(@"(?<!/Pg |/Obj )(\d+) 0 R")]
    private static partial Regex StructRef();

    [GeneratedRegex(@"/Kids \[([^\]]*)\]")]
    private static partial Regex PagesKids();

    [GeneratedRegex(@"/Ref \[ (\d+) 0 R \]")]
    private static partial Regex RefEntry();

    [GeneratedRegex(@"^\s*\d+ 0 R\s*$")]
    private static partial Regex SingleRef();

    [GeneratedRegex(@"/D \[(\d+) 0 R [^\]]*\]\s+/SD \[(\d+) 0 R")]
    private static partial Regex GoToEntries();

    [GeneratedRegex(@"/ListNumbering /(\w+)")]
    private static partial Regex ListNumberingEntry();
}
