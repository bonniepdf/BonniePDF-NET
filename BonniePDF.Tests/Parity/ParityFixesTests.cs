using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BonniePDF.Drawing;
using BonniePDF.Metadata;
using BonniePDF.Tests.Definition;
using Xunit;

namespace BonniePDF.Tests.Parity;

[Collection("Serial")]
public sealed partial class ParityFixesTests
{
    private const string ExpectedCanonicalXmp =
        "<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n" +
        "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n" +
        "  <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n" +
        "    <rdf:Description rdf:about=\"\" xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\">\n" +
        "      <pdfaid:part>2</pdfaid:part>\n" +
        "      <pdfaid:conformance>B</pdfaid:conformance>\n" +
        "    </rdf:Description>\n" +
        "    <rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n" +
        "      <dc:title>\n" +
        "        <rdf:Alt>\n" +
        "          <rdf:li xml:lang=\"x-default\">Spec Report</rdf:li>\n" +
        "        </rdf:Alt>\n" +
        "      </dc:title>\n" +
        "      <dc:creator>\n" +
        "        <rdf:Seq>\n" +
        "          <rdf:li>Jane Doe</rdf:li>\n" +
        "        </rdf:Seq>\n" +
        "      </dc:creator>\n" +
        "      <dc:subject>\n" +
        "        <rdf:Bag>\n" +
        "          <rdf:li>alpha</rdf:li>\n" +
        "          <rdf:li>beta</rdf:li>\n" +
        "          <rdf:li>gamma</rdf:li>\n" +
        "        </rdf:Bag>\n" +
        "      </dc:subject>\n" +
        "      <dc:description>\n" +
        "        <rdf:Alt>\n" +
        "          <rdf:li xml:lang=\"x-default\">Quarterly</rdf:li>\n" +
        "        </rdf:Alt>\n" +
        "      </dc:description>\n" +
        "    </rdf:Description>\n" +
        "    <rdf:Description rdf:about=\"\" xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\">\n" +
        "      <pdf:Keywords>alpha, beta; gamma</pdf:Keywords>\n" +
        "      <pdf:Producer>BonniePDF</pdf:Producer>\n" +
        "    </rdf:Description>\n" +
        "    <rdf:Description rdf:about=\"\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\">\n" +
        "      <xmp:CreatorTool>BonniePDF Test</xmp:CreatorTool>\n" +
        "      <xmp:CreateDate>2026-01-15T09:30:45Z</xmp:CreateDate>\n" +
        "      <xmp:ModifyDate>2026-01-15T09:30:45Z</xmp:ModifyDate>\n" +
        "    </rdf:Description>\n" +
        "  </rdf:RDF>\n" +
        "</x:xmpmeta>\n" +
        "<?xpacket end=\"w\"?>";

    [GeneratedRegex(@"/N 3[\s\S]*?/Length1 \d+[\s\S]*?/Filter /FlateDecode", RegexOptions.None, 5000)]
    private static partial Regex CompressedIccProfileRegex();

    [GeneratedRegex("@font-face", RegexOptions.None, 5000)]
    private static partial Regex FontFaceRuleRegex();

    private static IEnumerable<LayoutNode> Walk(LayoutNode node)
    {
        yield return node;
        foreach (LayoutNode child in node.Children)
            foreach (LayoutNode d in Walk(child))
                yield return d;
    }

    [Fact]
    public void EmitsNumericV1AndV2InRc4EncryptDictionaries()
    {
        using var doc40 = new BonniePDF.Document();
        doc40.Add(new TextBlock("RC4-40"));
        doc40.Settings.WithEncryption40(o => o.WithUserPassword("u").WithOwnerPassword("o"));
        string text40 = Encoding.Latin1.GetString(doc40.GenerateBytes());
        Assert.Contains("/V 1", text40, StringComparison.Ordinal);
        Assert.Contains("/R 2", text40, StringComparison.Ordinal);

        using var doc128 = new BonniePDF.Document();
        doc128.Add(new TextBlock("RC4-128"));
        doc128.Settings.WithEncryption128(o => o.WithUserPassword("u").WithOwnerPassword("o"));
        string text128 = Encoding.Latin1.GetString(doc128.GenerateBytes());
        Assert.Contains("/V 2", text128, StringComparison.Ordinal);
        Assert.Contains("/R 3", text128, StringComparison.Ordinal);
    }

