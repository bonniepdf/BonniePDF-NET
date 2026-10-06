using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BonniePDF.Tests.Compliance;

/// <summary>
/// WCAG 2.2 AA contrast preflight — mirrors the TypeScript
/// <c>contrast-preflight.test.ts</c>. Covers the contrast maths, paint capture into
/// the snapshot, and the evaluator's contrast findings
/// (the specification §4.3).
/// </summary>
public sealed class ContrastPreflightTests
{
    private static IEnumerable<LayoutNode> Walk(LayoutNode node)
    {
        yield return node;
        foreach (LayoutNode child in node.Children)
            foreach (LayoutNode descendant in Walk(child))
                yield return descendant;
    }

    /// <summary>A tagged document with a title and language, so only contrast findings remain.</summary>
    private static BonniePDF.Document BaselineDocument()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "Contrast fixture";
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        return doc;
    }

    private static LayoutSnapshot Capture(BonniePDF.Document doc) =>
        doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot!;

    // ── Contrast maths ────────────────────────────────────────────────────────

    [Fact]
    public void ReferenceLuminances_ForBlackAndWhite()
    {
        Assert.Equal(0.0, Contrast.RelativeLuminance((0, 0, 0)), 10);
        Assert.Equal(1.0, Contrast.RelativeLuminance((255, 255, 255)), 10);
    }

    [Fact]
    public void BlackOnWhite_Is21To1_InEitherOrder()
    {
        Assert.Equal(21.0, Contrast.Ratio((0, 0, 0), (255, 255, 255)), 10);
        Assert.Equal(21.0, Contrast.Ratio((255, 255, 255), (0, 0, 0)), 10);
    }

    [Fact]
    public void ColourAgainstItself_Is1To1()
    {
        Assert.Equal(1.0, Contrast.Ratio((18, 52, 86), (18, 52, 86)), 10);
    }

    [Fact]
    public void MatchesWcagWorkedExample_ForMidGreyOnWhite()
    {
        // #767676 is the canonical "just passes 4.5:1 on white" grey.
        double ratio = Contrast.Ratio((0x76, 0x76, 0x76), (255, 255, 255));
        Assert.True(ratio >= 4.5);
        Assert.True(ratio < 4.6);
    }

    [Fact]
    public void LargeTextThreshold_AppliesAt18pt_Or14ptBold()
    {
        Assert.Equal(4.5, Contrast.RequiredRatio(12f, false));
        Assert.Equal(4.5, Contrast.RequiredRatio(14f, false));
        Assert.Equal(3.0, Contrast.RequiredRatio(14f, true));
        Assert.Equal(3.0, Contrast.RequiredRatio(18f, false));
        Assert.Equal(4.5, Contrast.RequiredRatio(null, false));
    }

    [Fact]
    public void MalformedHex_IsRejectedRatherThanGuessed()
    {
        Assert.True(Contrast.TryParseHexRgb("#ffffff", out var white));
        Assert.Equal(((byte)255, (byte)255, (byte)255), white);
        Assert.False(Contrast.TryParseHexRgb("#fff", out _));
        Assert.False(Contrast.TryParseHexRgb("ffffff", out _));
        Assert.False(Contrast.TryParseHexRgb("#gggggg", out _));
        Assert.False(Contrast.TryParseHexRgb(null, out _));
    }

    // ── Paint capture ─────────────────────────────────────────────────────────

    [Fact]
    public void PaintOf_CapturesTextColourAndSize()
    {
        var tb = new TextBlock { Text = "Body", FontSize = 11, Color = new Color(0x33, 0x33, 0x33) };
        NodePaint? paint = LayoutRecorder.PaintOf(tb, "TextBlock");
        Assert.NotNull(paint);
        Assert.Equal("#333333", paint!.Text);
        Assert.Equal(11f, paint.FontSize);
    }

    [Fact]
    public void PaintOf_CapturesContainerBackground()
    {
        var container = new Container { BackgroundColor = new Color(0xee, 0xee, 0xee) };
        NodePaint? paint = LayoutRecorder.PaintOf(container, "Container");
        Assert.NotNull(paint);
        Assert.Equal("#eeeeee", paint!.Background);
    }

    [Fact]
    public void PaintOf_OmitsAlpha_WhenColourIsOpaque()
    {
        var container = new Container { BackgroundColor = new Color(0, 0, 0) };
        Assert.Null(LayoutRecorder.PaintOf(container, "Container")!.BackgroundAlpha);
    }

    [Fact]
    public void PaintOf_RecordsAlpha_WhenColourIsTranslucent()
    {
        var container = new Container { BackgroundColor = new Color(0, 0, 0, 128) };
        float? alpha = LayoutRecorder.PaintOf(container, "Container")!.BackgroundAlpha;
        Assert.NotNull(alpha);
        Assert.True(alpha < 1f);
    }

    [Fact]
    public void PaintOf_MarksLayeredAndDrawnTypes_AsUnmodellableBackdrops()
    {
        Assert.True(LayoutRecorder.PaintOf(new object(), "Layers")!.BackdropUnknown);
        Assert.True(LayoutRecorder.PaintOf(new object(), "CanvasElement")!.BackdropUnknown);
    }

    [Fact]
    public void PaintOf_ReturnsNull_ForElementThatPaintsNothingRelevant()
    {
        Assert.Null(LayoutRecorder.PaintOf(new object(), "VerticalStack"));
    }

    [Fact]
    public void PaintOf_MarksBold_FromWeight()
    {
        var tb = new TextBlock { Text = "Body", FontSize = 11, Color = new Color(0x33, 0x33, 0x33) };
        tb.FontWeight = FontWeight.Bold;
        Assert.True(LayoutRecorder.PaintOf(tb, "TextBlock")!.Bold);
    }

    // Most examples in both catalogues (and Hyperlink, which exposes no Weight
    // property at all) style bold by setting FontName directly to a bold
    // PostScript name rather than through Weight - that idiom must be detected
    // too, or link/text contrast is checked against the wrong threshold.
    [Fact]
    public void PaintOf_MarksBold_FromBoldPostScriptFontName_IndependentOfWeight()
    {
        var tb = new TextBlock { Text = "Body", FontSize = 11, Color = new Color(0x33, 0x33, 0x33), FontName = StandardFonts.HelveticaBold };
        Assert.True(tb.FontWeight < FontWeight.Bold);
        Assert.True(LayoutRecorder.PaintOf(tb, "TextBlock")!.Bold);
    }

    [Fact]
    public void PaintOf_DoesNotMarkBold_ForRegularWeightFontName()
    {
        var tb = new TextBlock { Text = "Body", FontSize = 11, Color = new Color(0x33, 0x33, 0x33), FontName = StandardFonts.Helvetica };
        Assert.Null(LayoutRecorder.PaintOf(tb, "TextBlock")!.Bold);
    }

    // ── End to end ────────────────────────────────────────────────────────────

    [Fact]
    public void EmitsSchemaV4Snapshots()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new TextBlock { Text = "Body", FontSize = 11 });
        Assert.True(Capture(doc).Version >= 4);
    }

    [Fact]
    public void DarkTextOnDefaultWhitePage_HasNoContrastFindings()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new TextBlock { Text = "Readable body text", FontSize = 11 });

        ComplianceReport report = CompliancePreflight.Evaluate(Capture(doc));

        Assert.DoesNotContain(report.Findings, f => f.Code == "contrast-insufficient");
        Assert.DoesNotContain(report.Findings, f => f.Code == "contrast-unknown");
    }

    [Fact]
    public void FlagsPaleGreyTextOnWhitePage()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new TextBlock { Text = "Barely visible", FontSize = 11, Color = new Color(0xcc, 0xcc, 0xcc) });

        ComplianceReport report = CompliancePreflight.Evaluate(Capture(doc));

        ComplianceFinding? finding = report.Findings.FirstOrDefault(f => f.Code == "contrast-insufficient");
        Assert.NotNull(finding);
        Assert.Equal(ComplianceSeverity.Warning, finding!.Severity);
        Assert.Equal("WCAG 2.2", finding.Standard);
        Assert.Equal("WCAG 2.2 SC 1.4.3 (AA)", finding.Clause);
        Assert.Equal(0, finding.Page);
        // The measured ratio and the threshold must both be in the message, so the
        // fix is a colour change rather than an investigation.
        Assert.Contains(":1", finding.Message);
        Assert.Contains("4.5:1", finding.Message);
    }

    [Fact]
    public void ContrastIsAWarning_SoTheReportStillPasses()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new TextBlock { Text = "Barely visible", FontSize = 11, Color = new Color(0xcc, 0xcc, 0xcc) });

        ComplianceReport report = CompliancePreflight.Evaluate(Capture(doc));

        Assert.Equal(0, report.ErrorCount);
        Assert.True(report.Passed);
    }

    [Fact]
    public void MeasuresAgainstContainerBackground_NotThePage()
    {
        BonniePDF.Document doc = BaselineDocument();
        // White text: fine on the dark container it actually sits on, and hopeless
        // against the white page it would be measured against if the ancestor walk
        // did not work.
        var label = new TextBlock { Text = "On a dark card", FontSize = 11, Color = new Color(255, 255, 255) };
        doc.Add(new Container { Child = label, BackgroundColor = new Color(0x11, 0x22, 0x33) });

        ComplianceReport report = CompliancePreflight.Evaluate(Capture(doc));

        Assert.DoesNotContain(report.Findings, f => f.Code == "contrast-insufficient");
    }

    [Fact]
    public void UsesResolvedTableRowBackground_ForACell()
    {
        BonniePDF.Document doc = BaselineDocument();
        // A dark row fill set on the table, which the cell element itself never carries.
        var table = new Table { RowBackgroundColor = new Color(0x1f, 0x2a, 0x37) };
        table.AddRow(
            new TextBlock { Text = "Light on dark", FontSize = 11, Color = new Color(255, 255, 255) },
            new TextBlock { Text = "Second", FontSize = 11 });
        doc.Add(table);

        LayoutSnapshot snapshot = Capture(doc);

        LayoutNode cell = Walk(snapshot.Root).First(n => n.Type == "TableCell");
        Assert.Equal("#1f2a37", cell.Paint?.Background);
    }

    [Fact]
    public void CapturesHyperlinkPaint_ThoughItIsNotATextElementBase()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new Hyperlink("https://example.com", "Terms and conditions")
        {
            FontSize = 11,
            Color = new Color(0x99, 0x99, 0x99),
        });

        LayoutSnapshot snapshot = Capture(doc);

        LayoutNode link = Walk(snapshot.Root).First(n => n.Type == "Hyperlink");
        Assert.Equal("#999999", link.Paint?.Text);
        Assert.Equal(11f, link.Paint?.FontSize);
        // This link's FontName is the default (regular) Helvetica, so it is
        // correctly not reported bold - see the Bold-via-FontName tests below
        // for the case where it is.
        Assert.Null(link.Paint?.Bold);
    }

    [Fact]
    public void FlagsLowContrastHyperlinkText()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new Hyperlink("https://example.com", "Terms and conditions")
        {
            FontSize = 11,
            Color = new Color(0x99, 0x99, 0x99),
        });

        ComplianceReport report = CompliancePreflight.Evaluate(Capture(doc));

        ComplianceFinding? finding = report.Findings.FirstOrDefault(f => f.Code == "contrast-insufficient");
        Assert.NotNull(finding);
        Assert.Equal(ComplianceSeverity.Warning, finding!.Severity);
        Assert.Equal("WCAG 2.2", finding.Standard);
    }

    // Hyperlink exposes no Weight property at all - bold styling can only be
    // authored by setting FontName to a bold PostScript name. Without that
    // being recognised as bold, this 14pt link would be held to the 4.5:1
    // regular-text threshold and wrongly flagged even though it clears the
    // 3:1 large-text threshold it actually qualifies for.
    [Fact]
    public void AppliesLargeTextThreshold_ToABoldHyperlinkStyledViaFontName()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new Hyperlink("https://example.com", "Terms and conditions")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Color = new Color(0xe1, 0x71, 0x00), // 3.20:1 on white - passes 3:1, fails 4.5:1
        });

        ComplianceReport report = CompliancePreflight.Evaluate(Capture(doc));

        Assert.DoesNotContain(report.Findings, f => f.Code == "contrast-insufficient");
    }

    [Fact]
    public void ReportsUnknown_WhenThePagePaintsABackgroundElement()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Pages[0].PageBackground = new Container { BackgroundColor = new Color(0x80, 0x80, 0x80) };
        doc.Add(new TextBlock { Text = "Over an unknown backdrop", FontSize = 11 });

        LayoutSnapshot snapshot = Capture(doc);
        Assert.True(snapshot.Pages[0].BackdropUnknown);

        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        ComplianceFinding? unknown = report.Findings.FirstOrDefault(f => f.Code == "contrast-unknown");
        Assert.NotNull(unknown);
        Assert.Equal(ComplianceSeverity.Info, unknown!.Severity);
        Assert.DoesNotContain(report.Findings, f => f.Code == "contrast-insufficient");
    }

    [Fact]
    public void ReportsUnknown_ForTranslucentText_RatherThanCompositing()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Add(new TextBlock { Text = "Half there", FontSize = 11, Color = new Color(0x33, 0x33, 0x33, 100) });

        ComplianceReport report = CompliancePreflight.Evaluate(Capture(doc));

        Assert.Contains(report.Findings, f => f.Code == "contrast-unknown");
        Assert.DoesNotContain(report.Findings, f => f.Code == "contrast-insufficient");
    }

    [Fact]
    public void ResolvesPageColour_WhenNoAncestorPaintsABackground()
    {
        BonniePDF.Document doc = BaselineDocument();
        doc.Settings.PageColor = new Color(0x22, 0x22, 0x22);
        // Near-black text on a near-black page: only detectable via the page colour.
        doc.Add(new TextBlock { Text = "Dark on dark", FontSize = 11 });

        LayoutSnapshot snapshot = Capture(doc);
        Assert.Equal("#222222", snapshot.Pages[0].Color);

        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        Assert.Contains(report.Findings, f => f.Code == "contrast-insufficient");
    }

    [Fact]
    public void AppliesLargeTextThreshold_So18ptGreyPassesWhere11ptFails()
    {
        var grey = new Color(0x94, 0x94, 0x94); // ~3.1:1 on white

        BonniePDF.Document small = BaselineDocument();
        small.Add(new TextBlock { Text = "Small grey", FontSize = 11, Color = grey });

        BonniePDF.Document large = BaselineDocument();
        large.Add(new TextBlock { Text = "Large grey", FontSize = 24, Color = grey });

        ComplianceReport smallReport = CompliancePreflight.Evaluate(Capture(small));
        ComplianceReport largeReport = CompliancePreflight.Evaluate(Capture(large));

        Assert.Contains(smallReport.Findings, f => f.Code == "contrast-insufficient");
        Assert.DoesNotContain(largeReport.Findings, f => f.Code == "contrast-insufficient");
    }

    [Fact]
    public void EvaluatesNothing_WhenSnapshotPredatesSchemaV4()
    {
        // A v2 consumer's snapshot carries no Paint, so the contrast rules must emit
        // nothing rather than assume a white page and a black text colour.
        var root = new LayoutNode { Id = "n0", Type = "Document" };
        var child = new LayoutNode { Id = "n1", Type = "TextBlock" };
        child.Rects.Add(new LayoutRect { Page = 0, X = 0, Y = 0, Width = 10, Height = 10 });
        root.Children.Add(child);

        var snapshot = new LayoutSnapshot
        {
            Version = 2,
            Pages = new[] { new LayoutSnapshot.PageSize { Width = 595.276f, Height = 841.89f } },
            Root = root,
            Meta = new SnapshotMeta
            {
                AccessibilityMode = 1,
                Conformance = "None",
                Language = "en",
                Title = "T"
            }
        };

        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        Assert.DoesNotContain(report.Findings, f => f.Standard == "WCAG 2.2");
    }

    // ── includeWcag / IncludeWcag ──────────────────────────────────────────────
    // Mirrors the shared fixture compliance-preflight/contrast-suppressed: the WCAG
    // family is a rule family the caller can switch off, and switching it off must
    // remove the findings from the counts too — not merely from a rendered list.

    /// <summary>The fixture document: mid-grey text on a light container fill.</summary>
    private static LayoutSnapshot SuppressionFixture()
    {
        BonniePDF.Document doc = BaselineDocument();
        var caption = new TextBlock { Text = "Muted caption", FontSize = 11, Color = new Color(0x99, 0x99, 0x99) };
        doc.Add(new Container { Child = caption, BackgroundColor = new Color(0xf1, 0xf5, 0xf9) });
        return Capture(doc);
    }

    [Fact]
    public void RunsWcagRulesByDefault_SoTheSingleArgumentOverloadIsUnchanged()
    {
        ComplianceReport report = CompliancePreflight.Evaluate(SuppressionFixture());

        Assert.Single(report.Findings.Where(f => f.Code == "contrast-insufficient"));
        Assert.Equal(1, report.WarningCount);
    }

    [Fact]
    public void IncludeWcagFalse_SuppressesEveryWcagFinding_AndItsContributionToTheCounts()
    {
        ComplianceReport report = CompliancePreflight.Evaluate(
            SuppressionFixture(), new CompliancePreflightOptions { IncludeWcag = false });

        Assert.DoesNotContain(report.Findings, f => f.Standard == "WCAG 2.2");
        Assert.Equal(0, report.WarningCount);
    }

    [Fact]
    public void IncludeWcagFalse_LeavesPdfUaFindingsAndPassedUntouched()
    {
        LayoutSnapshot snapshot = SuppressionFixture();
        ComplianceReport on = CompliancePreflight.Evaluate(snapshot);
        ComplianceReport off = CompliancePreflight.Evaluate(
            snapshot, new CompliancePreflightOptions { IncludeWcag = false });

        static string[] Ua(ComplianceReport r) =>
            r.Findings.Where(f => f.Standard == "PDF/UA-1").Select(f => f.Code).ToArray();

        Assert.Equal(Ua(on), Ua(off));
        Assert.Equal(on.ErrorCount, off.ErrorCount);
        Assert.Equal(on.Passed, off.Passed);
    }

    [Fact]
    public void IncludeWcagFalse_IsExactlyTheUnfilteredReportMinusItsWcagFindings()
    {
        LayoutSnapshot snapshot = SuppressionFixture();
        ComplianceReport on = CompliancePreflight.Evaluate(snapshot);
        ComplianceReport off = CompliancePreflight.Evaluate(
            snapshot, new CompliancePreflightOptions { IncludeWcag = false });

        Assert.Equal(
            on.Findings.Where(f => f.Standard != "WCAG 2.2").Select(f => f.Code).ToArray(),
            off.Findings.Select(f => f.Code).ToArray());
    }

    [Fact]
    public void CaptureStaysObservational_ByteIdenticalSvg()
    {
        static BonniePDF.Document Build()
        {
            BonniePDF.Document doc = BaselineDocument();
            var label = new TextBlock { Text = "On a card", FontSize = 11, Color = new Color(0x33, 0x33, 0x33) };
            doc.Add(new Container { Child = label, BackgroundColor = new Color(0xee, 0xee, 0xee) });
            return doc;
        }

        SvgDocumentExport withCapture = Build().GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true });
        SvgDocumentExport without = Build().GenerateSvgDocument(new SvgPageExportOptions());

        Assert.Equal(without.Pages, withCapture.Pages);
        Assert.NotNull(withCapture.Snapshot);
        Assert.Null(without.Snapshot);
    }
}
