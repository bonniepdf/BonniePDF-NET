using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BonniePDF.Tests.Compliance;

/// <summary>
/// Accessibility / compliance preflight — mirrors the TypeScript
/// <c>compliance-preflight.test.ts</c>. Verifies a11y capture into the snapshot and
/// the evaluator's per-element findings against the PDF/UA-1 rule set
/// (the specification).
/// </summary>
public sealed class CompliancePreflightTests
{
    private static System.Collections.Generic.IEnumerable<LayoutNode> Walk(LayoutNode node)
    {
        yield return node;
        foreach (LayoutNode child in node.Children)
            foreach (LayoutNode d in Walk(child))
                yield return d;
    }

    [Fact]
    public void AccessibilityOf_DerivesHeadingLevelAndTagged()
    {
        var h = new Heading("Title") { Level = 2 };
        NodeAccessibility? a11y = LayoutRecorder.AccessibilityOf(h, "Heading");
        Assert.NotNull(a11y);
        Assert.Equal("H2", a11y!.Role);
        Assert.True(a11y.Tagged);
        Assert.False(a11y.Artifact);
        Assert.Equal(2, a11y.HeadingLevel);
    }

    [Fact]
    public void AccessibilityOf_AltlessQrCode_IsImageLikeArtifact()
    {
        var qr = new QRCode { Data = "https://example.com" };
        NodeAccessibility? a11y = LayoutRecorder.AccessibilityOf(qr, "QRCode");
        Assert.NotNull(a11y);
        Assert.Equal("Artifact", a11y!.Role);
        Assert.True(a11y.Artifact);
        Assert.False(a11y.Tagged);
        Assert.True(a11y.ImageLike);
        Assert.Null(a11y.Alt);
    }

    [Fact]
    public void CapturesA11y_AndFlagsAltlessFigureAsError()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "Report";
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Add(new Heading("Overview") { Level = 1 });
        doc.Add(new QRCode { Data = "https://example.com" });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        Assert.NotNull(snapshot);

        LayoutNode qrNode = Walk(snapshot.Root).First(n => n.Type == "QRCode");
        Assert.NotNull(qrNode.A11y);
        Assert.Equal("Artifact", qrNode.A11y!.Role);
        Assert.True(qrNode.A11y.ImageLike);

        Assert.NotNull(snapshot.Meta);
        Assert.Equal((int)AccessibilityMode.PdfUa1, snapshot.Meta!.AccessibilityMode);
        Assert.Equal("en-GB", snapshot.Meta.Language);
        Assert.Equal("Report", snapshot.Meta.Title);

        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        ComplianceFinding? figure = report.Findings.FirstOrDefault(f => f.Code == "figure-missing-alt");
        Assert.NotNull(figure);
        Assert.Equal(ComplianceSeverity.Error, figure!.Severity);
        Assert.Equal(qrNode.Id, figure.NodeId);
        Assert.Equal(0, figure.Page);
        Assert.False(report.Passed);
    }

    [Fact]
    public void PassesDescribedFigure_WithTitleLanguageAndH1()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "Accessible";
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en";
        doc.Add(new Heading("Intro") { Level = 1 });
        doc.Add(new QRCode { Data = "x", AltText = "Scan to open the portal" });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);

        Assert.DoesNotContain(report.Findings, f => f.Code == "figure-missing-alt");
        Assert.Equal(0, report.ErrorCount);
        Assert.True(report.Passed);
    }

    [Fact]
    public void FlagsMissingLanguageAndTitle_ForUntaggedDocument_AsWarnings()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "Body text", FontSize = 12 });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);

        Assert.Equal(ComplianceSeverity.Info, report.Findings.First(f => f.Code == "doc-not-tagged").Severity);
        Assert.Equal(ComplianceSeverity.Warning, report.Findings.First(f => f.Code == "doc-lang-missing").Severity);
        Assert.Equal(ComplianceSeverity.Warning, report.Findings.First(f => f.Code == "doc-title-missing").Severity);
        Assert.Contains(report.Findings, f => f.Code == "doc-no-headings");
    }

    [Fact]
    public void CapturesTableCells_AndDoesNotFlagTableWithHeaders()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "T";
        doc.Settings.Language = "en";
        var table = new Table(1, 1);
        table.AddHeader(new TextBlock { Text = "Property" }, new TextBlock { Text = "Description" });
        table.AddRow(new TextBlock { Text = "Margin.Top" }, new TextBlock { Text = "Space above" });
        doc.Add(table);

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;

        var roles = new HashSet<string>();
        void Collect(LayoutNode n)
        {
            if (n.A11y?.Role is string r) roles.Add(r);
            foreach (LayoutNode c in n.Children) Collect(c);
        }
        Collect(snapshot.Root);
        Assert.Contains("TH", roles);
        Assert.Contains("TD", roles);
        Assert.Contains("TR", roles);

        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        Assert.DoesNotContain(report.Findings, f => f.Code == "table-no-header");
    }

    [Fact]
    public void DoesNotFlagPresentationTable_CapturingItAsDiv()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "T";
        doc.Settings.Language = "en";
        var table = new Table(1, 1) { Presentation = true }; // layout grid, not tabular data
        table.AddRow(new TextBlock { Text = "Bill to" }, new TextBlock { Text = "Ship to" });
        table.AddRow(new TextBlock { Text = "Acme Inc" }, new TextBlock { Text = "42 Road" });
        doc.Add(table);

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;

        var roles = new HashSet<string>();
        void Collect(LayoutNode n)
        {
            if (n.A11y?.Role is string r) roles.Add(r);
            foreach (LayoutNode c in n.Children) Collect(c);
        }
        Collect(snapshot.Root);
        Assert.Contains("Div", roles);
        Assert.DoesNotContain("Table", roles);
        Assert.DoesNotContain("TD", roles);
        Assert.DoesNotContain("TH", roles);

        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        Assert.DoesNotContain(report.Findings, f => f.Code == "table-no-header");
    }

    [Fact]
    public void FlagsTableWithNoHeaderCells()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "T";
        doc.Settings.Language = "en";
        var table = new Table(1, 1);
        table.AddRow(new TextBlock { Text = "a" }, new TextBlock { Text = "b" });
        table.AddRow(new TextBlock { Text = "c" }, new TextBlock { Text = "d" });
        doc.Add(table);

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        ComplianceFinding? finding = report.Findings.FirstOrDefault(f => f.Code == "table-no-header");
        Assert.NotNull(finding);
        Assert.Equal(ComplianceSeverity.Warning, finding!.Severity);
    }

    [Fact]
    public void FlagsSkippedHeadingLevel()
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "T";
        doc.Settings.Language = "en";
        doc.Add(new Heading("One") { Level = 1 });
        doc.Add(new Heading("Three") { Level = 3 });

        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);

        ComplianceFinding? skip = report.Findings.FirstOrDefault(f => f.Code == "heading-skip");
        Assert.NotNull(skip);
        Assert.Equal("H3", skip!.Role);
        Assert.Equal(ComplianceSeverity.Warning, skip.Severity);
    }
}
