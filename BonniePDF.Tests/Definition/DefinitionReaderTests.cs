using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Definition;

/// <summary>
/// The Document Definition reader (spec 15), driven by the shared conformance fixtures
/// document-definition/valid, generated into this suite from the specification.
///
/// Mirrors core/tests/definition/definition.test.ts.
/// </summary>
public partial class DefinitionReaderTests
{
    [GeneratedRegex("<text[^>]*>([^<]*)</text>")]
    private static partial Regex SvgText();

    [GeneratedRegex(@"/ID \[[^\]]*\]")]
    private static partial Regex TrailerId();

    [GeneratedRegex("[A-Z]{6}\\+")]
    private static partial Regex SubsetPrefix();

    [GeneratedRegex(@"/(?:Creation|Mod)Date[^\n]*")]
    private static partial Regex Dates();

    [GeneratedRegex(@"/BaseFont /(?:[A-Z]{6}\+)?([A-Za-z-]+)")]
    private static partial Regex BaseFont();

    /// <summary>A 1x1 PNG.</summary>
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static string Key(DefinitionIssue i) => $"{i.Path} {i.Code}";

    private static List<string> Texts(string svg) => SvgText().Matches(svg).Select(m => m.Groups[1].Value).ToList();

    private static string Base64(byte[] bytes) => Convert.ToBase64String(bytes);

    private static byte[] Font(string subfamily) =>
        FontManager.GetBytesForFamily("liberation sans", subfamily) ?? throw new InvalidOperationException(subfamily);

    private static string Minimal(string pages = """[{"content":[{"type":"TextBlock","text":"x"}]}]""", string extra = "") =>
        "{\"version\":1,\"minorVersion\":0" + extra + ",\"pages\":" + pages + "}";

    /// <summary>Replaces @KEY@ placeholders, so JSON with many braces needs no interpolation.</summary>
    private static string Fill(string template, params (string Key, string Value)[] values) =>
        values.Aggregate(template, (text, v) => text.Replace($"@{v.Key}@", v.Value, StringComparison.Ordinal));

    /// <summary>The trailer's /ID, the subset prefixes and the dates are random or clock-based; nothing else may differ.</summary>
    private static string Masked(byte[] pdf) =>
        Dates().Replace(SubsetPrefix().Replace(TrailerId().Replace(Encoding.Latin1.GetString(pdf), "/ID []"), "S+"), "");

    public static TheoryData<string> Valid() => DefinitionFixtures.Theory("valid");

