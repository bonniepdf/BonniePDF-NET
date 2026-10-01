using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BonniePDF.Compliance;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Export;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Export;

// Mirrors the TypeScript suite tests/export/svg-document-export.test.ts and the
// shared conformance fixtures svg-document-export/with-layout and
// svg-document-export/without-layout, plus the shared JSON conventions the
// export is serialized under.
//
// The fixture document is tagged, and PDF/UA-1 forces the standard-14 faces to
// be embedded, so its Helvetica is drawn in the bundled Liberation Sans and the
// export carries that subset as font CSS.
public partial class SvgDocumentExportTests
{
    [GeneratedRegex("<svg\\b[^>]*>")]
    private static partial Regex SvgRoot();

    [GeneratedRegex("\\b(width|height|viewBox)=\"([^\"]*)\"")]
    private static partial Regex RootAttribute();

    /// <summary>The fixture document: tagged, titled, one light card holding
    /// mid-grey text. It is the compliance-preflight/contrast-insufficient
    /// document, so its report is already pinned by that fixture.</summary>
    private static BonniePDF.Document FixtureDocument()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "Contrast fixture";
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        var caption = new TextBlock { Text = "Muted caption", FontSize = 11, Color = new Color(0x99, 0x99, 0x99) };
        doc.Add(new Container { Child = caption, BackgroundColor = new Color(0xf1, 0xf5, 0xf9) });
        return doc;
    }

    private static SvgDocumentExport WithLayout() =>
        FixtureDocument().GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true });

    private static (double Width, double Height, string ViewBox) RootAttributes(string svg)
    {
        string root = SvgRoot().Match(svg).Value;
        var attrs = RootAttribute().Matches(root).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value, StringComparer.Ordinal);
        return (double.Parse(attrs["width"], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(attrs["height"], System.Globalization.CultureInfo.InvariantCulture),
                attrs["viewBox"]);
    }

    private static IEnumerable<JsonElement> EveryValue(JsonElement element)
    {
        yield return element;
        if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in element.EnumerateArray())
                foreach (JsonElement d in EveryValue(item)) yield return d;
        else if (element.ValueKind == JsonValueKind.Object)
            foreach (JsonProperty p in element.EnumerateObject())
                foreach (JsonElement d in EveryValue(p.Value)) yield return d;
    }

    private static IEnumerable<string> EveryKey(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in element.EnumerateArray())
                foreach (string k in EveryKey(item)) yield return k;
        else if (element.ValueKind == JsonValueKind.Object)
            foreach (JsonProperty p in element.EnumerateObject())
            {
                yield return p.Name;
                foreach (string k in EveryKey(p.Value)) yield return k;
            }
    }

    // ── with layout ──────────────────────────────────────────────────────────

    [Fact]
    public void CarriesVersion1_OnePageWithItsSizeInTheRoot_AndTheEmbeddedFontCss()
    {
        SvgDocumentExport export = WithLayout();

        Assert.Equal(1, SvgDocumentExport.CurrentVersion);
        Assert.Equal(1, export.Version);
        Assert.Single(export.Pages);
        (double width, double height, string viewBox) = RootAttributes(export.Pages[0]);
        Assert.Equal(595.276, width);
        Assert.Equal(841.89, height);
        Assert.Equal("0 0 595.276 841.89", viewBox);
        // PDF/UA-1 embeds the Helvetica substitute, so its subset travels with the pages.
        Assert.Contains("@font-face", export.FontFaceCss, StringComparison.Ordinal);
        Assert.Contains("font-family:'bpf-helvetica'", export.FontFaceCss, StringComparison.Ordinal);
        Assert.Contains("font-family:'bpf-helvetica'", export.Pages[0], StringComparison.Ordinal);
    }

    [Fact]
    public void CarriesTheSnapshot_WithItsOwnVersion_AndDocumentMeta()
    {
        LayoutSnapshot snapshot = WithLayout().Snapshot!;

        Assert.Equal(LayoutSnapshotSchema.Version, snapshot.Version);
        Assert.Equal("Document", snapshot.Root.Type);
        Assert.Equal(1, snapshot.Meta!.AccessibilityMode);
        Assert.Equal("None", snapshot.Meta.Conformance);
        Assert.Equal("en-GB", snapshot.Meta.Language);
        Assert.Equal("Contrast fixture", snapshot.Meta.Title);
    }

    [Fact]
    public void AttachesTheReportTheEvaluatorProduces_WithDefaultOptions()
    {
        SvgDocumentExport export = WithLayout();
        ComplianceReport expected = CompliancePreflight.Evaluate(export.Snapshot!);
        ComplianceReport report = export.Report!;

        Assert.Equal(
            expected.Findings.Select(f => (f.Code, f.Severity, f.Standard, f.Clause, f.NodeId)),
            report.Findings.Select(f => (f.Code, f.Severity, f.Standard, f.Clause, f.NodeId)));

        // The contrast-insufficient fixture's expected report.
        Assert.True(report.Passed);
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(1, report.WarningCount);
        Assert.Equal(1, report.InfoCount);
        Assert.Contains(report.Findings, f =>
            f.Code == "contrast-insufficient" && f.Severity == ComplianceSeverity.Warning &&
            f.Standard == "WCAG 2.2" && f.Clause == "WCAG 2.2 SC 1.4.3 (AA)" && f.NodeId == "n3");
        Assert.Contains(report.Findings, f =>
            f.Code == "doc-no-headings" && f.Severity == ComplianceSeverity.Info &&
            f.Standard == "PDF/UA-1" && f.Clause == "ISO 14289-1:2014 §7.4" && f.NodeId is null);
    }

    // ── without layout ───────────────────────────────────────────────────────

    [Fact]
    public void WithoutLayout_OmitsBothSnapshotAndReport_RatherThanWritingNull()
    {
        SvgDocumentExport export = FixtureDocument().GenerateSvgDocument();

        Assert.Equal(1, export.Version);
        Assert.Single(export.Pages);
        Assert.Contains("font-family:'bpf-helvetica'", export.FontFaceCss, StringComparison.Ordinal);
        Assert.Null(export.Snapshot);
        Assert.Null(export.Report);

        using JsonDocument json = JsonDocument.Parse(export.ToJson());
        Assert.Equal(
            new[] { "fontFaceCss", "pages", "version" },
            json.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    // ── JSON form ────────────────────────────────────────────────────────────

    [Fact]
    public void Json_UsesCamelCaseKeys_AtTheRootAndThroughout()
    {
        using JsonDocument json = JsonDocument.Parse(WithLayout().ToJson());

        Assert.Equal(
            new[] { "fontFaceCss", "pages", "report", "snapshot", "version" },
            json.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.All(EveryKey(json.RootElement), key => Assert.True(char.IsLower(key[0]), key));
    }

    [Fact]
    public void Json_NeverWritesNull()
    {
        using JsonDocument json = JsonDocument.Parse(WithLayout().ToJson());
        Assert.DoesNotContain(EveryValue(json.RootElement), v => v.ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public void Json_WritesSeveritiesAsLowerCaseStrings_NeverTheEnumInteger()
    {
        using JsonDocument json = JsonDocument.Parse(WithLayout().ToJson());
        JsonElement findings = json.RootElement.GetProperty("report").GetProperty("findings");

        Assert.NotEqual(0, findings.GetArrayLength());
        foreach (JsonElement finding in findings.EnumerateArray())
        {
            JsonElement severity = finding.GetProperty("severity");
            Assert.Equal(JsonValueKind.String, severity.ValueKind);
            Assert.Contains(severity.GetString(), new[] { "error", "warning", "info" });
        }
    }

    [Fact]
    public void Json_WritesSinglePrecisionValuesInTheirShortestForm()
    {
        SvgDocumentExport export = WithLayout();
        float height = export.Snapshot!.Pages[0].Height;
        string json = export.ToJson();

        // A single-precision value is written as itself, not as the longer
        // expansion it becomes when widened to double.
        string shortest = height.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        string widened = ((double)height).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        Assert.NotEqual(shortest, widened);
        Assert.Contains($"\"height\":{shortest}", json, StringComparison.Ordinal);
        Assert.DoesNotContain(widened, json, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_KeepsSvgMarkupUnescaped_AsJavaScriptWritesIt()
    {
        string json = WithLayout().ToJson();

        Assert.Contains("\"<svg ", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u003C", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_RoundTripsThePagesAndFontCss()
    {
        SvgDocumentExport export = WithLayout();
        using JsonDocument json = JsonDocument.Parse(export.ToJson());

        Assert.Equal(export.Pages, json.RootElement.GetProperty("pages").EnumerateArray().Select(p => p.GetString()!));
        Assert.Equal(export.FontFaceCss, json.RootElement.GetProperty("fontFaceCss").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("version").GetInt32());
    }
}