    [Fact]
    public void CompressesPdfASrgbIccProfileWithFlateDecodeAndLength1()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA2b;
        doc.Add(new TextBlock("PDF/A ICC"));
        string text = Encoding.Latin1.GetString(doc.GenerateBytes());
        Assert.Matches(CompressedIccProfileRegex(), text);
    }

    [Fact]
    public void EmitsCanonicalByteIdenticalXmpMetadata()
    {
        var meta = new DocumentMetadata
        {
            Title = "Spec Report",
            Author = "Jane Doe",
            Subject = "Quarterly",
            Keywords = "alpha, beta; gamma",
            Creator = "BonniePDF Test",
            Producer = "BonniePDF",
            CreationDate = new DateTimeOffset(2026, 1, 15, 9, 30, 45, TimeSpan.Zero),
            ModifiedDate = new DateTimeOffset(2026, 1, 15, 9, 30, 45, TimeSpan.Zero),
        };

        string xml = XmpMetadataWriter.GenerateXmpXml(meta, [XmpConformanceBlock.PdfA2B()]);
        Assert.Equal(ExpectedCanonicalXmp, xml);

        string empty = XmpMetadataWriter.GenerateXmpXml(new DocumentMetadata(), Array.Empty<XmpConformanceBlock>());
        Assert.Contains("<?xpacket begin=", empty, StringComparison.Ordinal);
        Assert.Contains("<?xpacket end=\"w\"?>", empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ChecksPdfAEncryptionConflictBeforeAesVersionWithoutMutatingVersion()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Conformance = PdfAConformance.PdfA2b;
        doc.Settings.Version = PdfVersion.Pdf14;
        doc.Settings.Encryption = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.AES_256,
            OwnerPassword = "owner",
        };
        doc.Add(new TextBlock("Conflict"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => doc.GenerateBytes());
        Assert.Contains("PDF/A", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PdfVersion.Pdf14, doc.Settings.Version);
    }

    [Fact]
    public void WritesDisplayDocTitleForTaggedDocumentsWithoutMutatingSettings()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en";
        doc.Metadata.Title = "Tagged";
        doc.Add(new TextBlock("Body"));

        string text = Encoding.Latin1.GetString(doc.GenerateBytes());
        Assert.Contains("/DisplayDocTitle true", text, StringComparison.Ordinal);
        Assert.False(doc.Settings.DisplayDocTitle);
    }

    [Fact]
    public void WritesActionBeforeStructParentInTaggedLinkAnnotations()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en";
        doc.Metadata.Title = "Links";
        doc.Add(new Hyperlink("https://example.com", "Example"));

        string text = Encoding.Latin1.GetString(doc.GenerateBytes());
        int linkStart = text.IndexOf("/Subtype /Link", StringComparison.Ordinal);
        Assert.True(linkStart >= 0);
        int linkEnd = text.IndexOf("endobj", linkStart, StringComparison.Ordinal);
        string dict = text[linkStart..linkEnd];
        int aPos = dict.IndexOf("/A ", StringComparison.Ordinal);
        int spPos = dict.IndexOf("/StructParent ", StringComparison.Ordinal);
        Assert.True(aPos >= 0);
        Assert.True(spPos > aPos);
    }

    [Fact]
    public void ReInheritsDefaultTextStyleFontFeaturesAcrossRegenerations()
    {
        using var doc = new BonniePDF.Document();
        var inherited = new TextBlock("Inherited");
        var explicitEl = new TextBlock("Explicit")
        {
            FontFeatures = new Dictionary<string, int>(StringComparer.Ordinal) { ["smcp"] = 1 },
        };
        doc.Add(inherited);
        doc.Add(explicitEl);

        doc.Settings.DefaultTextStyle = new TextStyle
        {
            FontFeatures = new Dictionary<string, int>(StringComparer.Ordinal) { ["liga"] = 0 },
        };
        doc.GenerateBytes();
        Assert.Equal(0, inherited.FontFeatures!["liga"]);
        Assert.Equal(1, explicitEl.FontFeatures!["smcp"]);

        doc.Settings.DefaultTextStyle = new TextStyle
        {
            FontFeatures = new Dictionary<string, int>(StringComparer.Ordinal) { ["tnum"] = 1 },
        };
        doc.GenerateBytes();
        Assert.Equal(1, inherited.FontFeatures!["tnum"]);
        Assert.False(inherited.FontFeatures.ContainsKey("liga"));
        Assert.Equal(1, explicitEl.FontFeatures!["smcp"]);
    }

    [Fact]
    public void SupportsBestEffortNewerMinorDefinitions()
    {
        string newerMinor = DefinitionFixtures.Text("invalid", "newer-minor");
        using var bestEffortDoc = BonniePDF.Document.FromDefinition(
            newerMinor,
            new DefinitionReadOptions { BestEffort = true });
        SvgDocumentExport exported = bestEffortDoc.GenerateSvgDocument();
        Assert.Contains("Kept", string.Join("\n", exported.Pages), StringComparison.Ordinal);
    }

    [Fact]
    public void SupportsSchemaAdditionsForPageNumberOfSectionLinkAndGrid()
    {
        const string json = """
        {
          "version": 1,
          "minorVersion": 0,
          "pages": [
            {
              "content": [
                { "type": "Section", "anchorId": "intro" },
                { "type": "SectionLink", "anchorTarget": "intro", "text": "Go to intro", "underline": true },
                { "type": "PageNumberOf", "anchorTarget": "intro" },
                {
                  "type": "Grid",
                  "columns": [1, 1],
                  "alternateRowBackgroundColor": "#f5f5f5",
                  "rows": [
                    {
                      "cells": [
                        { "type": "TableCell", "content": { "type": "TextBlock", "text": "A" } },
                        { "type": "TableCell", "content": { "type": "TextBlock", "text": "B" } }
                      ]
                    }
                  ]
                },
                {
                  "type": "Container",
                  "border": { "top": 1, "left": 1, "right": 1, "bottom": 3, "color": "#000000" },
                  "child": { "type": "TextBlock", "text": "Box" }
                }
              ]
            }
          ]
        }
        """;

        DefinitionValidationResult validation = DocumentDefinition.Validate(json);
        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);

        using var built = BonniePDF.Document.FromDefinition(json);
        var sectionLink = Assert.IsType<SectionLink>(built.Pages[0].Elements[1]);
        Assert.True(sectionLink.Underline);
        var grid = Assert.IsType<Grid>(built.Pages[0].Elements[3]);
        Assert.Equal(Color.FromHex("#f5f5f5"), grid.AlternateRowBackgroundColor);
        var container = Assert.IsType<Container>(built.Pages[0].Elements[4]);
        Assert.Equal(1f, container.Border.Top);
        Assert.Equal(3f, container.Border.Bottom);

        string roundTrip = built.ToDefinition().Definition.ToJsonString();
        using JsonDocument parsed = JsonDocument.Parse(roundTrip);
        JsonElement content = parsed.RootElement.GetProperty("pages")[0].GetProperty("content");
        Assert.True(content[1].GetProperty("underline").GetBoolean());
        Assert.Equal("#f5f5f5", content[3].GetProperty("alternateRowBackgroundColor").GetString());
    }

    [Fact]
    public void UsesFontDisplayBlockAndScopesSvgFontFaceCssToMaxPages()
    {
        byte[] sans = FontManager.GetBytesForFamily("liberation sans", "regular")!;
        byte[] mono = FontManager.GetBytesForFamily("liberation mono", "regular")!;

        using var doc = new BonniePDF.Document();
        doc.Fonts.Add(sans, "PageOneFont");
        doc.Fonts.Add(mono, "PageTwoFont");

        doc.Add(new TextBlock("Page one text") { FontFamily = "PageOneFont" });
        Page p2 = doc.AddPage();
        p2.Add(new TextBlock("Page two text") { FontFamily = "PageTwoFont" });

        SvgDocumentExport onePage = doc.GenerateSvgDocument(new SvgPageExportOptions { MaxPages = 1 });
        SvgDocumentExport twoPages = doc.GenerateSvgDocument();

        Assert.Contains("font-display:block", onePage.FontFaceCss, StringComparison.Ordinal);
        Assert.DoesNotContain("font-display:swap", onePage.FontFaceCss, StringComparison.Ordinal);
        Assert.Equal(1, FontFaceRuleRegex().Count(onePage.FontFaceCss));
        Assert.Equal(2, FontFaceRuleRegex().Count(twoPages.FontFaceCss));
    }

    [Fact]
    public void CapturesVisualTocPaintSplitRowCellFillTextBackdropJsonAndTableNoHeaderFixHint()
    {
        using var doc = new BonniePDF.Document();
        doc.Metadata.Title = "Parity Snapshot";
        doc.Settings.Language = "en";

        var toc = new VisualToc
        {
            HeadingIdentifiers = new List<string> { "ch1" },
            Color = Color.FromHex("#123456"),
            FontSize = 11,
        };
        doc.Add(toc);
        doc.Add(new Heading("Chapter 1") { Level = 1, TocIdentifier = "ch1" });

        doc.Add(new CanvasElement((canvas, _, _) =>
        {
            canvas.FillRect(0, 0, 140, 24, Color.FromHex("#003366"));
            canvas.DrawText(6, 4, "Badge", Color.White, 10, StandardFonts.Helvetica, TextAlignment.Left);
        }) { DrawWidth = 140, DrawHeight = 24, AltText = "Badge" });

        var table = new Table(1) { RowBackgroundColor = Color.FromHex("#111111") };
        var longStack = new VerticalStack();
        for (int i = 0; i < 65; i++)
            longStack.Add(new TextBlock($"Split row line {i}") { Color = Color.White, FontSize = 12 });
        table.AddRow(new TableCell { Content = longStack });
        doc.Add(table);

        SvgDocumentExport exported = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true });
        Assert.True(exported.Pages.Count > 1);
        Assert.NotNull(exported.Snapshot);

        List<LayoutNode> nodes = Walk(exported.Snapshot!.Root).ToList();
        LayoutNode? tocNode = nodes.Find(n => string.Equals(n.Type, "VisualToc", StringComparison.Ordinal));
        Assert.Equal("#123456", tocNode?.Paint?.Text);
        Assert.Equal(11f, tocNode?.Paint?.FontSize);

        LayoutNode? cellNode = nodes.Find(n => string.Equals(n.Type, "TableCell", StringComparison.Ordinal));
        Assert.Equal("#111111", cellNode?.Paint?.Background);

        string json = exported.ToJson();
        Assert.Contains("\"textBackdrop\":\"#003366\"", json, StringComparison.Ordinal);

        ComplianceReport report = CompliancePreflight.Evaluate(exported.Snapshot);
        Assert.DoesNotContain(report.Findings, f => string.Equals(f.Code, "contrast-insufficient", StringComparison.Ordinal));

        ComplianceFinding? noHeader = report.Findings.FirstOrDefault(f => string.Equals(f.Code, "table-no-header", StringComparison.Ordinal));
        Assert.NotNull(noHeader);
        Assert.Contains("AddHeader", noHeader!.FixHint, StringComparison.Ordinal);
        Assert.Contains("Grid", noHeader.FixHint, StringComparison.Ordinal);
        Assert.Contains("Presentation = true", noHeader.FixHint, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHeaderRow", noHeader.FixHint, StringComparison.Ordinal);
    }

    [Fact]
    public void LetsFontManagerRegistrationsShadowStandardAliasesInResolveKnownAndSvgProvider()
    {
        byte[] mono = FontManager.GetBytesForFamily("liberation mono", "regular")!;
        try
        {
            FontManager.RegisterFont(mono, "Arial", "Regular");
            string? resolved = FontFamilyResolver.ResolveKnown("Arial", FontWeight.Normal, false);
            Assert.NotNull(resolved);
            Assert.NotEqual(StandardFonts.Helvetica, resolved, StringComparer.Ordinal);

            var provider = new BonniePdfFontProvider();
            Assert.NotNull(provider.Resolve("Arial", 400, false));
        }
        finally
        {
            FontManager.Clear();
            FontManager.RestoreBundledFonts();
        }
    }
}