    [Theory]
    [MemberData(nameof(Valid))]
    public async Task ValidFixturesBuildAndGenerate(string name)
    {
        string json = DefinitionFixtures.Text("valid", name);
        using JsonDocument fixture = JsonDocument.Parse(json);
        JsonElement expect = fixture.RootElement.TryGetProperty("_expect", out JsonElement e) ? e : default;

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(json);
        SvgDocumentExport exported = await doc.GenerateSvgDocumentAsync(new SvgPageExportOptions { CaptureLayout = true });
        byte[] bytes = doc.GenerateBytes();

        Assert.StartsWith("%PDF-", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
        if (expect.ValueKind == JsonValueKind.Object)
        {
            if (expect.TryGetProperty("pages", out JsonElement pages)) Assert.Equal(pages.GetInt32(), exported.Pages.Count);
            string all = string.Join("\n", exported.Pages.SelectMany(Texts));
            if (expect.TryGetProperty("text", out JsonElement text))
                foreach (JsonElement t in text.EnumerateArray()) Assert.Contains(t.GetString()!, all, StringComparison.Ordinal);
            if (expect.TryGetProperty("ids", out JsonElement ids))
            {
                var found = new List<string>();
                void Walk(LayoutNode n)
                {
                    if (n.SourceId is not null) found.Add(n.SourceId);
                    foreach (LayoutNode c in n.Children) Walk(c);
                }
                Walk(exported.Snapshot!.Root);
                foreach (JsonElement id in ids.EnumerateArray()) Assert.Contains(id.GetString()!, found);
            }
        }
    }

    [Fact]
    public async Task PageChromePutsTheChosenHeaderAndFooterOnEachPage()
    {
        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(DefinitionFixtures.Text("valid", "page-chrome"));

        List<List<string>> pages = (await doc.GenerateSvgDocumentAsync()).Pages.Select(Texts).ToList();

        Assert.Contains("Northwind — Confidential", pages[0]);
        Assert.Contains("Page 1 of ", pages[0]);
        Assert.Contains("Even: 2 / ", pages[1]);
        Assert.DoesNotContain("Northwind — Confidential", pages[1]);
        Assert.Contains("Page 3 of ", pages[2]);
        Assert.All(pages, p => Assert.Contains("3", p));
    }

    [Fact]
    public async Task ReadsTheSameDocumentFromTextAndFromAParsedValue()
    {
        string json = DefinitionFixtures.Text("valid", "report-table-chart");
        using JsonDocument parsed = JsonDocument.Parse(json);

        using BonniePDF.Document a = await BonniePDF.Document.FromDefinitionAsync(json);
        using BonniePDF.Document b = await BonniePDF.Document.FromDefinitionAsync(parsed.RootElement);

        Assert.Equal(Masked(a.GenerateBytes()), Masked(b.GenerateBytes()));
    }

    [Fact]
    public async Task RejectsWithEveryIssueAtOnceNeverAHalfBuiltDocument()
    {
        string bad = Minimal("""[{"content":[{"type":"Spacer","height":"far"},{"type":"Nope"}]}]""");

        DefinitionException error = await Assert.ThrowsAsync<DefinitionException>(() => BonniePDF.Document.FromDefinitionAsync(bad));

        Assert.Equal(2, error.Issues.Count);
        Assert.Contains("2 problems", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CarriesAnImageByDataAndByRefThroughTheHostAndChecksTheHashAndTheType()
    {
        string sha = Convert.ToHexString(SHA256.HashData(Png)).ToLowerInvariant();
        string definition = Minimal(
            """[{"content":[{"type":"Image","resource":"inline","width":20},{"type":"Image","resource":"hosted","width":20}]}]""",
            Fill(""","resources":{"inline":{"kind":"image","mediaType":"image/png","data":"@PNG@","sha256":"@SHA@"},"hosted":{"kind":"image","mediaType":"image/png","ref":"logo-file","sha256":"@SHA@"}}""",
                ("PNG", Base64(Png)), ("SHA", sha)));
        var requested = new List<string>();

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(definition, new DefinitionReadOptions
        {
            Resources = (reference, _, _) =>
            {
                requested.Add(reference);
                return Task.FromResult<byte[]?>(Png);
            },
        });

        Assert.Equal(["logo-file"], requested);
        Assert.Contains("/Subtype /Image", Encoding.Latin1.GetString(doc.GenerateBytes()), StringComparison.Ordinal);

        string tampered = definition.Replace(sha, new string('0', 64), StringComparison.Ordinal);
        DefinitionException hash = await Assert.ThrowsAsync<DefinitionException>(() => BonniePDF.Document.FromDefinitionAsync(tampered, new DefinitionReadOptions { Resources = (_, _, _) => Task.FromResult<byte[]?>(Png) }));
        Assert.Equal(["/resources/hosted/sha256 resource-hash-mismatch", "/resources/inline/sha256 resource-hash-mismatch"], hash.Issues.Select(Key).Order(StringComparer.Ordinal));

        string wrong = Minimal("""[{"content":[{"type":"Image","resource":"pic"}]}]""", Fill(""","resources":{"pic":{"kind":"image","mediaType":"image/jpeg","data":"@PNG@"}}""", ("PNG", Base64(Png))));
        DefinitionException type = await Assert.ThrowsAsync<DefinitionException>(() => BonniePDF.Document.FromDefinitionAsync(wrong));
        Assert.Equal(["/resources/pic resource-type-mismatch"], type.Issues.Select(Key));
    }

    [Fact]
    public async Task ListsEveryUnresolvedRefAtOnce()
    {
        string definition = Minimal(extra: ""","resources":{"a":{"kind":"image","mediaType":"image/png","ref":"one"},"b":{"kind":"image","mediaType":"image/png","ref":"two"}}""");

        DefinitionException error = await Assert.ThrowsAsync<DefinitionException>(() =>
            BonniePDF.Document.FromDefinitionAsync(definition, new DefinitionReadOptions { Resources = (_, _, _) => Task.FromResult<byte[]?>(null) }));
        Assert.Equal(["/resources/a/ref resource-unresolved", "/resources/b/ref resource-unresolved"], error.Issues.Select(Key));

        DefinitionException none = await Assert.ThrowsAsync<DefinitionException>(() => BonniePDF.Document.FromDefinitionAsync(definition));
        Assert.Equal(2, none.Issues.Count);
    }

    [Fact]
    public async Task GivesTheDocumentItsOwnFontsSeveralFacesOfOneFamily()
    {
        string definition = Minimal(
            """[{"content":[{"type":"TextBlock","text":"Plain","fontFamily":"Inter"},{"type":"TextBlock","text":"Strong","fontFamily":"Inter","fontWeight":"bold"}]}]""",
            Fill(""","resources":{"inter":{"kind":"font","mediaType":"font/ttf","data":"@REGULAR@","family":"Inter"},"interBold":{"kind":"font","mediaType":"font/ttf","data":"@BOLD@","family":"Inter","weight":"bold"}}""",
                ("REGULAR", Base64(Font("regular"))), ("BOLD", Base64(Font("bold")))));

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(definition);

        Assert.Equal([("Inter", 400), ("Inter", 700)], doc.Fonts.Faces.Select(f => (f.Family, f.Weight)));
        var embedded = BaseFont().Matches(Encoding.Latin1.GetString(doc.GenerateBytes())).Select(m => m.Groups[1].Value).Distinct().Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["LiberationSans", "LiberationSans-Bold"], embedded);
    }

    [Fact]
    public async Task AppliesSettingsMetadataAndPerPageSizes()
    {
        string definition = Minimal(
            """[{"size":"letter","content":[]},{"content":[]},{"size":{"width":200,"height":300},"margin":"1cm","pageColor":"slate-100","content":[]}]""",
            """
            ,"settings":{"pageSize":"a5","orientation":"landscape","margin":30,"conformance":"pdf/a-2b","language":"en-GB","pdfVersion":"pdf-1.7","namedDestinations":{"start":0},"bookmarks":[{"title":"One","pageIndex":0}]},
             "metadata":{"title":"Titled","author":"Someone","keywords":"a, b","customProperties":{"Project":"X"},"creationDate":"2026-09-30T10:00:00Z"}
            """);

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(definition);

        Assert.Equal("A5", doc.Settings.PageSize.Name);
        Assert.Equal(30f, doc.Settings.Margin);
        Assert.Equal("Titled", doc.Metadata.Title);
        Assert.Equal("X", doc.Metadata.CustomProperties["Project"]);
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.Zero), doc.Metadata.CreationDate);
        Assert.Equal((792, 612), ((int)Math.Round(doc.Pages[0].Size.Width), (int)Math.Round(doc.Pages[0].Size.Height))); // letter, in the document's landscape orientation
        Assert.Equal((595, 420), ((int)Math.Round(doc.Pages[1].Size.Width), (int)Math.Round(doc.Pages[1].Size.Height))); // the document default
        Assert.Equal((300, 200), ((int)Math.Round(doc.Pages[2].Size.Width), (int)Math.Round(doc.Pages[2].Size.Height))); // its own size, in the document's landscape orientation
        Assert.Equal(28.35f, doc.Pages[2].Margin, 2);
    }

    [Fact]
    public async Task MarksEachElementWithItsIdAndLeavesTheOutputAlone()
    {
        string with = Minimal("""[{"content":[{"type":"TextBlock","id":"hello","text":"Same"}]}]""");
        string without = Minimal("""[{"content":[{"type":"TextBlock","text":"Same"}]}]""");

        using BonniePDF.Document a = await BonniePDF.Document.FromDefinitionAsync(with);
        using BonniePDF.Document b = await BonniePDF.Document.FromDefinitionAsync(without);

        Assert.Equal(Masked(a.GenerateBytes()), Masked(b.GenerateBytes()));
        var ids = new List<string>();
        void Walk(LayoutNode n)
        {
            if (n.SourceId is not null) ids.Add(n.SourceId);
            foreach (LayoutNode c in n.Children) Walk(c);
        }
        Walk((await a.GenerateSvgDocumentAsync(new SvgPageExportOptions { CaptureLayout = true })).Snapshot!.Root);
        Assert.Equal(["hello"], ids);
    }

    [Fact]
    public async Task NeedsTheOwnerPasswordFromTheHostForAnEncryptedDefinitionAndNeverStoresIt()
    {
        string definition = Minimal(extra: ""","settings":{"encryption":{"algorithm":"aes-256","permissions":["print","copy"]},"pdfVersion":"pdf-1.7"}""");

        DefinitionException missing = await Assert.ThrowsAsync<DefinitionException>(() => BonniePDF.Document.FromDefinitionAsync(definition));
        Assert.Equal(["/settings/encryption secret-required"], missing.Issues.Select(Key));

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(definition, new DefinitionReadOptions { Passwords = new DefinitionPasswords { Owner = "owner-secret" } });
        Assert.Contains("/Encrypt", Encoding.Latin1.GetString(doc.GenerateBytes()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildsEveryKindOfElement()
    {
        string charts = string.Join(",", new[] { "BarChart", "GroupedBarChart", "LineChart", "AreaChart", "DotPlotChart", "LollipopChart", "PieChart" }.Select(type =>
            Fill("""{"type":"@T@","altText":"@T@","width":300,"height":160,"data":{"series":[{"name":"S","dataPoints":[{"label":"a","value":1.5},{"label":"b","value":2,"labelFormat":"F1"}]}]}}""", ("T", type))));
        string definition = Minimal(
            Fill(
                """
                [{"content":[
                  {"type":"Heading","text":"H","level":2,"tocIdentifier":"h","anchorId":"a"},
                  {"type":"VisualToc","headingIdentifiers":["h"]},
                  {"type":"Section","anchorId":"sec"},
                  {"type":"SectionLink","text":"go","anchorTarget":"sec"},
                  {"type":"PageNumberOf","text":"p {{CurrentPage}}","anchorTarget":"a"},
                  {"type":"Hyperlink","text":"site","uri":"https://example.com"},
                  {"type":"Hyperlink","text":"first","targetPageIndex":0},
                  {"type":"HorizontalStack","spacing":4,"widthDistribution":"weighted","children":[{"weight":2,"element":{"type":"Circle","diameter":10}},{"type":"Ellipse","width":20,"height":8}]},
                  {"type":"Layers","primary":{"type":"Spacer","height":30},"backgrounds":[{"type":"Line","thickness":2,"orientation":"horizontal"}],"foregrounds":[{"type":"Line","orientation":"vertical","length":10}]},
                  {"type":"SemanticGroup","role":"Sect","children":[{"type":"TextBlock","text":"in a section"}]},
                  {"type":"EnsureSpace","minHeight":"2cm","child":{"type":"TextBlock","text":"kept"}},
                  {"type":"PreventPageBreak","child":{"type":"Spacer"}},{"type":"ShowEntire","child":{"type":"Spacer"}},{"type":"StopPaging","child":{"type":"Spacer"}},
                  {"type":"ShowOnce","child":{"type":"Spacer"}},{"type":"SkipOnce","child":{"type":"Spacer"}},{"type":"Unconstrained","child":{"type":"Spacer"}},{"type":"ShowIf","child":{"type":"Spacer"}},
                  {"type":"OrderedList","numberFormat":"lower-roman","startNumber":3,"items":["one",{"type":"OrderedList","items":["sub"]}]},
                  {"type":"Grid","columns":[1,2],"rows":[["a","b"]]},
                  {"type":"QRCode","data":"https://example.com","size":60,"errorCorrection":"high","altText":"link"},
                  {"type":"Barcode","value":"123456789012","symbology":"ean13","altText":"code"},
                  {"type":"DataMatrix","data":"dm","size":40,"altText":"dm"},
                  {"type":"Svg","svgData":"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"20\"><rect width=\"20\" height=\"20\" fill=\"#f00\"/></svg>","width":20,"altText":"red"},
                  @CHARTS@
                ]},{"content":[{"type":"PageBreak"},{"type":"TextBlock","text":"later"}],"background":{"type":"Spacer"},"foreground":{"type":"Spacer"}}]
                """,
                ("CHARTS", charts)),
            ""","settings":{"pageChrome":{"footer":{"default":{"type":"TextBlock","text":"{{CurrentPage}}"}}}}""");

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(definition);

        Assert.True((await doc.GenerateSvgDocumentAsync()).Pages.Count >= 2);
        Assert.Contains("%%EOF", Encoding.Latin1.GetString(doc.GenerateBytes()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormatsChartLabelsWithTheSharedGrammar()
    {
        string chart = """[{"content":[{"type":"BarChart","altText":"x","width":400,"height":200,"showValueLabels":true,"yAxis":{"prefix":"$","minValue":0,"maxValue":20000,"stepCount":5},"data":{"series":[{"dataPoints":[{"label":"a","value":1234.5,"labelFormat":"N0"}]}]}}]}]""";

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(Minimal(chart));

        List<string> texts = Texts((await doc.GenerateSvgDocumentAsync()).Pages[0]);
        Assert.Contains("1,235", texts);
        Assert.Contains("$4,000", texts);
    }
}
